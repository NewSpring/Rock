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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Security;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Groups
{
    [DisplayName( "Group Attendance List" )]
    [Category( "Groups" )]
    [Description( "Lists all the scheduled occurrences for a given group." )]

    [LinkedPage( "Detail Page",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.DetailPage )]
    [BooleanField( "Allow Add",
        Description = "Should block support adding new attendance dates outside of the group's configured schedule and group type's exclusion dates?",
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKey.AllowAdd )]
    [BooleanField( "Allow Campus Filter",
        Description = "Should block add an option to allow filtering attendance counts and percentage by campus?",
        DefaultBooleanValue = false,
        Order = 2,
        Key = AttributeKey.AllowCampusFilter )]
    [BooleanField( "Display Notes",
        Description = "Should the Notes column be displayed?",
        DefaultBooleanValue = true,
        Order = 3,
        Key = AttributeKey.DisplayNotes)]
    [BooleanField( "Display Attendance Type",
        Description = "Should the Attendance Type column be displayed?",
        DefaultBooleanValue = true,
        Order = 4,
        Key = AttributeKey.DisplayAttendanceType )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.GROUP_ATTENDANCE_LIST )]
    public partial class GroupAttendanceList : RockBlock, ICustomGridColumns
    {
        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string AllowAdd = "AllowAdd";
            public const string AllowCampusFilter = "AllowCampusFilter";
            public const string DisplayNotes = "DisplayNotes";
            public const string DisplayAttendanceType = "DisplayAttendanceType";
        }

        #region Private Variables

        private RockContext _rockContext = null;
        private Group _group = null;
        private bool _canView = false;
        private bool _allowCampusFilter = false;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _rockContext = new RockContext();

            var allowPredictableIds = !this.PageCache.Layout.Site.DisablePredictableIds;
            var groupKey = PageParameter( "GroupId" );

            int groupId = groupKey.AsInteger() > 0 && allowPredictableIds ? groupKey.ToIntSafe() : IdHasher.Instance.GetId( groupKey ).ToIntSafe();

            _group = new GroupService( _rockContext )
                .Queryable( "GroupLocations" ).AsNoTracking()
                .FirstOrDefault( g => g.Id == groupId );

            if ( _group != null && _group.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
            {
                _group.LoadAttributes( _rockContext );
                _canView = true;

                rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
                gOccurrences.DataKeyNames = new string[] { "Id", "OccurrenceDate", "ScheduleId", "LocationId" };
                gOccurrences.Actions.AddClick += gOccurrences_Add;
                gOccurrences.GridRebind += gOccurrences_GridRebind;
                gOccurrences.RowDataBound += gOccurrences_RowDataBound;

                // make sure they have Auth to edit the block OR edit to the Group
                bool canEditBlock = IsUserAuthorized( Authorization.EDIT ) || _group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || _group.IsAuthorized( Authorization.TAKE_ATTENDANCE, this.CurrentPerson );
                gOccurrences.Actions.ShowAdd = canEditBlock && GetAttributeValue( AttributeKey.AllowAdd ).AsBoolean();
                gOccurrences.IsDeleteEnabled = canEditBlock;

                var notesColumn = gOccurrences.Columns.OfType<BoundField>().Where( a => a.DataField == "Notes" ).FirstOrDefault();
                if ( notesColumn != null )
                {
                    notesColumn.Visible = GetAttributeValue( AttributeKey.DisplayNotes ).AsBoolean();
                }

                var attendanceTypeColumn = gOccurrences.Columns.OfType<BoundField>().Where( a => a.DataField == "AttendanceTypeValueId" ).FirstOrDefault();
                if ( attendanceTypeColumn != null )
                {
                    attendanceTypeColumn.Visible = GetAttributeValue( AttributeKey.DisplayAttendanceType ).AsBoolean();
                }
            }

            _allowCampusFilter = GetAttributeValue( AttributeKey.AllowCampusFilter ).AsBoolean();
            bddlCampus.Visible = _allowCampusFilter;
            if ( _allowCampusFilter )
            {
                bddlCampus.DataSource = CampusCache.All();
                bddlCampus.DataBind();
                bddlCampus.Items.Insert( 0, new ListItem( "All Campuses", "0" ) );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            pnlContent.Visible = _canView;

            if ( !Page.IsPostBack && _canView )
            {
                if ( _allowCampusFilter )
                {
                    var preferences = GetBlockPersonPreferences();
                    var campus = CampusCache.Get( preferences.GetValue( "Campus" ).AsInteger() );
                    if ( campus != null )
                    {
                        bddlCampus.Title = campus.Name;
                        bddlCampus.SetValue( campus.Id );
                    }
                }
                BindFilter();
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the SelectionChanged event of the bddlCampus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bddlCampus_SelectionChanged( object sender, EventArgs e )
        {
            var preferences = GetBlockPersonPreferences();

            preferences.SetValue( "Campus", bddlCampus.SelectedValue );
            preferences.Save();

            var campus = CampusCache.Get( bddlCampus.SelectedValueAsInt() ?? 0 );
            bddlCampus.Title = campus != null ? campus.Name : "All Campuses";
            BindGrid();
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SetFilterPreference( MakeKeyUniqueToGroup( "Date Range" ), "Date Range", drpDates.DelimitedValues );
            rFilter.SetFilterPreference( MakeKeyUniqueToGroup( "Schedule" ), "Schedule", ddlSchedule.SelectedValue );
            rFilter.SetFilterPreference( MakeKeyUniqueToGroup( "Location" ), "Location", ddlLocation.SelectedValue );

            BindGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rFilter_ClearFilterClick( object sender, EventArgs e )
        {
            rFilter.DeleteFilterPreferences();
            BindFilter();
        }

        /// <summary>
        /// Rs the filter_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( e.Key == MakeKeyUniqueToGroup( "Date Range" ) )
            {
                // show the date range pickers current value, instead of the user preference since we set it to a default value if blank
                e.Value = DateRangePicker.FormatDelimitedValues( drpDates.DelimitedValues );
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Schedule" ) )
            {
                int scheduleId = e.Value.AsInteger();
                if ( scheduleId != 0 )
                {
                    var schedule = new ScheduleService( _rockContext ).Get( scheduleId );
                    e.Value = schedule != null ? schedule.Name : string.Empty;
                }
                else
                {
                    e.Value = string.Empty;
                }
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Location" ) )
            {
                string locId = e.Value;
                if ( locId.StartsWith( "P" ) )
                {
                    locId = locId.Substring( 1 );
                }

                int locationId = locId.AsInteger();
                e.Value = new LocationService( _rockContext ).GetPath( locationId );
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gOccurrences_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var occurrence = e.Row.DataItem as AttendanceListOccurrence;
                if ( occurrence == null || occurrence.Id == 0 )
                {
                    var deleteField = gOccurrences.Columns.OfType<DeleteField>().First();
                    var cell = e.Row.Cells[gOccurrences.GetColumnIndex( deleteField )];
                    var lb = cell.ControlsOfTypeRecursive<LinkButton>().FirstOrDefault();
                    if ( lb != null )
                    {
                        lb.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Edit event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gOccurrences_Edit( object sender, RowEventArgs e )
        {
            var qryParams = PageParameters()?.AsEnumerable()
                .ToDictionary( ( kv ) => kv.Key, ( kv ) => kv.Value.ToStringSafe() );
            qryParams.AddOrReplace( "GroupId", _group.Id.ToString() );
            qryParams.AddOrReplace( "ReturnUrl", Request.RawUrl );

            int? id = e.RowKeyValues["Id"].ToString().AsIntegerOrNull();
            if ( id.HasValue )
            {
                qryParams.AddOrReplace( "OccurrenceId", id.Value.ToString() );
            }

            if ( !id.HasValue || id.Value == 0 )
            {
                string occurrenceDate = ( ( DateTime ) e.RowKeyValues["OccurrenceDate"] ).ToString( "yyyy-MM-ddTHH:mm:ss" );
                qryParams.AddOrReplace( "Date", occurrenceDate );

                var locationId = e.RowKeyValues["LocationId"] as int?;
                if ( locationId.HasValue )
                {
                    qryParams.AddOrReplace( "LocationId", locationId.Value.ToString() );
                }

                var scheduleId = e.RowKeyValues["ScheduleId"] as int?;
                if ( scheduleId.HasValue )
                {
                    qryParams.AddOrReplace( "ScheduleId", scheduleId.Value.ToString() );
                }

                var groupTypeIds = PageParameter( "GroupTypeIds" );
                if ( !string.IsNullOrWhiteSpace( groupTypeIds ) )
                {
                    qryParams.AddOrReplace( "GroupTypeIds", groupTypeIds );
                }
            }

            NavigateToLinkedPage( AttributeKey.DetailPage, qryParams );
        }

        /// <summary>
        /// Handles the Add event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gOccurrences_Add( object sender, EventArgs e )
        {
            var qryParams = PageParameters()?.AsEnumerable()
                .ToDictionary( ( kv ) => kv.Key, ( kv ) => kv.Value.ToStringSafe() );
            qryParams.AddOrReplace( "GroupId", _group.Id.ToString() );
            qryParams.AddOrReplace( "ReturnUrl", Request.RawUrl );

            if ( ddlSchedule.Visible && ddlSchedule.SelectedValue != "0" )
            {
                qryParams.AddOrReplace( "ScheduleId", ddlSchedule.SelectedValue );
            }

            if ( ddlLocation.Visible )
            {
                int? locId = ddlLocation.SelectedValueAsInt();
                if ( locId.HasValue && locId.Value != 0 )
                {
                    qryParams.AddOrReplace( "LocationId", locId.Value.ToString() );
                }
            }

            var groupTypeIds = PageParameter( "GroupTypeIds" );
            if ( !string.IsNullOrWhiteSpace( groupTypeIds ) )
            {
                qryParams.AddOrReplace( "GroupTypeIds", groupTypeIds );
            }

            NavigateToLinkedPage( AttributeKey.DetailPage, qryParams );
        }

        /// <summary>
        /// Handles the Delete event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gOccurrences_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            var occurrenceService = new AttendanceOccurrenceService( rockContext );
            var attendanceService = new AttendanceService( rockContext );
            var occurrence = occurrenceService.Get( e.RowKeyId );

            if ( occurrence != null )
            {
                var locationId = occurrence.LocationId;

                string errorMessage;
                if ( !occurrenceService.CanDelete( occurrence, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Alert );
                    return;
                }

                // Delete the attendees for this occurrence since it is not a cascading delete
                var attendees = attendanceService.Queryable().Where( a => a.OccurrenceId == occurrence.Id );
                rockContext.BulkDelete<Attendance>( attendees );

                occurrenceService.Delete( occurrence );
                rockContext.SaveChanges();

                if ( locationId.HasValue )
                {
                    Rock.CheckIn.KioskLocationAttendance.Remove( locationId.Value );
                }
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gOccurrences_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        protected void BindFilter()
        {
            string dateRangePreference = rFilter.GetFilterPreference( MakeKeyUniqueToGroup( "Date Range" ) );
            if ( string.IsNullOrWhiteSpace( dateRangePreference ) )
            {
                // set the dateRangePreference to force rFilter_DisplayFilterValue to show our default three month limit
                dateRangePreference = ",";
                rFilter.SetFilterPreference( MakeKeyUniqueToGroup( "Date Range" ), "Date Range", dateRangePreference );
            }

            var dateRange = DateRangePicker.CalculateDateRangeFromDelimitedValues( dateRangePreference );

            // if there is no start date, default to three months ago to minimize the chance of loading too much data
            drpDates.LowerValue = dateRange.Start ?? RockDateTime.Today.AddMonths( -3 );
            drpDates.UpperValue = dateRange.End;

            if ( _group != null )
            {
                var grouplocations = _group.GroupLocations
                    .Where( l =>
                        l.Location.Name != null &&
                        l.Location.Name != string.Empty )
                    .ToList();

                var locations = new Dictionary<string, string> { { string.Empty, string.Empty } };

                var locationService = new LocationService( _rockContext );
                foreach ( var location in grouplocations.Select( l => l.Location ) )
                {
                    if ( !locations.ContainsKey( location.Id.ToString() ) )
                    {
                        locations.Add( location.Id.ToString(), locationService.GetPath( location.Id ) );
                    }

                    var parentLocation = location.ParentLocation;
                    while ( parentLocation != null )
                    {
                        string key = string.Format( "P{0}", parentLocation.Id );
                        if ( !locations.ContainsKey( key ) )
                        {
                            locations.Add( key, locationService.GetPath( parentLocation.Id ) );
                        }

                        parentLocation = parentLocation.ParentLocation;
                    }
                }

                var scheduleField = gOccurrences.ColumnsOfType<RockBoundField>().FirstOrDefault( a => a.DataField == "ScheduleName" );
                if ( locations.Any() )
                {
                    ddlLocation.Visible = true;
                    if ( scheduleField != null )
                    {
                        scheduleField.Visible = true;
                    }
                    ddlLocation.DataSource = locations.OrderBy( l => l.Value );
                    ddlLocation.DataBind();
                    ddlLocation.SetValue( rFilter.GetFilterPreference( MakeKeyUniqueToGroup( "Location" ) ) );
                }
                else
                {
                    ddlLocation.Visible = false;
                    if ( scheduleField != null )
                    {
                        scheduleField.Visible = false;
                    }
                }

                var schedules = new Dictionary<int, string> { { 0, string.Empty } };
                grouplocations.SelectMany( l => l.Schedules ).OrderBy( s => s.Name ).ToList()
                    .ForEach( s => schedules.TryAdd( s.Id, s.Name ) );
                var locationField = gOccurrences.ColumnsOfType<RockTemplateField>().FirstOrDefault( a => a.HeaderText == "Location" );
                if ( schedules.Any() )
                {
                    ddlSchedule.Visible = true;
                    if ( locationField != null )
                    {
                        locationField.Visible = true;
                    }
                    ddlSchedule.DataSource = schedules;
                    ddlSchedule.DataBind();
                    ddlSchedule.SetValue( rFilter.GetFilterPreference( MakeKeyUniqueToGroup( "Schedule" ) ) );
                }
                else
                {
                    ddlSchedule.Visible = false;
                    if ( locationField != null )
                    {
                        locationField.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindGrid()
        {
            if ( _group != null )
            {
                lHeading.Text = _group.Name;

                DateTime? fromDateTime = drpDates.LowerValue;
                DateTime? toDateTime = drpDates.UpperValue;
                List<int> locationIds = new List<int>();
                List<int> scheduleIds = new List<int>();

                // Location Filter
                if ( ddlLocation.Visible )
                {
                    string locValue = ddlLocation.SelectedValue;
                    if ( locValue.StartsWith( "P" ) )
                    {
                        int? parentLocationId = locValue.Substring( 1 ).AsIntegerOrNull();
                        if ( parentLocationId.HasValue )
                        {
                            locationIds = new LocationService( _rockContext )
                                .GetAllDescendents( parentLocationId.Value )
                                .Select( l => l.Id )
                                .ToList();
                        }
                    }
                    else
                    {
                        int? locationId = locValue.AsIntegerOrNull();
                        if ( locationId.HasValue )
                        {
                            locationIds.Add( locationId.Value );
                        }
                    }
                }

                // Schedule Filter
                if ( ddlSchedule.Visible && ddlSchedule.SelectedValue != "0" )
                {
                    scheduleIds.Add( ddlSchedule.SelectedValueAsInt() ?? 0 );
                }

                // Get all the occurrences for this group for the selected dates, location and schedule
                var occurrences = new AttendanceOccurrenceService( _rockContext )
                    .GetGroupOccurrences( _group, fromDateTime, toDateTime, locationIds, scheduleIds )
                    .Select( o => new AttendanceListOccurrence( o ) )
                    .ToList();

                var locationService = new LocationService( _rockContext );

                // Check to see if filtering by campus, and if so, only select those occurrences who's 
                // location is associated with that campus (or not associated with any campus)
                int? campusId = bddlCampus.SelectedValueAsInt();
                if ( campusId.HasValue )
                {
                    // If campus filter is selected, load all the descendant locations for each campus into a dictionary
                    var locCampus = new Dictionary<int, int>();
                    foreach ( var campus in CampusCache.All().Where( c => c.LocationId.HasValue ) )
                    {
                        locCampus.TryAdd( campus.LocationId.Value, campus.Id );
                        foreach ( var locId in locationService.GetAllDescendentIds( campus.LocationId.Value ) )
                        {
                            locCampus.TryAdd( locId, campus.Id );
                        }
                    }

                    // Using that dictionary, set the campus id for each occurrence
                    foreach ( var occ in occurrences )
                    {
                        if ( occ.LocationId.HasValue && locCampus.ContainsKey( occ.LocationId.Value ) )
                        {
                            occ.CampusId = locCampus[occ.LocationId.Value];
                        }
                    }

                    // Remove the occurrences that are associated with another campus
                    occurrences = occurrences
                        .Where( o =>
                            !o.CampusId.HasValue ||
                            o.CampusId.Value == campusId.Value )
                        .ToList();
                }

                // Update the Parent Location path 
                foreach ( int parentLocationId in occurrences
                    .Where( o => o.ParentLocationId.HasValue )
                    .Select( o => o.ParentLocationId.Value )
                    .Distinct() )
                {
                    string parentLocationPath = locationService.GetPath( parentLocationId );
                    foreach ( var occ in occurrences
                        .Where( o =>
                            o.ParentLocationId.HasValue &&
                            o.ParentLocationId.Value == parentLocationId ) )
                    {
                        occ.ParentLocationPath = parentLocationPath;
                    }
                }

                // Sort the occurrences
                SortProperty sortProperty = gOccurrences.SortProperty;
                List<AttendanceListOccurrence> sortedOccurrences = null;
                if ( sortProperty != null )
                {
                    if ( sortProperty.Property == "LocationPath,LocationName" )
                    {
                        if ( sortProperty.Direction == SortDirection.Ascending )
                        {
                            sortedOccurrences = occurrences.OrderBy( o => o.ParentLocationPath ).ThenBy( o => o.LocationName ).ToList();
                        }
                        else
                        {
                            sortedOccurrences = occurrences.OrderByDescending( o => o.ParentLocationPath ).ThenByDescending( o => o.LocationName ).ToList();
                        }
                    }
                    else
                    {
                        sortedOccurrences = occurrences.AsQueryable().Sort( sortProperty ).ToList();
                    }
                }
                else
                {
                    sortedOccurrences = occurrences.OrderByDescending( a => a.OccurrenceDate ).ThenByDescending( a => a.StartTime ).ToList();
                }

                gOccurrences.DataSource = sortedOccurrences;
                gOccurrences.DataBind();
            }
        }

        /// <summary>
        /// Makes the key unique to group.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private string MakeKeyUniqueToGroup( string key )
        {
            if ( _group != null )
            {
                return string.Format( "{0}-{1}", _group.Id, key );
            }

            return key;
        }

        #endregion
    }

    public class AttendanceListOccurrence : LavaDataObject
    {
        public int Id { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? ParentLocationId { get; set; }
        public string ParentLocationPath { get; set; }
        public int? ScheduleId { get; set; }
        public string ScheduleName { get; set; }
        public TimeSpan StartTime { get; set; }
        public int? CampusId { get; set; }
        public bool AttendanceEntered { get; set; }
        public bool DidNotOccur { get; set; }
        public int DidAttendCount { get; set; }
        public double AttendanceRate { get; set; }
        public bool CanDelete { get; set; }
        public string Notes { get; set; }
        public int? AttendanceTypeValueId { get; set; }

        public AttendanceListOccurrence( AttendanceOccurrence occurrence )
        {
            Id = occurrence.Id;
            OccurrenceDate = occurrence.OccurrenceDate;
            LocationId = occurrence.LocationId;

            if ( occurrence.Location != null )
            {
                LocationName = occurrence.Location.ToString( true );
            }

            LocationName = occurrence.Location != null ? occurrence.Location.Name : string.Empty;
            ParentLocationId = occurrence.Location != null ? occurrence.Location.ParentLocationId : ( int? ) null;
            ScheduleId = occurrence.ScheduleId;

            if ( occurrence.Schedule != null )
            {
                if ( occurrence.Schedule.Name.IsNotNullOrWhiteSpace() )
                {
                    ScheduleName = occurrence.Schedule.Name.EncodeHtml();
                }
                else
                {
                    ScheduleName = occurrence.Schedule.ToString();
                }
            }

            StartTime = occurrence.Schedule != null ? occurrence.Schedule.StartTimeOfDay : new TimeSpan();
            AttendanceEntered = occurrence.AttendanceEntered;
            DidNotOccur = occurrence.DidNotOccur ?? false;
            DidAttendCount = occurrence.DidAttendCount;
            AttendanceRate = occurrence.AttendanceRate;
            Notes = occurrence.Notes;
            AttendanceTypeValueId = occurrence.AttendanceTypeValueId;
        }
    }


}