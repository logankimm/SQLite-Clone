using System;

namespace DatabaseCore;

public class Block : IBlock
{
    // What is the purpose of this block here? - can someoen explain please fml - is an array of the bytesectorsize
    readonly byte[] firstSector;
    readonly long?[] cachedHeaderValue = new long?[5];
    readonly Stream stream;
    readonly BlockStorage storage;

    bool isFirstSectionDirty = false;
    bool isDisposed = false;
    readonly uint id;

    public event EventHandler Disposed;

    public uint Id
    {
        get
        {
            return id;
        }
    }

    public Block(BlockStorage storage, uint id, byte[] firstSector, Stream stream)
    {
        // Error handling for bad inputs
        if (stream == null)
            throw new ArgumentNullException("stream");

        if (firstSector == null)
            throw new ArgumentNullException("firstSector");

        if (firstSector.Length != storage.DiskSectorSize)
            throw new ArgumentException("firstSector length must be " + storage.DiskSectorSize);

        this.storage = storage;
        this.id = id
        this.firstSector = firstSector;
        this.stream = stream;
    }

    public long GetHeader(int field)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("Block");
        }


    }

    public void SetHeader(int field, long value)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("Block");
        }

        // Update cache if this field is cached
        if (field < cachedHeaderValue.Length)
        {
            cachedHeaderValue[field] = value;
        }

        // Write in cached buffer
        // Why need to convert value into a long if it's already a long??????????????????????????
        BufferHelper.WriteBuffer((long)value, firstSector, field * 8);
        isFirstSectorDirty = true;

    }
    public void Read()
    {

    }

    public void Dispose()
    {
        Dispose(true);
        GC.suppressFinalize(this);
    }

    protected virtual void Disposed(bool disposing)
    {

    }
}