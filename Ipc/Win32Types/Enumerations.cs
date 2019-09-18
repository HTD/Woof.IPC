using System;

namespace Woof.Ipc.Win32Types {

    /// <summary>
    /// ShowWindow command enumeration.
    /// </summary>
    enum ShowWindow : short {

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,
        
        /// <summary>
        /// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
        /// </summary>
        ShowNormal = 1,
        
        /// <summary>
        /// Default window size and position.
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,
        
        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 3,
        
        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        Maximize = 3,
        
        /// <summary>
        /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
        /// </summary>
        ShowNoActivate = 4,
        
        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,
        
        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        Minimize = 6,
        
        /// <summary>
        /// Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
        /// </summary>
        ShowMinNoActive = 7,
        
        /// <summary>
        /// Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
        /// </summary>
        ShowNA = 8,
        
        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,
        
        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        ShowDefault = 10,
        
        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        Max = 10

    }

    /// <summary>
    /// Specifies the connection state of a Remote Desktop Services session.
    /// </summary>
    enum WtsConnectStateClass {

        /// <summary>
        /// A user is logged on to the WinStation.
        /// </summary>
        Active,

        /// <summary>
        /// The WinStation is connected to the client.
        /// </summary>
        Connected,

        /// <summary>
        /// The WinStation is in the process of connecting to the client.
        /// </summary>
        ConnectQuery,

        /// <summary>
        /// The WinStation is shadowing another WinStation.
        /// </summary>
        Shadow,

        /// <summary>
        /// The WinStation is active but the client is disconnected.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The WinStation is waiting for a client to connect.
        /// </summary>
        Idle,

        /// <summary>
        /// The WinStation is listening for a connection. A listener session waits for requests for new client connections. No user is logged on a listener session. A listener session cannot be reset, shadowed, or changed to a regular client session.
        /// </summary>
        Listen,

        /// <summary>
        /// The WinStation is being reset.
        /// </summary>
        Reset,

        /// <summary>
        /// The WinStation is down due to an error.
        /// </summary>
        Down,

        /// <summary>
        /// The WinStation is initializing.
        /// </summary>
        Init

    }

    /// <summary>
    /// Contains values that specify security impersonation levels. Security impersonation levels govern the degree to which a server process can act on behalf of a client process.
    /// </summary>
    enum SecurityImpersonationLevel {

        /// <summary>
        /// The server process cannot obtain identification information about the client, and it cannot impersonate the client. It is defined with no value given, and thus, by ANSI C rules, defaults to a value of zero.
        /// </summary>
        Anonymous = 0,

        /// <summary>
        /// The server process can obtain information about the client, such as security identifiers and privileges, but it cannot impersonate the client. This is useful for servers that export their own objects, for example, database products that export tables and views. Using the retrieved client-security information, the server can make access-validation decisions without being able to use other services that are using the client's security context.
        /// </summary>
        Identification = 1,

        /// <summary>
        /// The server process can impersonate the client's security context on its local system. The server cannot impersonate the client on remote systems.
        /// </summary>
        Impersonation = 2,

        /// <summary>
        /// The server process can impersonate the client's security context on remote systems.
        /// </summary>
        Delegation = 3

    }

    /// <summary>
    /// Contains values that differentiate between a primary token and an impersonation token.
    /// </summary>
    enum TokenType {

        /// <summary>
        /// Indicates a primary token.
        /// </summary>
        Primary = 1,

        /// <summary>
        /// Indicates an impersonation token.
        /// </summary>
        Impersonation = 2

    }

    /// <summary>
    /// Used by the CreateProcess, CreateProcessAsUser, CreateProcessWithLogonW, and CreateProcessWithTokenW functions. They can be specified in any combination, except as noted.
    /// </summary>
    [Flags]
    enum ProcessCreationFlags {

        /// <summary>
        /// The child processes of a process associated with a job are not associated with the job.
        /// If the calling process is not associated with a job, this constant has no effect. If the calling process is associated with a job, the job must set the JOB_OBJECT_LIMIT_BREAKAWAY_OK limit.
        /// </summary>
        CreateBreakawayFromJob = 0x01000000,

        /// <summary>
        /// The new process does not inherit the error mode of the calling process. Instead, the new process gets the default error mode.
        /// This feature is particularly useful for multithreaded shell applications that run with hard errors disabled.
        /// The default behavior is for the new process to inherit the error mode of the caller.Setting this flag changes that default behavior.
        /// </summary>
        CreateDefaultErrorMode = 0x04000000,

        /// <summary>
        /// The new process has a new console, instead of inheriting its parent's console (the default). For more information, see Creation of a Console.
        /// This flag cannot be used with DETACHED_PROCESS.
        /// </summary>
        CreateNewConsole = 0x00000010,

