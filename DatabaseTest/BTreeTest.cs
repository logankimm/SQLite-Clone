using System.Reflection.Metadata;
using DatabaseCore;
using NUnit.Framework;

namespace DatabaseTest;

[TestFixture]
public class BTreeTest
{
    [Test]
    public void EmptyTreeTest()
    {
        var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

        Assert.That(0, Is.EqualTo((from node in tree.LargerThan(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LargerThanOrEqualTo(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LessThan(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LessThanOrEqualTo(7) select node).Count()));
    }

    [Test]
    public void NonFullRootNodeTest()
    {
        var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

        tree.Insert(1, "1");

        Assert.That(0, Is.EqualTo((from node in tree.LargerThan(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LargerThanOrEqualTo(7) select node).Count()));
        Assert.That(1, Is.EqualTo((from node in tree.LessThan(7) select node).Count()));
        Assert.That(1, Is.EqualTo((from node in tree.LessThanOrEqualTo(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LessThan(1) select node).Count()));
        Assert.That(1, Is.EqualTo((from node in tree.LessThanOrEqualTo(1) select node).Count()));

        tree.Insert(5, "9");
        tree.Insert(9, "9");

        Assert.That(new int[]{9}, Is.EqualTo(from node in tree.LargerThan(5) select node.Item1));
        Assert.That(new int[]{5, 9}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(5) select node.Item1));
        Assert.That(new int[]{5, 1}, Is.EqualTo(from node in tree.LessThan(9) select node.Item1));
        Assert.That(new int[]{9, 5, 1}, Is.EqualTo(from node in tree.LessThanOrEqualTo(9) select node.Item1));

        Assert.Throws<TreeKeyExistsException>(() =>
            tree.Insert(9, "9")
        );
    }

    [Test]
    public void SplitRootNodeTest()
    {
        // insertions to cause overflow and test
        var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

        tree.Insert(0, "00");
        tree.Insert(1, "11");
        tree.Insert(2, "22");
        tree.Insert(3, "33");
        tree.Insert(4, "44");

        Assert.That(tree.Get(8), Is.Null);
        Assert.That(tree.Get(-2), Is.Null);
        Assert.That(tree.Get(99), Is.Null);

        Assert.That(tree.Get(0), Is.Not.Null);
        Assert.That(tree.Get(1), Is.Not.Null);
        Assert.That(tree.Get(2), Is.Not.Null);
        Assert.That(tree.Get(3), Is.Not.Null);
        Assert.That(tree.Get(4), Is.Not.Null);

        Assert.That(new int[]{4}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(4) select node.Item1));
        Assert.That(new int[]{2, 3, 4}, Is.EqualTo(from node in tree.LargerThan(1) select node.Item1));
        Assert.That(new int[]{3, 2, 1, 0}, Is.EqualTo(from node in tree.LessThanOrEqualTo(3) select node.Item1));
        Assert.That(new int[]{2, 1, 0}, Is.EqualTo(from node in tree.LessThan(3) select node.Item1));
    }

    [Test]
    public void SplitChildNodeTest ()
    {
        // Insert too much at root node that it overvlow
        var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

        for (var i = 0; i <= 100; i++)
        {
            tree.Insert(i, i.ToString());
            var result = (from tuple in tree.LargerThanOrEqualTo(0) select tuple.Item1).ToList();
            Assert.That(i + 1, Is.EqualTo(result.Count));
        }
    }

    [Test]
    public void RandomTest()
    {
        var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

        var sequence = new List<int>();
        var random = new Random(198);

        for (var i = 0; i < 1000; i++)
        {
            var node = random.Next(0, 2000);
            while (sequence.Contains(node) == true)
            {
                node = random.Next(0, 2000);
            }

            tree.Insert(node, node.ToString());
            sequence.Add(node);
        }

        // making a copy of sequence
        var sortedSequence = sequence.FindAll(t => true);
        sortedSequence.Sort(Comparer<int>.Default);
        Assert.That(sortedSequence, Is.EqualTo(from node in tree.LargerThanOrEqualTo(0) select node.Item1));

        for (var i = 0; i <= 100; i++)
        {
            var number = i = random.Next(0, 2000);
            var prevNumber = 0;
            foreach (var currNumber in from tuple in tree.LargerThanOrEqualTo(number) select tuple.Item1)
            {
                Assert.That(currNumber >= number);
                Assert.That(currNumber > prevNumber);
                prevNumber = currNumber;
            }

            prevNumber = 0;
            foreach (var currNumber in from tuple in tree.LargerThan(number) select tuple.Item1)
            {
                Assert.That(currNumber > number);
                Assert.That(currNumber > prevNumber);
                prevNumber = currNumber;
            }

            prevNumber = 999999;
            foreach (var currNumber in from tuple in tree.LessThanOrEqualTo(number) select tuple.Item1)
            {
                Assert.That(currNumber <= number);
                Assert.That(currNumber < prevNumber);
                prevNumber = currNumber;
            }

            prevNumber = 999999;
            foreach (var currNumber in from tuple in tree.LessThan(number) select tuple.Item1)
            {
                Assert.That(currNumber < number);
                Assert.That(currNumber < prevNumber);
                prevNumber = currNumber;
            }
        }
    }
}