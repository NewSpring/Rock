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
using Rock.ViewModels.Core.Grid;
using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class BulkRequestViewBag
    {
        public string IdKey { get; set; }

        public PersonFieldBag Requester { get; set; }

        public string ConnectionOpportunity { get; set; }

        public string ConnectionTypeSource { get; set; }

        public ListItemBag Connector { get; set; }

        public string DueDate { get; set; }

        public ConnectionStatusBag ConnectionStatus { get; set; }

        public string CelebrationText { get; set; }
    }
}
