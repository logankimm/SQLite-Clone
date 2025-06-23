using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DatabaseCore;

public class TreeNode<K, V>
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


        if (IsLeaf)
        {
            entries.RemoveAt(removeAt);
            nodeManager.MarkAsChanged(this);

            // check if minimum number of entries is met and adjust if so
            if (EntriesCount >= nodeManager.MinEntriesPerNode || parentId == 0)
            {
                return;
            }
            else
            {
                this.Rebalance();
            }
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

    public void Split(out TreeNode<K, V> outLeftNode, out TreeNode<K, V> outRightNode)
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

    /// <summary>
    /// Find largest node on the subtree
    /// </summary>
    public void FindLargest(out TreeNode<K, V> node, out int index)
    {
        if (IsLeaf == true)
        {
            node = this;
            index = entries.Count - 1;
            return;
        }

        // recursive formula to find it
        var rightMostNode = nodeManager.Find(this.childrenIds[this.childrenIds.Count - 1]);
        rightMostNode.FindLargest(out node, out index);
    }

    public void FindSmallest(out TreeNode<K, V> node, out int index)
    {
        if (IsLeaf == true)
        {
            node = this;
            index = 0;
            return;
        }

        // recursive formula to find it
        var leftMostNode = nodeManager.Find(this.childrenIds[0]);
        leftMostNode.FindSmallest(out node, out index);
    }


    /// <summary>
    /// Get this node's index in its parent's childrenIds
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
    public TreeNode<K, V> GetChildNode(int atIndex)
    {
        return nodeManager.Find(childrenIds[atIndex]);
    }
    public Tuple<K, V> GetEntry(int atIndex)
    {
        return entries[atIndex];
    }
    public bool EntryExists(int atIndex)
    {
        return atIndex < entries.Count;
    }



    //
    // Private Methods
    //

    /// <summary>
    /// Rebalance this node after an element has been removed causing it to underflow
    /// </summary>
    private void Rebalance()
    {
        // check if the right sibling exists and add a node to the left child
        var indexInParent = IndexInParent();
        var parent = nodeManager.Find(ParentId);
        var rightSibling = ((indexInParent + 1) < parent.ChildrenNodeCount) ? parent.GetChildNode(indexInParent + 1) : null;
        if ((rightSibling != null) && (rightSibling.EntriesCount > nodeManager.MinEntriesPerNode))
        {
            // copy the corresponding right-entry in parent node and add it
            entries.Add(parent.GetEntry(indexInParent));
            // then make the right node the new parent entry
            parent.entries[indexInParent] = rightSibling.Entries[0];
            rightSibling.entries.RemoveAt(0);

            if (rightSibling.IsLeaf == false)
            {
                // update the children of the moved node - the child node being moved will always be
                // original parent node < X (child node being moved) < rightSibling[0]
                var rightSiblingChild = nodeManager.Find(rightSibling.childrenIds[0]);
                rightSiblingChild.parentId = this.id;
                nodeManager.MarkAsChanged(rightSiblingChild);

                // add the moved child node to current entries (since that is what's updating)
                childrenIds.Add(rightSibling.childrenIds[0]);
                rightSibling.childrenIds.RemoveAt(0);
            }

            nodeManager.MarkAsChanged(this);
            nodeManager.MarkAsChanged(parent);
            nodeManager.MarkAsChanged(rightSibling);
            return;
        }

        // rotate from the left to the right child
        var leftSibling = ((indexInParent - 1) < parent.ChildrenNodeCount) ? parent.GetChildNode(indexInParent - 1) : null;
        if ((leftSibling != null) && (leftSibling.EntriesCount > nodeManager.MinEntriesPerNode))
        {
            entries.Insert(0, parent.GetEntry(indexInParent - 1));
            // replaces the node with the leftSiblings replacement and removes it
            parent.entries[indexInParent - 1] = leftSibling.Entries[leftSibling.entries.Count - 1];
            leftSibling.entries.RemoveAt(leftSibling.entries.Count - 1);
            // if left sibling has children, move its children to the current node
            if (leftSibling.IsLeaf == false)
            {
                var leftSiblingChild = nodeManager.Find(leftSibling.ChildrenIds[leftSibling.childrenIds.Count - 1]);
                leftSiblingChild.parentId = this.id;
                nodeManager.MarkAsChanged(leftSiblingChild);

                childrenIds.Insert(0, leftSibling.childrenIds[leftSibling.childrenIds.Count - 1]);
                leftSiblingChild.childrenIds.RemoveAt(leftSibling.childrenIds.Count - 1);
            }

            nodeManager.MarkAsChanged(this);
            nodeManager.MarkAsChanged(parent);
            nodeManager.MarkAsChanged(leftSibling);
            return;
        }

        // last situation where both siblings can't move
        // merge the siblings together into a sibling sandwich
    }
}