using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;

using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class StatisticsStrategyConfiguration
    {
        public static readonly string ID = "com.shadowsocks.strategy.statistics";
        public bool StatisticsEnabled { get; set; } = false;
        public bool ByHourOfDay { get; set; } = true;
        public bool Ping { get; set; }
        public int ChoiceKeptMinutes { get; set; } = 10;
        public int DataCollectionMinutes { get; set; } = 10;
        public int RepeatTimesNum { get; set; } = 4;

        private const string ConfigFile = "statistics-config.json";

        public static StatisticsStrategyConfiguration Load()
        {
            try
            {
                var content = File.ReadAllText(ConfigFile);
                var configuration = JsonConvert.DeserializeObject<StatisticsStrategyConfiguration>(content);
                return configuration;
            }
            catch (FileNotFoundException)
            {
                var configuration = new StatisticsStrategyConfiguration();
                Save(configuration);
                return configuration;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return new StatisticsStrategyConfiguration();
            }
        }

        public static void Save(StatisticsStrategyConfiguration configuration)
        {
            try
            {
                var content = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(ConfigFile, content);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public Dictionary<string, float> Calculations;

        public StatisticsStrategyConfiguration()
        {
            var properties = typeof(StatisticsRecord).GetFields(BindingFlags.Instance | BindingFlags.Public);
            Calculations = properties.ToDictionary(p => p.Name, _ => (float)0);
        }
    }
}
