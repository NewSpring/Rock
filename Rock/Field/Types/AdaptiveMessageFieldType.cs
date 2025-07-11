﻿// <copyright>
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

using Rock.Attribute;
using Rock.SystemGuid;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Field.Types
{
    /// <summary>
    /// Adaptive Message Field Type. 
    /// </summary>
    [FieldTypeUsage( FieldTypeUsage.Administrative )]
    [FieldTypeGuid( SystemGuid.FieldType.ADAPTIVE_MESSAGE )]
    internal class AdaptiveMessageFieldType : UniversalItemTreePickerFieldType
    {
        /// <inheritdoc/>
        protected override string GetRootRestUrl( Dictionary<string, string> privateConfigurationValues )
        {
            return "/api/v2/controls/AdaptiveMessagePickerGetAdaptiveMessages";
        }

        /// <inheritdoc/>
        protected override List<ListItemBag> GetItemBags( IEnumerable<string> values, Dictionary<string, string> privateConfigurationValues )
        {
            var guids = values.Select( v => v.AsGuid() ).ToList();

            var messages = AdaptiveMessageCache.GetMany( guids );
            var msgBags = messages
                .Select( m => new ListItemBag
                {
                    Value = m.Guid.ToString(),
                    Text = m.Name
                } ).ToList();

            return msgBags;
        }

        /// <inheritdoc/>
        protected override List<string> GetSelectableItemTypes( Dictionary<string, string> privateConfigurationValues )
        {
            return new List<string> { "Item" };
        }
    }
}
