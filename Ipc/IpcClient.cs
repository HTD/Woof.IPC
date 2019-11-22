using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.Ipc {

    /// <summary>
    /// IPC named pipe client.
    /// </summary>
    public sealed class IpcClient : IpcBase, IDisposable {

        #region Events

        /// <summary>
        /// Occurs when the client is started.
        /// </summary>
        public event EventHandler ClientStarted;
        
        /// <summary>
        /// Occurs when the client is stopped.
        /// </summary>
        public event EventHandler ClientStopped;
        
        /// <summary>
        /// Occurs when the server is connected.
        /// </summary>
        public event EventHandler ServerConnected;
        
        /// <summary>
        /// Occurs when the server is disconnected.
        /// </summary>
        public event EventHandler ServerDisconnected;
        
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<BinaryMessageEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when an exception is thrown inside an active message loop.
        /// </summary>
        public event EventHandler<Exception> MessageLoopException;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the time (in milliseconds) to wait for the connection with the server before timing out.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 500;

        /// <summary>
        /// Gets or sets the time (in milliseconds) between the client polls for server available.
        /// </summary>
        public int ReconnectPollingInterval { get; set; } = 500;

        /// <summary>
        /// Gets a value indicating whether the server is connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        #endregion

        #region Public c-tors and methods

        /// <summary>
        /// Creates new <see cref="IpcClient"/> instance without setting the pipe name.
        /// </summary>
        public IpcClient() { }

        /// <summary>
        /// Creates new <see cref="IpcClient"/> instance setting the pipe name.
        /// </summary>
        /// <param name="pipeName">Pipe name.</param>
        public IpcClient(string pipeName) => PipeName = pipeName;

        /// <summary>
        /// Starts the client. Does not block.
        /// </summary>
        public void Start() {
            if (IsStarted || IsDisposed || IsStarting || IsStopping) return;
            IsStarting = true;
            CTS = new CancellationTokenSource();
            CancellationToken = CTS.Token;
            if (ReconnectPollingInterval > 0) {
                ClientStarted?.Invoke(this, EventArgs.Empty);
                new Task(ConnectLoop, CancellationToken, TaskCreationOptions.LongRunning).Start();
            }
            else {
                Connect();
                IsStarted = IsConnected;
                IsStarting = false;
                if (IsStarted) ClientStarted.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Stops the client. Blocks untill server connection is properly disconnected and closed.
        /// </summary>
        public void Stop() {
            if (IsConnected) {
                CTS.Cancel();
                //Thread.Sleep(1);
                DuplexStream.Dispose();
                //Thread.Sleep(1);
                if (IsStopping) ShutdownSemaphore.Wait(2500);
                IsStopping = IsStarting = IsStarted = false;
                CTS.Dispose();
                CTS = null;
                ClientStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">Binary data.</param>
        public void Send(byte[] message) {
            if (CancellationToken.IsCancellationRequested) return;
            var length = message.Length;
            if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
            DuplexStream.Output.Write(message, 0, length);
        }

        /// <summary>
        /// Sends a message to the server asynchronously.
        /// </summary>
        /// <param name="message">Binary data.</param>
        /// <returns>Task.</returns>
        public async Task SendAsync(byte[] message) {
            if (CancellationToken.IsCancellationRequested) return;
            var length = message.Length;
            if (length > MessageBufferSize) throw new ArgumentOutOfRangeException(nameof(message), ExceptionMessages.InsufficientBufferSize);
            await DuplexStream.Output.WriteAsync(message, 0, length, CancellationToken);
        }

        /// <summary>
        /// Disposes disposable resources.
        /// </summary>
        public void Dispose() {
            if (IsDisposed) return;
            Stop();
            ShutdownSemaphore.Dispose();
            IsDisposed = true;
        }

        #endregion

        #region Private methods (implementation)

        /// <summary>
        /// Tries to connect to the server. Does not block. When connected starts the message loop asynchronously.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TimeoutException is not general.")]
        private void Connect() {
            if (IsConnected) return;
            DuplexStream = GetDuplexStream();
            try {
                DuplexStream.Input.Connect(ConnectionTimeout);
                DuplexStream.Output.Connect(ConnectionTimeout);
            }
            catch (TimeoutException) { return; }
            if (!DuplexStream.Input.IsConnected || !DuplexStream.Output.IsConnected) return;
            var messageLoopTask = new Task(MessageLoop, CancellationToken, TaskCreationOptions.LongRunning);
            messageLoopTask.Start();
            IsConnected = true;
            ServerConnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Constantly tries to connect the server if currently not connected.
        /// </summary>
        private void ConnectLoop() {
            while (!CancellationToken.IsCancellationRequested) {
                if (!IsConnected) Connect();
                Thread.Sleep(ReconnectPollingInterval);
            }
        }

        /// <summary>
        /// Receives data continuously until the connection is disconnected.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "No need to throw in code here.")]
        private void MessageLoop() {
            IsStarting = false;
            IsStarted = true;
            try {
                var buffer = new byte[MessageBufferSize];
                while (DuplexStream.Input.IsConnected && !CancellationToken.IsCancellationRequested) {
                    var length = DuplexStream.Input.Read(buffer, 0, buffer.Length);
                    if (length < 1) break; // disconnection
                    var messageEventArgs = new BinaryMessageEventArgs(buffer, length);
                    MessageReceived?.Invoke(DuplexStream.Output, messageEventArgs);
                    if (!(messageEventArgs.Response is null)) {
                        DuplexStream.Output.Write(messageEventArgs.Response, 0, messageEventArgs.Response.Length);
                    }
                }
            }
            catch (Exception messageLoopException) {
                MessageLoopException?.Invoke(this, messageLoopException);
            }
            IsConnected = false;
            if (CancellationToken.IsCancellationRequested && !IsStopping) IsStopping = true;
            if (!IsStopping) {
                DuplexStream?.Dispose();
                DuplexStream = null;
            }
            ServerDisconnected?.Invoke(this, EventArgs.Empty);
            if (IsStopping && ShutdownSemaphore.CurrentCount < 1) {
                IsStopping = false;
                ShutdownSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets configured <see cref="NamedPipeClientStream"/> for specifeid <see cref="PipeDirection"/>.
        /// </summary>
        /// <param name="direction">Direction of the simplex pipe.</param>
        /// <returns>Stream.</returns>
        private NamedPipeClientStream GetSimplexStream(PipeDirection direction) =>
            new NamedPipeClientStream(
                ".",
                GetSimplexPipeName(direction),
                direction,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                TokenImpersonationLevel.Identification,
                System.IO.HandleInheritability.Inheritable

            );

        /// <summary>
        /// Gets the new, configured <see cref="DuplexNamedPipeClientStream"/>.
        /// </summary>
        /// <returns>Stream.</returns>
        private DuplexNamedPipeClientStream GetDuplexStream() => new DuplexNamedPipeClientStream(
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
                case PipeDirection.In: return $"{PipeName}-OUT"; // server out is client in
                case PipeDirection.Out: return $"{PipeName}-IN"; // server in is client out
                default: throw new ArgumentException(ExceptionMessages.InvalidDirectionValue, nameof(pipeDirection));
            }
        }
        #endregion

        #region Private data

        private DuplexNamedPipeClientStream DuplexStream;
        private CancellationTokenSource CTS;
        private readonly SemaphoreSlim ShutdownSemaphore = new SemaphoreSlim(0, 1);
        private CancellationToken CancellationToken;

        #endregion


    }
}
