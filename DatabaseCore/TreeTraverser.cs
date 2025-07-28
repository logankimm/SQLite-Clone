using System;
using log4net;
using System.Collections.Generic;
using System.Numerics;
using log4net.Config;

namespace DatabaseCore;

public class TreeTraverser<K, V> : IEnumerable<Tuple<K, V>>
{
    readonly TreeNode<K, V> fromNode;
    readonly int fromIndex;
    readonly TreeTraverseDirection direction;
    readonly ITreeNodeManager<K, V> nodeManager;

    public TreeTraverser (
        ITreeNodeManager<K, V> nodeManager,
        TreeNode<K, V> fromNode,
        int fromIndex,
        TreeTraverseDirection direction
    )
    {
        if (fromNode == null)
        {
            throw new ArgumentNullException("fromNode");
        }

        this.direction = direction;
        this.fromIndex = fromIndex;
        this.fromNode = fromNode;
        this.nodeManager = nodeManager; 
    }

    IEnumerator<Tuple<K, V>> IEnumerable<Tuple<K, V>>.GetEnumerator()
    {
        return new TreeEnumerator<K, V>(nodeManager, fromNode, fromIndex, direction);
    }
}