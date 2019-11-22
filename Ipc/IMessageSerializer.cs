namespace Woof.Ipc {
    
    /// <summary>
    /// Message serializer.
    /// </summary>
    public interface IMessageSerializer {

        /// <summary>
        /// Deserializes binary data to boxed object.
        /// </summary>
        /// <param name="data">Binary data.</param>
        /// <returns>Boxed object.</returns>
        object Deserialize(byte[] data);

        /// <summary>
        /// Deserializes binary data to specified type.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Binary data.</param>
        /// <returns>Deserialized data.</returns>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// Serializes serializable boxed object.
        /// </summary>
        /// <param name="data">Serializable boxed object.</param>
        /// <returns>Binary data.</returns>
        byte[] Serialize(object data);

        /// <summary>
        /// Serializes serializable object with type.
        /// </summary>
        /// <typeparam name="T">Serializable type.</typeparam>
        /// <param name="data">Data to serialize.</param>
        /// <returns>Binary data.</returns>
        byte[] Serialize<T>(T data);

    }

}