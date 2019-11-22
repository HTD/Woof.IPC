using System;

namespace Woof.Ipc {

    /// <summary>
    /// Event arguments for transmitting short binary messages.
    /// </summary>
    public class BinaryMessageEventArgs : EventArgs {

        /// <summary>
        /// Gets the received message.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Writing irrelevant")]
        public byte[] Message { get; }

        /// <summary>
        /// Gets or sets the response message, if not set - there will be no response to the message received.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Writing intended")]
        public byte[] Response { get; set; }

        /// <summary>
        /// Creates a message event arguments from the raw binary data.
        /// </summary>
        /// <param name="message">Message data.</param>
        public BinaryMessageEventArgs(byte[] message) => Message = message;

        /// <summary>
        /// Creates a message event arguments from the buffer and the specified length.
        /// </summary>
        /// <param name="buffer">Buffer containing message data.</param>
        /// <param name="length">The actual length of the data.</param>
        public BinaryMessageEventArgs(byte[] buffer, int length) {
            Message = new byte[length];
            Buffer.BlockCopy(buffer, 0, Message, 0, length);
        }

    }

}