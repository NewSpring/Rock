using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rock.Model.Connection.ConnectionType.DTO
{
    /// <summary>
    /// Represents completion related metrics for connection requests
    /// within a specified date range for a single connection type.
    /// </summary>
    internal class ConnectionRequestCompletionMetricsSummary
    {
        /// <summary>
        /// The identifier of the connection type this summary applies to.
        /// </summary>
        public int ConnectionTypeId { get; set; }

        /// <summary>
        /// The percentage of completed connection requests that were
        /// completed on or before their due date.
        /// </summary>
        public decimal TimelinessPercent { get; set; }

        /// <summary>
        /// The average number of days between request creation and
        /// first logged activity.
        /// </summary>
        public decimal AverageResponsivenessDays { get; set; }

        /// <summary>
        /// The total number of connection requests completed
        /// within the date range.
        /// </summary>
        public int RequestsCompletedCount { get; set; }

        /// <summary>
        /// The average number of days between request creation and
        /// completion.
        /// </summary>
        public decimal AverageCompletionDays { get; set; }
    }
}
