namespace DatabaseCore

public interface IRecordStorage {
    // An array of blocks included?
    // an initial record for deletion
    // Update a record's data
    void Update(uint recordId, byte[] data);

    byte[] Find(byte[] recordId);

    // Create a new empty record and returns an id
    uint Create();

    // Create an empty record given data and returns a new id
    uint Create(byte[] data);

    // <summary>
    // Creates an empty record given a generator to fill data and returns a new id
    // </summary>
    uint Create(Func<uint, byte[]> dataGenerator);

    void Delete(uint recordId);
}