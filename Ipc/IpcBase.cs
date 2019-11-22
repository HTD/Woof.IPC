namespace Woof.Ipc {

    /// <summary>
    /// Base class for IPC client and server communication via named pipes.
    /// </summary>
    public abstract class IpcBase {

        /// <summary>
        /// Gets or sets the buffer size for a single message.
        /// Message size cannot exceed this value.
        /// Default 1024 bytes.
        /// </summary>
        public int MessageBufferSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the pipe name.
        /// </summary>
        public string PipeName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the instance has been started.
        /// </summary>
        public bool IsStarted { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; protected set; }
        
        /// <summary>
        /// True if the instance startup sequence is initialized but not finished.
        /// </summary>
        protected bool IsStarting;

        /// <summary>
        /// True if the instance shutdown sequence is initialized but not finished.
        /// </summary>
        protected bool IsStopping;

        /// <summary>
        /// Common exception messages for the module.
        /// </summary>
        protected static class ExceptionMessages {
            
            /// <summary>
            /// Insufficient buffer size exception message.
            /// </summary>
            public static readonly string InsufficientBufferSize = $"{nameof(MessageBufferSize)}";
            
            /// <summary>
            /// Invalid direction value exception message.
            /// </summary>
            public const string InvalidDirectionValue = "Invalid direction value";
            
            /// <summary>
            /// Pipe name not set.
            /// </summary>
            public const string PipeNameNotSet = "Pipe name not set";
        
        }
    
    }

}