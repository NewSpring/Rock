using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rock.Model.Connection.ConnectionType.DTO
{
    /// <summary>
    /// Represents completion metrics along with deltas compared
    /// to a previous equivalent time period.
    /// </summary>
    internal class ConnectionRequestCompletionMetricsComparison
    {
        /// <summary>
        /// The identifier of the connection type this comparison applies to.
        /// </summary>
        public int ConnectionTypeId { get; set; }

        /// <summary>
        /// Metrics for the current period.
        /// </summary>
        public ConnectionRequestCompletionMetricsSummary Current { get; set; }

        /// <summary>
        /// Metrics for the previous equivalent period.
        /// </summary>
        public ConnectionRequestCompletionMetricsSummary Previous { get; set; }

        /// <summary>
        /// Difference in timeliness percentage between periods.
        /// </summary>
        public decimal TimelinessPercentDelta { get; set; }

        /// <summary>
        /// Difference in average responsiveness days between periods.
        /// </summary>
        public decimal AverageResponsivenessDaysDelta { get; set; }

        /// <summary>
        /// Difference in completed request count between periods.
        /// </summary>
        public int RequestsCompletedCountDelta { get; set; }

        /// <summary>
        /// Difference in average completion days between periods.
        /// </summary>
        public decimal AverageCompletionDaysDelta { get; set; }
    }
}
