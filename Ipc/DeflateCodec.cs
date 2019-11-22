using System.IO;
using System.IO.Compression;

namespace Woof.Ipc {

    /// <summary>
    /// Message compression codec using <see cref="DeflateStream"/>.
    /// </summary>
    public sealed class DeflateCodec : MessageCodecBase {

        /// <summary>
        /// Compresses the data.
        /// </summary>
        /// <param name="data">Raw data.</param>
        /// <returns>Compressed data.</returns>
        public override byte[] Encode(byte[] data) {
            using (var outputStream = new MemoryStream()) {
                using (var compressionStream = new DeflateStream(outputStream, CompressionLevel.Fastest))
                using (var inputStream = new MemoryStream(data)) inputStream.CopyTo(compressionStream);
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the data.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <returns>Decompressed data.</returns>
        public override byte[] Decode(byte[] data) {
            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream()) {
                using (var compressionStream = new DeflateStream(inputStream, CompressionMode.Decompress)) compressionStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

    }

}