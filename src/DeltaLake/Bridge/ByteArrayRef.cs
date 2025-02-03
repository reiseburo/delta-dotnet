using System;
using System.Buffers;
using System.Text;

namespace DeltaLake.Bridge
{
    /// <summary>
    /// Representation of a byte array owned by .NET. Users should usually use a
    /// </summary>
    internal sealed class ByteArrayRef : IDisposable
    {
        private readonly MemoryHandle bytesHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayRef"/> class.
        /// </summary>
        /// <param name="bytes">Byte array to use.</param>
        public ByteArrayRef(byte[] bytes)
            : this(bytes, bytes.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayRef"/> class.
        /// </summary>
        /// <param name="bytes">Byte array to use.</param>
        /// <param name="length">Amount of bytes to use.</param>
        public ByteArrayRef(byte[] bytes, int length)
            : this(new Memory<byte>(bytes, 0, length))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayRef"/> class.
        /// </summary>
        /// <param name="bytes">Byte array to use.</param>
        public ByteArrayRef(Memory<byte> bytes)
        {
            Bytes = bytes;
            bytesHandle = bytes.Pin();
            unsafe
            {
                Ref = new Interop.ByteArrayRef()
                {
                    data = (byte*)bytesHandle.Pointer,
                    size = (UIntPtr)bytes.Length,
                };
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            bytesHandle.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets empty byte array.
        /// </summary>
        public static ByteArrayRef Empty { get; } = new(Array.Empty<byte>());

        /// <summary>
        /// Gets current byte array for this ref.
        /// </summary>
        public Memory<byte> Bytes { get; private init; }

        /// <summary>
        /// Gets internal ref.
        /// </summary>
        public Interop.ByteArrayRef Ref { get; private init; }

        /// <summary>
        /// Gets strict UTF-8 encoding.
        /// </summary>
        internal static UTF8Encoding StrictUTF8 { get; } = new(false, true);

        /// <summary>
        /// Convert a string to a UTF-8 byte array.
        /// </summary>
        /// <param name="s">String to convert.</param>
        /// <returns>Converted byte array.</returns>
        public static ByteArrayRef FromUTF8(string s)
        {
            if (s.Length == 0)
            {
                return Empty;
            }

            return new ByteArrayRef(StrictUTF8.GetBytes(s));
        }

        /// <summary>
        /// Convert a string to a UTF-8 byte array.
        /// </summary>
        /// <param name="s">String to convert.</param>
        /// <returns>Converted byte array.</returns>
        public static RentedByteArrayRef RentUtf8(string s)
        {
            if (s.Length == 0)
            {

                return RentedByteArrayRef.Empty;
            }

            var bytes = ArrayPool<byte>.Shared.Rent(StrictUTF8.GetByteCount(s));
#if NETCOREAPP
            var length = StrictUTF8.GetBytes(s, bytes);
#else
            var length = StrictUTF8.GetBytes(s, 0, s.Length, bytes, 0);
#endif
            return new RentedByteArrayRef(new ByteArrayRef(bytes.AsMemory(0, length)), bytes, ArrayPool<byte>.Shared);
        }

        /// <summary>
        /// Copy a byte array ref contents to a UTF8 string.
        /// </summary>
        /// <param name="byteArray">Byte array ref.</param>
        /// <returns>String.</returns>
        public static unsafe string ToUtf8(Interop.ByteArrayRef byteArray) =>
            StrictUTF8.GetString(byteArray.data, (int)byteArray.size);
    }
}