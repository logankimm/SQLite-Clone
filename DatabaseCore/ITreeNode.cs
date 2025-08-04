namespace DatabaseCore;

public interface ITreeNode<K, V>
{
    K MaxKey { get; }
    K MinKey { get; }
    bool IsEmpty { get; }
    bool IsLeaf { get; }
    /// <summary>
    /// Why is this one  * 2?
    /// </summary>
    bool IsOverflow { get; }
    int EntriesCount { get; }
    int ChildrenNodeCount { get; }
    uint ParentId { get; }
    uint[] ChildrenIds { get; }
    Tuple<K, V>[] Entries { get; }
    uint Id { get; }
    void Remove(int removeAt);
    void InsertAsLeaf(K key, V value, int insertPosition);
    void InsertAsParent(K key, V value, uint leftReference, uint rightReference, out int insertPosition);
    void Split(out ITreeNode<K, V> outLeftNode, out ITreeNode<K, V> outRightNode);

    // Search Operations
    int BinarySearchEntriesForKey(K key);
    int BinarySearchEntriesForKey(K key, bool firstOccurence);
    void FindLargest(out ITreeNode<K, V> node, out int index);
    void FindSmallest(out ITreeNode<K, V> node, out int index);
    
    // Navigation
    int IndexInParent();
    ITreeNode<K, V> GetChildNode(int atIndex);
    Tuple<K, V> GetEntry(int atIndex);
    bool EntryExists(int atIndex);
}