using System;
using System.Runtime.InteropServices;

using Woof.Ipc.Win32Types;

namespace Woof.Ipc.Win32Imports {

    internal static class NativeMethods {

        /// <summary>
        /// Creates a new process and its primary thread. The new process runs in the security context of the user represented by the specified token.
        /// </summary>
        /// <param name="hToken">A handle to the primary token that represents a user.</param>
        /// <param name="lpApplicationName">The name of the module to be executed.</param>
        /// <param name="lpCommandLine">The command line to be executed. The maximum length of this string is 32K characters. If lpApplicationName is NULL, the module name portion of lpCommandLine is limited to MAX_PATH characters.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new process object and determines whether child processes can inherit the returned handle to the process. If lpProcessAttributes is NULL or lpSecurityDescriptor is NULL, the process gets a default security descriptor and the handle cannot be inherited. The default security descriptor is that of the user referenced in the hToken parameter. This security descriptor may not allow access for the caller, in which case the process may not be opened again after it is run. The process handle is valid and will continue to have full access rights.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread object and determines whether child processes can inherit the returned handle to the thread. If lpThreadAttributes is NULL or lpSecurityDescriptor is NULL, the thread gets a default security descriptor and the handle cannot be inherited. The default security descriptor is that of the user referenced in the hToken parameter. This security descriptor may not allow access for the caller.</param>
        /// <param name="bInheritHandle">If this parameter is TRUE, each inheritable handle in the calling process is inherited by the new process. If the parameter is FALSE, the handles are not inherited. Note that inherited handles have the same value and access rights as the original handles. Terminal Services:  You cannot inherit handles across sessions. Additionally, if this parameter is TRUE, you must create the process in the same session as the caller.</param>
        /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process. For a list of values, see Process Creation Flags. This parameter also controls the new process's priority class, which is used to determine the scheduling priorities of the process's threads. For a list of values, see GetPriorityClass. If none of the priority class flags is specified, the priority class defaults to NORMAL_PRIORITY_CLASS unless the priority class of the creating process is IDLE_PRIORITY_CLASS or BELOW_NORMAL_PRIORITY_CLASS. In this case, the child process receives the default priority class of the calling process.</param>
        /// <param name="lpEnvironment">A pointer to an environment block for the new process. If this parameter is NULL, the new process uses the environment of the calling process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the process. The string can also specify a UNC path. If this parameter is NULL, the new process will have the same current drive and directory as the calling process. (This feature is provided primarily for shells that need to start an application and specify its initial drive and working directory.)</param>
        /// <param name="lpStartupInfo">A pointer to a <see cref="StartupInfo"/> or STARTUPINFOEX structure.</param>
        /// <param name="lpProcessInformation">A pointer to a PROCESS_INFORMATION structure that receives identification information about the new process. Handles in PROCESS_INFORMATION must be closed with CloseHandle when they are no longer needed.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.To get extended error information, call GetLastError.</returns>
        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            String lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandle,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        /// <summary>
        /// Creates a new access token that duplicates an existing token. This function can create either a primary token or an impersonation token.
        /// </summary>
        /// <param name="ExistingTokenHandle">A handle to an access token opened with TOKEN_DUPLICATE access.</param>
        /// <param name="dwDesiredAccess">Specifies the requested access rights for the new token. The DuplicateTokenEx function compares the requested access rights with the existing token's discretionary access control list (DACL) to determine which rights are granted or denied. To request the same access rights as the existing token, specify zero. To request all access rights that are valid for the caller, specify MAXIMUM_ALLOWED. For a list of access rights for access tokens, see Access Rights for Access-Token Objects.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new token and determines whether child processes can inherit the token. If lpTokenAttributes is NULL, the token gets a default security descriptor and the handle cannot be inherited. If the security descriptor contains a system access control list (SACL), the token gets ACCESS_SYSTEM_SECURITY access right, even if it was not requested in dwDesiredAccess. To set the owner in the security descriptor for the new token, the caller's process token must have the SE_RESTORE_NAME privilege set.</param>
        /// <param name="TokenType">Specifies one of the following values from the TOKEN_TYPE enumeration.</param>
        /// <param name="ImpersonationLevel">Specifies a value from the <see cref="SecurityImpersonationLevel"/> enumeration that indicates the impersonation level of the new token.</param>
        /// <param name="DuplicateTokenHandle">A pointer to a variable that receives a handle to the duplicate token. This handle has TOKEN_IMPERSONATE and TOKEN_QUERY access to the new token. When you have finished using the new token, call the CloseHandle function to close the token handle.</param>
        /// <returns>If the function succeeds, the function returns a nonzero value. If the function fails, it returns zero.To get extended error information, call GetLastError.</returns>
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            ref IntPtr DuplicateTokenHandle);

