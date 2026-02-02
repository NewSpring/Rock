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
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionRequestBag
    {
        public string ConnectionOpportunityGuid { get; set; }

        public ListItemBag Requester { get; set; }

        public string ConnectorPersonAliasGuid { get; set; }

        public ConnectionState? ConnectionState { get; set; }

        public DateTimeOffset? FollowUpDate { get; set; }

        public string ConnectionStatusGuid { get; set; }

        public string PlacementGroupGuid { get; set; }

        public string GroupMemberRoleGuid { get; set; }

        public GroupMemberStatus? GroupMemberStatus { get; set; }

        public string Comments { get; set; }

        public string RequestSourceGuid { get; set; }

        /// <summary>
        /// Gets or sets the attribute values for the Connection Request.
        /// </summary>
        /// <value>
        /// The attribute values.
        /// </value>
        public Dictionary<string, string> ConnectionRequestAttributeValues { get; set; }

        /// <summary>
        /// Gets or sets the attribute values for the placement group member.
        /// </summary>
        /// <value>
        /// The attribute values.
        /// </value>
        public Dictionary<string, string> PlacementGroupMemberAttributeValues { get; set; }
    }
}
