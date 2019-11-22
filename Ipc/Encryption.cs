using System;
using System.Security.Cryptography;

namespace Woof.Ipc {

    /// <summary>
    /// IPC encryption module.
    /// </summary>
    public sealed class Encryption {

        #region Key data

        /// <summary>
        /// Gets or sets the secret key to use for the symmetric algorithm.
        /// </summary>
        private byte[] Key;

        /// <summary>
        /// Gets or sets the initialization vector to use for the symmetric algorithm.
        /// </summary>
        private byte[] IV;

        /// <summary>
        /// Gets or sets both <see cref="Key"/> and <see cref="IV"/> from merged binary buffer.
        /// </summary>
        public byte[] GetKeyData() {
            var pack = new byte[48];
            Buffer.BlockCopy(Key, 0, pack, 0, 32);
            Buffer.BlockCopy(IV, 0, pack, 32, 16);
            return pack;
        }

        /// <summary>
        /// Gets or sets both <see cref="Key"/> and <see cref="IV"/> from merged binary buffer.
        /// </summary>
        public void SetKeyData(byte[] value) {
            if (value != null) {
                IV = new byte[16];
                Key = new byte[32];
                Buffer.BlockCopy(value, 0, Key, 0, 32);
                Buffer.BlockCopy(value, 32, IV, 0, 16);
            }
            else {
                using (var aes = Aes.Create()) {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    Key = aes.Key;
                    IV = aes.IV;
                }
            }
        }

        #endregion

        /// <summary>
        /// Initializes AES IPC encryption with PRNG data.
        /// </summary>
        public Encryption() => SetKeyData(null); // initialization with random Key and IV

        /// <summary>
        /// Initializes AES IPC encryption with received data.
        /// </summary>
        /// <param name="keyData">48 bytes.</param>
        public Encryption(byte[] keyData) => SetKeyData(keyData);
        /// <summary>
        /// Encrypts binary message for IPC.
        /// </summary>
        /// <param name="message">Raw bytes to encrypt.</param>
        /// <returns>Encrypted bytes.</returns>
        public byte[] Encrypt(byte[] message) {
            using (var aes = Aes.Create()) {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (var encryptor = aes.CreateEncryptor(Key, IV)) return encryptor.TransformFinalBlock(message, 0, message.Length);
            }
        }

        /// <summary>
        /// Decrypts binary message for IPC
        /// </summary>
        /// <param name="message">Encrypted bytes.</param>
        /// <returns>Decrypted bytes.</returns>
        public byte[] Decrypt(byte[] message) {
            using (var aes = Aes.Create()) {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor(Key, IV)) return decryptor.TransformFinalBlock(message, 0, message.Length);
            }
        }

    }

}