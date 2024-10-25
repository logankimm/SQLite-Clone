using System;

namespace DatabaseCore;

public static class BufferHelper
{
    public static Guid ReadBufferGuid(byte[] buffer, int bufferOffset)
    {
        var guidBuffer = new byte[16];
        Buffer.BlockCopy(buffer, bufferOffset, guidBuffer, 0, 16);
        return new Guid(guidBuffer);
    }
    public static uint ReadBufferUInt32(byte[] buffer, int bufferOffset)
    {
        var uintBuffer = new byte[4];
        Buffer.BlockCopy(buffer, bufferOffset, uintBuffer, 0, 4);
        return LittleEndianByteOrder.GetUInt32(uintBuffer);
    }

    public static int ReadBufferInt32(byte[] buffer, int bufferOffset)
    {
        var intBuffer = new byte[4];
        Buffer.BlockCopy(buffer, bufferOffset, intBuffer, 0, 4);
        return LittleEndianByteOrder.GetInt32(intBuffer);
    }

    public static void WriteBuffer(long value, byte[] buffer, int bufferOffset)
    {
        Buffer.BlockCopy(LittleEndianByteOrder.GetBytes(value), 0, buffer, bufferOffset, 8);
    }
}