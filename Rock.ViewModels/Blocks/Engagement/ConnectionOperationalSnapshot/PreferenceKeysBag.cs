namespace Rock.ViewModels.Blocks.Engagement.ConnectionOperationalSnapshot
{
    /// <summary>
    /// Represents a container for preference keys used to store filter values in person preferences.
    /// </summary>
    public class PreferenceKeysBag
    {
        /// <summary>
        /// Gets or sets the preference key used to store the selected date range filter value.
        /// </summary>
        public string SelectedDateRangeFilter { get; set; }

        /// <summary>
        /// Gets or sets the preference key used to store the selected connection opportunity filter value.
        /// </summary>
        public string ConnectionOpportunityFilter { get; set; }
    }
}
