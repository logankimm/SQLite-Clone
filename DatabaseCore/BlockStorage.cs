using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseCore;

public class BlockStorage : IBlockStorage
{
    /// <summary>
    /// Number of bytes of custom data per block that this storage can handle.
    /// </summary>
    readonly Stream stream;
    readonly int blockSize;
    readonly int blockHeaderSize;
    readonly int blockContentSize;
    readonly int unitOfWork;
    readonly Dictionary<uint, Block> blocks = new Dictionary<uint, Block>();

    public int DiskSectorSize
    {
        get
        {
            return unitOfWork;
        }
    }

    public int BlockSize
    {
        get
        {
            return unitOfWork;
        }
    }

    public int BlockHeaderSize
    {
        get
        {
            return blockHeaderSize;
        }
    }

    public int blockContentSize
    {
        get
        {
            return blockContentSize;
        }
    }
    public BlockStorage(Stream storage, int blockSize = 4096, int blockHeaderSize = 48)
    {
        if (storage == null)
            throw new ArgumentNullException("no storage/data stream attached");

        // No difference between this and combining into 1 string
        if (blockHeaderSize >= blockSize)
        {
            throw new ArgumentException("blockHeaderSize cannot be " +
                "larger than or equal " +
                "to " + "blockSize");
        }

        if (blockSize < 128)
        {
            throw new ArgumentException("blockSize too small");
        }

        this.unitOfWork = ((blockSize >= 4096) ? 4096 : 128)
        this.blockSize = blockSize;
        this.blockHeaderSize = blockHeaderSize;
        this.blockContentSize = blockSize - blockHeaderSize;
        this.stream = storage;
    }

    public IBlock Find(uint blockId)
    {
        // Check from already created blocks and return if instance is found
        if (blocks.ContainsKey(blockId) == true)
        {
            return blocks[blockId];
        }

        // Move to the block and check if the block is valid
        var blockPosition = blockId * blockSize;
        // check to make sure that the block exists within the stream (necessary with block creation)
        if ((blockPosition + blockSize) > this.stream.Length)
        {
            return null;
        }

        // Now that you know it exists read through the stream and return the block - stream position will always match blockId? - how does deletion work
        var firstSector = byte[DiskSectorSize];
        stream.Position = blockId * blockSize;
        stream.Read(firstSector, 0, DiskSectorSize);

        // construct new block
        var block = new Block(this, blockId, firstSector, this.stream);
        OnBlockInitialized(block);
        return block;
    }

    public IBlock CreateNew()
    {
        // Create a check for the stream to be out of sync index-wise with the blockSize
        if (this.stream.Length % blockSize != 0)
        {
            throw new DataMisalignedException("Unexpected length of the stream: " + this.stream.Length);
        }

        // Calculate position for new blockId (next available block)
        var blockId = (uint)(this.stream.Length / blockSize);

        // Extend length of underlying stream
        this.stream.SetLength((long)((blockId * blockSize) + blockSize));
        this.stream.Flush();

        // construct new block
        var block = new Block(this, blockId, firstSector, this.stream);
        OnBlockInitialized(block);
        return block;
    }

    // Adds newly created to block to cache (dictionary block) and marks it for disposed memory
    protected virtual void OnBlockInitialized(Block block)
    {
        blocks[block.Id] = block;
        block.Diposed += HandleBlockDisposed;
    }

    protected virtual void HandleBlockDisposed(object sender, EventArgs e)
    {
        var block = (Block)sender;
        block.Disposed -= HandleBlockDisposed;

        blocks.Remove(block.Id);
    }
}