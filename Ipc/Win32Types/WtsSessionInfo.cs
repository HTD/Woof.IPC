using System;
using System.Runtime.InteropServices;

namespace Woof.Ipc.Win32Types {

    /// <summary>
    /// Contains information about a client session on a Remote Desktop Session Host (RD Session Host) server.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct WtsSessionInfo {
        
        /// <summary>
        /// Session identifier of the session.
        /// </summary>
        public readonly UInt32 SessionId;
        
        /// <summary>
        /// String that contains the WinStation name of this session. The WinStation name is a name that Windows associates with the session, for example, "services", "console", or "RDP-Tcp#0".
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public readonly String WinStationName;
        
        /// <summary>
        /// A value from the <see cref="WtsConnectStateClass"/> enumeration type that indicates the session's current connection state.
        /// </summary>
        public readonly WtsConnectStateClass State;

    }

}