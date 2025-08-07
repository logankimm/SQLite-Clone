using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using DatabaseCore;
using Microsoft.VisualBasic;

namespace DatabaseTest;

[TestFixture]
public class BlockStorageTest
{
    [Test]
    public void TestingBlockStoragePersistent()
    {
        using (var ms = new MemoryStream())
        {
            var storage = new BlockStorage(ms);

            using (var firstBlock = storage.CreateNew())
            using (var secondBlock = storage.CreateNew())
            using (var thirdBlock = storage.CreateNew())
            {
                Assert.That(firstBlock.Id, Is.EqualTo(0));
                Assert.That(secondBlock.Id, Is.EqualTo(1));

                // change the kcontentlength and krecord storage
                secondBlock.SetHeader(1, 100);
                secondBlock.SetHeader(2, 200);

                Assert.That(thirdBlock.Id, Is.EqualTo(2));
                Assert.That(ms.Length, Is.EqualTo(storage.BlockSize * 3));
            }

            // Test to make sure our creation persists
            var storage2 = new BlockStorage (ms);
            Assert.That(storage2.Find(0).Id, Is.EqualTo(0));
            Assert.That(storage2.Find(1).Id, Is.EqualTo(1));
            Assert.That(storage2.Find(2).Id, Is.EqualTo(2));

            Assert.That(storage2.Find(1).GetHeader(1), Is.EqualTo(100));
            Assert.That(storage2.Find(1).GetHeader(2), Is.EqualTo(200));
        }
    }

    [Test]
    public void TestManangingBlockInstances()
    {
        var manager = new BlockStorage(new MemoryStream());
        var a = manager.CreateNew();
        var b = manager.CreateNew();
        Assert.That(manager.Find(0), Is.SameAs(a));
        Assert.That(manager.Find(1), Is.SameAs(b));
        Assert.That(a, Is.Not.SameAs(b));

        a.Dispose();
        Assert.That(a, Is.Not.SameAs(manager.Find(0)));
    }

    [Test]
    public void Test8KBlock()
    {
        using var ms = new MemoryStream();
        var blockStorage = new BlockStorage(ms, 12288, 48);

        using (var block = blockStorage.CreateNew())
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Write(new byte[4048], 0, 1, 8192)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Write(new byte[4048], 0, 1, 8193)
            );
        }

        var data = UnitTestHelper.RandomData(8192);
        var ex0 = new byte[blockStorage.BlockContentSize];
        var ex1 = new byte[2381];
        var ex2 = new byte[37];
        var ex3 = new byte[137];
        var ex4 = new byte[6028];
        var ex5 = new byte[1000];
        Buffer.BlockCopy(src: data, srcOffset: 16, dst: ex0, dstOffset: 1294, count: 2381);
        Buffer.BlockCopy(src: data, srcOffset: 16, dst: ex1, dstOffset: 0, count: 2381);
        Buffer.BlockCopy(src: data, srcOffset: 16, dst: ex2, dstOffset: 0, count: 37);
        Buffer.BlockCopy(src: data, srcOffset: 32, dst: ex3, dstOffset: 0, count: 137);
        Buffer.BlockCopy(src: data, srcOffset: 32, dst: ex4, dstOffset: 0, count: 6028);
        Buffer.BlockCopy(src: data, srcOffset: 32, dst: ex5, dstOffset: 0, count: 1000);
        
        using(var a = blockStorage.CreateNew())
        using(var b = blockStorage.CreateNew())
        using(var c = blockStorage.CreateNew())
        using(var d = blockStorage.CreateNew())
        using(var e = blockStorage.CreateNew())
        {
            a.Write(src: data, srcOffset: 16, dstOffset: 1294, count: 2381);
            b.Write(src: data, srcOffset: 16, dstOffset: 12, count: 37);
            c.Write(src: data, srcOffset: 32, dstOffset: 4078, count: 137);
            d.Write(src: data, srcOffset: 32, dstOffset: 4048, count: 6028);
            e.Write(src: data, srcOffset: 32, dstOffset: 4096 - 1000 - 48, count: 1000);

            var t0 = new byte[blockStorage.BlockContentSize];
            var t1 = new byte[2381];
            var t2 = new byte[37];
            var t3 = new byte[137];
            var t4 = new byte[6028];
            var t5 = new byte[1000];

            a.Read(dst: t0, dstOffset: 0, srcOffset: 0, count: t0.Length);
            a.Read(dst: t1, dstOffset: 0, srcOffset: 1294, count: 2381);
            b.Read(dst: t2, dstOffset: 0, srcOffset: 12, count: 37);
            c.Read(dst: t3, dstOffset: 0, srcOffset: 4078, count: 137);
            d.Read(dst: t4, dstOffset: 0, srcOffset: 4048, count: 6028);
            e.Read(dst: t5, dstOffset: 0, srcOffset: 4096 - 1000 - 48, count: 1000);

            Assert.That(ex0, Is.EqualTo(t0));
            Assert.That(ex1, Is.EqualTo(t1));
            Assert.That(ex2, Is.EqualTo(t2));
            Assert.That(ex3, Is.EqualTo(t3));
            Assert.That(ex4, Is.EqualTo(t4));
            Assert.That(ex5, Is.EqualTo(t5));
        }

        // checking blockstorage persistence
        blockStorage = new BlockStorage(new MemoryStream(ms.ToArray()), 12288, 48);
        using (var a = blockStorage.Find(1))
        using (var b = blockStorage.Find(2))
        using (var c = blockStorage.Find(3))
        using (var d = blockStorage.Find(4))
        using (var e = blockStorage.Find(5))
        {
            var t1 = new byte[2381];
            var t2 = new byte[37];
            var t3 = new byte[137];
            var t4 = new byte[6028];
            var t5 = new byte[1000];
            a.Read(dst: t1, dstOffset: 0, srcOffset: 1294, count: 2381);
            b.Read(dst: t2, dstOffset: 0, srcOffset: 12, count: 37);
            c.Read(dst: t3, dstOffset: 0, srcOffset: 4078, count: 137);
            d.Read(dst: t4, dstOffset: 0, srcOffset: 4048, count: 6028);
            e.Read(dst: t5, dstOffset: 0, srcOffset: 4096 - 1000 - 48, count: 1000);

            Console.WriteLine($"Expected first byte: {ex1[0]}");
            Console.WriteLine($"Actual first byte: {t1[0]}");
            Console.WriteLine($"Data source first byte: {data[16]}");
            Assert.That(ex1, Is.EqualTo(t1));
            Assert.That(ex2, Is.EqualTo(t2));
            Assert.That(ex3, Is.EqualTo(t3));
            Assert.That(ex4, Is.EqualTo(t4));
            Assert.That(ex5, Is.EqualTo(t5));
        }
    }
}