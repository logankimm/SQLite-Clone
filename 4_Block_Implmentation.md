## Additionally/misc data
- Data is not seen as a stream but split int sizeable chunks known as blocks that can be reused after deletion
## Block storage
- One individual file that should hold the blocks?
### Class/Interface Attributes
1. blockSize - size of individual blocks within the file system
    1. blockContentSize - Size of content (overall size - header size data)
    2. unitOfWork - not sure what this does yet - testing formatting rn - Is the same thing as blockSize/DiskSectorSize - what's the reason for making 2 separate variable names (maybe has to do with how the os operates? when determining sizes)
2. stream - what is this? - look at teh following data type of stream
    1. an example used in test - new MemoryStream
    2. an example - FileStream (allows for reading and writing of a file)
3. What the fuck is stream - incoming stream of data to be processed I think? - please let me know because I'm so fucking confused this doesn't make any fucking sense - yes it is

### Functions
- Find
1. firstSector - byte[] of size DiskSectorSize
## Individual Blocks
- Size of blocks (must be of multiple 4KB as OS writes/reads data in 4KB chunks)
- While OS's read in 4KB, they some have sub-page writes (256/512B) for smaller data. Additionally 256 is 2^8, which allows for bitwise operations, and thus more efficient operations