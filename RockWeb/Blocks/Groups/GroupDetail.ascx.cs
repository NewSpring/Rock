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
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Humanizer;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Communication.Chat;
using Rock.Constants;
using Rock.Data;
using Rock.Enums.Communication.Chat;
using Rock.Enums.Group;
using Rock.Model;
using Rock.Model.Groups.Group.Options;
using Rock.Security;
using Rock.Utility;
using Rock.Utility.Enums;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using Attribute = Rock.Model.Attribute;

namespace RockWeb.Blocks.Groups
{
    [DisplayName( "Group Detail" )]
    [Category( "Groups" )]
    [Description( "Displays the details of the given group." )]
    [ContextAware( typeof( Group ) )]

    #region Block Attributes

    [GroupTypesField( "Group Types Include",
        Key = AttributeKey.GroupTypesInclude,
        Description = "Select group types to show in this block.  Leave all unchecked to show all but the excluded group types.",
        IsRequired = false,
        Order = 0 )]

    [GroupTypesField( "Group Types Exclude",
        Key = AttributeKey.GroupTypesExclude,
        Description = "Select group types to exclude from this block.",
        IsRequired = false,
        Order = 1 )]

    [BooleanField( "Limit to Security Role Groups",
        Key = AttributeKey.LimittoSecurityRoleGroups,
        DefaultBooleanValue = false,
        Order = 2 )]

    [BooleanField( "Limit to Group Types that are shown in navigation",
        Key = AttributeKey.LimitToShowInNavigationGroupTypes,
        DefaultBooleanValue = false,
        Order = 3 )]

    [DefinedValueField( "Map Style",
        Key = AttributeKey.MapStyle,
        Description = "The style of maps to use.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.MAP_STYLES,
        IsRequired = false,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK,
        Order = 4 )]

    [LinkedPage( "Group Map Page",
        Key = AttributeKey.GroupMapPage,
        Description = "The page to display detailed group map.",
        IsRequired = false,
        Order = 5 )]

    [LinkedPage( "Attendance Page",
        Key = AttributeKey.AttendancePage,
        Description = "The page to display attendance list.",
        IsRequired = false,
        Order = 6 )]

    [LinkedPage( "Registration Instance Page",
        Key = AttributeKey.RegistrationInstancePage,
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 7 )]

    [LinkedPage( "Event Item Occurrence Page",
        Key = AttributeKey.EventItemOccurrencePage,
        Description = "The page to display event item occurrence details.",
        IsRequired = false,
        Order = 8 )]

    [LinkedPage( "Content Item Page",
        Key = AttributeKey.ContentItemPage,
        Description = "The page to display registration details.",
        IsRequired = false,
        Order = 9 )]

    [BooleanField( "Show Copy Button",
        Key = AttributeKey.ShowCopyButton,
        Description = "Copies the group and all of its associated authorization rules",
        DefaultBooleanValue = false,
        Order = 10 )]

    [LinkedPage( "Group List Page",
        Key = AttributeKey.GroupListPage,
        Description = "The page to display related Group List.",
        IsRequired = false,
        Order = 11 )]

    [LinkedPage( "Fundraising Progress Page",
        Key = AttributeKey.FundraisingProgressPage,
        Description = "The page to display fundraising progress for all its members.",
        IsRequired = false,
        Order = 12 )]

    [BooleanField( "Show Location Addresses",
        Key = AttributeKey.ShowLocationAddresses,
        Description = "Determines if the location address should be shown when viewing the group details.",
        DefaultBooleanValue = true,
        Order = 13 )]

    [BooleanField( "Prevent Selecting Inactive Campus",
        Key = AttributeKey.PreventSelectingInactiveCampus,
        Description = "Should inactive campuses be excluded from the campus field when editing a group?.",
        DefaultBooleanValue = false,
        Order = 14 )]

    [LinkedPage( "Group History Page",
        Key = AttributeKey.GroupHistoryPage,
        Description = "The page to display group history.",
        IsRequired = false,
        Order = 15 )]

    [LinkedPage( "Group Scheduler Page",
        Key = AttributeKey.GroupSchedulerPage,
        Description = "The page to schedule this group.",
        IsRequired = false,
        DefaultValue = "1815D8C6-7C4A-4C05-A810-CF23BA937477,D0F198E2-6111-4EC1-8D1D-55AC10E28D04",
        Order = 16 )]

    [LinkedPage( "Group RSVP List Page",
        Key = AttributeKey.GroupRSVPPage,
        Description = "The page to manage RSVPs for this group.",
        IsRequired = false,
        DefaultValue = Rock.SystemGuid.Page.GROUP_RSVP_LIST,
        Order = 17 )]

    [BooleanField( "Enable Group Tags",
        Key = AttributeKey.EnableGroupTags,
        Description = "If enabled, the tags will be shown.",
        DefaultBooleanValue = true,
        Order = 18 )]

    [BooleanField( "Add Administrate Security to Group Creator",
        Key = AttributeKey.AddAdministrateSecurityToGroupCreator,
        Description = "If enabled, the person who creates a new group will be granted 'Administrate' security rights to the group.  This was the behavior in previous versions of Rock.  If disabled, the group creator will not be able to edit security or possibly perform other functions without the Rock administrator settings up a role that is allowed to perform such functions.",
        DefaultBooleanValue = false,
        Order = 19 )]

    #endregion Block Attributes

    [Rock.SystemGuid.BlockTypeGuid( "582BEEA1-5B27-444D-BC0A-F60CEB053981" )]
    public partial class GroupDetail : ContextEntityBlock
    {
        #region Attribute Keys

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string ExpandedIds = "ExpandedIds";
            public const string EventItemOccurrenceId = "EventItemOccurrenceId";
            public const string ParentGroupId = "ParentGroupId";
        }

        private static class AttributeKey
        {
            public const string GroupTypesInclude = "GroupTypes";
            public const string GroupTypesExclude = "GroupTypesExclude";
            public const string LimittoSecurityRoleGroups = "LimittoSecurityRoleGroups";
            public const string LimitToShowInNavigationGroupTypes = "LimitToShowInNavigationGroupTypes";
            public const string MapStyle = "MapStyle";
            public const string GroupMapPage = "GroupMapPage";
            public const string AttendancePage = "AttendancePage";
            public const string RegistrationInstancePage = "RegistrationInstancePage";
            public const string EventItemOccurrencePage = "EventItemOccurrencePage";
            public const string ContentItemPage = "ContentItemPage";
            public const string ShowCopyButton = "ShowCopyButton";
            public const string GroupListPage = "GroupListPage";
            public const string FundraisingProgressPage = "FundraisingProgressPage";
            public const string ShowLocationAddresses = "ShowLocationAddresses";
            public const string PreventSelectingInactiveCampus = "PreventSelectingInactiveCampus";
            public const string GroupHistoryPage = "GroupHistoryPage";
            public const string GroupSchedulerPage = "GroupSchedulerPage";
            public const string GroupRSVPPage = "GroupRSVPPage";
            public const string EnableGroupTags = "EnableGroupTags";
            public const string AddAdministrateSecurityToGroupCreator = "AddAdministrateSecurityToGroupCreator";
            public const string IsScheduleTabVisible = "IsScheduleTabVisible";
        }

        #endregion Attribute Keys

        #region Constants

        private const string MEMBER_LOCATION_TAB_TITLE = "Member Location";
        private const string OTHER_LOCATION_TAB_TITLE = "Other Location";

        #endregion

        #region Fields

        private readonly List<string> _tabs = new List<string> { MEMBER_LOCATION_TAB_TITLE, OTHER_LOCATION_TAB_TITLE };

        /// <summary>
        /// Used in binding data to the grid, also allows for detecting existing locations
        /// </summary>
        private class GridLocation
        {
            public Guid Guid { get; set; }

            public Location Location { get; set; }

            public string Type { get; set; }

            public int Order { get; set; }

            public string Schedules { get; set; }
        }

        #endregion

        #region Properties

        private string LocationTypeTab { get; set; }

        private int CurrentGroupTypeId { get; set; }

        private List<GroupLocation> GroupLocationsState { get; set; }

        private List<InheritedAttribute> GroupMemberAttributesInheritedState { get; set; }

        private List<Attribute> GroupMemberAttributesState { get; set; }

        private List<GroupRequirement> GroupRequirementsState { get; set; }

        private List<Attribute> GroupDateAttributesState { get; set; }

        private static List<int> DateFieldTypeIds
        {
            get
            {
                // Set the field types that are related to dates for group requirements.
                return new List<int>
                {
                    FieldTypeCache.GetId( Rock.SystemGuid.FieldType.DATE.AsGuid() ).Value,
                    FieldTypeCache.GetId( Rock.SystemGuid.FieldType.DATE_TIME.AsGuid() ).Value
                };
            }
        }

        private bool AllowMultipleLocations { get; set; }

        private List<GroupSyncViewModel> GroupSyncState { get; set; }

        private List<GroupMemberWorkflowTrigger> MemberWorkflowTriggersState { get; set; }

        private GroupTypeCache CurrentGroupTypeCache
        {
            get
            {
                return GroupTypeCache.Get( CurrentGroupTypeId );
            }

            set
            {
                CurrentGroupTypeId = value != null ? value.Id : 0;
            }
        }

