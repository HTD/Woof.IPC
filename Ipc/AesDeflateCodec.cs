using System;

namespace Woof.Ipc {

    /// <summary>
    /// Encryption and compression in one codec.
    /// </summary>
    public sealed class AesDeflateCodec : IMessageEncryption, IMessageCompression, IDisposable {

        /// <summary>
        /// Gets the required key length in bytes.
        /// </summary>
        public int KeyLength => CryptoCodec.KeyLength;

        /// <summary>
        /// Creates a new instance of the codec with pre-generated key and initialization vector.
        /// </summary>
        public AesDeflateCodec() {
            CryptoCodec = new AesCryptoCodec();
            CompressionCodec = new DeflateCodec();
        }

        /// <summary>
        /// Creates a new instance of the codec with specified key and pre-generated initialization vector.
        /// </summary>
        /// <param name="key"></param>
        public AesDeflateCodec(byte[] key) {
            CryptoCodec = new AesCryptoCodec(key);
            CompressionCodec = new DeflateCodec();
        }

        /// <summary>
        /// Encodes the data.
        /// </summary>
        /// <param name="data">Raw data.</param>
        /// <returns>Encoded data.</returns>
        public byte[] Encode(byte[] data) => CryptoCodec.Encode(CompressionCodec.Encode(data));
        
        /// <summary>
        /// Decodes the data.
        /// </summary>
        /// <param name="data">Encoded data.</param>
        /// <returns>Decoded data.</returns>
        public byte[] Decode(byte[] data) => CompressionCodec.Decode(CryptoCodec.Decode(data));

        /// <summary>
        /// Sets the key data.
        /// </summary>
        /// <param name="key">New key data.</param>
        public void SetKey(byte[] key) => CryptoCodec.SetKey(key);
        
        /// <summary>
        /// Gets the key data.
        /// </summary>
        /// <returns>Current key data.</returns>
        public byte[] GetKey() => CryptoCodec.GetKey();
        
        /// <summary>
        /// Disposes the <see cref="AesCryptoCodec"/> instance.
        /// </summary>
        public void Dispose() => (CryptoCodec as AesCryptoCodec).Dispose();

        /// <summary>
        /// Crypto codec.
        /// </summary>
        private readonly IMessageEncryption CryptoCodec;

        /// <summary>
        /// Compression codec.
        /// </summary>
        private readonly IMessageCompression CompressionCodec;
        
    }

}