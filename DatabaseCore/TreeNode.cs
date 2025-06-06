using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DatabaseCore;

public class TreeNode<K, V> : ITreeNode<K, V>
{
    protected uint id = 0;
    protected uint parentId;
    protected readonly ITreeNodeManager<K, V> nodeManager;
    protected readonly List<uint> childrenIds;
    protected readonly List<Tuple<K, V>> entries;

    public K MaxKey
    {
        get
        {
            return entries[entries.Count - 1].Item1;
        }
    }

    public K MinKey
    {
        get
        {
            return entries[0].Item1;
        }
    }

    public bool IsEmpty
    {
        get
        {
            return entries.Count == 0;
        }
    }

    public bool IsLeaf
    {
        get
        {
            return childrenIds.Count == 0;
        }
    }

    public bool IsOverflow
    {
        get
        {
            return entries.Count > (nodeManager.MinEntriesPerNode * 2);
        }
    }

    public int EntriesCount
    {
        get
        {
            return entries.Count;
        }
    }

    public int ChildrenNodeCount
    {
        get
        {
            return childrenIds.Count;
        }
    }

    public uint ParentId
    {
        get
        {
            return this.parentId;
        }

        private set
        {
            parentId = value;
            nodeManager.MarkAsChanged(this);
        }
    }

    public uint[] ChildrenIds
    {
        get
        {
            return childrenIds.ToArray();
        }
    }

    public Tuple<K, V>[] Entries
    {
        get
        {
            return entries.ToArray();
        }
    }

    /// <summary>
    /// Id of this node, assigned by node manager. Node never change its id itself
    /// </summary>
    public uint Id
    {
        get
        {
            return this.id;
        }
    }

    public TreeNode(ITreeNodeManager<K, V> nodeManager,
        uint id,
        uint parentId,
        IEnumerable<Tuple<K, V>> entries = null,
        IEnumerable<uint> childrenIds = null)
    {
        if (nodeManager == null)
        {
            throw new ArgumentNullException(nameof(nodeManager));
        }

        this.id = id;
        this.parentId = parentId;

        // for read-only attributes/fields
        this.nodeManager = nodeManager;
        this.childrenIds = new List<uint>();
        this.entries = new List<Tuple<K, V>>(this.nodeManager.MinEntriesPerNode * 2);

        if (entries != null)
        {
            this.entries.AddRange(entries);
        }
        if (childrenIds != null)
        {
            this.childrenIds.AddRange(childrenIds);
        }
    }

    public void Remove(int removeAt)
    {
        if (removeAt < 0 || removeAt >= this.entries.Count)
        {
            throw new ArgumentOutOfRangeException();
        }


        if IsLeaf()
        {

        }
    }

    public void InsertAsLeaf(K key, V value, int insertPosition)
    {

    }

    public void InsertAsParent(K key, V value, uint leftReference, uint rightReference, out int insertPosition)
    {
        insertPosition = 0;
        return;
    }

    public void Split(out ITreeNode<K, V> outLeftNode, out ITreeNode<K, V> outRightNode)
    {

    }

    // Search Operations
    public int BinarySearchEntriesForKey(K key)
    {
        return 0;
    }
    public int BinarySearchEntriesForKey(K key, bool firstOccurence)
    {

    }
    public void FindLargest(out ITreeNode<K, V> node, out int index)
    {

    }
    public void FindSmallest(out ITreeNode<K, V> node, out int index)
    {

    }


    /// <summary>
    /// Get this node's index in its parent
    /// e.g. parent node: [A, B, C], for node A it would return 0
    /// </summary>
    public int IndexInParent()
    {
        var parent = nodeManager.Find(parentId);
        if (parent == null)
        {
            throw new Exception("IndexInParent fails to find parent node of " + id);
        }

        var childrenIds = parent.ChildrenIds;
        // Length is used since ChildrenIds capitalized returns a ToArray
        for (int i = 0; i < childrenIds.Length; i++)
        {
            if (this.id == childrenIds[i])
            {
                return i;
            }
        }
        
        throw new Exception("Failed to find index of node " + id + " in its parent");
    }
    public ITreeNode<K, V> GetChildNode(int atIndex)
    {

    }
    public Tuple<K, V> GetEntry(int atIndex)
    {

    }
    public bool EntryExists(int atIndex)
    {

    }
}