        /// <summary>
        /// Retrieves the environment variables for the specified user. This block can then be passed to the CreateProcessAsUser function.
        /// </summary>
        /// <param name="lpEnvironment">When this function returns, receives a pointer to the new environment block. The environment block is an array of null-terminated Unicode strings. The list ends with two nulls (\0\0).</param>
        /// <param name="hToken">Token for the user, returned from the LogonUser function. If this is a primary token, the token must have TOKEN_QUERY and TOKEN_DUPLICATE access. If the token is an impersonation token, it must have TOKEN_QUERY access. For more information, see Access Rights for Access-Token Objects. If this parameter is NULL, the returned environment block contains system variables only.</param>
        /// <param name="bInherit">Specifies whether to inherit from the current process' environment. If this value is TRUE, the process inherits the current process' environment. If this value is FALSE, the process does not inherit the current process' environment.</param>
        /// <returns>TRUE if successful; otherwise, FALSE. To get extended error information, call GetLastError.</returns>
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        /// <summary>
        /// Frees environment variables created by the CreateEnvironmentBlock function.
        /// </summary>
        /// <param name="lpEnvironment">Pointer to the environment block created by CreateEnvironmentBlock. The environment block is an array of null-terminated Unicode strings. The list ends with two nulls (\0\0).</param>
        /// <returns>TRUE if successful; otherwise, FALSE. To get extended error information, call GetLastError.</returns>
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>TRUE if successful; otherwise, FALSE. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Retrieves the session identifier of the console session. The console session is the session that is currently attached to the physical console. Note that it is not necessary that Remote Desktop Services be running for this function to succeed.
        /// </summary>
        /// <returns>The session identifier of the session that is attached to the physical console. If there is no session attached to the physical console, (for example, if the physical console session is in the process of being attached or detached), this function returns 0xFFFFFFFF.</returns>
        [DllImport("kernel32.dll")]
        public static extern uint WTSGetActiveConsoleSessionId();

        /// <summary>
        /// Obtains the primary access token of the logged-on user specified by the session ID. To call this function successfully, the calling application must be running within the context of the LocalSystem account and have the SE_TCB_NAME privilege.
        /// </summary>
        /// <param name="SessionId">A Remote Desktop Services session identifier. Any program running in the context of a service will have a session identifier of zero (0). You can use the WTSEnumerateSessions function to retrieve the identifiers of all sessions on a specified RD Session Host server. To be able to query information for another user's session, you need to have the Query Information permission. For more information, see Remote Desktop Services Permissions. To modify permissions on a session, use the Remote Desktop Services Configuration administrative tool.</param>
        /// <param name="phToken">If the function succeeds, receives a pointer to the token handle for the logged-on user. Note that you must call the CloseHandle function to close this handle.</param>
        /// <returns></returns>
        [DllImport("Wtsapi32.dll")]
        public static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        /// <summary>
        /// Retrieves a list of sessions on a Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <param name="hServer">A handle to the RD Session Host server.</param>
        /// <param name="Reserved">This parameter is reserved. It must be zero.</param>
        /// <param name="Version">The version of the enumeration request. This parameter must be 1.</param>
        /// <param name="ppSessionInfo">A pointer to an array of WTS_SESSION_INFO structures that represent the retrieved sessions. To free the returned buffer, call the WTSFreeMemory function.</param>
        /// <param name="pCount">A pointer to the number of WTS_SESSION_INFO structures returned in the ppSessionInfo parameter.</param>
        /// <returns></returns>
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern int WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        /// <summary>
        /// Disconnects the logged-on user from the specified Remote Desktop Services session without closing the session. If the user subsequently logs on to the same Remote Desktop Session Host (RD Session Host) server, the user is reconnected to the same session.
        /// </summary>
        /// <param name="hServer">A handle to an RD Session Host server. Specify a handle opened by the WTSOpenServer or WTSOpenServerEx function, or specify WTS_CURRENT_SERVER_HANDLE to indicate the RD Session Host server on which your application is running.</param>
        /// <param name="sessionId">A Remote Desktop Services session identifier. To indicate the current session, specify WTS_CURRENT_SESSION. To retrieve the identifiers of all sessions on a specified RD Session Host server, use the WTSEnumerateSessions function.</param>
        /// <param name="bWait">Indicates whether the operation is synchronous. Specify TRUE to wait for the operation to complete, or FALSE to return immediately.</param>
        /// <returns></returns>
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSDisconnectSession(IntPtr hServer, uint sessionId, bool bWait);

        /// <summary>
        /// Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Posts a message to the message queue of the specified thread. It returns without waiting for the thread to process the message.
        /// </summary>
        /// <param name="threadId">The identifier of the thread to which the message is to be posted.</param>
        /// <param name="msg">The type of message to be posted.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Enumerates all nonchild windows associated with a thread by passing the handle to each window, in turn, to an application-defined callback function. EnumThreadWindows continues until the last window is enumerated or the callback function returns FALSE. To enumerate child windows of a particular window, use the EnumChildWindows function.
        /// </summary>
        /// <param name="dwThreadId">The identifier of the thread whose windows are to be enumerated.</param>
        /// <param name="lpfn">A pointer to an application-defined callback function. For more information, see EnumThreadWndProc.</param>
        /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpdwProcessId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.</param>
        /// <returns>The return value is the identifier of the thread that created the window.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// An application-defined callback function used with the EnumThreadWindows function. It receives the window handles associated with a thread. The WNDENUMPROC type defines a pointer to this callback function. EnumThreadWndProc is a placeholder for the application-defined function name.
        /// </summary>
        /// <param name="hWnd">A handle to a window associated with the thread specified in the EnumThreadWindows function.</param>
        /// <param name="lParam">The application-defined value given in the EnumThreadWindows function.</param>
        /// <returns></returns>
        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    }

}