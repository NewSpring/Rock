// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

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
