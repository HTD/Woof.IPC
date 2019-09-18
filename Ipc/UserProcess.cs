﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Woof.Ipc.Win32Imports;
using Woof.Ipc.Win32Types;

namespace Woof.Ipc {

    /// <summary>
    /// Special class allowing to create process as user from SYSTEM account context.
    /// </summary>
    public static class UserProcess {

        /// <summary>
        /// Gets a value indicationg whether the current account is Windows System account.
        /// </summary>
        public static bool IsSystemContext => Environment.UserName == "SYSTEM";

        /// <summary>
        /// Starts a process as user from SYSTEM account context, in user context behaves exactly as <see cref="Process.Start(ProcessStartInfo)"/>.
        /// IMPORTANT: UseShellExecute property of the <see cref="ProcessStartInfo"/> provided must be false!
        /// </summary>
        /// <param name="processStartInfo">
        /// The <see cref="System.Diagnostics.ProcessStartInfo"/> that contains the information that is 
        /// used to start the process, including the file name and any command-line arguments.</param>
        /// <returns>A new System.Diagnostics.Process that is associated with the process resource,
        /// or null if no process resource is started. Note that a new process that’s started
        /// alongside already running instances of the same process will be independent from
        /// the others. In addition, Start may return a non-null Process with its System.Diagnostics.Process.HasExited
        /// property already set to true. In this case, the started process may have activated
        /// an existing instance of itself and then exited.</returns>
        public static Process Start(ProcessStartInfo processStartInfo)
            => (IsSystemContext && !processStartInfo.UseShellExecute)
                ? CreateProcessAsUser(processStartInfo)
                : Process.Start(processStartInfo);

