namespace Woof.Ipc {
    
    /// <summary>
    /// An interface for a message encryption codec.
    /// </summary>
    interface IMessageEncryption : IMessageCodec {

        /// <summary>
        /// Gets the required key length in bytes.
        /// </summary>
        int KeyLength { get; }

        /// <summary>
        /// Gets the current key.
        /// </summary>
        /// <returns>Current key.</returns>
        byte[] GetKey();

        /// <summary>
        /// Sets the current key.
        /// </summary>
        /// <param name="key">New key data.</param>
        void SetKey(byte[] key);

    }

}