using System.Windows;
using System.Windows.Controls;

namespace Woof.IPC.Tests.Client {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() => InitializeComponent();

        private void Received_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            var textBox = sender as TextBox;
            textBox.ScrollToEnd();
        }
    }
}
