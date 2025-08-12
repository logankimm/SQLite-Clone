using DatabaseCore;
using log4net.Config;
using NUnit.Framework;

namespace DatabaseTest;

[TestFixture]
public class PersistentTreeTest
{
    [Test]
    public void TestSavingEmptyTree()
    {
        // create a tree with random elements then persist in memory stream to recreate
        BasicConfigurator.Configure();

        var stream = new MemoryStream();
        var nodeManager = new TreeDiskNodeManager<int, long>
        (
            new TreeIntSerializer(),
            new TreeLongSerializer(),
            new RecordStorage
            (
                new BlockStorage
                (
                    stream,
                    4096,
                    48
                )
            )
        );
        var tree = new Tree<int, long>(nodeManager);

        stream.Position = 0;
        var nodeManager2 = new TreeDiskNodeManager<int, long>
        (
            new TreeIntSerializer(),
            new TreeLongSerializer(),
            new RecordStorage
            (
                new BlockStorage
                (
                    stream,
                    4096,
                    48
                )
            )
        );
        var tree2 = new Tree<int, long>(nodeManager2);
        var result = (from i in tree2.LargerThanOrEqualTo(0) select i).ToList();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TestSavingFullTree()
    {
        // same as previous but fill it
        BasicConfigurator.Configure();

        var stream = new MemoryStream();
        var tree = new Tree<int, long>
        (
            new TreeDiskNodeManager<int, long>
            (
                new TreeIntSerializer(),
                new TreeLongSerializer(),
                new RecordStorage
                (
                    new BlockStorage
                    (
                        stream,
                        4096,
                        48
                    )
                )
            ),
            true
        );

        var sampleData = new List<Tuple<int, long>>();
        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {
            sampleData.Add(new Tuple<int, long>
            (
                random.Next(Int32.MinValue, Int32.MaxValue),
                (long)random.Next(Int32.MinValue, Int32.MaxValue)
            ));
        }

        // tree insertion
        foreach (var data in sampleData)
        {
            tree.Insert(data.Item1, data.Item2);

            var tree2 = new Tree<int, long>
            (
                new TreeDiskNodeManager<int, long>
                (
                    new TreeIntSerializer(),
                    new TreeLongSerializer(),
                    new RecordStorage
                    (
                        new BlockStorage
                        (
                            stream,
                            4096,
                            48
                        )
                    )
                ),
                true
            );
            var actual = (from i in tree2.LargerThanOrEqualTo(Int32.MinValue) select i).ToList();
            var expected = (from i in tree.LargerThanOrEqualTo(Int32.MinValue) select i).ToList();
            Assert.That(actual, Is.EqualTo(expected));
        }

        for (var i = 0; i < sampleData.Count; i++)
        {
            var deleteIndex = random.Next(0, sampleData.Count);
            var deleteKey = sampleData[deleteIndex];
            sampleData.RemoveAt(deleteIndex);
            tree.Delete(deleteKey.Item1, deleteKey.Item2);

            // create tree to see if changes to stream are affected
            var tree2 = new Tree<int, long>
            (
                new TreeDiskNodeManager<int, long>
                (
                    new TreeIntSerializer(),
                    new TreeLongSerializer(),
                    new RecordStorage
                    (
                        new BlockStorage
                        (
                            stream,
                            4096,
                            48
                        )
                    )
                ),
                true
            );
            var actual = (from ii in tree2.LargerThanOrEqualTo(Int32.MinValue) select ii).ToList();
            var expected = (from ii in tree.LargerThanOrEqualTo(Int32.MinValue) select ii).ToList();
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}