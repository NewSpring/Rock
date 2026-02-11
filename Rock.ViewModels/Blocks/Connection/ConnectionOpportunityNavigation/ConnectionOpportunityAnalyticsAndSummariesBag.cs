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

using System.Collections.Generic;
using System.Linq;

namespace Rock.ViewModels.Blocks.Connection.ConnectionOpportunityNavigation
{
    /// <summary>
    /// A bag that contains information about connection opportunity analytics and summaries for the Connection
    /// Opportunity Navigation block.
    /// </summary>
    public class ConnectionOpportunityAnalyticsAndSummariesBag
    {
        /// <summary>
        /// Gets or sets the list of connection opportunity summaries to display.
        /// </summary>
        public List<ConnectionOpportunitySummaryBag> ConnectionOpportunitySummaries { get; set; }

        /// <summary>
        /// Gets the total count of active connection requests for all <see cref="ConnectionOpportunitySummaries"/>.
        /// </summary>
        public int TotalActiveRequestsCount
        {
            get
            {
                if ( ConnectionOpportunitySummaries?.Any() != true )
                {
                    return 0;
                }

                return ConnectionOpportunitySummaries.Sum( s => s.ActiveRequestCount );
            }
        }
    }
}
