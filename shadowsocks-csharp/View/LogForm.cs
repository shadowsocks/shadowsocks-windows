using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;

using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Model;
using Shadowsocks.Util;
using System.Text;

namespace Shadowsocks.View
{
    public partial class LogForm : Form
    {
        long lastOffset;
        string filename;
        Timer timer;
        const int BACK_OFFSET = 65536;
        ShadowsocksController controller;

        // global traffic update lock, make it static
        private static readonly object _lock = new object();

        #region Traffic Chart
        Queue<TrafficInfo> trafficInfoQueue = new Queue<TrafficInfo>();
        const int queueMaxLength = 60;
        long lastInbound, lastOutbound;
        long maxSpeed = 0, lastMaxSpeed = 0;
        const long minScale = 50;
        BandwidthScaleInfo bandwidthScale;
        List<float> inboundPoints = new List<float>();
        List<float> outboundPoints = new List<float>();
        TextAnnotation inboundAnnotation = new TextAnnotation();
        TextAnnotation outboundAnnotation = new TextAnnotation();
        #endregion

        public LogForm(ShadowsocksController controller, string filename)
        {
            this.controller = controller;
            this.filename = filename;
            InitializeComponent();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;

            topMostTrigger = config.topMost;
            wrapTextTrigger = config.wrapText;
            toolbarTrigger = config.toolbarShown;
            LogMessageTextBox.BackColor = config.BackgroundColor;
            LogMessageTextBox.ForeColor = config.TextColor;
            LogMessageTextBox.Font = config.Font;

            controller.TrafficChanged += controller_TrafficChanged;

            UpdateTexts();
        }

        private void UpdateTrafficChart()
        {
            lock (_lock)
            {
                if (trafficInfoQueue.Count == 0)
                    return;

                inboundPoints.Clear();
                outboundPoints.Clear();
                maxSpeed = 0;
                foreach (var trafficInfo in trafficInfoQueue)
                {
                    inboundPoints.Add(trafficInfo.inbound);
                    outboundPoints.Add(trafficInfo.outbound);
                    maxSpeed = Math.Max(maxSpeed, Math.Max(trafficInfo.inbound, trafficInfo.outbound));
                }
                lastInbound = trafficInfoQueue.Last().inbound;
                lastOutbound = trafficInfoQueue.Last().outbound;
            }

            if (maxSpeed > 0)
            {
                lastMaxSpeed -= lastMaxSpeed / 32;
                maxSpeed = Math.Max(minScale, Math.Max(maxSpeed, lastMaxSpeed));
                lastMaxSpeed = maxSpeed;
            }
            else
            {
                maxSpeed = lastMaxSpeed = minScale;
            }

            bandwidthScale = Utils.GetBandwidthScale(maxSpeed);

            // re-scale the original data points, since it is List<float>, .ForEach does not work
            inboundPoints = inboundPoints.Select(p => p / bandwidthScale.unit).ToList();
            outboundPoints = outboundPoints.Select(p => p / bandwidthScale.unit).ToList();

            if (trafficChart.IsHandleCreated)
            {
                trafficChart.Series["Inbound"].Points.DataBindY(inboundPoints);
                trafficChart.Series["Outbound"].Points.DataBindY(outboundPoints);
                trafficChart.ChartAreas[0].AxisY.LabelStyle.Format = "{0:0.##} " + bandwidthScale.unitName;
                trafficChart.ChartAreas[0].AxisY.Maximum = bandwidthScale.value;
                inboundAnnotation.AnchorDataPoint = trafficChart.Series["Inbound"].Points.Last();
                inboundAnnotation.Text = Utils.FormatBandwidth(lastInbound);
                outboundAnnotation.AnchorDataPoint = trafficChart.Series["Outbound"].Points.Last();
                outboundAnnotation.Text = Utils.FormatBandwidth(lastOutbound);
                trafficChart.Annotations.Clear();
                trafficChart.Annotations.Add(inboundAnnotation);
                trafficChart.Annotations.Add(outboundAnnotation);
            }
        }

