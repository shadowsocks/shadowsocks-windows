using GlobalHotKey;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using Shadowsocks.Controller.Hotkeys;
using System.Windows.Input;


namespace Shadowsocks.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestCompareVersion()
        {
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("2.3.1.0", "2.3.1") == 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.2", "1.3") < 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3", "1.3") == 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.2.1", "1.2") > 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("2.3.1", "2.4") < 0);
            Assert.IsTrue(UpdateChecker.Asset.CompareVersion("1.3.2", "1.3.1") > 0);
        }

        [TestMethod]
        public void TestHotKey2Str()
        {
            Assert.AreEqual("Ctrl+A", HotKeys.HotKey2Str(Key.A, ModifierKeys.Control));
            Assert.AreEqual("Ctrl+Alt+D2", HotKeys.HotKey2Str(Key.D2, (ModifierKeys.Alt | ModifierKeys.Control)));
            Assert.AreEqual("Ctrl+Alt+Shift+NumPad7", HotKeys.HotKey2Str(Key.NumPad7, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
            Assert.AreEqual("Ctrl+Alt+Shift+F6", HotKeys.HotKey2Str(Key.F6, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
            Assert.AreNotEqual("Ctrl+Shift+Alt+F6", HotKeys.HotKey2Str(Key.F6, (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)));
        }

        [TestMethod]
        public void TestStr2HotKey()
        {
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+A").Equals(new HotKey(Key.A, ModifierKeys.Control)));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Alt+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt))));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Shift+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Shift))));
            Assert.IsTrue(HotKeys.Str2HotKey("Ctrl+Alt+Shift+A").Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey0 = HotKeys.Str2HotKey("Ctrl+Alt+Shift+A");
            Assert.IsTrue(testKey0 != null && testKey0.Equals(new HotKey(Key.A, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey1 = HotKeys.Str2HotKey("Ctrl+Alt+Shift+F2");
            Assert.IsTrue(testKey1 != null && testKey1.Equals(new HotKey(Key.F2, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey2 = HotKeys.Str2HotKey("Ctrl+Shift+Alt+D7");
            Assert.IsTrue(testKey2 != null && testKey2.Equals(new HotKey(Key.D7, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
            HotKey testKey3 = HotKeys.Str2HotKey("Ctrl+Shift+Alt+NumPad7");
            Assert.IsTrue(testKey3 != null && testKey3.Equals(new HotKey(Key.NumPad7, (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))));
        }

        [TestMethod]
        public void TestGetBandwidthScale()
        {
            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 1,
                unitName = "B",
                unit = 1
            }, Util.Utils.GetBandwidthScale(1));

            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 1024,
                unitName = "B",
                unit = 1
            }, Util.Utils.GetBandwidthScale(1024));

            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 1.125f,
                unitName = "KiB",
                unit = 1024
            }, Util.Utils.GetBandwidthScale(1152));

            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 1024,
                unitName = "KiB",
                unit = 1024
            }, Util.Utils.GetBandwidthScale(1048576));

            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 2,
                unitName = "GiB",
                unit = 1073741824
            }, Util.Utils.GetBandwidthScale(2147483648));
            Assert.AreEqual(new Util.BandwidthScaleInfo()
            {
                value = 2,
                unitName = "TiB",
                unit = 1099511627776
            }, Util.Utils.GetBandwidthScale(2199023255552));
        }
    }
}
