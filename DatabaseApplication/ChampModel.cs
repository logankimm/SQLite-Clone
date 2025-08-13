namespace DatabaseApplication;

public class ChampModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Class { get; set; }
    public int Set { get; set; }
    public byte[] UnitData { get; set; }
    public override string ToString()
    {
        return string.Format("[ChampModel: Id={0}, Name={1}, Set={2}, Class={3}, UnitData={4}]", Id, Name, Set, Class, UnitData.Length + " bytes");
    }
}
