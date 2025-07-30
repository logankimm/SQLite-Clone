using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace DatabaseCore;

public sealed class TreeDiskNodeSerializer<K, V>
{
    ISerializer<K> keySerializer;
    ISerializer<V> valueSerializer;
    ITreeNodeManager<K, V> nodeManager;

    public TreeDiskNodeSerializer (
        ITreeNodeManager<K, V> nodeManager,
        ISerializer<K> keySerializer,
        ISerializer<V> valueSerializer
    )
    {
        ArgumentNullException.ThrowIfNull(nodeManager, nameof(nodeManager));
        ArgumentNullException.ThrowIfNull(valueSerializer, nameof(valueSerializer));
        ArgumentNullException.ThrowIfNull(keySerializer, nameof(keySerializer));

        this.nodeManager = nodeManager;
        this.keySerializer = keySerializer;
        this.valueSerializer = valueSerializer;
    }

    public byte[] Serialize (TreeNode<K, V> node)
    {
        if (keySerializer.IsFixedSize && valueSerializer.IsFixedSize)
        {
            return FixedLengthSerialize(node);
        }
        if (valueSerializer.IsFixedSize)
        {
            return VariableKeyLengthSerialize(node);
        }
        // for variable length information
        throw new NotSupportedException ();
    }

    public TreeNode<K, V> Deserialize(uint assignId, byte[] record)
    {
        if (keySerializer.IsFixedSize && valueSerializer.IsFixedSize)
        {
            return FixedLengthDeserialize(assignId, record);
        }
        if (valueSerializer.IsFixedSize)
        {
            return VariableKeyLengthDeserialize(assignId, record);
        }
        // for variable length information
        throw new NotSupportedException ();
    }

    private byte[] FixedLengthSerialize(TreeNode<K, V> node)
    {
        var entrySize = this.keySerializer.Length + this.valueSerializer.Length;
        var size = 16 + node.Entries.Length * entrySize + node.ChildrenIds.Length * 4;

        if (size >= 1024 * 64)
        {
            throw new Exception("Serialized node too large: " + size);
        }
        var buffer = new byte[size];

        // first 4 bytes is parent node id
        BufferHelper.WriteBuffer(node.ParentId, buffer, 0);
        // 4 bytes of entries length
        BufferHelper.WriteBuffer((uint)node.EntriesCount, buffer, 4);
        // 4 bytes of children length/references
        BufferHelper.WriteBuffer((uint)node.ChildrenNodeCount, buffer, 8);

        // writing entries and children references
        for (var i = 0; i < node.EntriesCount; i++)
        {
            var entry = node.GetEntry(i);
            Buffer.BlockCopy(this.keySerializer.Serialize(entry.Item1), 0, buffer, 12 + i * entrySize, this.keySerializer.Length);
            Buffer.BlockCopy(this.valueSerializer.Serialize(entry.Item2), 0, buffer, 12 + i * entrySize + this.keySerializer.Length, this.valueSerializer.Length);
        }

        var childrenIds = node.ChildrenIds;
        for (var i = 0; i < node.ChildrenNodeCount; i++)
        {
            BufferHelper.WriteBuffer(childrenIds[i], buffer, 12 + entrySize * node.EntriesCount + (i * 4));
        }

        return buffer;
    }

    private byte[] VariableKeyLengthSerialize(TreeNode<K, V> node)
    {
        using (var m = new MemoryStream())
        {
            m.Write(LittleEndianByteOrder.GetBytes((uint)node.ParentId), 0, 4);
            m.Write(LittleEndianByteOrder.GetBytes((uint)node.EntriesCount), 0, 4);
            m.Write(LittleEndianByteOrder.GetBytes((uint)node.ChildrenNodeCount), 0, 4);

            // writing entries
            for (var i = 0; i < node.EntriesCount; i++)
            {
                var entry = node.GetEntry(i);
                var key = this.keySerializer.Serialize(entry.Item1);
                var value = this.valueSerializer.Serialize(entry.Item2);

                m.Write(LittleEndianByteOrder.GetBytes((int)key.Length), 0, 4);
                m.Write(key, 0, key.Length);
                m.Write(value, 0, value.Length);
            }

            // writing children
            var childrenIds = node.ChildrenIds;
            for (var i = 0; i < node.ChildrenNodeCount; i++)
            {
                m.Write(LittleEndianByteOrder.GetBytes((int)childrenIds[i]), 0, 4);
            }

            return m.ToArray();
        }
    }
    
    private TreeNode<K, V> FixedLengthDeserialize(uint assignId, byte[] buffer)
    {
        var entrySize = this.keySerializer.Length + this.valueSerializer.Length;

        var parentId = BufferHelper.ReadBufferUInt32(buffer, 0);
        var entriesCount = BufferHelper.ReadBufferUInt32(buffer, 4);
        var childrenCount = BufferHelper.ReadBufferUInt32(buffer, 8);

        // deserialize entries
        var entries = new Tuple<K, V>[entriesCount];
        for (var i = 0; i < entriesCount; i++)
        {
            var key = this.keySerializer.Deserialize(
                buffer,
                12 + i * entrySize,
                this.keySerializer.Length
            );
            var value = this.valueSerializer.Deserialize(
                buffer,
                12 + i * entrySize + this.keySerializer.Length,
                this.valueSerializer.Length
            );
            entries[i] = new Tuple<K, V>(key, value);
        }

        // deserialize children
        var children = new uint[childrenCount];
        for (var i = 0; i < childrenCount; i++)
        {
            children[i] = BufferHelper.ReadBufferUInt32(buffer, (int)(12 + entrySize * entriesCount + (i * 4)));
        }

        return new TreeNode<K, V>(nodeManager, assignId, parentId, entries, children);
    }

    private TreeNode<K, V> VariableKeyLengthDeserialize(uint assignId, byte[] buffer)
    {
        var parentId = BufferHelper.ReadBufferUInt32(buffer, 0);
        var entriesCount = BufferHelper.ReadBufferUInt32(buffer, 4);
        var childrenCount = BufferHelper.ReadBufferUInt32(buffer, 8);

        var entries = new Tuple<K, V>[entriesCount];
        // what the fuck is p????? - position within buffer
        var p = 12;
        for (var i = 0; i < entriesCount; i++)
        {
            var keyLength = BufferHelper.ReadBufferInt32(buffer, p);
            var key = this.keySerializer.Deserialize(
                buffer,
                p + 4,
                keyLength
            );
            var value = this.valueSerializer.Deserialize(
                buffer,
                p + 4 + keyLength,
                this.valueSerializer.Length
            );
            entries[i] = new Tuple<K, V>(key, value);

            p += 4 + keyLength + valueSerializer.Length;
        }

        var children = new uint[childrenCount];
        for (var i = 0; i < childrenCount; i++)
        {
            children[i] = BufferHelper.ReadBufferUInt32(buffer, (int)(p + (i * 4)));
        }

        return new TreeNode<K, V>(nodeManager, assignId, parentId, entries, children);
    }
}