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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Event
{
    /// <summary>
    /// A Block that displays the list of Registrants related to a Registration Instance.
    /// </summary>
    [DisplayName( "Registration Instance - Registrant List" )]
    [Category( "Event" )]
    [Description( "Displays the list of Registrants related to a Registration Instance." )]

    #region Block Attributes

    [LinkedPage(
        "Registration Page",
        Description = "The page for editing registration and registrant information",
        Key = AttributeKey.RegistrationPage,
        DefaultValue = Rock.SystemGuid.Page.REGISTRATION_DETAIL,
        IsRequired = false,
        Order = 1 )]

    [LinkedPage(
        "Group Placement Page",
        Description = "The page for managing the registrant's group placements",
        Key = AttributeKey.GroupPlacementPage,
        DefaultValue = Rock.SystemGuid.Page.REGISTRATION_INSTANCE_PLACEMENT_GROUPS,
        IsRequired = false,
        Order = 2 )]

    [LinkedPage(
        "Group Detail Page",
        Description = "The page for viewing details about a group",
        Key = AttributeKey.GroupDetailPage,
        IsRequired = true,
        DefaultValue = Rock.SystemGuid.Page.GROUP_VIEWER,
        Order = 3 )]

    #endregion

    [Rock.SystemGuid.BlockTypeGuid( "4D4FBC7B-068C-499A-8BA4-C9209CA9BB6E" )]
    public partial class RegistrationInstanceRegistrantList : RegistrationInstanceBlock, ISecondaryBlock, ICustomGridOptions
    {
        #region Properties

        private const string SIGNATURE_LINK_TEMPLATE = @"<a href='{0}' target='_blank' rel='noopener noreferrer' style='color: black;'><i class='fa fa-file-signature'></i></a>";
        private const string SIGNATURE_NOT_SIGNED_INDICATOR = @"<i class='fa fa-edit text-danger' data-toggle='tooltip' data-original-title='{0}'></i>";

        #endregion

        #region Attribute Keys

        /// <summary>
        /// Keys for block attributes
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The linked page used to display registration details.
            /// </summary>
            public const string RegistrationPage = "RegistrationPage";

            /// <summary>
            /// The group placement page
            /// </summary>
            public const string GroupPlacementPage = "GroupPlacementPage";

            public const string GroupDetailPage = "GroupDetailPage";
        }

        #endregion Attribute Keys

        #region Page Parameter Keys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        private static class PageParameterKey
        {
            /// <summary>
            /// The Registration Instance identifier
            /// </summary>
            public const string RegistrationInstanceId = "RegistrationInstanceId";
        }

        #endregion Page Parameter Keys

        #region Properties and Fields

        private Dictionary<int, Location> _homeAddresses = new Dictionary<int, Location>();
        private Dictionary<int, PhoneNumber> _mobilePhoneNumbers = new Dictionary<int, PhoneNumber>();
        private Dictionary<int, PhoneNumber> _homePhoneNumbers = new Dictionary<int, PhoneNumber>();
        private Dictionary<int, PhoneNumber> _workPhoneNumbers = new Dictionary<int, PhoneNumber>();
        private Dictionary<int, SignatureDocument> _signatureDocuments = new Dictionary<int, SignatureDocument>();
        private List<RegistrationTemplatePlacement> _registrationTemplatePlacements = null;
        private List<PlacementGroupInfo> _placementGroupInfoList = null;
        private RockLiteralField _placementsField = null;

        private bool _isExporting = false;

        /// <summary>
        /// Gets or sets the registrant form fields that were configured as 'Show on Grid' for the registration template
        /// </summary>
        /// <value>
        /// The registrant fields.
        /// </value>
        public RegistrantFormField[] RegistrantFields { get; set; }

        /// <summary>
        /// Gets or sets the person campus ids.
        /// </summary>
        /// <value>
        /// The person campus ids.
        /// </value>
        private Dictionary<int, List<int>> PersonCampusIds { get; set; }

        /// <summary>
        /// Gets or sets the signed person ids.
        /// </summary>
        /// <value>
        /// The signed person ids.
        /// </value>
        private List<int> SignersPersonAliasIds { get; set; }

        /// <summary>
        /// Gets or sets the group links.
        /// </summary>
        /// <value>
        /// The group links.
        /// </value>
        private Dictionary<int, string> GroupLinks { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            RegistrantFields = ViewState[ViewStateKeyBase.RegistrantFields] as RegistrantFormField[];

            SetUserPreferencePrefix( this.RegistrationTemplateId.GetValueOrDefault() );

            // Don't set the dynamic control values if this is a postback from a grid 'ClearFilter'.
            bool setValues = this.Request.Params["__EVENTTARGET"] == null || !this.Request.Params["__EVENTTARGET"].EndsWith( "_lbClearFilter" );

            AddDynamicControls( setValues );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            ddlRegistrantsInGroup.Items.Clear();
            ddlRegistrantsInGroup.Items.Add( new ListItem() );
            ddlRegistrantsInGroup.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlRegistrantsInGroup.Items.Add( new ListItem( "No", "No" ) );

            ddlRegistrantsSignedDocument.Items.Clear();
            ddlRegistrantsSignedDocument.Items.Add( new ListItem() );
            ddlRegistrantsSignedDocument.Items.Add( new ListItem( "Yes", "Yes" ) );
            ddlRegistrantsSignedDocument.Items.Add( new ListItem( "No", "No" ) );

            fRegistrants.ApplyFilterClick += fRegistrants_ApplyFilterClick;

            gRegistrants.EmptyDataText = "No Registrants Found";
            gRegistrants.DataKeyNames = new string[] { "Id" };
            gRegistrants.PersonIdField = "PersonAlias.PersonId";
            gRegistrants.Actions.ShowAdd = true;
            gRegistrants.Actions.AddClick += gRegistrants_AddClick;
            gRegistrants.RowDataBound += gRegistrants_RowDataBound;
            gRegistrants.GridRebind += gRegistrants_GridRebind;

            // Add a custom button with an EventHandler that is only in this block.
            var customActionConfigEventButton = new CustomActionConfigEvent
            {
                IconCssClass = "fa fa-user-friends",
                HelpText = "Communicate to Registrars",
                EventHandler = LbRegistrarCommunication_Click
            };

            gRegistrants.Actions.AddCustomActionBlockButton( customActionConfigEventButton );

            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                ShowDetail();
            }

            base.OnLoad( e );
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState[ViewStateKeyBase.RegistrantFields] = RegistrantFields;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the fRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrants_ApplyFilterClick( object sender, EventArgs e )
        {
            fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_RegistrantsDateRange, "Registrants Date Range", sdrpRegistrantsRegistrantDateRange.DelimitedValues );
            fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_FirstName, tbRegistrantsRegistrantFirstName.Text );
            fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_LastName, tbRegistrantsRegistrantLastName.Text );
            fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_InGroup, ddlRegistrantsInGroup.SelectedValue );
            fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_SignedDocument, ddlRegistrantsSignedDocument.SelectedValue );

            if ( RegistrantFields != null )
            {
                foreach ( var field in RegistrantFields )
                {
                    if ( field.FieldSource == RegistrationFieldSource.PersonField && field.PersonFieldType.HasValue )
                    {
                        switch ( field.PersonFieldType.Value )
                        {
                            case RegistrationPersonFieldType.Campus:
                                var ddlCampus = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CAMPUS_ID ) as RockDropDownList;
                                if ( ddlCampus != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_HomeCampus, ddlCampus.SelectedValue );
                                }

                                break;

                            case RegistrationPersonFieldType.Email:
                                var tbEmailFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_EMAIL_ID ) as RockTextBox;
                                if ( tbEmailFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_Email, tbEmailFilter.Text );
                                }

                                break;

                            case RegistrationPersonFieldType.Birthdate:
                                var drpBirthdateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_BIRTHDATE_ID ) as DateRangePicker;
                                if ( drpBirthdateFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_BirthdateRange, drpBirthdateFilter.DelimitedValues );
                                }

                                break;
                            case RegistrationPersonFieldType.MiddleName:
                                var tbMiddleNameFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MIDDLE_NAME_ID ) as RockTextBox;
                                if ( tbMiddleNameFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_MiddleName, tbMiddleNameFilter.Text );
                                }

                                break;
                            case RegistrationPersonFieldType.AnniversaryDate:
                                var drAnniversaryDateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ANNIVERSARY_DATE_ID ) as DateRangePicker;
                                if ( drAnniversaryDateFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_AnniversaryDateRange, drAnniversaryDateFilter.DelimitedValues );
                                }

                                break;
                            case RegistrationPersonFieldType.Grade:
                                var gpGradeFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GRADE_ID ) as GradePicker;
                                if ( gpGradeFilter != null )
                                {
                                    int? gradeOffset = gpGradeFilter.SelectedValueAsInt( false );
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_Grade, gradeOffset.HasValue ? gradeOffset.Value.ToString() : string.Empty );
                                }

                                break;

                            case RegistrationPersonFieldType.Gender:
                                var ddlGenderFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GENDER_ID ) as RockDropDownList;
                                if ( ddlGenderFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_Gender, ddlGenderFilter.SelectedValue );
                                }

                                break;

                            case RegistrationPersonFieldType.MaritalStatus:
                                var dvpMaritalStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MARITAL_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpMaritalStatusFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_MaritalStatus, dvpMaritalStatusFilter.SelectedValue );
                                }

                                break;

                            case RegistrationPersonFieldType.ConnectionStatus:
                                var dvpConnectionStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CONNECTION_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpConnectionStatusFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_ConnectionStatus, dvpConnectionStatusFilter.SelectedValue );
                                }

                                break;

                            case RegistrationPersonFieldType.MobilePhone:
                                var tbMobilePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MOBILE_PHONE_ID ) as RockTextBox;
                                if ( tbMobilePhoneFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_CellPhone, "Cell Phone", tbMobilePhoneFilter.Text );
                                }

                                break;

                            case RegistrationPersonFieldType.HomePhone:
                                var tbHomePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_HOME_PHONE_ID ) as RockTextBox;
                                if ( tbHomePhoneFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_HomePhone, tbHomePhoneFilter.Text );
                                }

                                break;

                            case RegistrationPersonFieldType.Race:
                                var rpRaceFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_RACE_ID ) as RacePicker;
                                if ( rpRaceFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_Race, rpRaceFilter.SelectedValue );
                                }

                                break;

                            case RegistrationPersonFieldType.Ethnicity:
                                var epEthnicityFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ETHNICITY_ID ) as EthnicityPicker;
                                if ( epEthnicityFilter != null )
                                {
                                    fRegistrants.SetFilterPreference( UserPreferenceKeyBase.GridFilter_Ethnicity, epEthnicityFilter.SelectedValue );
                                }

                                break;
                        }
                    }

                    if ( field.Attribute != null )
                    {
                        var attribute = field.Attribute;
                        var filterControl = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ATTRIBUTE_PREFIX + attribute.Id.ToString() );
                        if ( filterControl != null )
                        {
                            try
                            {
                                var values = attribute.FieldType.Field.GetFilterValues( filterControl, field.Attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                fRegistrants.SetFilterPreference( attribute.Key, attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }
                }
            }

            BindRegistrantsGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the fRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fRegistrants_ClearFilterClick( object sender, EventArgs e )
        {
            fRegistrants.DeleteFilterPreferences();

            foreach ( var control in phRegistrantsRegistrantFormFieldFilters.ControlsOfTypeRecursive<Control>().Where( a => a.ID != null && a.ID.StartsWith( "filter" ) && a.ID.Contains( "_" ) ) )
            {
                var attributeId = control.ID.Split( '_' )[1].AsInteger();
                var attribute = AttributeCache.Get( attributeId );
                if ( attribute != null )
                {
                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, new List<string>() );
                }
            }

            if ( RegistrantFields != null )
            {
                foreach ( var field in RegistrantFields )
                {
                    if ( field.FieldSource == RegistrationFieldSource.PersonField && field.PersonFieldType.HasValue )
                    {
                        switch ( field.PersonFieldType.Value )
                        {
                            case RegistrationPersonFieldType.Campus:
                                var ddlCampus = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CAMPUS_ID ) as RockDropDownList;
                                if ( ddlCampus != null )
                                {
                                    ddlCampus.SetValue( ( Guid? ) null );
                                }

                                break;

                            case RegistrationPersonFieldType.Email:
                                var tbEmailFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_EMAIL_ID ) as RockTextBox;
                                if ( tbEmailFilter != null )
                                {
                                    tbEmailFilter.Text = string.Empty;
                                }

                                break;

                            case RegistrationPersonFieldType.Birthdate:
                                var drpBirthdateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_BIRTHDATE_ID ) as DateRangePicker;
                                if ( drpBirthdateFilter != null )
                                {
                                    drpBirthdateFilter.LowerValue = null;
                                    drpBirthdateFilter.UpperValue = null;
                                }

                                break;
                            case RegistrationPersonFieldType.MiddleName:
                                var tbMiddleNameFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MIDDLE_NAME_ID ) as RockTextBox;
                                if ( tbMiddleNameFilter != null )
                                {
                                    tbMiddleNameFilter.Text = string.Empty;
                                }

                                break;
                            case RegistrationPersonFieldType.AnniversaryDate:
                                var drAnniversaryDateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ANNIVERSARY_DATE_ID ) as DateRangePicker;
                                if ( drAnniversaryDateFilter != null )
                                {
                                    drAnniversaryDateFilter.LowerValue = null;
                                    drAnniversaryDateFilter.UpperValue = null;
                                }

                                break;
                            case RegistrationPersonFieldType.Grade:
                                var gpGradeFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GRADE_ID ) as GradePicker;
                                if ( gpGradeFilter != null )
                                {
                                    gpGradeFilter.SetValue( ( Guid? ) null );
                                }

                                break;

                            case RegistrationPersonFieldType.Gender:
                                var ddlGenderFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GENDER_ID ) as RockDropDownList;
                                if ( ddlGenderFilter != null )
                                {
                                    ddlGenderFilter.SetValue( ( Guid? ) null );
                                }

                                break;

                            case RegistrationPersonFieldType.MaritalStatus:
                                var dvpMaritalStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MARITAL_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpMaritalStatusFilter != null )
                                {
                                    dvpMaritalStatusFilter.SetValue( ( Guid? ) null );
                                }

                                break;

                            case RegistrationPersonFieldType.ConnectionStatus:
                                var dvpConnectionStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CONNECTION_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpConnectionStatusFilter != null )
                                {
                                    dvpConnectionStatusFilter.SetValue( ( Guid? ) null );
                                }

                                break;

                            case RegistrationPersonFieldType.MobilePhone:
                                var tbMobilePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MOBILE_PHONE_ID ) as RockTextBox;
                                if ( tbMobilePhoneFilter != null )
                                {
                                    tbMobilePhoneFilter.Text = string.Empty;
                                }

                                break;

                            case RegistrationPersonFieldType.HomePhone:
                                var tbHomePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_HOME_PHONE_ID ) as RockTextBox;
                                if ( tbHomePhoneFilter != null )
                                {
                                    tbHomePhoneFilter.Text = string.Empty;
                                }

                                break;
                        }
                    }
                }
            }

            BindRegistrantsFilter( null );
        }

        /// <summary>
        /// Gets the display value for a filter field.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fRegistrants_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( RegistrantFields != null )
            {
                var attribute = RegistrantFields
                    .Where( a =>
                        a.Attribute != null &&
                        a.Attribute.Key == e.Key )
                    .Select( a => a.Attribute )
                    .FirstOrDefault();

                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            switch ( e.Key )
            {
                case "Registrants Date Range":
                    e.Value = SlidingDateRangePicker.FormatDelimitedValues( e.Value );
                    break;

                case "Birthdate Range":
                    // The value might either be from a SlidingDateRangePicker or a DateRangePicker, so try both
                    var storedValue = e.Value;
                    e.Value = SlidingDateRangePicker.FormatDelimitedValues( storedValue );
                    if ( e.Value.IsNullOrWhiteSpace() )
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( storedValue );
                    }

                    break;

                case "Grade":
                    e.Value = Person.GradeFormattedFromGradeOffset( e.Value.AsIntegerOrNull() );
                    break;

                case "First Name":
                case "Last Name":
                case "Email":
                case "Signed Document":
                case "Home Phone":
                case "Cell Phone":
                    break;

                case "Gender":
                    var gender = e.Value.ConvertToEnumOrNull<Gender>();
                    e.Value = gender.HasValue ? gender.ConvertToString() : string.Empty;
                    break;

                case "Campus":
                    int? campusId = e.Value.AsIntegerOrNull();
                    if ( campusId.HasValue )
                    {
                        var campus = CampusCache.Get( campusId.Value );
                        e.Value = campus != null ? campus.Name : string.Empty;
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }

                    break;

                case "Marital Status":
                    int? dvId = e.Value.AsIntegerOrNull();
                    if ( dvId.HasValue )
                    {
                        var maritalStatus = DefinedValueCache.Get( dvId.Value );
                        e.Value = maritalStatus != null ? maritalStatus.Value : string.Empty;
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }

                    break;

                case "Connection Status":
                    int? connStatId = e.Value.AsIntegerOrNull();
                    if ( connStatId.HasValue )
                    {
                        var connectionStatus = DefinedValueCache.Get( connStatId.Value );
                        e.Value = connectionStatus != null ? connectionStatus.Value : string.Empty;
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }

                    break;

                case "In Group":
                    e.Value = e.Value;
                    break;

                default:
                    e.Value = string.Empty;
                    break;
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_GridRebind( object sender, GridRebindEventArgs e )
        {
            var registrationInstanceId = hfRegistrationInstanceId.Value.AsInteger();

            var registrationInstance = GetRegistrationInstance( registrationInstanceId, new RockContext() );

            var name = registrationInstance.Name.FormatAsHtmlTitle();

            gRegistrants.ExportTitleName = name + " - Registrants";
            gRegistrants.ExportFilename = gRegistrants.ExportFilename ?? name + "Registrants";
            BindRegistrantsGrid( e.IsExporting );
        }

        /// <summary>
        /// Handles the RowDataBound event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void gRegistrants_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var registrant = e.Row.DataItem as RegistrationRegistrant;
            if ( registrant == null )
            {
                return;
            }

            // Set the registrant name value
            var lRegistrant = e.Row.FindControl( "lRegistrant" ) as Literal;
            if ( lRegistrant != null )
            {
                if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                {
                    lRegistrant.Text = registrant.PersonAlias.Person.FullNameReversed;
                }
                else
                {
                    lRegistrant.Text = string.Empty;
                }
            }

            // Set the Group Name
            if ( registrant.GroupMember != null && GroupLinks.ContainsKey( registrant.GroupMember.GroupId ) )
            {
                var lGroup = e.Row.FindControl( "lGroup" ) as Literal;
                if ( lGroup != null )
                {
                    lGroup.Text = GroupLinks[registrant.GroupMember.GroupId];
                }
            }

            // Set the campus
            var lCampus = e.Row.FindControl( "lRegistrantsCampus" ) as Literal;

            // if it's null, try looking for the "lGroupPlacementsCampus" control since this RowDataBound event is shared between
            // two different grids.
            if ( lCampus == null )
            {
                lCampus = e.Row.FindControl( "lGroupPlacementsCampus" ) as Literal;
            }

            if ( lCampus != null && PersonCampusIds != null )
            {
                if ( registrant.PersonAlias != null )
                {
                    if ( PersonCampusIds.ContainsKey( registrant.PersonAlias.PersonId ) )
                    {
                        var campusIds = PersonCampusIds[registrant.PersonAlias.PersonId];
                        if ( campusIds.Any() )
                        {
                            var campusNames = new List<string>();
                            foreach ( int campusId in campusIds )
                            {
                                var campus = CampusCache.Get( campusId );
                                if ( campus != null )
                                {
                                    campusNames.Add( campus.Name );
                                }
                            }

                            lCampus.Text = campusNames.AsDelimited( "<br/>" );
                        }
                    }
                }
            }

            // Set the Fees
            var lFees = e.Row.FindControl( "lFees" ) as Literal;
            if ( lFees != null )
            {
                if ( registrant.Fees != null && registrant.Fees.Any() )
                {
                    var feeDesc = new List<string>();
                    foreach ( var fee in registrant.Fees )
                    {
                        feeDesc.Add( string.Format(
                            "{0}{1} ({2})",
                            fee.Quantity > 1 ? fee.Quantity.ToString( "N0" ) + " " : string.Empty,
                            fee.Quantity > 1 ? fee.RegistrationTemplateFee.Name.Pluralize() : fee.RegistrationTemplateFee.Name,
                            fee.Cost.FormatAsCurrency() ) );
                    }

                    lFees.Text = feeDesc.AsDelimited( "<br/>" );
                }
            }

            var lPlacements = e.Row.FindControl( "lPlacements" ) as Literal;
            if ( lPlacements != null )
            {
                SetPlacementFieldHtml( registrant, lPlacements );
            }

            if ( registrant.Registration.RegistrationInstance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue )
            {
                var lSignedDocument = e.Row.FindControl( "rlSignedDocument" ) as Literal;

                if ( _signatureDocuments.ContainsKey( registrant.PersonId.Value ) )
                {
                    var document = _signatureDocuments[registrant.PersonId.Value];
                    if ( document.Status == SignatureDocumentStatus.Signed )
                    {
                        lSignedDocument.Text = string.Format( SIGNATURE_LINK_TEMPLATE, FileUrlHelper.GetFileUrl( document.BinaryFileId ) );
                    }
                    else
                    {
                        string message;
                        if ( document.LastInviteDate.HasValue )
                        {
                            message = $"A signed {registrant.Registration.RegistrationInstance.RegistrationTemplate.Name} document has not yet been received for {registrant.NickName}. The last request was sent {document.LastInviteDate.Value.ToElapsedString()}.";
                        }
                        else
                        {
                            message = $"The required {registrant.Registration.RegistrationInstance.RegistrationTemplate.Name} document has not yet been sent to {registrant.NickName} for signing.";
                        }

                        lSignedDocument.Text = string.Format( SIGNATURE_NOT_SIGNED_INDICATOR, message );
                    }
                }
                else
                {
                    lSignedDocument.Text = string.Format( SIGNATURE_NOT_SIGNED_INDICATOR, "Document not signed" );
                }
            }

            if ( _homeAddresses.Any() && _homeAddresses.ContainsKey( registrant.PersonId.Value ) )
            {
                var location = _homeAddresses[registrant.PersonId.Value];

                // break up addresses if exporting
                if ( _isExporting )
                {
                    var lStreet1 = e.Row.FindControl( "lStreet1" ) as Literal;
                    var lStreet2 = e.Row.FindControl( "lStreet2" ) as Literal;
                    var lCity = e.Row.FindControl( "lCity" ) as Literal;
                    var lState = e.Row.FindControl( "lState" ) as Literal;
                    var lPostalCode = e.Row.FindControl( "lPostalCode" ) as Literal;
                    var lCountry = e.Row.FindControl( "lCountry" ) as Literal;

                    if ( location != null )
                    {
                        lStreet1.Text = location.Street1;
                        lStreet2.Text = location.Street2;
                        lCity.Text = location.City;
                        lState.Text = location.State;
                        lPostalCode.Text = location.PostalCode;
                        lCountry.Text = location.Country;
                    }
                }
                else
                {
                    var addressField = e.Row.FindControl( "lRegistrantsAddress" ) as Literal ?? e.Row.FindControl( "lGroupPlacementsAddress" ) as Literal;
                    if ( addressField != null )
                    {
                        addressField.Text = location != null && location.FormattedAddress.IsNotNullOrWhiteSpace() ? location.FormattedAddress : string.Empty;
                    }
                }
            }

            if ( _mobilePhoneNumbers.Any() )
            {
                var mobileNumber = _mobilePhoneNumbers[registrant.PersonId.Value];
                var mobileField = e.Row.FindControl( "lMobile" ) as Literal ?? e.Row.FindControl( "lRegistrantsMobile" ) as Literal ?? e.Row.FindControl( "lGroupPlacementsMobile" ) as Literal;
                if ( mobileField != null )
                {
                    if ( mobileNumber == null || mobileNumber.NumberFormatted.IsNullOrWhiteSpace() )
                    {
                        mobileField.Text = string.Empty;
                    }
                    else
                    {
                        mobileField.Text = mobileNumber.IsUnlisted ? "Unlisted" : mobileNumber.NumberFormatted;
                    }
                }
            }

            if ( _homePhoneNumbers.Any() )
            {
                var homePhoneNumber = _homePhoneNumbers[registrant.PersonId.Value];
                var homePhoneField = e.Row.FindControl( "lHomePhone" ) as Literal ?? e.Row.FindControl( "lRegistrantsHomePhone" ) as Literal ?? e.Row.FindControl( "lGroupPlacementsHomePhone" ) as Literal;
                if ( homePhoneField != null )
                {
                    if ( homePhoneNumber == null || homePhoneNumber.NumberFormatted.IsNullOrWhiteSpace() )
                    {
                        homePhoneField.Text = string.Empty;
                    }
                    else
                    {
                        homePhoneField.Text = homePhoneNumber.IsUnlisted ? "Unlisted" : homePhoneNumber.NumberFormatted;
                    }
                }
            }

            if ( _workPhoneNumbers.Any() )
            {
                var workPhoneNumber = _workPhoneNumbers[registrant.PersonId.Value];
                var workPhoneField = e.Row.FindControl( "lWorkPhone" ) as Literal ?? e.Row.FindControl( "lRegistrantsWorkPhone" ) as Literal ?? e.Row.FindControl( "lGroupPlacementsWorkPhone" ) as Literal;
                if ( workPhoneField != null )
                {
                    if ( workPhoneNumber == null || workPhoneNumber.NumberFormatted.IsNullOrWhiteSpace() )
                    {
                        workPhoneField.Text = string.Empty;
                    }
                    else
                    {
                        workPhoneField.Text = workPhoneNumber.IsUnlisted ? "Unlisted" : workPhoneNumber.NumberFormatted;
                    }
                }
            }

            // Set the registrant race
            var lRace = e.Row.FindControl( "lRace" ) as Literal;
            if ( lRace != null )
            {
                if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                {
                    lRace.Text = registrant.PersonAlias.Person.RaceValue?.Value;
                }
                else
                {
                    lRace.Text = string.Empty;
                }
            }

            // Set the registrant ethnicity
            var lEthnicity = e.Row.FindControl( "lEthnicity" ) as Literal;
            if ( lEthnicity != null )
            {
                if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                {
                    lEthnicity.Text = registrant.PersonAlias.Person.EthnicityValue?.Value;
                }
                else
                {
                    lEthnicity.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// Sets the placement field HTML.
        /// </summary>
        /// <param name="registrant">The registrant.</param>
        /// <param name="lPlacements">The l placements.</param>
        private void SetPlacementFieldHtml( RegistrationRegistrant registrant, Literal lPlacements )
        {
            var placementsHtmlBuilder = new StringBuilder();
            foreach ( var registrationTemplatePlacement in _registrationTemplatePlacements )
            {
                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "RegistrationTemplatePlacementId", registrationTemplatePlacement.Id.ToString() );
                queryParams.Add( "RegistrationInstanceId", this.RegistrationInstanceId.ToString() );

                /* NOTE: MDP - 2020-02-12
                  We could add RegistrantId has a parameter, but decided not to do this (yet).
                  // queryParams.Add( "RegistrantId", registrant.Id.ToString() );
                */

                var groupPlacementUrl = LinkedPageUrl( AttributeKey.GroupPlacementPage, queryParams );
                groupPlacementUrl += "#PersonId_" + registrant.PersonId.ToString();

                var registrantPlacedGroups = this._placementGroupInfoList.Where( a =>
                     ( a.RegistrationTemplatePlacementId.HasValue && a.RegistrationTemplatePlacementId.Value == registrationTemplatePlacement.Id )
                     || ( !a.RegistrationTemplatePlacementId.HasValue && a.Group.GroupTypeId == registrationTemplatePlacement.GroupTypeId ) )
                    .Where( a => a.PersonIds.Contains( registrant.PersonAlias.PersonId ) ).ToList();

                var groupCount = registrantPlacedGroups.Count();
                var toolTip = registrantPlacedGroups.Select( a => a.Group.Name ).ToList().AsDelimited( ", ", " and " );

                string btnClass;
                if ( groupCount > 0 )
                {
                    btnClass = "btn btn-success btn-xs btn-placement-status registrant-is-placed";
                }
                else
                {
                    btnClass = "btn btn-default btn-xs btn-placement-status registrant-not-placed";
                }

                string iconCssClass = registrationTemplatePlacement.GetIconCssClass();
                if ( iconCssClass.IsNullOrWhiteSpace() )
                {
                    iconCssClass = "fa fa-users";
                }

                string groupCountText;
                if ( registrationTemplatePlacement.AllowMultiplePlacements )
                {
                    groupCountText = groupCount.ToString();
                }
                else
                {
                    groupCountText = string.Empty;
                }

                if ( _isExporting )
                {
                    placementsHtmlBuilder.AppendLine( toolTip );
                }
                else
                {
                    placementsHtmlBuilder.AppendLine(
                        string.Format(
                            @"<a class='{0}' href='{1}' title='{2}'><i class='{3}'></i>{4}</a>",
                            btnClass, // {0}
                            groupPlacementUrl, // {1}
                            toolTip, // {2}
                            iconCssClass, // {3}
                            groupCountText ) ); // {4}
                }
            }

            lPlacements.Text = string.Format( "<div class='placement-list'>{0}</div>", placementsHtmlBuilder.ToString() );
        }

        /// <summary>
        /// Handles the AddClick event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gRegistrants_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.RegistrationPage, "RegistrationId", 0, "RegistrationInstanceId", hfRegistrationInstanceId.ValueAsInt() );
        }

        /// <summary>
        /// Handles the RowSelected event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_RowSelected( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrant = registrantService.Get( e.RowKeyId );
                if ( registrant != null )
                {
                    var qryParams = new Dictionary<string, string>();
                    qryParams.Add( "RegistrationId", registrant.RegistrationId.ToString() );
                    qryParams.Add( "RegistrationInstanceId", hfRegistrationInstanceId.Value );
                    string url = LinkedPageUrl( AttributeKey.RegistrationPage, qryParams );
                    url += "#" + e.RowKeyValue;
                    Response.Redirect( url, false );
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        /// <summary>
        /// Handles the Delete event of the gRegistrants control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gRegistrants_Delete( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrantService = new RegistrationRegistrantService( rockContext );
                var registrant = registrantService.Get( e.RowKeyId );
                if ( registrant != null )
                {
                    string errorMessage;
                    if ( !registrantService.CanDelete( registrant, out errorMessage ) )
                    {
                        mdRegistrantsGridWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    registrantService.Delete( registrant );
                    rockContext.SaveChanges();
                }
            }

            BindRegistrantsGrid();
        }

        /// <summary>
        /// Handles the Registrar Communication event of the Custom Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void LbRegistrarCommunication_Click( object sender, EventArgs e )
        {
            /*
                04/01/2022 - CWR
                This click event mirrors the Grid.cs -> Actions_Communicate event, without access to the private methods and properties.
                Clicking a Grid Action button without selecting any rows assumes that all rows are desired (the behavior of Grid's Actions_Communicate).
                In order to achieve this, we need to use the BindRegistrantsGrid method without paging to bring back all the Grid's DataKeys.
             */
            var itemsSelected = new List<int>();
            if ( gRegistrants.SelectedKeys.Any() )
            {
                gRegistrants.SelectedKeys.ToList().ForEach( f => itemsSelected.Add( f.ToString().AsInteger() ) );
            }
            else
            {
                // If nothing is selected, assume all, and add all the data keys to the itemsSelected list.

                // If the grid allows paging and there's more than one page, rebind the grid without paging so that all keys are available.
                if ( gRegistrants.AllowPaging && gRegistrants.PageCount > 1 )
                {
                    gRegistrants.AllowPaging = false;

                    BindRegistrantsGrid();
                }

                foreach ( DataKey dataKey in gRegistrants.DataKeys )
                {
                    itemsSelected.Add( dataKey.Value.ToString().AsInteger() );
                }
            }

            // Create a dictionary of the additional merge fields that were created for the communication
            var communicationMergeFields = new Dictionary<string, string>();
            foreach ( string mergeField in gRegistrants.CommunicateMergeFields )
            {
                var parts = mergeField.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                if ( parts.Any() )
                {
                    communicationMergeFields.TryAdd( parts.First().Replace( '.', '_' ), parts.Last().Replace( '.', '_' ) );
                }
            }

            if ( itemsSelected.Any() )
            {
                var rockContext = new RockContext();
                var registrationRegistrantService = new RegistrationRegistrantService( rockContext );

                // Get the PersonAliases from the selected registrants.
                var personAliasIdQuery = registrationRegistrantService.Queryable()
                    .Where( rr => itemsSelected.Contains( rr.Id ) )
                    .Select( p => p.PersonAliasId );

                var registrationQuery = registrationRegistrantService.Queryable()
                    .Where( r =>
                    personAliasIdQuery.Contains( r.PersonAliasId.Value ) &&
                    r.Registration.RegistrationInstanceId == this.RegistrationInstanceId )
                    .Select( r => r.Registration );

                var personIds = registrationQuery.Select( r => r.PersonAlias.PersonId ).Distinct().ToList();

                if ( personIds.Any() )
                {
                    // Create communication
                    var communicationRockContext = new RockContext();
                    var communicationService = new CommunicationService( communicationRockContext );
                    var communication = new Communication();
                    communication.IsBulkCommunication = true;
                    communication.Status = CommunicationStatus.Transient;

                    if ( CurrentPerson != null )
                    {
                        communication.SenderPersonAliasId = CurrentPersonAliasId;
                    }

                    if ( Request != null && Request.UrlProxySafe() != null )
                    {
                        communication.UrlReferrer = Request.UrlProxySafe().AbsoluteUri.TrimForMaxLength( communication, "UrlReferrer" );
                    }

                    communicationService.Add( communication );

                    // save communication to get Id
                    communicationRockContext.SaveChanges();

                    var personAliasService = new PersonAliasService( new RockContext() );

                    // Get the primary aliases
                    List<Rock.Model.PersonAlias> primaryAliasList = new List<PersonAlias>( personIds.Count );

                    // get the data in chunks just in case we have a large list of PersonIds (to avoid a SQL Expression limit error)
                    var chunkedPersonIds = personIds.Take( 1000 );
                    int skipCount = 0;
                    while ( chunkedPersonIds.Any() )
                    {
                        var chunkedPrimaryAliasList = personAliasService.Queryable()
                            .Where( p => p.PersonId == p.AliasPersonId && chunkedPersonIds.Contains( p.PersonId ) ).AsNoTracking().ToList();
                        primaryAliasList.AddRange( chunkedPrimaryAliasList );
                        skipCount += 1000;
                        chunkedPersonIds = personIds.Skip( skipCount ).Take( 1000 );
                    }

                    // NOTE: Set CreatedDateTime, ModifiedDateTime, etc manually set we are using BulkInsert
                    var currentDateTime = RockDateTime.Now;
                    var currentPersonAliasId = CurrentPersonAliasId;

                    var communicationRecipientList = primaryAliasList.Select( a => new Rock.Model.CommunicationRecipient
                    {
                        CommunicationId = communication.Id,
                        PersonAliasId = a.Id,
                        CreatedByPersonAliasId = currentPersonAliasId,
                        ModifiedByPersonAliasId = currentPersonAliasId,
                        CreatedDateTime = currentDateTime,
                        ModifiedDateTime = currentDateTime
                    } ).ToList();

                    // BulkInsert to quickly insert the CommunicationRecipient records. Note: This is much faster, but will bypass EF and Rock processing.
                    var communicationRecipientRockContext = new RockContext();
                    communicationRecipientRockContext.BulkInsert( communicationRecipientList );

                    var pageRef = this.RockPage.Site.CommunicationPageReference;
                    string communicationUrl;
                    if ( pageRef.PageId > 0 )
                    {
                        pageRef.Parameters.AddOrReplace( "CommunicationId", communication.Id.ToString() );
                        communicationUrl = pageRef.BuildUrl();
                    }
                    else
                    {
                        communicationUrl = "~/Communication/{0}";
                    }

                    Page.Response.Redirect( communicationUrl, false );
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    // No people found in the registrations query.
                    BindRegistrantsGrid();
                    gRegistrants.ShowModalAlertMessage( "Registrations list has no recipients", ModalAlertType.Warning );
                }
            }
            else
            {
                // Nobody is in list or nobody is selected.
                BindRegistrantsGrid();
                gRegistrants.ShowModalAlertMessage( "Grid has no recipients", ModalAlertType.Warning );
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        private void ShowDetail()
        {
            var registrationInstance = this.RegistrationInstance;

            if ( registrationInstance == null )
            {
                pnlDetails.Visible = false;
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                hfRegistrationInstanceId.Value = registrationInstance.Id.ToString();
                hfRegistrationTemplateId.Value = registrationInstance.RegistrationTemplateId.ToString();

                this.RegistrantFields = GetRegistrantFormFields().Where( a => a.IsGridField ).ToArray();

                SetUserPreferencePrefix( hfRegistrationTemplateId.ValueAsInt() );

                AddDynamicControls( true );

                BindRegistrantsFilter( registrationInstance );
                BindRegistrantsGrid();
            }
        }

        /// <summary>
        /// Sets the user preference prefix.
        /// </summary>
        private void SetUserPreferencePrefix( int registrationTemplateId )
        {
            fRegistrants.PreferenceKeyPrefix = string.Format( "{0}-", registrationTemplateId );
        }

        /// <summary>
        /// Binds the registrants filter.
        /// </summary>
        private void BindRegistrantsFilter( RegistrationInstance instance )
        {
            sdrpRegistrantsRegistrantDateRange.DelimitedValues = fRegistrants.GetFilterPreference( UserPreferenceKeyBase.GridFilter_RegistrantsDateRange );
            tbRegistrantsRegistrantFirstName.Text = fRegistrants.GetFilterPreference( UserPreferenceKeyBase.GridFilter_FirstName );
            tbRegistrantsRegistrantLastName.Text = fRegistrants.GetFilterPreference( UserPreferenceKeyBase.GridFilter_LastName );
            ddlRegistrantsInGroup.SetValue( fRegistrants.GetFilterPreference( UserPreferenceKeyBase.GridFilter_InGroup ) );

            ddlRegistrantsSignedDocument.SetValue( fRegistrants.GetFilterPreference( UserPreferenceKeyBase.GridFilter_SignedDocument ) );
            ddlRegistrantsSignedDocument.Visible = instance != null && instance.RegistrationTemplate != null && instance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.HasValue;
        }

        /// <summary>
        /// Binds the registrants grid.
        /// </summary>
        private void BindRegistrantsGrid( bool isExporting = false )
        {
            _isExporting = isExporting;

            int? instanceId = this.RegistrationInstanceId;

            if ( !instanceId.HasValue )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var registrationInstanceService = new RegistrationInstanceService( rockContext );

                var registrationInstance = registrationInstanceService.GetNoTracking( instanceId.Value );
                if ( registrationInstance == null )
                {
                    return;
                }

                var requiredSignatureDocumentTemplateId = registrationInstance.RegistrationTemplate.RequiredSignatureDocumentTemplateId;

                if ( requiredSignatureDocumentTemplateId.HasValue )
                {
                    rlSignedDocument.Visible = true;

                    SignersPersonAliasIds = new SignatureDocumentService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( d =>
                            d.SignatureDocumentTemplateId == requiredSignatureDocumentTemplateId.Value &&
                            d.Status == SignatureDocumentStatus.Signed &&
                            d.BinaryFileId.HasValue &&
                            d.AppliesToPersonAlias != null )
                        .OrderByDescending( d => d.LastStatusDate )
                        .Select( d => d.AppliesToPersonAlias.PersonId )
                        .ToList();
                }

                // Start query for registrants
                var registrationRegistrantService = new RegistrationRegistrantService( rockContext );
                var qry = registrationRegistrantService
                .Queryable( "PersonAlias.Person.PhoneNumbers.NumberTypeValue,Fees.RegistrationTemplateFee,GroupMember.Group" ).AsNoTracking()
                .Where( r =>
                    r.Registration.RegistrationInstanceId == instanceId.Value &&
                    r.PersonAlias != null &&
                    r.PersonAlias.Person != null &&
                    r.OnWaitList == false );

                // Filter by daterange
                var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( sdrpRegistrantsRegistrantDateRange.DelimitedValues );
                if ( dateRange.Start.HasValue )
                {
                    qry = qry.Where( r =>
                        r.CreatedDateTime.HasValue &&
                        r.CreatedDateTime.Value >= dateRange.Start.Value );
                }

                if ( dateRange.End.HasValue )
                {
                    qry = qry.Where( r =>
                        r.CreatedDateTime.HasValue &&
                        r.CreatedDateTime.Value < dateRange.End.Value );
                }

                // Filter by first name
                if ( !string.IsNullOrWhiteSpace( tbRegistrantsRegistrantFirstName.Text ) )
                {
                    string rfname = tbRegistrantsRegistrantFirstName.Text;
                    qry = qry.Where( r =>
                        r.PersonAlias.Person.NickName.StartsWith( rfname ) ||
                        r.PersonAlias.Person.FirstName.StartsWith( rfname ) );
                }

                // Filter by last name
                if ( !string.IsNullOrWhiteSpace( tbRegistrantsRegistrantLastName.Text ) )
                {
                    string rlname = tbRegistrantsRegistrantLastName.Text;
                    qry = qry.Where( r =>
                        r.PersonAlias.Person.LastName.StartsWith( rlname ) );
                }

                // Filter by signed documents
                if ( SignersPersonAliasIds != null )
                {
                    if ( ddlRegistrantsSignedDocument.SelectedValue.AsBooleanOrNull() == true )
                    {
                        qry = qry.Where( r => SignersPersonAliasIds.Contains( r.PersonAlias.PersonId ) );
                    }
                    else if ( ddlRegistrantsSignedDocument.SelectedValue.AsBooleanOrNull() == false )
                    {
                        qry = qry.Where( r => !SignersPersonAliasIds.Contains( r.PersonAlias.PersonId ) );
                    }
                }

                if ( ddlRegistrantsInGroup.SelectedValue.AsBooleanOrNull() == true )
                {
                    qry = qry.Where( r => r.GroupMemberId.HasValue );
                }
                else if ( ddlRegistrantsInGroup.SelectedValue.AsBooleanOrNull() == false )
                {
                    qry = qry.Where( r => !r.GroupMemberId.HasValue );
                }

                bool preloadCampusValues = false;
                var registrantAttributes = new List<AttributeCache>();
                var personAttributes = new List<AttributeCache>();
                var groupMemberAttributes = new List<AttributeCache>();
                var registrantAttributeIds = new List<int>();
                var personAttributesIds = new List<int>();
                var groupMemberAttributesIds = new List<int>();

                var personIds = qry.Select( r => r.PersonAlias.PersonId ).Distinct().ToList();

                gRegistrants.EntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.RegistrationRegistrant ) ).Id;

                if ( isExporting || ( RegistrantFields != null && RegistrantFields.Any( f => f.PersonFieldType != null && f.PersonFieldType == RegistrationPersonFieldType.Address ) ) )
                {
                    _homeAddresses = Person.GetHomeLocations( personIds );
                }

                _registrationTemplatePlacements = registrationInstance.RegistrationTemplate.Placements.OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList();

                _placementsField.Visible = _registrationTemplatePlacements.Any();

                if ( _registrationTemplatePlacements.Any() )
                {
                    var registrationTemplatePlacementService = new RegistrationTemplatePlacementService( rockContext );

                    _placementGroupInfoList = new List<PlacementGroupInfo>();
                    foreach ( var placementTemplate in _registrationTemplatePlacements )
                    {
                        // Template Placement Id is needed as a parameter to properly collect placements by their group.
                        var instancePlacementGroupsByTemplateQry = registrationInstanceService.GetRegistrationInstancePlacementGroupsByPlacement( registrationInstance, placementTemplate.Id );
                        var _instancePlacementGroupInfoList = instancePlacementGroupsByTemplateQry.AsNoTracking().Select( s => new
                        {
                            Group = s,
                            PersonIds = s.Members.Select( m => m.PersonId ).ToList()
                        } )
                            .ToList()
                            .Select( a => new PlacementGroupInfo
                            {
                                Group = a.Group,
                                RegistrationTemplatePlacementId = placementTemplate.Id,
                                PersonIds = a.PersonIds.ToArray(),
                            } ).ToList();

                        if ( _instancePlacementGroupInfoList.Any() )
                        {
                            _placementGroupInfoList.AddRange( _instancePlacementGroupInfoList );
                        }

                        var registrationTemplatePlacementPlacementGroupsQuery = registrationTemplatePlacementService.GetRegistrationTemplatePlacementPlacementGroups( placementTemplate );
                        var templatePlacementGroupInfoList = registrationTemplatePlacementPlacementGroupsQuery.AsNoTracking()
                            .Select( s => new
                            {
                                Group = s,
                                PersonIds = s.Members.Select( m => m.PersonId ).ToList()
                            } )
                            .ToList()
                            .Select( a => new PlacementGroupInfo
                            {
                                Group = a.Group,
                                RegistrationTemplatePlacementId = placementTemplate.Id,
                                PersonIds = a.PersonIds.ToArray()
                            } ).ToList();

                        _placementGroupInfoList = _placementGroupInfoList.Union( templatePlacementGroupInfoList ).ToList();
                    }
                }
                else
                {
                    _placementGroupInfoList = new List<PlacementGroupInfo>();
                }

                if ( RegistrantFields != null )
                {
                    _mobilePhoneNumbers = GetPersonMobilePhoneLookup( rockContext, this.RegistrantFields, personIds );
                    _homePhoneNumbers = GetPersonHomePhoneLookup( rockContext, this.RegistrantFields, personIds );
                    _workPhoneNumbers = GetPersonWorkPhoneLookup( rockContext, this.RegistrantFields, personIds );
                    _signatureDocuments = GetPersonSignatureDocumentLookup( rockContext, personIds, registrationInstance );

                    // Filter by any selected
                    foreach ( var personFieldType in RegistrantFields
                        .Where( f =>
                            f.FieldSource == RegistrationFieldSource.PersonField &&
                            f.PersonFieldType.HasValue )
                        .Select( f => f.PersonFieldType.Value ) )
                    {
                        switch ( personFieldType )
                        {
                            case RegistrationPersonFieldType.Campus:
                                preloadCampusValues = true;

                                var ddlCampus = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CAMPUS_ID ) as RockDropDownList;
                                if ( ddlCampus != null )
                                {
                                    var campusId = ddlCampus.SelectedValue.AsIntegerOrNull();
                                    if ( campusId.HasValue )
                                    {
                                        var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.Members.Any( m =>
                                                m.Group.GroupType.Guid == familyGroupTypeGuid &&
                                                m.Group.CampusId.HasValue &&
                                                m.Group.CampusId.Value == campusId ) );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.Email:
                                var tbEmailFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_EMAIL_ID ) as RockTextBox;
                                if ( tbEmailFilter != null && !string.IsNullOrWhiteSpace( tbEmailFilter.Text ) )
                                {
                                    qry = qry.Where( r =>
                                        r.PersonAlias.Person.Email != null &&
                                        r.PersonAlias.Person.Email.Contains( tbEmailFilter.Text ) );
                                }

                                break;

                            case RegistrationPersonFieldType.Birthdate:
                                var drpBirthdateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_BIRTHDATE_ID ) as DateRangePicker;
                                if ( drpBirthdateFilter != null )
                                {
                                    if ( drpBirthdateFilter.LowerValue.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.BirthDate.HasValue &&
                                            r.PersonAlias.Person.BirthDate.Value >= drpBirthdateFilter.LowerValue.Value );
                                    }

                                    if ( drpBirthdateFilter.UpperValue.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.BirthDate.HasValue &&
                                            r.PersonAlias.Person.BirthDate.Value <= drpBirthdateFilter.UpperValue.Value );
                                    }
                                }

                                break;
                            case RegistrationPersonFieldType.MiddleName:
                                var tbMiddleNameFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MIDDLE_NAME_ID ) as RockTextBox;
                                if ( tbMiddleNameFilter != null && !string.IsNullOrWhiteSpace( tbMiddleNameFilter.Text ) )
                                {
                                    qry = qry.Where( r =>
                                        r.PersonAlias.Person.MiddleName != null &&
                                        r.PersonAlias.Person.MiddleName.Contains( tbMiddleNameFilter.Text ) );
                                }

                                break;
                            case RegistrationPersonFieldType.AnniversaryDate:
                                var drpAnniversaryDateFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ANNIVERSARY_DATE_ID ) as DateRangePicker;
                                if ( drpAnniversaryDateFilter != null )
                                {
                                    if ( drpAnniversaryDateFilter.LowerValue.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.AnniversaryDate.HasValue &&
                                            r.PersonAlias.Person.AnniversaryDate.Value >= drpAnniversaryDateFilter.LowerValue.Value );
                                    }

                                    if ( drpAnniversaryDateFilter.UpperValue.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.AnniversaryDate.HasValue &&
                                            r.PersonAlias.Person.AnniversaryDate.Value <= drpAnniversaryDateFilter.UpperValue.Value );
                                    }
                                }

                                break;
                            case RegistrationPersonFieldType.Grade:
                                var gpGradeFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GRADE_ID ) as GradePicker;
                                if ( gpGradeFilter != null )
                                {
                                    int? graduationYear = Person.GraduationYearFromGradeOffset( gpGradeFilter.SelectedValueAsInt( false ) );
                                    if ( graduationYear.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.GraduationYear.HasValue &&
                                            r.PersonAlias.Person.GraduationYear == graduationYear.Value );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.Gender:
                                var ddlGenderFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_GENDER_ID ) as RockDropDownList;
                                if ( ddlGenderFilter != null )
                                {
                                    var gender = ddlGenderFilter.SelectedValue.ConvertToEnumOrNull<Gender>();
                                    if ( gender.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.Gender == gender );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.MaritalStatus:
                                var dvpMaritalStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MARITAL_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpMaritalStatusFilter != null )
                                {
                                    var maritalStatusId = dvpMaritalStatusFilter.SelectedValue.AsIntegerOrNull();
                                    if ( maritalStatusId.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                            r.PersonAlias.Person.MaritalStatusValueId.HasValue &&
                                            r.PersonAlias.Person.MaritalStatusValueId.Value == maritalStatusId.Value );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.ConnectionStatus:
                                var dvpConnectionStatusFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_CONNECTION_STATUS_ID ) as DefinedValuePicker;
                                if ( dvpConnectionStatusFilter != null )
                                {
                                    var connectionStatusId = dvpConnectionStatusFilter.SelectedValue.AsIntegerOrNull();
                                    if ( connectionStatusId.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                           r.PersonAlias.Person.ConnectionStatusValueId.HasValue &&
                                           r.PersonAlias.Person.ConnectionStatusValueId.Value == connectionStatusId.Value );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.MobilePhone:
                                var tbMobilePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_MOBILE_PHONE_ID ) as RockTextBox;
                                if ( tbMobilePhoneFilter != null && !string.IsNullOrWhiteSpace( tbMobilePhoneFilter.Text ) )
                                {
                                    string numericPhone = tbMobilePhoneFilter.Text.AsNumeric();
                                    if ( !string.IsNullOrEmpty( numericPhone ) )
                                    {
                                        var phoneNumberPersonIdQry = new PhoneNumberService( rockContext )
                                            .Queryable()
                                            .Where( a => a.Number.Contains( numericPhone ) )
                                            .Select( a => a.PersonId );

                                        qry = qry.Where( r => phoneNumberPersonIdQry.Contains( r.PersonAlias.PersonId ) );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.HomePhone:
                                var tbHomePhoneFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_HOME_PHONE_ID ) as RockTextBox;
                                if ( tbHomePhoneFilter != null && !string.IsNullOrWhiteSpace( tbHomePhoneFilter.Text ) )
                                {
                                    string numericPhone = tbHomePhoneFilter.Text.AsNumeric();
                                    if ( !string.IsNullOrEmpty( numericPhone ) )
                                    {
                                        var phoneNumberPersonIdQry = new PhoneNumberService( rockContext )
                                            .Queryable()
                                            .Where( a => a.Number.Contains( numericPhone ) )
                                            .Select( a => a.PersonId );

                                        qry = qry.Where( r => phoneNumberPersonIdQry.Contains( r.PersonAlias.PersonId ) );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.Race:
                                var rpRaceFilter = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_RACE_ID ) as RacePicker;
                                if ( rpRaceFilter != null )
                                {
                                    var raceValueId = rpRaceFilter.SelectedValue.AsIntegerOrNull();
                                    if ( raceValueId.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                           r.PersonAlias.Person.RaceValueId.HasValue &&
                                           r.PersonAlias.Person.RaceValueId.Value == raceValueId.Value );
                                    }
                                }

                                break;

                            case RegistrationPersonFieldType.Ethnicity:
                                var epEthnicityPicker = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ETHNICITY_ID ) as EthnicityPicker;
                                if ( epEthnicityPicker != null )
                                {
                                    var ethnicityValueId = epEthnicityPicker.SelectedValue.AsIntegerOrNull();
                                    if ( ethnicityValueId.HasValue )
                                    {
                                        qry = qry.Where( r =>
                                           r.PersonAlias.Person.EthnicityValueId.HasValue &&
                                           r.PersonAlias.Person.EthnicityValueId.Value == ethnicityValueId.Value );
                                    }
                                }

                                break;
                        }
                    }

                    // Get all the registrant attributes selected to be on grid
                    registrantAttributes = RegistrantFields
                        .Where( f =>
                            f.Attribute != null &&
                            f.FieldSource == RegistrationFieldSource.RegistrantAttribute )
                        .Select( f => f.Attribute )
                        .ToList();
                    registrantAttributeIds = registrantAttributes.Select( a => a.Id ).Distinct()
                        .ToList();

                    // Filter query by any configured registrant attribute filters
                    if ( registrantAttributes != null && registrantAttributes.Any() )
                    {
                        foreach ( var attribute in registrantAttributes )
                        {
                            var filterControl = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ATTRIBUTE_PREFIX + attribute.Id.ToString() );
                            qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, registrationRegistrantService, Rock.Reporting.FilterMode.SimpleFilter );
                        }
                    }

                    // Get all the person attributes selected to be on grid
                    personAttributes = RegistrantFields
                        .Where( f =>
                            f.Attribute != null &&
                            f.FieldSource == RegistrationFieldSource.PersonAttribute )
                        .Select( f => f.Attribute )
                        .ToList()
                        .Where( a => a.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson ) )
                        .ToList();
                    personAttributesIds = personAttributes.Select( a => a.Id ).Distinct().ToList();

                    // Filter query by any configured person attribute filters
                    if ( personAttributes != null && personAttributes.Any() )
                    {
                        PersonService personService = new PersonService( rockContext );
                        var personQry = personService.Queryable().AsNoTracking();
                        foreach ( var attribute in personAttributes )
                        {
                            var filterControl = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ATTRIBUTE_PREFIX + attribute.Id.ToString() );
                            personQry = attribute.FieldType.Field.ApplyAttributeQueryFilter( personQry, filterControl, attribute, personService, Rock.Reporting.FilterMode.SimpleFilter );
                        }

                        qry = qry.Where( r => personQry.Any( p => p.Id == r.PersonAlias.PersonId ) );
                    }

                    // Get all the group member attributes selected to be on grid
                    groupMemberAttributes = RegistrantFields
                        .Where( f =>
                            f.Attribute != null &&
                            f.FieldSource == RegistrationFieldSource.GroupMemberAttribute )
                        .Select( f => f.Attribute )
                        .ToList();
                    groupMemberAttributesIds = groupMemberAttributes.Select( a => a.Id ).Distinct().ToList();

                    // Filter query by any configured person attribute filters
                    if ( groupMemberAttributes != null && groupMemberAttributes.Any() )
                    {
                        /*
                            SK - 07/23/2024
                            The logic below evaluates if any filter is applied to any group member attribute. If not, it returns all the data irrespective of whether the registrant is part of any group. If a filter is applied, the registrant must be part of a group.
                        */
                        var groupMemberService = new GroupMemberService( rockContext );
                        var groupMemberQry = groupMemberService.Queryable().AsNoTracking();
                        bool isFilterModeApplied = false;
                        foreach ( var attribute in groupMemberAttributes )
                        {
                            var filterControl = phRegistrantsRegistrantFormFieldFilters.FindControl( FILTER_ATTRIBUTE_PREFIX + attribute.Id.ToString() );
                            var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            if ( filterValues.Count > 1 )
                            {
                                var comparisionType = filterValues[0].ConvertToEnumOrNull<ComparisonType>();
                                if ( comparisionType.HasValue && ( comparisionType == ComparisonType.IsBlank || comparisionType == ComparisonType.IsNotBlank || filterValues.Last().IsNotNullOrWhiteSpace() ) )
                                {
                                    isFilterModeApplied = true;
                                }
                            }
                            else if ( filterValues.Any( a => a.IsNotNullOrWhiteSpace() ) )
                            {
                                isFilterModeApplied = true;
                            }
                        }

                        if ( isFilterModeApplied )
                        {
                            qry = qry.Where( r => groupMemberQry.Any( g => g.Id == r.GroupMemberId ) );
                        }
                    }
                }

                // Sort the query
                IOrderedQueryable<RegistrationRegistrant> orderedQry = null;
                SortProperty sortProperty = gRegistrants.SortProperty;
                if ( sortProperty != null )
                {
                    orderedQry = qry.Sort( sortProperty );
                }
                else
                {
                    orderedQry = qry
                        .OrderBy( r => r.PersonAlias.Person.LastName )
                        .ThenBy( r => r.PersonAlias.Person.NickName );
                }

                // increase the timeout just in case. A complex filter on the grid might slow things down
                rockContext.Database.CommandTimeout = 180;

                // Set the grids LinqDataSource which will run query and set results for current page
                gRegistrants.SetLinqDataSource<RegistrationRegistrant>( orderedQry );

                if ( RegistrantFields != null )
                {
                    // Get the query results for the current page
                    var currentPageRegistrants = gRegistrants.DataSource as List<RegistrationRegistrant>;
                    if ( currentPageRegistrants != null )
                    {
                        // Get all the registrant ids in current page of query results
                        var registrantIds = currentPageRegistrants
                            .Select( r => r.Id )
                            .Distinct()
                            .ToList();

                        // Get all the person ids in current page of query results
                        var currentPagePersonIds = currentPageRegistrants
                            .Select( r => r.PersonAlias.PersonId )
                            .Distinct()
                            .ToList();

                        // Get all the group member ids and the group id in current page of query results
                        var groupMemberIds = new List<int>();
                        GroupLinks = new Dictionary<int, string>();
                        foreach ( var groupMember in currentPageRegistrants
                            .Where( m =>
                                m.GroupMember != null &&
                                m.GroupMember.Group != null )
                            .Select( m => m.GroupMember ) )
                        {
                            groupMemberIds.Add( groupMember.Id );

                            string linkedPageUrl = LinkedPageUrl( AttributeKey.GroupDetailPage, new Dictionary<string, string> { { "GroupId", groupMember.GroupId.ToString() } } );
                            GroupLinks.TryAdd(
                                groupMember.GroupId,
                                isExporting ? groupMember.Group.Name : string.Format( "<a href='{0}'>{1}</a>", linkedPageUrl, groupMember.Group.Name ) );
                        }

                        // If the campus column was selected to be displayed on grid, preload all the people's
                        // campuses so that the databind does not need to query each row
                        if ( preloadCampusValues )
                        {
                            PersonCampusIds = new Dictionary<int, List<int>>();

                            Guid familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                            foreach ( var personCampusList in new GroupMemberService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( m =>
                                    m.Group.GroupType.Guid == familyGroupTypeGuid &&
                                    currentPagePersonIds.Contains( m.PersonId ) )
                                .GroupBy( m => m.PersonId )
                                .Select( m => new
                                {
                                    PersonId = m.Key,
                                    CampusIds = m
                                        .Where( g => g.Group.CampusId.HasValue )
                                        .Select( g => g.Group.CampusId.Value )
                                        .ToList()
                                } ) )
                            {
                                PersonCampusIds.Add( personCampusList.PersonId, personCampusList.CampusIds );
                            }
                        }

                        // If there are any attributes that were selected to be displayed, we're going
                        // to try and read all attribute values in one query and then put them into a
                        // custom grid ObjectList property so that the AttributeField columns don't need
                        // to do the LoadAttributes and querying of values for each row/column
                        if ( personAttributesIds.Any() || groupMemberAttributesIds.Any() || registrantAttributeIds.Any() )
                        {
                            // Query the attribute values for all rows and attributes
                            var attributeValues = new AttributeValueService( rockContext )
                                .Queryable( "Attribute" ).AsNoTracking()
                                .Where( v =>
                                    v.EntityId.HasValue &&
                                    (
                                        (
                                            personAttributesIds.Contains( v.AttributeId ) &&
                                            currentPagePersonIds.Contains( v.EntityId.Value )
                                        ) ||
                                        (
                                            groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                            groupMemberIds.Contains( v.EntityId.Value )
                                        ) ||
                                        (
                                            registrantAttributeIds.Contains( v.AttributeId ) &&
                                            registrantIds.Contains( v.EntityId.Value )
                                        )
                                    ) ).ToList();

                            // Get the attributes to add to each row's object
                            var attributes = new Dictionary<string, AttributeCache>();
                            RegistrantFields
                                    .Where( f => f.Attribute != null )
                                    .Select( f => f.Attribute )
                                    .ToList()
                                .ForEach( a => attributes
                                    .TryAdd( a.Id.ToString() + a.Key, a ) );

                            // Initialize the grid's object list
                            gRegistrants.ObjectList = new Dictionary<string, object>();

                            // Loop through each of the current page's registrants and build an attribute
                            // field object for storing attributes and the values for each of the registrants
                            foreach ( var registrant in currentPageRegistrants )
                            {
                                // Create a row attribute object
                                var attributeFieldObject = new AttributeFieldObject();

                                // Add the attributes to the attribute object
                                attributeFieldObject.Attributes = attributes;

                                // Add any person attribute values to object
                                attributeValues
                                    .Where( v =>
                                        personAttributesIds.Contains( v.AttributeId ) &&
                                        v.EntityId.Value == registrant.PersonAlias.PersonId )
                                    .ToList()
                                    .ForEach( v => attributeFieldObject.AttributeValues
                                        .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                // Add any group member attribute values to object
                                if ( registrant.GroupMemberId.HasValue )
                                {
                                    attributeValues
                                        .Where( v =>
                                            groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                            v.EntityId.Value == registrant.GroupMemberId.Value )
                                        .ToList()
                                        .ForEach( v => attributeFieldObject.AttributeValues
                                            .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );
                                }

                                // Add any registrant attribute values to object
                                attributeValues
                                    .Where( v =>
                                        registrantAttributeIds.Contains( v.AttributeId ) &&
                                        v.EntityId.Value == registrant.Id )
                                    .ToList()
                                    .Where( a => a.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson ) )
                                    .ToList()
                                    .ForEach( v => attributeFieldObject.AttributeValues
                                        .Add( v.AttributeId.ToString() + v.Attribute.Key, new AttributeValueCache( v ) ) );

                                // Add row attribute object to grid's object list
                                gRegistrants.ObjectList.Add( registrant.Id.ToString(), attributeFieldObject );
                            }
                        }
                    }
                }

                gRegistrants.DataBind();
            }
        }

        /// <summary>
        /// Gets the person signature document lookup.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="personIds">The person ids.</param>
        /// <param name="registrationInstance">The registration instance.</param>
        /// <returns></returns>
        private Dictionary<int, SignatureDocument> GetPersonSignatureDocumentLookup( RockContext rockContext, List<int> personIds, RegistrationInstance registrationInstance )
        {
            var signatureDocuments = new Dictionary<int, SignatureDocument>();
            var documents = new SignatureDocumentService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( d =>
                        d.SignatureDocumentTemplateId == registrationInstance.RegistrationTemplate.RequiredSignatureDocumentTemplateId.Value &&
                        d.AppliesToPersonAlias != null && personIds.Contains( d.AppliesToPersonAlias.PersonId ) )
                    .OrderByDescending( d => d.LastStatusDate )
                    .ToList();

            foreach ( var personId in personIds )
            {
                var document = documents.Find( d => d.AppliesToPersonAlias.PersonId == personId );
                if ( document != null )
                {
                    signatureDocuments[personId] = document;
                }
            }

            return signatureDocuments;
        }

        /// <summary>
        /// Add dynamically-generated controls to the form.
        /// </summary>
        /// <param name="setValues"></param>
        private void AddDynamicControls( bool setValues )
        {
            AddRegistrationTemplateFieldsToGrid( this.RegistrantFields, phRegistrantsRegistrantFormFieldFilters, gRegistrants, fRegistrants, setValues, false );

            var feeField = new RockLiteralField();
            feeField.ID = "lFees";
            feeField.HeaderText = "Fees";
            gRegistrants.Columns.Add( feeField );

            _placementsField = new RockLiteralField();
            _placementsField.ID = "lPlacements";
            _placementsField.HeaderText = "Placements";
            gRegistrants.Columns.Add( _placementsField );

            var deleteField = new DeleteField();
            gRegistrants.Columns.Add( deleteField );
            deleteField.Click += gRegistrants_Delete;
        }

        #endregion

        #region ISecondaryBlock

        /// <summary>
        /// Sets the visibility of the block.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlDetails.Visible = visible;
        }

        #endregion

        #region classes

        /// <summary>
        ///
        /// </summary>
        [System.Diagnostics.DebuggerDisplay( "{Group.Name}, RegistrationTemplatePlacementId = {RegistrationTemplatePlacementId} " )]
        protected class PlacementGroupInfo
        {
            /// <summary>
            /// Gets or sets the group.
            /// </summary>
            /// <value>
            /// The group.
            /// </value>
            public Group Group { get; set; }

            /// <summary>
            /// Gets or sets the registration template placement identifier.
            /// </summary>
            /// <value>
            /// The registration template placement identifier.
            /// </value>
            public int? RegistrationTemplatePlacementId { get; set; }

            /// <summary>
            /// Gets or sets the person ids.
            /// </summary>
            /// <value>
            /// The person ids.
            /// </value>
            public int[] PersonIds { get; set; }
        }

        #endregion classes
    }
}