        private void controller_TrafficChanged(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (trafficInfoQueue.Count == 0)
                {
                    // Init an empty queue
                    for (int i = 0; i < queueMaxLength; i++)
                    {
                        trafficInfoQueue.Enqueue(new TrafficInfo(0, 0));
                    }

                    foreach (var trafficPerSecond in controller.trafficPerSecondQueue)
                    {
                        trafficInfoQueue.Enqueue(new TrafficInfo(trafficPerSecond.inboundIncreasement,
                                                                 trafficPerSecond.outboundIncreasement));
                        if (trafficInfoQueue.Count > queueMaxLength)
                            trafficInfoQueue.Dequeue();
                    }
                }
                else
                {
                    var lastTraffic = controller.trafficPerSecondQueue.Last();
                    trafficInfoQueue.Enqueue(new TrafficInfo(lastTraffic.inboundIncreasement,
                                                             lastTraffic.outboundIncreasement));
                    if (trafficInfoQueue.Count > queueMaxLength)
                        trafficInfoQueue.Dequeue();
                }
            }
        }

        private void UpdateTexts()
        {
            FileMenuItem.Text = I18N.GetString("&File");
            OpenLocationMenuItem.Text = I18N.GetString("&Open Location");
            ExitMenuItem.Text = I18N.GetString("E&xit");
            CleanLogsButton.Text = I18N.GetString("&Clean Logs");
            ChangeFontButton.Text = I18N.GetString("Change &Font");
            WrapTextCheckBox.Text = I18N.GetString("&Wrap Text");
            TopMostCheckBox.Text = I18N.GetString("&Top Most");
            ViewMenuItem.Text = I18N.GetString("&View");
            CleanLogsMenuItem.Text = I18N.GetString("&Clean Logs");
            ChangeFontMenuItem.Text = I18N.GetString("Change &Font");
            WrapTextMenuItem.Text = I18N.GetString("&Wrap Text");
            TopMostMenuItem.Text = I18N.GetString("&Top Most");
            ShowToolbarMenuItem.Text = I18N.GetString("&Show Toolbar");
            Text = I18N.GetString("Log Viewer");
            // traffic chart
            trafficChart.Series["Inbound"].LegendText = I18N.GetString("Inbound");
            trafficChart.Series["Outbound"].LegendText = I18N.GetString("Outbound");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateContent();
            UpdateTrafficChart();
        }

