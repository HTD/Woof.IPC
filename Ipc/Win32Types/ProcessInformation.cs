using System;
using System.Runtime.InteropServices;

namespace Woof.Ipc.Win32Types {

    /// <summary>
    /// Contains information about a newly created process and its primary thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct ProcessInformation {

        /// <summary>
        /// A handle to the newly created process. The handle is used to specify the process in all functions that perform operations on the process object.
        /// </summary>
        public IntPtr ProcessHandle;

        /// <summary>
        /// A handle to the primary thread of the newly created process. The handle is used to specify the thread in all functions that perform operations on the thread object.
        /// </summary>
        public IntPtr ThreadHandle;

        /// <summary>
        /// A value that can be used to identify a process. The value is valid from the time the process is created until all handles to the process are closed and the process object is freed; at this point, the identifier may be reused.
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// A value that can be used to identify a thread. The value is valid from the time the thread is created until all handles to the thread are closed and the thread object is freed; at this point, the identifier may be reused.
        /// </summary>
        public uint ThreadId;

    }

}