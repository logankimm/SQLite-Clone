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
    public void TestBlockStoragePersistent()
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

            // Test to make sure our creation persists and makes changes to the memorystream
            var storage2 = new BlockStorage(ms);
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
        using (var ms = new MemoryStream())
        {
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

                Assert.That(ex1, Is.EqualTo(t1));
                Assert.That(ex2, Is.EqualTo(t2));
                Assert.That(ex3, Is.EqualTo(t3));
                Assert.That(ex4, Is.EqualTo(t4));
                Assert.That(ex5, Is.EqualTo(t5));
            }
        }
    }

    [Test]
    public void Test4kBlock()
    {
        using var ms = new MemoryStream();

        var blockStorage = new BlockStorage(ms, 4096, 48);
        using (var block = blockStorage.CreateNew())
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Write(new byte[4048], 0, 1, 4048)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Write(new byte[4048], 0, 0, 4049)
            );
        }

        var d1 = UnitTestHelper.RandomData(4048);
        var d2 = UnitTestHelper.RandomData(1294);
        var d3 = UnitTestHelper.RandomData(4048);

        using (var a = blockStorage.CreateNew())
        using (var b = blockStorage.CreateNew())
        using (var c = blockStorage.CreateNew())
        {
            a.Write(d1, 0, 0, 4048);
            b.Write(d2, 0, 0, 1294);
            c.Write(d3, 0, 0, 4048);

            var r1 = new byte[4048];
            a.Read (r1, 0, 0, 4048);

            var r2 = new byte[1294];
            b.Read (r2, 0, 0, 1294);

            var r3 = new byte[4048];
            c.Read (r3, 0, 0, 4048);

            Assert.That(d1, Is.EqualTo(r1));
            Assert.That(d2, Is.EqualTo(r2));
            Assert.That(d3, Is.EqualTo(r3));
        }

        blockStorage = new BlockStorage(new MemoryStream(ms.ToArray()), 4096, 48);
        using (var a = blockStorage.Find(1u))
        using (var b = blockStorage.Find(2u))
        using (var c = blockStorage.Find(3u))
        {
            var r1 = new byte[4048];
            a.Read (r1, 0, 0, 4048);

            var r2 = new byte[1294];
            b.Read (r2, 0, 0, 1294);

            var r3 = new byte[4048];
            c.Read (r3, 0, 0, 4048);

            Assert.That(d1, Is.EqualTo(r1));
            Assert.That(d2, Is.EqualTo(r2));
            Assert.That(d3, Is.EqualTo(r3));
        }   
    }

    [Test]
    public void TestBlockGetSetHeader()
    {
        using var ms = new MemoryStream();

        var storage = new BlockStorage(new MemoryStream());
        var blockStart = storage.BlockSize * 22;

        ms.Write(new byte[blockStart], 0, blockStart);

        ms.Write(LittleEndianByteOrder.GetBytes(11L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(22L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(33L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(44L), 0, 8);

        var firstSector = new byte[storage.DiskSectorSize];
        ms.Position -= 4 * 8;
        ms.Read(firstSector);
        using (var block = new Block(storage, 22, firstSector, ms))
        {
            block.GetHeader(0);

            Assert.That(11L, Is.EqualTo(block.GetHeader(0)));
            Assert.That(22L, Is.EqualTo(block.GetHeader(1)));
            Assert.That(33L, Is.EqualTo(block.GetHeader(2)));
            Assert.That(44L, Is.EqualTo(block.GetHeader(3)));

            block.SetHeader(1, 33L);

            Assert.That(33L, Is.EqualTo(block.GetHeader(1)));

            var buffer = new byte[8];
            ms.Position = blockStart + 8;
            ms.Read (buffer, 0, 8);
            Assert.That(22L, Is.EqualTo(LittleEndianByteOrder.GetInt64(buffer)));
        }

        // until flushing?
        {
            var buffer = new byte[8];
            ms.Position = blockStart + 8;
            ms.Read (buffer, 0, 8);
            Assert.That(33L, Is.EqualTo(LittleEndianByteOrder.GetInt64(buffer)));
        }
    }

    [Test]
    public void TestBlockWriteBody()
    {
        using var ms = new MemoryStream();

        var storage = new BlockStorage(new MemoryStream());
        var firstSector = new byte[storage.DiskSectorSize];
        ms.Position = storage.BlockSize * 3;
        ms.Read(firstSector);
        using (var block = new Block(storage, 3, firstSector, ms))
        {
            var data = new byte[]{
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
            };
            block.Write(data, 2, 3, 4);

            var readBackResult = new byte[12];
            block.Read(readBackResult, 0, 0, readBackResult.Length);
            Assert.That(readBackResult, Is.EqualTo(new byte[] {
                0x00, 0x00, 0x00, 0x03, 0x04, 0x05, 
                0x06, 0x00, 0x00, 0x00, 0x00, 0x00,
            }));
        }

        var expectedReadBackResult = new byte[4095];

        using (var block = new Block(storage, 3, firstSector, ms))
        {
            var readBackResult = new byte[12];
            block.Read(readBackResult, 0, 0, readBackResult.Length);
            Assert.That(readBackResult, Is.EqualTo(new byte[] {
                0x00, 0x00, 0x00, 0x03, 0x04, 0x05, 
                0x06, 0x00, 0x00, 0x00, 0x00, 0x00,
            }));

            // writing data outside of the first 4KB block
            var data = new byte[4096 * 2];
            var rnd = new Random();
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)rnd.Next(0, 256);
            }
            block.Write(data, 16, 4096 + 32, 4095);

            // reading back results
            readBackResult = new byte[4095];
            block.Read(readBackResult, 0, 4096 + 32, 4095);

            Buffer.BlockCopy(data, 16, expectedReadBackResult, 0, expectedReadBackResult.Length);
            Assert.That(readBackResult, Is.EqualTo(expectedReadBackResult));
        }

        // persistence
        using(var block = new Block(storage, 3, firstSector, ms))
        {
            var readBackResult = new byte[4095];
            block.Read(readBackResult, 0, 4096 + 32, 4095);
            Assert.That(readBackResult, Is.EqualTo(expectedReadBackResult));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Write(new byte[32], 0, storage.BlockContentSize - 31, 32)
            );

            Assert.DoesNotThrow(() =>
                block.Write(new byte[32], 0, storage.BlockContentSize - 32, 32)
            );
        }
    }

    [Test]
    public void TestBlockReadBody()
    {
        using var ms = new MemoryStream();

        var storage = new BlockStorage(new MemoryStream());
        var blockStart = storage.BlockSize * 3;

        ms.Write(LittleEndianByteOrder.GetBytes(11L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(22L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(33L), 0, 8);
        ms.Write(LittleEndianByteOrder.GetBytes(44L), 0, 8);

        ms.Position = storage.BlockSize * 3 + storage.BlockHeaderSize;

        // random data of 6KB
        var bodyLength = (1024 * 6) + 71;
        var body = new byte[bodyLength];
        var rnd = new Random();
        for (var i = 0; i < body.Length; i++)
        {
            body[i] = (byte)rnd.Next(0, 256);
        }
        ms.Write(body);

        var firstSector = new byte[storage.DiskSectorSize];
        ms.Position = storage.BlockSize * 3;
        ms.Read(firstSector);
        using (var block = new Block(storage, 3, firstSector, ms))
        {
            var result = new byte[bodyLength];
            block.Read(result, 0, 0, bodyLength);
            Assert.That(result, Is.EqualTo(body));

            result = new byte[1024];
            block.Read(
                result,
                64,
                storage.DiskSectorSize - storage.BlockHeaderSize,
                1024 - 64
            );
            var result2 = new byte[1024 - 64];
            Buffer.BlockCopy(result, 64, result2, 0, result2.Length);

            var expectedResult2 = new byte[1024 - 64];
            Buffer.BlockCopy(
                body,
                storage.DiskSectorSize - storage.BlockHeaderSize,
                expectedResult2,
                0,
                expectedResult2.Length
            );
            Assert.That(result2, Is.EqualTo(expectedResult2));

            result = new byte[128];
            block.Read(result, 64, 16, 128 - 64);

            var result3 = new byte[128 - 64];
            Buffer.BlockCopy(result, 64, result3, 0, result3.Length);

            var expectedResult3 = new byte[128 - 64];
            Buffer.BlockCopy(body, 16, expectedResult3, 0, expectedResult3.Length);

            Assert.That(result3, Is.EqualTo(expectedResult3));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Read(result, 0, 0, bodyLength + 1)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                block.Read(new byte[1024 * 12], 0, 0, storage.BlockContentSize)
            );
        }
    }
}