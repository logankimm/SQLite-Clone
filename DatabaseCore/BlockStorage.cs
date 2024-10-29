using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseCore;

public class BlockStorage : IBlockStorage
{
    // Is stream/storage teh place where all blocks live?
    readonly Stream stream;
    readonly int blockSize;
    readonly int blockHeaderSize;
    readonly int blockContentSize { get; };
    readonly int unitOfWork;
    readonly Dictionary<uint, Block> blocks = new Dictionary<uint, Block>();

    // Not sure what the purpose of this property is?
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
            throw new ArgumentNullException("no storage attached");

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
        // Check from already created blocks within memo (blocks)
        if (blocks.ContainsKey(blockId) == true)
        {
            return blocks[blockId];
        }

        // This part should never be triggered ever then?
        // Otherwise look inside the stream to find where the block fits into place
        var blockPosition = blockId * blockSize;
        // check to make sure that the block exists within the stream - check if the data would exist
        if ((blockPosition + blockSize) > this.stream.Length)
        {
            return null;
        }

        // Now that you know it exists read through the stream and return the block?
        var firstSector = byte[DiskSectorSize];
        stream.Position = blockId * blockSize;
        stream.Read(firstSector, 0, DiskSectorSize);

        // construct new block
        var block = new Block(this, blockId, firstSector, this.stream);
        OnBlockInitialized(block); //??????
        return block;
    }

    public IBlock CreateNew()
    {

    }

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