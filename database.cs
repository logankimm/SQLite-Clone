// Type of data being stored within the database
public class ChampModel
{
    public Guid Id { get; set; }
    public string name { get; set; }
    public int cost { get; set; }
    public string setName { get; set; }
    public byte[] unitData { get; set; }
}

// Creating class library that allows file to be used/imported within other projects
public interface IChampDatabase
{
    void Insert(ChampModel champ);
    void Delete(ChampModel champ);
    void Update(ChampModel champ);
    ChampModel Find(Guid id);
    IEnumerable<ChampModel> FindBy(string name, int cost);
}

// Implementation of block storage (don't go from a stream - store in individual array boxes)
public interface IBlockStorage
{
    int BlockContentSize { get; }
    int BlockHeaderSize { get; }
    int BlockSize { get; }
    IBlock Find(uint blockId);
    IBlock CreateNew();
}

public interface IBlock : IDisposable
{
    // Unsigned integer (only stores positives not negatives) that is unique
    uint Id { get; }
    long GetHeader(int field);
    void SetHeader(int field, long newHeader);
    void Read(byte[] dst, int dstOffSet, int srcOffSet, int count);

    /// <summary>
    /// Write content of given buffer (src) into this (dst)
    /// </summary>
    void Write(byte[] src, int srcOffset, int dstOffset, int count);
}

// Creating input command line interface (REPL)
int main(int argc, char* argv[])
{

}