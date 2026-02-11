
namespace Rock.Model.Connection.ConnectionType.DTO
{
    /// <summary>
    /// Represents a single status segment within the distribution of
    /// connection requests for a connection type.
    /// </summary>
    internal class ConnectionRequestStatusDistribution
    {
        /// <summary>
        /// The semantic color associated with the connection request status.
        /// This value is typically derived from the status definition.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// The name of the connection request status represented by this segment.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The number of connection requests that currently have this status.
        /// </summary>
        public int Count { get; set; }
    }
}
