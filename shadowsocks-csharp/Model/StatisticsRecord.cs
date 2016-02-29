using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shadowsocks.Model
{
    // Simple processed records for a short period of time
    public class StatisticsRecord
    {
        public DateTime Timestamp;

        public string ServerName;

        // these fields ping-only records would be null
        public int? AverageLatency;
        public int? MinLatency;
        public int? MaxLatency;

        public int? AverageInboundSpeed;
        public int? MinInboundSpeed;
        public int? MaxInboundSpeed;

        public int? AverageOutboundSpeed;
        public int? MinOutboundSpeed;
        public int? MaxOutboundSpeed;

        // if user disabled ping test, response would be null
        public int? AverageResponse;
        public int? MinResponse;
        public int? MaxResponse;
        public float? PackageLoss;

        public StatisticsRecord()
        {
        }

        public StatisticsRecord(string identifier, IEnumerable<int> inboundSpeedRecords, IEnumerable<int> outboundSpeedRecords, IEnumerable<int> latencyRecords)
        {
            Timestamp = DateTime.Now;
            ServerName = identifier;
            if (inboundSpeedRecords != null && inboundSpeedRecords.Any())
            {
                AverageInboundSpeed = (int) inboundSpeedRecords.Average();
                MinInboundSpeed = inboundSpeedRecords.Min();
                MaxInboundSpeed = inboundSpeedRecords.Max();
            }
            if (outboundSpeedRecords != null && outboundSpeedRecords.Any())
            {
                AverageOutboundSpeed = (int) outboundSpeedRecords.Average();
                MinOutboundSpeed = outboundSpeedRecords.Min();
                MaxOutboundSpeed = outboundSpeedRecords.Max();
            }
            if (latencyRecords != null && latencyRecords.Any())
            {
                AverageLatency = (int) latencyRecords.Average();
                MinLatency = latencyRecords.Min();
                MaxLatency = latencyRecords.Max();
            }
        }

        public StatisticsRecord(string identifier, IEnumerable<int?> responseRecords)
        {
            Timestamp = DateTime.Now;
            ServerName = identifier;
            setResponse(responseRecords);
        }

        public void setResponse(IEnumerable<int?> responseRecords)
        {
            if (responseRecords == null) return;
            var records = responseRecords.Where(response => response != null).Select(response => response.Value).ToList();
            if (!records.Any()) return;
            AverageResponse = (int?) records.Average();
            MinResponse = records.Min();
            MaxResponse = records.Max();
            PackageLoss = responseRecords.Count(response => response != null)/(float) responseRecords.Count();
        }
    }
}
