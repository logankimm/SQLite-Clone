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
}