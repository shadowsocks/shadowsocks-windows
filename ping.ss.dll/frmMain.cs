using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ping.ss.ProxySocket;
using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace ping.ss
{
    public partial class frmMain : Form
    {
        private readonly ShadowsocksController controller;
        private QQWry qqwry;

        #region Delegate
        private delegate void selectMinMax();

        private void SelectMinMax()
        {
            if (dgvMain.InvokeRequired)
            {
                var invoke = new selectMinMax(SelectMinMax);
                Invoke(invoke);
            }
            else
            {
                var q = dgvMain.Rows.Cast<DataGridViewRow>().Where(row => (int)row.Cells["Average"].Value != 9999).ToArray();
                int max = q.Max(x => Convert.ToInt32(x.Cells["Average"].Value));
                int min = q.Min(x => Convert.ToInt32(x.Cells["Average"].Value));
                foreach (DataGridViewRow row in dgvMain.Rows)
                {
                    if ((int)row.Cells["Average"].Value == min) row.DefaultCellStyle.ForeColor = Color.Green;
                    if ((int)row.Cells["Average"].Value == max) row.DefaultCellStyle.ForeColor = Color.Red;
                }
            }
        }

        private delegate void changeStatus(string val);
        private void ChangeStatus(string val)
        {
            if (statusStrip1.InvokeRequired)
            {
                var invoke = new changeStatus(ChangeStatus);
                Invoke(invoke, val);
            }
            else
            {
                if (val == I18N.GetString("Ready"))
                {
                    pBar.Visible = false;
                    dgvMain.Enabled = true;
                    tssStatus.Text = I18N.GetString("Ready");
                    tssStatus.ForeColor = Color.Green;
                }
                else if (val == I18N.GetString("Wrong"))
                {
                    pBar.Visible = false;
                    dgvMain.Enabled = true;
                    tssStatus.Text = I18N.GetString("Wrong");
                    tssStatus.ForeColor = Color.Red;
                }
                else
                {
                    pBar.Visible = true;
                    dgvMain.Enabled = false;
                    tssStatus.Text = val;
                    tssStatus.ForeColor = Color.Blue;
                }
            }
        }
        private delegate void modifyRow(int rowID, string ip, string loc, int max, int min, int avg, int failtime);
        private void ModifyRow(int rowID, string ip, string loc, int max, int min, int avg, int failtime)
        {
            if (dgvMain.InvokeRequired)
            {
                var del = new modifyRow(ModifyRow);
                Invoke(del, new object[] { rowID, ip, loc, max, min, avg, failtime });
            }
            else
            {
                dgvMain.Rows[rowID].Cells[1].Value = string.IsNullOrEmpty(ip) ? I18N.GetString("PingFail") : ip;
                dgvMain.Rows[rowID].Cells[4].Value = max;
                dgvMain.Rows[rowID].Cells[5].Value = min;
                dgvMain.Rows[rowID].Cells[6].Value = avg;
                dgvMain.Rows[rowID].Cells[7].Value = failtime;
                if (!string.IsNullOrEmpty(loc)) dgvMain.Rows[rowID].Cells[3].Value = loc;
            }
        }

        private delegate void modifyRowSpeed(int rowID, string ip, string loc, string speed);

        private void ModifyRowSpeed(int rowID, string ip, string loc, string speed)
        {
            if (dgvMain.InvokeRequired)
            {
                var del = new modifyRowSpeed(ModifyRowSpeed);
                Invoke(del, new object[] { rowID, ip, loc, speed });
            }
            else
            {
                dgvMain.Rows[rowID].Cells[1].Value = string.IsNullOrEmpty(ip) ? I18N.GetString("PingFail") : ip;
                if (!string.IsNullOrEmpty(loc)) dgvMain.Rows[rowID].Cells[3].Value = loc;
                if (!string.IsNullOrEmpty(speed)) dgvMain.Rows[rowID].Cells[8].Value = speed;
            }
        }

        #endregion

        #region Method
        private void LoadConfiguration(Configuration configuration)
        {
            dgvMain.Rows.Clear();
            foreach (Server server in configuration.configs)
            {
                int index = dgvMain.Rows.Add();
                dgvMain.Rows[index].Cells[0].Value = server.server;
                dgvMain.Rows[index].Cells[1].Value = "Working";
                dgvMain.Rows[index].Cells[2].Value = server.remarks;
                dgvMain.Rows[index].Cells[9].Value = "Test";
            }
            var row = dgvMain.Rows[configuration.index];
            dgvMain.ClearSelection();
            dgvMain.CurrentCell = row.Cells[0];
            row.Selected = true;
        }


        private void Test(object index)
        {
            try
            {
                var rowIndex = (int)index;
                string ip, location;
                float speed;
                GetIPAddress(rowIndex, out ip, out location, out speed);
                ModifyRowSpeed(rowIndex, ip, location, speed.ToString("N0") + "KB/s");
                ChangeStatus("Ready");
            }
            catch
            {
                ChangeStatus("Wrong");
            }
        }

        private void Ping(object r)
        {
            var row = r as DataGridViewRow;
            if (row == null) return;
            var addr = "";
            var result = new List<long>();
            var failTime = 0;

            using (var ping = new Ping())
            {
                for (var times = 0; times < 4; times++)
                {
                    try
                    {
                        var reply = ping.Send((string)row.Cells[0].Value, 2000);
                        if (reply == null) { failTime++; continue; }
                        addr = reply.Address.ToString();
                        if (reply.Status == IPStatus.Success)
                        {
                            result.Add(reply.RoundtripTime);
                        }
                        else
                        {
                            failTime++;
                        }
                    }
                    catch
                    {
                        failTime++;
                    }
                }
            }
            var location = "";
            if (qqwry != null & addr != "")
            {
                var v = qqwry.SearchIPLocation(addr);
                location = (v.country + v.area.Replace("CZ88.NET", ""));
            }
            if (result.Count == 0)
                ModifyRow(row.Index, addr, location, 9999, 9999, 9999, 10);
            else
                ModifyRow(row.Index, addr, location, (int)result.Max(), (int)result.Min(), (int)result.Average(), failTime);
        }

        private void Go(object rc)
        {
            var rows = rc as DataGridViewRowCollection;
            if (rows != null)
            {
                foreach (DataGridViewRow row in rows)
                    Ping(row);
                SelectMinMax();
                ChangeStatus("Ready");
            }
            else
            {
                ChangeStatus("Wrong");
            }
        }

        private void GetIPAddress(int svc, out string addr, out string stat, out float speed)
        {
            var currentIndex = controller.GetCurrentConfiguration().index;
            controller.SelectServerIndex(svc);
            var webClient = new SocksWebClient { ProxyDetails = new ProxyDetails(controller.GetCurrentConfiguration().localPort) };

            try
            {
                //get location
                var regx1 = new Regex(@"\d+\.\d+\.\d+\.\d+");
                var regx2 = new Regex(@"来自：(.*?)\<");
                var response = webClient.DownloadString(@"http://1111.ip138.com/ic.asp");

                var mc1 = regx1.Match(response);
                addr = mc1.Success ? mc1.Value : "Unknow";

                var mc2 = regx2.Match(response);
                stat = mc2.Success ? mc2.Groups[1].Value : "…(⊙_⊙;)…";
            }
            catch
            {
                addr = "Unknow";
                stat = "…(⊙_⊙;)…";
            }

            try
            {
                //speed test
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var dl = webClient.DownloadData("https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B7B1E2CBF-95F1-5FDD-C836-E5930E3E51CD%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26installdataindex%3Ddefaultbrowser/update2/installers/ChromeSetup.exe"); //http://dl.google.com/googletalk/googletalk-setup.exe
                sw.Stop();
                var len = dl.Length/1024f;
                var sec = sw.Elapsed.Milliseconds/1000f;
                speed = len/sec;
            }
            catch
            {
                speed = 0;
            }
            finally
            {
                webClient.Dispose();
                controller.SelectServerIndex(currentIndex);
            }
        }
        #endregion
        public frmMain(ShadowsocksController sc)
        {
            InitializeComponent();

            //初始化QQWry
            var qqwryPath = Environment.CurrentDirectory + "\\qqwry.dat";
            if (qqwry == null && File.Exists(qqwryPath)) qqwry = new QQWry(qqwryPath);
            controller = sc;

            #region i18N
            if (System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag.ToLowerInvariant().StartsWith("zh"))
            {
                Text = "Ping测试";
                dgvMain.Columns[0].HeaderText = "地址";
                dgvMain.Columns[1].HeaderText = "IP地址";
                dgvMain.Columns[2].HeaderText = "备注";
                dgvMain.Columns[3].HeaderText = "物理地址";
                dgvMain.Columns[4].HeaderText = "最大Ping值";
                dgvMain.Columns[5].HeaderText = "最小Ping值";
                dgvMain.Columns[6].HeaderText = "平均Ping值";
                dgvMain.Columns[7].HeaderText = "失败次数";
                dgvMain.Columns[8].HeaderText = "下行速度";
                dgvMain.Columns[9].HeaderText = "测速";
                tssStatusLabel.Text = "当前状态：";
                tssStatus.Text = "准备就绪";
            }
            #endregion

            LoadConfiguration(controller.GetCurrentConfiguration());

            if (dgvMain.Rows.Count <= 5)
            {
                foreach (var row in dgvMain.Rows)
                {
                    var t = new Thread(Ping) { IsBackground = true };
                    t.Start(row);
                }
            }
            else
            {
                ChangeStatus("Busy...");
                var t = new Thread(Go) { IsBackground = true };
                t.Start(dgvMain.Rows);
            }
        }
        private void dgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvMain.Columns[e.ColumnIndex].Name != "TestSpeed") return;
            if ((int) dgvMain.Rows[e.RowIndex].Cells[4].Value == 9999) return;
            ChangeStatus("Busy...");
            var t = new Thread(Test) { IsBackground = true };
            t.Start(e.RowIndex);
            //10sec time out because WebClient dont have Timeout property
            var tm = new System.Timers.Timer(5000);
            tm.Elapsed += (ss, ee) => { tm.Stop(); t.Abort(); };
            tm.Start();
        }

        private void dgvMain_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((int)dgvMain.Rows[e.RowIndex].Cells[4].Value == 9999)
            { if (MessageBox.Show("Seems your server is down, r u sure apply this server?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return; }
            controller.SelectServerIndex(e.RowIndex);
            Close();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            qqwry = null;
            GC.Collect();
        }
    }
}
