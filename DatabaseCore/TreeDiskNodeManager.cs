using System;
using log4net;
using System.Collections.Generic;
using System.Numerics;
using log4net.Filter;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

namespace DatabaseCore;

public sealed class TreeDiskNodeManager<K, V> : ITreeNodeManager<K, V>
{
    readonly IRecordStorage recordStorage;
    readonly Dictionary<uint, TreeNode<K, V>> dirtyNodes = new Dictionary<uint, TreeNode<K, V>>();
    readonly Dictionary<uint, WeakReference<TreeNode<K, V>>> nodeWeakRefs = new Dictionary<uint, WeakReference<TreeNode<K, V>>>();
    readonly Queue<TreeNode<K, V>> nodeStrongRefs = new Queue<TreeNode<K, V>>();
    readonly TreeDiskNodeSerializer<K, V> serializer;
    readonly int maxStrongNodeRefs = 200;
    readonly ushort minEntriesPerNode = 36;

    TreeNode<K, V> rootNode;
    int cleanupCounter = 0;

    public ushort MinEntriesPerNode
    {
        get
        {
            return minEntriesPerNode;
        }
    }
    public IComparer<K> KeyComparer
    {
        get;
        private set;
    }
    
    public IComparer<Tuple<K, V>> EntryComparer
    {
        get;
        private set;
    }
    
    public TreeNode<K, V> RootNode
    {
        get
        {
            return rootNode;
        }
    }

    public TreeDiskNodeManager (
        ISerializer<K> keySerializer,
        ISerializer<V> valueSerializer,
        IRecordStorage nodeStorage
    ) : this (keySerializer, valueSerializer, nodeStorage, Comparer<K>.Default)
    {}
    
    public TreeDiskNodeManager (
        ISerializer<K> keySerializer,
        ISerializer<V> valueSerializer,
        IRecordStorage recordStorage,
        IComparer<K> keyComparer
    )
    {
        ArgumentNullException.ThrowIfNull(recordStorage, nameof(recordStorage));

        this.recordStorage = recordStorage;
        this.serializer = new TreeDiskNodeSerializer<K, V> (this, keySerializer, valueSerializer);
        this.KeyComparer = keyComparer;
        this.EntryComparer = Comparer<Tuple<K, V>>.Create((a, b) =>
        {
            return KeyComparer.Compare(a.Item1, b.Item1);
        });

        // Find the root node or create it
        var firstBlockData = recordStorage.Find(1u);
        if (firstBlockData != null)
        {
            this.rootNode = Find(BufferHelper.ReadBufferUInt32(firstBlockData, 0));
        }
        else
        {
            this.rootNode = CreateFirstRoot();
        }
    }

    public TreeNode<K, V> Create(IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
    {
        TreeNode<K, V> node = null;

        recordStorage.Create(nodeId =>
        {
            node = new TreeNode<K, V>(this, nodeId, 0, entries, childrenIds);
            OnNodeInitialized(node);

            return this.serializer.Serialize(node);
        });

        if (node == null) {
            throw new Exception("dataGenerator wasn'never called by nodeStorage");
        }

        return node;
    }

    public TreeNode<K, V> Find(uint id)
    {
        // check if node is held in memory and return, otherwise remove weak reference
        if (nodeWeakRefs.ContainsKey(id))
        {
            TreeNode<K, V> node;
            if (nodeWeakRefs[id].TryGetTarget(out node))
            {
                return node;
            }
            nodeWeakRefs.Remove(id);
        }

        var data = recordStorage.Find(id);
        if (data == null)
        {
            return null;
        }
        var dNode = this.serializer.Deserialize(id, data);

        OnNodeInitialized(dNode);
        return dNode;
    }

    public TreeNode<K, V> CreateNewRoot(K key, V value, uint leftNodeId, uint rightNodeId)
    {
        var node = Create(new Tuple<K, V>[]
        {
            new Tuple<K, V> (key, value)
        }, new uint[] {
            leftNodeId,
            rightNodeId
        });

        this.rootNode = node;
        recordStorage.Update(1u, LittleEndianByteOrder.GetBytes(node.Id));

        return this.rootNode;
    }

    public void MakeRoot(TreeNode<K, V> node)
    {
        this.rootNode = node;
        recordStorage.Update(1u, LittleEndianByteOrder.GetBytes(node.Id));
    }

    public void Delete(TreeNode<K, V> node)
    {
        if (node == this.rootNode) {
            this.rootNode = null;
        }

        recordStorage.Delete(node.Id);

        // simplified version of removal
        dirtyNodes.Remove(node.Id);
        // if (dirtyNodes.ContainsKey(node.Id))
        // {
        //     dirtyNodes.Remove(node.Id);
        // }
    }

    public void MarkAsChanged(TreeNode<K, V> node)
    {
        if (dirtyNodes.ContainsKey(node.Id) == false)
        {
            dirtyNodes.Add(node.Id, node);
        }
    }

    public void SaveChanges()
    {
        foreach (var kv in dirtyNodes)
        {
            recordStorage.Update(kv.Value.Id, this.serializer.Serialize(kv.Value));
        }

        dirtyNodes.Clear();
    }
    private TreeNode<K, V> CreateFirstRoot()
    {
        // record first node in first block with id = 2
        recordStorage.Create(LittleEndianByteOrder.GetBytes((uint)2));

        // return a new node with id of 2
        return Create(null, null);
    }

    private void OnNodeInitialized(TreeNode<K, V> node)
    {
        nodeWeakRefs.Add(node.Id, new WeakReference<TreeNode<K, V>>(node));

        nodeStrongRefs.Enqueue(node);

        // clean strong refs
        if (nodeStrongRefs.Count >= this.maxStrongNodeRefs)
        {
            while (nodeStrongRefs.Count >= this.maxStrongNodeRefs / 2f)
            {
                nodeStrongRefs.Dequeue();
            }
        }

        // clean weak refs
        if (this.cleanupCounter++ >= 1000)
        {
            this.cleanupCounter = 0;
            var tobeDeleted = new List<uint>();
            foreach (var kv in this.nodeWeakRefs)
            {
                TreeNode<K, V> target;
                if (kv.Value.TryGetTarget(out target) == false)
                {
                    tobeDeleted.Add(kv.Key);
                }
            }

            foreach (var key in tobeDeleted)
            {
                this.nodeWeakRefs.Remove(key);
            }
        }
    }
}