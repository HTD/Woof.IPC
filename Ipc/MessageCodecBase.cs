namespace Woof.Ipc {

    /// <summary>
    /// Defines basic message codec methods.
    /// </summary>
    public abstract class MessageCodecBase : IMessageCodec {

        /// <summary>
        /// Encodes the data.
        /// </summary>
        /// <param name="data">Raw data.</param>
        /// <returns>Encoded data.</returns>
        public abstract byte[] Encode(byte[] data);

        /// <summary>
        /// Decodes the data.
        /// </summary>
        /// <param name="data">Encoded data.</param>
        /// <returns>Decoded data.</returns>
        public abstract byte[] Decode(byte[] data);

        /// <summary>
        /// Encodes or decodes the data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="decode">Default: false - encode, true - decode.</param>
        /// <returns>Processed data.</returns>
        public byte[] Apply(byte[] data, bool decode = false) => decode ? Decode(data) : Encode(data);

        /// <summary>
        /// Encodes or decodes the data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="decode">Default: false - encode, true - decode.</param>
        public void Apply(ref byte[] data, bool decode = false) => data = decode ? Decode(data) : Encode(data);

    }

}