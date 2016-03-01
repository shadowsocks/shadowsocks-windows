using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shadowsocks.Model
{
    // Simple processed records for a short period of time
    public class StatisticsRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ServerIdentifier { get; set; }

        // in ping-only records, these fields would be null
        public int? AverageLatency;
        public int? MinLatency;
        public int? MaxLatency;

        private bool EmptyLatencyData => (AverageLatency == null) && (MinLatency == null) && (MaxLatency == null);

        public int? AverageInboundSpeed;
        public int? MinInboundSpeed;
        public int? MaxInboundSpeed;

        private bool EmptyInboundSpeedData
            => (AverageInboundSpeed == null) && (MinInboundSpeed == null) && (MaxInboundSpeed == null);

        public int? AverageOutboundSpeed;
        public int? MinOutboundSpeed;
        public int? MaxOutboundSpeed;

        private bool EmptyOutboundSpeedData
            => (AverageOutboundSpeed == null) && (MinOutboundSpeed == null) && (MaxOutboundSpeed == null);

        // if user disabled ping test, response would be null
        public int? AverageResponse;
        public int? MinResponse;
        public int? MaxResponse;
        public float? PackageLoss;

        private bool EmptyResponseData
            => (AverageResponse == null) && (MinResponse == null) && (MaxResponse == null) && (PackageLoss == null);

        public bool IsEmptyData() {
            return EmptyInboundSpeedData && EmptyOutboundSpeedData && EmptyResponseData && EmptyLatencyData;
        }

        public StatisticsRecord()
        {
        }

        public StatisticsRecord(string identifier, ICollection<int> inboundSpeedRecords, ICollection<int> outboundSpeedRecords, ICollection<int> latencyRecords)
        {
            ServerIdentifier = identifier;
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

        public StatisticsRecord(string identifier, ICollection<int?> responseRecords)
        {
            ServerIdentifier = identifier;
            SetResponse(responseRecords);
        }

        public void SetResponse(ICollection<int?> responseRecords)
        {
            if (responseRecords == null) return;
            var records = responseRecords.Where(response => response != null).Select(response => response.Value).ToList();
            if (!records.Any()) return;
            AverageResponse = (int?) records.Average();
            MinResponse = records.Min();
            MaxResponse = records.Max();
            PackageLoss = responseRecords.Count(response => response != null)/(float) responseRecords.Count;
        }
    }
}