        /// <summary>
        /// Uses Win32 API CreateProcessAsUser to start defined process in current user (GUI session owner) context.
        /// </summary>
        /// <returns>The process created.</returns>
        /// <exception cref="ArgumentException">The process specified by the processId parameter is not running. The identifier might be expired.</exception>
        /// <exception cref="InvalidOperationException"><see cref="NativeMethods.CreateEnvironmentBlock(ref IntPtr, IntPtr, bool)"/> or <see cref="NativeMethods.CreateProcessAsUser(IntPtr, string, string, IntPtr, IntPtr, bool, ProcessCreationFlags, IntPtr, string, ref StartupInfo, out ProcessInformation)"/> failed.</exception>
        /// <exception cref="UnauthorizedAccessException"><see cref="GetSessionUserToken(ref IntPtr)"/> failed.</exception>
        public static Process CreateProcessAsUser(ProcessStartInfo startInfo) {
            var command = Path.GetFileNameWithoutExtension(startInfo.FileName);
            var cmdLine = String.Join(" ", command, startInfo.Arguments);
            var userTokenHandle = IntPtr.Zero;
            var pEnv = IntPtr.Zero;
            var startupInfo = new StartupInfo() {
                Size = Marshal.SizeOf<StartupInfo>(),
                ShowWindow = ShowWindow.Hide,
                Desktop = null // default active, "winsta0\\default" didn't work from Windows Installer.
            };
            var processInformation = new ProcessInformation();
            int iResultOfCreateProcessAsUser;
            try {
                if (!GetSessionUserToken(ref userTokenHandle)) throw new UnauthorizedAccessException("GetSessionUserToken failed.");
                var creationFlags = ProcessCreationFlags.CreateUnicodeEnvironment;
                if (startInfo.CreateNoWindow) creationFlags |= ProcessCreationFlags.CreateNoWindow;
                if (!NativeMethods.CreateEnvironmentBlock(ref pEnv, userTokenHandle, false))
                    throw new InvalidOperationException("CreateEnvironmentBlock failed.");
                var ok = NativeMethods.CreateProcessAsUser(
                    userTokenHandle, // A handle to the primary token that represents a user
                    Path.Combine(startInfo.WorkingDirectory, startInfo.FileName), // The name of the module to be executed
                    cmdLine, // The command line to be executed
                    IntPtr.Zero, // A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new process object and determines whether child processes can inherit the returned handle to the process
                    IntPtr.Zero, // A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread object and determines whether child processes can inherit the returned handle to the thread
                    true, // If this parameter is TRUE, each inheritable handle in the calling process is inherited by the new process.
                    creationFlags, // The flags that control the priority class and the creation of the process
                    pEnv, // A pointer to an environment block for the new process.
                    startInfo.WorkingDirectory, // The full path to the current directory for the process
                    ref startupInfo, // STARTUPINFO or STARTUPINFOEX structure
                    out processInformation // PROCESS_INFORMATION structure that receives identification information about the new process
                );
                iResultOfCreateProcessAsUser = Marshal.GetLastWin32Error();
                if (!ok) throw new InvalidOperationException($"CreateProcessAsUser failed, Win32 error: {iResultOfCreateProcessAsUser}.");
            }
            finally {
                NativeMethods.CloseHandle(userTokenHandle);
                NativeMethods.CloseHandle(processInformation.ProcessHandle);
                NativeMethods.CloseHandle(processInformation.ThreadHandle);
                if (pEnv != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(pEnv);
            }
            return Process.GetProcessById((int)processInformation.ProcessId);
        }

        /// <summary>
        /// Gets the active session identifier.
        /// </summary>
        /// <returns>Active session identifier.</returns>
        public static uint GetActiveSessionId() {
            const uint invalidSessionId = 0xffffffff;
            var wtsCurrentServerHandle = IntPtr.Zero;
            var activeSessionId = invalidSessionId;
            var sessionInfoPtr = IntPtr.Zero;
            var sessionCount = 0;
            if (NativeMethods.WTSEnumerateSessions(wtsCurrentServerHandle, 0, 1, ref sessionInfoPtr, ref sessionCount) != 0) {
                var arrayElementSize = Marshal.SizeOf(typeof(WtsSessionInfo));
                var currentSessionInfoPtr = sessionInfoPtr;
                for (var i = 0; i < sessionCount; i++) {
                    var sessionInfo = (WtsSessionInfo)Marshal.PtrToStructure(currentSessionInfoPtr, typeof(WtsSessionInfo));
                    currentSessionInfoPtr += arrayElementSize;
                    if (sessionInfo.State == WtsConnectStateClass.Active) activeSessionId = sessionInfo.SessionId;
                }
            }
            if (activeSessionId == invalidSessionId) activeSessionId = NativeMethods.WTSGetActiveConsoleSessionId();
            if (activeSessionId == invalidSessionId) throw new InvalidOperationException();
            return activeSessionId;
        }

        /// <summary>
        /// Disconnects the active session. The local user using the computer is logged off immediately.
        /// </summary>
        public static void DisconnectActiveSession() {
            var wtsCurrentServerHandle = IntPtr.Zero;
            var activeSessionId = GetActiveSessionId();
            NativeMethods.WTSDisconnectSession(wtsCurrentServerHandle, activeSessionId, false);
        }

        /// <summary>
        /// Gets the user token from the currently active session
        /// </summary>
        /// <param name="phUserToken">A pointer to user token structure.</param>
        /// <returns>True if successfull.</returns>
        private static bool GetSessionUserToken(ref IntPtr phUserToken) {
            var result = false;
            var impersonationTokenHandle = IntPtr.Zero;
            var activeSessionId = GetActiveSessionId();
            if (NativeMethods.WTSQueryUserToken(activeSessionId, ref impersonationTokenHandle) != 0) {
                // Convert the impersonation token to a primary token
                result = NativeMethods.DuplicateTokenEx(impersonationTokenHandle, 0, IntPtr.Zero,
                    (int)SecurityImpersonationLevel.Impersonation, (int)TokenType.Primary,
                    ref phUserToken);
                NativeMethods.CloseHandle(impersonationTokenHandle);
            }
            return result;
        }

    }

}