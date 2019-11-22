namespace Woof.Ipc {

    /// <summary>
    /// An interface for a message codec.
    /// </summary>
    interface IMessageCodec {
        
        /// <summary>
        /// Encodes the data.
        /// </summary>
        /// <param name="data">Raw data.</param>
        /// <returns>Encoded data.</returns>
        byte[] Encode(byte[] data);

        /// <summary>
        /// Decodes the data.
        /// </summary>
        /// <param name="data">Encoded data.</param>
        /// <returns>Decoded data.</returns>
        byte[] Decode(byte[] data);

    }

}