        /// <summary>
        /// The new process is the root process of a new process group. The process group includes all processes that are descendants of this root process. The process identifier of the new process group is the same as the process identifier, which is returned in the lpProcessInformation parameter. Process groups are used by the GenerateConsoleCtrlEvent function to enable sending a CTRL+BREAK signal to a group of console processes.
        /// If this flag is specified, CTRL+C signals will be disabled for all processes within the new process group.
        /// This flag is ignored if specified with CREATE_NEW_CONSOLE.
        /// </summary>
        CreateNewProcessGroup = 0x00000200,

        /// <summary>
        /// The process is a console application that is being run without a console window. Therefore, the console handle for the application is not set.
        /// This flag is ignored if the application is not a console application, or if it is used with either CREATE_NEW_CONSOLE or DETACHED_PROCESS.
        /// </summary>
        CreateNoWindow = 0x08000000,

        /// <summary>
        /// The process is to be run as a protected process. The system restricts access to protected processes and the threads of protected processes. For more information on how processes can interact with protected processes, see Process Security and Access Rights.
        /// To activate a protected process, the binary must have a special signature.This signature is provided by Microsoft but not currently available for non-Microsoft binaries.There are currently four protected processes: media foundation, audio engine, Windows error reporting, and system.Components that load into these binaries must also be signed.Multimedia companies can leverage the first two protected processes.For more information, see Overview of the Protected Media Path.
        /// Windows Server 2003 and Windows XP: This value is not supported.
        /// </summary>
        CreateProtectedProcess = 0x00040000,

        /// <summary>
        /// Allows the caller to execute a child process that bypasses the process restrictions that would normally be applied automatically to the process.
        /// </summary>
        CreatePreserveCodeAuthzLevel = 0x02000000,

        /// <summary>
        /// This flag allows secure processes, that run in the Virtualization-Based Security environment, to launch.
        /// </summary>
        CreateSecureProcess = 0x00400000,

        /// <summary>
        /// This flag is valid only when starting a 16-bit Windows-based application. If set, the new process runs in a private Virtual DOS Machine (VDM). By default, all 16-bit Windows-based applications run as threads in a single, shared VDM. The advantage of running separately is that a crash only terminates the single VDM; any other programs running in distinct VDMs continue to function normally. Also, 16-bit Windows-based applications that are run in separate VDMs have separate input queues. That means that if one application stops responding momentarily, applications in separate VDMs continue to receive input. The disadvantage of running separately is that it takes significantly more memory to do so. You should use this flag only if the user requests that 16-bit applications should run in their own VDM.
        /// </summary>
        CreateSeparateWowVdm = 0x00000800,

        /// <summary>
        /// The flag is valid only when starting a 16-bit Windows-based application. If the DefaultSeparateVDM switch in the Windows section of WIN.INI is TRUE, this flag overrides the switch. The new process is run in the shared Virtual DOS Machine.
        /// </summary>
        CreateSharedWowVdm = 0x00001000,

        /// <summary>
        /// The primary thread of the new process is created in a suspended state, and does not run until the ResumeThread function is called.
        /// </summary>
        CreateSuspended = 0x00000004,

        /// <summary>
        /// If this flag is set, the environment block pointed to by lpEnvironment uses Unicode characters. Otherwise, the environment block uses ANSI characters.
        /// </summary>
        CreateUnicodeEnvironment = 0x00000400,

        /// <summary>
        /// The calling thread starts and debugs the new process. It can receive all related debug events using the WaitForDebugEvent function.
        /// </summary>
        DebugOnlyThisProcess = 0x00000002,

        /// <summary>
        /// The calling thread starts and debugs the new process and all child processes created by the new process. It can receive all related debug events using the WaitForDebugEvent function.
        /// A process that uses DEBUG_PROCESS becomes the root of a debugging chain. This continues until another process in the chain is created with DEBUG_PROCESS.
        /// If this flag is combined with DEBUG_ONLY_THIS_PROCESS, the caller debugs only the new process, not any child processes.
        /// </summary>
        DebugProcess = 0x00000001,

        /// <summary>
        /// For console processes, the new process does not inherit its parent's console (the default). The new process can call the AllocConsole function at a later time to create a console. For more information, see Creation of a Console.
        /// This value cannot be used with CREATE_NEW_CONSOLE.
        /// </summary>
        DetachedProcess = 0x00000008,

        /// <summary>
        /// The process is created with extended startup information; the lpStartupInfo parameter specifies a STARTUPINFOEX structure.
        /// Windows Server 2003 and Windows XP: This value is not supported.
        /// </summary>
        ExtendedStartupInfoPresent = 0x00080000,

        /// <summary>
        /// The process inherits its parent's affinity. If the parent process has threads in more than one processor group, the new process inherits the group-relative affinity of an arbitrary group in use by the parent.
        /// Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP: This value is not supported.
        /// </summary>
        InheritParentAffinity = 0x00010000

    }

}