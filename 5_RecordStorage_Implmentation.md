# Purpose
RecordStorage is used to handle data with variable length(that ususally exceed the BlockSize) by allocating data onto a different block, similar to a linked list
- For example: If BlockSize = 4096 and Data = 5000, the first 4096 - BlockHeaderSize bytes are allocated into a first block, and the rest is stored in a different block whose index can be found in the first's BlockHeader
- Record: Another word for a collection/linked list of blocks that makeup data from one source
1. Parts:
    1. ID: The first block in the record
#### Special Notes
- The first block contains a stack of deleted blocks that can be reused/reallocated so that memory is not wasted
- TrailingContent == Block Content

## Class/Interface Attributes
1. kRecordLength - represents the length of data inside the record (so this would match for every single block in a record?)
    - thinking: this is only for the first block inside a record


### Functions
- Find
    - Default values for header is 00..00. So, previous blockId is set to 0 at start. If it is 0, then it has no containing information, and therefore is invalid?
    - currBlock.Read() - cast currBlockSize to (int) since its an incoming long? - not sure how this could cause problems but it could I guess
        - Reading index is already offsetting header size so need to do it again
- Create()
    1. Creates the new block that is empty
    2. Returns the Id of created block
    3. Then disposes the data after (the using still occurs even after the return)
- Create(dataGenerator)
    - dataGenerator: is a function that takes in input uint and return a byte[]
- Delete()
    - Doesn't reset the data within teh block here (not sure why this is the case? - isn't more memory efficient to rewrite/delete the content here to free up memory?)
- Update()
    - Could input byte[] data be a pointer instead of the actual data
    - I'm pretty sure it replaces teh entire contents instead of editing a single portion of the data

#### Protected Methods
- FindBlocks() - Blocks is data type List<IBlock> instead of IBlock[] because List<> allows for variable lengths
    - the do {} while() is used so that the code always executes once before checking the while condition (for the use case where FindBlocks(0))
    - finally{} will always occur regardless of what happens in the try clause
- TryFindFreeBlock() - Helper function to find the next available free block (from deleted/reusable blocks in record 0?)
    - Check the record 0 content length
        - if length == 0, pop the last found block in secondLastBlock & then reassign lastBlock to be the next available block in the chain
        - if length != 0, then pop from lastBlock and update its contentLength by -= 4
    - Not sure why you can't just return lastblock.id if (currentBlockContentLength == 0), but will look into it later for optimization



### Individual Blocks
- 


### Implementations
1. 

### Examples

# Helper Classes

## LittleEndianByteOrder

### Purpose
- Static (no need to instantiate a class) methods to read and write numeric data from bytes -> uint

### Static functions
- GetUInt32 - Takes in array of little endian -> uint32
    - For BigEndian instance, a copy array is created and reversed rather than working on original b/c it modifies the original, input instance
        ```
        <!-- This code modifies the original copy so future methods/changes work off of changed version rather than original -->
        if (!BitConverter.IsLittleEndian) 
            {
                Array.Reverse(bytes); // Modifies the original array!
                return BitConverter.ToUInt32(bytes, 0);
            }
        ```