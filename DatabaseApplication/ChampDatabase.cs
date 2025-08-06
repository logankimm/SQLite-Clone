// Type of data being stored within the database
using System;
using System.IO;
using System.Collections.Generic;
using DatabaseCore;

namespace DatabaseApplication;
public class ChampDatabase : IDisposable
{
    readonly Stream mainDatabaseFile;
    readonly Stream primaryIndexFile;
    readonly Stream secondaryIndexFile;
    readonly Tree<Guid, uint> primaryIndex;
    readonly Tree<Tuple<string, int>, uint> secondaryIndex;
    readonly RecordStorage champRecords;
    readonly ChampSerializer champSerializer = new ChampSerializer();

    public ChampDatabase (string pathToChampDb)
    {
        ArgumentNullException.ThrowIfNull(pathToChampDb);

        this.mainDatabaseFile = new FileStream (pathToChampDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
        this.primaryIndexFile = new FileStream (pathToChampDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
        this.secondaryIndexFile = new FileStream (pathToChampDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

        this.champRecords = new RecordStorage(new BlockStorage(this.mainDatabaseFile, blockSize: 4096, blockHeaderSize: 48));

        this.primaryIndex = new Tree<Guid, uint> (
            new TreeDiskNodeManager<Guid, uint> (
                new GuidSerializer(),
                new TreeUIntSerializer(),
                new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
            ),
            false
        );

        this.secondaryIndex = new Tree<Tuple<string, int>, uint> (
            new TreeDiskNodeManager<Tuple<string, int>, uint> (
                new StringIntSerializer(),
                new TreeUIntSerializer(),
                new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
            ),
            true
        );
    }

    public void Update(ChampModel champ)
    {
        ObjectDisposedException.ThrowIf(disposed, "ChampDatabase");

        // Find the original cow to get its old secondary key
        var Champ = Find(champ.Id);
        if (Champ == null) {
            throw new InvalidOperationException("Champ not found, cannot update.");
        }

        // Find the record ID using the primary index
        var entry = this.primaryIndex.Get(champ.Id);
        if (entry == null) {
            // This case should theoretically not be reached if Champ was found, but it's good practice
            throw new InvalidOperationException("Champ record not found in primary index.");
        }
        var recordId = entry.Item2;

        // 1. Delete the old secondary index entry
        var oldSecondaryKey = new Tuple<string, int>(Champ.Name, Champ.Set);
        this.secondaryIndex.Delete(oldSecondaryKey, recordId);

        // 2. Update the actual cow record in the main database file
        this.champRecords.Update(recordId, this.champSerializer.Serialize(champ));

        // 3. Insert the new secondary index entry
        var newSecondaryKey = new Tuple<string, int>(champ.Name, champ.Set);
        this.secondaryIndex.Insert(newSecondaryKey, recordId);
    }

    public void Insert(ChampModel champ)
    {
        ObjectDisposedException.ThrowIf(disposed, "ChampDatabase");

        var recordId = this.champRecords.Create(this.champSerializer.Serialize(champ));
        this.primaryIndex.Insert(champ.Id, recordId);
        this.secondaryIndex.Insert(new Tuple<string, int>(champ.Name, champ.Set), recordId);
    }

    public ChampModel Find(Guid champId)
    {
        ObjectDisposedException.ThrowIf(disposed, "ChampDatabase");

        var entry = this.primaryIndex.Get(champId);
        if (entry == null)
        {
            return null;
        }

        return this.champSerializer.Deserializer(this.champRecords.Find(entry.Item2));
    }

    public IEnumerable<ChampModel> FindBy(string name, int set)
    {
        var comparer = Comparer<Tuple<string, int>>.Default;
        var searchKey = new Tuple<string, int>(name, set);

        foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(searchKey))
        {
            // stop after encountering something larger than the key
            if (comparer.Compare(entry.Item1, searchKey) > 0)
            {
                break;
            }

            yield return this.champSerializer.Deserializer(this.champRecords.Find(entry.Item2));
        }
    }

    public void Delete (ChampModel champ)
    {
        ObjectDisposedException.ThrowIf(disposed, "ChampDatabase");

        // Find the record ID from the primary index
        var entry = this.primaryIndex.Get(champ.Id);
        if (entry == null) {
            // Cow doesn't exist, so nothing to delete
            return;
        }
        var recordId = entry.Item2;

        // 1. Delete from primary index
        this.primaryIndex.Delete(champ.Id);

        // 2. Delete from secondary index
        // Note: The secondary index allows duplicates, so we need to provide the key and value to delete the specific entry
        var secondaryKey = new Tuple<string, int>(champ.Name, champ.Set);
        this.secondaryIndex.Delete(secondaryKey, recordId);

        // 3. Delete the actual record
        this.champRecords.Delete(recordId);
    }
    
    #region Dispose
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            this.mainDatabaseFile.Dispose();
            this.secondaryIndexFile.Dispose();
            this.primaryIndexFile.Dispose();
            this.disposed = true;
        }
    }

    ~ChampDatabase() 
    {
        Dispose(false);
    }
    #endregion
}