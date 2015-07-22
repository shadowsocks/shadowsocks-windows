using Shadowsocks.Controller;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Controller.Strategy
{
    class StrategyManager
    {
        List<IStrategy> _strategies;
        public StrategyManager(ShadowsocksController controller)
        {
            _strategies = new List<IStrategy>();
            _strategies.Add(new BalancingStrategy(controller));
        }
        public IList<IStrategy> GetStrategies()
        {
            return _strategies;
        }
    }
}
