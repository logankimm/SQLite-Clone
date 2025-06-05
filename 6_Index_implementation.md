# Purpose
Indexing through a B-tree allows for search based on multiple key value pairs (e.g. search by Id, Breed, weight, etc.)
- There can be one or more IIndex's to create a complete database

#### Special Notes
- Multi-level indexes
| Root Index (Level 1)       | Sub-Table Address (Level 2)  | Entries Mapped          |
|----------------------------|-----------------------------|-------------------------|
| `1`                        | `0x1000`                    | Entries 1-32            |
| `33`                       | `0x2000`                    | Entries 33-61           |
| `62`                       | `0x3000`                    | Entries 62-95           |
| `...`                      | `...`                       | `...`                   |
| `N`                        | `0xNNNN`                    | Entries N-(N+chunk_size)|

Table for actual data
| id  | name  | data1 |
|---------------------|
| 1   | John  | dog   |
| 2   | Bob   | cat   |

#### B-Tree Rules
1. When creating a new node, the current node must have at least m/2 children inside
2. The root node must have a minimum of 2 entries
3. All leaf nodes must be at the same level
4. The tree builds from bottom up

## Class/Interface Attributes
1. kRecordLeng


### Functions


#### Protected Methods




### Individual Blocks
- 


### Implementations
1. 

### Examples

# Helper Classes

## TreeNode

### Purpose
I have no clue

## TreeNode

### Purpose
Individual nodes within the B-Tree
Structure:
    - [Child pointer, index, index data pointer, child2 pointer, index2, ....]
    - Nodes are stored inside pages which sturctured: [pointer to another page, data]

#### Special Notes
- Constructor:
    - List(3) - specificies initial length of 3

### Class/Interface Attributes
1. entries - list of indicies within each node
4. childrenIds - I assume these are pointers for children nodes - why is it a list of tuples - couldn't this be pointers instead?
    - Leaf nodes are nodes that have 0 children, or are at the bottom of the tree


### Functions


#### Protected Methods

## TreeNodeManager

### Functions
- CreateNewRoot()
    - Curly braces initalize the array with the elements inside