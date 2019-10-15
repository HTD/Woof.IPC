using System.IO;
using System.IO.Compression;

namespace Woof.Ipc {

    /// <summary>
    /// IPC compression module.
    /// </summary>
    public sealed class Compression {

        /// <summary>
        /// Compresses data using <see cref="DeflateStream"/>.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <returns>Compressed data.</returns>
        public static byte[] Compress(byte[] data) {
            using (var outputStream = new MemoryStream()) {
                using (var compressionStream = new DeflateStream(outputStream, CompressionLevel.Fastest))
                using (var inputStream = new MemoryStream(data)) inputStream.CopyTo(compressionStream);
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Compressed data using <see cref="DeflateStream"/>.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <returns>Decompressed data.</returns>
        public static byte[] Decompress(byte[] data) {
            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream()) {
                using (var compressionStream = new DeflateStream(inputStream, CompressionMode.Decompress)) compressionStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

    }

}