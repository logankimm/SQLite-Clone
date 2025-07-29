using System;
using log4net;
using System.Collections.Generic;
using System.Numerics;
using log4net.Filter;

namespace DatabaseCore;

public class Tree<K, V> : IIndex<K, V>
{
    readonly ITreeNodeManager<K, V> nodeManager;
    readonly bool allowDuplicateKeys;

    public Tree(ITreeNodeManager<K, V> nodeManager, bool allowDuplicateKeys = false)
    {
        if (nodeManager == null)
        {
            throw new ArgumentNullException("nodeManager does not exist");
        }
        this.nodeManager = nodeManager;
        this.allowDuplicateKeys = allowDuplicateKeys;
    }

    public void Insert(K key, V value)
    {
        // find index for insertion
        int insertionIndex = 0;
        var leafNode = FindNodeForInsertion(key, ref insertionIndex);

        if (insertionIndex >= 0 && allowDuplicateKeys == false) 
        {
            throw new TreeKeyExistsException(key);
        }

        leafNode.InsertAsLeaf(key, value, insertionIndex >= 0 ? insertionIndex : ~insertionIndex);

        if (leafNode.IsOverflow)
        {
            TreeNode<K, V> left, right;
            leafNode.Split(out left, out right);
        }

        nodeManager.SaveChanges();
    }

    public Tuple<K, V> Get(K key)
    {
        var insertionIndex = 0;
        var node = FindNodeForInsertion(key, ref insertionIndex);
        if (insertionIndex < 0)
        {
            return null;
        }
        return node.GetEntry(insertionIndex);
    }

    /// <summary>
    /// Delete all instances of a specified entry of a key
    /// </summary>
    public bool Delete(K key, V value, IComparer<V> valueComparer = null)
    {
        if (allowDuplicateKeys == true)
        {
            throw new InvalidOperationException("This method should be called only from unique tree");
        }

        valueComparer = valueComparer == null ? Comparer<V>.Default : valueComparer;

        var deleted = false;
        var shouldContinue = true;

        try
        {
            while (shouldContinue)
            {
                // recreate a new enumerator each time in case tree rebalances
                using var enumerator = (TreeEnumerator<K, V>)LargerThanOrEqualTo(key).GetEnumerator();
                {
                    while (true)
                    {
                        if (enumerator.MoveNext() == false)
                        {
                            shouldContinue = false;
                            break;
                        }

                        var entry = enumerator.Current;

                        // stop searching after finding non-equal element
                        if (nodeManager.KeyComparer.Compare(entry.Item1, key) > 0)
                        {
                            shouldContinue = false;
                            break;
                        }

                        // case of match
                        if (valueComparer.Compare(entry.Item2, value) == 0)
                        {
                            enumerator.CurrentNode.Remove(enumerator.CurrentEntry);
                            deleted = true;
                            break;
                        }
                    }
                }   
            }
        }
        catch (EndEnumeratingException)
        {

        }

        nodeManager.SaveChanges ();
        return deleted;
    }

    private class EndEnumeratingException : Exception { }

