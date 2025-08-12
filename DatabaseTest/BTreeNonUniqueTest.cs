using DatabaseCore;
using NUnit.Framework;

namespace DatabaseTest;

[TestFixture]
public class BTreeNonUniqueTest()
{
    [Test]
    public void NonFullRootNodeTest()
    {
        var tree = new Tree<int, string>
        (
            new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default),
            allowDuplicateKeys: true
        );

        tree.Insert(1, "1");

        Assert.That(0, Is.EqualTo((from node in tree.LargerThan(7) select node).Count()));
        Assert.That(0, Is.EqualTo((from node in tree.LargerThanOrEqualTo(7) select node).Count()));
        Assert.That(1, Is.EqualTo((from node in tree.LessThan(7) select node).FirstOrDefault().Item1));
        Assert.That(1, Is.EqualTo((from node in tree.LessThanOrEqualTo(7) select node).FirstOrDefault().Item1));
        Assert.That(0, Is.EqualTo((from node in tree.LessThan(1) select node).Count()));
        Assert.That(1, Is.EqualTo((from node in tree.LessThanOrEqualTo(1) select node).FirstOrDefault().Item1));

        tree.Insert(5, "5");
        tree.Insert(9, "9");

        Assert.That(new int[]{9}, Is.EqualTo(from node in tree.LargerThan(5) select node.Item1));
        Assert.That(new int[]{5, 9}, Is.EqualTo(from node in tree.LargerThanOrEqualTo(5) select node.Item1));
        Assert.That(new int[]{5, 1}, Is.EqualTo(from node in tree.LessThan(9) select node.Item1));
        Assert.That(new int[]{9, 5, 1}, Is.EqualTo(from node in tree.LessThanOrEqualTo(9) select node.Item1));

        Assert.DoesNotThrow(() =>
            tree.Insert(5, "5.1")
        );

        var nodes = (from node in tree.LargerThanOrEqualTo(5) select node.Item2).ToList();
        Assert.That(nodes.Count, Is.EqualTo(3));
        Assert.That(nodes.Contains("5"));
        Assert.That(nodes.Contains("5.1"));
    }

    [Test]
    public void BreakNodeTest()
    {
        var sequence = new List<double>();
        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {
            sequence.Add(random.Next(0, 10));
        }

        var tree = new Tree<double, string>
        (
            new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
            allowDuplicateKeys: true
        );
        foreach (var node in sequence)
        {
            tree.Insert(node, node.ToString());
        }

        var keys = (from node in tree.LargerThanOrEqualTo(0) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo(sequence.Count));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, sequence)));
        }

        keys = (from node in tree.LargerThanOrEqualTo(7) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node >= 7 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node >= 7 select node)));
        }

        keys = (from node in tree.LargerThan(7) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node > 7 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node > 7 select node)));
        }

        keys = (from node in tree.LargerThanOrEqualTo(7.5) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node >= 7.5 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node >= 7.5 select node)));
        }

        keys = (from node in tree.LargerThan(7.5) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node > 7.5 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node > 7.5 select node)));
        }

        keys = (from node in tree.LessThanOrEqualTo(7) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node <= 7 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node <= 7 select node)));
        }

        keys = (from node in tree.LessThan(7) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node < 7 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node < 7 select node)));
        }

        keys = (from node in tree.LessThanOrEqualTo(7.5) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node <= 7.5 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node <= 7.5 select node)));
        }

        keys = (from node in tree.LessThan(7.5) select node.Item1).ToArray();
        Assert.That(keys.Length, Is.EqualTo((from node in sequence where node < 7.5 select node).Count()));
        foreach (var key in keys)
        {
            Assert.That(OccurencesInList(key, keys), Is.EqualTo(OccurencesInList(key, from node in sequence where node < 7.5 select node)));
        }
    }

    private static int OccurencesInList (double value, IEnumerable<double> list)
    {
        return (from t in list where t == value select t).Count();
    }
}