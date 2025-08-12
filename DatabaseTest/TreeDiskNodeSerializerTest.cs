using System.Diagnostics.Tracing;
using DatabaseCore;
using log4net.Config;
using NUnit.Framework;

namespace DatabaseTest;

[TestFixture]
public class TreeDiskNodeSerializerTest()
{
    [Test]
    public void TestVariableLengthKeySize()
    {
        var nodeManager = new TreeDiskNodeManager<string, long>
        (
            new TreeStringSerializer(),
            new TreeLongSerializer(),
            new RecordStorage
            (
                new BlockStorage
                (
                    new MemoryStream(),
                    4096,
                    48
                )
            )
        );
        var node = new TreeNode<string, long>
        (
            nodeManager, 11, 88, new List<Tuple<string, long>>
            {
                new Tuple<string, long>("The quick brown foxs run over the lazy dog", 22L),
                new Tuple<string, long>("Testing string please work please please please", 33L),
                new Tuple<string, long>(String.Empty, 55L)
            },
            new List<uint>
            {
                111, 222, 333, 444, 555
            }
        );
        var serializer = new TreeDiskNodeSerializer<string, long>
        (
            nodeManager,
            new TreeStringSerializer(),
            new TreeLongSerializer()
        );
        var data = serializer.Serialize(node);
        var node2 = serializer.Deserialize(11, data);

        Assert.That(node2, Is.Not.Null);
        Assert.That(node.Id, Is.EqualTo(node2.Id));
        Assert.That(node.ParentId, Is.EqualTo(node2.ParentId));
        Assert.That(node.Entries, Is.EqualTo(node2.Entries));
        Assert.That(node.ChildrenIds, Is.EqualTo(node2.ChildrenIds));
    }

    [Test]
    public void TestEmptyNodeFixedSize()
    {
        var nodeManager = new TreeDiskNodeManager<int, long>
        (
            new TreeIntSerializer(),
            new TreeLongSerializer(),
            new RecordStorage
            (
                new BlockStorage
                (
                    new MemoryStream(),
                    4096,
                    48
                )
            )
        );
        var node = new TreeNode<int, long>(nodeManager, 11, 88);
        var serializer = new TreeDiskNodeSerializer<int, long>
        (
            nodeManager,
            new TreeIntSerializer(),
            new TreeLongSerializer()
        );
        var data = serializer.Serialize(node);
        var node2 = serializer.Deserialize(11, data);

        Assert.That(node2, Is.Not.Null);
        Assert.That(node.Id, Is.EqualTo(node2.Id));
        Assert.That(node.ParentId, Is.EqualTo(node2.ParentId));
        Assert.That(node.Entries, Is.EqualTo(node2.Entries));
        Assert.That(node.ChildrenIds, Is.EqualTo(node2.ChildrenIds));
    }
    
    [Test]
    public void TestNonEmptyNodeFixedSize()
    {
        var nodeManager = new TreeDiskNodeManager<int, long>
        (
            new TreeIntSerializer(),
            new TreeLongSerializer(),
            new RecordStorage
            (
                new BlockStorage
                (
                    new MemoryStream(),
                    4096,
                    48
                )
            )
        );
        var node = new TreeNode<int, long>
        (
            nodeManager, 11, 88, new List<Tuple<int, long>>
            {
                new Tuple<int, long>(11, 22L),
                new Tuple<int, long>(22, 33L),
                new Tuple<int, long>(44, 55L)
            },
            new List<uint>
            {
                111, 222, 333, 444, 555
            }
        );
        var serializer = new TreeDiskNodeSerializer<int, long>
        (
            nodeManager,
            new TreeIntSerializer(),
            new TreeLongSerializer()
        );
        var data = serializer.Serialize(node);
        var node2 = serializer.Deserialize(11, data);
        
        Assert.That(node2, Is.Not.Null);
        Assert.That(node.Id, Is.EqualTo(node2.Id));
        Assert.That(node.ParentId, Is.EqualTo(node2.ParentId));
        Assert.That(node.Entries, Is.EqualTo(node2.Entries));
        Assert.That(node.ChildrenIds, Is.EqualTo(node2.ChildrenIds));
    }
}