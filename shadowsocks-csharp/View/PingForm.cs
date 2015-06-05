using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks._3rd;
using Shadowsocks._3rd.ProxySocket;

namespace Shadowsocks.View
{
    public sealed partial class PingForm : Form
    {
        private readonly ShadowsocksController controller;

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
        private delegate void modifyRow(int rowID, string ip,string loc,  int max, int min, int avg, int failtime);
        private void ModifyRow(int rowID, string ip,string loc, int max, int min, int avg, int failtime)
        {
            if (dgvMain.InvokeRequired)
            {
                var del = new modifyRow(ModifyRow);
                Invoke(del, new object[] { rowID, ip,loc, max, min, avg, failtime });
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

        private delegate void modifyRowSpeed(int rowID,string ip, string loc,string speed);

        private void ModifyRowSpeed(int rowID, string ip, string loc, string speed)
        {
            if (dgvMain.InvokeRequired)
            {
                var del = new modifyRowSpeed(ModifyRowSpeed);
                Invoke(del, new object[] {rowID, ip, loc, speed});
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
                        if (reply == null) { failTime++; continue;}
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
            if (Util.Utils.qqwry != null & addr != "")
            {
                var v = Util.Utils.qqwry.SearchIPLocation(addr);
                location = (v.country + v.area.Replace("CZ88.NET", ""));
            }
            if(result.Count == 0)
                ModifyRow(row.Index, addr,location, 9999, 9999, 9999, 10);
            else
                ModifyRow(row.Index, addr,location, (int)result.Max(), (int)result.Min(), (int)result.Average(), failTime);
        }

        private void Go(object rc)
        {
            var rows = rc as DataGridViewRowCollection;
            if (rows != null)
            {
                foreach (DataGridViewRow row in rows)
                    Ping(row);
                SelectMinMax();
                ChangeStatus(I18N.GetString("Ready"));
            }
            else
            {
                ChangeStatus(I18N.GetString("Wrong"));
            }
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
                ChangeStatus(I18N.GetString("Ready"));
            }
            catch
            {
                ChangeStatus(I18N.GetString("Wrong"));
            }
        }

        private void GetIPAddress(int svc, out string addr, out string stat, out float speed)
        {
            var currentIndex = controller.GetConfiguration().index;
            controller.SelectServerIndexTemp(svc);
            var webClient = new SocksWebClient { ProxyDetails = new ProxyDetails(controller.GetConfiguration().localPort)};

            try
            {
				//get location
                var regx1 = new Regex(@"\d+\.\d+\.\d+\.\d+");
                var regx2 = new Regex(@"来自：(.*?)\<");
                var response = webClient.DownloadString(@"http://1111.ip138.com/ic.asp");

                var mc1 = regx1.Match(response);
                addr = mc1.Success ? mc1.Value : I18N.GetString("Unknow");

                var mc2 = regx2.Match(response);
                stat = mc2.Success ? mc2.Groups[1].Value : I18N.GetString("…(⊙_⊙;)…");
            }
            catch
            {
                addr = I18N.GetString("Unknow");
                stat = I18N.GetString("…(⊙_⊙;)…");
            }

            try
            {
                //speed test
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var dl = webClient.DownloadData("https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B7B1E2CBF-95F1-5FDD-C836-E5930E3E51CD%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26installdataindex%3Ddefaultbrowser/update2/installers/ChromeSetup.exe");//http://dl.google.com/googletalk/googletalk-setup.exe
                sw.Stop();
                var len = dl.Length / 1024f;
                var sec = sw.Elapsed.Milliseconds / 1000f;
                speed = len / sec;
            }
            catch
            {
                speed = 0;
            }
            webClient.Dispose();
            controller.SelectServerIndexTemp(currentIndex);
        }
        #endregion
        public PingForm(ShadowsocksController sc)
        {
            InitializeComponent();

            var qqwryPath = Environment.CurrentDirectory + "\\qqwry.dat";
            if (Util.Utils.qqwry == null && File.Exists(qqwryPath)) Util.Utils.qqwry = new QQWry(qqwryPath);

            controller = sc;
           
            Font = Util.Utils.GetFont();

            PerformLayout();

            UpdateTexts();

            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            LoadConfiguration(controller.GetConfiguration());

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
                ChangeStatus(I18N.GetString("DoSomething"));
                var t = new Thread(Go) { IsBackground = true };
                t.Start(dgvMain.Rows);
            }

        }

        private void UpdateTexts()
        {
            foreach (DataGridViewColumn col in dgvMain.Columns)
                col.HeaderText = I18N.GetString(col.Name);
            Text = I18N.GetString("UsableTest");
            tssStatusLabel.Text = I18N.GetString("CurrentStatus:");
            tssStatus.Text = I18N.GetString("Ready");
        }


        private void LoadConfiguration(Configuration configuration)
        {
            dgvMain.Rows.Clear();
            foreach (Server server in configuration.configs)
            {
                int index = dgvMain.Rows.Add();
                dgvMain.Rows[index].Cells[0].Value = server.server;
                dgvMain.Rows[index].Cells[1].Value = I18N.GetString("Pinging");
                dgvMain.Rows[index].Cells[2].Value = server.remarks;
                dgvMain.Rows[index].Cells[9].Value = I18N.GetString("TestSpeed");
            }
            var row = dgvMain.Rows[configuration.index];
            dgvMain.ClearSelection();
            dgvMain.CurrentCell = row.Cells[0];
            row.Selected = true;
        }


        private void dgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvMain.Columns[e.ColumnIndex].Name != "TestSpeed") return;
            ChangeStatus(I18N.GetString("DoSomething"));
            var t = new Thread(Test) {IsBackground = true};
            t.Start(e.RowIndex);
            //10sec time out because WebClient dont have Timeout property
            var tm = new System.Timers.Timer(10000);
            tm.Elapsed += (ss, ee) =>{ tm.Stop(); t.Abort(); };
            tm.Start();
        }

        private void dgvMain_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((int) dgvMain.Rows[e.RowIndex].Cells[4].Value == 9999)
            { if (MessageBox.Show(I18N.GetString("Seems your server is down, r u sure apply this server?"), "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return; }
            controller.SelectServerIndex(e.RowIndex);
            Close();
        }
    }
}
