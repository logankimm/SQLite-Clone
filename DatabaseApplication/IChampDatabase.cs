
// Creating class library that allows file to be used/imported within other projects
public interface IChampDatabase
{
    void Insert(ChampModel champ);
    void Delete(ChampModel champ);
    void Update(ChampModel champ);
    ChampModel Find(Guid id);
    IEnumerable<ChampModel> FindBy(string name, int cost);
}
