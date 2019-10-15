using System;
using System.Threading.Tasks;
using System.Windows.Interop;

using Woof.Ipc.Win32Imports;

namespace Woof.Ipc {

    /// <summary>
    /// Contains methods for reading the calling thread message queue.
    /// </summary>
    public static class ThreadMessageQueue {

        /// <summary>
        /// Waits synchronously for the message to appear in the calling thred's message queue, then returns it.
        /// </summary>
        /// <param name="filterMin">Lowest message value to retrieve.</param>
        /// <param name="filterMax">Highest message value to retrieve.</param>
        /// <returns>Message retrieved.</returns>
        public static MSG GetMessage(uint filterMin = 0, uint filterMax = 0) {
            NativeMethods.GetMessage(out var msg, IntPtr.Zero, filterMin, filterMax);
            return msg;
        }

        /// <summary>
        /// Waits asynchronously for the message to appear in the calling thred's message queue, then returns it.
        /// </summary>
        /// <param name="filterMin">Lowest message value to retrieve.</param>
        /// <param name="filterMax">Highest message value to retrieve.</param>
        /// <returns>Message retrieved.</returns>
        public static async Task<MSG> GetMessageAsync(uint filterMin = 0, uint filterMax = 0)
            => await Task.Run(() => {
            NativeMethods.GetMessage(out var msg, IntPtr.Zero, filterMin, filterMax);
            return msg;
        });

        /// <summary>
        /// Waits synchronously for the message to appear in the calling thred's message queue.
        /// </summary>
        /// <param name="signal">Message value to wait for.</param>
        public static void WaitMessage(uint signal) => NativeMethods.GetMessage(out _, IntPtr.Zero, signal, signal);

        /// <summary>
        /// Waits asynchronously for the message to appear in the calling thred's message queue.
        /// </summary>
        /// <param name="signal">Message value to wait for.</param>
        /// <returns>Task.</returns>
        public static async Task WaitMessageAsync(uint signal) => await Task.Run(() => NativeMethods.GetMessage(out _, IntPtr.Zero, signal, signal));

    }

}