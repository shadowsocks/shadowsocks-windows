using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using SimpleJson;

namespace Shadowsocks.View
{
    public partial class StatisticsStrategyConfigurationForm: Form
    {
        private ShadowsocksController _controller;
        private StatisticsStrategyConfiguration _configuration;

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
            _configuration = _controller.GetConfigurationCopy()?.statisticsStrategyConfiguration
                             ?? new StatisticsStrategyConfiguration();
        }

        private void InitData()
        {
            bindingConfiguration.Add(_configuration);
            foreach (var kv in _configuration.Calculations)
            {
                var calculation = new CalculationControl(kv.Key);
                calculationContainer.Controls.Add(calculation);
            }
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
            Close();
        }
    }
}
