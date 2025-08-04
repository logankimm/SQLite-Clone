using System;

namespace DatabaseCore;

/// <summary>
/// Helper class contains static methods to read and write
/// numeric data in little endian byte order
/// </summary>
public static class LittleEndianByteOrder
{
    public static byte[] GetBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian == false)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public static byte[] GetBytes(long value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian == false)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public static byte[] GetBytes(uint value)
    {
        var bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian == false)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public static long GetInt64(byte[] bytes)
    {
        // Given bytes are little endian
        // If this computer is big endian then result need to be reversed
        if (BitConverter.IsLittleEndian == false)
        {
            var bytesClone = new byte[bytes.Length];
            bytes.CopyTo(bytesClone, 0);
            Array.Reverse(bytesClone);
            return BitConverter.ToInt64(bytesClone, 0);
        }
        else
        {
            return BitConverter.ToInt64(bytes, 0);
        }
    }

    public static int GetInt32(byte[] bytes)
    {
        // Given bytes are little endian
        // If this computer is big endian then result need to be reversed
        if (BitConverter.IsLittleEndian == false)
        {
            var bytesClone = new byte[bytes.Length];
            bytes.CopyTo(bytesClone, 0);
            Array.Reverse(bytesClone);
            return BitConverter.ToInt32(bytesClone, 0);
        }
        else
        {
            return BitConverter.ToInt32(bytes, 0);
        }
    }

    public static uint GetUInt32(byte[] bytes)
    {
        // default assumption is bytes = little endian, if not reverse it
        if (BitConverter.IsLittleEndian == false)
        {
            var byteCopy = new byte[bytes.Length];
            bytes.CopyTo(byteCopy, 0);
            Array.Reverse(byteCopy);
            return BitConverter.ToUInt32(byteCopy, 0);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }
}