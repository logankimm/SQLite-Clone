using System;
using System.Diagnostics;
using System.IO;

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
        if (field >= (this.storage.BlockHeaderSize / 8))
        {
            throw new ArgumentException("Invalid field: " + field);
        }

        // Check if stored in cached already else return the long - cache in on a first-come first serve basis?
        // Caches are unique to each block instance and not shared - which means everything is always cached probably
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
        checkDisposed();

        if (field < 0)
        {
            throw new IndexOutOfRangeException();
        }
        // Update cache if this field is cached
        if (field < cachedHeaderValue.Length)
        {
            cachedHeaderValue[field] = value;
        }

        // firstSector is the location of the data within the stream, and field is multiplied by 8 since each field size is 8 bytes
        BufferHelper.WriteBuffer((long)value, firstSector, field * 8);
        isFirstSectorDirty = true;
    }
    public void Read(byte[] dest, int destOffSet, int srcOffSet, int count)
    {
        checkDisposed();

        // Make sure the count is in bounds of the block content size and destination
        if ((count < 0) || ((count + srcOffSet) > this.storage.blockContentSize))
        {
            throw new ArgumentOutOfRangeException("Requested count is outside of src bounds: Count=" + count, "count");
        }
        if ((count + destOffset) > dest.Length)
        {
            throw new ArgumentOutOfRangeException("Requested count is outside of dest bounds: Count=" + count);
        }

        // dataCopied = index of bytes that have been read?
        var dataIndexRead = 0;

        var bool copyableFromFirstSector = (this.storage.BlockHeaderSize + srcOffSet < this.storage.DiskSectorSize);
        // Read available data from the cached memory first
        if (copyableFromFirstSector)
        {
            // First part of the min is just making sure that its within boundaries of the firstSector
            var numCacheBytesToRead = Math.min(this.storage.DiskSectorSize - this.storage.BlockHeaderSize - srcOffSet, count);
            Buffer.BlockCopy(
                src: this.firstSector,
                srcOffSet: this.storage.BlockHeaderSize + srcOffSet,
                dst: dest,
                destOffSet: destOffSet,
                count: numCacheBytesToRead
            );

            dataIndexRead += numCacheBytesToRead;
        }

        // Check if there's data to be copied still
        if (dataIndexRead < count)
        {
            // then update the position within the stream to read the rest of the data depending on whether or not it copied from firstSector
            if (copyableFromFirstSector)
            {
                this.stream.Position = (this.id * this.storage.BlockSize) + this.storage.DiskSectorSize;
            }
            else
            {
                this.stream.Position = (this.id * this.storage.BlockSize) + this.storage.BlockStorage + srcOffSet;
            }
        }

        while (dataIndexRead < count)
        {
            var numBytesToRead = Math.min(this.storage.DiskSectorSize, count - dataIndexRead);
            this.stream.Read(
                dst: dest,
                destOffSet: destOffSet + dataIndexRead,
                count: numBytesToRead
            );
            // Error statement that only triggers if the stream index being read doesn't exist
            if (thisRead == 0)
            {
                throw new EndOfStreamException();
            }
            dataIndexRead += numCacheBytesToRead;
        }
    }

    public void Write(byte[] src, int srcOffSet, int destOffSet, int count)
    {
        checkDisposed();

        // make sure count and destination are still within bounds
        if (srcOffSet < 0 || srcOffSet + count >= src.Length)
        {
            throw new ArgumentOutOfRangeException("Requested count is outside of src bounds: Count=" + count, "count");
        }
        // Makes sure incoming data is always less than BlockSize
        if (destOffSet < 0 || (destOffSet + count > this.storage.BlockContentSize))
        {

            throw new ArgumentOutOfRangeException("Count argument is outside of dest bounds: Count=" + count, "count");
        }

        // check if it can be written into the first sector
        if (this.storage.BlockHeaderSize + destOffSet, this.storage.DiskSectorSize)
        {
            var sectorBytesWritten = Math.min(this.storage.DiskSectorSize - this.storage.BlockHeaderSize - destOffSet, count);
            Buffer.BlockCopy(
                src: src,
                srcOffSet: srcOffSet,
                dst: this.firstSector,
                destOffSet: this.storage.BlockHeaderSize + destOffSet,
                count: sectorBytesWritten
            );

            // if the count is reached after writing to firstSector, then there's no reason to trigger next if statement
            // therefore, offSet/current position can just be set to DiskSectorSize
            destOffSet += sectorBytesWritten;
            srcOffSet += sectorBytesWritten;
            count -= sectorBytesWritten;
            isFirstSectionDirty = true;
        }

        // write the rest of the data to post-blocksize - but what if the block overfills??????????????????
        if (this.storage.BlockHeaderSize + destOffSet + count > this.storage.DiskSectorSize)
        {
            // max is necessary for when writing to a position in the block that is out of DiskSectorSize. E.g. DiskSectorSize = 4096, destOffSet = 5670
            this.stream.Position = (Id * this.storage.BlockSize) + Math.max(this.storage.DiskSectorSize, this.storage.BlockHeaderSize + destOffSet);

            // Write remaining rest of bytes
            var bytesWritten = 0;
            while (bytesWritten < count)
            {
                var bytesToWrite = Math.min(4096, count - bytesWritten)
                this.stream.Write(
                    src: src,
                    srcOffSet: srcOffSet + bytesWritten,
                    count: bytesToWrite
                );
                this.stream.Flush();

                bytesWritten += bytesToWrite;
            }
        }
    }

    public override string ToString()
    {
        return string.Format("[Block: Id={0}, ContentLength={1}, Prev={2}, Next={3}]"
            , Id
            , GetHeader(2)
            , GetHeader(3)
            , GetHeader(0));
    }

    private void checkDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("Block");
        }
    }

    //
    // Protected Methods
    //

    protected virtual void OnDisposed(EventArgs e)
    {
        if (Disposed != null)
        {
            Disposed(this, e);
        }
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