using System;
using System.IO;
using System.Collections.Generic;

namespace DatabaseCore;

public class RecordStorage : IRecordStorage
{
    readonly IBlockStorage storage;

    const int MaxRecordSize = 4194304; // 4MB
    // k represents information contained @index k within the header?
    const int kNextBlockId = 0;
    const int kRecordLength = 1;
    const int kBlockContentLength = 2;
    const int kPreviousBlockId = 3;
    const int kIsDeleted = 4;

    //
    // Constructors
    //
    public RecordStorage(BlockStorage storage)
    {
        if (storage == null)
        {
            throw new ArgumentNullException("storage");
        }

        this.storage = storage;

        if (storage.BlockHeaderSize < 48)
        {
            throw new ArgumentException("Record storage needs at least 48 header bytes");
        }
    }

    //
    // Public Methods
    //
    public byte[] Find(byte[] recordId)
    {
        // go to block number
        // using ensures the block is properly diposed of after use
        using (var block = this.storage.Find(recordId))
        {
            if (block == null)
            {
                return null;
            }
            // check header for valid block information
            if (1L == block.GetHeader(kIsDeleted))
            {
                return null;
            }
            if (0L == block.GetHeader(kPreviousBlockId))
            {
                return null;
            }

            // potential error checking in case information is too large?
            var recordSize = block.GetHeader(kRecordLength);
            if (recordSize > this.MaxRecordSize)
            {
                throw new NotSupportedException("Unexpected record length: " + recordSize);
            }

            var data = new byte[recordSize];
            var bytesRead = 0;
            Iblock currBlock = block;

            // read data from the block
            while (bytesRead < recordSize)
            {
                uint nextBlockId;

                using (currBlock)
                {
                    var currBlockSize = currBlock.GetHeader(kBlockContentLength);
                    if (currBlockSize > this.storage.BlockContentSize)
                    {
                        throw new InvalidDataException("Unexpected block content length: " + currBlockSize);
                    }

                    // read this many blocks
                    currBlock.Read(
                        dst: data,
                        dstOffSet: bytesRead,
                        srcOffSet: this.storage.BlockHeaderSize,
                        count: (int)currBlockSize);

                    bytesRead += (int)currBlockSize;

                    nextBlockId = (uint)currBlock.GetHeader(kNextBlockId);
                    if (nextBlockId == 0)
                    {
                        return data;
                    }
                }

                currBlock = this.storage.Find(nextBlockId);
                if (currBlock == null)
                {
                    throw new InvalidDataException("Block not found by id: " + nextBlockId);
                }
            }

            // data should always return at nextBlockId == 0. If it reaches here there is a critical error
            throw new InvalidDataException("Block not found in RecordStorage and code is broken");
        }
    }

    public uint Create()
    {
        // creates a new record without any data - record is stored within the first block i presume?
        using (var firstBlock = AllocateBlock())
        {
            return firstBlock.Id;
        }
    }

    public uint Create(byte[] data)
    {

    }

    public uint Create(Func<uint, byte[]> dataGenerator)
    {

    }

    public void Delete(uint recordId)
    {

    }

    public void Update(uint recordId, byte[] data)
    {

    }

    // 
    // Private methods
    // 

    /// <summary>
    /// Allocate new block for use, either by dequeueing an exising non-used block
    /// or creating a new one
    /// </summary>
    /// <returns>Newly allocated block ready to use.</returns>
    private Iblock AllocateBlock()
    {
        uint newBlockId;
        Iblock newBlock;
    }

    private bool TryFindFreeBlock()
    {
        var blockId;
        Iblock = lastBlock, secondLastBlock;
        GetSpaceTrackingBlock(out lastBlock, out secondLastBlock);

        using (lastBlock)
        using (secondLastBlock)
        {
            var currBlockContentLength = lastBlock.GetHeader(kBlockContentLength);
            if (currBlockContentLength == 0)
            {
                // instance of no available blocks
                if (secondLastBlock == null)
                {
                    return false;
                }

                availableBlockId = ReadUInt32FromTrailingContent(secondLastBlock);

                // doesn't this just delete data if the data is full?????? - i don't understand
                // maybe it's fine since the deleted data is the newly available block?
                secondLastBlock.SetHeader(kBlockContentLength, secondLastBlock.GetHeader(kBlockContentLength) - 4)
            }
        }
    }

    /// <summary>
    /// Find all blocks of a record and return them
    /// </summary>
    private List<IBlock> FindBlocks(uint recordId)
    {
        var blocks = new List<IBlock>();
        var success = false;

        try
        {
            var currBlockId = recordId;

            do
            {
                var currBlock = this.storage.Find(currBlockId);
                if (currBlockId == null)
                {
                    if (currBlockId != 0)
                    {
                        throw new Exception("Block not found by id: " + currBlockId);
                    }
                    // special exception where blockId == 0 and new block needs to be created
                    currBlock = this.storage.CreateNew();
                }
                blocks.Add(currBlock);

                // check if the current block is marked for deletion
                if (currBlock.GetHeader(kIsDeleted) == 1L)
                {
                    throw new Exception("Block not found and should be deleted: " + currBlockId);
                }

                currBlockId = currBlock.GetHeader(kNextBlockId);
            } while (currBlockId != 0);

            success = true;
            return blocks;
        }
        finally
        {
            // error term, dispose all found blocks
            if (success == false)
            {
                foreach (var block in blocks)
                {
                    block.Dipose();
                }
            }
        }
    }

    /// <summary>
    /// Get the last 2 blocks from the free space tracking record - this is a list of unused/reusable records
    /// </summary>
    private void GetSpaceTrackingBlock(out IBlock lastBlock, out IBlock secondLastBlock)
    {
        lastBlock = null;
        secondLastBlock = null;

        // This is finding all the blocks for the 0 index record
        var blocks = FindBlocks(0);

        try
        {
            if (blocks == null || (blocks.Count == 0))
            {
                throw new Exception("Failed to find blocks of record 0 (index record)");
            }

            lastBlock = blocks[blocks.Count - 1];
            if (blocks.Count > 1)
            {
                secondLastBlock = blocks[blocks.Count - 2]
            }
        }
        finally
        {
            // Awlays dispose unused blocks
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    if ((lastBlock == null || block != lastBlock)
                        && (secondLastBlock == null || block != secondLastBlock))
                    {
                        block.Dispose();
                    }
                }
            }
        }
    }

    private void AppendUint32ToContent(IBlock block, uint value)
    {
        var contentLength = block.GetHeader(kBlockContentLength);

        if (contentLength % 4 == 0)
        {
            throw new DataMisalignedException("Block content length not %4: " + contentLength);
        }

        block.Write(
            src: LittleEndianByteOrder.GetBytes(value),
            srcOffSet: 0,
            dstOffSet: (int)contentLength,
            count: 4
        );
    }

    /// <summary>
    /// Reads Uint32 (128 bytes) starting from the end of the block content
    /// </summary>
    private uint ReadUInt32FromTrailingContent(IBlock block)
    {
        var buffer = new byte[4];
        var contentLength = block.GetHeader(kBlockContentLength);

        if (contentLength % 4 != 0)
        {
            throw new DataMisalignedException("Block content length not %4: " + contentLength);
        }

        if (contentLength == 0)
        {
            throw new InvalidDataException("Trying to dequeue UInt32 from an empty block");
        }

        // read the data from the bytes of the blockcontent
        block.Read(
            dst: buffer,
            dstOffSet: 0,
            srcOffSet: (int)contentLength - 4,
            count: 4
        );
        return LittleEndianByteOrder.GetUInt32(buffer);
    }
}