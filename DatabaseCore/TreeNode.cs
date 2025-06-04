using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DatabaseCore;

public class TreeNode<K, V> : ITreeNode<K, V>
{
    protected uint id = 0;
    protected uint parentId;
    protected readonly ITreeNodeManger<K, V> nodeManger;
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
            return entries.Count > (nodeManger.MinEntriesPerNode * 2);
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
            nodeManger.MarkAsChanged(this);
        }
    }

    public uint[] ChildrenIds
    {
        get
        {
            return childrenIds.toArray();
        }
    }

    public Tuple<K, V>[] Entries
    {
        get
        {
            return entries.toArray();
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

    public TreeNode(ITreeNodeManager<K, V> nodeManger,
        uint id,
        uint parentId,
        IEnumerable<Tuple<K, V>> entries = null,
        IEnumerable<Tuple<K, V>> childrenIds = null)
    {
        if (nodeManger == null)
        {
            throw new ArgumentNullException(nameof(nodeManager));
        }

        this.id = id;
        this.parentId = parentId;

        // for read-only attributes/fields
        this.nodeManger = nodeManger;
        this.childrenIds = new List<uint>();
        this.entries = new List<Tuple<K, V>>(this.nodeManager.MinEntriesPerNode * 2);

        if (entries != null)
        {
            this.entries.AddRange(entires);
        }
        if (childrenIds != null)
        {
            this.childrenIds.AddRange(childrenIds);
        }
    }

    public void Remove(int removeAt)
    {

    }
}