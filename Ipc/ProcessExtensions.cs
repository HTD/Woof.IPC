using System;
using System.Diagnostics;

using Woof.Ipc.Win32Imports;

namespace Woof.Ipc {

    /// <summary>
    /// Extends the Process class providing methods to communicate with processes.
    /// </summary>
    public static class ProcessExtensions {

        /// <summary>
        /// Sends a WM_CLOSE message to the process main window, or all the process threads.
        /// Windowless application must handle the message manually, via <see cref="ThreadMessageQueue.WaitMessageAsync(uint)"/>.
        /// </summary>
        /// <param name="process">Process.</param>
        /// <returns>True if successful.</returns>
        public static void SendCloseRequest(this Process process) {
            const uint WM_CLOSE = 0x0010;
            if (process.MainWindowHandle != IntPtr.Zero) {
                process.CloseMainWindow();
            } else {
                foreach (ProcessThread thread in process.Threads) NativeMethods.PostThreadMessage((uint)thread.Id, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

    }

}