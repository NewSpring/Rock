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
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Field.Types
{
    /// <summary>
    /// Field Type to select 0 or more Checkin Configuration Areas/Types. (Weekly Service Check-in, Volunteer Check-in, etc)
    /// Stored as a list of GroupType Guids
    /// </summary>
    [FieldTypeUsage( FieldTypeUsage.System )]
    [RockPlatformSupport( Utility.RockPlatform.WebForms, Utility.RockPlatform.Obsidian )]
    [Rock.SystemGuid.FieldTypeGuid( Rock.SystemGuid.FieldType.CHECK_IN_CONFIGURATION_TYPE )]
    public class CheckinConfigurationTypeFieldType : SelectFromListFieldType, IEntityReferenceFieldType
    {
        private const string VALUES_PUBLIC_KEY = "values";

        #region Configuration

        /// <inheritdoc/>
        public override Dictionary<string, string> GetPublicConfigurationValues( Dictionary<string, string> privateConfigurationValues, ConfigurationValueUsage usage, string privateValue )
        {
            var publicConfigurationValues = base.GetPublicConfigurationValues( privateConfigurationValues, usage, privateValue );
           
            using ( var rockContext = new RockContext() )
            {
                GroupTypeService groupTypeService = new GroupTypeService( rockContext );
                int? groupTypePurposeCheckinTemplateValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid() );
                publicConfigurationValues[VALUES_PUBLIC_KEY] = new GroupTypeService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( a => a.GroupTypePurposeValueId.HasValue && a.GroupTypePurposeValueId.Value == groupTypePurposeCheckinTemplateValueId )
                    .OrderBy( o => o.Name )
                    .Select( o => new ListItemBag
                    {
                        Value = o.Guid.ToString(),
                        Text = o.Name
                    } )
                    .ToCamelCaseJson( false, true );
            }
            return publicConfigurationValues;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the list source.
        /// </summary>
        /// <value>
        /// The list source.
        /// </value>
        internal override Dictionary<string, string> GetListSource( Dictionary<string, ConfigurationValue> configurationValues )
        {
            GroupTypeService groupTypeService = new GroupTypeService( new RockContext() );
            int? groupTypePurposeCheckinTemplateValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid() );
            return groupTypeService.Queryable()
                .Where( a => a.GroupTypePurposeValueId.HasValue && a.GroupTypePurposeValueId.Value == groupTypePurposeCheckinTemplateValueId )
                .OrderBy( a => a.Order )
                .ThenBy( a => a.Name )
                .ToDictionary( k => k.Guid.ToString(), v => v.Name );
        }

        #endregion

        #region IEntityReferenceFieldType

        /// <inheritdoc/>
        List<ReferencedEntity> IEntityReferenceFieldType.GetReferencedEntities( string privateValue, Dictionary<string, string> privateConfigurationValues )
        {
            if ( privateValue.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var valueGuidList = privateValue.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).AsGuidList();

            var ids = valueGuidList
                .Select( guid => GroupTypeCache.GetId( guid ) )
                .Where( id => id.HasValue )
                .ToList();

            var groupTypeEntityTypeId = EntityTypeCache.GetId<GroupType>().Value;

            return ids
                .Select( id => new ReferencedEntity( groupTypeEntityTypeId, id.Value ) )
                .ToList();
        }

        /// <inheritdoc/>
        List<ReferencedProperty> IEntityReferenceFieldType.GetReferencedProperties( Dictionary<string, string> privateConfigurationValues )
        {
            // This field type references the Name property of a GroupType and
            // should have its persisted values updated when changed.
            return new List<ReferencedProperty>
            {
                new ReferencedProperty( EntityTypeCache.GetId<GroupType>().Value, nameof( GroupType.Name ) )
            };
        }

        #endregion
    }
}