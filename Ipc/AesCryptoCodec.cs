using System;
using System.Security.Cryptography;

namespace Woof.Ipc {

    /// <summary>
    /// AES symetric encryption codec.
    /// </summary>
    public sealed class AesCryptoCodec : MessageCodecBase, IMessageEncryption, IDisposable {
        
        /// <summary>
        /// Gets the required key length in bytes.
        /// </summary>
        public int KeyLength { get; }

        /// <summary>
        /// Creates a new instance of the codec with pre-generated key and initialization vector.
        /// </summary>
        public AesCryptoCodec() {
            Cipher = Aes.Create();
            KeyLength = Cipher.Key.Length;
            IvLength = Cipher.IV.Length;
            Cipher.Mode = CipherMode.CBC;
            Cipher.Padding = PaddingMode.ISO10126;
        }

        /// <summary>
        /// Creates a new instance of the codec with specified key and pre-generated initialization vector.
        /// </summary>
        /// <param name="key">Key data.</param>
        public AesCryptoCodec(byte[] key) : this() => Cipher.Key = key;

        /// <summary>
        /// Encodes the data using AES with IV first.
        /// </summary>
        /// <param name="data">Data to encode.</param>
        /// <returns>IV + data.</returns>
        public override byte[] Encode(byte[] data) {
            using (var encryptor = Cipher.CreateEncryptor()) {
                var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                var pack = new byte[encrypted.Length + IvLength];
                Buffer.BlockCopy(Cipher.IV, 0, pack, 0, IvLength);
                Buffer.BlockCopy(encrypted, 0, pack, IvLength, encrypted.Length);
                Cipher.GenerateIV();
                return pack;
            }
            
        }

        /// <summary>
        /// Decodes the data using AES with IV first.
        /// </summary>
        /// <param name="data">IV + encoded data.</param>
        /// <returns>Decoded data.</returns>
        public override byte[] Decode(byte[] data) {
            var iv = new byte[IvLength];
            Buffer.BlockCopy(data, 0, iv, 0, IvLength);
            Cipher.IV = iv;
            using (var decryptor = Cipher.CreateDecryptor()) {
                var decrypted = decryptor.TransformFinalBlock(data, IvLength, data.Length - IvLength);
                return decrypted;
            }
        }

        /// <summary>
        /// Gets the current AES key.
        /// </summary>
        /// <returns>Current AES key.</returns>
        public byte[] GetKey() => Cipher.Key;
        
        /// <summary>
        /// Sets the current AES key.
        /// </summary>
        /// <param name="key">New key data.</param>
        public void SetKey(byte[] key) => Cipher.Key = key;
        
        /// <summary>
        /// Disposes resources taken by <see cref="Cipher"/>.
        /// </summary>
        public void Dispose() => Cipher.Dispose();

        /// <summary>
        /// Internal cipher algorithm.
        /// </summary>
        private readonly Aes Cipher;

        /// <summary>
        /// The length (in bytes) of the initialization vector.
        /// </summary>
        private readonly int IvLength;

    }

}