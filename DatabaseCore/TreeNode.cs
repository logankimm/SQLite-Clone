using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualBasic;

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

    /// <summary>
    /// remove an entry from this node
    /// </summary>
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

            return;
        }

        // case where the node is node is not a leaf
        // replace data with largest in its left child subtree and delete the node
        TreeNode<K, V> leftLargest; int leftLargestIndex;

        var leftSubTree = nodeManager.Find(this.childrenIds[removeAt]);
        // function uses find largest since the left subtree could have a right child that is greater
        leftSubTree.FindLargest(out leftLargest, out leftLargestIndex);
        var replacementEntry = leftLargest.GetEntry(leftLargestIndex);

        // replace data
        this.entries[removeAt] = replacementEntry;
        nodeManager.MarkAsChanged(this);

        // Remove it from the node we took it from
        // don't need to call MarkAsChanged since that is done recursively
        leftLargest.Remove(leftLargestIndex);
    }

    public void InsertAsLeaf(K key, V value, int insertPosition)
    {
        Debug.Assert(IsLeaf, "Call InsertAsLeaf() on leaf only");

        entries.Insert(insertPosition, new Tuple<K, V>(key, value));
        nodeManager.MarkAsChanged(this);
    }

    public void InsertAsParent(K key, V value, uint leftReference, uint rightReference, out int insertPosition)
    {
        Debug.Assert(IsLeaf == false, "Call InsertAsParent() on non-leaf only");

        insertPosition = BinarySearchEntriesForKey(key);
        // convert -1's into 0 if entry not found
        insertPosition = insertPosition >= 0 ? insertPosition : ~insertPosition;

        entries.Insert(insertPosition, new Tuple<K, V>(key, value));

        // update children nodes - not sure why this is necessary right now
        childrenIds.Insert(insertPosition, leftReference);
        childrenIds[insertPosition + 1] = rightReference;

        nodeManager.MarkAsChanged(this);
    }

    public void Split(out TreeNode<K, V> outLeftNode, out TreeNode<K, V> outRightNode)
    {

    }

    // Search Operations
    public int BinarySearchEntriesForKey(K key)
    {
        return entries.BinarySearch(new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
    }
    public int BinarySearchEntriesForKey(K key, bool firstOccurence)
    {
        // return first instance of the found entry
        if (firstOccurence)
        {
            return entries.BinarySearchFirst(new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
        }
        // return last instance of the found entry
       return entries.BinarySearchLast(new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
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
        // merge the current node together with either rightSibling or leftSibling node
        var leftChild = rightSibling != null ? this : leftSibling;
        var rightChild = rightSibling != null ? rightSibling : this;
        var separatorParentIndex = rightSibling != null ? indexInParent : (indexInParent - 1);

        leftChild.entries.Add(parent.entries[separatorParentIndex]);
        // combine all the values together into one node - then split afterwards
        leftChild.entries.AddRange(rightChild.entries);
        leftChild.childrenIds.AddRange(rightChild.childrenIds);

        // update parentIds for the rightChild children
        foreach (var id in rightChild.childrenIds)
        {
            // doesn't this set the parentId to itself? or am i crazy - it does but for children AHHHHH
            var n = nodeManager.Find(id);
            n.parentId = leftChild.id;
            nodeManager.MarkAsChanged(n);
        }

        // update the parent node by removing the right node from entries and children
        parent.entries.RemoveAt(separatorParentIndex);
        parent.childrenIds.RemoveAt(separatorParentIndex + 1);
        nodeManager.Delete(rightChild);

        // case where the parent is the root and is not empty
        if (parent.parentId == 0 && parent.EntriesCount == 0)
        {
            // delete the current parent node and set the merged node as the parent
            leftChild.parentId = 0;
            nodeManager.MarkAsChanged(leftChild);
            nodeManager.MakeRoot(leftChild);
            nodeManager.Delete(parent);
            return;
        }

        // case where parent is NOT the root && is not empty but is missing minimum number of elements
        if (parent.parentId != 0 && parent.EntriesCount < nodeManager.MinEntriesPerNode)
        {
            nodeManager.MarkAsChanged(leftChild);
            nodeManager.MarkAsChanged(parent);
            parent.Rebalance();
            return;
        }

        nodeManager.MarkAsChanged(leftChild);
        nodeManager.MarkAsChanged(parent);
    }
}