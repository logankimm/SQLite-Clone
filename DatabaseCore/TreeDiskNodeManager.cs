using System;
using log4net;
using System.Collections.Generic;
using System.Numerics;
using log4net.Filter;

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
    }

    TreeNode<K, V> ITreeNodeManager<K, V>.Create(IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
    {
        throw new NotImplementedException();
    }

    TreeNode<K, V> ITreeNodeManager<K, V>.Find(uint id)
    {
        throw new NotImplementedException();
    }

    TreeNode<K, V> ITreeNodeManager<K, V>.CreateNewRoot(K key, V value, uint leftNodeId, uint rightNodeId)
    {
        throw new NotImplementedException();
    }

    void ITreeNodeManager<K, V>.MakeRoot(TreeNode<K, V> node)
    {
        throw new NotImplementedException();
    }

    void ITreeNodeManager<K, V>.MarkAsChanged(TreeNode<K, V> node)
    {
        throw new NotImplementedException();
    }

    void ITreeNodeManager<K, V>.Delete(TreeNode<K, V> node)
    {
        throw new NotImplementedException();
    }

    void ITreeNodeManager<K, V>.SaveChanges()
    {
        throw new NotImplementedException();
    }
}