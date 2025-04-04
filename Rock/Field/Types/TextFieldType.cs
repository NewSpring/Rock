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
using System.Web.UI;
using System.Web.UI.WebControls;
#endif
using Rock.Attribute;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace Rock.Field.Types
{
    /// <summary>
    /// Field used to save and display a text value
    /// </summary>
    [Serializable]
    [IconSvg( @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 16 16""><path d=""M13.14,12.29h-.69L8.78,2.56A.85.85,0,0,0,8,2a.86.86,0,0,0-.81.56L3.55,12.29H2.86a.86.86,0,1,0,0,1.71H5.43a.86.86,0,1,0,0-1.71h0L5.84,11h4.27l.46,1.29h0a.86.86,0,1,0,0,1.71h2.57a.86.86,0,1,0,0-1.71Zm-6.63-3L8,5.3l1.5,4Z""/></svg>" )]
    [FieldTypeUsage( FieldTypeUsage.Common )]
    [RockPlatformSupport( Utility.RockPlatform.WebForms, Utility.RockPlatform.Obsidian )]
    [Rock.SystemGuid.FieldTypeGuid( Rock.SystemGuid.FieldType.TEXT )]
    public class TextFieldType : FieldType
    {
        #region Configuration

        private const string IS_PASSWORD_KEY = "ispassword";
        private const string MAX_CHARACTERS = "maxcharacters";
        private const string SHOW_COUNT_DOWN = "showcountdown";
        private const string IS_FIRST_NAME = "isfirstname";

        /// <summary>
        /// Determines whether the Attribute Configuration for this field has IsPassword = True
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public bool IsPassword( Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( configurationValues != null && configurationValues.ContainsKey( IS_PASSWORD_KEY ) )
            {
                return configurationValues[IS_PASSWORD_KEY].Value.AsBoolean();
            }

            return false;
        }

        #endregion

        #region Formatting

        /// <inheritdoc/>
        public override string GetTextValue( string value, Dictionary<string, string> configurationValues )
        {
            if ( configurationValues != null &&
                configurationValues.ContainsKey( IS_PASSWORD_KEY ) &&
                configurationValues[IS_PASSWORD_KEY].AsBoolean() )
            {
                return "********";
            }

            return value;
        }

        /// <inheritdoc/>
        public override string GetCondensedTextValue( string value, Dictionary<string, string> configurationValues )
        {
            if ( configurationValues != null &&
                configurationValues.ContainsKey( IS_PASSWORD_KEY ) &&
                configurationValues[IS_PASSWORD_KEY].AsBoolean() )
            {
                return "********";
            }

            return base.GetCondensedTextValue( value, configurationValues );
        }

        #endregion

        #region Edit Control

        #endregion

        #region FilterControl

        /// <summary>
        /// Determines whether [has filter control].
        /// </summary>
        /// <returns></returns>
        public override bool HasFilterControl()
        {
            return true;
        }

        /// <summary>
        /// Gets the type of the filter comparison.
        /// </summary>
        /// <value>
        /// The type of the filter comparison.
        /// </value>
        public override Model.ComparisonType FilterComparisonType
        {
            get
            {
                return ComparisonHelper.StringFilterComparisonTypes;
            }
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
            configKeys.Add( IS_PASSWORD_KEY );
            configKeys.Add( MAX_CHARACTERS );
            configKeys.Add( SHOW_COUNT_DOWN );
            configKeys.Add( IS_FIRST_NAME );
            return configKeys;
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            var controls = base.ConfigurationControls();

            // Add checkbox for deciding if the textbox is used for storing a password
            var cbIsPasswordField = new RockCheckBox();
            controls.Add( cbIsPasswordField );
            cbIsPasswordField.AutoPostBack = true;
            cbIsPasswordField.CheckedChanged += OnQualifierUpdated;
            cbIsPasswordField.Label = "Password Field";
            cbIsPasswordField.Help = "When set, edit field will be masked.";

            // Add number box for selecting the maximum number of characters
            var nbMaxCharacters = new NumberBox();
            controls.Add( nbMaxCharacters );
            nbMaxCharacters.AutoPostBack = true;
            nbMaxCharacters.TextChanged += OnQualifierUpdated;
            nbMaxCharacters.NumberType = ValidationDataType.Integer;
            nbMaxCharacters.Label = "Max Characters";
            nbMaxCharacters.Help = "The maximum number of characters to allow. Leave this field empty to allow for an unlimited amount of text.";

            // Add checkbox indicating whether to show the count down.
            var cbShowCountDown = new RockCheckBox();
            controls.Add( cbShowCountDown );
            cbShowCountDown.AutoPostBack = true;
            cbShowCountDown.CheckedChanged += OnQualifierUpdated;
            cbShowCountDown.Label = "Show Character Limit Countdown";
            cbShowCountDown.Help = "When set, displays a countdown showing how many characters remain (for the Max Characters setting).";

            // Add checkbox for deciding if the textbox is used for storing a first name.
            var cbIsFirstNameField= new RockCheckBox();
            controls.Add( cbIsFirstNameField );
            cbIsFirstNameField.AutoPostBack = true;
            cbIsFirstNameField.CheckedChanged += OnQualifierUpdated;
            cbIsFirstNameField.Label = "FirstName Field";
            cbIsFirstNameField.Help = "When set, edit field will be validated as a first name.";

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
            configurationValues.Add( IS_PASSWORD_KEY, new ConfigurationValue( "Password Field", "When set, edit field will be masked.", "" ) );
            configurationValues.Add( MAX_CHARACTERS, new ConfigurationValue( "Max Characters", "The maximum number of characters to allow. Leave this field empty to allow for an unlimited amount of text.", "" ) );
            configurationValues.Add( SHOW_COUNT_DOWN, new ConfigurationValue( "Show Character Limit Countdown", "When set, displays a countdown showing how many characters remain (for the Max Characters setting).", "" ) );
            configurationValues.Add( IS_FIRST_NAME, new ConfigurationValue( "FirstName Field", "When set, edit field will be validated as a first name.", "" ) );

            if ( controls != null )
            {
                if ( controls.Count > 0 )
                {
                    CheckBox cbIsPasswordField = controls[0] as CheckBox;
                    if ( cbIsPasswordField != null )
                    {
                        configurationValues[IS_PASSWORD_KEY].Value = cbIsPasswordField.Checked.ToString();
                    }
                }

                if ( controls.Count > 1 )
                {
                    NumberBox nbMaxCharacters = controls[1] as NumberBox;
                    if ( nbMaxCharacters != null )
                    {
                        configurationValues[MAX_CHARACTERS].Value = nbMaxCharacters.Text;
                    }
                }

                if ( controls.Count > 2 )
                {
                    CheckBox cbShowCountDown = controls[2] as CheckBox;
                    if ( cbShowCountDown != null )
                    {
                        configurationValues[SHOW_COUNT_DOWN].Value = cbShowCountDown.Checked.ToString();
                    }
                }

                if ( controls.Count > 3 )
                {
                    CheckBox cbIsFirstNameField = controls[3] as CheckBox;
                    if ( cbIsFirstNameField != null )
                    {
                        configurationValues[IS_FIRST_NAME].Value = cbIsFirstNameField.Checked.ToString();
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
            if ( controls != null && controls.Count > 0 && configurationValues != null )
            {
                if ( controls.Count > 0 && configurationValues.ContainsKey( IS_PASSWORD_KEY ) )
                {
                    CheckBox cbIsPasswordField = controls[0] as CheckBox;
                    if ( cbIsPasswordField != null )
                    {
                        cbIsPasswordField.Checked = configurationValues[IS_PASSWORD_KEY].Value.AsBoolean();
                    }
                }

                if ( controls.Count > 1 && configurationValues.ContainsKey( MAX_CHARACTERS ) )
                {
                    NumberBox nbMaxCharacters = controls[1] as NumberBox;
                    if ( nbMaxCharacters != null )
                    {
                        nbMaxCharacters.Text = configurationValues[MAX_CHARACTERS].Value;
                    }
                }

                if ( controls.Count > 2 && configurationValues.ContainsKey( SHOW_COUNT_DOWN ) )
                {
                    CheckBox cbShowCountDown = controls[2] as CheckBox;
                    if ( cbShowCountDown != null )
                    {
                        cbShowCountDown.Checked = configurationValues[SHOW_COUNT_DOWN].Value.AsBoolean();
                    }
                }

                if ( controls.Count > 3 && configurationValues.ContainsKey( IS_FIRST_NAME ) )
                {
                    CheckBox cbIsFirstNameField = controls[3] as CheckBox;
                    if ( cbIsFirstNameField != null )
                    {
                        cbIsFirstNameField.Checked = configurationValues[IS_FIRST_NAME].Value.AsBoolean();
                    }
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
        /// Returns the value that should be used for sorting, using the most appropriate datatype
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override object SortValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues )
        {
            // use un-condensed formatted value as the sort value
            return this.FormatValue( parentControl, value, configurationValues, false );
        }

        /// <summary>
        /// Formats the value as HTML.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <returns></returns>
        public override string FormatValueAsHtml( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed = false )
        {
            // NOTE: this really should not be encoding the value. FormatValueAsHtml method is really designed to wrap a value with appropriate html (i.e. convert an email into a mailto anchor tag)
            // but keeping it here for backward compatibility.
            return System.Web.HttpUtility.HtmlEncode( FormatValue( parentControl, value, configurationValues, condensed ) );
        }

        /// <summary>
        /// Formats the value as HTML.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="entityTypeId">The entity type identifier.</param>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <returns></returns>
        public override string FormatValueAsHtml( Control parentControl, int? entityTypeId, int? entityId, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed = false )
        {
            // NOTE: this really should not be encoding the value. FormatValueAsHtml method is really designed to wrap a value with appropriate html (i.e. convert an email into a mailto anchor tag)
            // but keeping it here for backward compatibility.
            return System.Web.HttpUtility.HtmlEncode( FormatValue( parentControl, entityTypeId, entityId, value, configurationValues, condensed ) );
        }

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            RockTextBox tb = base.EditControl( configurationValues, id ) as RockTextBox;

            if ( configurationValues != null )
            {
                if ( configurationValues.ContainsKey( IS_FIRST_NAME ) &&
                    configurationValues[IS_FIRST_NAME].Value.AsBoolean() )
                {
                    tb = new FirstNameTextBox()
                    {
                        ID = id,
                    };
                }

                if ( configurationValues.ContainsKey( IS_PASSWORD_KEY ) &&
                    configurationValues[IS_PASSWORD_KEY].Value.AsBoolean() )
                {
                    tb.TextMode = TextBoxMode.Password;
                }

                if ( configurationValues.ContainsKey( MAX_CHARACTERS ) )
                {
                    int? maximumLength = configurationValues[MAX_CHARACTERS].Value.AsIntegerOrNull();
                    if ( maximumLength.HasValue )
                    {
                        tb.MaxLength = maximumLength.Value;
                    }
                }

                if ( configurationValues.ContainsKey( SHOW_COUNT_DOWN ) )
                {
                    tb.ShowCountDown = configurationValues[SHOW_COUNT_DOWN].Value.AsBoolean();
                }
            }
            return tb;
        }

        /// <summary>
        /// Gets the filter compare control.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="filterMode">The filter mode.</param>
        /// <returns></returns>
        public override Control FilterCompareControl( Dictionary<string, ConfigurationValue> configurationValues, string id, bool required, FilterMode filterMode )
        {
            if ( filterMode == FilterMode.SimpleFilter )
            {
                // hide the compare control for SimpleFilter mode
                RockDropDownList ddlCompare = ComparisonHelper.ComparisonControl( FilterComparisonType, required );
                ddlCompare.ID = string.Format( "{0}_ddlCompare", id );
                ddlCompare.AddCssClass( "js-filter-compare" );
                ddlCompare.Visible = false;
                return ddlCompare;
            }
            else
            {
                return base.FilterCompareControl( configurationValues, id, required, filterMode );
            }
        }

        /// <summary>
        /// Gets the filter values.
        /// </summary>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="filterMode">The filter mode.</param>
        /// <returns></returns>
        public override List<string> GetFilterValues( Control filterControl, Dictionary<string, ConfigurationValue> configurationValues, FilterMode filterMode )
        {
            // If this is a simple filter, only return values if something was actually entered into the filter's text field
            var values = base.GetFilterValues( filterControl, configurationValues, filterMode );
            if ( filterMode == FilterMode.SimpleFilter &&
                values.Count == 2 &&
                values[0].ConvertToEnum<ComparisonType>() == ComparisonType.Contains &&
                values[1] == "" )
            {
                return new List<string>();
            }

            return values;
        }

        /// <summary>
        /// Gets the filter compare value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="filterMode">The filter mode.</param>
        /// <returns></returns>
        public override string GetFilterCompareValue( Control control, FilterMode filterMode )
        {
            bool filterValueControlVisible = true;
            var filterField = control.FirstParentControlOfType<FilterField>();
            if ( filterField != null && filterField.HideFilterCriteria )
            {
                filterValueControlVisible = false;
            }

            if ( filterMode == FilterMode.SimpleFilter && filterValueControlVisible )
            {
                // hard code to Contains when in SimpleFilter mode and the FilterValue control is visible
                return ComparisonType.Contains.ConvertToInt().ToString();
            }
            else
            {
                return base.GetFilterCompareValue( control, filterMode );
            }
        }

#endif
        #endregion
    }
}