        /// <summary>
        /// Gets or sets if the Schedule Tab Visible.
        /// </summary>
        public bool IsScheduleTabVisible
        {
            get { return ViewState[AttributeKey.IsScheduleTabVisible] as bool? ?? false; }
            set { ViewState[AttributeKey.IsScheduleTabVisible] = value; }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            LocationTypeTab = ViewState["LocationTypeTab"] as string ?? MEMBER_LOCATION_TAB_TITLE;
            CurrentGroupTypeId = ViewState["CurrentGroupTypeId"] as int? ?? 0;

            // NOTE: These things are converted to JSON prior to going into ViewState, so the json variable could be null or the string "null"!
            string json = ViewState["GroupLocationsState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupLocationsState = new List<GroupLocation>();
            }
            else
            {
                GroupLocationsState = JsonConvert.DeserializeObject<List<GroupLocation>>( json );
            }

            json = ViewState["GroupMemberAttributesInheritedState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupMemberAttributesInheritedState = new List<InheritedAttribute>();
            }
            else
            {
                GroupMemberAttributesInheritedState = JsonConvert.DeserializeObject<List<InheritedAttribute>>( json );
            }

            json = ViewState["GroupMemberAttributesState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupMemberAttributesState = new List<Attribute>();
            }
            else
            {
                GroupMemberAttributesState = JsonConvert.DeserializeObject<List<Attribute>>( json );
            }

            json = ViewState["GroupDateAttributesState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupDateAttributesState = new List<Attribute>();
            }
            else
            {
                GroupDateAttributesState = JsonConvert.DeserializeObject<List<Attribute>>( json );
            }

            json = ViewState["GroupRequirementsState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupRequirementsState = new List<GroupRequirement>();
            }
            else
            {
                GroupRequirementsState = JsonConvert.DeserializeObject<List<GroupRequirement>>( json ) ?? new List<GroupRequirement>();
            }

            // get the GroupRole for each GroupRequirement from the database it case it isn't serialized, and we'll need it
            var groupRoleIds = GroupRequirementsState.Where( a => a.GroupRoleId.HasValue && a.GroupRole == null ).Select( a => a.GroupRoleId.Value ).Distinct().ToList();
            if ( groupRoleIds.Any() )
            {
                var groupRoles = new GroupTypeRoleService( new RockContext() ).GetByIds( groupRoleIds );
                GroupRequirementsState.ForEach( a =>
                {
                    if ( a.GroupRoleId.HasValue )
                    {
                        a.GroupRole = groupRoles.FirstOrDefault( b => b.Id == a.GroupRoleId );
                    }
                } );
            }

            AllowMultipleLocations = ViewState["AllowMultipleLocations"] as bool? ?? false;

            json = ViewState["GroupSyncState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupSyncState = new List<GroupSyncViewModel>();
            }
            else
            {
                GroupSyncState = JsonConvert.DeserializeObject<List<GroupSyncViewModel>>( json );
            }

            json = ViewState["MemberWorkflowTriggersState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                MemberWorkflowTriggersState = new List<GroupMemberWorkflowTrigger>();
            }
            else
            {
                MemberWorkflowTriggersState = JsonConvert.DeserializeObject<List<GroupMemberWorkflowTrigger>>( json );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gGroupLocations.DataKeyNames = new string[] { "Guid" };
            gGroupLocations.Actions.AddClick += gGroupLocations_Add;
            gGroupLocations.GridRebind += gGroupLocations_GridRebind;

            gGroupMemberAttributesInherited.Actions.ShowAdd = false;
            gGroupMemberAttributesInherited.EmptyDataText = Server.HtmlEncode( None.Text );
            gGroupMemberAttributesInherited.GridRebind += gGroupMemberAttributesInherited_GridRebind;

            gGroupMemberAttributes.DataKeyNames = new string[] { "Guid" };
            gGroupMemberAttributes.Actions.ShowAdd = true;
            gGroupMemberAttributes.Actions.AddClick += gGroupMemberAttributes_Add;
            gGroupMemberAttributes.EmptyDataText = Server.HtmlEncode( None.Text );
            gGroupMemberAttributes.GridRebind += gGroupMemberAttributes_GridRebind;
            gGroupMemberAttributes.GridReorder += gGroupMemberAttributes_GridReorder;

            SecurityField groupMemberAttributeSecurityField = gGroupMemberAttributes.Columns.OfType<SecurityField>().FirstOrDefault();
            groupMemberAttributeSecurityField.EntityTypeId = EntityTypeCache.GetId<Attribute>() ?? 0;

            gGroupRequirements.DataKeyNames = new string[] { "Guid" };
            gGroupRequirements.Actions.ShowAdd = true;
            gGroupRequirements.Actions.AddClick += gGroupRequirements_Add;
            gGroupRequirements.EmptyDataText = Server.HtmlEncode( None.Text );
            gGroupRequirements.GridRebind += gGroupRequirements_GridRebind;

            gGroupSyncs.DataKeyNames = new string[] { "Guid" };
            gGroupSyncs.Actions.ShowAdd = true;
            gGroupSyncs.Actions.AddClick += gGroupSyncs_Add;
            gGroupSyncs.GridRebind += gGroupSyncs_GridRebind;

            gMemberWorkflowTriggers.DataKeyNames = new string[] { "Guid" };
            gMemberWorkflowTriggers.Actions.ShowAdd = true;
            gMemberWorkflowTriggers.Actions.AddClick += gMemberWorkflowTriggers_Add;
            gMemberWorkflowTriggers.EmptyDataText = Server.HtmlEncode( None.Text );
            gMemberWorkflowTriggers.GridRebind += gMemberWorkflowTriggers_GridRebind;
            gMemberWorkflowTriggers.GridReorder += gMemberWorkflowTriggers_GridReorder;

            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Group.FriendlyTypeName );
            btnSecurity.EntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Group ) ).Id;

            rblScheduleSelect.BindToEnum<ScheduleType>();

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlGroupDetail );

            // Add all of the badges for Group to the badge list control
            var badgeCaches = BadgeCache.All( typeof( Group ) );

            if ( badgeCaches.Any() )
            {
                blBadgeList.BadgeTypes.AddRange( badgeCaches );
            }
            else
            {
                divBadgeContainer.Visible = false;
            }

            rblRelationshipStrength.BindToEnum<RelationshipStrength>();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            int? groupId = 0;
            if ( !string.IsNullOrWhiteSpace( PageParameter( PageParameterKey.GroupId ) ) )
            {
                groupId = PageParameter( PageParameterKey.GroupId ).AsIntegerOrNull();
            }

            if ( !Page.IsPostBack )
            {
                btnCopy.Visible = GetAttributeValue( AttributeKey.ShowCopyButton ).AsBoolean();
                if ( groupId.HasValue )
                {
                    ShowDetail( groupId.Value, PageParameter( PageParameterKey.ParentGroupId ).AsIntegerOrNull() );
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
            else
            {
                nbNotAllowedToEdit.Visible = false;
                nbInvalidWorkflowType.Visible = false;
                nbInvalidParentGroup.Visible = false;
                ShowDialog();
            }

            // Rebuild the attribute controls on postback based on group type
            if ( pnlDetails.Visible )
            {
                if ( CurrentGroupTypeId > 0 )
                {
                    var group = new Group { GroupTypeId = CurrentGroupTypeId };

                    ShowGroupTypeEditDetails( CurrentGroupTypeCache, group, false );
                }
            }

            RockContext rockContext = new RockContext();

            if ( groupId.HasValue && groupId.Value != 0 )
            {
                var group = GetGroup( groupId.Value, rockContext );
                if ( group != null )
                {
                    // Handle tags
                    taglGroupTags.EntityTypeId = group.TypeId;
                    taglGroupTags.EntityGuid = group.Guid;
                    taglGroupTags.CategoryGuid = GetAttributeValue( "TagCategory" ).AsGuidOrNull();
                    taglGroupTags.GetTagValues( CurrentPersonId );
                    taglGroupTags.Visible = GetAttributeValue( AttributeKey.EnableGroupTags ).AsBoolean() && group.GroupType.EnableGroupTag;

                    FollowingsHelper.SetFollowing( group, pnlFollowing, this.CurrentPerson );
                }
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
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["LocationTypeTab"] = LocationTypeTab;
            ViewState["CurrentGroupTypeId"] = CurrentGroupTypeId;
            ViewState["GroupLocationsState"] = JsonConvert.SerializeObject( GroupLocationsState, Formatting.None, jsonSetting );
            ViewState["GroupMemberAttributesInheritedState"] = JsonConvert.SerializeObject( GroupMemberAttributesInheritedState, Formatting.None, jsonSetting );
            ViewState["GroupMemberAttributesState"] = JsonConvert.SerializeObject( GroupMemberAttributesState, Formatting.None, jsonSetting );
            ViewState["GroupRequirementsState"] = JsonConvert.SerializeObject( GroupRequirementsState, Formatting.None, jsonSetting );
            ViewState["GroupDateAttributesState"] = JsonConvert.SerializeObject( GroupDateAttributesState, Formatting.None, jsonSetting );
            ViewState["AllowMultipleLocations"] = AllowMultipleLocations;
            ViewState["GroupSyncState"] = JsonConvert.SerializeObject( GroupSyncState, Formatting.None, jsonSetting );
            ViewState["MemberWorkflowTriggersState"] = JsonConvert.SerializeObject( MemberWorkflowTriggersState, Formatting.None, jsonSetting );
            ViewState["ScheduleCoordinatorNotificationTypes"] = cblScheduleCoordinatorNotificationTypes.SelectedValuesAsInt;

            return base.SaveViewState();
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs.
        /// </summary>
        /// <param name="pageReference">The <see cref="Rock.Web.PageReference" />.</param>
        /// <returns>
        /// A <see cref="System.Collections.Generic.List{BreadCrumb}" /> of block related <see cref="Rock.Web.UI.BreadCrumb">BreadCrumbs</see>.
        /// </returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? groupId = PageParameter( pageReference, PageParameterKey.GroupId ).AsIntegerOrNull();
            if ( groupId != null )
            {
                Group group = new GroupService( new RockContext() ).Get( groupId.Value );
                if ( group != null )
                {
                    breadCrumbs.Add( new BreadCrumb( group.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Group", pageReference ) );
                }
            }
            else
            {
                // don't show a breadcrumb if we don't have a pageparam to work with
            }

            return breadCrumbs;
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            ShowEditDetails( GetGroup( hfGroupId.Value.AsInteger() ) );
        }

        /// <summary>
        /// Handles the Click event of the btnArchive control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing much the same event data.</param>
        protected void btnArchive_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groupId = hfGroupId.Value.AsInteger();
            if ( groupService.Queryable().Any( r => r.ParentGroupId == groupId ) )
            {
                mdArchive.Show();
                return;
            }

            ArchiveSingleGroup( hfGroupId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the SaveThenAddClick event of the mdArchive control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdArchive_SingleGroupClick( object sender, EventArgs e )
        {
            ArchiveSingleGroup( hfGroupId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the SaveClick event of the mdArchive control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdArchive_AllChildGroupsClick( object sender, EventArgs e )
        {
            ArchiveAllChildGroups( hfGroupId.Value.AsInteger() );
        }

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnDelete_Click( object sender, EventArgs e )
        {
            int? parentGroupId = null;
            RockContext rockContext = new RockContext();

            GroupService groupService = new GroupService( rockContext );
            AuthService authService = new AuthService( rockContext );
            Group group = groupService.Get( hfGroupId.Value.AsInteger() );

            if ( group != null )
            {
                if ( !group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) )
                {
                    mdDeleteWarning.Show( "You are not authorized to delete this group.", ModalAlertType.Information );
                    return;
                }

                parentGroupId = group.ParentGroupId;
                string errorMessage;
                if ( !groupService.CanDelete( group, out errorMessage, true ) )
                {
                    mdDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                // If group has a non-named schedule, delete the schedule record.
                if ( group.ScheduleId.HasValue )
                {
                    var scheduleService = new ScheduleService( rockContext );
                    var schedule = scheduleService.Get( group.ScheduleId.Value );
                    if ( schedule != null && schedule.ScheduleType != ScheduleType.Named )
                    {
                        // Make sure this is the only group trying to use this schedule.
                        if ( !groupService.Queryable().Where( g => g.ScheduleId == schedule.Id && g.Id != group.Id ).Any() )
                        {
                            scheduleService.Delete( schedule );
                        }
                    }
                }

                // NOTE: groupService.Delete will automatically Archive instead Delete if this Group has GroupHistory enabled, but since this block has UI logic for Archive vs Delete, we can do a direct Archive in btnArchive_Click
                if ( group.IsSecurityRoleOrSecurityGroupType() )
                {
                    GroupService.DeleteSecurityRoleGroup( group.Id );
                }
                else
                {
                    groupService.Delete( group );
                }

                rockContext.SaveChanges();
            }

            NavigateAfterDeleteOrArchive( parentGroupId );
        }

        /// <summary>
        /// Navigates after a group is deleted or archived
        /// </summary>
        /// <param name="parentGroupId">The parent group identifier.</param>
        private void NavigateAfterDeleteOrArchive( int? parentGroupId )
        {
            // reload page, selecting the deleted group's parent
            var qryParams = new Dictionary<string, string>();
            if ( parentGroupId != null )
            {
                qryParams[PageParameterKey.GroupId] = parentGroupId.ToString();
            }

            qryParams[PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds );

            if ( GetAttributeValue( AttributeKey.GroupListPage ).AsGuid() != Guid.Empty )
            {
                NavigateToLinkedPage( AttributeKey.GroupListPage, qryParams );
            }
            else
            {
                NavigateToPage( RockPage.Guid, qryParams );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Group group;
            bool wasSecurityRole = false;
            bool triggersUpdated = false;
            bool checkinDataUpdated = false;

            var rockContext = new RockContext();

            var groupService = new GroupService( rockContext );
            var groupLocationService = new GroupLocationService( rockContext );
            var groupRequirementService = new GroupRequirementService( rockContext );
            var groupMemberWorkflowTriggerService = new GroupMemberWorkflowTriggerService( rockContext );
            var scheduleService = new ScheduleService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var attributeQualifierService = new AttributeQualifierService( rockContext );
            var categoryService = new CategoryService( rockContext );
            var groupSyncService = new GroupSyncService( rockContext );
            var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );

            var roleGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );
            int roleGroupTypeId = roleGroupType != null ? roleGroupType.Id : int.MinValue;

            if ( CurrentGroupTypeId == 0 )
            {
                ddlGroupType.ShowErrorMessage( Rock.Constants.WarningMessage.CannotBeBlank( GroupType.FriendlyTypeName ) );
                return;
            }

            int groupId = hfGroupId.Value.AsInteger();

            if ( groupId == 0 )
            {
                group = new Group();
                group.IsSystem = false;
                group.Name = string.Empty;
                group.GroupTypeId = CurrentGroupTypeId;
            }
            else
            {
                group = groupService.Queryable( "Schedule,GroupLocations.Schedules" ).Where( g => g.Id == groupId ).FirstOrDefault();
                wasSecurityRole = group.IsActive && ( group.IsSecurityRole || group.GroupTypeId == roleGroupTypeId );

                // Remove any locations that removed in the UI
                var selectedLocations = GroupLocationsState.Select( l => l.Guid );
                foreach ( var groupLocation in group.GroupLocations.Where( l => !selectedLocations.Contains( l.Guid ) ).ToList() )
                {
                    List<GroupLocationScheduleConfig> accessModGroupLocationScheduleConfigsToRemove = new List<GroupLocationScheduleConfig>();

                    // Create a list of configurations to be removed cannot modify a collection that is being iterated
                    foreach ( var grouplocationScheduleConfig in groupLocation.GroupLocationScheduleConfigs )
                    {
                        accessModGroupLocationScheduleConfigsToRemove.Add( grouplocationScheduleConfig );
                    }

                    // Remove the dependent group location schedule configurations
                    foreach ( var deleteConfig in accessModGroupLocationScheduleConfigsToRemove )
                    {
                        groupLocation.GroupLocationScheduleConfigs.Remove( deleteConfig );
                    }

                    // Remove GroupMember assignments for this location
                    foreach ( var schedule in groupLocation.Schedules )
                    {
                        var configuredSchedules = groupMemberAssignmentService.Queryable()
                            .Where( a => a.ScheduleId == schedule.Id && a.LocationId == groupLocation.LocationId && a.GroupMember.GroupId == groupLocation.GroupId )
                            .ToList();

                        groupMemberAssignmentService.DeleteRange( configuredSchedules );
                    }

                    // Remove the location.
                    group.GroupLocations.Remove( groupLocation );
                    groupLocationService.Delete( groupLocation );
                    checkinDataUpdated = true;
                }

                // Remove any group requirements that removed in the UI
                var selectedGroupRequirements = GroupRequirementsState.Select( a => a.Guid );
                foreach ( var groupRequirement in group.GetGroupRequirements( rockContext ).Where( a => a.GroupId.HasValue ).Where( a => !selectedGroupRequirements.Contains( a.Guid ) ).ToList() )
                {
                    groupRequirementService.Delete( groupRequirement );
                }

                // Remove any triggers that were removed in the UI
                var selectedTriggerGuids = MemberWorkflowTriggersState.Select( r => r.Guid );
                foreach ( var trigger in group.GroupMemberWorkflowTriggers.Where( r => !selectedTriggerGuids.Contains( r.Guid ) ).ToList() )
                {
                    group.GroupMemberWorkflowTriggers.Remove( trigger );
                    groupMemberWorkflowTriggerService.Delete( trigger );
                    triggersUpdated = true;
                }

                // Remove any GroupSyncs that were removed in the UI
                var selectedGroupSyncs = GroupSyncState.Select( s => s.Guid );
                foreach ( var groupSync in group.GroupSyncs.Where( s => !selectedGroupSyncs.Contains( s.Guid ) ).ToList() )
                {
                    group.GroupSyncs.Remove( groupSync );
                    groupSyncService.Delete( groupSync );
                }
            }

            // Assign the Group Type.
            group.GroupType = new GroupTypeService( rockContext ).Get( group.GroupTypeId );

            List<GroupRequirement> groupRequirementsToInsert = new List<GroupRequirement>();

            // Add/Update any group requirements that were added or changed in the UI (we already removed the ones that were removed above)
            foreach ( var groupRequirementState in GroupRequirementsState )
            {
                GroupRequirement groupRequirement = group.GetGroupRequirements( rockContext ).Where( a => a.GroupId.HasValue ).Where( a => a.Guid == groupRequirementState.Guid ).FirstOrDefault();
                if ( groupRequirement == null )
                {
                    groupRequirement = new GroupRequirement();
                    groupRequirementsToInsert.Add( groupRequirement );
                }

                groupRequirement.CopyPropertiesFrom( groupRequirementState );
            }

            var deletedSchedules = new List<int>();

            // Add/Update any group locations that were added or changed in the UI (we already removed the ones that were removed above).
            foreach ( var groupLocationState in GroupLocationsState )
            {
                GroupLocation groupLocation = group.GroupLocations.Where( l => l.Guid == groupLocationState.Guid ).FirstOrDefault();
                if ( groupLocation == null )
                {
                    groupLocation = new GroupLocation();
                    group.GroupLocations.Add( groupLocation );
                }
                else
                {
                    groupLocationState.Id = groupLocation.Id;
                    groupLocationState.Guid = groupLocation.Guid;

                    var selectedSchedules = groupLocationState.Schedules.Select( s => s.Guid ).ToList();

                    // If the location has changed for this groupLocation then any existing GroupAssignment is not longer valid so we delete them.
                    if ( groupLocationState.LocationId != groupLocation.LocationId )
                    {
                        foreach ( var schedule in groupLocationState.Schedules )
                        {
                            var configuredSchedules = groupMemberAssignmentService.Queryable()
                                .Where( a => a.ScheduleId == schedule.Id && a.LocationId == groupLocation.LocationId && a.GroupMember.GroupId == groupLocation.GroupId )
                                .ToList();

                            groupMemberAssignmentService.DeleteRange( configuredSchedules );
                        }
                    }

                    foreach ( var schedule in groupLocation.Schedules.Where( s => !selectedSchedules.Contains( s.Guid ) ).ToList() )
                    {
                        deletedSchedules.Add( schedule.Id );
                        groupLocation.Schedules.Remove( schedule );
                    }
                }

                groupLocation.CopyPropertiesFrom( groupLocationState );

                // Update Group Location Schedules
                var existingSchedules = groupLocation.Schedules.Select( s => s.Guid ).ToList();
                var existingGroupLocationConfigs = groupLocation.GroupLocationScheduleConfigs.Select( g => g );

                foreach ( var scheduleState in groupLocationState.Schedules.Where( s => !existingSchedules.Contains( s.Guid ) ).ToList() )
                {
                    var schedule = scheduleService.Get( scheduleState.Guid );
                    if ( schedule != null )
                    {
                        groupLocation.Schedules.Add( schedule );
                    }
                }

                // Get existing configurations with modified capacity values.
                var modifiedScheduleConfigs = groupLocationState.GroupLocationScheduleConfigs
                    .Where( s => groupLocation.GroupLocationScheduleConfigs
                        .Where( exs => ( exs.ScheduleId == s.ScheduleId )
                            && exs.GroupLocationId == s.GroupLocationId
                            && ( exs.MinimumCapacity != s.MinimumCapacity
                            || exs.DesiredCapacity != s.DesiredCapacity
                            || exs.MaximumCapacity != s.MaximumCapacity ) ).Any() )
                    .ToList();

                // Add new scheduling configurations.
                var newGroupLocationScheduleConfigs = groupLocationState.GroupLocationScheduleConfigs
                    .Where( s => !existingGroupLocationConfigs.Any( a => a.ScheduleId == s.ScheduleId ) )
                    .ToList();

                foreach ( var addedGroupLocationScheduleConfigs in newGroupLocationScheduleConfigs )
                {
                    groupLocation.GroupLocationScheduleConfigs.Add(
                        new GroupLocationScheduleConfig
                        {
                            ScheduleId = addedGroupLocationScheduleConfigs.ScheduleId,
                            MinimumCapacity = addedGroupLocationScheduleConfigs.MinimumCapacity,
                            DesiredCapacity = addedGroupLocationScheduleConfigs.DesiredCapacity,
                            MaximumCapacity = addedGroupLocationScheduleConfigs.MaximumCapacity
                        } );
                }

                // Update the scheduling configs
                foreach ( var updatedSchedulingConfig in modifiedScheduleConfigs )
                {
                    var currentSchedulingConfig = groupLocation.GroupLocationScheduleConfigs
                        .Where( curr => curr.ScheduleId == updatedSchedulingConfig.ScheduleId
                        && curr.GroupLocationId == updatedSchedulingConfig.GroupLocationId ).FirstOrDefault();

                    currentSchedulingConfig.MinimumCapacity = updatedSchedulingConfig.MinimumCapacity;
                    currentSchedulingConfig.DesiredCapacity = updatedSchedulingConfig.DesiredCapacity;
                    currentSchedulingConfig.MaximumCapacity = updatedSchedulingConfig.MaximumCapacity;
                }

                // Delete the scheduling configs
                foreach ( var deletedScheduleId in deletedSchedules )
                {
                    var associatedConfig = groupLocation.GroupLocationScheduleConfigs.Where( cfg => cfg.Schedule != null && cfg.Schedule.Id == deletedScheduleId ).FirstOrDefault();
                    groupLocation.GroupLocationScheduleConfigs.Remove( associatedConfig );
                }

                checkinDataUpdated = true;
            }

            int? orphanedChatChannelAvatarId = null;

            if ( ChatHelper.IsChatEnabled && group.GroupType?.IsChatAllowed == true )
            {
                group.IsChatEnabledOverride = ddlIsChatEnabled.SelectedValue.AsBooleanOrNull();
                group.IsLeavingChatChannelAllowedOverride = ddlIsLeavingChatChannelAllowed.SelectedValue.AsBooleanOrNull();
                group.IsChatChannelPublicOverride = ddlIsChatChannelPublic.SelectedValue.AsBooleanOrNull();
                group.IsChatChannelAlwaysShownOverride = ddlIsChatChannelAlwaysShown.SelectedValue.AsBooleanOrNull();
                group.ChatPushNotificationModeOverride = ddlChatPushNotificationMode.SelectedValueAsEnumOrNull<ChatNotificationMode>();

                if ( group.ChatChannelAvatarBinaryFileId != imgChatChannelAvatar.BinaryFileId )
                {
                    orphanedChatChannelAvatarId = group.ChatChannelAvatarBinaryFileId;
                }

                group.ChatChannelAvatarBinaryFileId = imgChatChannelAvatar.BinaryFileId;
            }

            // Add/update GroupSyncs
            foreach ( var groupSyncState in GroupSyncState )
            {
                GroupSync groupSync = group.GroupSyncs.Where( s => s.Guid == groupSyncState.Guid ).FirstOrDefault();
                if ( groupSync == null )
                {
                    groupSync = new GroupSync();
                    group.GroupSyncs.Add( groupSync );
                }

                groupSync.CopyPropertiesFrom( groupSyncState );
            }

            // Add/update workflow triggers
            foreach ( var triggerState in MemberWorkflowTriggersState )
            {
                GroupMemberWorkflowTrigger trigger = group.GroupMemberWorkflowTriggers.Where( r => r.Guid == triggerState.Guid ).FirstOrDefault();
                if ( trigger == null )
                {
                    trigger = new GroupMemberWorkflowTrigger();
                    group.GroupMemberWorkflowTriggers.Add( trigger );
                }
                else
                {
                    triggerState.Id = trigger.Id;
                    triggerState.Guid = trigger.Guid;
                }

                trigger.CopyPropertiesFrom( triggerState );
                triggersUpdated = true;
            }

            int? campusId = cpCampus.SelectedCampusId;
            if ( !campusId.HasValue && group.GroupType.GroupsRequireCampus )
            {
                // If the CampusPicker doesn't have a selected value AND there is only one campus, grab its ID from the cache
                campusId = CampusCache.SingleCampusId;
            }

            group.Name = tbName.Text;
            group.Description = tbDescription.Text;
            group.CampusId = campusId;
            group.GroupTypeId = CurrentGroupTypeId;
            group.ParentGroupId = gpParentGroup.SelectedValueAsInt();
            group.StatusValueId = dvpGroupStatus.SelectedValueAsId();
            group.GroupCapacity = nbGroupCapacity.Text.AsIntegerOrNull();
            group.RequiredSignatureDocumentTemplateId = ddlSignatureDocumentTemplate.SelectedValueAsInt();

            if ( group.GroupType.AllowGroupSpecificRecordSource )
            {
                group.GroupMemberRecordSourceValueId = dvpRecordSource.SelectedValueAsInt();
            }
            else
            {
                group.GroupMemberRecordSourceValueId = null;
            }

            group.IsSecurityRole = cbIsSecurityRole.Checked;

            // If this block's attribute limits group to SecurityRoleGroups, don't let them edit the SecurityRole checkbox value
            if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
            {
                group.IsSecurityRole = true;
            }

            group.ElevatedSecurityLevel = rblElevatedSecurityLevel.SelectedValue.ConvertToEnum<ElevatedSecurityLevel>();
            if ( !group.IsSecurityRole )
            {
                group.ElevatedSecurityLevel = ElevatedSecurityLevel.None;
            }

            group.IsActive = cbIsActive.Checked;
            group.IsPublic = cbIsPublic.Checked;

            // Don't save inactive properties if the group is active.
            group.InactiveReasonValueId = cbIsActive.Checked ? null : ddlInactiveReason.SelectedValueAsInt();
            group.InactiveReasonNote = cbIsActive.Checked ? null : tbInactiveNote.Text;

            // Save Peer Network settings.
            if ( group.GroupType.IsPeerNetworkEnabled )
            {
                if ( cbOverrideRelationshipStrength.Checked )
                {
                    group.RelationshipStrengthOverride = rblRelationshipStrength.SelectedValueAsInt() ?? ( int ) RelationshipStrength.None;
                    group.RelationshipGrowthEnabledOverride = cbEnableRelationshipGrowth.Checked;

                    // This approach will only apply overrides if they're explicitly assigned (and will allow null values
                    // so the parent group type's values can take effect where needed).
                    group.LeaderToLeaderRelationshipMultiplierOverride = tbLeaderToLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( minPercentage: 0, maxPercentage: 100 );
                    group.LeaderToNonLeaderRelationshipMultiplierOverride = tbLeaderToNonLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( minPercentage: 0, maxPercentage: 100 );
                    group.NonLeaderToLeaderRelationshipMultiplierOverride = tbNonLeaderToLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( minPercentage: 0, maxPercentage: 100 );
                    group.NonLeaderToNonLeaderRelationshipMultiplierOverride = tbNonLeaderToNonLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( minPercentage: 0, maxPercentage: 100 );
                }
                else
                {
                    // Clear out any previous overrides, so the parent group type settings will take full effect.
                    group.RelationshipStrengthOverride = null;
                    group.RelationshipGrowthEnabledOverride = null;

                    group.LeaderToLeaderRelationshipMultiplierOverride = null;
                    group.LeaderToNonLeaderRelationshipMultiplierOverride = null;
                    group.NonLeaderToLeaderRelationshipMultiplierOverride = null;
                    group.NonLeaderToNonLeaderRelationshipMultiplierOverride = null;
                }
            }
            // else: leave the group's existing relationship strength overrides (if any) in place, as the calculations
            // will safely ignore this group's values (since the parent group type has disabled the peer network feature).

            // Save RSVP settings.
            if ( group.GroupType.EnableRSVP )
            {
                // Offset Days
                if ( group.GroupType.RSVPReminderOffsetDays.HasValue )
                {
                    group.RSVPReminderOffsetDays = null;
                }
                else
                {
                    // Group Type setting takes precedence over Group setting.
                    group.RSVPReminderOffsetDays = rsRsvpReminderOffsetDays.SelectedValue;
                }

                // Reminder
                if ( group.GroupType.RSVPReminderSystemCommunicationId.HasValue )
                {
                    group.RSVPReminderSystemCommunicationId = null;
                }
                else
                {
                    // Group Type setting takes precedence over Group setting.
                    group.RSVPReminderSystemCommunicationId = ddlRsvpReminderSystemCommunication.SelectedValueAsInt();
                }
            }
            else
            {
                group.RSVPReminderOffsetDays = null;
                group.RSVPReminderSystemCommunicationId = null;
            }

            // Save Scheduling settings.
            group.SchedulingMustMeetRequirements = cbSchedulingMustMeetRequirements.Checked;
            group.AttendanceRecordRequiredForCheckIn = ddlAttendanceRecordRequiredForCheckIn.SelectedValueAsEnum<AttendanceRecordRequiredForCheckIn>();
            group.ScheduleCoordinatorPersonAliasId = ppScheduleCoordinatorPerson.PersonAliasId;
            group.DisableScheduling = cbDisableGroupScheduling.Checked;
            group.DisableScheduleToolboxAccess = cbDisableScheduleToolboxAccess.Checked;
            group.ScheduleConfirmationLogic = ddlScheduleConfirmationLogic.SelectedValueAsEnumOrNull<ScheduleConfirmationLogic>();
            string iCalendarContent = string.Empty;

            ScheduleCoordinatorNotificationType? notificationTypes = null;
            foreach ( var li in cblScheduleCoordinatorNotificationTypes.Items.Cast<ListItem>() )
            {
                if ( !li.Selected )
                {
                    continue;
                }

                var selectedType = ( ScheduleCoordinatorNotificationType ) li.Value.AsInteger();
                if ( selectedType == ScheduleCoordinatorNotificationType.None )
                {
                    // Ensure that if "None" is selected, it's the only value that can be saved.
                    notificationTypes = ScheduleCoordinatorNotificationType.None;
                    break;
                }

                // Otherwise save all selected values.
                notificationTypes = notificationTypes.HasValue
                    ? notificationTypes | selectedType
                    : selectedType;
            }

            group.ScheduleCoordinatorNotificationTypes = notificationTypes;

            // If unique schedule option was selected, but a schedule was not defined, set option to 'None'
            var scheduleType = rblScheduleSelect.SelectedValueAsEnum<ScheduleType>( ScheduleType.None );
            if ( scheduleType == ScheduleType.Custom )
            {
                iCalendarContent = sbSchedule.iCalendarContent;
                var calEvent = InetCalendarHelper.CreateCalendarEvent( iCalendarContent );
                if ( calEvent == null || calEvent.DtStart == null )
                {
                    scheduleType = ScheduleType.None;
                }
            }

            if ( scheduleType == ScheduleType.Weekly )
            {
                if ( !dowWeekly.SelectedDayOfWeek.HasValue )
                {
                    scheduleType = ScheduleType.None;
                }
            }

            int? oldScheduleId = hfUniqueScheduleId.Value.AsIntegerOrNull();
            if ( scheduleType == ScheduleType.Custom || scheduleType == ScheduleType.Weekly )
            {
                if ( !oldScheduleId.HasValue || group.Schedule == null )
                {
                    group.Schedule = new Schedule();

                    // NOTE: Schedule Name should be set to string.Empty to indicate that it is a Custom or Weekly schedule and not a "Named" schedule
                    group.Schedule.Name = string.Empty;
                }

                if ( scheduleType == ScheduleType.Custom )
                {
                    group.Schedule.iCalendarContent = iCalendarContent;
                    group.Schedule.WeeklyDayOfWeek = null;
                    group.Schedule.WeeklyTimeOfDay = null;
                }
                else
                {
                    group.Schedule.iCalendarContent = null;
                    group.Schedule.WeeklyDayOfWeek = dowWeekly.SelectedDayOfWeek;
                    group.Schedule.WeeklyTimeOfDay = timeWeekly.SelectedTime;
                }
            }
            else
            {
                // If group did have a unique schedule, delete that schedule
                if ( oldScheduleId.HasValue )
                {
                    var schedule = scheduleService.Get( oldScheduleId.Value );
                    if ( schedule != null && string.IsNullOrEmpty( schedule.Name ) )
                    {
                        // Make sure this is the only thing using this schedule.
                        string errorMessage;
                        if ( scheduleService.CanDelete( schedule, out errorMessage ) )
                        {
                            scheduleService.Delete( schedule );
                        }
                    }
                }

                if ( scheduleType == ScheduleType.Named )
                {
                    group.ScheduleId = spSchedule.SelectedValueAsId();
                }
                else
                {
                    group.ScheduleId = null;
                }
            }

            if ( group.ParentGroupId == group.Id )
            {
                gpParentGroup.ShowErrorMessage( "Group cannot be a Parent Group of itself." );
                return;
            }

            group.LoadAttributes();
            Helper.GetEditValues( phGroupAttributes, group );

            if ( group.ParentGroupId.HasValue )
            {
                group.ParentGroup = groupService.Get( group.ParentGroupId.Value );
            }

            if ( group.GroupType.ShowAdministrator )
            {
                group.GroupAdministratorPersonAliasId = ppAdministrator.PersonAliasId;
            }

            // Check to see if group type is allowed as a child of new parent group.
            if ( group.ParentGroup != null )
            {
                var allowedGroupTypeIds = GetAllowedGroupTypes( GroupTypeCache.Get( group.ParentGroup.GroupTypeId ), rockContext ).Select( t => t.Id ).ToList();
                if ( !allowedGroupTypeIds.Contains( group.GroupTypeId ) )
                {
                    var groupType = CurrentGroupTypeCache;
                    nbInvalidParentGroup.Text = string.Format( "The '{0}' group does not allow child groups with a '{1}' group type.", group.ParentGroup.Name, groupType != null ? groupType.Name : string.Empty );
                    nbInvalidParentGroup.Visible = true;
                    return;
                }
            }

            // Check to see if user is still allowed to edit with selected group type and parent group
            if ( !group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                nbNotAllowedToEdit.Visible = true;
                return;
            }

            if ( !Page.IsValid )
            {
                return;
            }

            // if the groupMember IsValid is false, and the UI controls didn't report any errors, it is probably because the custom rules of GroupMember didn't pass.
            // So, make sure a message is displayed in the validation summary
            cvGroup.IsValid = group.IsValid;

            if ( !cvGroup.IsValid )
            {
                cvGroup.ErrorMessage = group.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                return;
            }

            // use WrapTransaction since SaveAttributeValues does its own RockContext.SaveChanges()
            rockContext.WrapTransaction( () =>
            {
                var adding = group.Id.Equals( 0 );
                if ( adding )
                {
                    groupService.Add( group );
                }

                // Save changes because we'll need the group's Id next...
                rockContext.SaveChanges();

                /* 2020-11-18 ETD
                 * Do not assign the group creator Administrate security permissions unless AddAdministrateSecurityToGroupCreator is true.
                 */

                if ( adding && GetAttributeValue( AttributeKey.AddAdministrateSecurityToGroupCreator ).AsBoolean() )
                {
                    // Add ADMINISTRATE to the person who added the group
                    Rock.Security.Authorization.AllowPerson( group, Authorization.ADMINISTRATE, this.CurrentPerson, rockContext );
                }

                if ( groupRequirementsToInsert.Any() )
                {
                    groupRequirementsToInsert.ForEach( a => a.GroupId = group.Id );
                    groupRequirementService.AddRange( groupRequirementsToInsert );
                }

                group.SaveAttributeValues( rockContext );

                /* Take care of Group Member Attributes */
                var entityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id;
                string qualifierColumn = "GroupId";
                string qualifierValue = group.Id.ToString();

                // Get the existing attributes for this entity type and qualifier value
                var attributes = attributeService.GetByEntityTypeQualifier( entityTypeId, qualifierColumn, qualifierValue, true );

                // Delete any of those attributes that were removed in the UI
                var selectedAttributeGuids = GroupMemberAttributesState.Select( a => a.Guid );
                foreach ( var attr in attributes.Where( a => !selectedAttributeGuids.Contains( a.Guid ) ) )
                {
                    attributeService.Delete( attr );
                }

                // Update the Attributes that were assigned in the UI
                foreach ( var attributeState in GroupMemberAttributesState )
                {
                    Rock.Attribute.Helper.SaveAttributeEdits( attributeState, entityTypeId, qualifierColumn, qualifierValue, rockContext );
                }

                rockContext.SaveChanges();

                if ( group.IsActive == false && cbInactivateChildGroups.Checked )
                {
                    var allActiveChildGroupsId = groupService.GetAllDescendentGroupIds( group.Id, false );
                    var allActiveChildGroups = groupService.GetByIds( allActiveChildGroupsId );
                    foreach ( var childGroup in allActiveChildGroups )
                    {
                        if ( childGroup.IsActive )
                        {
                            childGroup.IsActive = false;
                            childGroup.InactiveReasonValueId = ddlInactiveReason.SelectedValueAsInt();
                            childGroup.InactiveReasonNote = "Parent Deactivated";
                            if ( tbInactiveNote.Text.IsNotNullOrWhiteSpace() )
                            {
                                childGroup.InactiveReasonNote += ": " + tbInactiveNote.Text;
                            }
                        }
                    }

                    rockContext.SaveChanges();
                }

                if ( orphanedChatChannelAvatarId.HasValue || group.ChatChannelAvatarBinaryFileId.HasValue )
                {
                    var binaryFileService = new BinaryFileService( rockContext );

                    if ( orphanedChatChannelAvatarId.HasValue )
                    {
                        var binaryFile = binaryFileService.Get( orphanedChatChannelAvatarId.Value );
                        if ( binaryFile != null )
                        {
                            binaryFile.IsTemporary = true;
                        }
                    }

                    if ( group.ChatChannelAvatarBinaryFileId.HasValue )
                    {
                        var binaryFile = binaryFileService.Get( group.ChatChannelAvatarBinaryFileId.Value );
                        if ( binaryFile != null )
                        {
                            binaryFile.IsTemporary = false;
                        }
                    }

                    rockContext.SaveChanges();
                }
            } );

            bool isNowSecurityRole = group.IsActive && ( group.IsSecurityRole || group.GroupTypeId == roleGroupTypeId );

            if ( group != null && wasSecurityRole )
            {
                if ( !isNowSecurityRole )
                {
                    // If this group was a SecurityRole, but no longer is, flush
                    Rock.Security.Authorization.Clear();
                }
            }
            else
            {
                if ( isNowSecurityRole )
                {
                    // New security role, flush
                    Rock.Security.Authorization.Clear();
                }
            }

            if ( triggersUpdated )
            {
                GroupMemberWorkflowTriggerService.RemoveCachedTriggers();
            }

            // Flush the kiosk devices cache if this group updated check-in data and its group type takes attendance
            if ( checkinDataUpdated && group.GroupType.TakesAttendance )
            {
                Rock.CheckIn.KioskDevice.Clear();
            }

            var qryParams = new Dictionary<string, string>();
            qryParams[PageParameterKey.GroupId] = group.Id.ToString();
            qryParams[PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds );

            NavigateToPage( RockPage.Guid, qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfGroupId.Value.Equals( "0" ) )
            {
                int? parentGroupId = PageParameter( PageParameterKey.ParentGroupId ).AsIntegerOrNull();
                if ( parentGroupId.HasValue )
                {
                    // Cancelling on Add, and we know the parentGroupID, so we are probably in treeview mode, so navigate to the current page
                    var qryParams = new Dictionary<string, string>();
                    if ( parentGroupId != 0 )
                    {
                        qryParams[PageParameterKey.GroupId] = parentGroupId.ToString();
                    }

                    qryParams[PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds );

                    NavigateToPage( RockPage.Guid, qryParams );
                }
                else
                {
                    if ( GetAttributeValue( AttributeKey.GroupListPage ).AsGuid() != Guid.Empty )
                    {
                        NavigateToLinkedPage( AttributeKey.GroupListPage );
                    }
                    else
                    {
                        NavigateToPage( RockPage.Guid, null );
                    }
                }
            }
            else
            {
                // Canceling on Edit.  Return to Details
                ShowReadonlyDetails( GetGroup( hfGroupId.Value.AsInteger() ) );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCopy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCopy_Click( object sender, EventArgs e )
        {
            mdCopyGroup.Show();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdCopyGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdCopyGroup_SaveClick( object sender, EventArgs e )
        {
            int groupId = hfGroupId.ValueAsInt();
            bool includeChildGroups = cbCopyGroupIncludeChildGroups.Checked;
            int? newGroupId = null;

            if ( groupId > 0 )
            {
                var currentGroup = GetGroup( hfGroupId.Value.AsInteger() );
                if ( currentGroup != null && !currentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
                {
                    nbEditModeMessage.Visible = true;
                    nbEditModeMessage.Text = "You are not authorized to copy the group";
                    return;
                }
                var copyGroupOptions = new CopyGroupOptions
                {
                    GroupId = groupId,
                    IncludeChildGroups = includeChildGroups,
                    CreatedByPersonAliasId = CurrentPersonAliasId
                };
                newGroupId = GroupService.CopyGroup( copyGroupOptions );
            }

            var qryParams = new Dictionary<string, string>();
            qryParams[PageParameterKey.GroupId] = newGroupId.HasValue && newGroupId > 0 ? newGroupId.Value.ToString() : groupId.ToString();
            qryParams[PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds );
            NavigateToPage( RockPage.Guid, qryParams );
        }

        #endregion

        #region Control Events

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlGroupType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlGroupType_SelectedIndexChanged( object sender, EventArgs e )
        {
            // Grouptype changed, so load up the new attributes and set controls to the default attribute values
            CurrentGroupTypeId = ddlGroupType.SelectedValueAsInt() ?? 0;
            if ( CurrentGroupTypeId > 0 )
            {
                var group = new Group { GroupTypeId = CurrentGroupTypeId };
                var groupType = CurrentGroupTypeCache;

                SetRecordSourceControls( groupType, group );
                SetPeerNetworkControls( groupType, group );
                SetRsvpControls( groupType, null );
                SetScheduleControls( groupType, null );
                ShowGroupTypeEditDetails( groupType, group, true );
                BindInheritedAttributes( CurrentGroupTypeId, new AttributeService( new RockContext() ) );
                BindGroupRequirementsGrid();
                BindAdministratorPerson( group, groupType );
                SetChatControls( groupType, group );
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlParentGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void ddlParentGroup_SelectedIndexChanged( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            int? parentGroupId = gpParentGroup.SelectedValueAsInt();
            int? parentGroupGroupTypeId = null;
            if ( parentGroupId.HasValue )
            {
                parentGroupGroupTypeId = new GroupService( rockContext ).GetSelect( parentGroupId.Value, s => ( int? ) s.GroupTypeId );
            }

            var groupTypeQry = GetAllowedGroupTypes( GroupTypeCache.Get( parentGroupGroupTypeId ?? 0 ), rockContext );

            List<GroupType> groupTypes = groupTypeQry.OrderBy( a => a.Name ).ToList();
            if ( groupTypes.Count() > 1 )
            {
                // Add a empty option so they are forced to choose
                groupTypes.Insert( 0, new GroupType { Id = 0, Name = string.Empty } );
            }

            // If the currently selected GroupType isn't an option anymore, set selected GroupType to null
            if ( ddlGroupType.Visible )
            {
                int? selectedGroupTypeId = ddlGroupType.SelectedValueAsInt();
                if ( ddlGroupType.SelectedValue != null )
                {
                    if ( !groupTypes.Any( a => a.Id.Equals( selectedGroupTypeId ?? 0 ) ) )
                    {
                        selectedGroupTypeId = null;
                    }
                }

                ddlGroupType.DataSource = groupTypes;
                ddlGroupType.DataBind();

                if ( selectedGroupTypeId.HasValue )
                {
                    CurrentGroupTypeId = selectedGroupTypeId.Value;
                    ddlGroupType.SelectedValue = selectedGroupTypeId.ToString();
                }
                else
                {
                    CurrentGroupTypeId = 0;
                    ddlGroupType.SelectedValue = null;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbProperty control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbLocationType_Click( object sender, EventArgs e )
        {
            LinkButton lb = sender as LinkButton;
            if ( lb != null )
            {
                LocationTypeTab = lb.Text;

                rptLocationTypes.DataSource = _tabs;
                rptLocationTypes.DataBind();
            }

            ShowSelectedPane();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            var currentGroup = GetGroup( hfGroupId.Value.AsInteger() );
            btnCopy.Visible = GetAttributeValue( AttributeKey.ShowCopyButton ).AsBoolean() && currentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson );
            if ( currentGroup != null )
            {
                ShowReadonlyDetails( currentGroup );
            }
            else
            {
                string groupId = PageParameter( PageParameterKey.GroupId );
                if ( !string.IsNullOrWhiteSpace( groupId ) )
                {
                    ShowDetail( groupId.AsInteger(), PageParameter( PageParameterKey.ParentGroupId ).AsIntegerOrNull() );
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblScheduleSelect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void rblScheduleSelect_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetScheduleDisplay();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblScheduleCoordinatorNotificationTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblScheduleCoordinatorNotificationTypes_SelectedIndexChanged( object sender, EventArgs e )
        {
            // Retrieve the previously-selected values before this postback.
            var previouslySelectedValues = ( List<int> ) ViewState["ScheduleCoordinatorNotificationTypes"] ?? new List<int>();

            // Was "None" selected before this postback?
            var wasNoneSelected = previouslySelectedValues.Any( v =>
                ( ScheduleCoordinatorNotificationType ) v == ScheduleCoordinatorNotificationType.None
            );

            // Get the currently-selected values.
            var selectedValues = cblScheduleCoordinatorNotificationTypes.SelectedValuesAsInt ?? new List<int>();

            // Is "None" selected now?
            var isNoneSelected = selectedValues.Any( v =>
                ( ScheduleCoordinatorNotificationType ) v == ScheduleCoordinatorNotificationType.None
            );

            // Are any other options selected now?
            var anyOthersSelected = selectedValues.Any( v =>
                ( ScheduleCoordinatorNotificationType ) v != ScheduleCoordinatorNotificationType.None
            );

            foreach ( var li in cblScheduleCoordinatorNotificationTypes.Items.Cast<ListItem>() )
            {
                var notificationType = ( ScheduleCoordinatorNotificationType ) li.Value.AsInteger();

                // If "None" wasn't selected, but is now, deselect all other options.
                if ( !wasNoneSelected && isNoneSelected )
                {
                    li.Selected = notificationType == ScheduleCoordinatorNotificationType.None;
                    continue;
                }

                // If "None" was (and still is) selected, but other options have now been selected, deselect "None".
                if ( isNoneSelected && anyOthersSelected && notificationType == ScheduleCoordinatorNotificationType.None )
                {
                    li.Selected = false;
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        public void ShowDetail( int groupId )
        {
            ShowDetail( groupId, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="parentGroupId">The parent group identifier.</param>
        public void ShowDetail( int groupId, int? parentGroupId )
        {
            Group group = null;

            bool viewAllowed = false;
            bool editAllowed = IsUserAuthorized( Authorization.EDIT );

            RockContext rockContext = new RockContext();

            if ( !groupId.Equals( 0 ) )
            {
                group = GetGroup( groupId, rockContext );
                if ( group == null )
                {
                    pnlDetails.Visible = false;
                    nbNotFoundOrArchived.Visible = true;
                    return;
                }
                else
                {
                    string lava = "{{ Group.Name | AddQuickReturn:'Groups', 20 }}";
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions() );
                    mergeFields.Add( "Group", group );
                    lava.ResolveMergeFields( mergeFields );
                }
            }

            if ( group == null )
            {
                group = new Group { Id = 0, IsActive = true, IsPublic = true, ParentGroupId = parentGroupId, Name = string.Empty };
                wpGeneral.Expanded = true;

                if ( parentGroupId.HasValue )
                {
                    // Set the new group's parent group (so security checks work)
                    var parentGroup = new GroupService( rockContext ).Get( parentGroupId.Value );
                    if ( parentGroup != null )
                    {
                        // Start by setting the group type to the same as the parent
                        group.ParentGroup = parentGroup;

                        // Get all the allowed GroupTypes as defined by the parent group type
                        var allowedChildGroupTypesOfParentGroup = GetAllowedGroupTypes( GroupTypeCache.Get( parentGroup.GroupTypeId ), rockContext ).ToList();

                        // Narrow it down to group types that the current user is allowed to edit
                        var authorizedGroupTypes = new List<GroupType>();
                        foreach ( var allowedGroupType in allowedChildGroupTypesOfParentGroup )
                        {
                            // To see if the user is authorized for the group type, test by setting the new group's grouptype and see if they are authorized
                            group.GroupTypeId = allowedGroupType.Id;
                            group.GroupType = allowedGroupType;

                            if ( group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
                            {
                                authorizedGroupTypes.Add( allowedGroupType );

                                // They have EDIT auth to at least one GroupType, so they are allowed to try to add this group
                                editAllowed = true;
                            }
                        }

                        // Exactly one grouptype is allowed/authorized, so it is safe to default this new group to it
                        if ( authorizedGroupTypes.Count() == 1 )
                        {
                            group.GroupType = authorizedGroupTypes.First();
                            group.GroupTypeId = group.GroupType.Id;
                        }
                        else
                        {
                            // more than one grouptype is allowed/authorized, so don't default it so they are forced to pick which one
                            group.GroupType = null;
                            group.GroupTypeId = 0;
                        }
                    }
                }
            }

            viewAllowed = editAllowed || group.IsAuthorized( Authorization.VIEW, CurrentPerson );
            editAllowed = editAllowed || group.IsAuthorized( Authorization.EDIT, CurrentPerson );

            pnlDetails.Visible = viewAllowed;

            hfGroupId.Value = group.Id.ToString();

            // Render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !editAllowed )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Group.FriendlyTypeName );
            }

            if ( group.IsSystem )
            {
                nbEditModeMessage.Text = EditModeMessage.System( Group.FriendlyTypeName );
            }

            string roleLimitWarnings;
            nbRoleLimitWarning.Visible = group.GetGroupTypeRoleLimitWarnings( out roleLimitWarnings );
            nbRoleLimitWarning.Text = roleLimitWarnings;

            if ( readOnly )
            {
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnArchive.Visible = false;
                btnCopy.Visible = false;
                ShowReadonlyDetails( group );
            }
            else
            {
                btnEdit.Visible = true;
                if ( group.Id > 0 )
                {
                    ShowReadonlyDetails( group );
                }
                else
                {
                    ShowEditDetails( group );
                }
            }
        }

        /// <summary>
        /// Sets the highlight label visibility.
        /// </summary>
        /// <param name="group">The group.</param>
        private void SetHighlightLabelVisibility( Group group, bool readOnly )
        {
            if ( readOnly )
            {
                // If we are just showing readonly detail of the group, we don't have to worry about the highlight labels changing while editing on the client
                hlInactive.Visible = !group.IsActive;
                hlIsPrivate.Visible = !group.IsPublic;
            }
            else
            {
                // In edit mode, the labels will have javascript handle if/when they are shown
                hlInactive.Visible = true;
                hlIsPrivate.Visible = true;
            }

            hlArchived.Visible = group.IsArchived;

            if ( group.IsActive )
            {
                hlInactive.Style[HtmlTextWriterStyle.Display] = "none";
            }

            if ( group.IsPublic )
            {
                hlIsPrivate.Style[HtmlTextWriterStyle.Display] = "none";
            }
        }

        private string GetGroupCapacityHelpText( GroupCapacityRule groupCapacityRule )
        {
            if ( groupCapacityRule == GroupCapacityRule.Soft )
            {
                return "The number of people that can be added to the group.  Once the capacity is reached, a warning will appear in the Group Toolbox but additional group members can still be added.";
            }

            if ( groupCapacityRule == GroupCapacityRule.Hard )
            {
                return "The number of people that can be added to the group. Once the capacity is reached no additional group members can be added.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="group">The group.</param>
        private void ShowEditDetails( Group group )
        {
            if ( group.Id == 0 )
            {
                lReadOnlyTitle.Text = ActionTitle.Add( Group.FriendlyTypeName ).FormatAsHtmlTitle();

                // Hide the panel drawer that show created and last modified dates
                pdAuditDetails.Visible = false;
            }
            else
            {
                lReadOnlyTitle.Text = group.Name.FormatAsHtmlTitle();
            }

            SetHighlightLabelVisibility( group, false );

            ddlGroupType.Visible = group.Id == 0;
            lGroupType.Visible = group.Id != 0;

            SetEditMode( true );

            tbName.Text = group.Name;
            tbDescription.Text = group.Description;
            nbGroupCapacity.Text = group.GroupCapacity.ToString();
            nbGroupCapacity.Required = group.GroupType != null && group.GroupType.IsCapacityRequired;
            cbIsSecurityRole.Checked = group.IsSecurityRole;

            LoadElevatedSecurityRadioList();

            rblElevatedSecurityLevel.SelectedValue = group.ElevatedSecurityLevel.ConvertToInt().ToString();

            cbIsActive.Checked = group.IsActive;
            cbIsPublic.Checked = group.IsPublic;

            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var attributeService = new AttributeService( rockContext );

            if ( group.GroupType != null && group.GroupType.EnableInactiveReason )
            {
                ddlInactiveReason.Visible = true;
                ddlInactiveReason.Items.Add( new ListItem() );
                ddlInactiveReason.Required = group.GroupType.RequiresInactiveReason;

                foreach ( var reason in new GroupTypeService( rockContext ).GetInactiveReasonsForGroupType( group.GroupTypeId ).Select( r => new { Text = r.Value, Value = r.Id } ) )
                {
                    ddlInactiveReason.Items.Add( new ListItem( reason.Text, reason.Value.ToString() ) );
                }

                ddlInactiveReason.SelectedValue = group.InactiveReasonValueId.ToString();

                tbInactiveNote.Visible = true;
                tbInactiveNote.Text = group.InactiveReasonNote;
            }

            // The inactivate child groups checkbox should only be visible if there are children to inactivate .js on the page will consume this.
            hfHasChildGroups.Value = groupService.HasDescendantGroups( group.Id, false ) ? "true" : "false";

            LoadDropDowns( rockContext );

            ddlRsvpReminderSystemCommunication.SetValue( group.RSVPReminderSystemCommunicationId );

            rsRsvpReminderOffsetDays.SelectedValue = group.RSVPReminderOffsetDays;

            ddlSignatureDocumentTemplate.SetValue( group.RequiredSignatureDocumentTemplateId );
            gpParentGroup.SetValue( group.ParentGroup ?? groupService.Get( group.ParentGroupId ?? 0 ) );

            // Hide sync and requirements panel if no admin access
            bool canAdministrate = group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
            wpGroupSync.Visible = canAdministrate;
            wpGroupRequirements.Visible = canAdministrate;
            wpGroupMemberAttributes.Visible = canAdministrate;

            GroupSyncState = new List<GroupSyncViewModel>();
            foreach ( var sync in group.GroupSyncs )
            {
                var syncViewModel = new GroupSyncViewModel();
                syncViewModel.CopyPropertiesFrom( sync );

                // add the stuff that the grid needs
                syncViewModel.GroupTypeRole = new GroupTypeRoleService( rockContext ).Get( syncViewModel.GroupTypeRoleId );
                syncViewModel.SyncDataView = new DataViewService( rockContext ).Get( syncViewModel.SyncDataViewId );

                GroupSyncState.Add( syncViewModel );
            }

            BindGroupSyncGrid();

            // Only Rock admins can alter if the group is a security role
            cbIsSecurityRole.Visible = groupService.GroupHasMember( new Guid( Rock.SystemGuid.Group.GROUP_ADMINISTRATORS ), CurrentUser.PersonId );

            // GroupType depends on Selected ParentGroup
            ddlParentGroup_SelectedIndexChanged( null, null );
            gpParentGroup.Label = "Parent Group";

            if ( group.Id == 0 && group.GroupType == null && ddlGroupType.Items.Count > 1 )
            {
                if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
                {
                    // Default GroupType for new Group to "Security Roles"  if LimittoSecurityRoleGroups
                    var securityRoleGroupType = GroupTypeCache.GetSecurityRoleGroupType();
                    if ( securityRoleGroupType != null )
                    {
                        CurrentGroupTypeId = securityRoleGroupType.Id;
                        ddlGroupType.SetValue( securityRoleGroupType.Id );
                    }
                    else
                    {
                        ddlGroupType.SelectedIndex = 0;
                    }
                }
                else
                {
                    // If this is a new group (and not AttributeKey.LimitToSecurityRoleGroups, and there is more than one choice for GroupType, default to no selection so they are forced to choose (vs unintentionally choosing the default one)
                    ddlGroupType.SelectedIndex = 0;
                }
            }
            else
            {
                CurrentGroupTypeId = group.GroupTypeId;
                if ( CurrentGroupTypeId == 0 )
                {
                    CurrentGroupTypeId = ddlGroupType.SelectedValueAsInt() ?? 0;
                }

                var groupType = GroupTypeCache.Get( CurrentGroupTypeId, rockContext );
                lGroupType.Text = groupType != null ? groupType.Name : string.Empty;
                ddlGroupType.SetValue( CurrentGroupTypeId );
            }

            cpCampus.IncludeInactive = !GetAttributeValue( AttributeKey.PreventSelectingInactiveCampus ).AsBoolean();
            cpCampus.SelectedCampusId = group.CampusId;

            GroupRequirementsState = group.GetGroupRequirements( rockContext ).Where( a => a.GroupId.HasValue ).ToList();
            GroupLocationsState = group.GroupLocations.OrderBy( a => a.Order ).ThenBy( a => a.Location.Name ).ToList();

            var groupTypeCache = CurrentGroupTypeCache;
            BindAdministratorPerson( group, groupTypeCache );
            nbGroupCapacity.Visible = groupTypeCache != null && groupTypeCache.GroupCapacityRule != GroupCapacityRule.None;
            nbGroupCapacity.Help = nbGroupCapacity.Visible ? GetGroupCapacityHelpText( groupTypeCache.GroupCapacityRule ) : string.Empty;
            SetRecordSourceControls( groupTypeCache, group );
            SetPeerNetworkControls( groupTypeCache, group );
            SetRsvpControls( groupTypeCache, group );
            SetScheduleControls( groupTypeCache, group );
            ShowGroupTypeEditDetails( groupTypeCache, group, true );
            SetChatControls( groupTypeCache, group );

            cbSchedulingMustMeetRequirements.Checked = group.SchedulingMustMeetRequirements;
            cbDisableScheduleToolboxAccess.Checked = group.DisableScheduleToolboxAccess;
            cbDisableGroupScheduling.Checked = group.DisableScheduling;
            ddlAttendanceRecordRequiredForCheckIn.SetValue( group.AttendanceRecordRequiredForCheckIn.ConvertToInt() );
            ddlScheduleConfirmationLogic.SetValue( group.ScheduleConfirmationLogic.HasValue ? group.ScheduleConfirmationLogic.ConvertToInt().ToString() : null );

            foreach ( var li in cblScheduleCoordinatorNotificationTypes.Items.Cast<ListItem>() )
            {
                var notificationType = ( ScheduleCoordinatorNotificationType ) li.Value.AsInteger();
                if ( notificationType == ScheduleCoordinatorNotificationType.None )
                {
                    // "None" must be explicitly evaluated, otherwise the bitwise operator could return false positives.
                    li.Selected = group.ScheduleCoordinatorNotificationTypes == ScheduleCoordinatorNotificationType.None;
                }
                else
                {
                    li.Selected = ( group.ScheduleCoordinatorNotificationTypes & notificationType ) == notificationType;
                }
            }

            if ( group.ScheduleCoordinatorPersonAlias != null )
            {
                ppScheduleCoordinatorPerson.SetValue( group.ScheduleCoordinatorPersonAlias.Person );
            }
            else
            {
                ppScheduleCoordinatorPerson.SetValue( null );
            }

            // If this block's attribute limit group to SecurityRoleGroups, don't let them edit the SecurityRole checkbox value
            if ( GetAttributeValue( AttributeKey.LimittoSecurityRoleGroups ).AsBoolean() )
            {
                cbIsSecurityRole.Enabled = false;
                cbIsSecurityRole.Checked = true;
            }

            string qualifierValue = group.Id.ToString();
            GroupMemberAttributesState = attributeService.GetByEntityTypeId( new GroupMember().TypeId, true ).AsQueryable()
                    .Where( a =>
                        a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) &&
                        a.EntityTypeQualifierValue.Equals( qualifierValue ) )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name )
                    .ToList();
            BindGroupMemberAttributesGrid();

            BindInheritedAttributes( group.GroupTypeId, attributeService );

            BindGroupRequirementsGrid();

            MemberWorkflowTriggersState = new List<GroupMemberWorkflowTrigger>();
            foreach ( var trigger in group.GroupMemberWorkflowTriggers )
            {
                MemberWorkflowTriggersState.Add( trigger );
            }

            BindMemberWorkflowTriggersGrid();
        }

        private void LoadElevatedSecurityRadioList()
        {
            rblElevatedSecurityLevel.Items.Clear();
            foreach ( ElevatedSecurityLevel value in Enum.GetValues( typeof( ElevatedSecurityLevel ) ) )
            {
                rblElevatedSecurityLevel.Items.Add( new ListItem( value.ToString(), value.ConvertToInt().ToString() ) );
            }
        }

        /// <summary>
        /// Bind the administrator person picker.
        /// </summary>
        /// <param name="group">The group.</param>
        private void BindAdministratorPerson( Group group, GroupTypeCache groupType )
        {
            var showAdministrator = groupType != null && groupType.ShowAdministrator;
            ppAdministrator.Visible = showAdministrator;
            if ( showAdministrator )
            {
                ppAdministrator.Label = groupType.AdministratorTerm;
                ppAdministrator.Help = string.Format( "Provide the person who is the {0} of the group.", groupType.AdministratorTerm );
                if ( group.GroupAdministratorPersonAliasId.HasValue )
                {
                    ppAdministrator.SetValue( group.GroupAdministratorPersonAlias.Person );
                }
            }
        }

        /// <summary>
        /// Shows the group type edit details.
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="group">The group.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowGroupTypeEditDetails( GroupTypeCache groupType, Group group, bool setValues )
        {
            if ( group == null )
            {
                // Shouldn't happen
                return;
            }

            // Save value to viewstate for use later when binding location grid
            AllowMultipleLocations = groupType != null && groupType.AllowMultipleLocations;

            // Show/Hide different Panel based on permissions from the group type
            if ( group.GroupTypeId != 0 && setValues )
            {
                using ( var rockContext = new RockContext() )
                {
                    GroupType selectedGroupType = new GroupTypeService( rockContext ).Get( group.GroupTypeId );

                    if ( selectedGroupType != null )
                    {
                        if ( !wpGroupSync.Visible || group.Id == 0 )
                        {
                            wpGroupSync.Visible = selectedGroupType.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) && ( selectedGroupType.AllowGroupSync || GroupSyncState.Any() );
                        }

                        wpMemberWorkflowTriggers.Visible = selectedGroupType.AllowSpecificGroupMemberWorkflows || group.GroupMemberWorkflowTriggers.Any();
                    }
                }
            }

            if ( groupType != null )
            {
                nbGroupCapacity.Visible = groupType.GroupCapacityRule != GroupCapacityRule.None;
                nbGroupCapacity.Help = nbGroupCapacity.Visible ? GetGroupCapacityHelpText( groupType.GroupCapacityRule ) : string.Empty;

                if ( setValues )
                {
                    nbGroupCapacity.Text = group.GroupCapacity.ToString();
                    nbGroupCapacity.Required = groupType.IsCapacityRequired;
                }

                if ( cbIsSecurityRole.Checked || groupType.Guid == Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() )
                {
                    pnlElevatedSecurity.Visible = true;
                }
                else
                {
                    pnlElevatedSecurity.Visible = false;

                    if ( setValues )
                    {
                        rblElevatedSecurityLevel.SelectedValue = ElevatedSecurityLevel.None.ConvertToInt().ToString();
                    }
                }

                if ( setValues )
                {
                    dvpGroupStatus.DefinedTypeId = groupType.GroupStatusDefinedTypeId;
                    if ( groupType.GroupStatusDefinedType != null )
                    {
                        dvpGroupStatus.Label = groupType.GroupStatusDefinedType.ToString();
                    }

                    dvpGroupStatus.Visible = groupType.GroupStatusDefinedTypeId.HasValue;
                    dvpGroupStatus.SetValue( group.StatusValueId );
                }
            }
            else
            {
                dvpGroupStatus.Visible = false;
            }

            if ( groupType != null && groupType.LocationSelectionMode != GroupLocationPickerMode.None )
            {
                wpMeetingDetails.Visible = true;
                gGroupLocations.Visible = true;
                BindGroupLocationsGrid();
            }
            else
            {
                wpMeetingDetails.Visible = IsScheduleTabVisible;
                gGroupLocations.Visible = false;
            }

            gGroupLocations.Columns[2].Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );
            spSchedules.Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );

            if ( groupType != null && groupType.LocationSelectionMode != GroupLocationPickerMode.None )
            {
                wpMeetingDetails.Visible = true;
                gGroupLocations.Visible = true;
                BindGroupLocationsGrid();
            }
            else
            {
                wpMeetingDetails.Visible = IsScheduleTabVisible;
                gGroupLocations.Visible = false;
            }

            gGroupLocations.Columns[2].Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );
            spSchedules.Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );

            phGroupAttributes.Controls.Clear();
            group.LoadAttributes();

            if ( group.Attributes != null && group.Attributes.Any() )
            {
                wpGroupAttributes.Visible = true;
                var excludeForEdit = group.Attributes.Where( a => !a.Value.IsAuthorized( Rock.Security.Authorization.EDIT, this.CurrentPerson ) ).Select( a => a.Key ).ToList();
                Rock.Attribute.Helper.AddEditControls( group, phGroupAttributes, setValues, BlockValidationGroup, excludeForEdit );

                if ( excludeForEdit.Count() == group.Attributes.Count() )
                {
                    wpGroupAttributes.Visible = false;
                }
            }
            else
            {
                wpGroupAttributes.Visible = false;
            }

            wpScheduling.Visible = groupType != null && groupType.IsSchedulingEnabled;
        }

        /// <summary>
        /// Sets the schedule controls.
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="group">The group.</param>
        private void SetScheduleControls( GroupTypeCache groupType, Group group )
        {
            IsScheduleTabVisible = false;
            if ( group != null )
            {
                dowWeekly.SelectedDayOfWeek = null;
                timeWeekly.SelectedTime = null;
                sbSchedule.iCalendarContent = string.Empty;
                spSchedule.SetValue( null );

                if ( group.Schedule != null )
                {
                    switch ( group.Schedule.ScheduleType )
                    {
                        case ScheduleType.Named:
                            spSchedule.SetValue( group.Schedule );
                            break;
                        case ScheduleType.Custom:
                            hfUniqueScheduleId.Value = group.Schedule.Id.ToString();
                            sbSchedule.iCalendarContent = group.Schedule.iCalendarContent;
                            break;
                        case ScheduleType.Weekly:
                            hfUniqueScheduleId.Value = group.Schedule.Id.ToString();
                            dowWeekly.SelectedDayOfWeek = group.Schedule.WeeklyDayOfWeek;
                            timeWeekly.SelectedTime = group.Schedule.WeeklyTimeOfDay;
                            break;
                    }
                }
            }

            pnlSchedule.Visible = false;
            rblScheduleSelect.Items.Clear();

            ListItem liNone = new ListItem( "None", "0" );
            liNone.Selected = group != null && ( group.Schedule == null || group.Schedule.ScheduleType == ScheduleType.None );
            rblScheduleSelect.Items.Add( liNone );

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Weekly ) == ScheduleType.Weekly )
            {
                ListItem li = new ListItem( "Weekly", "1" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Weekly;
                rblScheduleSelect.Items.Add( li );
                pnlSchedule.Visible = IsScheduleTabVisible = true;
            }

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Custom ) == ScheduleType.Custom )
            {
                ListItem li = new ListItem( "Custom", "2" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Custom;
                rblScheduleSelect.Items.Add( li );
                pnlSchedule.Visible = IsScheduleTabVisible = true;
            }

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Named ) == ScheduleType.Named )
            {
                ListItem li = new ListItem( "Named", "4" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Named;
                rblScheduleSelect.Items.Add( li );
                pnlSchedule.Visible = IsScheduleTabVisible = true;
            }

            SetScheduleDisplay();
        }

        /// <summary>
        /// Sets the record source controls.
        /// </summary>
        /// <param name="groupType">The group type cache.</param>
        /// <param name="group">The group.</param>
        private void SetRecordSourceControls( GroupTypeCache groupType, Group group )
        {
            if ( groupType?.AllowGroupSpecificRecordSource == true )
            {
                // Setting the type here, as setting it in `LoadDropDowns()` wasn't reliably working.
                dvpRecordSource.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.RECORD_SOURCE_TYPE.AsGuid() )?.Id;
                dvpRecordSource.SetValue( group.GroupMemberRecordSourceValueId );
                dvpRecordSource.Visible = true;
            }
            else
            {
                dvpRecordSource.Visible = false;
            }
        }

        /// <summary>
        /// Sets the Peer Network controls.
        /// </summary>
        /// <param name="groupType">The group type cache.</param>
        /// <param name="group">The group.</param>
        private void SetPeerNetworkControls( GroupTypeCache groupType, Group group )
        {
            var isPeerNetworkOverridable = groupType?.IsPeerNetworkEnabled == true;

            pnlPeerNetworkOverride.Visible = isPeerNetworkOverridable;

            if ( !isPeerNetworkOverridable )
            {
                return;
            }

            cbOverrideRelationshipStrength.Checked = group.IsOverridingGroupTypePeerNetworkConfiguration;
            pnlPeerNetwork.Visible = group.IsOverridingGroupTypePeerNetworkConfiguration;

            // For relationship strength and growth settings, start by checking if a value is defined for this group,
            // and fall back to the values defined at the group type level.
            var relationshipStrength = group.RelationshipStrengthOverride ?? groupType.RelationshipStrength;
            rblRelationshipStrength.SetValue( relationshipStrength );

            cbEnableRelationshipGrowth.Checked = group.RelationshipGrowthEnabledOverride ?? groupType.RelationshipGrowthEnabled;

            var showPeerNetworkAdvancedSettings = groupType.AreAnyRelationshipMultipliersCustomized || group.AreAnyRelationshipMultipliersCustomized;
            swShowPeerNetworkAdvancedSettings.Checked = showPeerNetworkAdvancedSettings;
            pnlPeerNetworkAdvanced.Visible = showPeerNetworkAdvancedSettings;

            // For relationship multipliers, set the group type's values as placeholders on the textboxes, while setting
            // the group's values as the actual values. This is because the stored procedure that's used to calculate
            // role-based strengths checks for each individual group-based multiplier override (rather than considering
            // them ALL to be overridden as a set). By showing the group type multiplier values as placeholders, the
            // admin can easily see which values they want to override without having to go back to the group type config.
            tbLeaderToLeaderRelationshipMultiplier.Placeholder = groupType.LeaderToLeaderRelationshipMultiplier.FormatAsPercent();
            tbLeaderToLeaderRelationshipMultiplier.Text = group.LeaderToLeaderRelationshipMultiplierOverride.HasValue
                ? group.LeaderToLeaderRelationshipMultiplierOverride.Value.FormatAsPercent()
                : null;

            tbLeaderToNonLeaderRelationshipMultiplier.Placeholder = groupType.LeaderToNonLeaderRelationshipMultiplier.FormatAsPercent();
            tbLeaderToNonLeaderRelationshipMultiplier.Text = group.LeaderToNonLeaderRelationshipMultiplierOverride.HasValue
                ? group.LeaderToNonLeaderRelationshipMultiplierOverride.Value.FormatAsPercent()
                : null;

            tbNonLeaderToLeaderRelationshipMultiplier.Placeholder = groupType.NonLeaderToLeaderRelationshipMultiplier.FormatAsPercent();
            tbNonLeaderToLeaderRelationshipMultiplier.Text = group.NonLeaderToLeaderRelationshipMultiplierOverride.HasValue
                ? group.NonLeaderToLeaderRelationshipMultiplierOverride.Value.FormatAsPercent()
                : null;

            tbNonLeaderToNonLeaderRelationshipMultiplier.Placeholder = groupType.NonLeaderToNonLeaderRelationshipMultiplier.FormatAsPercent();
            tbNonLeaderToNonLeaderRelationshipMultiplier.Text = group.NonLeaderToNonLeaderRelationshipMultiplierOverride.HasValue
                ? group.NonLeaderToNonLeaderRelationshipMultiplierOverride.Value.FormatAsPercent()
                : null;

            SetPeerNetworkSubControlVisibility( relationshipStrength, showPeerNetworkAdvancedSettings );
        }

        /// <summary>
        /// Sets the visibility of peer network secondary controls, based on the provided relationship strength.
        /// </summary>
        /// <param name="relationshipStrength">The relationship strength.</param>
        /// <param name="isAdvancedPanelVisible">Whether the peer network advanced panel is currently visible.</param>
        private void SetPeerNetworkSubControlVisibility( int relationshipStrength, bool isAdvancedPanelVisible )
        {
            var isVisible = relationshipStrength != 0;

            pnlRelationshipGrowth.Visible = isVisible;
            pnlShowPeerNetworkAdvancedSettings.Visible = isVisible;
            pnlPeerNetworkAdvanced.Visible = isAdvancedPanelVisible && isVisible;
        }

        /// <summary>
        /// Sets the RSVP controls.
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="group">The group.</param>
        private void SetRsvpControls( GroupTypeCache groupType, Group group )
        {
            bool showRsvp = false;
            int? offsetDays = 0;
            int? reminderSystemCommunicationId = null;
            bool isReadOnly_Offset = true;
            bool isReadOnly_Reminder = true;

            if ( groupType != null )
            {
                showRsvp = groupType.EnableRSVP;

                if ( showRsvp )
                {
                    // Assign default values.
                    rsRsvpReminderOffsetDays.Enabled = false;
                    ddlRsvpReminderSystemCommunication.Enabled = false;
                    offsetDays = groupType.RSVPReminderOffsetDays;
                    reminderSystemCommunicationId = groupType.RSVPReminderSystemCommunicationId;

                    // If a specific RSVP Communication Template has been assigned for this Group Type, the RSVP settings for
                    // Groups of this type are read-only.
                    isReadOnly_Offset = groupType.RSVPReminderOffsetDays.HasValue;
                    isReadOnly_Reminder = groupType.RSVPReminderSystemCommunicationId.HasValue;

                    if ( !isReadOnly_Offset )
                    {
                        rsRsvpReminderOffsetDays.Enabled = true;
                        offsetDays = ( group != null ) ? group.RSVPReminderOffsetDays : 0;
                    }

                    if ( !isReadOnly_Reminder )
                    {
                        ddlRsvpReminderSystemCommunication.Enabled = true;
                        reminderSystemCommunicationId = ( group != null ) ? group.RSVPReminderSystemCommunicationId : null;
                    }
                }
            }

            wpRsvp.Visible = showRsvp;
            rsRsvpReminderOffsetDays.SelectedValue = offsetDays.GetValueOrDefault( 0 );
            ddlRsvpReminderSystemCommunication.SetValue( reminderSystemCommunicationId );
        }

        /// <summary>
        /// Sets the chat controls.
        /// </summary>
        /// <param name="groupType">The group type cache.</param>
        /// <param name="group">The group.</param>
        private void SetChatControls( GroupTypeCache groupType, Group group )
        {
            if ( ChatHelper.IsChatEnabled && groupType?.IsChatAllowed == true )
            {
                var isChatEnabled = group.IsChatEnabledOverride.HasValue
                    ? group.IsChatEnabledOverride.Value ? "y" : "n"
                    : string.Empty;

                var isLeavingChatChannelAllowed = group.IsLeavingChatChannelAllowedOverride.HasValue
                    ? group.IsLeavingChatChannelAllowedOverride.Value ? "y" : "n"
                    : string.Empty;

                var isChatChannelPublic = group.IsChatChannelPublicOverride.HasValue
                    ? group.IsChatChannelPublicOverride.Value ? "y" : "n"
                    : string.Empty;

                var isChatChannelAlwaysShown = group.IsChatChannelAlwaysShownOverride.HasValue
                    ? group.IsChatChannelAlwaysShownOverride.Value ? "y" : "n"
                    : string.Empty;

                var chatPushNotificationMode = group.ChatPushNotificationModeOverride.HasValue
                    ? group.ChatPushNotificationModeOverride.Value.ConvertToInt()
                    : ( int? ) null;

                ddlIsChatEnabled.SetValue( isChatEnabled );
                ddlIsLeavingChatChannelAllowed.SetValue( isLeavingChatChannelAllowed );
                ddlIsChatChannelPublic.SetValue( isChatChannelPublic );
                ddlIsChatChannelAlwaysShown.SetValue( isChatChannelAlwaysShown );
                ddlChatPushNotificationMode.SetValue( chatPushNotificationMode );

                imgChatChannelAvatar.BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT.AsGuid();
                imgChatChannelAvatar.BinaryFileId = group.ChatChannelAvatarBinaryFileId;

                if ( group.IsSystem )
                {
                    ddlIsChatEnabled.Enabled = false;
                    ddlIsLeavingChatChannelAllowed.Enabled = false;
                    ddlIsChatChannelPublic.Enabled = false;
                    ddlIsChatChannelAlwaysShown.Enabled = false;
                    ddlChatPushNotificationMode.Enabled = false;
                    imgChatChannelAvatar.Enabled = false;
                }

                wpChat.Visible = true;
            }
            else
            {
                wpChat.Visible = false;
            }
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="group">The group.</param>
        private void ShowReadonlyDetails( Group group )
        {
            btnDelete.Visible = !group.IsSystem && group.IsAuthorized( Authorization.EDIT, CurrentPerson );
            btnArchive.Visible = false;

            var rockContext = new RockContext();
            GroupTypeCache groupType = GroupTypeCache.Get( group.GroupTypeId );

            // If History is enabled (and this isn't an IsSystem group), additional logic for if the Archive or Delete button is visible
            if ( !group.IsSystem )
            {
                if ( !group.IsArchived )
                {
                    if ( groupType != null && groupType.EnableGroupHistory )
                    {
                        bool hasGroupHistory = new GroupHistoricalService( rockContext ).Queryable().Any( a => a.GroupId == group.Id )
                            || new GroupMemberHistoricalService( rockContext ).Queryable().Any( a => a.GroupId == group.Id );
                        if ( hasGroupHistory )
                        {
                            // If the group has GroupHistory enabled, and has group history snapshots, prompt to archive instead of delete
                            btnDelete.Visible = false;

                            // Show the archive button if the user is authorized to see it.
                            btnArchive.Visible = !group.IsSystem && group.IsAuthorized( Authorization.EDIT, CurrentPerson );
                        }
                    }
                }
                else
                {
                    btnDelete.Visible = false;
                }
            }

            SetHighlightLabelVisibility( group, true );
            SetEditMode( false );

            string groupIconHtml = string.Empty;
            if ( groupType != null )
            {
                groupIconHtml = !string.IsNullOrWhiteSpace( groupType.IconCssClass ) ?
                    string.Format( "<i class='{0}' ></i>", groupType.IconCssClass ) : string.Empty;

                if ( groupType.IsPeerNetworkEnabled )
                {
                    var groupTypeRelationshipStrength = groupType.RelationshipStrength;
                    var groupRelationshipStrength = group.RelationshipStrengthOverride;

                    var isRelationshipStrengthOverridden = groupRelationshipStrength.HasValue
                        && groupRelationshipStrength.Value != groupTypeRelationshipStrength;

                    var finalRelationshipStrength = isRelationshipStrengthOverridden
                        ? groupRelationshipStrength.Value
                        : groupTypeRelationshipStrength;

                    var strengthLabel = GetRelationshipStrengthLabel( finalRelationshipStrength );

                    var isRelationshipGrowthEnabled = group.RelationshipGrowthEnabledOverride.HasValue
                        ? group.RelationshipGrowthEnabledOverride.Value
                        : groupType.RelationshipGrowthEnabled;

                    var relationshipGrowthTooltip = string.Empty;
                    var relationshipOverrideTooltip = string.Empty;

                    var relationshipLabelIconsSb = new StringBuilder();

                    // Only show the growth icon and tooltip if growth enabled AND the strength is not "None".
                    if ( isRelationshipGrowthEnabled && finalRelationshipStrength > 0 )
                    {
                        relationshipLabelIconsSb.Append( $@" <i class=""fa fa-chart-line""></i>" );

                        relationshipGrowthTooltip = " The relationship is also set to strengthen over time.";
                    }

                    // Show the overridden icon if this group is overriding its parent group type's peer network configuration in any way.
                    if ( group.IsOverridingGroupTypePeerNetworkConfiguration )
                    {
                        relationshipLabelIconsSb.Append( $@" <i class=""fa fa-star-of-life""></i>" );
                    }

                    // Only show the overridden tooltip if the relationship strength itself has been overridden.
                    if ( isRelationshipStrengthOverridden )
                    {
                        var overriddenStrengthLabel = GetRelationshipStrengthLabel( groupTypeRelationshipStrength );

                        relationshipOverrideTooltip = $", overriding the group type's default setting of{( overriddenStrengthLabel.Article.IsNotNullOrWhiteSpace() ? $" {overriddenStrengthLabel.Article}" : string.Empty )} {overriddenStrengthLabel.Relationship} relationship";
                    }

                    hlPeerNetwork.Text = $"{strengthLabel.Relationship.Titleize()} Relationships{relationshipLabelIconsSb}";
                    hlPeerNetwork.ToolTip = $"Individuals in this group share{( strengthLabel.Article.IsNotNullOrWhiteSpace() ? $" {strengthLabel.Article}" : string.Empty )} {strengthLabel.Relationship} relationship{relationshipOverrideTooltip}.{relationshipGrowthTooltip}";

                    hlPeerNetwork.Visible = true;
                }
                else
                {
                    hlPeerNetwork.Visible = false;
                }

                if ( ChatHelper.IsChatEnabled && group.GetIsChatEnabled() )
                {
                    hlChat.Text = $"Chat-Enabled <i class=\"fa fa-comments-o\"></i>";
                    hlChat.Visible = true;
                }
                else
                {
                    hlChat.Visible = false;
                }

                if ( groupType.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) )
                {
                    var groupTypeDetailPage = new PageReference( Rock.SystemGuid.Page.GROUP_TYPE_DETAIL ).BuildUrl();
                    hlType.Text = string.Format( "<a href='{0}?GroupTypeId={1}'>{2}</a>", groupTypeDetailPage, groupType.Id, groupType.Name );
                }
                else
                {
                    hlType.Text = groupType.Name;
                }

                hlType.ToolTip = groupType.Description;
            }

            hfGroupId.SetValue( group.Id );
            lGroupIconHtml.Text = groupIconHtml;
            lReadOnlyTitle.Text = group.Name.FormatAsHtmlTitle();

            pdAuditDetails.SetEntity( group, ResolveRockUrl( "~" ) );

            if ( group.Campus != null )
            {
                hlCampus.Visible = true;
                hlCampus.Text = group.Campus.Name;
            }
            else
            {
                hlCampus.Visible = false;
            }

            if ( group.IsSecurityRole && group.ElevatedSecurityLevel > ElevatedSecurityLevel.None )
            {
                hlElevatedSecurityLevel.Visible = true;
                hlElevatedSecurityLevel.Text = $"Security Level: {group.ElevatedSecurityLevel.ConvertToString( true )}";
                if ( group.ElevatedSecurityLevel == ElevatedSecurityLevel.Extreme )
                {
                    hlElevatedSecurityLevel.LabelType = LabelType.Danger;
                }
                else
                {
                    hlElevatedSecurityLevel.LabelType = LabelType.Warning;
                }
            }
            else
            {
                hlElevatedSecurityLevel.Visible = true;
            }

            var pageParams = new Dictionary<string, string>();
            pageParams.Add( PageParameterKey.GroupId, group.Id.ToString() );

            hlAttendance.Visible = groupType != null && groupType.TakesAttendance;
            hlAttendance.NavigateUrl = LinkedPageUrl( AttributeKey.AttendancePage, pageParams );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Group", group );
            mergeFields.Add( AttributeKey.RegistrationInstancePage, LinkedPageRoute( AttributeKey.RegistrationInstancePage ) );
            mergeFields.Add( AttributeKey.EventItemOccurrencePage, LinkedPageRoute( AttributeKey.EventItemOccurrencePage ) );
            mergeFields.Add( AttributeKey.ContentItemPage, LinkedPageRoute( AttributeKey.ContentItemPage ) );
            mergeFields.Add( AttributeKey.ShowLocationAddresses, GetAttributeValue( AttributeKey.ShowLocationAddresses ).AsBoolean() );

            var mapStyleValue = DefinedValueCache.Get( GetAttributeValue( AttributeKey.MapStyle ) );
            if ( mapStyleValue == null )
            {
                mapStyleValue = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK );
            }

            mergeFields.Add( AttributeKey.MapStyle, mapStyleValue );

            string groupMapUrl = LinkedPageUrl( AttributeKey.GroupMapPage, pageParams );
            mergeFields.Add( "GroupMapUrl", groupMapUrl );

            if ( groupMapUrl.IsNotNullOrWhiteSpace() )
            {
                hlMap.Visible = true;
                hlMap.NavigateUrl = groupMapUrl;
            }
            else
            {
                hlMap.Visible = false;
            }

            string groupRSVPUrl = LinkedPageUrl( AttributeKey.GroupRSVPPage, pageParams );
            if ( groupRSVPUrl.IsNotNullOrWhiteSpace() )
            {
                hlGroupRSVP.Visible = ( groupType != null ) && ( groupType.EnableRSVP == true );
                hlGroupRSVP.NavigateUrl = groupRSVPUrl;
            }
            else
            {
                hlGroupRSVP.Visible = false;
            }

            string groupSchedulerUrl = LinkedPageUrl( AttributeKey.GroupSchedulerPage, pageParams );
            if ( groupSchedulerUrl.IsNotNullOrWhiteSpace() )
            {
                hlGroupScheduler.Visible = groupType != null && groupType.IsSchedulingEnabled;
                if ( group.DisableScheduling )
                {
                    hlGroupScheduler.Enabled = false;
                }
                else
                {
                    hlGroupScheduler.NavigateUrl = groupSchedulerUrl;
                    hlGroupScheduler.Enabled = true;
                }
            }
            else
            {
                hlGroupScheduler.Visible = false;
            }

            string groupHistoryUrl = LinkedPageUrl( AttributeKey.GroupHistoryPage, pageParams );
            mergeFields.Add( "GroupHistoryUrl", groupHistoryUrl );
            if ( groupHistoryUrl.IsNotNullOrWhiteSpace() )
            {
                hlGroupHistory.Visible = groupType != null && groupType.EnableGroupHistory;
                hlGroupHistory.NavigateUrl = groupHistoryUrl;
            }
            else
            {
                hlGroupHistory.Visible = false;
            }

            if ( groupType != null )
            {
                string template = groupType.GroupViewLavaTemplate;
                lContent.Text = template.ResolveMergeFields( mergeFields ).ResolveClientIds( upnlGroupDetail.ClientID );
            }

            string fundraisingProgressUrl = LinkedPageUrl( AttributeKey.FundraisingProgressPage, pageParams );
            var groupTypeIdFundraising = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FUNDRAISINGOPPORTUNITY.AsGuid() ).Id;
            var fundraisingGroupTypeIdList = new GroupTypeService( rockContext ).Queryable().Where( a => a.Id == groupTypeIdFundraising || a.InheritedGroupTypeId == groupTypeIdFundraising ).Select( a => a.Id ).ToList();

            if ( fundraisingProgressUrl.IsNotNullOrWhiteSpace() && fundraisingGroupTypeIdList.Contains( group.GroupTypeId ) )
            {
                hlFundraisingProgress.NavigateUrl = fundraisingProgressUrl;
                hlFundraisingProgress.Visible = true;
            }
            else
            {
                hlFundraisingProgress.Visible = false;
            }

            btnSecurity.Visible = group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
            btnSecurity.EntityId = group.Id;
        }

        /// <summary>
        /// A POCO to provide a friendly label for a given relationship strength.
        /// </summary>
        private class RelationshipStrengthLabel
        {
            /// <summary>
            /// The indefinite article to use when describing this relationship.
            /// </summary>
            public string Article { get; set; }

            /// <summary>
            /// The friendly relationship label.
            /// </summary>
            public string Relationship { get; set; }
        }

        /// <summary>
        /// Gets a friendly label for the provided relationship strength.
        /// </summary>
        /// <param name="strength">The integer representation of the relationship strength.</param>
        /// <returns>A friendly label for the provided relationship strength.</returns>
        private RelationshipStrengthLabel GetRelationshipStrengthLabel( int strength )
        {
            var label = new RelationshipStrengthLabel();

            var relationshipStrength = strength.ToString().ConvertToEnumOrNull<RelationshipStrength>();
            if ( relationshipStrength == null )
            {
                // Since the db holds an int value, it's possible someone could manually set an unrepresented value here
                // (and the stored procedure performing peer network calculations would still work just fine). Let's at
                // least use this opportunity to point out to the admin that an unknown strength value is in place.
                label.Article = "an";
                label.Relationship = "unknown";
            }
            else
            {
                switch ( relationshipStrength.Value )
                {
                    case RelationshipStrength.None:
                        label.Article = null;
                        label.Relationship = "no";
                        break;
                    default:
                        label.Article = "a";
                        label.Relationship = relationshipStrength.Value.ConvertToString().ToLower();
                        break;
                }
            }

            return label;
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewDetails.Visible = !editable;
            this.HideSecondaryBlocks( editable );
        }

        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        private Group GetGroup( int groupId, RockContext rockContext = null )
        {
            string key = string.Format( "Group:{0}", groupId );
            Group group = RockPage.GetSharedItem( key ) as Group;
            if ( group == null )
            {
                rockContext = rockContext ?? new RockContext();
                group = new GroupService( rockContext )
                    .Queryable()
                    .Include( g => g.GroupType )
                    .Include( g => g.GroupLocations.Select( s => s.Schedules ) )
                    .Include( g => g.GroupSyncs )
                    .Where( g => g.Id == groupId )
                    .FirstOrDefault();
                RockPage.SaveSharedItem( key, group );
            }

            return group;
        }

        /// <summary>
        /// Gets the allowed group types.
        /// </summary>
        /// <param name="parentGroupType">Type of the parent group.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private IQueryable<GroupType> GetAllowedGroupTypes( GroupTypeCache parentGroupGroupType, RockContext rockContext )
        {
            rockContext = rockContext ?? new RockContext();

            GroupTypeService groupTypeService = new GroupTypeService( rockContext );

            var groupTypeQry = groupTypeService.Queryable();

            // Limit GroupType selection to what Block Attributes allow
            List<Guid> groupTypeIncludeGuids = GetAttributeValue( AttributeKey.GroupTypesInclude ).SplitDelimitedValues().AsGuidList();
            List<Guid> groupTypeExcludeGuids = GetAttributeValue( AttributeKey.GroupTypesExclude ).SplitDelimitedValues().AsGuidList();
            if ( groupTypeIncludeGuids.Any() )
            {
                groupTypeQry = groupTypeQry.Where( a => groupTypeIncludeGuids.Contains( a.Guid ) );
            }
            else if ( groupTypeExcludeGuids.Any() )
            {
                groupTypeQry = groupTypeQry.Where( a => !groupTypeExcludeGuids.Contains( a.Guid ) );
            }

            // Next, limit GroupType to ChildGroupTypes that the ParentGroup allows
            if ( parentGroupGroupType != null )
            {
                if ( !parentGroupGroupType.AllowAnyChildGroupType )
                {
                    List<int> allowedChildGroupTypeIds = parentGroupGroupType.ChildGroupTypes.Select( a => a.Id ).ToList();
                    groupTypeQry = groupTypeQry.Where( a => allowedChildGroupTypeIds.Contains( a.Id ) );
                }
            }

            // Limit to GroupTypes where ShowInNavigation=True depending on block setting
            if ( GetAttributeValue( AttributeKey.LimitToShowInNavigationGroupTypes ).AsBoolean() )
            {
                groupTypeQry = groupTypeQry.Where( a => a.ShowInNavigation );
            }

            return groupTypeQry;
        }

        /// <summary>
        /// Registrations the instance URL.
        /// </summary>
        /// <param name="registrationInstanceId">The registration instance identifier.</param>
        /// <returns></returns>
        protected string RegistrationInstanceUrl( int registrationInstanceId )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( "RegistrationInstanceId", registrationInstanceId.ToString() );
            return LinkedPageUrl( AttributeKey.RegistrationInstancePage, qryParams );
        }

        /// <summary>
        /// Events the item occurrence URL.
        /// </summary>
        /// <param name="eventItemOccurrenceId">The event item occurrence identifier.</param>
        /// <returns></returns>
        protected string EventItemOccurrenceUrl( int eventItemOccurrenceId )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( PageParameterKey.EventItemOccurrenceId, eventItemOccurrenceId.ToString() );
            return LinkedPageUrl( AttributeKey.EventItemOccurrencePage, qryParams );
        }

        /// <summary>
        /// Contents the item URL.
        /// </summary>
        /// <param name="contentItemId">The content item identifier.</param>
        /// <returns></returns>
        protected string ContentItemUrl( int contentItemId )
        {
            var qryParams = new Dictionary<string, string>();
            qryParams.Add( "ContentItemId", contentItemId.ToString() );
            return LinkedPageUrl( AttributeKey.ContentItemPage, qryParams );
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns( RockContext rockContext )
        {
            // Populate Signature Document Templates
            ddlSignatureDocumentTemplate.Items.Clear();

            ddlSignatureDocumentTemplate.Items.Add( new ListItem() );

            foreach ( var documentType in new SignatureDocumentTemplateService( rockContext ).GetLegacyTemplates() )
            {
                ddlSignatureDocumentTemplate.Items.Add( new ListItem( documentType.Name, documentType.Id.ToString() ) );
            }

            // Populate RSVP Reminder Communication Templates
            ddlRsvpReminderSystemCommunication.Items.Clear();

            ddlRsvpReminderSystemCommunication.Items.Add( new ListItem() );

            var communicationService = new SystemCommunicationService( rockContext );

            var rsvpReminderCategoryId = CategoryCache.GetId( Rock.SystemGuid.Category.SYSTEM_COMMUNICATION_RSVP_CONFIRMATION.AsGuid() );
            var rsvpReminderCommunications = communicationService.Queryable()
                .AsNoTracking()
                .Where( c => c.CategoryId == rsvpReminderCategoryId )
                .OrderBy( t => t.Title )
                .Select( a => new
                {
                    a.Id,
                    a.Title
                } );

            foreach ( var rsvpReminder in rsvpReminderCommunications )
            {
                ddlRsvpReminderSystemCommunication.Items.Add( new ListItem( rsvpReminder.Title, rsvpReminder.Id.ToString() ) );
            }

            ddlAttendanceRecordRequiredForCheckIn.BindToEnum<AttendanceRecordRequiredForCheckIn>();
            ddlScheduleConfirmationLogic.BindToEnum<ScheduleConfirmationLogic>( true );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( string dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ToUpper().Trim();
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            switch ( hfActiveDialog.Value )
            {
                case "LOCATIONS":
                    dlgLocations.Show();
                    break;
                case "GROUPMEMBERATTRIBUTES":
                    dlgGroupMemberAttribute.Show();
                    break;
                case "GROUPREQUIREMENTS":
                    mdGroupRequirement.Show();
                    break;
                case "MEMBERWORKFLOWTRIGGERS":
                    dlgMemberWorkflowTriggers.Show();
                    break;
                case "GROUPSYNCSETTINGS":
                    mdGroupSyncSettings.Show();
                    break;
            }
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        private void HideDialog()
        {
            switch ( hfActiveDialog.Value )
            {
                case "LOCATIONS":
                    dlgLocations.Hide();
                    break;
                case "GROUPMEMBERATTRIBUTES":
                    dlgGroupMemberAttribute.Hide();
                    break;
                case "GROUPREQUIREMENTS":
                    mdGroupRequirement.Hide();
                    break;
                case "MEMBERWORKFLOWTRIGGERS":
                    dlgMemberWorkflowTriggers.Hide();
                    break;
                case "GROUPSYNCSETTINGS":
                    mdGroupSyncSettings.Hide();
                    break;
            }

            hfActiveDialog.Value = string.Empty;
        }

        /// <summary>
        /// Gets the tab class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        protected string GetTabClass( object property )
        {
            if ( property.ToString() == LocationTypeTab )
            {
                return "active";
            }

            return string.Empty;
        }

        /// <summary>
        /// Shows the selected pane.
        /// </summary>
        private void ShowSelectedPane()
        {
            if ( LocationTypeTab.Equals( MEMBER_LOCATION_TAB_TITLE ) )
            {
                pnlMemberSelect.Visible = true;
                pnlLocationSelect.Visible = false;
            }
            else if ( LocationTypeTab.Equals( OTHER_LOCATION_TAB_TITLE ) )
            {
                pnlMemberSelect.Visible = false;
                pnlLocationSelect.Visible = true;
            }
        }

        /// <summary>
        /// Binds the inherited attributes.
        /// </summary>
        /// <param name="inheritedGroupTypeId">The inherited group type identifier.</param>
        /// <param name="attributeService">The attribute service.</param>
        private void BindInheritedAttributes( int? inheritedGroupTypeId, AttributeService attributeService )
        {
            GroupMemberAttributesInheritedState = new List<InheritedAttribute>();
            GroupDateAttributesState = new List<Attribute>();

            while ( inheritedGroupTypeId.HasValue )
            {
                var inheritedGroupType = GroupTypeCache.Get( inheritedGroupTypeId.Value );
                if ( inheritedGroupType != null )
                {
                    string qualifierValue = inheritedGroupType.Id.ToString();
                    foreach ( var attribute in attributeService.GetByEntityTypeId( new GroupMember().TypeId, false ).AsQueryable()
                        .Where( a =>
                            a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                            a.EntityTypeQualifierValue.Equals( qualifierValue ) )
                        .OrderBy( a => a.Order )
                        .ThenBy( a => a.Name )
                        .ToList() )
                    {
                        GroupMemberAttributesInheritedState.Add( new InheritedAttribute(
                            attribute.Name,
                            attribute.Key,
                            attribute.Description,
                            Page.ResolveUrl( "~/GroupType/" + attribute.EntityTypeQualifierValue ),
                            inheritedGroupType.Name ) );
                    }

                    // Get group attributes with a date or datetime field.
                    GroupDateAttributesState.AddRange( attributeService.GetByEntityTypeId( new Group().TypeId, true ).AsQueryable()
                        .Where( a =>
                            a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) &&
                            a.EntityTypeQualifierValue.Equals( qualifierValue ) &&
                            DateFieldTypeIds.Contains( a.FieldTypeId ) )
                        .OrderBy( a => a.Order )
                        .ThenBy( a => a.Name )
                        .ToList() );

                    inheritedGroupTypeId = inheritedGroupType.InheritedGroupTypeId;
                }
                else
                {
                    inheritedGroupTypeId = null;
                }
            }

            BindGroupMemberAttributesInheritedGrid();
        }

        /// <summary>
        /// Sets the attribute list order.
        /// </summary>
        /// <param name="attributeList">The attribute list.</param>
        private void SetAttributeListOrder( List<Attribute> attributeList )
        {
            int order = 0;
            attributeList.OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList().ForEach( a => a.Order = order++ );
        }

        /// <summary>
        /// Reorders the attribute list.
        /// </summary>
        /// <param name="itemList">The item list.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        private void ReorderAttributeList( List<Attribute> itemList, int oldIndex, int newIndex )
        {
            var movedItem = itemList.Where( a => a.Order == oldIndex ).FirstOrDefault();
            if ( movedItem != null )
            {
                if ( newIndex < oldIndex )
                {
                    // Moved up
                    foreach ( var otherItem in itemList.Where( a => a.Order < oldIndex && a.Order >= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order + 1;
                    }
                }
                else
                {
                    // Moved Down
                    foreach ( var otherItem in itemList.Where( a => a.Order > oldIndex && a.Order <= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order - 1;
                    }
                }

                movedItem.Order = newIndex;
            }
        }

        /// <summary>
        /// Sets the group type role list order.
        /// </summary>
        /// <param name="itemList">The item list.</param>
        private void SetMemberWorkflowTriggerListOrder( List<GroupMemberWorkflowTrigger> itemList )
        {
            int order = 0;
            itemList.OrderBy( a => a.Order ).ToList().ForEach( a => a.Order = order++ );
        }

        /// <summary>
        /// Reorders the group type role list.
        /// </summary>
        /// <param name="itemList">The item list.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        private void ReorderMemberWorkflowTriggerList( List<GroupMemberWorkflowTrigger> itemList, int oldIndex, int newIndex )
        {
            var movedItem = itemList.Where( a => a.Order == oldIndex ).FirstOrDefault();
            if ( movedItem != null )
            {
                if ( newIndex < oldIndex )
                {
                    // Moved up
                    foreach ( var otherItem in itemList.Where( a => a.Order < oldIndex && a.Order >= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order + 1;
                    }
                }
                else
                {
                    // Moved Down
                    foreach ( var otherItem in itemList.Where( a => a.Order > oldIndex && a.Order <= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order - 1;
                    }
                }

                movedItem.Order = newIndex;
            }
        }

        /// <summary>
        /// Handles the SelectItem event of the SpSchedules control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void spSchedules_SelectItem( object sender, EventArgs e )
        {
            if ( !LocationSelected() )
            {
                nbGroupLocationEditMessage.Text = "Please select a location.";
                nbGroupLocationEditMessage.Visible = true;
                spSchedules.SetValue( 0 );
                return;
            }

            var rockContext = new RockContext();

            var selectedScheduleIds = spSchedules.SelectedValuesAsInt().ToList();

            // Get the selected schedules
            var schedules = new ScheduleService( rockContext ).GetByIds( selectedScheduleIds ).ToList();

            var groupLocationGuid = hfGroupLocationGuid.Value.AsGuid();

            List<GroupLocationScheduleConfig> currentGroupLocationScheduleConfigs = new List<GroupLocationScheduleConfig>();

            // Get the displayed GroupLocationScheduleConfigs from Controls in the repeater for the currently selected schedules.
            foreach ( var repeaterItem in rptGroupLocationScheduleCapacities.Items.OfType<RepeaterItem>() )
            {
                var hfScheduleId = repeaterItem.FindControl( "hfScheduleId" ) as HiddenField;
                int scheduleId = hfScheduleId.Value.AsInteger();
                if ( selectedScheduleIds.Contains( scheduleId ) )
                {
                    var nbMinimumCapacity = repeaterItem.FindControl( "nbMinimumCapacity" ) as NumberBox;
                    var nbDesiredCapacity = repeaterItem.FindControl( "nbDesiredCapacity" ) as NumberBox;
                    var nbMaximumCapacity = repeaterItem.FindControl( "nbMaximumCapacity" ) as NumberBox;

                    currentGroupLocationScheduleConfigs.Add( new GroupLocationScheduleConfig
                    {
                        ScheduleId = scheduleId,
                        Schedule = schedules.First( s => s.Id == scheduleId ),
                        MinimumCapacity = nbMinimumCapacity.Text.AsIntegerOrNull(),
                        DesiredCapacity = nbDesiredCapacity.Text.AsIntegerOrNull(),
                        MaximumCapacity = nbMaximumCapacity.Text.AsIntegerOrNull(),
                    } );
                }
            }

            // add any schedules that weren't shown in the repeater
            foreach ( var schedule in schedules.Where( s => s.IsActive ).Where( s => !currentGroupLocationScheduleConfigs.Any( x => x.ScheduleId == s.Id ) ) )
            {
                currentGroupLocationScheduleConfigs.Add( new GroupLocationScheduleConfig
                {
                    ScheduleId = schedule.Id,
                    Schedule = schedule
                } );
            }

            BindGroupLocationScheduleCapacities( currentGroupLocationScheduleConfigs );
        }

        /// <summary>
        /// Binds the group location schedule capacities.
        /// </summary>
        /// <param name="currentGroupLocationScheduleConfigs">The current group location schedule configs.</param>
        private void BindGroupLocationScheduleCapacities( List<GroupLocationScheduleConfig> currentGroupLocationScheduleConfigs )
        {
            // Calculate the Next Start Date Time based on the start of the week so that schedules are in the correct order
            var occurrenceDate = RockDateTime.Now.SundayDate().AddDays( 1 );

            rptGroupLocationScheduleCapacities.DataSource = currentGroupLocationScheduleConfigs.OrderBy( s => s.Schedule.GetNextStartDateTime( occurrenceDate ) );
            rptGroupLocationScheduleCapacities.Visible = true;
            rptGroupLocationScheduleCapacities.DataBind();
            rcwGroupLocationScheduleCapacities.Visible = currentGroupLocationScheduleConfigs.Any();
        }

        /// <summary>
        /// Used to determine if a user has selected a location
        /// </summary>
        /// <returns></returns>
        private bool LocationSelected()
        {
            var selectedLocation = locpGroupLocation.Location;
            return selectedLocation != null;
        }

        /// <summary>
        /// Used to archive the single group
        /// </summary>
        private void ArchiveSingleGroup( int groupId )
        {
            int? parentGroupId = null;
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            AuthService authService = new AuthService( rockContext );

            Group group = groupService.Get( hfGroupId.Value.AsInteger() );
            if ( group != null )
            {
                if ( !group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) )
                {
                    mdArchive.Hide();
                    mdDeleteWarning.Show( "You are not authorized to archive this group.", ModalAlertType.Information );
                    return;
                }

                parentGroupId = group.ParentGroupId;
                groupService.Archive( group, this.CurrentPersonAliasId, true );

                rockContext.SaveChanges();
            }

            NavigateAfterDeleteOrArchive( parentGroupId );
        }

        /// <summary>
        /// Used to archive the group and all child groups
        /// </summary>
        private void ArchiveAllChildGroups( int groupId )
        {
            int? parentGroupId = null;
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            AuthService authService = new AuthService( rockContext );

            Group group = groupService.Get( hfGroupId.Value.AsInteger() );
            if ( group != null )
            {
                if ( !group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) )
                {
                    mdArchive.Hide();
                    mdDeleteWarning.Show( "You are not authorized to archive this group.", ModalAlertType.Information );
                    return;
                }

                parentGroupId = group.ParentGroupId;

                var childGroups = groupService.GetAllDescendentGroups( group.Id, true );
                foreach ( var childGroup in childGroups )
                {
                    groupService.Archive( childGroup, this.CurrentPersonAliasId, true );
                }

                groupService.Archive( group, this.CurrentPersonAliasId, true );

                rockContext.SaveChanges();
            }

            NavigateAfterDeleteOrArchive( parentGroupId );
        }

        #endregion

        #region Location Grid and Picker

        /// <summary>
        /// Handles the Add event of the gGroupLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupLocations_Add( object sender, EventArgs e )
        {
            hfAction.Value = "Add";
            gGroupLocations_ShowEdit( Guid.Empty );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupLocations_Edit( object sender, RowEventArgs e )
        {
            hfAction.Value = "Edit";
            Guid locationGuid = ( Guid ) e.RowKeyValue;
            gGroupLocations_ShowEdit( locationGuid );
        }

        /// <summary>
        /// Gs the locations_ show edit.
        /// </summary>
        /// <param name="locationGuid">The location unique identifier.</param>
        protected void gGroupLocations_ShowEdit( Guid locationGuid )
        {
            ResetLocationDialog();
            hfGroupLocationGuid.Value = locationGuid.ToString();

            var rockContext = new RockContext();
            ddlMember.Items.Clear();
            int? groupTypeId = this.CurrentGroupTypeId;
            if ( !groupTypeId.HasValue )
            {
                return;
            }

            var groupType = GroupTypeCache.Get( groupTypeId.Value );
            if ( groupType == null )
            {
                return;
            }

            GroupLocationPickerMode groupTypeModes = groupType.LocationSelectionMode;
            if ( groupTypeModes == GroupLocationPickerMode.None )
            {
                return;
            }

            // Set the location picker modes allowed based on the group type's allowed modes
            LocationPickerMode modes = LocationPickerMode.None;
            if ( ( groupTypeModes & GroupLocationPickerMode.Named ) == GroupLocationPickerMode.Named )
            {
                modes = modes | LocationPickerMode.Named;
            }

            if ( ( groupTypeModes & GroupLocationPickerMode.Address ) == GroupLocationPickerMode.Address )
            {
                modes = modes | LocationPickerMode.Address;
            }

            if ( ( groupTypeModes & GroupLocationPickerMode.Point ) == GroupLocationPickerMode.Point )
            {
                modes = modes | LocationPickerMode.Point;
            }

            if ( ( groupTypeModes & GroupLocationPickerMode.Polygon ) == GroupLocationPickerMode.Polygon )
            {
                modes = modes | LocationPickerMode.Polygon;
            }

            bool displayMemberTab = ( groupTypeModes & GroupLocationPickerMode.GroupMember ) == GroupLocationPickerMode.GroupMember;
            bool displayOtherTab = modes != LocationPickerMode.None;

            ulNav.Visible = displayOtherTab && displayMemberTab;
            pnlMemberSelect.Visible = displayMemberTab;
            pnlLocationSelect.Visible = displayOtherTab && !displayMemberTab;

            if ( displayMemberTab )
            {
                int groupId = hfGroupId.ValueAsInt();
                if ( groupId != 0 )
                {
                    var personService = new PersonService( rockContext );
                    Guid previousLocationType = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid();

                    foreach ( GroupMember member in new GroupMemberService( rockContext ).GetByGroupId( groupId ) )
                    {
                        foreach ( Group family in personService.GetFamilies( member.PersonId ) )
                        {
                            foreach ( GroupLocation familyGroupLocation in family.GroupLocations
                                .Where( l => l.IsMappedLocation && !l.GroupLocationTypeValue.Guid.Equals( previousLocationType ) ) )
                            {
                                ListItem li = new ListItem(
                                    string.Format( "{0} {1} ({2})", member.Person.FullName, familyGroupLocation.GroupLocationTypeValue.Value, familyGroupLocation.Location.ToString() ),
                                    string.Format( "{0}|{1}", familyGroupLocation.Location.Id, member.PersonId ) );

                                ddlMember.Items.Add( li );
                            }
                        }
                    }
                }
            }

            if ( displayOtherTab )
            {
                locpGroupLocation.AllowedPickerModes = modes;
                locpGroupLocation.SetBestPickerModeForLocation( null );
            }

            ddlLocationType.DataSource = groupType.LocationTypeValues.ToList();
            ddlLocationType.DataBind();

            var groupLocation = GroupLocationsState.FirstOrDefault( l => l.Guid.Equals( locationGuid ) );
            if ( groupLocation != null && groupLocation.Location != null )
            {
                if ( displayOtherTab )
                {
                    locpGroupLocation.SetBestPickerModeForLocation( groupLocation.Location );

                    locpGroupLocation.MapStyleValueGuid = GetAttributeValue( AttributeKey.MapStyle ).AsGuid();

                    if ( groupLocation.Location != null )
                    {
                        locpGroupLocation.Location = new LocationService( rockContext ).Get( groupLocation.Location.Id );
                    }
                }

                if ( displayMemberTab && ddlMember.Items.Count > 0 && groupLocation.GroupMemberPersonAliasId.HasValue )
                {
                    LocationTypeTab = MEMBER_LOCATION_TAB_TITLE;
                    int? personId = new PersonAliasService( rockContext ).GetPersonId( groupLocation.GroupMemberPersonAliasId.Value );
                    if ( personId.HasValue )
                    {
                        ddlMember.SetValue( string.Format( "{0}|{1}", groupLocation.LocationId, personId.Value ) );
                    }
                }
                else if ( displayOtherTab )
                {
                    LocationTypeTab = OTHER_LOCATION_TAB_TITLE;
                }

                ddlLocationType.SetValue( groupLocation.GroupLocationTypeValueId );

                var activeSchedules = groupLocation.Schedules.Where( s => s.IsActive );
                var inactiveScheduleIds = groupLocation.Schedules.Where( s => !s.IsActive ).Select( s => s.Id ).ToList();
                spSchedules.SetValues( activeSchedules );
                hfInactiveGroupLocationSchedules.Value = inactiveScheduleIds.AsDelimited( "," );

                hfAddLocationGroupGuid.Value = locationGuid.ToString();
            }
            else
            {
                hfAddLocationGroupGuid.Value = string.Empty;
                LocationTypeTab = ( displayMemberTab && ddlMember.Items.Count > 0 ) ? MEMBER_LOCATION_TAB_TITLE : OTHER_LOCATION_TAB_TITLE;
            }

            rptLocationTypes.DataSource = _tabs;
            rptLocationTypes.DataBind();

            rcwGroupLocationScheduleCapacities.Visible = groupType.IsSchedulingEnabled;
            if ( groupType.IsSchedulingEnabled )
            {
                var schedules = new ScheduleService( rockContext ).GetByIds( spSchedules.SelectedValuesAsInt().ToList() );

                List<GroupLocationScheduleConfig> groupLocationScheduleConfigList = schedules.ToList().Select( s =>
                {
                    GroupLocationScheduleConfig groupLocationScheduleConfig = groupLocation == null ? null : groupLocation.GroupLocationScheduleConfigs.FirstOrDefault( a => a.ScheduleId == s.Id );
                    if ( groupLocationScheduleConfig != null )
                    {
                        return groupLocationScheduleConfig;
                    }
                    else
                    {
                        return new GroupLocationScheduleConfig
                        {
                            ScheduleId = s.Id,
                            Schedule = s
                        };
                    }
                } ).ToList();

                // Handle case where schedules are created and no group location configuration exists yet
                if ( groupLocationScheduleConfigList.Count() == 0 && schedules.Count() > 0 )
                {
                    // No schedules have been saved yet.
                    groupLocationScheduleConfigList = new List<GroupLocationScheduleConfig>();
                    foreach ( var schedule in schedules )
                    {
                        groupLocationScheduleConfigList.Add( new GroupLocationScheduleConfig
                        {
                            ScheduleId = schedule.Id,
                            Schedule = schedule
                        } );
                    }
                }

                BindGroupLocationScheduleCapacities( groupLocationScheduleConfigList );
            }

            ShowSelectedPane();
            ShowDialog( "Locations", true );
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptGroupLocationScheduleCapacities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptGroupLocationScheduleCapacities_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            // Builds the display of schedules for the Grid
            GroupLocationScheduleConfig groupLocationScheduleConfig = e.Item.DataItem as GroupLocationScheduleConfig;
            if ( groupLocationScheduleConfig == null )
            {
                return;
            }

            Literal lScheduleName = e.Item.FindControl( "lScheduleName" ) as Literal;
            lScheduleName.Text = groupLocationScheduleConfig.Schedule == null ? string.Empty : groupLocationScheduleConfig.Schedule.Name;

            HiddenField hfScheduleId = e.Item.FindControl( "hfScheduleId" ) as HiddenField;
            NumberBox nbMinimumCapacity = e.Item.FindControl( "nbMinimumCapacity" ) as NumberBox;
            NumberBox nbDesiredCapacity = e.Item.FindControl( "nbDesiredCapacity" ) as NumberBox;
            NumberBox nbMaximumCapacity = e.Item.FindControl( "nbMaximumCapacity" ) as NumberBox;

            hfScheduleId.Value = groupLocationScheduleConfig.ScheduleId.ToString();
            nbMinimumCapacity.Text = groupLocationScheduleConfig.MinimumCapacity.ToString();
            nbDesiredCapacity.Text = groupLocationScheduleConfig.DesiredCapacity.ToString();
            nbMaximumCapacity.Text = groupLocationScheduleConfig.MaximumCapacity.ToString();
        }

        /// <summary>
        /// Handles the Delete event of the gGroupLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupLocations_Delete( object sender, RowEventArgs e )
        {
            Guid rowGuid = ( Guid ) e.RowKeyValue;
            GroupLocationsState.RemoveEntity( rowGuid );
            BindGroupLocationsGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupLocations_GridRebind( object sender, EventArgs e )
        {
            BindGroupLocationsGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the dlgLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgLocations_OkClick( object sender, EventArgs e )
        {
            Location location = null;
            int? memberPersonAliasId = null;
            RockContext rockContext = new RockContext();

            if ( LocationTypeTab.Equals( MEMBER_LOCATION_TAB_TITLE ) )
            {
                if ( ddlMember.SelectedValue != null )
                {
                    var ids = ddlMember.SelectedValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).AsIntegerList().ToArray();
                    if ( ids.Length == 2 )
                    {
                        int locationId = ids[0];
                        int primaryAliasId = ids[1];
                        var dbLocation = new LocationService( rockContext ).Get( locationId );
                        if ( dbLocation != null )
                        {
                            location = new Location();
                            location.CopyPropertiesFrom( dbLocation );
                        }

                        memberPersonAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( primaryAliasId );
                    }
                }
            }
            else
            {
                if ( locpGroupLocation.Location != null )
                {
                    var selectedLocation = locpGroupLocation.Location;

                    location = new Location();
                    location.CopyPropertiesFrom( selectedLocation );
                }
            }

            var locationGroupGuid = hfAddLocationGroupGuid.Value.AsGuid();

            if ( location != null )
            {
                GroupLocation groupLocation = null;

                if ( !locationGroupGuid.IsEmpty() )
                {
                    groupLocation = GroupLocationsState.FirstOrDefault( l => l.Guid.Equals( locationGroupGuid ) );
                }

                if ( groupLocation == null )
                {
                    groupLocation = new GroupLocation();
                    if ( GroupLocationsState.Any() )
                    {
                        groupLocation.Order = GroupLocationsState.Max( l => l.Order ) + 1;
                    }

                    GroupLocationsState.Add( groupLocation );
                }

                var schedules = new ScheduleService( rockContext ).GetByIds( spSchedules.SelectedValuesAsInt().ToList() ).ToList();

                // Builds the display of capacities for the group location edit dialog.
                foreach ( RepeaterItem rItem in rptGroupLocationScheduleCapacities.Items )
                {
                    var hfScheduleId = rItem.FindControl( "hfScheduleId" ) as HiddenField;
                    var nbMinimumCapacity = rItem.FindControl( "nbMinimumCapacity" ) as NumberBox;
                    var nbDesiredCapacity = rItem.FindControl( "nbDesiredCapacity" ) as NumberBox;
                    var nbMaximumCapacity = rItem.FindControl( "nbMaximumCapacity" ) as NumberBox;
                    var iScheduleId = hfScheduleId.Value.AsIntegerOrNull();
                    var iMinCapacity = nbMinimumCapacity.Text.AsIntegerOrNull();
                    var iDesiredCapacity = nbDesiredCapacity.Text.AsIntegerOrNull();
                    var iMaxCapacity = nbMaximumCapacity.Text.AsIntegerOrNull();
                    var schedule = schedules.Where( s => s.Id == iScheduleId ).FirstOrDefault();

                    if ( iScheduleId != null )
                    {
                        var currentgroupLocationScheduleConfig = groupLocation.GroupLocationScheduleConfigs.Where( i => i.ScheduleId == iScheduleId ).FirstOrDefault();
                        if ( currentgroupLocationScheduleConfig != null )
                        {
                            currentgroupLocationScheduleConfig.Schedule = schedule;
                            currentgroupLocationScheduleConfig.MinimumCapacity = iMinCapacity;
                            currentgroupLocationScheduleConfig.DesiredCapacity = iDesiredCapacity;
                            currentgroupLocationScheduleConfig.MaximumCapacity = iMaxCapacity;
                        }
                        else
                        {
                            currentgroupLocationScheduleConfig = new GroupLocationScheduleConfig
                            {
                                ScheduleId = ( int ) iScheduleId,
                                Schedule = schedule,
                                MinimumCapacity = iMinCapacity,
                                DesiredCapacity = iDesiredCapacity,
                                MaximumCapacity = iMaxCapacity
                            };
                            groupLocation.GroupLocationScheduleConfigs.Add( currentgroupLocationScheduleConfig );
                        }
                    }
                }

                groupLocation.GroupMemberPersonAliasId = memberPersonAliasId;
                groupLocation.Location = location;
                groupLocation.LocationId = groupLocation.Location.Id;
                groupLocation.GroupLocationTypeValueId = ddlLocationType.SelectedValueAsId();

                var selectedIds = spSchedules.SelectedValuesAsInt();
                var inactiveSchedulesIds = new List<int>();
                if ( hfInactiveGroupLocationSchedules.Value.IsNotNullOrWhiteSpace() )
                {
                    inactiveSchedulesIds = hfInactiveGroupLocationSchedules.Value.SplitDelimitedValues( "," ).Select( i => int.Parse( i ) ).ToList();
                }

                var scheduleService = new ScheduleService( rockContext );
                var groupLocationSchedules = scheduleService.Queryable()
                    .Where( s => selectedIds.Contains( s.Id ) || inactiveSchedulesIds.Contains( s.Id ) ).ToList();
                groupLocation.Schedules = groupLocationSchedules;

                if ( groupLocation.GroupLocationTypeValueId.HasValue )
                {
                    groupLocation.GroupLocationTypeValue = new DefinedValue();
                    var definedValue = new DefinedValueService( rockContext ).Get( groupLocation.GroupLocationTypeValueId.Value );
                    if ( definedValue != null )
                    {
                        groupLocation.GroupLocationTypeValue.CopyPropertiesFrom( definedValue );
                    }
                }
            }
            else
            {
                if ( !locationGroupGuid.IsEmpty() )
                {
                    var groupLocation = GroupLocationsState.FirstOrDefault( l => l.Guid.Equals( locationGroupGuid ) );

                    if ( groupLocation != null )
                    {
                        GroupLocationsState.Remove( groupLocation );
                    }
                }
            }

            BindGroupLocationsGrid();
            spSchedules.SetValue( 0 );
            HideDialog();
        }

        /// <summary>
        /// Detect  a existing the location on add.
        /// </summary>
        /// <param name="selectedLocation">The selected location.</param>
        /// <returns>return false if not an add or location not selected</returns>
        private bool ExistingLocationOnAdd( Location selectedLocation )
        {
            if ( hfAction.Value == "Add" && selectedLocation != null )
            {
                List<GridLocation> existingLocations = gGroupLocations.DataSourceAsList as List<GridLocation>;

                return existingLocations.Where( x => x.Location.Name == selectedLocation.Name && x.Location.Guid == selectedLocation.Guid ).Any();
            }

            return false;
        }

        /// <summary>
        /// Binds the locations grid.
        /// </summary>
        private void BindGroupLocationsGrid()
        {
            gGroupLocations.Actions.ShowAdd = AllowMultipleLocations || !GroupLocationsState.Any();

            gGroupLocations.DataSource = GroupLocationsState
                .Select( gl => new GridLocation
                {
                    Guid = gl.Guid,
                    Location = gl.Location,
                    Type = gl.GroupLocationTypeValue != null ? gl.GroupLocationTypeValue.Value : string.Empty,
                    Order = gl.GroupLocationTypeValue != null ? gl.GroupLocationTypeValue.Order : 0,
                    Schedules = gl.Schedules != null ? gl.Schedules.Where( s => s.IsActive ).Select( s => s.Name ).ToList().AsDelimited( ", " ) : string.Empty
                } )
                .OrderBy( i => i.Order )
                .ToList();
            gGroupLocations.DataBind();
        }

        /// <summary>
        /// Resets the location dialog.
        /// </summary>
        private void ResetLocationDialog()
        {
            locpGroupLocation.Location = null;
            spSchedules.SetValue( 0 );
            rptGroupLocationScheduleCapacities.DataSource = null;
            rptGroupLocationScheduleCapacities.DataBind();
            rptGroupLocationScheduleCapacities.Visible = false;
            nbEditModeMessage.Text = string.Empty;
            nbEditModeMessage.Visible = false;
        }

        /// <summary>
        /// Handles the SelectLocation event of the locpGroupLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void locpGroupLocation_SelectLocation( object sender, EventArgs e )
        {
            nbGroupLocationEditMessage.Text = string.Empty;
            nbGroupLocationEditMessage.Visible = false;
            nbGroupLocationEditMessage.Visible = true;
            var selectedLocation = locpGroupLocation.Location;
            if ( ExistingLocationOnAdd( selectedLocation ) )
            {
                nbGroupLocationEditMessage.Text = string.Format( "{0} already exists in meeting details and can not be selected again.", selectedLocation.Name );
                nbGroupLocationEditMessage.Visible = true;
                locpGroupLocation.Location = null;
            }
        }

        #endregion

        #region GroupRequirements Grid and Picker

        /// <summary>
        /// Handles the Add event of the gGroupRequirements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupRequirements_Add( object sender, EventArgs e )
        {
            gGroupRequirements_ShowEdit( Guid.Empty );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupRequirements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupRequirements_Edit( object sender, RowEventArgs e )
        {
            Guid groupRequirementGuid = ( Guid ) e.RowKeyValue;
            gGroupRequirements_ShowEdit( groupRequirementGuid );
        }

        /// <summary>
        /// Shows the modal dialog to add/edit a Group Requirement
        /// </summary>
        /// <param name="groupRequirementGuid">The group requirement unique identifier.</param>
        protected void gGroupRequirements_ShowEdit( Guid groupRequirementGuid )
        {
            var rockContext = new RockContext();

            var groupRequirementTypeService = new GroupRequirementTypeService( rockContext );
            var list = groupRequirementTypeService.Queryable().OrderBy( a => a.Name ).ToList();
            ddlGroupRequirementType.Items.Clear();
            ddlGroupRequirementType.Items.Add( new ListItem() );
            foreach ( var item in list )
            {
                ddlGroupRequirementType.Items.Add( new ListItem( item.Name, item.Id.ToString() ) );
            }

            var selectedGroupRequirement = this.GroupRequirementsState.FirstOrDefault( a => a.Guid == groupRequirementGuid );
            grpGroupRequirementGroupRole.GroupTypeId = this.CurrentGroupTypeId;

            Group group = GetGroup( hfGroupId.Value.AsInteger() );
            ddlDueDateGroupAttribute.Items.Clear();

            // If the group is null (new and unsaved, with a groupId of 0), look to the inherited attributes for the current group type.
            if ( group == null )
            {
                foreach ( var attribute in GroupDateAttributesState.ToList() )
                {
                    ddlDueDateGroupAttribute.Items.Add( new ListItem( attribute.Name, attribute.Id.ToString() ) );
                }
            }
            else
            {
                group.LoadAttributes();
                foreach ( var attribute in group.Attributes.Select( a => a.Value ).Where( a => DateFieldTypeIds.Contains( a.FieldTypeId ) ).ToList() )
                {
                    ddlDueDateGroupAttribute.Items.Add( new ListItem( attribute.Name, attribute.Id.ToString() ) );
                }
            }

            ddlDueDateGroupAttribute.DataBind();

            // Make sure that the Due Date controls are not visible unless the requirement has a due date.
            ddlDueDateGroupAttribute.Visible = false;
            ddlDueDateGroupAttribute.Required = false;
            dpDueDate.Visible = false;
            dpDueDate.Required = false;

            rblAppliesToAgeClassification.Items.Clear();

            foreach ( var ageClassification in Enum.GetValues( typeof( AppliesToAgeClassification ) ).Cast<AppliesToAgeClassification>() )
            {
                rblAppliesToAgeClassification.Items.Add( new ListItem( ageClassification.ConvertToString( true ), ageClassification.ConvertToString( false ) ) );
            }

            rblAppliesToAgeClassification.DataBind();

            if ( selectedGroupRequirement != null )
            {
                ddlGroupRequirementType.SelectedValue = selectedGroupRequirement.GroupRequirementTypeId.ToString();
                grpGroupRequirementGroupRole.GroupRoleId = selectedGroupRequirement.GroupRoleId;
                cbMembersMustMeetRequirementOnAdd.Checked = selectedGroupRequirement.MustMeetRequirementToAddMember;

                var groupRequirementType = list.Where( r => r.Id == selectedGroupRequirement.GroupRequirementTypeId ).First();

                ShowDueDateQualifierControls();
                rblAppliesToAgeClassification.SelectedValue = selectedGroupRequirement.AppliesToAgeClassification.ToString();
                dvpAppliesToDataView.SetValue( selectedGroupRequirement.AppliesToDataViewId );
                cbAllowLeadersToOverride.Checked = selectedGroupRequirement.AllowLeadersToOverride;
                if ( groupRequirementType.DueDateType == DueDateType.GroupAttribute )
                {
                    ddlDueDateGroupAttribute.Visible = true;
                    ddlDueDateGroupAttribute.Required = true;
                    ddlDueDateGroupAttribute.SetValue( selectedGroupRequirement.DueDateAttributeId.HasValue ? selectedGroupRequirement.DueDateAttributeId.ToString() : string.Empty );
                }

                if ( groupRequirementType.DueDateType == DueDateType.ConfiguredDate )
                {
                    dpDueDate.Visible = true;
                    dpDueDate.Required = true;
                    dpDueDate.SelectedDate = selectedGroupRequirement.DueDateStaticDate;
                }
            }
            else
            {
                ddlGroupRequirementType.SelectedIndex = 0;
                grpGroupRequirementGroupRole.GroupRoleId = null;
                cbMembersMustMeetRequirementOnAdd.Checked = false;
                rblAppliesToAgeClassification.SetValue( AppliesToAgeClassification.All.ToString() );
            }

            nbDuplicateGroupRequirement.Visible = false;

            hfGroupRequirementGuid.Value = groupRequirementGuid.ToString();

            ShowDialog( "GroupRequirements", true );
        }

        /// <summary>
        /// Handles the DataBound event of the lAppliesToDataViewId control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void lAppliesToDataViewId_OnDataBound( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            Literal lAppliesToDataViewId = sender as Literal;
            GroupRequirement requirement = e.Row.DataItem as GroupRequirement;
            if ( requirement != null )
            {
                lAppliesToDataViewId.Text = requirement.AppliesToDataViewId.HasValue ? "<i class=\"fa fa-check\"></i>" : string.Empty;
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdGroupRequirement control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdGroupRequirement_SaveClick( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            Guid groupRequirementGuid = hfGroupRequirementGuid.Value.AsGuid();

            var groupRequirement = this.GroupRequirementsState.FirstOrDefault( a => a.Guid == groupRequirementGuid );
            if ( groupRequirement == null )
            {
                groupRequirement = new GroupRequirement();
                groupRequirement.Guid = Guid.NewGuid();
                this.GroupRequirementsState.Add( groupRequirement );
            }

            groupRequirement.GroupRequirementTypeId = ddlGroupRequirementType.SelectedValue.AsInteger();
            groupRequirement.GroupRequirementType = new GroupRequirementTypeService( rockContext ).Get( groupRequirement.GroupRequirementTypeId );
            groupRequirement.GroupRoleId = grpGroupRequirementGroupRole.GroupRoleId;
            groupRequirement.MustMeetRequirementToAddMember = cbMembersMustMeetRequirementOnAdd.Checked;
            if ( groupRequirement.GroupRoleId.HasValue )
            {
                groupRequirement.GroupRole = new GroupTypeRoleService( rockContext ).Get( groupRequirement.GroupRoleId.Value );
            }
            else
            {
                groupRequirement.GroupRole = null;
            }

            groupRequirement.AppliesToAgeClassification = rblAppliesToAgeClassification.SelectedValue.ConvertToEnum<AppliesToAgeClassification>();
            groupRequirement.AppliesToDataViewId = dvpAppliesToDataView.SelectedValueAsId();
            groupRequirement.AllowLeadersToOverride = cbAllowLeadersToOverride.Checked;

            if ( groupRequirement.GroupRequirementType.DueDateType == DueDateType.ConfiguredDate )
            {
                groupRequirement.DueDateStaticDate = dpDueDate.SelectedDate;
            }

            if ( groupRequirement.GroupRequirementType.DueDateType == DueDateType.GroupAttribute )
            {
                // Set this due date attribute if it exists.
                var groupDueDateAttributes = AttributeCache.AllForEntityType<Group>().Where( a => a.Id == ddlDueDateGroupAttribute.SelectedValue.AsIntegerOrNull() );
                if ( groupDueDateAttributes.Any() )
                {
                    groupRequirement.DueDateAttributeId = groupDueDateAttributes.First().Id;
                }
            }

            // Make sure we aren't adding a duplicate group requirement (same group requirement type and role)
            var duplicateGroupRequirement = this.GroupRequirementsState.Any( a =>
                a.GroupRequirementTypeId == groupRequirement.GroupRequirementTypeId
                && a.GroupRoleId == groupRequirement.GroupRoleId
                && a.Guid != groupRequirement.Guid );

            if ( duplicateGroupRequirement )
            {
                nbDuplicateGroupRequirement.Text = string.Format(
                    "This group already has a group requirement of {0} {1}",
                    groupRequirement.GroupRequirementType.Name,
                    groupRequirement.GroupRoleId.HasValue ? "for group role " + groupRequirement.GroupRole.Name : string.Empty );
                nbDuplicateGroupRequirement.Visible = true;
                this.GroupRequirementsState.Remove( groupRequirement );
                return;
            }
            else
            {
                nbDuplicateGroupRequirement.Visible = false;
                BindGroupRequirementsGrid();
                HideDialog();
            }
        }

        /// <summary>
        /// Handles the Delete event of the gGroupRequirements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupRequirements_Delete( object sender, RowEventArgs e )
        {
            Guid rowGuid = ( Guid ) e.RowKeyValue;
            GroupRequirementsState.RemoveEntity( rowGuid );

            BindGroupRequirementsGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupRequirements control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupRequirements_GridRebind( object sender, EventArgs e )
        {
            BindGroupRequirementsGrid();
        }

        #endregion

        #region Group Syncs Grid and Picker

        /// <summary>
        /// Handles the Add event of the gGroupSyncs control.
        /// Shows the GroupSync modal and populates the controls without explicitly assigning
        /// a value
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupSyncs_Add( object sender, EventArgs e )
        {
            ClearGroupSyncModal();

            RockContext rockContext = new RockContext();

            CreateRoleDropDownList( rockContext );
            CreateSystemCommunicationDropDownLists( rockContext );

            dvipSyncDataView.EntityTypeId = EntityTypeCache.Get( typeof( Person ) ).Id;

            ShowDialog( "GROUPSYNCSETTINGS", true );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupSyncs control.
        /// Shows the GroupSync modal, populates the controls, and selects
        /// the data for the selected group sync.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupSyncs_Edit( object sender, RowEventArgs e )
        {
            ClearGroupSyncModal();

            Guid syncGuid = ( Guid ) e.RowKeyValue;
            GroupSync groupSync = GroupSyncState.Where( s => s.Guid == syncGuid ).FirstOrDefault();
            RockContext rockContext = new RockContext();

            hfGroupSyncGuid.Value = syncGuid.ToString();

            dvipSyncDataView.EntityTypeId = EntityTypeCache.Get( typeof( Person ) ).Id;
            dvipSyncDataView.SetValue( groupSync.SyncDataViewId );

            CreateRoleDropDownList( rockContext, groupSync.GroupTypeRoleId );
            ddlGroupRoles.SetValue( groupSync.GroupTypeRoleId );

            CreateSystemCommunicationDropDownLists( rockContext );
            ddlWelcomeCommunication.SetValue( groupSync.WelcomeSystemCommunicationId );
            ddlExitCommunication.SetValue( groupSync.ExitSystemCommunicationId );

            cbCreateLoginDuringSync.Checked = groupSync.AddUserAccountsDuringSync;
            ipScheduleIntervalMinutes.IntervalInMinutes = groupSync.ScheduleIntervalMinutes.HasValue ? groupSync.ScheduleIntervalMinutes.Value : 12 * 60;

            ShowDialog( "GROUPSYNCSETTINGS", true );
        }

        /// <summary>
        /// Handles the Delete event of the gGroupSyncs control.
        /// Removes the group sync from the List<> in the current state
        /// and the grid. Will not be removed from the group until the group
        /// is saved.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroupSyncs_Delete( object sender, RowEventArgs e )
        {
            Guid guid = ( Guid ) e.RowKeyValue;
            GroupSyncState.RemoveEntity( guid );
            BindGroupSyncGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupSyncs control.
        /// This call BindGroupSyncGrid()
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroupSyncs_GridRebind( object sender, EventArgs e )
        {
            BindGroupSyncGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the mdGroupSyncSettings control.
        /// Adds the group sync to the List for the current state and to the grid.
        /// Won't be added to the group until the group is saved.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdGroupSyncSettings_SaveClick( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            Guid syncGuid = hfGroupSyncGuid.Value.AsGuid();

            // Create a new obj
            var groupSync = GroupSyncState.Where( s => s.Guid == syncGuid ).FirstOrDefault();
            if ( groupSync == null )
            {
                groupSync = new GroupSyncViewModel();
                groupSync.Guid = Guid.NewGuid();
                GroupSyncState.Add( groupSync );
            }

            groupSync.GroupId = hfGroupId.ValueAsInt();
            groupSync.GroupTypeRoleId = ddlGroupRoles.SelectedValue.AsInteger();
            groupSync.GroupTypeRole = new GroupTypeRoleService( rockContext ).Get( groupSync.GroupTypeRoleId );
            groupSync.SyncDataViewId = dvipSyncDataView.SelectedValueAsInt() ?? 0;
            groupSync.SyncDataView = new DataViewService( rockContext ).Get( groupSync.SyncDataViewId );
            groupSync.ExitSystemCommunicationId = ddlExitCommunication.SelectedValue.AsIntegerOrNull();
            groupSync.WelcomeSystemCommunicationId = ddlWelcomeCommunication.SelectedValue.AsIntegerOrNull();
            groupSync.AddUserAccountsDuringSync = cbCreateLoginDuringSync.Checked;
            groupSync.ScheduleIntervalMinutes = ipScheduleIntervalMinutes.IntervalInMinutes;

            hfGroupSyncGuid.Value = string.Empty;

            BindGroupSyncGrid();

            HideDialog();
        }

        /// <summary>
        /// Binds the GroupSync grid to the List stored in the current state
        /// </summary>
        private void BindGroupSyncGrid()
        {
            gGroupSyncs.DataSource = GroupSyncState;
            gGroupSyncs.DataBind();
        }

        /// <summary>
        /// Creates the System Communication drop-down lists. Does not set a selected value.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        private void CreateSystemCommunicationDropDownLists( RockContext rockContext )
        {
            // Populate the communication fields
            var systemCommunications = new SystemCommunicationService( rockContext )
                .Queryable()
                .OrderBy( e => e.Title )
                .Select( a => new { a.Id, a.Title } );

            // add a blank for the first option
            ddlWelcomeCommunication.Items.Add( new ListItem() );
            ddlExitCommunication.Items.Add( new ListItem() );
            ddlRsvpReminderSystemCommunication.Items.Add( new ListItem() );

            if ( systemCommunications.Any() )
            {
                foreach ( var systemCommunication in systemCommunications )
                {
                    ddlWelcomeCommunication.Items.Add( new ListItem( systemCommunication.Title, systemCommunication.Id.ToString() ) );
                    ddlExitCommunication.Items.Add( new ListItem( systemCommunication.Title, systemCommunication.Id.ToString() ) );
                    ddlRsvpReminderSystemCommunication.Items.Add( new ListItem( systemCommunication.Title, systemCommunication.Id.ToString() ) );
                }
            }
        }

        /// <summary>
        /// Creates the role drop down list.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="RoleId">The role identifier if editing, otherwise leave default</param>
        private void CreateRoleDropDownList( RockContext rockContext, int roleId = -1 )
        {
            List<int> currentSyncdRoles = new List<int>();
            int groupTypeId = ddlGroupType.SelectedValue.AsInteger();
            int groupId = hfGroupId.ValueAsInt();

            currentSyncdRoles = GroupSyncState
                    .Where( s => s.GroupId == groupId )
                    .Select( s => s.GroupTypeRoleId )
                    .ToList();

            currentSyncdRoles.Remove( roleId );

            // If not 0 then get the existing roles to remove, if 0 then this is a new group that has not yet been saved.
            if ( groupId > 0 )
            {
                groupTypeId = new GroupService( rockContext ).Get( groupId ).GroupTypeId;
            }

            var roles = new GroupTypeRoleService( rockContext )
                .Queryable()
                .Where( r => r.GroupTypeId == groupTypeId && !currentSyncdRoles.Contains( r.Id ) )
                .ToList();

            // Give a blank for the first selection
            ddlGroupRoles.Items.Add( new ListItem() );

            foreach ( var role in roles )
            {
                ddlGroupRoles.Items.Add( new ListItem( role.Name, role.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Clears the values from the group sync modal controls.
        /// </summary>
        private void ClearGroupSyncModal()
        {
            ddlGroupRoles.Items.Clear();
            ddlWelcomeCommunication.Items.Clear();
            ddlExitCommunication.Items.Clear();
            cbCreateLoginDuringSync.Checked = false;
            ipScheduleIntervalMinutes.IntervalInMinutes = 12 * 60;
        }

        #endregion

        #region GroupMemberAttributes Grid and Picker

        /// <summary>
        /// Handles the Add event of the gGroupMemberAttributes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gGroupMemberAttributes_Add( object sender, EventArgs e )
        {
            gGroupMemberAttributes_ShowEdit( Guid.Empty );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupMemberAttributes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gGroupMemberAttributes_Edit( object sender, RowEventArgs e )
        {
            Guid attributeGuid = ( Guid ) e.RowKeyValue;
            gGroupMemberAttributes_ShowEdit( attributeGuid );
        }

        /// <summary>
        /// Gs the group member attributes_ show edit.
        /// </summary>
        /// <param name="attributeGuid">The attribute GUID.</param>
        protected void gGroupMemberAttributes_ShowEdit( Guid attributeGuid )
        {
            Attribute attribute;
            if ( attributeGuid.Equals( Guid.Empty ) )
            {
                attribute = new Attribute();
                attribute.FieldTypeId = FieldTypeCache.Get( Rock.SystemGuid.FieldType.TEXT ).Id;
                edtGroupMemberAttributes.ActionTitle = ActionTitle.Add( "attribute for group members of " + tbName.Text );
            }
            else
            {
                attribute = GroupMemberAttributesState.First( a => a.Guid.Equals( attributeGuid ) );
                edtGroupMemberAttributes.ActionTitle = ActionTitle.Edit( "attribute for group members of " + tbName.Text );
            }

            var reservedKeyNames = new List<string>();
            GroupMemberAttributesInheritedState.Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
            GroupMemberAttributesState.Where( a => !a.Guid.Equals( attributeGuid ) ).Select( a => a.Key ).ToList().ForEach( a => reservedKeyNames.Add( a ) );
            edtGroupMemberAttributes.ReservedKeyNames = reservedKeyNames.ToList();

            edtGroupMemberAttributes.SetAttributeProperties( attribute, typeof( GroupMember ) );

            ShowDialog( "GroupMemberAttributes", true );
        }

        /// <summary>
        /// Handles the GridReorder event of the gGroupMemberAttributes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridReorderEventArgs"/> instance containing the event data.</param>
        protected void gGroupMemberAttributes_GridReorder( object sender, GridReorderEventArgs e )
        {
            ReorderAttributeList( GroupMemberAttributesState, e.OldIndex, e.NewIndex );
            BindGroupMemberAttributesGrid();
        }

        /// <summary>
        /// Handles the Delete event of the gGroupMemberAttributes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gGroupMemberAttributes_Delete( object sender, RowEventArgs e )
        {
            Guid attributeGuid = ( Guid ) e.RowKeyValue;
            GroupMemberAttributesState.RemoveEntity( attributeGuid );

            BindGroupMemberAttributesGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupMemberAttributesInherited control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gGroupMemberAttributesInherited_GridRebind( object sender, EventArgs e )
        {
            BindGroupMemberAttributesInheritedGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupMemberAttributes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gGroupMemberAttributes_GridRebind( object sender, EventArgs e )
        {
            BindGroupMemberAttributesGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the dlgGroupMemberAttribute control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgGroupMemberAttribute_SaveClick( object sender, EventArgs e )
        {
            Rock.Model.Attribute attribute = new Rock.Model.Attribute();
            edtGroupMemberAttributes.GetAttributeProperties( attribute );

            // Controls will show warnings
            if ( !attribute.IsValid )
            {
                return;
            }

            if ( GroupMemberAttributesState.Any( a => a.Guid.Equals( attribute.Guid ) ) )
            {
                // get the non-editable stuff from the state and put it back into the object...
                var attributeState = GroupMemberAttributesState.Where( a => a.Guid.Equals( attribute.Guid ) ).FirstOrDefault();
                if ( attributeState != null )
                {
                    attribute.Order = attributeState.Order;
                    attribute.CreatedDateTime = attributeState.CreatedDateTime;
                    attribute.CreatedByPersonAliasId = attributeState.CreatedByPersonAliasId;
                    attribute.ForeignGuid = attributeState.ForeignGuid;
                    attribute.ForeignId = attributeState.ForeignId;
                    attribute.ForeignKey = attributeState.ForeignKey;
                }

                GroupMemberAttributesState.RemoveEntity( attribute.Guid );
            }
            else
            {
                attribute.Order = GroupMemberAttributesState.Any() ? GroupMemberAttributesState.Max( a => a.Order ) + 1 : 0;
            }

            GroupMemberAttributesState.Add( attribute );

            BindGroupMemberAttributesGrid();

            HideDialog();
        }

        /// <summary>
        /// Binds the group member attributes inherited grid.
        /// </summary>
        private void BindGroupMemberAttributesInheritedGrid()
        {
            // Don't make the Group Member Attributes PanelWidget visible if it's already hidden (due to permissions)
            if ( CurrentGroupTypeCache != null && wpGroupMemberAttributes.Visible )
            {
                wpGroupMemberAttributes.Visible = GroupMemberAttributesInheritedState.Any() || GroupMemberAttributesState.Any() || CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes;
                rcwGroupMemberAttributes.Visible = GroupMemberAttributesInheritedState.Any() || GroupMemberAttributesState.Any() || CurrentGroupTypeCache.AllowSpecificGroupMemberAttributes;
            }

            gGroupMemberAttributesInherited.AddCssClass( "inherited-attribute-grid" );
            gGroupMemberAttributesInherited.DataSource = GroupMemberAttributesInheritedState;
            gGroupMemberAttributesInherited.DataBind();
            rcwGroupMemberAttributesInherited.Visible = GroupMemberAttributesInheritedState.Any();

            rcwGroupMemberAttributes.Label = GroupMemberAttributesInheritedState.Any() ? "Group Member Attributes" : string.Empty;
        }

        /// <summary>
        /// Binds the group member attributes grid.
        /// </summary>
        private void BindGroupMemberAttributesGrid()
        {
            gGroupMemberAttributes.AddCssClass( "attribute-grid" );
            SetAttributeListOrder( GroupMemberAttributesState );
            gGroupMemberAttributes.DataSource = GroupMemberAttributesState.OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList();
            gGroupMemberAttributes.DataBind();
        }

        /// <summary>
        /// Binds the group requirements grids
        /// </summary>
        private void BindGroupRequirementsGrid()
        {
            var rockContext = new RockContext();
            var groupTypeGroupRequirements = new GroupRequirementService( rockContext ).Queryable().Where( a => a.GroupTypeId.HasValue && a.GroupTypeId == CurrentGroupTypeId ).ToList();
            var groupGroupRequirements = GroupRequirementsState.ToList();

            rcwGroupTypeGroupRequirements.Visible = groupTypeGroupRequirements.Any();
            rcwGroupRequirements.Label = groupGroupRequirements.Any() ? "Specific Group Requirements" : string.Empty;

            if ( CurrentGroupTypeCache != null )
            {
                lGroupTypeGroupRequirementsFrom.Text = string.Format( "(From <a href='{0}' target='_blank' rel='noopener noreferrer'>{1}</a>)", this.ResolveUrl( "~/GroupType/" + CurrentGroupTypeCache.Id ), CurrentGroupTypeCache.Name );
                rcwGroupRequirements.Visible = CurrentGroupTypeCache.EnableSpecificGroupRequirements || groupGroupRequirements.Any();
                gGroupRequirements.Actions.ShowAdd = CurrentGroupTypeCache.EnableSpecificGroupRequirements;
                wpGroupRequirements.Visible = groupTypeGroupRequirements.Any() || groupGroupRequirements.Any() || CurrentGroupTypeCache.EnableSpecificGroupRequirements;
            }

            gGroupTypeGroupRequirements.AddCssClass( "grouptype-group-requirements-grid" );
            gGroupTypeGroupRequirements.DataSource = groupTypeGroupRequirements.OrderBy( a => a.GroupRequirementType.Name ).ToList();
            gGroupTypeGroupRequirements.DataBind();

            gGroupRequirements.AddCssClass( "group-requirements-grid" );
            gGroupRequirements.DataSource = groupGroupRequirements.OrderBy( a => a.GroupRequirementType.Name ).ToList();
            gGroupRequirements.DataBind();
        }

        /// <summary>
        /// Sets the schedule display.
        /// </summary>
        private void SetScheduleDisplay()
        {
            dowWeekly.Visible = false;
            timeWeekly.Visible = false;
            spSchedule.Visible = false;
            sbSchedule.Visible = false;

            if ( !string.IsNullOrWhiteSpace( rblScheduleSelect.SelectedValue ) )
            {
                switch ( rblScheduleSelect.SelectedValueAsEnum<ScheduleType>() )
                {
                    case ScheduleType.None:
                        {
                            break;
                        }

                    case ScheduleType.Weekly:
                        {
                            dowWeekly.Visible = true;
                            timeWeekly.Visible = true;
                            break;
                        }

                    case ScheduleType.Custom:
                        {
                            sbSchedule.Visible = true;
                            break;
                        }

                    case ScheduleType.Named:
                        {
                            spSchedule.Visible = true;
                            break;
                        }
                }
            }
        }

        #endregion

        #region Group Member Workflow Trigger Grid and Picker

        /// <summary>
        /// Handles the Add event of the gMemberWorkflowTriggers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gMemberWorkflowTriggers_Add( object sender, EventArgs e )
        {
            gMemberWorkflowTriggers_ShowEdit( Guid.Empty );
        }

        /// <summary>
        /// Handles the Edit event of the gMemberWorkflowTriggers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gMemberWorkflowTriggers_Edit( object sender, RowEventArgs e )
        {
            Guid attributeGuid = ( Guid ) e.RowKeyValue;
            gMemberWorkflowTriggers_ShowEdit( attributeGuid );
        }

        /// <summary>
        /// Gs the group attributes_ show edit.
        /// </summary>
        /// <param name="attributeGuid">The attribute GUID.</param>
        protected void gMemberWorkflowTriggers_ShowEdit( Guid memberWorkflowTriggersGuid )
        {
            ddlTriggerType.BindToEnum<GroupMemberWorkflowTriggerType>( false );

            ddlTriggerFromStatus.BindToEnum<GroupMemberStatus>( false );
            ddlTriggerFromStatus.Items.Insert( 0, new ListItem( "Any", string.Empty ) );

            ddlTriggerToStatus.BindToEnum<GroupMemberStatus>( false );
            ddlTriggerToStatus.Items.Insert( 0, new ListItem( "Any", string.Empty ) );

            ddlTriggerFromRole.Items.Clear();
            ddlTriggerToRole.Items.Clear();
            var groupType = CurrentGroupTypeCache;
            if ( groupType != null )
            {
                ddlTriggerFromRole.DataSource = groupType.Roles;
                ddlTriggerFromRole.DataBind();

                ddlTriggerToRole.DataSource = groupType.Roles;
                ddlTriggerToRole.DataBind();
            }

            ddlTriggerFromRole.Items.Insert( 0, new ListItem( "Any", string.Empty ) );
            ddlTriggerToRole.Items.Insert( 0, new ListItem( "Any", string.Empty ) );

            GroupMemberWorkflowTrigger memberWorkflowTrigger = MemberWorkflowTriggersState.FirstOrDefault( a => a.Guid.Equals( memberWorkflowTriggersGuid ) );
            if ( memberWorkflowTrigger == null )
            {
                memberWorkflowTrigger = new GroupMemberWorkflowTrigger { IsActive = true };
                dlgMemberWorkflowTriggers.Title = "Add Trigger";
            }
            else
            {
                dlgMemberWorkflowTriggers.Title = "Edit Trigger";
            }

            hfTriggerGuid.Value = memberWorkflowTrigger.Guid.ToString();
            tbTriggerName.Text = memberWorkflowTrigger.Name;
            cbTriggerIsActive.Checked = memberWorkflowTrigger.IsActive;

            if ( memberWorkflowTrigger.WorkflowTypeId != 0 )
            {
                var workflowType = new WorkflowTypeService( new RockContext() ).Queryable().FirstOrDefault( a => a.Id == memberWorkflowTrigger.WorkflowTypeId );
                wtpWorkflowType.SetValue( workflowType );
            }
            else
            {
                wtpWorkflowType.SetValue( null );
            }

            ddlTriggerType.SetValue( memberWorkflowTrigger.TriggerType.ConvertToInt() );

            var qualifierParts = ( memberWorkflowTrigger.TypeQualifier ?? string.Empty ).Split( new char[] { '|' } );
            ddlTriggerToStatus.SetValue( qualifierParts.Length > 0 ? qualifierParts[0] : string.Empty );
            ddlTriggerToRole.SetValue( qualifierParts.Length > 1 ? qualifierParts[1] : string.Empty );
            ddlTriggerFromStatus.SetValue( qualifierParts.Length > 2 ? qualifierParts[2] : string.Empty );
            ddlTriggerFromRole.SetValue( qualifierParts.Length > 3 ? qualifierParts[3] : string.Empty );
            cbTriggerFirstTime.Checked = qualifierParts.Length > 4 ? qualifierParts[4].AsBoolean() : false;
            cbTriggerPlacedElsewhereShowNote.Checked = qualifierParts.Length > 5 ? qualifierParts[5].AsBoolean() : false;
            cbTriggerPlacedElsewhereRequireNote.Checked = qualifierParts.Length > 6 ? qualifierParts[6].AsBoolean() : false;

            ShowTriggerQualifierControls();
            ShowDialog( "MemberWorkflowTriggers", true );
        }

        /// <summary>
        /// Shows the trigger qualifier controls.
        /// </summary>
        protected void ShowTriggerQualifierControls()
        {
            var triggerType = ddlTriggerType.SelectedValueAsEnum<GroupMemberWorkflowTriggerType>();
            switch ( triggerType )
            {
                case GroupMemberWorkflowTriggerType.MemberAddedToGroup:
                case GroupMemberWorkflowTriggerType.MemberRemovedFromGroup:
                    {
                        ddlTriggerFromStatus.Visible = false;
                        ddlTriggerToStatus.Label = "With Status of";
                        ddlTriggerToStatus.Visible = true;

                        ddlTriggerFromRole.Visible = false;
                        ddlTriggerToRole.Label = "With Role of";
                        ddlTriggerToRole.Visible = true;

                        cbTriggerFirstTime.Visible = false;

                        cbTriggerPlacedElsewhereShowNote.Visible = false;
                        cbTriggerPlacedElsewhereRequireNote.Visible = false;

                        break;
                    }

                case GroupMemberWorkflowTriggerType.MemberAttendedGroup:
                    {
                        ddlTriggerFromStatus.Visible = false;
                        ddlTriggerToStatus.Visible = false;

                        ddlTriggerFromRole.Visible = false;
                        ddlTriggerToRole.Visible = false;

                        cbTriggerFirstTime.Visible = true;

                        cbTriggerPlacedElsewhereShowNote.Visible = false;
                        cbTriggerPlacedElsewhereRequireNote.Visible = false;

                        break;
                    }

                case GroupMemberWorkflowTriggerType.MemberPlacedElsewhere:
                    {
                        ddlTriggerFromStatus.Visible = false;
                        ddlTriggerToStatus.Visible = false;

                        ddlTriggerFromRole.Visible = false;
                        ddlTriggerToRole.Visible = false;

                        cbTriggerFirstTime.Visible = false;

                        cbTriggerPlacedElsewhereShowNote.Visible = true;
                        cbTriggerPlacedElsewhereRequireNote.Visible = true;

                        break;
                    }

                case GroupMemberWorkflowTriggerType.MemberRoleChanged:
                    {
                        ddlTriggerFromStatus.Visible = false;
                        ddlTriggerToStatus.Visible = false;

                        ddlTriggerFromRole.Visible = true;
                        ddlTriggerToRole.Label = "To Role of";
                        ddlTriggerToRole.Visible = true;

                        cbTriggerFirstTime.Visible = false;

                        cbTriggerPlacedElsewhereShowNote.Visible = false;
                        cbTriggerPlacedElsewhereRequireNote.Visible = false;

                        break;
                    }

                case GroupMemberWorkflowTriggerType.MemberStatusChanged:
                    {
                        ddlTriggerFromStatus.Visible = true;
                        ddlTriggerToStatus.Label = "To Status of";
                        ddlTriggerToStatus.Visible = true;

                        ddlTriggerFromRole.Visible = false;
                        ddlTriggerToRole.Visible = false;

                        cbTriggerFirstTime.Visible = false;

                        cbTriggerPlacedElsewhereShowNote.Visible = false;
                        cbTriggerPlacedElsewhereRequireNote.Visible = false;

                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridReorder event of the gMemberWorkflowTriggers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridReorderEventArgs"/> instance containing the event data.</param>
        protected void gMemberWorkflowTriggers_GridReorder( object sender, GridReorderEventArgs e )
        {
            ReorderMemberWorkflowTriggerList( MemberWorkflowTriggersState, e.OldIndex, e.NewIndex );
            BindMemberWorkflowTriggersGrid();
        }

        /// <summary>
        /// Handles the Delete event of the gMemberWorkflowTriggers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gMemberWorkflowTriggers_Delete( object sender, RowEventArgs e )
        {
            Guid rowGuid = ( Guid ) e.RowKeyValue;
            MemberWorkflowTriggersState.RemoveEntity( rowGuid );

            BindMemberWorkflowTriggersGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gMemberWorkflowTriggers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gMemberWorkflowTriggers_GridRebind( object sender, EventArgs e )
        {
            BindMemberWorkflowTriggersGrid();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlTriggerType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlTriggerType_SelectedIndexChanged( object sender, EventArgs e )
        {
            ShowTriggerQualifierControls();
        }

        /// <summary>
        /// Handles the SaveClick event of the dlgGroupMemberAttribute control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgMemberWorkflowTriggers_SaveClick( object sender, EventArgs e )
        {
            var memberWorkflowTrigger = new GroupMemberWorkflowTrigger();

            var existingMemberWorkflowTrigger = MemberWorkflowTriggersState.FirstOrDefault( r => r.Guid.Equals( hfTriggerGuid.Value.AsGuid() ) );
            if ( existingMemberWorkflowTrigger != null )
            {
                memberWorkflowTrigger.CopyPropertiesFrom( existingMemberWorkflowTrigger );
            }
            else
            {
                memberWorkflowTrigger.Order = MemberWorkflowTriggersState.Any() ? MemberWorkflowTriggersState.Max( a => a.Order ) + 1 : 0;
                memberWorkflowTrigger.GroupId = hfGroupId.ValueAsInt();
            }

            memberWorkflowTrigger.Name = tbTriggerName.Text;
            memberWorkflowTrigger.IsActive = cbTriggerIsActive.Checked;

            var workflowTypeId = wtpWorkflowType.SelectedValueAsInt();
            if ( workflowTypeId.HasValue )
            {
                var workflowType = new WorkflowTypeService( new RockContext() ).Queryable().FirstOrDefault( a => a.Id == workflowTypeId.Value );
                if ( workflowType != null )
                {
                    memberWorkflowTrigger.WorkflowType = workflowType;
                    memberWorkflowTrigger.WorkflowTypeId = workflowType.Id;
                }
                else
                {
                    memberWorkflowTrigger.WorkflowType = null;
                    memberWorkflowTrigger.WorkflowTypeId = 0;
                }
            }
            else
            {
                memberWorkflowTrigger.WorkflowTypeId = 0;
            }

            if ( memberWorkflowTrigger.WorkflowTypeId == 0 )
            {
                nbInvalidWorkflowType.Visible = true;
                return;
            }

            memberWorkflowTrigger.TriggerType = ddlTriggerType.SelectedValueAsEnum<GroupMemberWorkflowTriggerType>();

            memberWorkflowTrigger.TypeQualifier = string.Format(
                "{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                ddlTriggerToStatus.SelectedValue,
                ddlTriggerToRole.SelectedValue,
                ddlTriggerFromStatus.SelectedValue,
                ddlTriggerFromRole.SelectedValue,
                cbTriggerFirstTime.Checked.ToString(),
                cbTriggerPlacedElsewhereShowNote.Checked.ToString(),
                cbTriggerPlacedElsewhereRequireNote.Checked.ToString() );

            // Controls will show warnings
            if ( !memberWorkflowTrigger.IsValid )
            {
                return;
            }

            MemberWorkflowTriggersState.RemoveEntity( memberWorkflowTrigger.Guid );
            MemberWorkflowTriggersState.Add( memberWorkflowTrigger );

            BindMemberWorkflowTriggersGrid();
            HideDialog();
        }

        /// <summary>
        /// Binds the group type attributes grid.
        /// </summary>
        private void BindMemberWorkflowTriggersGrid()
        {
            SetMemberWorkflowTriggerListOrder( MemberWorkflowTriggersState );
            gMemberWorkflowTriggers.DataSource = MemberWorkflowTriggersState.OrderBy( a => a.Order ).ToList();
            gMemberWorkflowTriggers.DataBind();
        }

        /// <summary>
        /// Formats the type of the trigger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="qualifier">The qualifier.</param>
        /// <returns></returns>
        protected string FormatTriggerType( object type, object qualifier )
        {
            var triggerType = type.ToString().ConvertToEnum<GroupMemberWorkflowTriggerType>();
            var typeQualifer = qualifier.ToString();

            var qualiferText = new List<string>();
            var qualifierParts = ( typeQualifer ?? string.Empty ).Split( new char[] { '|' } );

            if ( qualifierParts.Length > 2 && !string.IsNullOrWhiteSpace( qualifierParts[2] ) )
            {
                var status = qualifierParts[2].ConvertToEnum<GroupMemberStatus>();
                qualiferText.Add( string.Format( " from status of <strong>{0}</strong>", status.ConvertToString() ) );
            }

            if ( qualifierParts.Length > 0 && !string.IsNullOrWhiteSpace( qualifierParts[0] ) )
            {
                var status = qualifierParts[0].ConvertToEnum<GroupMemberStatus>();
                if ( triggerType == GroupMemberWorkflowTriggerType.MemberStatusChanged )
                {
                    qualiferText.Add( string.Format( " to status of <strong>{0}</strong>", status.ConvertToString() ) );
                }
                else
                {
                    qualiferText.Add( string.Format( " with status of <strong>{0}</strong>", status.ConvertToString() ) );
                }
            }

            var groupType = CurrentGroupTypeCache;
            if ( groupType != null )
            {
                if ( qualifierParts.Length > 3 && !string.IsNullOrWhiteSpace( qualifierParts[3] ) )
                {
                    Guid roleGuid = qualifierParts[3].AsGuid();
                    var role = groupType.Roles.FirstOrDefault( r => r.Guid.Equals( roleGuid ) );
                    if ( role != null )
                    {
                        qualiferText.Add( string.Format( " from role of <strong>{0}</strong>", role.Name ) );
                    }
                }

                if ( qualifierParts.Length > 1 && !string.IsNullOrWhiteSpace( qualifierParts[1] ) )
                {
                    Guid roleGuid = qualifierParts[1].AsGuid();
                    var role = groupType.Roles.FirstOrDefault( r => r.Guid.Equals( roleGuid ) );
                    if ( role != null )
                    {
                        if ( triggerType == GroupMemberWorkflowTriggerType.MemberRoleChanged )
                        {
                            qualiferText.Add( string.Format( " to role of <strong>{0}</strong>", role.Name ) );
                        }
                        else
                        {
                            qualiferText.Add( string.Format( " with role of <strong>{0}</strong>", role.Name ) );
                        }
                    }
                }
            }

            if ( qualifierParts.Length > 4 && qualifierParts[4].AsBoolean() )
            {
                qualiferText.Add( " for the first time" );
            }

            return triggerType.ConvertToString() + qualiferText.AsDelimited( " and " );
        }

        #endregion

        #region Peer Network Controls

        /// <summary>
        /// Handles the CheckedChanged event of the cbOverrideRelationshipStrength control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void cbOverrideRelationshipStrength_CheckedChanged( object sender, EventArgs e )
        {
            pnlPeerNetwork.Visible = cbOverrideRelationshipStrength.Checked;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblRelationshipStrength control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rblRelationshipStrength_SelectedIndexChanged( object sender, EventArgs e )
        {
            var relationshipStrength = rblRelationshipStrength.SelectedValueAsInt() ?? 0;
            var isAdvancedPanelVisible = swShowPeerNetworkAdvancedSettings.Checked;

            SetPeerNetworkSubControlVisibility( relationshipStrength, isAdvancedPanelVisible );
        }

        /// <summary>
        /// Handles the CheckedChanged event of the swShowPeerNetworkAdvancedSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void swShowPeerNetworkAdvancedSettings_CheckedChanged( object sender, EventArgs e )
        {
            pnlPeerNetworkAdvanced.Visible = swShowPeerNetworkAdvancedSettings.Checked;
        }

        #endregion Peer Network Controls

        /// <summary>
        /// Handles the CheckedChanged event of the cbIsSecurityRole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbIsSecurityRole_CheckedChanged( object sender, EventArgs e )
        {
            var groupId = PageParameter( PageParameterKey.GroupId ).AsIntegerOrNull();
            if ( !groupId.HasValue )
            {
                return;
            }

            var groupType = CurrentGroupTypeCache;
            var group = GetGroup( groupId.Value );
            if ( group == null || groupType == null)
            {
                return;
            }

            group.IsSecurityRole = cbIsSecurityRole.Checked || groupType.Guid == Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid();
            ShowGroupTypeEditDetails( groupType, group, true );
        }

        /// <summary>
        /// Shows the Due Date qualifier controls.
        /// </summary>
        protected void ShowDueDateQualifierControls()
        {
            int groupRequirementTypeId = ddlGroupRequirementType.SelectedValue.AsInteger();
            var rockContext = new RockContext();
            var groupRequirementTypeService = new GroupRequirementTypeService( rockContext );
            var groupRequirementTypes = groupRequirementTypeService.Queryable().Where( grt => grt.Id == groupRequirementTypeId );
            if ( !groupRequirementTypes.Any() )
            {
                return;
            }

            var groupRequirementType = groupRequirementTypes.First();

            switch ( groupRequirementType.DueDateType )
            {
                case DueDateType.Immediate:
                case DueDateType.DaysAfterJoining:
                    {
                        ddlDueDateGroupAttribute.Visible = false;
                        ddlDueDateGroupAttribute.Required = false;
                        dpDueDate.Visible = false;
                        dpDueDate.Required = false;
                        break;
                    }

                case DueDateType.ConfiguredDate:
                    {
                        ddlDueDateGroupAttribute.Visible = false;
                        ddlDueDateGroupAttribute.Required = false;
                        dpDueDate.Visible = true;
                        dpDueDate.Required = true;
                        break;
                    }

                case DueDateType.GroupAttribute:
                    {
                        ddlDueDateGroupAttribute.Visible = true;
                        ddlDueDateGroupAttribute.Required = true;
                        dpDueDate.Visible = false;
                        dpDueDate.Required = false;
                        break;
                    }
            }
        }

        protected void ddlGroupRequirementType_SelectedIndexChanged( object sender, EventArgs e )
        {
            ShowDueDateQualifierControls();
        }
    }

    public class GroupSyncViewModel : GroupSync
    {
        public TimeIntervalSetting ScheduleTimeInterval
        {
            get
            {
                return new TimeIntervalSetting( ScheduleIntervalMinutes, null );
            }
        }
    }
}