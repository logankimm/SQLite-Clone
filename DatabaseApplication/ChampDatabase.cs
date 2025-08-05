// Type of data being stored within the database
using DatabaseCore;

namespace DatabaseApplication;
public class ChampDatabase
{
    readonly Stream mainDatabaseFile;
    readonly Stream primaryIndexFile;
    readonly Stream secondaryIndexFile;
    readonly Tree<Guid, uint> primaryIndex;
    readonly Tree<Tuple<string, int>, uint> secondaryIndex;
    readonly RecordStorage champRecords;
    readonly ChampSerializer champSerializer = new ChampSerializer();
}