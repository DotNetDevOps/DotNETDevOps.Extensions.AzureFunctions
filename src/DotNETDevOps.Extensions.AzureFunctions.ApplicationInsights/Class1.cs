using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;

namespace DotNETDevOps.Extensions.AzureFunctions.ApplicationInsights
{
    public class AggressivelySampleFastRequests : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;
        private readonly AdaptiveSamplingTelemetryProcessor samplingProcessor;
        public AggressivelySampleFastRequests(ITelemetryProcessor next)
        {
            // Next TelemetryProcessor in the chain
            _next = next;
            this.samplingProcessor = new AdaptiveSamplingTelemetryProcessor(next)
            {
                ExcludedTypes = "Event", // exclude custom events from being sampled
                MaxTelemetryItemsPerSecond = 1, // default: 5 calls/sec
                SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(1), // default: 2 min
                SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(1), // default: 30 sec
                EvaluationInterval = TimeSpan.FromSeconds(1), // default: 15 sec
                InitialSamplingPercentage = 25, // default: 100%
            };

        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request)
            {
                if (request.Duration < TimeSpan.FromMilliseconds(500) || request.ResponseCode == "200")
                {
                    // let sampling processor decide what to do
                    // with this fast incoming request
                    this.samplingProcessor.Process(item);
                    return;
                }


            }

            // Send the item to the next TelemetryProcessor
            _next.Process(item);
        }
    }
}
