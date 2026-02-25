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
    public class ConnectionRequestDetailsBag
    {
        public string ConnectionRequestIdKey { get; set; }

        public RequesterPersonBag RequesterPerson { get; set; }

        public List<AdditionalRequestBag> AdditionalRequests { get; set; }

        public ConnectionState ConnectionState { get; set; }

        public DateTimeOffset? FollowUpDate { get; set; }

        public string ConnectionOpportunityName { get; set; }

        public string ConnectionOpportunityIcon { get; set; }

        /// <summary>
        /// The selected Campus on the Connection Request
        /// </summary>
        public string Campus { get; set; }

        public string ConnectorPerson { get; set; }

        public List<PersonFieldBag> ConnectorItems { get; set; }

        public DateTimeOffset? CreatedDateTime { get; set; }

        public DateTimeOffset? DueDate { get; set; }

        public DueStatus DueStatus { get; set; }

        public List<ListItemBag> ActionItems { get; set; }

        public string Comments { get; set; }

        public string ConnectionTypeSource { get; set; }

        public PlacementGroupDetailsBag PlacementGroup { get; set; }

        public string LavaHeadingTemplate { get; set; }

        public string LavaBadgeBar { get; set; }

        public List<ActivityEntryBag> ActivityEntries { get; set; }

        /// <summary>
        /// The attributes for the selected Connection Request.
        /// </summary>
        public Dictionary<string, PublicAttributeBag> Attributes { get; set; }

        /// <summary>
        /// The attribute values for the selected Connection Request.
        /// </summary>
        public Dictionary<string, string> AttributeValues { get; set; }


        // We can get this from the parent but this panel needs to function outside of the parent scope.

        public bool IsFutureFollowUpEnabled { get; set; }

        public bool IsRequestSecurityEnabled { get; set; }

        public bool AreRemindersEnabled { get; set; }

        public bool AreCelebrationsEnabled { get; set; }

        public bool AreGroupPlacementsEnabled { get; set; }

        public bool? IsSequentialStatusMode { get; set; }
    }
}
