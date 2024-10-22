namespace database;

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