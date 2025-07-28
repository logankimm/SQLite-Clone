using System;
using System.Collections.Generic;
using System.Collections;

namespace DatabaseCore;

public class TreeEnumerator<K, V> : IEnumerator<Tuple<K, V>>
{
    readonly ITreeNodeManager<K, V> nodeManager;
    readonly TreeTraverseDirection direction;
    bool doneIterating = false;
    int currentEntry = 0;
    TreeNode<K, V> currentNode;
    Tuple<K, V> current;

    public TreeEnumerator (
        ITreeNodeManager<K, V> nodeManager,
        TreeNode<K, V> node,
        int fromIndex,
        TreeTraverseDirection direction
    )
    {
        this.direction = direction;
        this.currentNode = node;
        this.currentEntry = fromIndex;
        this.nodeManager = nodeManager; 
    }

    public bool MoveNext()
    {
        if (doneIterating)
        {
            return false;
        }
        return true;
    }
}