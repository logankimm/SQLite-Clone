
// Implementation of block storage (don't go from a stream - store in individual array boxes)
public interface IBlockStorage
{
    int BlockContentSize { get; }
    int BlockHeaderSize { get; }
    int BlockSize { get; }
    IBlock Find(uint blockId);
    IBlock CreateNew();
}
