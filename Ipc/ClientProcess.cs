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
            Client = new ProcessEx(path, arguments);
            IpcChannel.Start();
        }

        /// <summary>
        /// Replaces specifed placeholder in executable arguments set with specified string.
        /// </summary>
        /// <param name="placeholder">Placeholder string.</param>
        /// <param name="value">String value to set.</param>
        public void SetArg(string placeholder, string value) => Client.SetArg(placeholder, value);

        /// <summary>
        /// Replaces multiple placeholders in executable arguments set with dictionary values.
        /// </summary>
        /// <param name="map">A dictionary of placeholder => value pairs.</param>
        public void SetArgs(Dictionary<string, string> map) => Client.SetArgs(map);

        /// <summary>
        /// Starts client process.
        /// </summary>
        /// <returns>True if a process resource is started; false if no new process resource is started (for example, if an existing process is reused).</returns>
        public bool Start() {
            if (ActualProcess != null) {
                if (ActualProcess.HasExited) IpcChannel.Reinitialize();
                else return false;
            }
            Client.StartAsCurrentUser = StartAsCurrentUser;
            if (ActualProcess == null && !StartAsCurrentUser) {
                Client.EnableRaisingEvents = true;
                Client.Exited += PassClientExited;
            }
            var startResult = Client.Start();
            if (ActualProcess != null && StartAsCurrentUser) ActualProcess.Exited -= PassClientExited;
            ActualProcess = Client.ActualProcess;
            if (startResult && StartAsCurrentUser) {
                ActualProcess.EnableRaisingEvents = true;
                ActualProcess.Exited += PassClientExited;
            }
            if (startResult && ActualProcess != null && !ActualProcess.HasExited)
                OnClientStarted(EventArgs.Empty);
            return startResult;
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

        readonly ProcessEx Client;
        readonly CombinedChannel IpcChannel;

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
                if (Client != null) Client.Dispose();
            }
        }

        #endregion

    }

}
