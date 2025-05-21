# Additionally/misc data
- Data is not seen as a stream but split int sizeable chunks known as blocks that can be reused after deletion
- Matching 1:1 for instance of BlockStorage to data stream
# Block storage
- One individual file that should hold the blocks?
## Class/Interface Attributes
1. blockSize - size of individual blocks within the file system
    1. blockContentSize - Size of content (overall size - header size data)
    2. unitOfWork - not sure what this does yet - testing formatting rn - Is the same thing as blockSize/DiskSectorSize - what's the reason for making 2 separate variable names (maybe has to do with how the os operates? when determining sizes)
2. stream - what is this? - look at teh following data type of stream
    1. an example used in test - new MemoryStream
    2. an example - FileStream (allows for reading and writing of a file)
    3. What is stream - incoming stream of data to be processed I think? - nope, it's the physical storage backend for data


### Functions
- Find
    What function handles reassigning blockIds for deleted variables
    1. firstSector - byte (values of 0 - 255) array of length DiskSectorSize e.g. [0, 1, 255, 254, 2, 0...]
- CreateNew()
    1. blockId is stored as uint (0 - 4mil and can't be negative) and is the next available id
        a. if stream length = 0, then no data is written, so write at 0
        b. if stream length = 4096, then 1 piece of data is written (id 0), so write at id 1
    2. Extending stream length
        a. the function is setLength NOT addLength - this.stream.Length + blockSize is an inefficient implementation since it doesn't factor deleted blocks
        b. Flush() - writes new data to disk immediately and executes changes before moving forward


#### Protected Methods
Protected methods can be accessed through subclasses. Code is separated from the Constructor because Block doesn't have access to key variables (e.g. dictionary block) and shouldn't have access to disposed fields b/c it would need access to stored cache (dictionary blocks) and it would never delete itself b/c event handlers ()


### Individual Blocks
- Size of blocks (must be of multiple 4KB as OS writes/reads data in 4KB chunks)
- While OS's read in 4KB, they some have sub-page writes (256/512B) for smaller data. Additionally 256 is 2^8, which allows for bit shifts, and thus more efficient operations


### Implementations
1. Creates private variable fields for the class using readonly. These can only be modified during the creation of an instance and not changed otherwise.
 - readonly are fields (private variables which can't be accessed) vs something like public int BlockSize which is a public property (which can be accessed through code)

### Examples
```
var nodeManager = new TreeDiskNodeManager<int, long> (
    new TreeIntSerializer(),
    new TreeLongSerializer(),
    new RecordStorage(
        new BlockStorage(
            stream, 
            4096, 
            48
        )
    )
); 


// Construct the RecordStorage that use to store main cow data
this.cowRecords = new RecordStorage (new BlockStorage(this.mainDatabaseFile, 4096, 48));
```

# Blocks

## Class/Interface Attributes
1. firstSector - data read from stream with length DiskSectorSize - the amount of data that's read/written per action - contains header data + more if for example:
```
    blockSize: 4096,  // 4KB blocks
    blockHeaderSize: 48,
    diskSectorSize: 512  // 512-byte sectors
```
7. isDisposed - reference to whether or not its deleted? - there's an event handler - yes, safety precaution to make sure removed blocks cannot be accessed

Still confused on why field is 8 bytes?


### Implementations
1. Creates a new block in memory? - don't think it is hard written to the stream (yeah this is done through BlockStorage.CreateNew())


### Functions
1. GetHeader() - 
    - BufferHelper.ReadBufferInt64 - custom class to help with reading/writing data in arrays. Reads the data in firstSector starting from the bufferOffset(field * 8) and returns the data after converting the byte[] -> long
    #### Implementation
3. Read() - 
    - Reads data from a stream and transfers it into a destination buffer

    - copyableFromFirstSector - Comparison is < and not <= because e.g. HeaderSize = 10, srcOffSet = 2, DiskSectorSize = 12, the first read byte is outside of the firstSector
    - numBytesToRead - In the form of a min because it's faster/efficient reading based off of DiskSectorSize
```

```


#### Protected Methods



### Individual Blocks
- 


### Examples