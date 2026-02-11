using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.ViewModels.Core.Grid;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionOperationalSnapshot
{
    public class MetricsBag
    {
        /// <summary>
        /// Gets or sets the current snapshot of connection request state.
        /// </summary>
        public RequestStateBag RequestState { get; set; }

        /// <summary>
        /// Gets or sets the time-based workload and upcoming follow-up data.
        /// </summary>
        public RequestTimelineBag RequestTimeline { get; set; }

        /// <summary>
        /// Gets or sets the metrics related to completion quality and efficiency.
        /// </summary>
        public CompletionMetricsBag CompletionMetrics { get; set; }
    }
}
