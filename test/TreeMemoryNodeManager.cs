using System;
using System.Collections.Generic;

namespace DatabaseCore;

public class TreeMemoryNodeManager<K, V> : ITreeNodeManager<K, V>
{
    readonly Dictionary<uint, TreeNode<K, V>> nodes = new Dictionary<uint, TreeNode<K, V>>();
}