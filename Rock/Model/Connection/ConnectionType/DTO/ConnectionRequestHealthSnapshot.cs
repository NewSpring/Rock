namespace Rock.Model.Connection.ConnectionType.DTO
{
    /// <summary>
    /// Represents aggregate health counts for Connection Requests within a single Connection Type.
    /// Used to support health and timeliness visualizations such as the Request Health Overview chart.
    /// </summary>
    internal class ConnectionRequestHealthSnapshot
    {
        /// <summary>
        /// Gets or sets the number of Connection Requests that are currently on track.
        /// </summary>
        public int OnTrackCount
        {
            get
            {
                return ActiveCount - DueSoonCount - OverdueCount;
            }
        }

        /// <summary>
        /// Gets or sets the number of Connection Requests that are approaching their due threshold.
        /// </summary>
        public int DueSoonCount { get; set; }

        /// <summary>
        /// Gets or sets the number of Connection Requests that are past their due threshold.
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// Gets or sets the number of Connection Requests that have not been assigned to a Connector.
        /// </summary>
        public int UnassignedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of currently active Connection Requests.
        /// </summary>
        public int ActiveCount { get; set; }
    }
}
