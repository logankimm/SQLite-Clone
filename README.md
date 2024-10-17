# SQLite-Clone
Clone of SQLite for better understanding of databases

Basic structure Integer (4 bytes) length of the following string (1 byte or 255 characters - 256 bytes is 2 bytes needed on the length not 1) string characters (fixed length of 255) - used so that you can look at rows and not search for a delimiter which is O(n)

Use a b-tree to parse through indicies quickly (a basic binary tree but number of children held is variable)