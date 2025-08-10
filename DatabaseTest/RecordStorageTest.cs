using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using DatabaseCore;
using System.Drawing.Printing;
using System.Net.Http.Headers;

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

        Assert.That(recordStorage.Find(1).SequenceEqual (x1));
        Assert.That(recordStorage.Find(2).SequenceEqual (x2u));
        Assert.That(recordStorage.Find(4).SequenceEqual (x3));
    }

    [Test]
    public void TestUpdateBlockToSmallerSize()
    {
        var recordStorage = new RecordStorage(new BlockStorage(new MemoryStream(), 8192, 48));
        var x1 = UnitTestHelper.RandomData(2491);
        var x2 = UnitTestHelper.RandomData(9182);
        var x3 = UnitTestHelper.RandomData(5182);
        
        recordStorage.Create(x1); // Use 1 block
        recordStorage.Create(x2); // Use 2 blocks
        recordStorage.Create(x3); // Use 1 block

        var x2u = UnitTestHelper.RandomData(1177); // Use 1 block, so this record should be smaller
        recordStorage.Update(2, x2u);
        
        Assert.That(recordStorage.Find(1), Is.EqualTo(x1));
        Assert.That(recordStorage.Find(2), Is.EqualTo(x2u));
        Assert.That(recordStorage.Find(4), Is.EqualTo(x3));

        Assert.That(recordStorage.Create(UnitTestHelper.RandomData(10)), Is.EqualTo(3));
    }

    [Test]
    public void TestUpdateBlockToBiggerSize()
    {
        var recordStorage = new RecordStorage(new BlockStorage(new MemoryStream(), 8192, 48));
        var x1 = UnitTestHelper.RandomData(2491);
        var x2 = UnitTestHelper.RandomData(9182);
        var x3 = UnitTestHelper.RandomData(5182);
        
        recordStorage.Create(x1); // Use 1 block
        recordStorage.Create(x2); // Use 2 blocks
        recordStorage.Create(x3); // Use 1 block

        var x2u = UnitTestHelper.RandomData(8192 * 2 + 19); // Use 3 block, so this record should be extended
        recordStorage.Update(2, x2u);
        
        Assert.That(recordStorage.Find(1), Is.EqualTo(x1));
        Assert.That(recordStorage.Find(2), Is.EqualTo(x2u));
        Assert.That(recordStorage.Find(4), Is.EqualTo(x3));
    }

    [Test]
    public void TestCreateNewPersist()
    {
        var customData = new byte[4096 * 16 + 27];
        var rnd = new Random();
        for (var i = 0; i < customData.Length; i++)
        {
            customData[i] = (byte)rnd.Next(0, 256);
        }

        using var ms = new MemoryStream();

        var recordStorage = new RecordStorage(new BlockStorage(ms));
        var recordId = recordStorage.Create(customData);

        Assert.That(recordId, Is.EqualTo(1));
        Assert.That(recordStorage.Find(1), Is.EqualTo(customData));

        var recordStorage2 = new RecordStorage(new BlockStorage(ms));
        Assert.That(recordStorage2.Find(1), Is.EqualTo(customData));
    }

    [Test]
    public void TestCreateNewPersistEmpty()
    {
        var customData = new byte[0];
        var rnd = new Random();
        for (var i = 0; i < customData.Length; i++)
        {
            customData[i] = (byte)rnd.Next(0, 256);
        }

        using var ms = new MemoryStream();

        var recordStorage = new RecordStorage(new BlockStorage(ms));
        var recordId = recordStorage.Create(customData);

        Assert.That(recordId, Is.EqualTo(1));
        Assert.That(recordStorage.Find(1), Is.EqualTo(customData));

        var recordStorage2 = new RecordStorage(new BlockStorage(ms));
        Assert.That(recordStorage2.Find(1), Is.EqualTo(customData));
    }
    
    [Test]
    public void TestCreateNewPersistSmall()
    {
        var customData = new byte[0];
        var rnd = new Random();
        for (var i = 0; i < customData.Length; i++)
        {
            customData[i] = (byte)rnd.Next(0, 256);
        }

        using var ms = new MemoryStream();

        var recordStorage = new RecordStorage(new BlockStorage(ms));
        var recordId = recordStorage.Create(customData);

        Assert.That(recordId, Is.EqualTo(1));
        Assert.That(recordStorage.Find(1), Is.EqualTo(customData));

        var recordStorage2 = new RecordStorage(new BlockStorage(ms));
        Assert.That(recordStorage2.Find(1), Is.EqualTo(customData));
    }

    [Test]
    public void TestTrackingOfLargeFreeBlockList()
    {
        var tmp = Path.Combine(System.IO.Path.GetTempPath(), "data.bin");
        // Console.WriteLine("asdf");

        try
        {
            using var ms = new FileStream(tmp, FileMode.Create);

            var recordStorage = new RecordStorage(new BlockStorage(ms));

            var ids = new Dictionary<uint, bool>();
            for (var i = 0; i < 15342; i++)
            {
                ids.Add(recordStorage.Create(new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07}), true);
            }

            foreach (var kv in ids)
            {
                try{
                    // Console.WriteLine(kv.Key);
                    recordStorage.Delete(kv.Key);
                }
                catch{
                    Console.WriteLine(kv.Key);
                    return;
                }
            }

            // create new records reusing ids
            var reusedSecondFreeBlockTracker = false;
            for (var i = 15342; i >= 0; i--)
            {
                var id = recordStorage.Create(new byte[]{0x00, 0x01, 0x02, 0x03});
                if (id == 15343)
                {
                    reusedSecondFreeBlockTracker = true;
                }
                else
                {
                    Assert.That(ids.ContainsKey(id));
                }
            }

            Assert.That(reusedSecondFreeBlockTracker);

            Assert.That(15344, Is.EqualTo(recordStorage.Create(new byte[] { 0x00, 0x01, 0x02, 0x03 })));
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
                Console.WriteLine ("Deleted: " + tmp);
            }
        }
    }

    [Test]
    public void TestDeletion()
    {
        var data1 = GenerateRandomData(1029);
        var data2 = GenerateRandomData(14 * 1024 * 4);
        var data3 = GenerateRandomData(3591);

        var data4 = GenerateRandomData(4444);
        var data5 = GenerateRandomData(5555);
        var data6 = GenerateRandomData(6666);

        using var ms = new MemoryStream();

        var recordStorage = new RecordStorage(new BlockStorage(ms));
        var r1 = recordStorage.Create(data1);
        var r2 = recordStorage.Create(data2);
        var r3 = recordStorage.Create(data3);

        Assert.That(r1, Is.EqualTo(1));
        Assert.That(r2, Is.EqualTo(2));
        Assert.That(r3, Is.EqualTo(4));

        recordStorage.Delete(r2);

        Assert.That(recordStorage.Find(r2) == null); 

        var r4 = recordStorage.Create(data4);
        var r5 = recordStorage.Create(data5);
        var r6 = recordStorage.Create(data6);
        Assert.That(3, Is.EqualTo(r4));
        Assert.That(2, Is.EqualTo(r5));
        Assert.That(5, Is.EqualTo(r6));

        Assert.That(data4, Is.EqualTo(recordStorage.Find(r4)));
        Assert.That(data5, Is.EqualTo(recordStorage.Find(r5)));
        Assert.That(data6, Is.EqualTo(recordStorage.Find(r6)));

        var recordStorage2 = new RecordStorage(new BlockStorage(ms));
        Assert.That(data1, Is.EqualTo(recordStorage2.Find(r1)));
        Assert.That(data3, Is.EqualTo(recordStorage2.Find(r3)));
        Assert.That(data4, Is.EqualTo(recordStorage2.Find(r4)));
        Assert.That(data5, Is.EqualTo(recordStorage2.Find(r5)));
        Assert.That(data6, Is.EqualTo(recordStorage2.Find(r6)));
    }
    
    static byte[] GenerateRandomData (int size)
    {
        var customData = new byte[size];
        var rnd = new Random();
        for (var i = 0; i < customData.Length; i++) {
            customData[i] = (byte)rnd.Next(0,256);
        }
        return customData;
    }
}