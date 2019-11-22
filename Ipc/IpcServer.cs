using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.Ipc {

    /// <summary>
    /// IPC named pipe server.
    /// </summary>
    public sealed class IpcServer : IpcBase, IDisposable {

        #region Events

        /// <summary>
        /// Occurs when the server is started.
        /// </summary>
        public event EventHandler ServerStarted;

        /// <summary>
        /// Occurs when the server is stopped.
        /// </summary>
        public event EventHandler ServerStopped;

        /// <summary>
        /// Occurs when a client connects to the server.
        /// </summary>
        public event EventHandler ClientConnected;

        /// <summary>
        /// Occurs when a client disconnects from the server.
        /// </summary>
        public event EventHandler ClientDisconnected;

        /// <summary>
        /// Occurs when the server receives a message from client. The server can reply to the message via setting Response property.
        /// </summary>
        public event EventHandler<BinaryMessageEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when an exception is thrown inside an active message loop.
        /// </summary>
        public event EventHandler<Exception> MessageLoopException;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the maximum number of clients that can connect to the server simultaneously.
        /// </summary>
        public int MaxClients { get; set; } = 16;

        /// <summary>
        /// Gets the number of clients connected to this server instance.
        /// </summary>
        public int ClientsConnected { get; private set; }

        #endregion

        #region Public c-tors and methods

        /// <summary>
        /// Creates new <see cref="IpcServer"/> instance without setting the pipe name.
        /// </summary>
        public IpcServer() { }

        /// <summary>
        /// Creates new <see cref="IpcServer"/> instance setting the pipe name.
        /// </summary>
        /// <param name="pipeName">Pipe name.</param>
        public IpcServer(string pipeName) => PipeName = pipeName;

        /// <summary>
        /// Starts the server. Does not block.
        /// </summary>
        public void Start() {
            if (String.IsNullOrEmpty(PipeName)) throw new InvalidOperationException(ExceptionMessages.PipeNameNotSet);
            if (IsStarted || IsDisposed || IsStarting || IsStopping) return;
            IsStarting = true;
            IsStarted = IsStopping = false;
            CTS = new CancellationTokenSource();
            CancellationToken = CTS.Token;
            StartListener();
        }

        /// <summary>
        /// Stops the server. Blocks untill all connections are properly disconnected and closed.
        /// </summary>
        public void Stop() {
            if (!IsStarted || IsDisposed || IsStarting || IsStopping) return;
            CTS.Cancel();
            while (DuplexStreams.FirstOrDefault() is DuplexNamedPipeServerStream duplexStream) {
                if (duplexStream.Input.IsConnected) duplexStream.Input.Disconnect();
                while (duplexStream.Input.IsConnected) Thread.Sleep(1);
                if (duplexStream.Output.IsConnected) duplexStream.Output.Disconnect();
                while (duplexStream.Output.IsConnected) Thread.Sleep(1);
                Thread.Sleep(1);
                DuplexStreams.Remove(duplexStream);
                duplexStream.Dispose();
            }
            if (IsStopping && ClientsConnected > 0 && ShutdownSemaphore.CurrentCount < 1) {
                ShutdownSemaphore.Wait(1000);
            }
            IsStarted = IsStopping = false;
            CTS.Dispose();
            CTS = null;
            ServerStopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="message">Binary message.</param>
        public void Broadcast(byte[] message) {
            if (ClientsConnected < 1 || !IsStarted || IsDisposed || IsStarting || IsStopping || CancellationToken.IsCancellationRequested) return;
            foreach (var duplexStream in DuplexStreams.Where(i => i.Output.IsConnected)) {
                if (CancellationToken.IsCancellationRequested) return;
                var length = message.Length;
                if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
                duplexStream.Output.Write(message, 0, length);
            }
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="message">Binary message.</param>
        /// <returns>Task.</returns>
        public async Task BroadcastAsync(byte[] message) {
            if (ClientsConnected < 1 || !IsStarted || IsDisposed || IsStarting || IsStopping || CancellationToken.IsCancellationRequested) return;
            foreach (var duplexStream in DuplexStreams.Where(i => i.Output.IsConnected)) {
                if (CancellationToken.IsCancellationRequested) return;
                var length = message.Length;
                if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
                await duplexStream.Output.WriteAsync(message, 0, length, CTS.Token);
            }
        }

        /// <summary>
        /// Sends a message to a specific client stream.
        /// </summary>
        /// <param name="clientStream">Client stream.</param>
        /// <param name="message">Binary message.</param>
        public void Send(NamedPipeServerStream clientStream, byte[] message) {
            if (ClientsConnected < 1 || !IsStarted || IsDisposed || IsStarting || IsStopping || CancellationToken.IsCancellationRequested) return;
            var length = message.Length;
            if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
            clientStream.Write(message, 0, length);
        }

        /// <summary>
        /// Sends a message to a specific client stream.
        /// </summary>
        /// <param name="clientStream">Client stream.</param>
        /// <param name="message">Binary message.</param>
        /// <returns>Task.</returns>
        public async Task SendAsync(NamedPipeServerStream clientStream, byte[] message) {
            if (ClientsConnected < 1 || !IsStarted || IsDisposed || IsStarting || IsStopping || CancellationToken.IsCancellationRequested) return;
            var length = message.Length;
            if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
            await clientStream.WriteAsync(message, 0, length, CTS.Token);
        }

        /// <summary>
        /// Disposes disposable resources.
        /// </summary>
        public void Dispose() {
            if (IsDisposed) return;
            Stop();
            IsDisposed = true;
            ShutdownSemaphore.Dispose();
        }

        #endregion

        #region Private methods (implementation)

        /// <summary>
        /// Initializes the static readonly properties.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Disposable WindowsIndentity.")]
        static IpcServer() {
            using (var currentIdentity = WindowsIdentity.GetCurrent()) CurrentProcessUser = currentIdentity.User;
            LocalUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            ServerAccessRules = new PipeAccessRule(CurrentProcessUser, PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow);
            ClientAccessRules = new PipeAccessRule(LocalUsers, PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow);
        }

        /// <summary>
        /// Starts a new "listener" asynchronously. Creates a new duplex stream from 2 simplex streams.
        /// </summary>
        private void StartListener() {
            var state = new ConnectionAsyncResult { DuplexStream = GetDuplexStream(), CancellationToken = CTS.Token };
            state.DuplexStream.Input.BeginWaitForConnection(IncommingConnectionCallback, state);
            state.DuplexStream.Output.BeginWaitForConnection(OutgoingConnectionCallback, state);
            DuplexStreams.Add(state.DuplexStream);
            if (!IsStarted) {
                IsStarted = true;
                IsStarting = false;
                ServerStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles incomming connections by receiving data continuously until the connection is disconnected.
        /// </summary>
        /// <param name="ar">The status of asynchronous operation.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is server.")]
        private void IncommingConnectionCallback(IAsyncResult ar) {
            ConnectionAsyncResult connectionAsyncResult;
            connectionAsyncResult = (ConnectionAsyncResult)ar.AsyncState;
            var dxStream = connectionAsyncResult.DuplexStream;
            var input = dxStream.Input;
            var output = dxStream.Output;
            var token = connectionAsyncResult.CancellationToken;
            if (token.IsCancellationRequested) return;
            dxStream.Input.EndWaitForConnection(ar);
            if (dxStream.Output.IsConnected) {
                ClientsConnected++;
                ClientConnected?.Invoke(this, EventArgs.Empty);
                if (ClientsConnected < MaxClients) StartListener();
            }
            try {
                var buffer = new byte[MessageBufferSize];
                while (input.IsConnected && !token.IsCancellationRequested) {
                    var length = connectionAsyncResult.DuplexStream.Input.Read(buffer, 0, buffer.Length);
                    if (length < 1) break; // disconnection
                    var messageEventArgs = new BinaryMessageEventArgs(buffer, length);
                    if (token.IsCancellationRequested) break;
                    MessageReceived?.Invoke(this, messageEventArgs);
                    if (!(messageEventArgs.Response is null) && !token.IsCancellationRequested) {
                        output.Write(messageEventArgs.Response, 0, messageEventArgs.Response.Length);
                    }
                }
            }
            catch (Exception messageLoopException) {
                MessageLoopException?.Invoke(this, messageLoopException);
            }
            finally {
                ClientsConnected--;
            }
            if (token.IsCancellationRequested && !IsStopping) IsStopping = true;
            lock (ConnectionLock) {
                if (!IsStopping) {
                    DuplexStreams.Remove(dxStream);
                    dxStream.Dispose();
                }
                ClientDisconnected?.Invoke(this, EventArgs.Empty);
                    if (ClientsConnected < 1 && IsStopping && ShutdownSemaphore.CurrentCount < 1) {
                        IsStopping = false;
                        ShutdownSemaphore.Release();
                    }
            }
        }
        /// <summary>
        /// Handles outgoing connections.
        /// </summary>
        /// <param name="ar">The status of asynchronous operation.</param>
        private void OutgoingConnectionCallback(IAsyncResult ar) {
            ConnectionAsyncResult connectionAsyncResult;
            connectionAsyncResult = (ConnectionAsyncResult)ar.AsyncState;
            if (connectionAsyncResult.CancellationToken.IsCancellationRequested) return;
            connectionAsyncResult.DuplexStream.Output.EndWaitForConnection(ar);
            if (connectionAsyncResult.DuplexStream.Input.IsConnected) {
                if (ClientsConnected < MaxClients) {
                    ClientsConnected++;
                    StartListener();
                    ClientConnected?.Invoke(connectionAsyncResult.DuplexStream, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the configured <see cref="NamedPipeServerStream"/> for specified <see cref="PipeDirection"/>.
        /// </summary>
        /// <param name="direction">Direction of the simplex pipe.</param>
        /// <returns>Stream.</returns>
        private NamedPipeServerStream GetSimplexStream(PipeDirection direction) =>
            new NamedPipeServerStream(
                GetSimplexPipeName(direction),
                direction,
                MaxClients + 1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                GetInputBufferSize(direction),
                GetOutputBufferSize(direction),
                GetPipeSecurity(),
                System.IO.HandleInheritability.Inheritable
            );

        /// <summary>
        /// Gets the new, configured <see cref="DuplexNamedPipeServerStream"/>.
        /// </summary>
        /// <returns>Stream.</returns>
        private DuplexNamedPipeServerStream GetDuplexStream() => new DuplexNamedPipeServerStream(
            GetSimplexStream(PipeDirection.In),
            GetSimplexStream(PipeDirection.Out)
        );

        /// <summary>
        /// Gets the simplex pipe name depending on <see cref="IpcBase.PipeName"/> and <see cref="PipeDirection"/>.
        /// </summary>
        /// <param name="pipeDirection">Direction of the simplex pipe.</param>
        /// <returns>Simplex pipe name.</returns>
        private string GetSimplexPipeName(PipeDirection pipeDirection) {
            switch (pipeDirection) {
                case PipeDirection.In: return $"{PipeName}-IN";
                case PipeDirection.Out: return $"{PipeName}-OUT";
                default: throw new ArgumentException(ExceptionMessages.InvalidDirectionValue, nameof(pipeDirection));
            }
        }

        /// <summary>
        /// Gets the message input buffer size for the specified pipe direction. For output pipe the buffer size should be zero.
        /// </summary>
        /// <param name="pipeDirection">Direction of the simplex pipe.</param>
        /// <returns><see cref="IpcBase.MessageBufferSize"/> or 0.</returns>
        private int GetInputBufferSize(PipeDirection pipeDirection) {
            switch (pipeDirection) {
                case PipeDirection.In: return MessageBufferSize;
                case PipeDirection.Out: return 0;
                default: throw new ArgumentException(ExceptionMessages.InvalidDirectionValue, nameof(pipeDirection));
            }
        }

        /// <summary>
        /// Gets the message output buffer size for the specified pipe direction. For input pipe the buffer size should be zero.
        /// </summary>
        /// <param name="pipeDirection">Direction of the simplex pipe.</param>
        /// <returns><see cref="IpcBase.MessageBufferSize"/> or 0.</returns>
        private int GetOutputBufferSize(PipeDirection pipeDirection) {
            switch (pipeDirection) {
                case PipeDirection.In: return 0;
                case PipeDirection.Out: return MessageBufferSize;
                default: throw new ArgumentException(ExceptionMessages.InvalidDirectionValue, nameof(pipeDirection));
            }
        }

        /// <summary>
        /// Gets the security settings with minimal access rules allowing clients started from different accounts on the same computer to communicate with each other.
        /// </summary>
        /// <returns><see cref="PipeSecurity"/>.</returns>
        private static PipeSecurity GetPipeSecurity() {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(ServerAccessRules);
            pipeSecurity.AddAccessRule(ClientAccessRules);
            return pipeSecurity;
        }

        #endregion

        #region Private data

        private CancellationTokenSource CTS;
        private CancellationToken CancellationToken;
        private readonly object ConnectionLock = new object();
        private readonly SemaphoreSlim ShutdownSemaphore = new SemaphoreSlim(0, 1);
        private readonly SynchronizedCollection<DuplexNamedPipeServerStream> DuplexStreams = new SynchronizedCollection<DuplexNamedPipeServerStream>();
        private readonly static SecurityIdentifier CurrentProcessUser;
        private readonly static SecurityIdentifier LocalUsers;
        private readonly static PipeAccessRule ServerAccessRules;
        private readonly static PipeAccessRule ClientAccessRules;
        private struct ConnectionAsyncResult {
            public DuplexNamedPipeServerStream DuplexStream;
            public CancellationToken CancellationToken;
        }

        #endregion

    }

}