    /// <summary>
    /// Delete a specified entry of a specific key
    /// </summary>
    public bool Delete(K key)
    {
        if (allowDuplicateKeys == true)
        {
            throw new InvalidOperationException("This method should be called only from unique tree");
        }

        using var enumerator = (TreeEnumerator<K, V>)LargerThanOrEqualTo(key).GetEnumerator();
        {
            // MoveNext returns true if a match has been found
            if (enumerator.MoveNext() && (nodeManager.KeyComparer.Compare(enumerator.Current.Item1, key) == 0))
            {
                enumerator.CurrentNode.Remove(enumerator.CurrentEntry);
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Tuple<K, V>> LargerThanOrEqualTo(K key)
    {
        var startIterationIndex = 0;
        var node = this.FindNodeForIteration(key, this.nodeManager.RootNode, true, ref startIterationIndex);

        return new TreeTraverser<K, V> (
            nodeManager,
            node,
            (startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex) - 1,
            TreeTraverseDirection.Ascending
        );
    }

    public IEnumerable<Tuple<K, V>> LargerThan(K key)
    {
        var startIterationIndex = 0;
        var node = this.FindNodeForIteration(key, this.nodeManager.RootNode, false, ref startIterationIndex);

        return new TreeTraverser<K, V> (
            nodeManager,
            node,
            startIterationIndex >= 0 ? startIterationIndex : (~startIterationIndex - 1),
            TreeTraverseDirection.Ascending
        );
    }

    public IEnumerable<Tuple<K, V>> LessThanOrEqualTo(K key)
    {
        var startIterationIndex = 0;
        var node = this.FindNodeForIteration(key, this.nodeManager.RootNode, false, ref startIterationIndex);

        return new TreeTraverser<K, V> (
            nodeManager,
            node,
            startIterationIndex >= 0 ? startIterationIndex + 1 : ~startIterationIndex,
            TreeTraverseDirection.Decending
        );
    }

    public IEnumerable<Tuple<K, V>> LessThan(K key)
    {
        var startIterationIndex = 0;
        var node = this.FindNodeForIteration(key, this.nodeManager.RootNode, false, ref startIterationIndex);

        return new TreeTraverser<K, V> (
            nodeManager,
            node,
            startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex,
            TreeTraverseDirection.Decending
        );
    }

    /// <summary>
    /// recursive function similar to FindNodeForInseration but for duplicate keys
    /// </summary>
    /// <param name="moveLeft">Decide whether to move left or right for duplicate keys</param>
    /// <returns></returns>
    private TreeNode<K, V> FindNodeForIteration(K key, TreeNode<K, V> node, bool moveLeft, ref int startIterationIndex)
    {
        // base case where node is empty (think only for parent root node)
        if (node.IsEmpty)
        {
            startIterationIndex = ~0;
            return node;
        }

        var binarySearchResult = node.BinarySearchEntriesForKey(key, moveLeft);

        // case where exact match for entry is found
        if (binarySearchResult >= 0)
        {
            // check if node is a leaf node and return found index
            if (node.IsLeaf)
            {
                startIterationIndex = binarySearchResult;
                return node;
            }

            return FindNodeForIteration(key, node.GetChildNode(moveLeft ? binarySearchResult : binarySearchResult + 1), moveLeft, ref startIterationIndex);
        }
        // keep searching if exact match is not fund
        if (node.IsLeaf == false)
        {
            return FindNodeForIteration(key, node.GetChildNode(~binarySearchResult), moveLeft, ref startIterationIndex);
        }
        // last case this is a leaf node and no valid entries are found, return this and bitwise complement
        startIterationIndex = binarySearchResult;
        return node;
    }
    
    ///
    /// Private Methods
    /// 

    /// <summary>
    /// search for a node that contains a certain key starting from a node
    /// <summary>
    private TreeNode<K, V> FindNodeForInsertion(K key, TreeNode<K, V> node, ref int insertionIndex)
    {
        // return straight away if the node is empty (this should always be a non-full root node)
        // returns bitwise complement instead of 0 index to indicate empty node - not sure why this is necessary
        if (node.IsEmpty)
        {
            insertionIndex = ~0;
            return node;
        }

        var binarySearchResult = node.BinarySearchEntriesForKey(key);
        // check if a valid result has been find
        if (binarySearchResult >= 0)
        {
            if (allowDuplicateKeys && node.IsLeaf == false)
            {
                return FindNodeForInsertion(key, node.GetChildNode(binarySearchResult), ref insertionIndex);
            }
            else
            {
                insertionIndex = binarySearchResult;
                return node;
            }
        }

        // keep searching inside children nodes
        if (node.IsLeaf == false)
        {
            return FindNodeForInsertion(key, node.GetChildNode(~binarySearchResult), ref insertionIndex);
        }

        // current node is leaf node and no children to search
        insertionIndex = binarySearchResult;
        return node;
    }

    /// <summary>
    /// SEarch for the node that contains given key, starting from the root node
    /// </summary>
    private TreeNode<K, V> FindNodeForInsertion(K key, ref int insertionIndex)
    {
        return FindNodeForInsertion(key, nodeManager.RootNode, ref insertionIndex);
    }
}