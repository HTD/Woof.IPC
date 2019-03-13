using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woof.Ipc;

namespace Woof.IPC.Tests {
    [TestClass]
    public class ProcessExTests {

        [TestMethod]
        public void ProcessExStartAsCurrentUser() {
            var path = @"C:\Windows\System32\notepad.exe";
            var dir = System.IO.Path.GetDirectoryName(path);
            using (var process = new ProcessEx(path)) {
                process.StartInfo.WorkingDirectory = dir;
                process.StartAsCurrentUser = true;
                process.Start();
            }
        }
    }
}
