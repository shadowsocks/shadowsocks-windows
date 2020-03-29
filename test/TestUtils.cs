using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Test
{
    class TestUtils
    {
        public static void ArrayEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string msg = "")
        {
            var e1 = expected.GetEnumerator();
            var e2 = actual.GetEnumerator();
            int ctr = 0;
            while (true)
            {
                var e1next = e1.MoveNext();
                var e2next = e2.MoveNext();

                if (e1next && e2next)
                {
                    Assert.AreEqual(e1.Current, e2.Current, "at " + ctr);
                }
                else if (!e1next && !e2next)
                {
                    return;
                }
                else if (!e1next)
                {
                    Assert.Fail($"actual longer than expected ({ctr}) {msg}");
                }
                else
                {
                    Assert.Fail($"actual shorter than expected ({ctr}) {msg}");
                }
                ctr++;
            }
        }

    }
}
