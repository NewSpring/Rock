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
    public class AdditionalRequestBag
    {
        public string RequestIdKey { get; set; }

        public string ConnectionOpportunityIdKey { get; set; }

        public string ConnectionOpportunityName { get; set; }

        public string ConnectionStatus { get; set; }

        public string Connector { get; set; }

        public DateTimeOffset? RequestCreatedDateTime { get; set; }

        public string Requester { get; set; }
    }
}
