using System.Collections.Generic;

namespace Shadowsocks.Controller.Strategy
{
    class StrategyManager
    {
        List<IStrategy> _strategies;
        public StrategyManager(ShadowsocksController controller)
        {
            _strategies = new List<IStrategy>
            {
                new BalancingStrategy(controller),
                new HighAvailabilityStrategy(controller),
                new StatisticsStrategy(controller)
            };
            // TODO: load DLL plugins
        }
        public IList<IStrategy> GetStrategies()
        {
            return _strategies;
        }
    }
}
