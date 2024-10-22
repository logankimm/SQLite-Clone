using System;
namespace DatabaseCore;

public interface IBlock : IDisposable
{
    // Unsigned integer (only stores positives not negatives) that is unique
    uint Id { get; }
    long GetHeader(int field);
    void SetHeader(int field, long newHeader);
    void Read(byte[] dst, int dstOffSet, int srcOffSet, int count);

    /// <summary>
    /// Write content of given buffer (src) into this (dst)
    /// </summary>
    void Write(byte[] src, int srcOffset, int dstOffset, int count);
}
