using System.Collections.Generic;

namespace Rock.Model.Connection.ConnectionType.DTO
{
    /// <summary>
    /// Represents the count of upcoming connection request follow ups
    /// for a specific connection type within a defined future time window.
    /// </summary>
    internal class ConnectionRequestUpcomingFollowUpWindow
    {
        /// <summary>
        /// Gets or sets the identifier of the Connection Type associated with the upcoming Connection Requests.
        /// </summary>
        public int ConnectionTypeId { get; set; }

        /// <summary>
        /// The number of days from now that marks the inclusive start of the window.
        /// </summary>
        public int StartOffsetDays { get; set; }

        /// <summary>
        /// The number of days from now that marks the inclusive end of the window.
        /// </summary>
        public int EndOffsetDays { get; set; }

        /// <summary>
        /// The number of connection requests with follow ups scheduled
        /// within this window.
        /// </summary>
        public int Count { get; set; }
    }

}
