using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Encryption.CircularBuffer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Test
{
    [TestClass]
    public class CircularBufferTest
    {
        void ArrayEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var l = expected.GetEnumerator();
            var r = actual.GetEnumerator();
            int p = 0;
            while (l.MoveNext() && r.MoveNext())
            {
                Assert.AreEqual(l.Current, r.Current, $"not equal at {p}");
                p++;
            }
            if (l.MoveNext()) Assert.Fail("expected longer than actual");
            else if (r.MoveNext()) Assert.Fail("expected shorter than actual");
        }
        [TestMethod]
        public void GetPut()
        {
            var c = new ByteCircularBuffer(8);
            c.Put(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            Assert.AreEqual(c.Head, 0);
            Assert.AreEqual(c.Tail, 0);
            c.Get(4);
            Assert.AreEqual(c.Head, 4);
            Assert.AreEqual(c.Tail, 0);
            Assert.AreEqual(c.Get(), 5);
            c.Put(new byte[] { 1, 2, 3, 4, 5 });
            Assert.AreEqual(c.Head, 5);
            Assert.AreEqual(c.Tail, 5);
            var content = c.Get(8);
            ArrayEqual(content, new byte[] { 6, 7, 8, 1, 2, 3, 4, 5 });
            c.Put(10);
            var b = new byte[1];
            c.Get(b);
            Assert.AreEqual(b[0], 10);
        }
    }
}