        private void InitContent()
        {
            using (StreamReader reader = new StreamReader(new FileStream(filename,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (reader.BaseStream.Length > BACK_OFFSET)
                {
                    reader.BaseStream.Seek(-BACK_OFFSET, SeekOrigin.End);
                    reader.ReadLine();
                }

                string line = "";
                StringBuilder appendText = new StringBuilder(1024);
                while ((line = reader.ReadLine()) != null)
                    appendText.Append(line + Environment.NewLine);

                LogMessageTextBox.AppendText(appendText.ToString());
                LogMessageTextBox.ScrollToCaret();

                lastOffset = reader.BaseStream.Position;
            }
        }

        private void UpdateContent()
        {
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(filename,
                         FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    reader.BaseStream.Seek(lastOffset, SeekOrigin.Begin);

                    string line = "";
                    bool changed = false;
                    StringBuilder appendText = new StringBuilder(128);
                    while ((line = reader.ReadLine()) != null)
                    {
                        changed = true;
                        appendText.Append(line + Environment.NewLine);
                    }

                    if (changed)
                    {
                        LogMessageTextBox.AppendText(appendText.ToString());
                        LogMessageTextBox.ScrollToCaret();
                    }

                    lastOffset = reader.BaseStream.Position;
                }
            }
            catch (FileNotFoundException)
            {
            }

            this.Text = I18N.GetString("Log Viewer") +
                $" [in: {Utils.FormatBytes(controller.InboundCounter)}, out: {Utils.FormatBytes(controller.OutboundCounter)}]";
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            InitContent();

            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Timer_Tick;
            timer.Start();

            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;

            Height = config.Height;
            Width = config.Width;
            Top = config.BestTop;
            Left = config.BestLeft;
            if (config.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }

            topMostTriggerLock = true;
            TopMost = TopMostMenuItem.Checked = TopMostCheckBox.Checked = topMostTrigger;
            topMostTriggerLock = false;

            wrapTextTriggerLock = true;
            LogMessageTextBox.WordWrap = WrapTextMenuItem.Checked = WrapTextCheckBox.Checked = wrapTextTrigger;
            wrapTextTriggerLock = false;

            ToolbarFlowLayoutPanel.Visible = ShowToolbarMenuItem.Checked = toolbarTrigger;
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
            controller.TrafficChanged -= controller_TrafficChanged;
            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;

            config.topMost = topMostTrigger;
            config.wrapText = wrapTextTrigger;
            config.toolbarShown = toolbarTrigger;
            config.Font = LogMessageTextBox.Font;
            config.BackgroundColor = LogMessageTextBox.BackColor;
            config.TextColor = LogMessageTextBox.ForeColor;
            if (WindowState != FormWindowState.Minimized && !(config.Maximized = WindowState == FormWindowState.Maximized))
            {
                config.Top = Top;
                config.Left = Left;
                config.Height = Height;
                config.Width = Width;
            }
            controller.SaveLogViewerConfig(config);
        }

        private void OpenLocationMenuItem_Click(object sender, EventArgs e)
        {
            string argument = "/select, \"" + filename + "\"";
            Logging.Debug(argument);
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LogForm_Shown(object sender, EventArgs e)
        {
            LogMessageTextBox.ScrollToCaret();
        }

        #region Clean up the content in LogMessageTextBox.
        private void DoCleanLogs()
        {
            Logging.Clear();
            lastOffset = 0;
            LogMessageTextBox.Clear();
        }

        private void CleanLogsMenuItem_Click(object sender, EventArgs e)
        {
            DoCleanLogs();
        }

        private void CleanLogsButton_Click(object sender, EventArgs e)
        {
            DoCleanLogs();
        }
        #endregion

        #region Change the font settings applied in LogMessageTextBox.
        private void DoChangeFont()
        {
            try
            {
                FontDialog fd = new FontDialog();
                fd.Font = LogMessageTextBox.Font;
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    LogMessageTextBox.Font = new Font(fd.Font.FontFamily, fd.Font.Size, fd.Font.Style);
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangeFontMenuItem_Click(object sender, EventArgs e)
        {
            DoChangeFont();
        }

        private void ChangeFontButton_Click(object sender, EventArgs e)
        {
            DoChangeFont();
        }
        #endregion

        #region Trigger the log messages to wrapable, or not.
        bool wrapTextTrigger = false;
        bool wrapTextTriggerLock = false;

        private void TriggerWrapText()
        {
            wrapTextTriggerLock = true;

            wrapTextTrigger = !wrapTextTrigger;
            LogMessageTextBox.WordWrap = wrapTextTrigger;
            LogMessageTextBox.ScrollToCaret();
            WrapTextMenuItem.Checked = WrapTextCheckBox.Checked = wrapTextTrigger;

            wrapTextTriggerLock = false;
        }

        private void WrapTextMenuItem_Click(object sender, EventArgs e)
        {
            if (!wrapTextTriggerLock)
            {
                TriggerWrapText();
            }
        }

        private void WrapTextCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!wrapTextTriggerLock)
            {
                TriggerWrapText();
            }
        }
        #endregion

        #region Trigger the window to top most, or not.
        bool topMostTrigger = false;
        bool topMostTriggerLock = false;

        private void TriggerTopMost()
        {
            topMostTriggerLock = true;

            topMostTrigger = !topMostTrigger;
            TopMost = topMostTrigger;
            TopMostMenuItem.Checked = TopMostCheckBox.Checked = topMostTrigger;

            topMostTriggerLock = false;
        }

        private void TopMostCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!topMostTriggerLock)
            {
                TriggerTopMost();
            }
        }

        private void TopMostMenuItem_Click(object sender, EventArgs e)
        {
            if (!topMostTriggerLock)
            {
                TriggerTopMost();
            }
        }
        #endregion

        private bool toolbarTrigger = false;

        private void ShowToolbarMenuItem_Click(object sender, EventArgs e)
        {
            toolbarTrigger = !toolbarTrigger;
            ToolbarFlowLayoutPanel.Visible = toolbarTrigger;
            ShowToolbarMenuItem.Checked = toolbarTrigger;
        }

        private class TrafficInfo
        {
            public long inbound;
            public long outbound;

            public TrafficInfo(long inbound, long outbound)
            {
                this.inbound = inbound;
                this.outbound = outbound;
            }
        }
    }
}
