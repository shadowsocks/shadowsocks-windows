using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class StatisticsStrategyConfigurationForm : Form
    {
        private readonly ShadowsocksController _controller;
        private StatisticsStrategyConfiguration _configuration;
        private DataTable _dataTable = new DataTable();
        private List<string> _servers;

        public StatisticsStrategyConfigurationForm(ShadowsocksController controller)
        {
            if (controller == null) return;
            InitializeComponent();
            _controller = controller;
            _controller.ConfigChanged += (sender, args) => LoadConfiguration();
            LoadConfiguration();
            Load += (sender, args) => InitData();
        }

        private void LoadConfiguration()
        {
            var configs = _controller.GetCurrentConfiguration().configs;
            _servers = configs.Select(server => server.FriendlyName()).ToList();
            _configuration = _controller.StatisticsConfiguration
                             ?? new StatisticsStrategyConfiguration();
            if (_configuration.Calculations == null)
            {
                _configuration = new StatisticsStrategyConfiguration();
            }
        }

        private void InitData()
        {
            bindingConfiguration.Add(_configuration);
            foreach (var kv in _configuration.Calculations)
            {
                var calculation = new CalculationControl(kv.Key, kv.Value);
                calculationContainer.Controls.Add(calculation);
            }

            serverSelector.DataSource = _servers;

            _dataTable.Columns.Add("Timestamp", typeof(DateTime));
            _dataTable.Columns.Add("Package Loss", typeof(int));
            _dataTable.Columns.Add("Ping", typeof(int));

            StatisticsChart.Series["Package Loss"].XValueMember = "Timestamp";
            StatisticsChart.Series["Package Loss"].YValueMembers = "Package Loss";
            StatisticsChart.Series["Ping"].XValueMember = "Timestamp";
            StatisticsChart.Series["Ping"].YValueMembers = "Ping";
            StatisticsChart.DataSource = _dataTable;
            loadChartData();
            StatisticsChart.DataBind();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            foreach (CalculationControl calculation in calculationContainer.Controls)
            {
                _configuration.Calculations[calculation.Value] = calculation.Factor;
            }
            _controller?.SaveStrategyConfigurations(_configuration);
            _controller?.UpdateStatisticsConfiguration(StatisticsEnabledCheckBox.Checked);
            Close();
        }

        private void loadChartData()
        {
            string serverName = _servers[serverSelector.SelectedIndex];
            _dataTable.Rows.Clear();

            //return directly when no data is usable
            if (_controller.availabilityStatistics?.FilteredStatistics == null) return;
            List<AvailabilityStatistics.RawStatisticsData> statistics;
            if (!_controller.availabilityStatistics.FilteredStatistics.TryGetValue(serverName, out statistics)) return;
            IEnumerable<IGrouping<int, AvailabilityStatistics.RawStatisticsData>> dataGroups;
            if (allMode.Checked)
            {
                dataGroups = statistics.GroupBy(data => data.Timestamp.DayOfYear);
                StatisticsChart.ChartAreas["DataArea"].AxisX.LabelStyle.Format = "MM/dd/yyyy";
                StatisticsChart.ChartAreas["DataArea"].AxisX2.LabelStyle.Format = "MM/dd/yyyy";
            }
            else
            {
                dataGroups = statistics.GroupBy(data => data.Timestamp.Hour);
                StatisticsChart.ChartAreas["DataArea"].AxisX.LabelStyle.Format = "HH:00";
                StatisticsChart.ChartAreas["DataArea"].AxisX2.LabelStyle.Format = "HH:00";
            }
            var finalData = from dataGroup in dataGroups
                            orderby dataGroup.Key
                            select new
                            {
                                Timestamp = dataGroup.First().Timestamp,
                                Ping = (int)dataGroup.Average(data => data.RoundtripTime),
                                PackageLoss = (int)
                                              (dataGroup.Count(data => data.ICMPStatus == IPStatus.TimedOut.ToString())
                                              / (float)dataGroup.Count() * 100)
                            };
            foreach (var data in finalData)
            {
                _dataTable.Rows.Add(data.Timestamp, data.PackageLoss, data.Ping);
            }
            StatisticsChart.DataBind();
        }

        private void serverSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadChartData();
        }

        private void chartModeSelector_Enter(object sender, EventArgs e)
        {

        }

        private void dayMode_CheckedChanged(object sender, EventArgs e)
        {
            loadChartData();
        }

        private void allMode_CheckedChanged(object sender, EventArgs e)
        {
            loadChartData();
        }
    }
}
