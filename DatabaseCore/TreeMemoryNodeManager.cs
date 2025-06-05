using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DatabaseCore;

public class TreeMemoryNodeManager<K, V> : ITreeNodeManager<K, V>
{
    readonly Dictionary<uint, TreeNode<K, V>> nodes = new Dictionary<uint, TreeNode<K, V>>();
    readonly ushort minEntriesCountPerNode;
    readonly IComparer<K> keyComparer;
    readonly IComparer<Tuple<K, V>> entryComparer;
    int idCounter = 1;
    TreeNode<K, V> rootNode;

    public ushort MinEntriesPerNode
    {
        get
        {
            return minEntriesCountPerNode;
        }
    }
    public IComparer<K> KeyComparer
    {
        get
        {
            return keyComparer;
        }
    }
    
    public IComparer<Tuple<K, V>> EntryComparer
    {
        get
        {
            return entryComparer;
        }
    }
    
    public TreeNode<K, V> RootNode
    {
        get
        {
            return rootNode;
        }
    }

    public TreeMemoryNodeManager(ushort minEntriesCountPerNode, IComparer<K> keyComparer)
    {
        this.keyComparer = keyComparer;
        this.entryComparer = Comparer<Tuple<K, V>>.Create((t1, t2) =>
            {
                return this.keyComparer.Compare(t1.Item1, t2.Item1);
            });
        this.minEntriesCountPerNode = minEntriesCountPerNode;
        this.rootNode = Create(null, null);
    }

    public TreeNode<K, V> Create(IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
    {
        TreeNode<K, V> newNode = new TreeNode<K, V>(this,
            (uint)this.idCounter++,
            0,
            entries,
            childrenIds
        );

        // this could be a pointer value instead of the actual value?
        nodes[newNode.Id] = newNode;

        return newNode;
    }

    public TreeNode<K, V> CreateNewRoot(K key, V value, uint leftNodeId, uint rightNodeId)
    {
        TreeNode<K, V> newNode = Create(new Tuple<K, V>[] {new Tuple<K, V>(key, value)},
            new uint[] { leftNodeId, rightNodeId }
        );

        this.rootNode = newNode;
        return newNode;
    }

    public TreeNode<K, V> Find(uint id)
    {
        if (nodes.ContainsKey(id) == false)
        {
            throw new ArgumentException("Node not found by id: " + id);
        }

        return nodes[id];
    }

    public void Delete(TreeNode<K, V> node)
    {
        if (node == RootNode)
        {
            this.rootNode = null;
        }
        if (nodes.ContainsKey(node.Id))
        {
            nodes.Remove(node.Id);
        }
    }

    public void MakeRoot(TreeNode<K, V> node)
    {
        this.rootNode = node;
    }

    public void MarkAsChanged(TreeNode<K, V> node)
    {
        // code does nothing?? = why?
    }

    public void SaveChanges()
    {
        // also does nothing?
    }
}