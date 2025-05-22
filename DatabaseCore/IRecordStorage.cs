namespace DatabaseCore

public interface IRecordStorage
{
    /// <summary>
    /// Grab a record's data
    /// </summary>
    byte[] Find(byte[] recordId);

    /// <summary>
    // Create a new empty record and returns an id
    /// </summary>
    uint Create();

    /// <summary>
    // Create a new record filed with input data and returns a new id
    /// </summary>
    uint Create(byte[] data);

    // <summary>
    // Creates an empty record given a generator to fill data and returns a new id
    // </summary>
    uint Create(Func<uint, byte[]> dataGenerator);

    /// <summary>
    // Delete recordId and its data
    /// </summary>
    void Delete(uint recordId);

    /// <summary>
    // Update a record's data
    /// </summary>
    void Update(uint recordId, byte[] data);
}