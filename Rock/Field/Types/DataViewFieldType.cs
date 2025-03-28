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
using System.Linq;
#if WEBFORMS
using System.Web.UI.WebControls;
using System.Web.UI;
#endif

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.ViewModels.Utility;

namespace Rock.Field.Types
{
    /// <summary>
    /// Data View Field Type.  Stored as DataViews's Guid
    /// </summary>
    [FieldTypeUsage( FieldTypeUsage.System )]
    [RockPlatformSupport( Utility.RockPlatform.WebForms, Utility.RockPlatform.Obsidian )]
    [Rock.SystemGuid.FieldTypeGuid( Rock.SystemGuid.FieldType.DATA_VIEW )]
    public class DataViewFieldType : FieldType, IEntityFieldType, IEntityReferenceFieldType
    {
        #region Configuration

        /// <summary>
        /// Entity Type Name Key
        /// </summary>
        protected const string ENTITY_TYPE_NAME_KEY = "entityTypeName";

        #endregion

        #region Edit Control

        /// <inheritdoc/>
        public override string GetPublicValue( string privateValue, Dictionary<string, string> privateConfigurationValues )
        {
            return GetTextValue( privateValue, privateConfigurationValues );
        }

        /// <inheritdoc/>
        public override string GetPrivateEditValue( string publicValue, Dictionary<string, string> privateConfigurationValues )
        {
            var dataViewValue = publicValue.FromJsonOrNull<ListItemBag>();

            if ( dataViewValue != null )
            {
                return dataViewValue.Value;
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public override string GetPublicEditValue( string privateValue, Dictionary<string, string> privateConfigurationValues )
        {
            if ( Guid.TryParse( privateValue, out Guid guid ) )
            {
                var dataView = DataViewCache.Get( guid );
                
                if ( dataView != null )
                {
                    return dataView.ToListItemBag().ToCamelCaseJson( false, true );
                }
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetPrivateConfigurationValues( Dictionary<string, string> publicConfigurationValues )
        {
            var privateConfigurationValues = base.GetPrivateConfigurationValues( publicConfigurationValues );

            if ( publicConfigurationValues.ContainsKey( ENTITY_TYPE_NAME_KEY ) )
            {
                var entityTypeNameValue = privateConfigurationValues[ENTITY_TYPE_NAME_KEY].FromJsonOrNull<ListItemBag>();
                if ( entityTypeNameValue != null )
                {
                    var entityType = EntityTypeCache.Get( entityTypeNameValue.Value.AsGuid() );

                    if ( entityType != null )
                    {
                        privateConfigurationValues[ENTITY_TYPE_NAME_KEY] = entityType.Name;
                    }
                }
            }

            return privateConfigurationValues;
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetPublicConfigurationValues( Dictionary<string, string> privateConfigurationValues, ConfigurationValueUsage usage, string value )
        {
            var publicConfigurationValues = base.GetPublicConfigurationValues( privateConfigurationValues, usage, value );

            if ( usage != ConfigurationValueUsage.View && publicConfigurationValues.ContainsKey( ENTITY_TYPE_NAME_KEY ) )
            {
                var entityTypeName = publicConfigurationValues[ENTITY_TYPE_NAME_KEY];
                var entityType = EntityTypeCache.Get( entityTypeName );
                if ( entityType != null )
                {
                    publicConfigurationValues[ENTITY_TYPE_NAME_KEY] = new ListItemBag()
                    {
                        Text = entityType.FriendlyName,
                        Value = entityType.Guid.ToString()
                    }.ToCamelCaseJson( false, true );
                }
            }

            return publicConfigurationValues;
        }

        #endregion

        #region Formatting

        /// <inheritdoc />
        public override string GetTextValue( string privateValue, Dictionary<string, string> privateConfigurationValues )
        {
            Guid? guid = privateValue.AsGuidOrNull();
            if ( guid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var service = new DataViewService( rockContext );
                    var dataview = service.GetNoTracking( guid.Value );

                    if ( dataview != null )
                    {
                        return dataview.Name;
                    }
                }
            }

            return string.Empty;
        }

        #endregion

        #region Edit Control 

        #endregion

        #region Entity Methods

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public IEntity GetEntity( string value )
        {
            return GetEntity( value, null );
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        public IEntity GetEntity( string value, RockContext rockContext )
        {
            Guid? guid = value.AsGuidOrNull();
            if ( guid.HasValue )
            {
                rockContext = rockContext ?? new RockContext();
                return new DataViewService( rockContext ).Get( guid.Value );
            }

            return null;
        }

        #endregion

        #region IEntityReferenceFieldType

        /// <inheritdoc/>
        List<ReferencedEntity> IEntityReferenceFieldType.GetReferencedEntities( string privateValue, Dictionary<string, string> privateConfigurationValues )
        {
            Guid? guid = privateValue.AsGuidOrNull();

            if ( !guid.HasValue )
            {
                return null;
            }

            using ( var rockContext = new RockContext() )
            {
                var dataViewId = new DataViewService( rockContext ).GetId( guid.Value );

                if ( !dataViewId.HasValue )
                {
                    return null;
                }

                return new List<ReferencedEntity>()
                {
                    new ReferencedEntity( EntityTypeCache.GetId<DataView>().Value, dataViewId.Value )
                };
            }
        }

        /// <inheritdoc/>
        List<ReferencedProperty> IEntityReferenceFieldType.GetReferencedProperties( Dictionary<string, string> privateConfigurationValues )
        {
            return new List<ReferencedProperty>
            {
                new ReferencedProperty( EntityTypeCache.GetId<DataView>().Value, nameof( DataView.Name ) )
            };
        }

        #endregion

        #region WebForms
#if WEBFORMS

        /// <summary>
        /// Returns a list of the configuration keys
        /// </summary>
        /// <returns></returns>
        public override List<string> ConfigurationKeys()
        {
            var configKeys = base.ConfigurationKeys();
            configKeys.Add( ENTITY_TYPE_NAME_KEY );
            return configKeys;
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            List<Control> controls = new List<Control>();

            var etp = new EntityTypePicker();
            controls.Add( etp );
            etp.EntityTypes = new EntityTypeService( new RockContext() )
                .GetEntities()
                .OrderBy( t => t.FriendlyName )
                .ToList();
            etp.AutoPostBack = true;
            etp.SelectedIndexChanged += OnQualifierUpdated;
            etp.Label = "Entity Type";
            etp.Help = "The type of entity to display dataviews for.";

            return controls;
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override Dictionary<string, ConfigurationValue> ConfigurationValues( List<Control> controls )
        {
            Dictionary<string, ConfigurationValue> configurationValues = new Dictionary<string, ConfigurationValue>();
            configurationValues.Add( ENTITY_TYPE_NAME_KEY, new ConfigurationValue( "Entity Type", "The type of entity to display dataviews for", "" ) );

            if ( controls != null && controls.Count == 1 )
            {
                if ( controls[0] != null && controls[0] is EntityTypePicker )
                {
                    int? entityTypeId = ( ( EntityTypePicker ) controls[0] ).SelectedValueAsInt();
                    if ( entityTypeId.HasValue )
                    {
                        var entityType = EntityTypeCache.Get( entityTypeId.Value );
                        configurationValues[ENTITY_TYPE_NAME_KEY].Value = entityType != null ? entityType.Name : string.Empty;
                    }
                }
            }

            return configurationValues;
        }

        /// <summary>
        /// Sets the configuration value.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="configurationValues"></param>
        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( controls != null && controls.Count == 1 && configurationValues != null )
            {
                if ( controls[0] != null && controls[0] is EntityTypePicker && configurationValues.ContainsKey( ENTITY_TYPE_NAME_KEY ) )
                {
                    var entityType = EntityTypeCache.Get( configurationValues[ENTITY_TYPE_NAME_KEY].Value );
                    ( ( EntityTypePicker ) controls[0] ).SetValue( entityType != null ? entityType.Id : ( int? ) null );
                }
            }
        }

        /// <summary>
        /// Returns the field's current value(s)
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return !condensed
                ? GetTextValue( value, configurationValues.ToDictionary( cv => cv.Key, cv => cv.Value.Value ) )
                : GetCondensedTextValue( value, configurationValues.ToDictionary( cv => cv.Key, cv => cv.Value.Value ) );
        }

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            string entityTypeName = string.Empty;
            int entityTypeId = 0;

            if ( configurationValues != null )
            {
                if ( configurationValues.ContainsKey( ENTITY_TYPE_NAME_KEY ) )
                {
                    entityTypeName = configurationValues[ENTITY_TYPE_NAME_KEY].Value;
                    if ( !string.IsNullOrWhiteSpace( entityTypeName ) && entityTypeName != None.IdValue )
                    {
                        var entityType = EntityTypeCache.Get( entityTypeName );
                        if ( entityType != null )
                        {
                            entityTypeId = entityType.Id;
                        }
                    }
                }
            }

            return new DataViewItemPicker { ID = id, EntityTypeId = entityTypeId };
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// returns DataView.Guid
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            var picker = control as DataViewItemPicker;

            if ( picker != null )
            {
                int? id = picker.SelectedValue.AsIntegerOrNull();
                if ( id.HasValue )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var dataview = new DataViewService( rockContext ).GetNoTracking( id.Value );

                        if ( dataview != null )
                        {
                            return dataview.Guid.ToString();
                        }
                    }
                }

                return string.Empty;
            }

            return null;
        }

        /// <summary>
        /// Sets the value.
        /// value is an Account.Guid
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            var picker = control as DataViewItemPicker;

            if ( picker != null )
            {
                Guid guid = value.AsGuid();

                // get the item (or null) and set it
                var dataview = new DataViewService( new RockContext() ).Get( guid );
                picker.SetValue( dataview );
            }
        }

        /// <summary>
        /// Gets the edit value as the IEntity.Id
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public int? GetEditValueAsEntityId( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            Guid guid = GetEditValue( control, configurationValues ).AsGuid();
            var item = new DataViewService( new RockContext() ).Get( guid );
            return item != null ? item.Id : ( int? ) null;
        }

        /// <summary>
        /// Sets the edit value from IEntity.Id value
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id">The identifier.</param>
        public void SetEditValueFromEntityId( Control control, Dictionary<string, ConfigurationValue> configurationValues, int? id )
        {
            var item = new DataViewService( new RockContext() ).Get( id ?? 0 );
            string guidValue = item != null ? item.Guid.ToString() : string.Empty;
            SetEditValue( control, configurationValues, guidValue );
        }

#endif
        #endregion
    }
}
