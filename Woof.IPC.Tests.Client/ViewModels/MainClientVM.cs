using System;
using System.ComponentModel;
using System.Text;
using System.Windows;

using Woof.Ipc;
using Woof.WindowsEx;

namespace Woof.IPC.Tests.Client.ViewModels {

    class MainClientVM : ViewModelBase {

        public bool IsConnectEnabled { get; set; } = true;

        public bool IsServerConnected { get; set; }

        public string ReceivedText { get; set; }

        public string TextToSend { get; set; }

        public MainClientVM() {
            if (DesignerProperties.GetIsInDesignMode(Application.Current.MainWindow)) return;
            Client = new IpcClient("Woof_TestIPC");
            Application.Current.RegisterDisposable(Client);
            Client.ClientStarted += Client_ClientStarted;
            Client.ClientStopped += Client_ClientStopped;
            Client.ServerConnected += Client_ServerConnected;
            Client.ServerDisconnected += Client_ServerDisconnected;
            Client.MessageReceived += Client_MessageReceived;
            Client.MessageLoopException += Client_MessageLoopException;
            Client.Start();
        }

        public override async void Execute(object parameter) {
            if (parameter is string command) {
                switch (command) {
                    case "Connect":
                        break;
                    case "Disconnect":
                        break;
                    case "Send":
                        if (TextToSend is null) return;
                        await Client.SendAsync(Encoding.UTF8.GetBytes(TextToSend));
                        TextToSend = String.Empty;
                        OnPropertyChanged(nameof(TextToSend));
                        break;
                }
            }
        }

        private void Client_ClientStarted(object sender, EventArgs e) {
            ReceivedText += "# CLIENT STARTED." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private void Client_ClientStopped(object sender, EventArgs e) {
            ReceivedText += "# CLIENT STOPPED." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private void Client_ServerConnected(object sender, EventArgs e) {
            ReceivedText += "# Server connected." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
            IsConnectEnabled = false;
            IsServerConnected = true;
            OnPropertyChanged(nameof(IsConnectEnabled));
            OnPropertyChanged(nameof(IsServerConnected));
        }

        private void Client_ServerDisconnected(object sender, EventArgs e) {
            ReceivedText += "# Server disconnected." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
            IsServerConnected = false;
            IsConnectEnabled = true;
            OnPropertyChanged(nameof(IsServerConnected));
            OnPropertyChanged(nameof(IsConnectEnabled));
        }

        private void Client_MessageReceived(object sender, BinaryMessageEventArgs e) {
            ReceivedText += Encoding.UTF8.GetString(e.Message) + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private void Client_MessageLoopException(object sender, Exception e) {
            ReceivedText += $"# MESSAGE LOOP EXCEPTION: {e.Message}." + Environment.NewLine;
            OnPropertyChanged(nameof(ReceivedText));
        }

        private readonly IpcClient Client;


    }



}
