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

using System;
using System.Collections.Generic;

using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// The additional configuration options for the Connections Hub block.
    /// </summary>
    public class ConnectionsHubOptionsBag
    {
        public string Title { get; set; }

        public string IconCssClass { get; set; }

        public string ConnectionTypeIdKey { get; set; }

        public Guid? ConnectionOpportunityGuidFromPageParameter { get; set; }

        public bool IsFutureFollowUpEnabled { get; set; }

        public bool IsRequestSecurityEnabled { get; set; }

        public bool AreRemindersEnabled { get; set; }

        public bool AreCelebrationsEnabled { get; set; }

        public bool AreGroupPlacementsEnabled { get; set; }

        public bool? IsSequentialStatusMode { get; set; }

        public List<Guid> BadgeGuids { get; set; }

        public List<ConnectionStatusBag> ConnectionStatuses { get; set; }

        public List<ListItemBag> ConnectionOpportunities { get; set; }

        public List<ListItemBag> ConnectionStates { get; set; }

        public List<ListItemBag> RequestSourceItems { get; set; }

        public List<ListItemBag> WorkflowItems { get; set; }

        public List<ListItemBag> AllPossibleConnectors { get; set; }

        public List<ListItemBag> GridDataToShowItems { get; set; }

        public List<ConnectionActivityBag> ConnectionActivities { get; set; }

        public ConnectionOpportunityDetailBag ConnectionOpportunityDetailsFromFilter { get; set; }

        /// <summary>
        /// Gets or sets the attributes for Connection Request attributes specified at the Connection Type level.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public Dictionary<string, PublicAttributeBag> ConnectionTypeRequestAttributes { get; set; }
    }
}
