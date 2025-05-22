using System;
using System.IO;
using System.Collections.Generic;

namespace DatabaseCore

public class RecordStorage : IRecordStorage
{
    readonly IBlockStorage storage;

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