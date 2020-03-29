using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Shadowsocks.Controller;

namespace Shadowsocks.Test
{
    [TestClass]
    public class CachedNetworkStreamTest
    {
        byte[] b0 = new byte[256];
        byte[] b1 = new byte[256];
        byte[] b2 = new byte[1024];

        // [TestInitialize]
        [TestInitialize]
        public void init()
        {
            for (int i = 0; i < 256; i++)
            {
                b0[i] = (byte)i;
                b1[i] = (byte)(255 - i);
            }

            b0.CopyTo(b2, 0);
            b1.CopyTo(b2, 256);
            b0.CopyTo(b2, 512);
        }

        [TestMethod]
        public void StreamTest()
        {
            using MemoryStream ms = new MemoryStream(b2);
            using CachedNetworkStream s = new CachedNetworkStream(ms);

            byte[] o = new byte[128];

            Assert.AreEqual(128, s.Read(o, 0, 128));
            TestUtils.ArrayEqual(b0[0..128], o);

            Assert.AreEqual(64, s.Read(o, 0, 64));
            TestUtils.ArrayEqual(b0[128..192], o[0..64]);

            s.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(64, s.Read(o, 0, 64));
            TestUtils.ArrayEqual(b0[0..64], o[0..64]);
            // refuse to go out of cached range
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                s.Seek(193, SeekOrigin.Begin);
            });
            Assert.AreEqual(128, s.Read(o, 0, 128));
            TestUtils.ArrayEqual(b0[64..192], o);

            Assert.IsTrue(s.CanSeek);
            Assert.AreEqual(128, s.Read(o, 0, 128));
            TestUtils.ArrayEqual(b0[192..256], o[0..64]);
            TestUtils.ArrayEqual(b1[0..64], o[64..128]);

            Assert.IsFalse(s.CanSeek);
            // refuse to go back when non-cache data has been read
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                s.Seek(0, SeekOrigin.Begin);
            });

            // read in non-cache range
            Assert.AreEqual(64, s.Read(o, 0, 64));
            s.Read(o, 0, 128);
            Assert.AreEqual(512, s.Position);

            Assert.AreEqual(128, s.Read(o, 0, 128));
            TestUtils.ArrayEqual(b0[0..128], o);
            s.Read(o, 0, 128);
            s.Read(o, 0, 128);
            s.Read(o, 0, 128);

            // read at eos
            Assert.AreEqual(0, s.Read(o, 0, 128));
        }
    }
}
