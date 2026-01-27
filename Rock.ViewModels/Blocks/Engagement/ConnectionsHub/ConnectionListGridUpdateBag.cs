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

using Rock.Enums.Connection;
using Rock.ViewModels.Core.Grid;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionListGridUpdateBag
    {
        public string IdKey { get; set; }

        public GroupingFieldBag StatusGrouping { get; set; }

        public GroupingFieldBag StateGrouping { get; set; }

        public GroupingFieldBag ConnectorGrouping { get; set; }

        public GroupingFieldBag OpportunityGrouping { get; set; }

        public GroupingFieldBag CampusGrouping { get; set; }

        public ConnectionStatusBag ConnectionStatusBag { get; set; }

        public ConnectionState ConnectionState { get; set; }

        // I could use the Person Field Bag from the Grouping Field Bag, but having it here for now.
        public PersonFieldBag PersonFieldBag { get; set; }
    }
}

