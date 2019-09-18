using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Woof.Ipc.Win32Imports;

namespace Woof.Ipc {

    /// <summary>
    /// Extends the Process class providing methods to communicate with processes.
    /// </summary>
    public static class ProcessExtensions {

        /// <summary>
        /// Sends a WM_CLOSE message to the process.
        /// This asks the process politely to shut down properly.
        /// </summary>
        /// <param name="process">Process.</param>
        /// <returns>True if successful.</returns>
        public static bool SendCloseRequest(this Process process) {
            const uint WM_CLOSE = 0x0010;
            return process.PostThreadMessage(WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Sends a message to the first enumerated window in the first enumerated thread with at least one window, and returns the handle of that window through the hwnd output parameter if such a window was enumerated.  If a window was enumerated, the return value is the return value of the SendMessage call, otherwise the return value is zero.
        /// </summary>
        /// <param name="p">Process.</param>
        /// <param name="msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
        public static IntPtr SendMessage(this Process p, UInt32 msg, IntPtr wParam, IntPtr lParam) {
            var hwnd = p.GetWindowHandles().FirstOrDefault();
            if (hwnd != IntPtr.Zero)
                return NativeMethods.SendMessage(hwnd, msg, wParam, lParam);
            else
                return IntPtr.Zero;
        }

        /// <summary>
        /// Posts a message to the first enumerated window in the first enumerated thread with at least one window, and returns the handle of that window through the hwnd output parameter if such a window was enumerated.  If a window was enumerated, the return value is the return value of the PostMessage call, otherwise the return value is false.
        /// </summary>
        /// <param name="p">Process.</param>
        /// <param name="msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>True if successfull.</returns>
        public static bool PostMessage(this Process p, UInt32 msg, IntPtr wParam, IntPtr lParam) {
            var hwnd = p.GetWindowHandles().FirstOrDefault();
            if (hwnd != IntPtr.Zero)
                return NativeMethods.PostMessage(hwnd, msg, wParam, lParam);
            else
                return false;
        }

        /// <summary>
        /// Posts a thread message to the first enumerated thread (when ensureTargetThreadHasWindow is false), or posts a thread message to the first enumerated thread with a window, unless no windows are found in which case the call fails.  If an appropriate thread was found, the return value is the return value of PostThreadMessage call, otherwise the return value is false.
        /// </summary>
        /// <param name="p">Process.</param>
        /// <param name="msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <param name="ensureTargetThreadHasWindow">Set true for threads having windows.</param>
        /// <returns>True if successfull.</returns>
        public static bool PostThreadMessage(this Process p, UInt32 msg, IntPtr wParam, IntPtr lParam, bool ensureTargetThreadHasWindow = false) {
            uint targetThreadId = 0;
            if (ensureTargetThreadHasWindow) {
                IntPtr hwnd = p.GetWindowHandles().FirstOrDefault();
                if (hwnd != IntPtr.Zero) targetThreadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
            }
            else {
                targetThreadId = (uint)p.Threads[0].Id;
            }
            if (targetThreadId != 0)
                return NativeMethods.PostThreadMessage(targetThreadId, msg, wParam, lParam);
            else
                return false;
        }

        /// <summary>
        /// Enumerates window handles of the process.
        /// </summary>
        /// <param name="process">Process.</param>
        /// <returns>Handles enumeration.</returns>
        public static IEnumerable<IntPtr> GetWindowHandles(this Process process) {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in process.Threads)
                NativeMethods.EnumThreadWindows((uint)thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            return handles;
        }

    }

}