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
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;
using System.Text.RegularExpressions;
using System.Data.Entity.SqlServer;
using Rock.Data;
using Rock.Web.Cache;
using System.Diagnostics;
using System.Data.Entity.Core.Objects;
using System.Text;

namespace RockWeb.Blocks.Crm
{
    [DisplayName( "Person Search" )]
    [Category( "CRM" )]
    [Description( "Displays list of people that match a given search type and term." )]

    #region Block Attributes

    [LinkedPage(
        "Person Detail Page",
        Key = AttributeKey.PersonDetailPage,
        Order = 0 )]

    [DefinedValueField(
        "Phone Number Types",
        Key = AttributeKey.PhoneNumberTypes,
        Description = "Types of phone numbers to include with person detail.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE,
        IsRequired = false,
        AllowMultiple = true,
        Order = 1 )]

    [BooleanField(
        "Show Birthdate",
        Key = AttributeKey.ShowBirthdate,
        Description = "Should a birthdate column be displayed?",
        DefaultBooleanValue = false,
        Order = 2 )]

    [BooleanField(
        "Show Age",
        Key = AttributeKey.ShowAge,
        Description = "Should an age column be displayed?",
        DefaultBooleanValue = true,
        Order = 3 )]

    [BooleanField(
        "Show Gender",
        Key = AttributeKey.ShowGender,
        Description = "Should a gender column be displayed?",
        DefaultBooleanValue = false,
        Order = 4 )]

    [BooleanField(
        "Show Spouse",
        Key = AttributeKey.ShowSpouse,
        Description = "Should a spouse column be displayed?",
        DefaultBooleanValue = false,
        Order = 5 )]

    [BooleanField(
        "Show Envelope Number",
        Key = AttributeKey.ShowEnvelopeNumber,
        Description = "Should an envelope # column be displayed?",
        DefaultBooleanValue = false,
        Order = 6 )]

    [BooleanField(
        "Show Performance",
        Key = AttributeKey.ShowPerformance,
        Description = "Displays how long the search took.",
        DefaultBooleanValue = false,
        Order = 7 )]

    [DataViewsField(
        "Highlight Indicators",
        Key = AttributeKey.DataViewIcons,
        Description = "Select one or more Data Views for Person search result icons. Note: More selections increase processing time.",
        EntityTypeName = "Rock.Model.Person",
        DisplayPersistedOnly = true,
        IsRequired = false,
        Order = 8 )]

    #endregion Block Attributes

    [Rock.SystemGuid.BlockTypeGuid( "764D3E67-2D01-437A-9F45-9F8C97878434" )]
    public partial class PersonSearch : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys
        private static class AttributeKey
        {
            public const string PersonDetailPage = "PersonDetailPage";
            public const string PhoneNumberTypes = "PhoneNumberTypes";
            public const string ShowBirthdate = "ShowBirthdate";
            public const string ShowAge = "ShowAge";
            public const string ShowGender = "ShowGender";
            public const string ShowSpouse = "ShowSpouse";
            public const string ShowEnvelopeNumber = "ShowEnvelopeNumber";
            public const string ShowPerformance = "ShowPerformance";
            public const string DataViewIcons = "DataViewIcons";
        }
        #endregion Attribute Keys

        #region Fields

        private List<Guid> _dataViewGuids = new List<Guid>();
        private List<Guid> _phoneTypeGuids = new List<Guid>();
        private bool _showSpouse = false;
        private DefinedValueCache _inactiveStatus = null;
        private Stopwatch _sw = new Stopwatch();
        private Literal _lPerf = new Literal();
        private Dictionary<int, string> _envelopeNumbers = null;

        #endregion

        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            _sw.Start();

            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/jquery.lazyload.min.js" );

            gPeople.DataKeyNames = new string[] { "Id" };
            gPeople.Actions.ShowAdd = false;
            gPeople.GridRebind += gPeople_GridRebind;
            gPeople.RowDataBound += gPeople_RowDataBound;
            gPeople.PersonIdField = "Id";

