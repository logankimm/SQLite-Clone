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

    // Blockstorage - original container for the blocks I assume
    // id - unique id to be referenced while creating the block
    // first sector - byte array containing the header metadata data?
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
        // Validate field number
        if (field < 0)
        {
            throw new IndexOutOfRangeException();
        }
        if (field >= (storage.BlockHeaderSize / 8))
        {
            throw new ArgumentException("Invalid field: " + field);
        }

        // Check if stored in cached already else return the long
        if (field < cachedHeaderValue.Length)
        {
            if (cachedHeaderValue[field] == null)
            {
                cachedHeaderValue[field] = BufferHelper.ReadBufferInt64(firstSector, field * 8);
            }
            return (long)cachedHeaderValue[field];
        }
        else
        {
            return BufferHelper.ReadBufferInt64(firstSector, field * 8)
        }
    }

    // I imagine field is the index at which the length of the header starts and its a constant length
    public void SetHeader(int field, long value)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("Block");
        }
        if (field < 0)
        {
            throw new IndexOutOfRangeException();
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
    public void Read(byte[] dest, int destOffSet, int srcOffSet, int count)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("Block");
        }

        // Make sure the count is in bounds of the block content size and is valid
        if ((count <= 0) || ((count + srcOffSet) >= storage.blockContentSize))
        {
            throw new ArgumentOutOfRangeException("Requested count is outside of src bounds: Count=" + count, "count");
        }
        if ((count + destOffset) >= dest.Length)
        {
            throw new ArgumentOutOfRangeException("Requested count is outside of dest bounds: Count=" + count);
        }

        var dataCopied = 0;
        var copyFromFirstSector = 
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