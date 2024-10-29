## Additionally/misc data
- Data is not seen as a stream but split int sizeable chunks known as blocks that can be reused after deletion
## Block storage
- One individual file that should hold the blocks?
### Class/Interface Attributes
1. blockSize - size of individual blocks within the file system
    1. blockContentSize - Size of content (overall size - header size data)
    2. unitOfWork - not sure what this does yet - testing formatting rn
## Individual Blocks
- Size of blocks (must be of multiple 4KB as OS writes/reads data in 4KB chunks)
- 