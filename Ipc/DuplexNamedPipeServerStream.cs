using System;
using System.IO.Pipes;

namespace Woof.Ipc {

    /// <summary>
    /// Duplex named pipe server stream consisting of 2 simplex <see cref="NamedPipeServerStream"/>s for input and output.
    /// </summary>
    public sealed class DuplexNamedPipeServerStream : IDisposable {

        /// <summary>
        /// Gets the stream used for reading.
        /// </summary>
        public NamedPipeServerStream Input { get; }

        /// <summary>
        /// Gets the stream used for writing.
        /// </summary>
        public NamedPipeServerStream Output { get; }

        /// <summary>
        /// Creates the duplex stream from 2 simplex streams.
        /// </summary>
        /// <param name="input">The stream used for reading.</param>
        /// <param name="output">The stream used for writing.</param>
        internal DuplexNamedPipeServerStream(NamedPipeServerStream input, NamedPipeServerStream output) {
            Input = input;
            Output = output;
        }

        /// <summary>
        /// Disposes both simplex streams.
        /// </summary>
        public void Dispose() {
            if (!IsDisposed) {
                Input.Dispose();
                Output.Dispose();
                IsDisposed = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool IsDisposed;

    }

}