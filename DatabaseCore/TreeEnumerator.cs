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

    public TreeNode<K, V> CurrentNode
    {
        get
        {
            return currentNode;
        }
    }
    public int CurrentEntry
    {
        get
        {
            return currentEntry;
        }
    }

    public Tuple<K, V> Current
    {
        get
        {
            return current;
        }
    }

    object IEnumerator.Current
    {
        get
        {
            return (object)Current;
        }
    }

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
        switch (this.direction)
        {
            case TreeTraverseDirection.Ascending:
                return MoveForward();
            case TreeTraverseDirection.Decending:
                return MoveBackward();
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    /// <summary>
    /// Return false when node cannot move in the future
    /// </summary>
    /// <returns></returns>
    private bool MoveForward()
    {
        // move right
        currentEntry++;

        if (currentNode.IsLeaf)
        {
            while (true)
            {
                if (currentEntry < currentNode.EntriesCount)
                {
                    current = currentNode.GetEntry(currentEntry);
                    return true;
                }
                // move up when there is no space to check right
                if (currentNode.ParentId != 0)
                {
                    currentEntry = currentNode.IndexInParent();
                    currentNode = nodeManager.Find(currentNode.ParentId);

                    if ((currentEntry < 0) || (currentNode == null)) {
                        throw new Exception ("Something gone wrong with the BTree");
                    }
                }
                current = null;
                doneIterating = true;
                return false;
            }
        }
        // otherwise instance of parent node? (why couldn't this be middle nodes?)
        while (currentNode.IsLeaf == false)
        {
            currentNode = currentNode.GetChildNode(currentEntry);
            currentEntry = 0;
        }

        current = currentNode.GetEntry(currentEntry);
        return true;
    }

    private bool MoveBackward()
    {
        if (currentNode.IsLeaf)
        {
            currentEntry--;

            while (true)
            {
                if (currentEntry >= 0)
                {
                    current = currentNode.GetEntry(currentEntry);
                    return true;
                }
                // move up when there is no space to check up and left
                if (currentNode.ParentId != 0)
                {
                    currentEntry = currentNode.IndexInParent() - 1;
                    currentNode = nodeManager.Find(currentNode.ParentId);

                    if (currentNode == null) {
                        throw new Exception ("Something gone wrong with the BTree");
                    }
                }
                current = null;
                doneIterating = true;
                return false;
            }
        }

        while (currentNode.IsLeaf == false)
        {
            currentNode = currentNode.GetChildNode(currentEntry);
            currentEntry = currentNode.EntriesCount;

            if ((currentEntry < 0) || (currentNode == null))
            {
                throw new Exception("Something gone wrong with the BTree");
            }
        }

        currentEntry -= 1;
        current = currentNode.GetEntry(currentEntry);
        return true;
    }
}