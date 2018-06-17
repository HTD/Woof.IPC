using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Woof.Ipc {

    /// <summary>
    /// IPC serialization module.
    /// </summary>
    public class Serialization {

        /// <summary>
        /// Serializes serializable boxed object.
        /// </summary>
        /// <param name="data">Serializable boxed object.</param>
        /// <returns>Binary data.</returns>
        public byte[] Serialize(object data) {
            if (data == null) return new byte[] { 0 };
            using (var memoryStream = new MemoryStream()) {
                new BinaryFormatter().Serialize(memoryStream, data);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes serializable object with type.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Data to serialize.</param>
        /// <returns>Binary data.</returns>
        public byte[] Serialize<T>(T data) => Serialize(data);

        /// <summary>
        /// Deserializes binary data to boxed object.
        /// </summary>
        /// <param name="data">Binary data.</param>
        /// <returns>Boxed object.</returns>
        public object Deserialize(byte[] data) {
            if (data == null || data.Length == 1 && data[0] == 0) return null;
            using (var memoryStream = new MemoryStream(data))
                return new BinaryFormatter().Deserialize(memoryStream);
        }

        /// <summary>
        /// Deserializes binary data to specified type.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Binary data.</param>
        /// <returns>Deserialized data.</returns>
        public T Deserialize<T>(byte[] data) => (T)Deserialize(data);

    }

}