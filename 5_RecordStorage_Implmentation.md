# Purpose
RecordStorage is used to handle data with variable length(that ususally exceed the BlockSize) by allocating data onto a different block, similar to a linked list
- For example: If BlockSize = 4096 and Data = 5000, the first 4096 - BlockHeaderSize bytes are allocated into a first block, and the rest is stored in a different block whose index can be found in the first's BlockHeader
- Record: Another word for a collection/linked list of blocks that makeup data from one source
1. Parts:
    1. ID: The first block it is made up of
#### Special Notes
The first block contains a stack of deleted blocks that can be reused/reallocated so that memory is not wasted

## Class/Interface Attributes
1. 


### Functions
- Find
    

#### Protected Methods



### Individual Blocks
- 


### Implementations
1. 

### Examples