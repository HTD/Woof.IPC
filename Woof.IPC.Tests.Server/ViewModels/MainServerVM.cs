using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using Woof.Ipc;
using Woof.WindowsEx;

namespace Woof.IPC.Tests.Server.ViewModels {

    sealed class MainServerVM : ViewModelBase, IDisposable {

        public string ReceivedText { get; set; } = "";

        public string TextToSend { get; set; } = "";

        public bool IsStartEnabled { get; set; } = true;

        public bool IsServerStarted { get; set; }

        public bool IsSendEnabled { get; set; }


        public MainServerVM() {
            if (DesignerProperties.GetIsInDesignMode(Application.Current.MainWindow)) return;
            Server = new IpcServer("Woof_TestIPC");
            Application.Current.RegisterDisposable(Server);
            Server.ServerStarted += Server_ServerStarted;
            Server.ServerStopped += Server_ServerStopped;
            Server.MessageLoopException += Server_MessageLoopException;
            Server.ClientConnected += Server_ClientConnected;
            Server.MessageReceived += Server_MessageReceived;
            Server.ClientDisconnected += Server_ClientDisconnected;
            Execute("Start");
        }

        public override async void Execute(object parameter) {
            if (parameter is string command) {
                switch (command) {
                    case "Start":
                        Server.Start();
                        IsServerStarted = true;
                        IsStartEnabled = false;
                        OnPropertyChanged(nameof(IsServerStarted));
                        OnPropertyChanged(nameof(IsStartEnabled));
                        break;
                    case "Stop":
                        Server.Stop();
                        IsServerStarted = false;
                        IsStartEnabled = true;
                        OnPropertyChanged(nameof(IsServerStarted));
                        OnPropertyChanged(nameof(IsStartEnabled));
                        break;
                    case "Send":
                        if (string.IsNullOrEmpty(TextToSend)) return;
                        await Server.BroadcastAsync(Encoding.UTF8.GetBytes(TextToSend));
                        TextToSend = null;
                        OnPropertyChanged(nameof(TextToSend));
                        break;
                }
            }
        }

        private void Server_ServerStarted(object sender, EventArgs e) {
            ReceivedText += "# SERVER STARTED." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private void Server_ServerStopped(object sender, EventArgs e) {
            ReceivedText += "# SERVER STOPPED." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private void Server_MessageLoopException(object sender, Exception e) {
            ReceivedText += $"# MESSAGE LOOP EXCEPTION: {e.Message}." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }


        private void Server_ClientConnected(object sender, EventArgs e) {
            ReceivedText += "# Client connected." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
            IsSendEnabled = Server.ClientsConnected > 0;
            OnPropertyChanged(nameof(IsSendEnabled));
        }

        private void Server_ClientDisconnected(object sender, EventArgs e) {
            ReceivedText += "# Client disconnected." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
            IsSendEnabled = (sender as IpcServer).ClientsConnected > 0;
            OnPropertyChanged(nameof(IsSendEnabled));
        }

        

        private void Server_MessageReceived(object sender, BinaryMessageEventArgs e) {
            var message = Encoding.UTF8.GetString(e.Message);
            ReceivedText += message  + Environment.NewLine;
            e.Response = Encoding.UTF8.GetBytes($"ACK: \"{message}\"");
            OnPropertyChanged(nameof(ReceivedText));
        }

        public void Dispose() => Server.Dispose();

        private readonly IpcServer Server;

    }

}
