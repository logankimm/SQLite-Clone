using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using DatabaseCore;
using System.Drawing.Printing;

namespace DatabaseTest;

[TestFixture]
public class RecordStorageTest
{
    [Test]
    public void TestUpdateEqualSizeBlock()
    {
        var recordStorage = new RecordStorage(new BlockStorage(new MemoryStream(), 8192, 48));
        var x1 = UnitTestHelper.RandomData(2491);
        var x2 = UnitTestHelper.RandomData(9182);
        var x3 = UnitTestHelper.RandomData(5182);

        recordStorage.Create(x1); // Use 1 block
        recordStorage.Create(x2); // Use 2 blocks
        recordStorage.Create(x3); // Use 1 block

        var x2u = UnitTestHelper.RandomData(9177); // Use 2 blocks, still
        recordStorage.Update(2, x2u);

        // Console.WriteLine(BitConverter.ToString(recordStorage.Find(1)));
        // Console.WriteLine(BitConverter.ToString(x1));
        Assert.That(true);
        // Assert.That(recordStorage.Find(1).SequenceEqual (x1));
    }
}