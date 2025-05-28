using System;

namespace DatabaseCore;

/// <summary>
/// Helper class contains static methods to read and write
/// numeric data in little endian byte order
/// </summary>
public static class LittleEndianByteOrder
{
    public static byte[] GetBytes(uint value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian == false)
        {
            Array.Reverse(bytes);
        }

        return bytes;

    }
    public static uint GetUint32(byte[] bytes)
    {
        // default assumption is bytes = little endian, if not reverse it
        if (BitConverter.IsLittleEndian == false)
        {
            var byteCopy = new byte[bytes.Length];
            bytes.CopyTo(byteCopy, 0);
            Array.Reverse(byteCopy);
            return BitConverter.ToUint32(byteCopy, 0);
        }

        return BitConverter.ToUint32(bytes, 0);
    }
}