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
    public class ActivityEntryBag
    {
        public string Key { get; set; }

        public ActivityEntryType EntryType { get; set; }

        public DateTimeOffset? EntryDateTime { get; set; }

        public string CreatedBy { get; set; }

        public CardEntryBag CardEntry { get; set; }

        public SystemUpdateBag SystemUpdate { get; set; }
    }

    public class CardEntryBag
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public string PhotoUrl { get; set; }

        public List<ListItemBag> Attachments { get; set; }

        /// <summary>
        /// The GUID of the activity type. Populated for Activity entries to support editing.
        /// </summary>
        public string ActivityTypeGuid { get; set; }

        /// <summary>
        /// The Name of the activity type. Populated for Activity entries to support editing.
        /// </summary>
        public string ActivityTypeName { get; set; }

        /// <summary>
        /// Gets or sets whether or not the activity type for this entry is a system activity type. Populated for Activity entries to support editing.
        /// </summary>
        public bool? IsSystemActivityType { get; set; }

        /// <summary>
        /// The GUID of the connector's person alias. Populated for Activity entries to support editing.
        /// </summary>
        public string ConnectorPersonAliasGuid { get; set; }
    }

    public class SystemUpdateBag
    {
        public SystemUpdateType SystemUpdateType { get; set; }

        public string PreviousValue { get; set; }

        public string NewValue { get; set; }
    }
}