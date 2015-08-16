using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shadowsocks.Controller.Strategy;

namespace Shadowsocks.Model
{
    [Serializable]
    public class StatisticsStrategyConfiguration
    {
        public static readonly string ID = "com.shadowsocks.strategy.statistics"; 
        private bool _statisticsEnabled;
        private bool _byIsp;
        private bool _byHourOfDay;
        private int _choiceKeptMinutes;
        private int _dataCollectionMinutes;
        private int _repeatTimesNum;

        public Dictionary<string, float> Calculations;

        public StatisticsStrategyConfiguration()
        {
            var statisticsStrategy = typeof (StatisticsStrategy);
            var statisticsData = statisticsStrategy.GetNestedType("StatisticsData");
            var properties = statisticsData.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Calculations = properties.ToDictionary(p => p.Name, _ => (float) 0);
        }

        public bool StatisticsEnabled
        {
            get { return _statisticsEnabled; }
            set { _statisticsEnabled = value; }
        }

        public bool ByIsp
        {
            get { return _byIsp; }
            set { _byIsp = value; }
        }

        public bool ByHourOfDay
        {
            get { return _byHourOfDay; }
            set { _byHourOfDay = value; }
        }

        public int ChoiceKeptMinutes
        {
            get { return _choiceKeptMinutes; }
            set { _choiceKeptMinutes = value; }
        }

        public int DataCollectionMinutes
        {
            get { return _dataCollectionMinutes; }
            set { _dataCollectionMinutes = value; }
        }

        public int RepeatTimesNum
        {
            get { return _repeatTimesNum; }
            set { _repeatTimesNum = value; }
        }
    }
}
