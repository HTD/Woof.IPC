using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Woof.Ipc {

    /// <summary>
    /// Main IPC process class.
    /// </summary>
    public sealed class ClientProcess : IDisposable {

        #region Public

        /// <summary>
        /// Gets or sets an options of starting process as current user. Set true to interact with user sessions from System process.
        /// </summary>
        public bool StartAsCurrentUser { get; set; }

        /// <summary>
        /// Returns underlying client process as <see cref="Process"/>.
        /// </summary>
        public Process ActualProcess { get; private set; }

        /// <summary>
        /// Gets the command line arguments collection created with the process.
        /// </summary>
        public ProcessArguments Arguments { get; }

        /// <summary>
        /// Gets the sets of values used to start the process.
        /// </summary>
        public ProcessStartInfo StartInfo { get; }

        //public ProcessArguments ArgumentsTemplate { get; set; }

        /// <summary>
        /// True if the process has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Occures when client process received data from server.
        /// </summary>
        public event EventHandler<Channel.DataEventArgs> DataReceived;

        /// <summary>
        /// Occurs when client process has been disconnected from server.
        /// </summary>
        public event EventHandler ClientDisconnected;

        /// <summary>
        /// Occurs when client process has started.
        /// </summary>
        public event EventHandler ClientStarted;

        /// <summary>
        /// Occurs when client process has exited.
        /// </summary>
        public event EventHandler ClientExited;

        

        /// <summary>
        /// Creates new IPC client process.
        /// </summary>
        /// <param name="path">Executable path.</param>
        /// <param name="pipeName">Name of the pipe used for communication.</param>
        /// <param name="arguments">String argumets passed to executable.</param>
        public ClientProcess(string path, string pipeName, params string[] arguments) {
            IpcChannel = new CombinedChannel(Channel.Modes.Server, pipeName);
            IpcChannel.DataReceived += PassDataReceived;
            IpcChannel.ClientDisconnected += PassClientDisconnected;
            var initialPipeId = IpcChannel.InitalPipeId;
            if (arguments != null && arguments.Length > 0) {
                for (int i = 0; i < arguments.Length; i++) if (arguments[i] == "PIPE_ID") arguments[i] = initialPipeId;
            }
            else arguments = new[] { initialPipeId };
            Arguments = new ProcessArguments(ArgumentsTemplate = arguments);
            StartInfo = new ProcessStartInfo(path, Arguments.ToString());
            IpcChannel.Start();
        }

        /// <summary>
        /// Replaces specifed placeholder in executable arguments set with specified string.
        /// </summary>
        /// <param name="placeholder">Placeholder string.</param>
        /// <param name="value">String value to set.</param>
        public void SetArg(string placeholder, string value) {
            var l = ArgumentsTemplate.Length;            
            string arg;
            for (int i = 0; i < l; i++) {
                arg = ArgumentsTemplate[i];
                Arguments[i] = arg == placeholder ? value : arg;
            }
            StartInfo.Arguments = Arguments.ToString();
        }

        /// <summary>
        /// Replaces multiple placeholders in executable arguments set with dictionary values.
        /// </summary>
        /// <param name="map">A dictionary of placeholder => value pairs.</param>
        public void SetArgs(Dictionary<string, string> map) {
            var l = ArgumentsTemplate.Length;
            string arg;
            for (int i = 0; i < l; i++) {
                arg = ArgumentsTemplate[i];
                Arguments[i] = map.ContainsKey(arg) ? map[arg] : arg;
            }
            StartInfo.Arguments = Arguments.ToString();
        }

        /// <summary>
        /// Starts client process.
        /// </summary>
        public void Start() {
            if (ActualProcess != null) {
                if (ActualProcess.HasExited) IpcChannel.Reinitialize();
                else return;
            }
            ActualProcess = StartAsCurrentUser ? UserProcess.Start(StartInfo) : Process.Start(StartInfo);
            ActualProcess.Exited += PassClientExited;
            ActualProcess.EnableRaisingEvents = true;
            if (!ActualProcess.HasExited) OnClientStarted(EventArgs.Empty);
        }

        /// <summary>
        /// Sends boxed object to client process.
        /// </summary>
        /// <param name="data">Boxed object.</param>
        public void SendData(object data) => IpcChannel.Write(data);

        /// <summary>
        /// Sends raw bytes to client process.
        /// </summary>
        /// <param name="data">Raw bytes.</param>
        public void SendBytes(byte[] data) => IpcChannel.WriteBytes(data);

        /// <summary>
        /// Sends UTF-8 encoded string to client process.
        /// </summary>
        /// <param name="data">Input string.</param>
        public void SendUTF8(string data) => IpcChannel.WriteUTF8(data);

        #endregion

        #region Private

        readonly CombinedChannel IpcChannel;

        /// <summary>
        /// Contains original process arguments as a template for <see cref="SetArg(string, string)"/> and <see cref="SetArgs(Dictionary{string, string})"/>.
        /// </summary>
        readonly string[] ArgumentsTemplate;

        /// <summary>
        /// Passes <see cref="CombinedChannel.DataReceived"/> event from combined channel to client process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PassDataReceived(object sender, Channel.DataEventArgs e) => OnDataReceived(e);

        /// <summary>
        /// Passes <see cref="CombinedChannel.ClientDisconnected"/> event from combined channel to client process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PassClientDisconnected(object sender, EventArgs e) => OnClientDisconnected(e);

        /// <summary>
        /// Passes <see cref="ClientExited"/> event from underlying <see cref="ProcessEx"/> to client process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PassClientExited(object sender, EventArgs e) => OnClientExited(e);

        /// <summary>
        /// Triggers <see cref="DataReceived"/> event.
        /// </summary>
        /// <param name="e"></param>
        void OnDataReceived(Channel.DataEventArgs e) => DataReceived?.Invoke(this, e);

        /// <summary>
        /// Triggers <see cref="ClientDisconnected"/> event.
        /// </summary>
        /// <param name="e"></param>
        void OnClientDisconnected(EventArgs e) => ClientDisconnected?.Invoke(this, e);

        /// <summary>
        /// Triggers <see cref="ClientExited"/> event.
        /// </summary>
        /// <param name="e"></param>
        void OnClientExited(EventArgs e) => ClientExited?.Invoke(this, e);

        /// <summary>
        /// Triggers <see cref="ClientStarted"/> event.
        /// </summary>
        /// <param name="e"></param>
        void OnClientStarted(EventArgs e) => ClientStarted?.Invoke(this, e);

        /// <summary>
        /// Disposes combined communication channel and underlying <see cref="Process"/> or <see cref="ProcessEx"/> object.
        /// </summary>
        public void Dispose() {
            if (!IsDisposed) {
                if (ActualProcess != null && !ActualProcess.HasExited) ActualProcess.Kill(); // when process is disposed, but not exited, it's killed here.
                IsDisposed = true;
                if (IpcChannel != null) IpcChannel.Dispose();
                if (ActualProcess != null) ActualProcess.Dispose();
            }
        }

        #endregion

    }

}
