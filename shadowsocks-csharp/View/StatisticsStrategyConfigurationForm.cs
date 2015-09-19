using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using SimpleJson;

namespace Shadowsocks.View
{
    public partial class StatisticsStrategyConfigurationForm: Form
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

            _dataTable.Columns.Add("Timestamp", typeof (DateTime));
            _dataTable.Columns.Add("Package Loss", typeof (int));
            _dataTable.Columns.Add("Ping", typeof (int));

            StatisticsChart.Series["Package Loss"].XValueMember = "Timestamp";
            StatisticsChart.Series["Package Loss"].YValueMembers = "PackageLoss";
            StatisticsChart.Series["Ping"].XValueMember = "Timestamp";
            StatisticsChart.Series["Ping"].YValueMembers = "Ping";
            StatisticsChart.DataSource = _dataTable;
            loadChartData(serverSelector.SelectedText);
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

        private void loadChartData(string serverName)
        {
            _dataTable.Rows.Clear();
            List<AvailabilityStatistics.RawStatisticsData> statistics;
            if (!_controller.availabilityStatistics.FilteredStatistics.TryGetValue(serverName, out statistics)) return;
            foreach (var data in statistics)
            {
                _dataTable.Rows.Add(data.Timestamp, (float) new Random().Next() % 50, new Random().Next() % 200);
            }
        }

        private void serverSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadChartData(_servers[serverSelector.SelectedIndex]);
        }
    }
}