            if ( GetAttributeValue( AttributeKey.ShowPerformance ).AsBoolean() )
            {
                gPeople.Actions.AddCustomActionControl( _lPerf );
            }

            _dataViewGuids = GetAttributeValue( AttributeKey.DataViewIcons ).SplitDelimitedValues().AsGuidList();
            _phoneTypeGuids = GetAttributeValue( AttributeKey.PhoneNumberTypes ).SplitDelimitedValues().AsGuidList();
            _showSpouse = GetAttributeValue( AttributeKey.ShowSpouse ).AsBoolean();

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
            _sw.Stop();


            _lPerf.Text = string.Format( "<small class='pull-left' style='margin-top: 6px;'>Search time: {0} ms.</small>", _sw.Elapsed.Milliseconds );

        }

        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        void gPeople_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        void gPeople_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var person = e.Row.DataItem as PersonSearchResult;
                if ( person != null )
                {
                    if ( _inactiveStatus != null &&
                        person.RecordStatusValueId.HasValue &&
                        person.RecordStatusValueId.Value == _inactiveStatus.Id )
                    {
                        e.Row.AddCssClass( "inactive" );
                    }

                    if ( person.IsDeceased )
                    {
                        e.Row.AddCssClass( "deceased" );
                    }

                    string delimitedCampuses = string.Empty;
                    if ( person.CampusIds.Any() )
                    {
                        var campuses = new List<string>();
                        foreach ( var campusId in person.CampusIds )
                        {
                            var campus = CampusCache.Get( campusId );
                            if ( campus != null )
                            {
                                campuses.Add( campus.Name );
                            }
                        }
                        if ( campuses.Any() )
                        {
                            delimitedCampuses = campuses.AsDelimited( ", " );
                            var lCampus = e.Row.FindControl( "lCampus" ) as Literal;
                            if ( lCampus != null )
                            {
                                lCampus.Text = delimitedCampuses;
                            }
                        }
                    }

                    var lPerson = e.Row.FindControl( "lPerson" ) as Literal;

                    if ( !person.IsBusiness )
                    {
                        StringBuilder sbPersonDetails = new StringBuilder();
                        sbPersonDetails.Append( string.Format( "<div class=\"photo-round photo-round-sm pull-left\" data-original=\"{0}&w=100\" style=\"background-image: url('{1}');\"></div>", person.PhotoUrl, ResolveUrl( "~/Assets/Images/person-no-photo-unknown.svg" ) ) );
                        sbPersonDetails.Append( "<div class=\"pull-left margin-l-sm\">" );
                        sbPersonDetails.Append( string.Format( "<strong>{0}</strong> ", person.FullNameReversed ) );
                        sbPersonDetails.Append( string.Format( "{0} ", Person.GetSignalMarkup( person.TopSignalColor, person.TopSignalIconCssClass ) ) );
                        sbPersonDetails.Append( string.Format( "<small class=\"hidden-sm hidden-md hidden-lg\"><br>{0}<br></small>", delimitedCampuses ) );
                        sbPersonDetails.Append( string.Format( "<small class=\"hidden-sm hidden-md hidden-lg\">{0}</small>", DefinedValueCache.GetName( person.ConnectionStatusValueId ) ) );
                        sbPersonDetails.Append( string.Format( " <small class=\"hidden-md hidden-lg\">{0}</small>", person.AgeFormatted ) );

                        foreach ( Guid phGuid in _phoneTypeGuids )
                        {
                            var dv = DefinedValueCache.Get( phGuid );
                            if ( dv != null )
                            {
                                var pn = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == dv.Id );
                                if ( pn != null )
                                {
                                    sbPersonDetails.Append( string.Format( "<br/><small>{0}: {1}</small>", dv.Value.Left( 1 ).ToUpper(), pn.Number ) );
                                }
                            }
                        }

                        if ( !string.IsNullOrWhiteSpace( person.Email ) )
                        {
                            sbPersonDetails.Append( string.Format( "<br/><small>{0}</small>", person.Email ) );
                        }

                        // add home addresses
                        foreach ( var location in person.HomeAddresses )
                        {
                            if ( string.IsNullOrWhiteSpace( location.Street1 ) &&
                                string.IsNullOrWhiteSpace( location.Street2 ) &&
                                string.IsNullOrWhiteSpace( location.City ) )
                            {
                                continue;
                            }

                            string format = string.Empty;
                            var countryValue = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.LOCATION_COUNTRIES.AsGuid() )
                                .DefinedValues
                                .Where( v => v.Value.Equals( location.Country, StringComparison.OrdinalIgnoreCase ) )
                                .FirstOrDefault();

                            if ( countryValue != null )
                            {
                                format = countryValue.GetAttributeValue( "AddressFormat" );
                            }

                            if ( !string.IsNullOrWhiteSpace( format ) )
                            {
                                var dict = location.ToDictionary();
                                dict["Country"] = countryValue.Description;
                                sbPersonDetails.Append( string.Format( "<small><br>{0}</small>", format.ResolveMergeFields( dict ).ConvertCrLfToHtmlBr().Replace( "<br><br>", "<br>" ) ) );
                            }
                            else
                            {
                                sbPersonDetails.Append( string.Format( string.Format( "<small><br>{0}<br>{1} {2}, {3} {4}</small>", location.Street1, location.Street2, location.City, location.State, location.PostalCode ) ) );
                            }
                        }
                        sbPersonDetails.Append( "</div>" );

                        lPerson.Text = sbPersonDetails.ToString();

                        if ( _showSpouse )
                        {
                            using ( var rockContext = new RockContext() )
                            {
                                var personRec = new PersonService( rockContext ).Get( person.Id );
                                if ( personRec != null )
                                {
                                    var lSpouse = e.Row.FindControl( "lSpouse" ) as Literal;
                                    var spouse = personRec.GetSpouse( rockContext );
                                    if ( lSpouse != null && spouse != null )
                                    {
                                        lSpouse.Text = spouse.FullName;
                                    }
                                }
                            }
                        }

                        if ( _envelopeNumbers != null && _envelopeNumbers.ContainsKey( person.Id ) )
                        {
                            var lEnvelopeNumber = e.Row.FindControl( "lEnvelopeNumber" ) as Literal;
                            if ( lEnvelopeNumber != null )
                            {
                                lEnvelopeNumber.Text = _envelopeNumbers[person.Id];
                            }
                        }
                    }
                    else
                    {
                        lPerson.Text = string.Format( "{0}", person.LastName );
                    }

                    if ( _dataViewGuids.Any() && person.DataViewIcons.Any() )
                    {
                        var lIcons = e.Row.FindControl( "lIcons" ) as Literal;

                        var iconsHtml = new StringBuilder();

                        foreach ( var icon in person.DataViewIcons )
                        {
                            var tooltip = string.Format( "{0} meets the conditions of the {1} data view.", person.NickName, icon.DataViewName );

                            if ( !string.IsNullOrWhiteSpace( icon.IconCssClass ) )
                            {
                                iconsHtml.AppendLine( string.Format( "<i style=\"color:{0}\" class=\"fa-3x fa-fw {1}\" data-toggle=\"tooltip\" title=\"{2}\"></i>", icon.HighlightColor, icon.IconCssClass, tooltip ) );
                            }
                            else
                            {
                                // Add a placeholder so that all (potential) icons align.
                                iconsHtml.AppendLine( "<span style=\"display:block;\" class=\"fa-3x fa-fw\">&nbsp;</span>" );
                            }
                        }

                        lIcons.Text = iconsHtml.ToString();
                    }
                }
            }
        }

        protected void gPeople_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.PersonDetailPage, "PersonId", ( int ) e.RowKeyId );
        }

        #endregion

        #region Internal Methods

        private void BindGrid()
        {
            string type = PageParameter( "SearchType" );
            string term = PageParameter( "SearchTerm" );


            if ( !string.IsNullOrWhiteSpace( type ) && !string.IsNullOrWhiteSpace( term ) )
            {
                term = term.Trim();
                type = type.Trim();
                var rockContext = new RockContext();

                var personService = new PersonService( rockContext );
                IQueryable<Person> people = null;
                IEnumerable<int> personIdList;

                if ( !term.IsSingleSpecialCharacter() )
                {
                    switch ( type.ToLower() )
                    {
                        case ( "name" ):
                            {
                                bool allowFirstNameOnly = false;
                                if ( !bool.TryParse( PageParameter( "AllowFirstNameOnly" ), out allowFirstNameOnly ) )
                                {
                                    allowFirstNameOnly = false;
                                }
                                people = personService.GetByFullName( term, allowFirstNameOnly, true );
                                break;
                            }
                        case ( "phone" ):
                            {
                                var phoneService = new PhoneNumberService( rockContext );
                                var phoneNumberPersonIds = phoneService.GetPersonIdsByNumber( term );
                                people = personService.Queryable( new PersonService.PersonQueryOptions { IncludeNameless = true } ).Where( p => phoneNumberPersonIds.Contains( p.Id ) );
                                break;
                            }
                        case ( "address" ):
                            {
                                var groupMemberService = new GroupMemberService( rockContext );
                                var groupMemberPersonIds = groupMemberService.GetPersonIdsByHomeAddress( term );
                                people = personService.Queryable().Where( p => groupMemberPersonIds.Contains( p.Id ) );
                                break;
                            }
                        case ( "email" ):
                            {
                                var emailSearchTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_EMAIL.AsGuid() );
                                var searchKeyQry = new PersonSearchKeyService( rockContext ).Queryable();
                                people = personService.Queryable()
                                    .Where( p => term != "" && p.Email.Contains( term ) );
                                break;
                            }
                        case ( "birthdate" ):
                            {
                                DateTime? birthDate = Request.QueryString["birthdate"].AsDateTime();
                                int? personId = Request.QueryString["person-id"].AsIntegerOrNull();
                                if ( birthDate == null )
                                {
                                    birthDate = term.AsDateTime();
                                }

                                if ( personId.HasValue )
                                {
                                    people = personService.Queryable().Where( a => a.Id == personId.Value );
                                }
                                else
                                {
                                    people = personService.Queryable().Where( p => p.BirthDate.HasValue && birthDate.HasValue && p.BirthDate == birthDate.Value );
                                }

                                break;
                            }
                    }

                    if ( type.ToLower() == "email" )
                    {
                        /*
                          SK: 01/26/2023
                          We restructured this part to use this at this stage in order to fix the deadlock issue found in the Spark website.
                        */
                        var emailSearchTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_EMAIL.AsGuid() );
                        var searchKeyQry = new PersonSearchKeyService( rockContext )
                            .Queryable( "PersonAlias" )
                            .Where( a => emailSearchTypeValueId.HasValue
                                                     && a.PersonAliasId.HasValue
                                                     && a.SearchTypeValueId == emailSearchTypeValueId.Value
                                                     && a.SearchValue.Contains( term ) );
                        personIdList = people.Select( p => p.Id ).Concat( searchKeyQry.Select( a => a.PersonAlias.PersonId ) );
                        personIdList = personIdList.Distinct();
                    }
                    else
                    {
                        personIdList = people.Select( p => p.Id ).Distinct();
                    }
                }
                else
                {
                    personIdList = Enumerable.Empty<int>();
                }

                // just leave the personIdList as a Queryable if it is over 10000 so that we don't throw a SQL exception due to the big list of ids
                if ( personIdList.Count() < 10000 )
                {
                    personIdList = personIdList.ToList();
                }

                if ( personIdList.Count() == 1 )
                {
                    // if there is exactly one result, just redirect to the person page
                    int personId = personIdList.First();
                    Response.Redirect( string.Format( "~/Person/{0}", personId ), false );
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // since there is not exactly one person found, show the list of people in the grid

                var familyGroupType = GroupTypeCache.GetFamilyGroupType();
                int familyGroupTypeId = familyGroupType != null ? familyGroupType.Id : 0;

                var groupLocationTypeHome = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
                int homeAddressTypeId = groupLocationTypeHome != null ? groupLocationTypeHome.Id : 0;

                var birthDateCol = gPeople.ColumnsOfType<DateField>().First( c => c.DataField == "BirthDate" );
                var ageCol = gPeople.ColumnsOfType<RockBoundField>().First( c => c.DataField == "Age" );
                var genderCol = gPeople.ColumnsOfType<RockBoundField>().First( c => c.DataField == "Gender" );

                var envelopeNumberField = gPeople.ColumnsOfType<RockLiteralField>().First( c => c.ID == "lEnvelopeNumber" );
                var spouseCol = gPeople.ColumnsOfType<RockTemplateField>().First( c => c.HeaderText == "Spouse" );

                var personGivingEnvelopeAttribute = AttributeCache.Get( Rock.SystemGuid.Attribute.PERSON_GIVING_ENVELOPE_NUMBER.AsGuid() );
                if ( personGivingEnvelopeAttribute != null )
                {
                    envelopeNumberField.Visible = GlobalAttributesCache.Get().EnableGivingEnvelopeNumber && this.GetAttributeValue( AttributeKey.ShowEnvelopeNumber ).AsBoolean();
                }
                else
                {
                    envelopeNumberField.Visible = false;
                }

                birthDateCol.Visible = GetAttributeValue( AttributeKey.ShowBirthdate ).AsBoolean();
                ageCol.Visible = GetAttributeValue( AttributeKey.ShowAge ).AsBoolean();
                genderCol.Visible = GetAttributeValue( AttributeKey.ShowGender ).AsBoolean();
                spouseCol.Visible = _showSpouse;

                people = personService.Queryable( true ).Where( p => personIdList.Contains( p.Id ) );

                SortProperty sortProperty = gPeople.SortProperty;
                if ( sortProperty != null )
                {
                    people = people.Sort( sortProperty );
                }
                else
                {
                    people = people.OrderBy( p => p.LastName ).ThenBy( p => p.FirstName );
                }

                var personList = people.Select( p => new PersonSearchResult
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    NickName = p.NickName,
                    LastName = p.LastName,
                    BirthDate = p.BirthDate,
                    DeceasedDate = p.DeceasedDate,
                    BirthYear = p.BirthYear,
                    BirthMonth = p.BirthMonth,
                    BirthDay = p.BirthDay,
                    ConnectionStatusValueId = p.ConnectionStatusValueId,
                    RecordStatusValueId = p.RecordStatusValueId,
                    RecordTypeValueId = p.RecordTypeValueId,
                    AgeClassification = p.AgeClassification,
                    SuffixValueId = p.SuffixValueId,
                    IsDeceased = p.IsDeceased,
                    Email = p.Email,
                    Gender = p.Gender,
                    PhotoId = p.PhotoId,
                    CampusIds = p.Members
                        .Where( m =>
                            m.Group.GroupTypeId == familyGroupTypeId &&
                            m.Group.CampusId.HasValue )
                        .Select( m => m.Group.CampusId.Value )
                        .ToList(),
                    HomeAddresses = p.Members
                        .Where( m => m.Group.GroupTypeId == familyGroupTypeId )
                        .SelectMany( m => m.Group.GroupLocations )
                        .Where( gl => gl.GroupLocationTypeValueId == homeAddressTypeId )
                        .Select( gl => gl.Location ),
                    PhoneNumbers = p.PhoneNumbers
                        .Where( n => n.NumberTypeValueId.HasValue )
                        .Select( n => new PersonSearchResultPhone
                        {
                            NumberTypeValueId = n.NumberTypeValueId.Value,
                            Number = n.NumberFormatted,
                            PhoneTypeName = n.NumberTypeValue.Value
                        } )
                        .ToList(),
                    TopSignalColor = p.TopSignalColor,
                    TopSignalIconCssClass = p.TopSignalIconCssClass
                } ).ToList();

                if ( _dataViewGuids.Any() )
                {
                    var dataViewService = new DataViewService( rockContext );

                    // First get the full DataView so we can verify access (by calling .ToList()).
                    // Then select just the values necessary for the DataViewIconResult.
                    var dataViews = dataViewService.Queryable()
                        .Where( d => _dataViewGuids.Contains( d.Guid ) )
                        .ToList()
                        .Where( d => d.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) )
                        .Select( d => new DataViewIconResult
                        {
                            DataViewId = d.Id,
                            DataViewName = d.Name,
                            IconCssClass = d.IconCssClass,
                            HighlightColor = d.HighlightColor
                        } )
                        .ToList();

                    var icons = new List<DataViewIconResult>();

                    var dataViewIds = dataViews.Select( d => d.DataViewId ).ToList();

                    // Get the persisted values for the PersonIds and DataViewIds.
                    var persistedValues = dataViewService.GetDataViewPersistedValuesForIds( personIdList, dataViewIds );

                    // Because we want all icons to be in the same "column"
                    // if a person is missing an icon we need a placeholder to take up that space.
                    // Join all persons to all data views (e.g. CROSS APPLY)
                    // then GroupJoin w/ DefaultIfEmpty (e.g. LEFT JOIN) to the persisted values
                    // If the persisted value exists then add the icon class and color.
                    var leftJoinResult =
                        // Create a list of all persons with all dataviews.
                        personList.Join(
                        dataViews,
                        p => 1,
                        dv => 1,
                        ( p, dv ) => new
                        {
                            PersonId = p.Id,
                            DataViewId = dv.DataViewId,
                            DataViewName = dv.DataViewName,
                            IconCssClass = dv.IconCssClass,
                            HighlightColor = dv.HighlightColor
                        } )

                        // Now add the IconCssClass and HighlightColor only to those with a persisted value.
                        // Include the PersistedValue Total count for ordering (e.g. to remove gaps between icons when no results were in a DataView).
                        .GroupJoin(
                            persistedValues,
                            src => src.DataViewId,
                            pv => pv.DataViewId,
                            ( src, pv ) => new
                            {
                                PersonId = src.PersonId,
                                CountInDataView = pv.Count(),
                                Icon = new DataViewIconResult
                                {
                                    DataViewId = src.DataViewId,
                                    DataViewName = src.DataViewName,
                                    IconCssClass = pv.DefaultIfEmpty().Any( p => p?.DataViewId == src.DataViewId && p?.EntityId == src.PersonId ) ? src.IconCssClass : string.Empty,
                                    HighlightColor = pv.DefaultIfEmpty().Any( p => p?.DataViewId == src.DataViewId && p?.EntityId == src.PersonId ) ? src.HighlightColor : string.Empty
                                }
                            }
                         );

                    // Add the complete list of icons for every person.
                    foreach ( var person in personList )
                    {
                        person.DataViewIcons = leftJoinResult.Where( l => l.PersonId == person.Id ).OrderByDescending( i => i.CountInDataView ).Select( l => l.Icon ).ToList();
                    }
                }

                if ( type.ToLower() == "name" )
                {
                    var similarNames = personService.GetSimilarNames( term,
                        personList.Select( p => p.Id ).ToList(), true );
                    if ( similarNames.Any() )
                    {
                        var hyperlinks = new List<string>();
                        foreach ( string name in similarNames.Distinct() )
                        {
                            var pageRef = CurrentPageReference;
                            pageRef.Parameters["SearchTerm"] = name;
                            hyperlinks.Add( string.Format( "<a href='{0}'>{1}</a>", pageRef.BuildUrl(), name ) );
                        }
                        string altNames = string.Join( ", ", hyperlinks );
                        nbNotice.Text = string.Format( "Other Possible Matches: {0}", altNames );
                        nbNotice.Visible = true;
                    }
                }

                _inactiveStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
                var personIds = personList.Select( a => a.Id ).ToList();

                if ( envelopeNumberField != null && envelopeNumberField.Visible )
                {
                    _envelopeNumbers = new AttributeValueService( rockContext ).Queryable()
                                        .Where( a => a.AttributeId == personGivingEnvelopeAttribute.Id )
                                        .Where( a => personIds.Contains( a.EntityId.Value ) )
                                        .Select( a => new
                                        {
                                            PersonId = a.EntityId.Value,
                                            Value = a.Value
                                        } ).ToList().ToDictionary( k => k.PersonId, v => v.Value );
                }

                gPeople.EntityTypeId = EntityTypeCache.GetId<Person>();

                gPeople.DataSource = personList;
                gPeople.DataBind();
            }
        }

        #endregion

    }

    #region result models
    public class PersonSearchResult
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the full name last first.
        /// </summary>
        /// <value>
        /// The full name last first.
        /// </value>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the name of the nick.
        /// </summary>
        /// <value>
        /// The name of the nick.
        /// </value>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets the initials of the person.
        /// </summary>
        public string Initials
        {
            get
            {
                return $"{NickName.Truncate( 1, false )}{LastName.Truncate( 1, false )}";
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is business.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is business; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusiness
        {
            get
            {
                int recordTypeValueIdBusiness = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;
                return this.RecordTypeValueId.HasValue && this.RecordTypeValueId.Value == recordTypeValueIdBusiness;
            }
        }

        /// <summary>
        /// Gets or sets the home addresses.
        /// </summary>
        /// <value>
        /// The home addresses.
        /// </value>
        public IEnumerable<Location> HomeAddresses { get; set; }

        /// <summary>
        /// Gets the photo URL.
        /// </summary>
        /// <value>
        /// The photo URL.
        /// </value>
        public string PhotoUrl
        {
            get
            {
                return Person.GetPersonPhotoUrl( this.Initials, this.PhotoId, this.Age, this.Gender, this.RecordTypeValueId, this.AgeClassification );
            }
            private set { }
        }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>
        /// The last name.
        /// </value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets the full name reversed.
        /// </summary>
        /// <value>
        /// The full name reversed.
        /// </value>
        public virtual string FullNameReversed
        {
            get
            {
                if ( this.IsBusiness )
                {
                    return LastName;
                }

                var fullName = new StringBuilder();

                fullName.Append( LastName );

                // Use the SuffixValueId and DefinedValue cache instead of referencing SuffixValue property so
                // that if FullName is used in datagrid, the SuffixValue is not lazy-loaded for each row
                if ( SuffixValueId.HasValue )
                {
                    var suffix = DefinedValueCache.GetName( SuffixValueId.Value );
                    if ( suffix != null )
                    {
                        fullName.AppendFormat( " {0}", suffix );
                    }
                }

                fullName.AppendFormat( ", {0}", NickName );
                return fullName.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the birth date.
        /// </summary>
        /// <value>
        /// The birth date.
        /// </value>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Gets or sets the deceased date.
        /// </summary>
        /// <value>
        /// The deceased date.
        /// </value>
        public DateTime? DeceasedDate { get; set; }

        /// <summary>
        /// Gets or sets the birth year.
        /// </summary>
        /// <value>
        /// The birth year.
        /// </value>
        public int? BirthYear { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the photo identifier.
        /// </summary>
        /// <value>
        /// The photo identifier.
        /// </value>
        public int? PhotoId { get; set; }

        /// <summary>
        /// Gets or sets the birth month.
        /// </summary>
        /// <value>
        /// The birth month.
        /// </value>
        public int? BirthMonth { get; set; }

        /// <summary>
        /// Gets or sets the birth day.
        /// </summary>
        /// <value>
        /// The birth day.
        /// </value>
        public int? BirthDay { get; set; }

        /// <summary>
        /// Gets or sets the families.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public List<int> CampusIds { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        /// <value>The gender.</value>
        public Gender Gender { get; set; }

        /// <summary>
        /// Gets or sets the is deceased.
        /// </summary>
        /// <value>
        /// The is deceased.
        /// </value>
        public bool IsDeceased { get; set; }

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <value>
        /// The age.
        /// </value>
        public int? Age
        {
            get
            {
                return Person.GetAge( BirthDate, DeceasedDate );
            }

            private set { }
        }

        /// <summary>
        /// Gets the age formatted.
        /// </summary>
        /// <value>
        /// The age formatted.
        /// </value>
        public string AgeFormatted
        {
            get
            {
                if ( this.Age.HasValue )
                {
                    return string.Format( "({0})", this.Age.Value.ToString() );
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the connection status.
        /// </summary>
        /// <value>The connection status.</value>
        public int? ConnectionStatusValueId { get; set; }

        /// <summary>
        /// Gets or sets the record type value.
        /// </summary>
        /// <value>
        /// The record type value.
        /// </value>
        public int? RecordTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets the suffix value.
        /// </summary>
        /// <value>
        /// The suffix value.
        /// </value>
        public int? SuffixValueId { get; set; }

        /// <summary>
        /// Gets or sets the record status.
        /// </summary>
        /// <value>The member status.</value>
        public int? RecordStatusValueId { get; set; }

        /// <summary>
        /// Gets the age classification.
        /// </summary>
        /// <value>
        /// The age classification.
        /// </value>
        public AgeClassification AgeClassification { get; set; }

        /// <summary>
        /// Gets or sets the name of the spouse.
        /// </summary>
        /// <value>
        /// The name of the spouse.
        /// </value>
        public string SpouseName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the picker item details HTML.
        /// </summary>
        /// <value>
        /// The picker item details HTML.
        /// </value>
        public string PickerItemDetailsHtml { get; set; }

        /// <summary>
        /// Gets or sets the phone numbers.
        /// </summary>
        /// <value>
        /// The phone numbers.
        /// </value>
        public List<PersonSearchResultPhone> PhoneNumbers { get; set; }

        /// <summary>
        /// Gets or sets the top signal color to indicate if this person has a signal attached.
        /// </summary>
        /// <value>
        /// The top signal color.
        /// </value>
        public string TopSignalColor { get; set; }

        /// <summary>
        /// Gets or sets the top signal icon of the person.
        /// </summary>
        /// <value>
        /// The top signal icon.
        /// </value>
        public string TopSignalIconCssClass { get; set; }

        /// <summary>
        /// Gets or sets the complete list of persisted DataViews where there's
        /// an IconCssClass and this Person is one of the records returned by the DataView.
        /// </summary>
        public List<DataViewIconResult> DataViewIcons { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class PersonSearchResultPhone
    {
        /// <summary>
        /// Gets or sets the number type value identifier.
        /// </summary>
        /// <value>
        /// The number type value identifier.
        /// </value>
        public int NumberTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>
        /// The number.
        /// </value>
        public string Number { get; set; }

        /// <summary>
        /// The phone type name (Mobile, Home, etc.)
        /// </summary>
        public string PhoneTypeName { get; set; }

        /// <summary>
        /// Provides a reasonable string representation of a phone number.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format( "{0} ({1})", Number, PhoneTypeName );
        }
    }

    /// <summary>
    /// Minimal icon information for a persisted DataView which defines an IconCssClass and (optionally) HighlightColor.
    /// </summary>
    public class DataViewIconResult
    {
        /// <summary>
        /// Gets or sets the IconCssClass defined by DataView.
        /// </summary>
        public string IconCssClass { get; set; }

        /// <summary>
        /// Gets or sets the Highlight color to be used by the Icon as defined by the DataView.
        /// </summary>
        public string HighlightColor { get; set; }

        /// <summary>
        /// Gets or sets the Id of the DataView where the Icon and Highlight Color come from.
        /// </summary>
        public int DataViewId { get; set; }

        /// <summary>
        /// Gets or sets the name of the DataView where the Icon and Highlight Color come from.
        /// </summary>
        public string DataViewName { get; set; }

        public override string ToString()
        {
            return string.Format( IconCssClass.Length > 0 ? IconCssClass : string.Empty );
        }
    }
    #endregion

}