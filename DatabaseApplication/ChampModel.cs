// Type of data being stored within the database
public class ChampModel
{
    public Guid Id { get; set; }
    public string name { get; set; }
    public int cost { get; set; }
    public string setName { get; set; }
    public byte[] unitData { get; set; }
}