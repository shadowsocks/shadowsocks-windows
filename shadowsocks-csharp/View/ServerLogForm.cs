using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System.Threading;

namespace Shadowsocks.View
{
    public partial class ServerLogForm : Form
    {
        private ShadowsocksController controller;
        private ContextMenu contextMenu1;
        private MenuItem clearItem;
        private List<int> listOrder = new List<int>();
        private int lastRefreshIndex = 0;
        private bool rowChange = false;
        private ServerSpeedLogShow[] ServerSpeedLogList;
        private Thread workerThread;

        public ServerLogForm(ShadowsocksController controller)
        {
            this.controller = controller;
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            InitializeComponent();
            this.Width = 760;

            Configuration config = controller.GetCurrentConfiguration();
            if (config.configs.Count < 8)
            {
                this.Height = 300;
            }
            else if (config.configs.Count < 20)
            {
                this.Height = 300 + (config.configs.Count - 8) * 16;
            }
            else
            {
                this.Height = 500;
            }
            UpdateTexts();
            UpdateLog();

            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                this.clearItem = CreateMenuItem("&Clear", new EventHandler(this.ClearItem_Click)),
            });
            ServerDataGrid.ContextMenu = contextMenu1;
            controller.ConfigChanged += controller_ConfigChanged;
        }

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private void UpdateTitle()
        {
            this.Text = I18N.GetString("ServerLog") + "("
                + (controller.GetCurrentConfiguration().shareOverLan ? "any" : "local") + ":" + controller.GetCurrentConfiguration().localPort.ToString()
                + I18N.GetString(" Version") + UpdateChecker.FullVersion
                + ")";
        }
        private void UpdateTexts()
        {
            UpdateTitle();
            for (int i = 0; i < ServerDataGrid.Columns.Count; ++i)
            {
                ServerDataGrid.Columns[i].HeaderText = I18N.GetString(ServerDataGrid.Columns[i].HeaderText);
            }
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }

        private string FormatBytes(long bytes)
        {
            const long K = 1024L;
            const long M = K * 1024L;
            const long G = M * 1024L;
            const long T = G * 1024L;
            const long P = T * 1024L;
            const long E = P * 1024L;

            if (bytes >= E * 2)
                return (bytes / (double)E).ToString("F5") + "E";
            if (bytes >= P * 2)
                return (bytes / (double)P).ToString("F5") + "P";
            if (bytes >= T * 2)
                return (bytes / (double)T).ToString("F5") + "T";
            if (bytes >= G * 2)
            {
                return (bytes / (double)G).ToString("F4") + "G";
            }
            if (bytes >= K * 990)
            {
                return (bytes / (double)M).ToString("F4") + "M";
            }
            if (bytes > K * 2)
            {
                return (bytes / (double)K).ToString("F3") + "K";
            }
            return bytes.ToString();
        }

        public bool SetBackColor(DataGridViewCell cell, Color newColor)
        {
            if (cell.Style.BackColor != newColor)
            {
                cell.Style.BackColor = newColor;
                rowChange = true;
                return true;
            }
            return false;
        }
        public bool SetCellToolTipText(DataGridViewCell cell, string newString)
        {
            if (cell.ToolTipText != newString)
            {
                cell.ToolTipText = newString;
                rowChange = true;
                return true;
            }
            return false;
        }
        public bool SetCellText(DataGridViewCell cell, string newString)
        {
            if ((string)cell.Value != newString)
            {
                cell.Value = newString;
                rowChange = true;
                return true;
            }
            return false;
        }
        public bool SetCellText(DataGridViewCell cell, long newInteger)
        {
            if ((string)cell.Value != newInteger.ToString())
            {
                cell.Value = newInteger.ToString();
                rowChange = true;
                return true;
            }
            return false;
        }
        byte ColorMix(byte a, byte b, double alpha)
        {
            return (byte)(b * alpha + a * (1 - alpha));
        }
        Color ColorMix(Color a, Color b, double alpha)
        {
            return Color.FromArgb(ColorMix(a.R, b.R, alpha),
                ColorMix(a.G, b.G, alpha),
                ColorMix(a.B, b.B, alpha));
        }
        public void UpdateLogThread()
        {
            while (workerThread != null)
            {
                Configuration config = controller.GetCurrentConfiguration();
                ServerSpeedLogShow[] _ServerSpeedLogList = new ServerSpeedLogShow[config.configs.Count];
                for (int i = 0; i < config.configs.Count; ++i)
                {
                    _ServerSpeedLogList[i] = config.configs[i].ServerSpeedLog().Translate();
                }
                ServerSpeedLogList = _ServerSpeedLogList;

                workerThread.Suspend();
            }
        }
        public void UpdateLog()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(this.UpdateLogThread);
                workerThread.Start();
            }
            else
            {
                try
                {
                    workerThread.Resume();
                }
                catch
                {

                }
            }
        }
        public void RefreshLog()
        {
            if (ServerSpeedLogList == null)
                return;

            Configuration config = controller.GetCurrentConfiguration();
            if (listOrder.Count > config.configs.Count)
            {
                listOrder.RemoveRange(config.configs.Count, listOrder.Count - config.configs.Count);
            }
            while (listOrder.Count < config.configs.Count)
            {
                listOrder.Add(0);
            }
            while (ServerDataGrid.Rows.Count < config.configs.Count)
            {
                ServerDataGrid.Rows.Add();
                int id = ServerDataGrid.Rows.Count - 1;
                ServerDataGrid[0, id].Value = id;
            }
            if (ServerDataGrid.Rows.Count > config.configs.Count)
            {
                for (int list_index = 0; list_index < ServerDataGrid.Rows.Count; ++list_index)
                {
                    DataGridViewCell id_cell = ServerDataGrid[0, list_index];
                    int id = (int)id_cell.Value;
                    if (id >= config.configs.Count)
                    {
                        ServerDataGrid.Rows.RemoveAt(list_index);
                        --list_index;
                    }
                }
            }
            //int index = 0;
            //foreach (Server server in config.configs)
            //for ( int id = 0; id < config.configs.Count; ++id)
            try
            {
                for (int list_index = (lastRefreshIndex >= ServerDataGrid.Rows.Count) ? 0 : lastRefreshIndex, rowChangeCnt = 0;
                    list_index < ServerDataGrid.Rows.Count && rowChangeCnt <= 200;
                    ++list_index, ++rowChangeCnt)
                {
                    lastRefreshIndex = list_index + 1;
                    DataGridViewCell id_cell = ServerDataGrid[0, list_index];
                    int id = (int)id_cell.Value;
                    Server server = config.configs[id];
                    ServerSpeedLogShow serverSpeedLog = ServerSpeedLogList[id];
                    listOrder[id] = list_index;
                    rowChange = false;
                    for (int curcol = 0; curcol < ServerDataGrid.Columns.Count; ++curcol)
                    {
                        DataGridViewCell cell = ServerDataGrid[curcol, list_index];
                        string columnName = ServerDataGrid.Columns[curcol].Name;
                        // Server
                        if (columnName == "Server")
                        {
                            if (config.index == id)
                                SetBackColor(cell, Color.Cyan);
                            else
                                SetBackColor(cell, Color.White);
                            SetCellText(cell, server.FriendlyName());
                        }
                        // Enable
                        if (columnName == "Enable")
                        {
                            if (server.isEnable())
                                SetBackColor(cell, Color.White);
                            else
                                SetBackColor(cell, Color.Red);
                        }
                        // TotalConnectTimes
                        else if (columnName == "TotalConnect")
                        {
                            SetCellText(cell, serverSpeedLog.totalConnectTimes);
                        }
                        // TotalConnecting
                        else if (columnName == "Connecting")
                        {
                            //SetCellText(cell, server.GetConnections().Count);
                            SetCellText(cell, serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
                        }
                        // AvgConnectTime
                        else if (columnName == "AvgLatency")
                        {
                            if (serverSpeedLog.avgConnectTime >= 0)
                                SetCellText(cell, serverSpeedLog.avgConnectTime);
                            else
                                SetCellText(cell, "-");
                        }
                        // AvgSpeed
                        else if (columnName == "AvgSpeed")
                        {
                            long avgBytes = serverSpeedLog.avgDownloadBytes;
                            string valStr = FormatBytes(avgBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (avgBytes < bytesList[i])
                                {
                                    SetBackColor(cell,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(avgBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // MaxSpeed
                        else if (columnName == "MaxSpeed")
                        {
                            long maxBytes = serverSpeedLog.maxDownloadBytes;
                            string valStr = FormatBytes(maxBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (maxBytes < bytesList[i])
                                {
                                    SetBackColor(cell,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(maxBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // TotalUploadBytes
                        else if (columnName == "Upload")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalUploadBytes);
                            string fullVal = serverSpeedLog.totalUploadBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetBackColor(cell, Color.White);
                                else
                                {
                                    SetBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0) SetBackColor(cell, Color.White);
                                //Color col = cell.Style.BackColor;
                                //SetBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        // TotalDownloadBytes
                        else if (columnName == "Download")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadBytes);
                            string fullVal = serverSpeedLog.totalDownloadBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetBackColor(cell, Color.White);
                                else
                                {
                                    SetBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0) SetBackColor(cell, Color.White);
                                //Color col = cell.Style.BackColor;
                                //SetBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        else if (columnName == "DownloadRaw")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadRawBytes);
                            string fullVal = serverSpeedLog.totalDownloadRawBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetBackColor(cell, Color.White);
                                else
                                {
                                    SetBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0) SetBackColor(cell, Color.White);
                                //Color col = cell.Style.BackColor;
                                //SetBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        // ErrorConnectTimes
                        else if (columnName == "ConnectError")
                        {
                            long val = serverSpeedLog.errorConnectTimes + serverSpeedLog.errorDecodeTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 2.5), (byte)Math.Max(0, 255 - val * 2.5));
                            SetBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorTimeoutTimes
                        else if (columnName == "ConnectTimeout")
                        {
                            SetCellText(cell, serverSpeedLog.errorTimeoutTimes);
                        }
                        // ErrorTimeoutTimes
                        else if (columnName == "ConnectEmpty")
                        {
                            long val = serverSpeedLog.errorEmptyTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            SetBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorContinurousTimes
                        else if (columnName == "Continuous")
                        {
                            long val = serverSpeedLog.errorContinurousTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            SetBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorPersent
                        else if (columnName == "ErrorPercent")
                        {
                            if (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes > 0)
                            {
                                double percent = (serverSpeedLog.errorConnectTimes
                                    + serverSpeedLog.errorTimeoutTimes
                                    + serverSpeedLog.errorDecodeTimes)
                                    * 100.00
                                    / (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
                                SetBackColor(cell, Color.FromArgb(255, (byte)(255 - percent * 2), (byte)(255 - percent * 2)));
                                SetCellText(cell, percent.ToString("F0") + "%");
                            }
                            else
                            {
                                SetBackColor(cell, Color.White);
                                SetCellText(cell, "-");
                            }
                        }
                        if (columnName == "Server")
                        {
                            if (cell.Style.Alignment != DataGridViewContentAlignment.MiddleLeft)
                                cell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                        }
                        else
                        {
                            if (cell.Style.Alignment != DataGridViewContentAlignment.MiddleRight)
                                cell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                        }
                    }
                    if (rowChange)
                        rowChangeCnt++;
                }
            }
            catch
            {

            }
            if (ServerDataGrid.SortedColumn != null)
            {
                ServerDataGrid.Sort(ServerDataGrid.SortedColumn, (ListSortDirection)((int)ServerDataGrid.SortOrder - 1));
            }
        }

        private void ClearItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            foreach (Server server in config.configs)
            {
                server.ServerSpeedLog().Clear();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            RefreshLog();
            UpdateLog();
        }

        private void ServerDataGrid_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenu1.Show(ServerDataGrid, new Point(e.X, e.Y));
            }
        }

        private void ServerDataGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int id = (int)ServerDataGrid[0, e.RowIndex].Value;
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "Server")
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    controller.SelectServerIndex(id);
                }
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "Enable")
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    Server server = config.configs[id];
                    server.setEnable(!server.isEnable());
                    controller.SelectServerIndex(config.index);
                }
                ServerDataGrid[0, e.RowIndex].Selected = true;
            }
        }

        private void ServerDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int id = (int)ServerDataGrid[0, e.RowIndex].Value;
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "Server")
                {
                    controller.ShowConfigForm();
                }
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "Connecting")
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    Server server = config.configs[id];
                    server.GetConnections().CloseAll();
                }
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "ErrorPercent")
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    config.configs[id].ServerSpeedLog().Clear();
                    config.configs[id].setEnable(true);
                }
                if (ServerDataGrid.Columns[e.ColumnIndex].Name == "ConnectError"
                    || ServerDataGrid.Columns[e.ColumnIndex].Name == "ConnectTimeout"
                    || ServerDataGrid.Columns[e.ColumnIndex].Name == "ConnectEmpty"
                    || ServerDataGrid.Columns[e.ColumnIndex].Name == "Continuous"
                    )
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    config.configs[id].ServerSpeedLog().ClearError();
                    config.configs[id].setEnable(true);
                }
                ServerDataGrid[0, e.RowIndex].Selected = true;
            }
        }

        private void ServerLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
            Thread thread = workerThread;
            workerThread = null;
            thread.Resume();
        }

        private long Str2Long(String str)
        {
            if (str == "-") return -1;
            if (str.LastIndexOf('K') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('K')));
                return (long)(ret * 1024);
            }
            if (str.LastIndexOf('M') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('M')));
                return (long)(ret * 1024 * 1024);
            }
            if (str.LastIndexOf('G') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('G')));
                return (long)(ret * 1024 * 1024 * 1024);
            }
            if (str.LastIndexOf('T') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('T')));
                return (long)(ret * 1024 * 1024 * 1024 * 1024);
            }
            try
            {
                Double ret = Convert.ToDouble(str);
                return (long)ret;
            }
            catch
            {
                return -1;
            }
        }

        private void ServerDataGrid_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            //e.SortResult = 0;
            if (e.Column.Name == "Server")
            {
                e.SortResult = System.String.Compare(Convert.ToString(e.CellValue1), Convert.ToString(e.CellValue2));
                e.Handled = true;
            }
            else if (e.Column.Name == "ID"
                || e.Column.Name == "TotalConnect"
                || e.Column.Name == "Connecting"
                || e.Column.Name == "ConnectError"
                || e.Column.Name == "ConnectTimeout"
                || e.Column.Name == "Continuous"
                )
            {
                Int32 v1 = Convert.ToInt32(e.CellValue1);
                Int32 v2 = Convert.ToInt32(e.CellValue2);
                e.SortResult = (v1 == v2 ? 0 : (v1 < v2 ? -1 : 1));
            }
            else if (e.Column.Name == "ErrorPercent")
            {
                String s1 = Convert.ToString(e.CellValue1);
                String s2 = Convert.ToString(e.CellValue2);
                Int32 v1 = s1.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s1.Substring(0, s1.Length - 1)) * 100);
                Int32 v2 = s2.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s2.Substring(0, s2.Length - 1)) * 100);
                e.SortResult = v1 == v2 ? 0 : v1 < v2 ? -1 : 1;
            }
            else if (e.Column.Name == "AvgLatency"
                || e.Column.Name == "AvgSpeed"
                || e.Column.Name == "MaxSpeed"
                || e.Column.Name == "Upload"
                || e.Column.Name == "Download"
                || e.Column.Name == "DownloadRaw"
                )
            {
                String s1 = Convert.ToString(e.CellValue1);
                String s2 = Convert.ToString(e.CellValue2);
                long v1 = Str2Long(s1);
                long v2 = Str2Long(s2);
                e.SortResult = (v1 == v2 ? 0 : (v1 < v2 ? -1 : 1));
            }
            if (e.SortResult == 0)
            {
                int v1 = listOrder[Convert.ToInt32(ServerDataGrid[0, e.RowIndex1].Value)];
                int v2 = listOrder[Convert.ToInt32(ServerDataGrid[0, e.RowIndex2].Value)];
                e.SortResult = (v1 == v2 ? 0 : (v1 < v2 ? -1 : 1));
                if (e.SortResult != 0 && ServerDataGrid.SortOrder == SortOrder.Descending)
                {
                    e.SortResult = -e.SortResult;
                }
            }
            if (e.SortResult != 0)
            {
                e.Handled = true;
            }
        }
    }
}
