using System;
using System.IO.Pipes;
using System.Timers;

namespace Woof.Ipc {

    /// <summary>
    /// IPC channel combined from named and anonymous pipes used for encrypted communication.
    /// </summary>
    public sealed class CombinedChannel : IDisposable {

        /// <summary>
        /// Gets or sets the time (in milliseconds) given to establish / complete communication before <see cref="TimeoutException"/> is triggered.
        /// </summary>
        public int Timeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the option of using encrypted communication. Default true.
        /// </summary>
        public bool UseEncryption {
            get => MainChannel.UseEncryption; set => MainChannel.UseEncryption = value;
        }

        /// <summary>
        /// Gets or sets the option of using compressed communication. Default true.
        /// </summary>
        public bool UseCompression {
            get => MainChannel.UseCompression; set => MainChannel.UseCompression = value;
        }

        /// <summary>
        /// Gets initial (anonymous) pipe identifier.
        /// </summary>
        public string InitalPipeId { get; private set; }

        /// <summary>
        /// Occurs when main channel has received data.
        /// </summary>
        public event EventHandler<Channel.DataEventArgs> DataReceived;

        /// <summary>
        /// Occurs when the client has disconnected from main channel.
        /// </summary>
        public event EventHandler ClientDisconnected;

        /// <summary>
        /// Creates new combined channel.
        /// </summary>
        /// <param name="mode"><see cref="Channel.Modes.Client"/> or <see cref="Channel.Modes.Server"/>.</param>
        /// <param name="name">Name for the named pipe.</param>
        /// <param name="id">Id of the anonymous pipe.</param>
        public CombinedChannel(Channel.Modes mode, string name, string id = null) {
            switch (mode) {
                case Channel.Modes.Client:
                    using (var t = new Timer(Timeout)) {
                        t.Elapsed += new ElapsedEventHandler((s, e) => { throw new TimeoutException(); });
                        t.Start();
                        InitialChannel = new Channel(mode, PipeDirection.In, id);
                        MainChannel = new Channel(mode, PipeDirection.InOut, name, InitialChannel.ReadBytes());
                    }
                    break;
                case Channel.Modes.Server:
                    InitialChannel = new Channel(mode, PipeDirection.Out);
                    MainChannel = new Channel(mode, PipeDirection.InOut, name);
                    InitalPipeId = InitialChannel.PipeId;
                    InitialChannel.WriteBytes(MainChannel.KeyData);
                    MainChannel.DataReceived += PassDataReceivedFromMainChannel;
                    MainChannel.ClientDisconnected += PassClientDisconnectedFromMainChannel;
                    break;
                default:
                    throw new ArgumentException("Mode not supported by CombinedChannel.");
            }
            UseEncryption = true;
            UseCompression = true;
        }

        /// <summary>
        /// Starts the communication with the main channel.
        /// </summary>
        public void Start() => MainChannel.Start();

        /// <summary>
        /// Writes key data again to the initial channel.
        /// </summary>
        public void Reinitialize() => InitialChannel.WriteBytes(MainChannel.KeyData);

        /// <summary>
        /// Reads data from main channel as object.
        /// </summary>
        /// <returns>Deserialized object.</returns>
        public object Read() {
            if (!MainChannel.Ready) MainChannel.Start();
            return MainChannel.Read();
        }

        /// <summary>
        /// Reads data from main channel as T.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <returns>Deserialized T.</returns>
        public T Read<T>() {
            if (!MainChannel.Ready) MainChannel.Start();
            return MainChannel.Read<T>();
        }

        /// <summary>
        /// Reads raw data from main channel.
        /// </summary>
        /// <returns>Raw data.</returns>
        public byte[] ReadBytes() {
            if (!MainChannel.Ready) MainChannel.Start();
            return MainChannel.ReadBytes();
        }

        /// <summary>
        /// Reads UTF-8 text from main channel.
        /// </summary>
        /// <returns>Unicode string.</returns>
        public string ReadUTF8() {
            if (!MainChannel.Ready) MainChannel.Start();
            return MainChannel.ReadUTF8();
        }

        /// <summary>
        /// Writes an object to main channel.
        /// </summary>
        /// <param name="data">Serializable object.</param>
        public void Write(object data) {
            if (MainChannel.Mode != Channel.Modes.Server && !MainChannel.Ready) MainChannel.Start();
            MainChannel.Write(data);
        }

        /// <summary>
        /// Writes a serializable object of type T to main channel.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Data to serialize.</param>
        public void Write<T>(T data) {
            if (MainChannel.Mode != Channel.Modes.Server && !MainChannel.Ready) MainChannel.Start();
            MainChannel.Write<T>(data);
        }

        /// <summary>
        /// Writes raw data to main channel.
        /// </summary>
        /// <param name="data">Raw data.</param>
        public void WriteBytes(byte[] data) {
            if (MainChannel.Mode != Channel.Modes.Server && !MainChannel.Ready) MainChannel.Start();
            MainChannel.WriteBytes(data);
        }

        /// <summary>
        /// Writes text as UTF8 to main channel.
        /// </summary>
        /// <param name="data">Unicode string.</param>
        public void WriteUTF8(string data) {
            if (MainChannel.Mode != Channel.Modes.Server && !MainChannel.Ready) MainChannel.Start();
            MainChannel.WriteUTF8(data);
        }

        /// <summary>
        /// Sends a request by initiating data exchange with the remote process.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="timeout">Time in milliseconds after <see cref="TimeoutException"/> will be thrown when remote process doesn't reply.</param>
        /// <returns>Response data.</returns>
        public object Request(object data, int timeout = 0) {
            if (timeout == 0) timeout = Timeout;
            using (var t = new Timer(Timeout)) {
                t.Elapsed += new ElapsedEventHandler((s, e) => { throw new TimeoutException(); });
                t.Start();
                Write(data);
                return Read();
            }
        }

        /// <summary>
        /// Sends a request by initiaing data exchange with the remote process.
        /// </summary>
        /// <typeparam name="TRequest">Request data type.</typeparam>
        /// <typeparam name="TResponse">Response data type.</typeparam>
        /// <param name="data">Request data.</param>
        /// <param name="timeout">Time in milliseconds after <see cref="TimeoutException"/> will be thrown when remote process doesn't reply.</param>
        /// <returns>Response data.</returns>
        public TResponse Request<TRequest, TResponse>(TRequest data, int timeout = 0) => (TResponse)Request(data, timeout);

        /// <summary>
        /// Sends a data object to remote process without receiving a response.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="timeout">Time in milliseconds after <see cref="TimeoutException"/> will be thrown when remote process doesn't receive.</param>
        public void Notify(object data, int timeout = 0) {
            if (timeout == 0) timeout = Timeout;
            using (var t = new Timer(Timeout)) {
                t.Elapsed += new ElapsedEventHandler((s, e) => { throw new TimeoutException(); });
                t.Start();
                Write(data);
            }
        }

        #region Private

        private readonly Channel InitialChannel;
        private readonly Channel MainChannel;

        void PassDataReceivedFromMainChannel(object sender, Channel.DataEventArgs e) => OnDataReceived(e);

        void PassClientDisconnectedFromMainChannel(object sender, EventArgs e) => OnClientDisconnected(e);

        void OnDataReceived(Channel.DataEventArgs e) => DataReceived?.Invoke(this, e);

        void OnClientDisconnected(EventArgs e) => ClientDisconnected?.Invoke(this, e);

        /// <summary>
        /// Disposes the initial and main channels if applicable.
        /// </summary>
        public void Dispose() {
            InitialChannel?.Dispose();
            MainChannel?.Dispose();
        }

        #endregion

    }

}
