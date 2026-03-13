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

using Rock.Enums.Connection;
using Rock.Model;
using Rock.ViewModels.Controls;
using Rock.ViewModels.Core.Grid;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionRequestDetailOptionsBag
    {
        public string ConnectionTypeIdKey { get; set; }

        public bool RequiresPlacementGroupToComplete { get; set; }

        public List<ConnectorItemBag> ConnectorItems { get; set; }

        public List<ConnectionStatusBag> ConnectionStatuses { get; set; }

        public List<ListItemBag> ConnectionStates { get; set; }

        public List<PlacementGroupDetailsBag> PlacementGroups { get; set; }

        public List<ConnectionActivityTypeBag> ConnectionActivities { get; set; }

        public List<ListItemBag> RequestSourceItems { get; set; }

        public string LavaHeadingTemplate { get; set; }

        public string LavaBadgeBar { get; set; }

        public bool IsFutureFollowUpEnabled { get; set; }

        public bool IsRequestSecurityEnabled { get; set; }

        public bool AreRemindersEnabled { get; set; }

        public bool AreCelebrationsEnabled { get; set; }

        public bool AreGroupPlacementsEnabled { get; set; }

        public bool IsSequentialStatusMode { get; set; }

        public List<Guid> BadgeGuids { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an active AI provider is configured.
        /// </summary>
        public bool IsAISummaryVisible { get; set; }
    }
}
