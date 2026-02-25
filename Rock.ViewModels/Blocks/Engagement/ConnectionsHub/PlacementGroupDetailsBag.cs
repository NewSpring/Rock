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

using Rock.Model;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class PlacementGroupDetailsBag
    {
        public List<ListItemBag> GroupMemberRoles { get; set; }

        public Dictionary<string, List<ListItemBag>> GroupMemberStatuses { get; set; }

        public string Name { get; set; }

        public string IconCssClass { get; set; }

        /// <summary>
        /// True if the requester already exists as a GroupMember in the placement group
        /// </summary>
        public bool? IsPendingGroupMember { get; set; }

        /// <summary>
        /// The Group Member IdKey of the placed group member.
        /// </summary>
        public string GroupMemberIdKey { get; set; }

        public List<GroupMemberRequirementBag> GroupMemberRequirements { get; set; }

        /// <summary>
        /// Gets or sets the group member attributes for the selected placement group.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public Dictionary<string, PublicAttributeBag> GroupMemberAttributes { get; set; }

        /// <summary>
        /// The attribute values for the placement group.
        /// </summary>
        public Dictionary<string, string> GroupMemberAttributeValues { get; set; }
    }

    public class GroupMemberRequirementBag
    {
        public string GroupRequirementTypeIdKey { get; set; }

        public string RequirementName { get; set; }

        public MeetsGroupRequirement GroupMemberRequirementState { get; set; }

        public bool? IsManualRequirement { get; set; }
    }
}
