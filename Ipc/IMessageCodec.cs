﻿namespace Woof.Ipc {

    /// <summary>
    /// An interface for a message codec.
    /// </summary>
    public interface IMessageCodec {
        
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

        /// <summary>
        /// Encodes or decodes the data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="decode">Default: false - encode, true - decode.</param>
        /// <returns>Processed data.</returns>
        byte[] Apply(byte[] data, bool decode = false);

        /// <summary>
        /// Encodes or decodes the data.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="decode">Default: false - encode, true - decode.</param>
        void Apply(ref byte[] data, bool decode = false);

    }

}