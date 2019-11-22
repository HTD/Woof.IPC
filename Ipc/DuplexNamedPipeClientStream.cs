using System;
using System.IO.Pipes;

namespace Woof.Ipc {

    /// <summary>
    /// Duplex named pipe client stream consisting of 2 simplex <see cref="NamedPipeClientStream"/>s for input and output.
    /// </summary>
    public sealed class DuplexNamedPipeClientStream : IDisposable {

        /// <summary>
        /// Gets the stream used for reading.    
        /// </summary>
        public NamedPipeClientStream Input { get; }
        
        /// <summary>
        /// Gets the stream used for writing.
        /// </summary>
        public NamedPipeClientStream Output { get; }

        /// <summary>
        /// Creates the duplex stream from 2 simplex streams.
        /// </summary>
        /// <param name="input">The stream used for reading.</param>
        /// <param name="output">The stream used for writing.</param>
        internal DuplexNamedPipeClientStream(NamedPipeClientStream input, NamedPipeClientStream output) {
            Input = input;
            Output = output;
        }

        /// <summary>
        /// Disposes both simplex streams.
        /// </summary>
        public void Dispose() {
            if (!IsDisposed) {
                IsDisposed = true;
                Input.Dispose();
                Output.Dispose();
            }
        }

        /// <summary>
        /// True if dispose was called.
        /// </summary>
        private bool IsDisposed;

    }

}