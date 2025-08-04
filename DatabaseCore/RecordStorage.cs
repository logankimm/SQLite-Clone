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
    public virtual byte[] Find(uint recordId)
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
            if (recordSize > MaxRecordSize)
            {
                throw new NotSupportedException("Unexpected record length: " + recordSize);
            }

            var data = new byte[recordSize];
            var bytesRead = 0;

            IBlock currBlock = block;
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
                        dest: data,
                        dstOffset: bytesRead,
                        srcOffset: this.storage.BlockHeaderSize,
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

    public virtual uint Create()
    {
        // creates a new record without any data - record is stored within the first block i presume?
        using (var firstBlock = this.AllocateBlock())
        {
            return firstBlock.Id;
        }
    }

    public virtual uint Create(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentException();
        }

        // recordId is the id from the Create(generator), 
        return Create(recordId => data);
    }

    public virtual uint Create(Func<uint, byte[]> dataGenerator)
    {
        if (dataGenerator == null)
        {
            throw new ArgumentException("Data Generatr is null");
        }

        using (var firstBlock = AllocateBlock())
        {
            var returnId = firstBlock.Id;

            var data = dataGenerator(returnId);
            var dataWritten = 0;
            var dataToWrite = data.Length;

            // wouldn't this be the maximum (blockSize, dataToWrite)?
            firstBlock.SetHeader(kBlockContentLength, dataToWrite);

            // check if there is no data to be written
            if (dataToWrite == 0)
            {
                return returnId;
            }

            // continue writing the rest of the data
            IBlock currBlock = firstBlock;
            while (dataWritten < dataToWrite)
            {
                // assigned outside of using so that it isn't disposed of 
                IBlock nextBlock = null;

                using (currBlock)
                {
                    var writeLength = (int)Math.Min(this.storage.BlockContentSize, dataToWrite - dataWritten);
                    currBlock.Write(
                        src: data,
                        srcOffset: dataWritten,
                        dstOffset: 0,
                        count: writeLength
                    );
                    currBlock.SetHeader(kBlockContentLength, writeLength);
                    dataWritten += writeLength;

                    // move to next block to continue reading
                    if (dataWritten < dataToWrite)
                    {
                        nextBlock = AllocateBlock();

                        // error checking
                        var success = false;
                        try
                        {
                            nextBlock.SetHeader(kPreviousBlockId, currBlock.Id);
                            currBlock.SetHeader(kNextBlockId, nextBlock.Id);
                            success = true;
                        }
                        finally
                        {
                            if ((success == false) && (nextBlock != null))
                            {
                                nextBlock.Dispose();
                                nextBlock = null;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }

                }

                if (nextBlock != null)
                {
                    currBlock = nextBlock;
                }
            }

            return returnId;
        }
    }

    public virtual void Delete(uint recordId)
    {
        using (var block = this.storage.Find(recordId))
        {
            IBlock currBlock = block;
            while (true)
            {
                IBlock nextBlock = null;

                using (currBlock)
                {
                    // delete the current block,
                    this.MarkAsFree(currBlock.Id);

                    // rewrite next block length and delete data
                    currBlock.SetHeader(kIsDeleted, 1L);
                    var nextBlockId = (uint)currBlock.GetHeader(kNextBlockId);

                    if (nextBlockId == 0)
                    {
                        break;
                    }

                    nextBlock = this.storage.Find(nextBlockId);
                    // what reason could there be for this if statement i don't understand
                    // wouldn't this check be needed to be done earlier to be effective?
                    // og code has this as currBlock but im p sure this is supposed to be nextBlock
                    if (currBlock == null || nextBlock == null)
                    {
                        throw new InvalidDataException("Block not found by id: " + nextBlockId);
                    }
                }

                // check if there's a next block, if there is, move current block to the next block
                if (nextBlock != null)
                {
                    currBlock = nextBlock;
                }
            }
        }
    }

    public virtual void Update(uint recordId, byte[] data)
    {
        var dataWritten = 0;
        var total = data.Length;
        var blocks = this.FindBlocks(recordId);
        var blocksParsed = 0;
        IBlock prevBlock = null;

        try
        {
            while (dataWritten < total)
            {
                var writeLength = Math.Min(total - dataWritten, this.storage.BlockContentSize);
                // i don't think casting as double is necessary and only needs to be an int im p sure
                var blockIndex = (int)Math.Floor((double)dataWritten / (double)this.storage.BlockContentSize);

                // find the write block to write to
                IBlock currBlock = null;
                if (blockIndex < blocks.Count)
                {
                    currBlock = blocks[blockIndex];
                }
                // if block is out of bounds then create a new block and add to List
                else
                {
                    currBlock = this.AllocateBlock();
                    if (currBlock == null)
                    {
                        throw new Exception("Failed to allocate new block");
                    }
                    blocks.Add(currBlock);
                }

                if (prevBlock != null)
                {
                    currBlock.SetHeader(kPreviousBlockId, prevBlock.Id);
                    prevBlock.SetHeader(kNextBlockId, currBlock.Id);
                }

                // write to the block
                currBlock.Write(
                    src: data,
                    srcOffset: dataWritten,
                    dstOffset: 0,
                    count: writeLength
                );
                currBlock.SetHeader(kBlockContentLength, writeLength);
                // this is necessary in case the update is a reduction in data so there is potentially no next block needed
                currBlock.SetHeader(kNextBlockId, 0);
                if (dataWritten == 0)
                {
                    currBlock.SetHeader(kRecordLength, total);
                }

                blocksParsed++;
                dataWritten += writeLength;
                prevBlock = currBlock;
            }

            // Scenario: if update reduces blocks length delete excess blocks that are no longer used
            if (blocksParsed < blocks.Count)
            {
                for (int i = blocksParsed; i < blocks.Count; i++)
                {
                    this.MarkAsFree(blocks[i].Id);
                }
            }
        }
        // memory disposal
        finally
        {
            foreach (var block in blocks)
            {
                block.Dispose();
            }
        }
    }

    // 
    // Private methods
    // 

    /// <summary>
    /// Allocate new block for use, either by dequeueing an exising non-used block
    /// or creating a new one
    /// </summary>
    /// <returns>Newly allocated block ready to use.</returns>
    private IBlock AllocateBlock()
    {
        uint reusableBlockId;
        IBlock newBlock;

        if (this.TryFindFreeBlock(out reusableBlockId) == false)
        {
            // then create a new block
            newBlock = this.storage.CreateNew();

            if (newBlock == null)
            {
                throw new InvalidDataException("Block not found by id: " + reusableBlockId);
            }
        }
        else
        {
            newBlock = this.storage.Find(reusableBlockId);

            if (newBlock == null)
            {
                throw new InvalidDataException("Block not found by id: " + reusableBlockId);
            }

            // Update header fields for error checking - not sure if this is necessary
            newBlock.SetHeader(kBlockContentLength, 0L);
            newBlock.SetHeader(kNextBlockId, 0L);
            newBlock.SetHeader(kPreviousBlockId, 0L);
            newBlock.SetHeader(kRecordLength, 0L);
            newBlock.SetHeader(kIsDeleted, 0L);
        }

        return newBlock;
    }

    private bool TryFindFreeBlock(out uint blockId)
    {
        blockId = 0;
        IBlock lastBlock, secondLastBlock;
        this.GetSpaceTrackingBlock(out lastBlock, out secondLastBlock);

        using (lastBlock)
        using (secondLastBlock)
        {
            var currBlockContentLength = lastBlock.GetHeader(kBlockContentLength);
            // does this mean availalbe contentlength or does it mean total content length used
            if (currBlockContentLength == 0)
            {
                // instance of no available blocks
                if (secondLastBlock == null)
                {
                    return false;
                }

                // dequeue/pop the next available block (why call it dequeue if it's a stack??????)
                blockId = this.ReadUInt32FromTrailingContent(secondLastBlock);

                // update content length after popping the last block from secondLastBlock
                secondLastBlock.SetHeader(kBlockContentLength, secondLastBlock.GetHeader(kBlockContentLength) - 4);

                // lastBlock is now empty, so it is added to secondLastBlock as the next available block to be used/can be used
                this.AppendUint32ToContent(secondLastBlock, lastBlock.Id);

                // update headers accordingly
                secondLastBlock.SetHeader(kBlockContentLength, secondLastBlock.GetHeader(kBlockContentLength) + 4);
                secondLastBlock.SetHeader(kNextBlockId, 0);
                lastBlock.SetHeader(kPreviousBlockId, 0);

                return true;
            }
            // else do the same thing but for the first block - but why wouldn't you just change the pointer
            else
            {
                blockId = this.ReadUInt32FromTrailingContent(lastBlock);
                lastBlock.SetHeader(kBlockContentLength, currBlockContentLength - 4);

                return true;
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

                currBlockId = (uint)currBlock.GetHeader(kNextBlockId);
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
                    block.Dispose();
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
                secondLastBlock = blocks[blocks.Count - 2];
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

    /// <summary>
    /// Appends/write Uint32 (128 bytes) starting from the end of the block content by converting it into a bytearray
    /// </summary>
    private void AppendUint32ToContent(IBlock block, uint value)
    {
        var contentLength = block.GetHeader(kBlockContentLength);

        if (contentLength % 4 == 0)
        {
            throw new DataMisalignedException("Block content length not %4: " + contentLength);
        }

        block.Write(
            src: LittleEndianByteOrder.GetBytes(value),
            srcOffset: 0,
            dstOffset: (int)contentLength,
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

        // read the data from the bytes of the blockcontent - reading doesn't delete the content/pop/update it - just writes it to a buffer?
        block.Read(
            dest: buffer,
            dstOffset: 0,
            srcOffset: (int)contentLength - 4,
            count: 4
        );
        return LittleEndianByteOrder.GetUInt32(buffer);
    }

    private void MarkAsFree(uint blockId)
    {
        // targetBlock is the available recordIdBlock
        IBlock lastBlock, secondLastBlock, targetBlock = null;
        GetSpaceTrackingBlock(out lastBlock, out secondLastBlock);

        using (lastBlock)
        using (secondLastBlock)
        {
            try
            {
                var recordLength = lastBlock.GetHeader(kBlockContentLength);
                // when there is available space in the last block
                if (recordLength + 4 <= this.storage.BlockContentSize)
                {
                    targetBlock = lastBlock;
                }
                else
                {
                    // change secondLastBlock to lastBlock
                    targetBlock = AllocateBlock();
                    
                    lastBlock.SetHeader(kNextBlockId, targetBlock.Id);
                    targetBlock.SetHeader(kPreviousBlockId, lastBlock.Id);
                }

                // add the blockId to the record keeper
                this.AppendUint32ToContent(targetBlock, blockId);

                // update header field afterwards since change was made
                targetBlock.SetHeader(kBlockContentLength, recordLength + 4);
            }
            finally
            {
                if (targetBlock != null)
                {
                    targetBlock.Dispose();
                }
            }
        }
    }
}