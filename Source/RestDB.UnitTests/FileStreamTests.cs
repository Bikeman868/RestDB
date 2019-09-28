using Moq.Modules;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class FileStreamTests: TestBase
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Ignore("This test fails proving that you can not make async calls on the same FileStream from different threads")]
        [TestCase(16000, 100)]
        public void multi_thread_async_write(int blockSize, int threadCount)
        {
            using (var fileStream = File.Create("C:\\temp\\test.bin"))
            {
                var threads = new List<Thread>();
                var locker = new object();

                for (var i = 0; i < threadCount; i++)
                {
                    var threadNum = i;
                    var thread = new Thread(() =>
                    {
                        var valueBytes = BitConverter.GetBytes((Int16)threadNum);
                        var buffer = new byte[blockSize];
                        for (var j = 0; j < blockSize; j += 2)
                            valueBytes.CopyTo(buffer, j);

                        Task task;

                        lock (locker)
                        {
                            fileStream.Seek(threadNum * blockSize, SeekOrigin.Begin);
                            task = fileStream.WriteAsync(buffer, 0, buffer.Length);
                        }

                        task.Wait();
                    })
                    {
                        IsBackground = true,
                        Name = "Thread " + threadNum
                    };
                    threads.Add(thread);
                }

                foreach (var thread in threads)
                    thread.Start();

                foreach (var thread in threads)
                    thread.Join();

                var readBuffer = new byte[blockSize];
                for (var i = 0; i < threadCount; i++)
                {
                    fileStream.Seek(i * blockSize, SeekOrigin.Begin);
                    fileStream.Read(readBuffer, 0, readBuffer.Length);
                    for (var j = 0; j < blockSize; j += 2)
                        Assert.AreEqual(i, BitConverter.ToInt16(readBuffer, j));
                }
            }
        }
    }
}