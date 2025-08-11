using System;
using System.Collections;
using System.Collections.Generic;

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
        ArgumentNullException.ThrowIfNull(fromNode, nameof(fromNode));

        this.direction = direction;
        this.fromIndex = fromIndex;
        this.fromNode = fromNode;
        this.nodeManager = nodeManager;
    }

    IEnumerator<Tuple<K, V>> IEnumerable<Tuple<K, V>>.GetEnumerator()
    {
        return new TreeEnumerator<K, V>(nodeManager, fromNode, fromIndex, direction);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<Tuple<K, V>>)this).GetEnumerator();
    }
}