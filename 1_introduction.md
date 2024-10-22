# SQLite-Clone
Clone of SQLite for better understanding of databases

Basic structure Integer (4 bytes) length of the following string (1 byte or 255 characters - 256 bytes is 2 bytes needed on the length not 1) string characters (fixed length of 255) - used so that you can look at rows and not search for a delimiter which is O(n)

Use a b-tree to parse through indicies quickly (a basic binary tree but number of children held is variable)

## Storage System
1. Initial use block storage - we store the data into separate chunks/blocks of a fixed length (which is decided later) for data to be written/read from
    Properties
    - Contains unique metadata header
    - Contains data to be stored
2. Account for variable length data - because some data can be longer than size allocated for blocks, we use another system called Record Storage. A record is composed of several blocks put together in the form of a linked list (blocks don't have to be sequential in memory, can point to a different block that doesn't come sequentially)
    Properties
    - Contains an id which matches the id of the first block
    - Record #0 (first record) - contains a list of deleted/to be deleted blocks. Whenever a block is marked for deletion, the pointer to the block is added onto the deleted record to be reused/free up space to be used in the future