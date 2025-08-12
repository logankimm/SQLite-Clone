using DatabaseCore;
using NUnit.Framework;

namespace DatabaseTest;

[TestFixture]
public class BTreeDeletionTest()
{
    [Test]
    public void NonFullRootNodeTest()
    {
        var tree = new Tree<double, string>
        (
            new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default)
        );

        tree.Insert(1, "1");
        tree.Insert(3, "3");
        tree.Insert(6, "6");
        tree.Insert(8, "8");

        tree.Delete(6);
        Assert.That(new int[] {1, 3, 8}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(0) select node.Item1));

        tree.Delete(3);
        Assert.That(new int[] {1, 8}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(0) select node.Item1));
        
        tree.Delete(1);
        Assert.That(new int[] {8}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(0) select node.Item1));
        
        tree.Delete(8);
        Assert.That(new int[] {}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(0) select node.Item1));
    }

    [Test]
    public void UniqueTreeTest()
    {
        var expectedNums = new List<double>();
        var tree = new Tree<double, string>
        (
            new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default)
        );

        for (var i = 0; i < 1000; i++)
        {
            tree.Insert(i, i.ToString());
            expectedNums.Add(i);
        }

        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {   
            var deleteIndex = random.Next(0, expectedNums.Count);
            var keyToDelete = expectedNums[deleteIndex];

            expectedNums.RemoveAt(deleteIndex);
            tree.Delete(keyToDelete);

            var treeNums = (from entry in tree.LargerThanOrEqualTo(0) select entry.Item1).ToArray();
            Assert.That(treeNums, Is.EqualTo(expectedNums));
        }

        Assert.Throws<InvalidOperationException>(() =>
            tree.Delete(888, "888")
        );
    }
    
    [Test]
    public void NonUniqueTreeTestWithNoDuplicateKey()
    {
        var expectedNums = new List<double>();
        var tree = new Tree<double, string>
        (
            new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
            true
        );

        for (var i = 0; i < 1000; i++)
        {
            tree.Insert(i, i.ToString());
            expectedNums.Add(i);
        }

        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {   
            var deleteIndex = random.Next(0, expectedNums.Count);
            var keyToDelete = expectedNums[deleteIndex];

            expectedNums.RemoveAt(deleteIndex);
            tree.Delete(keyToDelete, keyToDelete.ToString());

            var treeNums = (from entry in tree.LargerThanOrEqualTo(0) select entry.Item1).ToArray();
            Assert.That(treeNums, Is.EqualTo(expectedNums));
        }
    }

    [Test]
    public void NonUniqueTreeTestWithDuplicateKeys()
    {
        var expectedNums = new List<Tuple<double, string>>();
        var tree = new Tree<double, string>
        (
            new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
            true
        );

        tree.Insert(1, "A");
        tree.Insert(1, "B");
        tree.Insert(1, "C");
        tree.Insert(1, "D");
        tree.Insert(1, "E");
        tree.Insert(1, "F");
        tree.Insert(1, "G");
        tree.Insert(1, "H");

        tree.Delete(1, "E");
        // values must be sorted or else they will be in random order ("F", "H", "G", "C", "D", "B", "A")
        var treeValues = (from entry in tree.LargerThanOrEqualTo(0) orderby entry.Item1, entry.Item2 select entry.Item2).ToArray();
        Assert.That(treeValues, Is.EqualTo(new List<string>{"A", "B", "C", "D", "F", "G", "H" }));

        tree.Delete(1, "A");
        tree.Delete(1, "B");
        tree.Delete(1, "C");
        tree.Delete(1, "D");
        tree.Delete(1, "F");
        tree.Delete(1, "G");
        tree.Delete(1, "H");

        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {
            tree.Insert(i, "A");
            expectedNums.Add(new Tuple<double, string>(i, "A"));

            var dupCount = random.Next(0, 3);
            for (var t = 0; t < dupCount; t++)
            {
                if (t == 0)
                {
                    tree.Insert(i, "B");
                    expectedNums.Add(new Tuple<double, string>(i, "B"));
                }
                else if (t == 1)
                {
                    tree.Insert(i, "C");
                    expectedNums.Add(new Tuple<double, string>(i, "C"));
                }
            }
        }

        for (var i = 0; i < 1000; i++)
        {
            var deleteIndex = random.Next(0, expectedNums.Count);
            var keyToDelete = expectedNums[deleteIndex];

            expectedNums.RemoveAt(deleteIndex);
            tree.Delete(keyToDelete.Item1, keyToDelete.Item2);

            var treeValues2 = (from entry in tree.LargerThanOrEqualTo(0) orderby entry.Item1, entry.Item2 select entry).ToArray();
            Assert.That(treeValues2, Is.EqualTo(expectedNums));
        }
    }
}