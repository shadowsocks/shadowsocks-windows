using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class StatisticsStrategyConfigurationForm : Form
    {
        private readonly ShadowsocksController _controller;
        private StatisticsStrategyConfiguration _configuration;
        private readonly DataTable _dataTable = new DataTable();
        private List<string> _servers;
        private readonly Series _speedSeries;
        private readonly Series _packageLossSeries;
        private readonly Series _pingSeries;

        public StatisticsStrategyConfigurationForm(ShadowsocksController controller)
        {
            if (controller == null) return;
            InitializeComponent();
            _speedSeries = StatisticsChart.Series["Speed"];
            _packageLossSeries = StatisticsChart.Series["Package Loss"];
            _pingSeries = StatisticsChart.Series["Ping"];
            _controller = controller;
            _controller.ConfigChanged += (sender, args) => LoadConfiguration();
            LoadConfiguration();
            Load += (sender, args) => InitData();

        }
        /*****************************************************************************/
        private void LoadConfiguration()
        {
            var configs = _controller.GetCurrentConfiguration().configs;
            _servers = configs.Select(server => server.Identifier()).ToList();
            _configuration = _controller.StatisticsConfiguration
                             ?? new StatisticsStrategyConfiguration();
            if (_configuration.Calculations == null)
            {
                _configuration = new StatisticsStrategyConfiguration();
            }
        }
        /*****************************************************************************/
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
            _dataTable.Columns.Add("Speed", typeof (int));
            _speedSeries.XValueMember = "Timestamp";
            _speedSeries.YValueMembers = "Speed";

            // might be empty
            _dataTable.Columns.Add("Package Loss", typeof (int));
            _dataTable.Columns.Add("Ping", typeof (int));
            _packageLossSeries.XValueMember = "Timestamp";
            _packageLossSeries.YValueMembers = "Package Loss";
            _pingSeries.XValueMember = "Timestamp";
            _pingSeries.YValueMembers = "Ping";

            StatisticsChart.DataSource = _dataTable;
            LoadChartData();
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

        private void LoadChartData()
        {
            var serverName = _servers[serverSelector.SelectedIndex];
            _dataTable.Rows.Clear();

            //return directly when no data is usable
            if (_controller.availabilityStatistics?.FilteredStatistics == null) return;
            List<StatisticsRecord> statistics;
            if (!_controller.availabilityStatistics.FilteredStatistics.TryGetValue(serverName, out statistics)) return;
            IEnumerable<IGrouping<int, StatisticsRecord>> dataGroups;
            if (allMode.Checked)
            {
                _pingSeries.XValueType = ChartValueType.DateTime;
                _packageLossSeries.XValueType = ChartValueType.DateTime;
                _speedSeries.XValueType = ChartValueType.DateTime;
                dataGroups = statistics.GroupBy(data => data.Timestamp.DayOfYear);
                StatisticsChart.ChartAreas["DataArea"].AxisX.LabelStyle.Format = "g";
                StatisticsChart.ChartAreas["DataArea"].AxisX2.LabelStyle.Format = "g";
            }
            else
            {
                _pingSeries.XValueType = ChartValueType.Time;
                _packageLossSeries.XValueType = ChartValueType.Time;
                _speedSeries.XValueType = ChartValueType.Time;
                dataGroups = statistics.GroupBy(data => data.Timestamp.Hour);
                StatisticsChart.ChartAreas["DataArea"].AxisX.LabelStyle.Format = "HH:00";
                StatisticsChart.ChartAreas["DataArea"].AxisX2.LabelStyle.Format = "HH:00";
            }
            var finalData = from dataGroup in dataGroups
                            orderby dataGroup.Key
                            select new
                            {
                                dataGroup.First().Timestamp,
                                Speed = dataGroup.Max(data => data.MaxInboundSpeed) ?? 0,
                                Ping = (int) (dataGroup.Average(data => data.AverageResponse) ?? 0),
                                PackageLossPercentage = (int) (dataGroup.Average(data => data.PackageLoss) ?? 0) * 100
                            };
            foreach (var data in finalData.Where(data => data.Speed != 0 || data.PackageLossPercentage != 0 || data.Ping != 0))
            {
                _dataTable.Rows.Add(data.Timestamp, data.Speed, data.PackageLossPercentage, data.Ping);
            }
            StatisticsChart.DataBind();
        }

        private void serverSelector_SelectionChangeCommitted(object sender, EventArgs e)
        {
            LoadChartData();
        }

        private void dayMode_CheckedChanged(object sender, EventArgs e)
        {
            LoadChartData();
        }

        private void allMode_CheckedChanged(object sender, EventArgs e)
        {
            LoadChartData();
        }

        private void PingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            repeatTimesNum.ReadOnly = !PingCheckBox.Checked;
        }
    }
}
