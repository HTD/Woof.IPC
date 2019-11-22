using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace Woof.Ipc {

    /// <summary>
    /// IPC channel based on named OR anonymous pipe.
    /// </summary>
    public sealed class Channel : IDisposable {

        #region Public

        #region Events

        /// <summary>
        /// Occurs when data from server is received.
        /// </summary>
        public event EventHandler<DataEventArgs> DataReceived;
        /// <summary>
        /// Occurs when client has disconnected from server.
        /// </summary>
        public event EventHandler ClientDisconnected;

        #endregion

        #region Properties

        /// <summary>
        /// Gets anonymous pipe ID.
        /// </summary>
        public string PipeId {
            get {
                if (Pipe is AnonymousPipeServerStream) {
                    var aps = Pipe as AnonymousPipeServerStream;
                    return aps.GetClientHandleAsString();
                }
                else return null;
            }
        }
        /// <summary>
        /// Gets or sets option of using encrypted communication.
        /// </summary>
        public bool UseEncryption { get; set; }

        /// <summary>
        /// Gets or sets option of using compressed communication.
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// Gets or sets message buffer size in bytes.
        /// </summary>
        public int MessageBufferSize { get; set; } = DefaultMessageBufferSize;

        /// <summary>
        /// Returns true if underlaying pipe is connected.
        /// </summary>
        public bool Ready {
            get {
                if (Pipe is AnonymousPipeClientStream apcs) return apcs.IsConnected;
                if (Pipe is AnonymousPipeServerStream apss) return apss.IsConnected;
                if (Pipe is NamedPipeClientStream npcs) return npcs.IsConnected;
                if (Pipe is NamedPipeServerStream npss) return npss.IsConnected;
                return true;
            }
        }

        /// <summary>
        /// Pipe communication mode: Client, Server or Stream.
        /// </summary>
        public Modes Mode { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates IPC channel.
        /// </summary>
        /// <param name="mode">Connection mode: Client, Server or Stream.</param>
        /// <param name="direction">Underlying pipe direction: In, Out or InOut.</param>
        /// <param name="id">Anonymous pipe id as number, or named pipe name.</param>
        /// <param name="keyData">Key data used for encryption, for default null new key will be generated.</param>
        public Channel(Modes mode, PipeDirection direction, string id = null, byte[] keyData = null) {
            Mode = mode;
            IsAnonymousPipe = id == null || Int32.TryParse(id, out _);
            switch (mode) {
                case Modes.Client:
                    Pipe = IsAnonymousPipe
                        ? (Stream)new AnonymousPipeClientStream(direction, id)
                        : (Stream)new NamedPipeClientStream(".", id, direction);
                    break;
                case Modes.Server: // IMPORTANT: PipeSecurity setting is necessary to connect processes of different users with named pipes!
                    Pipe = IsAnonymousPipe
                        ? (Stream)new AnonymousPipeServerStream(direction, HandleInheritability.Inheritable, MessageBufferSize)
                        : (Stream)new NamedPipeServerStream(id, direction, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, MessageBufferSize, MessageBufferSize, IpcSecurity);
                    break; // WARNING: FAILING TO SET PipeSecurity and trying to send data to a process owned by different user will cause this to FAIL SILENTLY!
                case Modes.Stream:
                    throw new ArgumentException("Invalid arguments for stream mode");
            }
            if (keyData != null) Encryption = new Encryption(keyData);
            Serialization = new Serialization();
        }

        /// <summary>
        /// Creates IPC channel in stream mode.
        /// </summary>
        /// <param name="stream">Communication stream.</param>
        /// <param name="keyData">Optional encryption key data.</param>
        public Channel(Stream stream, byte[] keyData = null) {
            Mode = Modes.Stream;
            Pipe = stream;
            if (keyData != null) Encryption = new Encryption(keyData);
            Serialization = new Serialization();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets key data bytes.
        /// If encryption is not configured yet, new key data is generated.
        /// </summary>
        public byte[] GetKeyData() {
            if (Encryption == null) Encryption = new Encryption();
            return Encryption.GetKeyData();
        }

        /// <summary>
        /// Starts communication.
        /// </summary>
        /// <param name="timeout">The number of milliseconds to wait for the server to respond before the connection times out.</param>
        /// <exception cref="TimeoutException">Could not connect to the server within the specified timeout period.</exception>
        /// <exception cref="InvalidOperationException">The client is already connected.</exception>
        /// <exception cref="IOException">The server is connected to another client and the time-out period has expired.</exception>
        public void Start(int timeout = 0) {
            switch (Mode) {
                case Modes.Client:
                    if (!IsAnonymousPipe) {
                        var npcs = Pipe as NamedPipeClientStream;
                        if (timeout > 0) npcs.Connect(timeout); else npcs.Connect();
                    }
                    break;
                case Modes.Server:
                    if (!IsAnonymousPipe) {
                        var npss = Pipe as NamedPipeServerStream;
                        try {
                            npss.BeginWaitForConnection(AsyncConnectionEstablished, npss);
#pragma warning disable CA1031 // Do not catch general exception types
                        }
                        catch (IOException) {
                            npss.Disconnect();
                            npss.BeginWaitForConnection(AsyncConnectionEstablished, npss);
                        }
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                    break;
            }
        }
        /// <summary>
        /// Reads boxed object from IPC channel.
        /// </summary>
        /// <returns>Received object after deserialization and optional decryption and decompression.</returns>
        public object Read() => Serialization.Deserialize(ReadBytes());

        /// <summary>
        /// Reads typed object from IPC channel.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <returns>Received object after deserialization and optional decryption and decompression.</returns>
        public T Read<T>() => Serialization.Deserialize<T>(ReadBytes());

        /// <summary>
        /// Writes boxed object to IPC channel.
        /// </summary>
        /// <param name="data">Boxed object.</param>
        public void Write(object data) => WriteBytes(Serialization.Serialize(data));

        /// <summary>
        /// Writes typed object to IPC channel.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Data to serialize.</param>
        public void Write<T>(T data) => WriteBytes(Serialization.Serialize<T>(data));

        /// <summary>
        /// Reads raw bytes from IPC channel.
        /// </summary>
        /// <returns>Received bytes after optional decryption and decompression.</returns>
        public byte[] ReadBytes() {
            if (IsDisposed) return null;
            byte[] readBuffer = new byte[MessageBufferSize];
            using (var outputStream = new MemoryStream()) {
                int length = 0;
                do {
                    length = Pipe.Read(readBuffer, 0, MessageBufferSize);
                    outputStream.Write(readBuffer, 0, length);
                } while (length == MessageBufferSize);
                return Receive(outputStream.ToArray());
            }
        }

        /// <summary>
        /// Reads UTF-8 encoded text from IPC channel.
        /// </summary>
        /// <returns>Dedoced string.</returns>
        public string ReadUTF8() => Encoding.UTF8.GetString(ReadBytes());

        /// <summary>
        /// Writes raw bytes to IPC channel.
        /// </summary>
        /// <param name="data">Raw data bytes.</param>
        public void WriteBytes(byte[] data) {
            if (IsDisposed) return;
            data = Dispatch(data);
            if (Pipe is NamedPipeServerStream npss && !npss.IsConnected) {
                if (WriteCache == null) WriteCache = new MemoryStream();
                WriteCache.Write(data, 0, data.Length);
                return;
            }
            Pipe.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Writes UTF-8 encoded text to IPC channel.
        /// </summary>
        /// <param name="data">Input string.</param>
        public void WriteUTF8(string data) => WriteBytes(Encoding.UTF8.GetBytes(data));

        #endregion
        /// <summary>
        /// Underlying pipe operation modes.
        /// </summary>
        public enum Modes {
            /// <summary>
            /// The pipe acts as client, listening server is required for starting communication.
            /// </summary>
            Client,
            /// <summary>
            /// The pipe acts as server, accepting connections from client.
            /// </summary>
            Server,
            /// <summary>
            /// The pipe operates in special, stream mode, not really suitable for normal IPC.
            /// </summary>
            Stream
        }

#pragma warning disable CA1034 // Nested types should not be visible
        /// <summary>
        /// Arguments for IPC data events.
        /// </summary>
        public class DataEventArgs : EventArgs {
#pragma warning restore CA1034 // Nested types should not be visible
            /// <summary>
            /// Boxed object passed to event handler.
            /// </summary>
            public object Request { get; set; }
            /// <summary>
            /// Boxed object passed as event handler result and returned to requesting process.
            /// </summary>
            public object Response { get; set; }
        }

        #endregion

        #region Private

        /// <summary>
        /// Default buffer size for messages: 64KB.
        /// </summary>
        const int DefaultMessageBufferSize = 0x10000;
        
        /// <summary>
        /// Generic pipe <see cref="Stream"/>.
        /// </summary>
        private readonly Stream Pipe;
        
        /// <summary>
        /// True if underlying pipe is <see cref="AnonymousPipeClientStream"/> or <see cref="AnonymousPipeServerStream"/>.
        /// </summary>
        private readonly bool IsAnonymousPipe;
        
        /// <summary>
        /// <see cref="MemoryStream"/> used as write cache.
        /// </summary>
        private MemoryStream WriteCache { get; set; }
        
        /// <summary>
        /// Data serialization module.
        /// </summary>
        private readonly Serialization Serialization;
        
        /// <summary>
        /// Data compression module.
        /// </summary>
        private Compression Compression;
        
        /// <summary>
        /// Data encryption module.
        /// </summary>
        private Encryption Encryption;

        /// <summary>
        /// <see cref="PipeSecurity"/> object for main <see cref="NamedPipeServerStream"/>.<br/>
        /// This is necessary to allow a named pipe to connect processes started by different users.
        /// </summary>
        private static PipeSecurity IpcSecurity {
            get {
                var pipeSecurity = new PipeSecurity();
                var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    sid,
                    PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize,
                    AccessControlType.Allow
                ));
                return pipeSecurity;
            }
        }

        /// <summary>
        /// Processes received data with encryption and compression modules.
        /// If enabled in properties - the data will be decrypted and then decompressed.
        /// </summary>
        /// <param name="data">Raw input data.</param>
        /// <returns>Processed data. Null for null or empty input.</returns>
        private byte[] Receive(byte[] data) {
            if (data == null || data.Length < 1) return null;
            if (UseEncryption) {
                if (Encryption == null) throw new InvalidOperationException("Key data not found.");
                data = Encryption.Decrypt(data);
            }
            if (UseCompression) {
                if (Compression == null) Compression = new Compression();
                data = Compression.Decompress(data);
            }
            return data;
        }

        /// <summary>
        /// Process data being dispatched with encryption and compression modules.
        /// If enabled in properties - the data will be compressed and then encrypted.
        /// </summary>
        /// <param name="data">Raw input data.</param>
        /// <returns>Processed data. Null for null or empty input.</returns>
        private byte[] Dispatch(byte[] data) {
            if (data == null || data.Length < 1) throw new InvalidOperationException("Zero bytes dispatch is not acceptable.");
            if (UseCompression) {
                if (Compression == null) Compression = new Compression();
                data = Compression.Compress(data);
            }
            if (UseEncryption) {
                if (Encryption == null) Encryption = new Encryption();
                data = Encryption.Encrypt(data);
            }
            return data;
        }

        /// <summary>
        /// The callback function for <see cref="NamedPipeServerStream.BeginWaitForConnection(AsyncCallback, object)"/>.
        /// </summary>
        /// <param name="a">Status of asynchronous operation.</param>
        private void AsyncConnectionEstablished(IAsyncResult a) {
            if (IsDisposed) return;
            var npss = a.AsyncState as NamedPipeServerStream;
            
            npss.EndWaitForConnection(a);
            while (npss != null && npss.IsConnected) {
                if (WriteCache != null) {
                    var cachedData = WriteCache.ToArray();
                    npss.Write(cachedData, 0, cachedData.Length);
                    WriteCache.Dispose();
                    WriteCache = null;
                }
                var receivedData = Read();
                if (receivedData == null) break;
                var dataEventArgs = new DataEventArgs { Request = receivedData };
                OnDataReceived(dataEventArgs);
                if (dataEventArgs.Response != null) Write(dataEventArgs.Response);
            }
            if (IsDisposed) return;
            if (npss.IsConnected) npss.Disconnect();
            OnClientDisconnected(EventArgs.Empty);
            Start();
        }

        /// <summary>
        /// Triggers <see cref="DataReceived"/> event.
        /// </summary>
        /// <param name="e"></param>
        private void OnDataReceived(DataEventArgs e) => DataReceived?.Invoke(this, e);

        /// <summary>
        /// Triggers <see cref="ClientDisconnected"/> event.
        /// </summary>
        /// <param name="e"></param>
        private void OnClientDisconnected(EventArgs e) => ClientDisconnected?.Invoke(this, e);

        #region IDisposable Impl.

        /// <summary>
        /// True if pipe streams has been disposed.
        /// </summary>
        private bool IsDisposed;

        /// <summary>
        /// Disposes pipe streams.
        /// </summary>
        public void Dispose() {
            if (!IsDisposed) {
                IsDisposed = true;
                if (Pipe is AnonymousPipeServerStream apss) apss.DisposeLocalCopyOfClientHandle();
                Pipe.Dispose();
                WriteCache?.Dispose();
            }
        }

        #endregion

        #endregion

    }

}