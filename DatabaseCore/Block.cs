using System;

namespace DatabaseCore;

public class Block : IBlock
{
    readonly uint id;

    public event EventHandler Disposed;

    public uint Id {
        get {
            return id;
        }
    }

    public Block(BlockStorage storage, uint Id, Stream stream)
    {

    }
}