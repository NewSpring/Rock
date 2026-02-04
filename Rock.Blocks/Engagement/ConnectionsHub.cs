using System.ComponentModel;
using System.Linq;
using Rock.Attribute;
using Rock.Web.UI;

using Rock.Model;
using Rock.ViewModels.Blocks.Engagement.ConnectionsHub;
using Rock.ViewModels.Blocks;
using Rock.Obsidian.UI;
using Rock.Web.Cache;
using System.Data.Entity;
using Rock.SystemGuid;
using Rock.ViewModels.Core.Grid;
using Rock.Utility;
using System;
using Rock.ViewModels.Utility;
using Rock.Security;
using System.Collections.Generic;
using Rock.Data;
using Newtonsoft.Json;
using Rock.SystemKey;
using Rock.Web;
using Rock.Enums.Connection;
using Rock.Tasks;

namespace Rock.Blocks.Engagement
{
    /// <summary>
    /// Displays the Connections Hub.
    /// </summary>

    [DisplayName( "Connections Hub" )]
    [Category( "Engagement" )]
    [Description( "Displays the Connections Hub." )]
    [IconCssClass( "ti ti-list" )]
    [SupportedSiteTypes( Model.SiteType.Web )]
    [ContextAware( typeof( Campus ), typeof( ConnectionOpportunity ) )]

    #region Block Attributes

    [LinkedPage(
        "Workflow Detail Page",
        Description = "Page used to display details about a workflow.",
        Order = 3,
        Key = AttributeKey.WorkflowDetailPage,
        DefaultValue = Rock.SystemGuid.Page.WORKFLOW_DETAIL )]

    [LinkedPage(
        "Workflow Entry Page",
        Description = "Page used to launch a new workflow of the selected type.",
        Order = 4,
        Key = AttributeKey.WorkflowEntryPage,
        DefaultValue = Rock.SystemGuid.Page.WORKFLOW_ENTRY )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "CEE15B88-3B23-4378-9CB1-E59A97A94D1B" )]
    [Rock.SystemGuid.BlockTypeGuid( "8674FB3A-9E0E-421C-821C-2DA862A20ED2" )]
    public class ConnectionsHub : RockBlockType
    {
        #region Keys

        private static class AttributeKey
        {
            public const string WorkflowDetailPage = "WorkflowDetailPage";
            public const string WorkflowEntryPage = "WorkflowEntryPage";
        }

        private static class PageParameterKey
        {
            public const string ConnectionType = "ConnectionType";
            public const string Connector = "Connector";
            public const string ConnectionOpportunity = "ConnectionOpportunity";
            public const string Request = "Request";
        }

        private static class PreferenceKey
        {
            public const string ConnectionmOpportunityFilterConnectionTypeIdKey = "ConnectionOpportunityFilter_ConnectionTypeIdKey_{0}";
            public const string SelectedGroupByMode = "SelectedGroupByMode";
        }

        #endregion Keys

        #region Methods

        public override object GetObsidianBlockInitialization()
        {
            var box = new ListBlockBox<ConnectionsHubOptionsBag>();
            var builder = GetGridBuilder();
            box.Options = GetOptions();

            box.GridDefinition = builder.BuildDefinition();

            return box;
        }

        private ConnectionsHubOptionsBag GetOptions()
        {
            var options = new ConnectionsHubOptionsBag();
            ConnectionType connectionType;

            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).GetInclude( PageParameter( PageParameterKey.ConnectionOpportunity ), o => o.ConnectionType, !PageCache.Layout.Site.DisablePredictableIds );

            if ( connectionOpportunity != null )
            {
                options.ConnectionOpportunityGuidFromPageParameter = connectionOpportunity.Guid;
                connectionType = connectionOpportunity.ConnectionType;
            }
            else
            {
                connectionType = new ConnectionTypeService( RockContext ).GetInclude( PageParameter( PageParameterKey.ConnectionType ), a => a.ConnectionStatuses, !PageCache.Layout.Site.DisablePredictableIds );
            }

            if ( connectionType == null )
            {
                return options;
            }

            options.Title = connectionType.Name + " Requests";
            options.IconCssClass = connectionType.IconCssClass;

            var connectionTypeIdKey = IdHasher.Instance.GetHash( connectionType.Id );
            options.ConnectionTypeIdKey = connectionTypeIdKey;
            options.IsSequentialStatusMode = connectionType.IsSequentialStatusEnforced;

            List<Rock.Enums.Connection.ConnectionState> ignoredConnectionStates = new List<Rock.Enums.Connection.ConnectionState>();

            if ( !connectionType.EnableFutureFollowup )
            {
                ignoredConnectionStates.Add( Rock.Enums.Connection.ConnectionState.FutureFollowUp );
            }

            var connectors = connectionType.ConnectionOpportunities
                .Where( o => o.IsActive )
                .SelectMany( o => o.ConnectionOpportunityConnectorGroups )
                .SelectMany( g => g.ConnectorGroup.Members )
                .DistinctBy( m => m.Person.PrimaryAlias.Guid )
                .Select( m => new ListItemBag
                {
                    Value = m.Person.PrimaryAlias.Guid.ToString(),
                    Text = m.Person.FullName
                } )
                .ToList();

            var currentPersonAliasGuid = RequestContext.CurrentPerson.PrimaryAlias?.Guid;

            // Add current person to the connector list to mirror Webforms
            if ( currentPersonAliasGuid.HasValue && !connectors.Any( c => c.Value == currentPersonAliasGuid.Value.ToString() ) )
            {
                connectors.Add( new ListItemBag
                {
                    Text = RequestContext.CurrentPerson.FullName,
                    Value = currentPersonAliasGuid.Value.ToString()
                } );
            }

            options.AllPossibleConnectors = connectors;
            options.ConnectionOpportunities = connectionType.ConnectionOpportunities.Where( o => o.IsActive ).ToListItemBagList();
            options.ConnectionStates = typeof( Rock.Enums.Connection.ConnectionState ).ToEnumListItemBag()
                .Where( i => !ignoredConnectionStates.Contains( ( Rock.Enums.Connection.ConnectionState ) i.Value.AsInteger() ) )
                .ToList();
            options.RequestSourceItems = connectionType.ConnectionTypeSources.ToListItemBagList();
            options.IsFutureFollowUpEnabled = connectionType.EnableFutureFollowup;
            options.IsRequestSecurityEnabled = connectionType.EnableRequestSecurity;
            options.AreCelebrationsEnabled = connectionType.EnabledFeatures.HasFlag( EnabledFeatureFlags.Celebration );
            options.AreRemindersEnabled = connectionType.EnabledFeatures.HasFlag( EnabledFeatureFlags.Reminder );
            options.AreGroupPlacementsEnabled = connectionType.EnabledFeatures.HasFlag( EnabledFeatureFlags.GroupPlacement );

            var manualWorkflows = new List<ConnectionWorkflow>();
            var authorizedWorkflowItems = new List<ListItemBag>();

            manualWorkflows.AddRange( connectionType.ConnectionWorkflows
                .Where( w => w.TriggerType == ConnectionWorkflowTriggerType.Manual && ( w.WorkflowType.IsActive ?? true ) ) // Mirroring Webforms by setting IsActive to true by default.
                .ToList() );

            manualWorkflows.AddRange( connectionType.ConnectionOpportunities
                .SelectMany( o => o.ConnectionWorkflows )
                .Where( w => w.TriggerType == ConnectionWorkflowTriggerType.Manual && ( w.WorkflowType.IsActive ?? true ) ) // Mirroring Webforms by setting IsActive to true by default.
                .ToList()
            );

            // TODO - test performance.

            foreach ( var manualWorkflow in manualWorkflows )
            {
                if ( manualWorkflow.WorkflowType.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                {
                    authorizedWorkflowItems.Add( new ListItemBag
                    {
                        Text = manualWorkflow.WorkflowType.Name,
                        Value = manualWorkflow.Guid.ToString(),
                        Category = manualWorkflow.ConnectionTypeId.HasValue ? "Connection Type Workflows" : "Connection Opportunity Workflows"
                    } );
                }
            }

            options.WorkflowItems = authorizedWorkflowItems;

            options.ConnectionStatuses = connectionType.ConnectionStatuses.Select( s => new ConnectionStatusBag
            {
                Guid = s.Guid,
                Name = s.Name,
                HighlightColor = s.HighlightColor,
                Order = s.Order,
                IsNoteRequiredOnCompletion = s.IsNoteRequiredOnCompletion
            } ).ToList();

            var tempConnectionRequest = new ConnectionRequest
            {
                ConnectionTypeId = connectionType.Id
            };

            tempConnectionRequest.LoadAttributes();

            options.ConnectionTypeRequestAttributes = tempConnectionRequest.GetPublicAttributesForEdit( RequestContext.CurrentPerson );


            var connectionOpportunityFilter = GetConnectionOpportunityFilter( connectionTypeIdKey );

            if ( connectionOpportunity == null && connectionOpportunityFilter.HasValue )
            {
                connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( connectionOpportunityFilter.Value );
            }

            if ( connectionOpportunity != null )
            {
                options.ConnectionOpportunityDetailsFromFilter = GetConnectionOpportunityDetailBag( connectionOpportunity );
            }

            // The values should equal the field names for each respective column.
            options.GridDataToShowItems = new List<ListItemBag>
            {
                new ListItemBag
                {
                    Text = "Due Date",
                    Value = "dueDate"
                },
                new ListItemBag
                {
                    Text = "Opportunity",
                    Value = "connectionOpportunity"
                },
                new ListItemBag
                {
                    Text = "Activity Count / Days",
                    Value = "activity"
                }
            };

            options.GridDataToShowItems.AddRange(
                tempConnectionRequest.Attributes
                    .Select( a => a.Value )
                    .Where( a => a.IsGridColumn )
                    .Select( a =>
                    {
                        var item = a.ToListItemBag();
                        // Overwrite the value with the field key that will be populated in the grid.
                        item.Value = $"attr_{a.Key}";
                        return item;
                    } )
            );

            options.ConnectionActivities = connectionType.ConnectionActivityTypes.Select( a => new ConnectionActivityBag
            {
                ActivityType = a.ToListItemBag(),
                PersonNoteCreationBehavior = a.PersonNoteCreationBehavior
            } ).ToList();

            return options;
        }

        private Guid? GetConnectionOpportunityFilter( string connectionTypeIdKey )
        {
            var preferences = GetBlockPersonPreferences();

            return preferences.GetValue( string.Format( PreferenceKey.ConnectionmOpportunityFilterConnectionTypeIdKey, connectionTypeIdKey ) ).AsGuidOrNull();
        }

        private ConnectionOpportunityDetailBag GetConnectionOpportunityDetailBag( ConnectionOpportunity connectionOpportunity )
        {
            var campusContext = RequestContext.GetContextEntity<Campus>();
            int? campusId = campusContext?.Id;

            var connectors = connectionOpportunity.ConnectionOpportunityConnectorGroups
                .Where( g => !campusId.HasValue || !g.CampusId.HasValue || g.CampusId.Value == campusId.Value )
                .SelectMany( g => g.ConnectorGroup.Members )
                .Select( m => new ListItemBag
                {
                    Value = m.Person.PrimaryAlias.Guid.ToString(),
                    Text = m.Person.FullName
                } )
                .DistinctBy( i => i.Value )
                .ToList();

            var currentPersonAliasGuid = RequestContext.CurrentPerson.PrimaryAlias?.Guid;

            // Add current person to the connector list to mirror Webforms
            if ( currentPersonAliasGuid.HasValue && !connectors.Any( c => c.Value == currentPersonAliasGuid.Value.ToString() ) )
            {
                connectors.Add( new ListItemBag
                {
                    Text = RequestContext.CurrentPerson.FullName,
                    Value = currentPersonAliasGuid.Value.ToString()
                } );
            }

            string defaultConnectorPersonAliasGuid = string.Empty;

            if ( campusId.HasValue )
            {
                var defaultConnector = connectionOpportunity.ConnectionOpportunityCampuses.Where( c => c.CampusId == campusId.Value && c.DefaultConnectorPersonAliasId.HasValue )
                    .Select( c => c.DefaultConnectorPersonAlias )
                    .FirstOrDefault();

                if ( defaultConnector != null )
                {
                    defaultConnectorPersonAliasGuid = defaultConnector.Guid.ToString();
                }
            }

            // TODO - hide if not enabled at connection type level
            var placementGroups = connectionOpportunity.ConnectionOpportunityGroups.Select( g => g.Group )
                .Where( g => !campusId.HasValue || !g.CampusId.HasValue || g.CampusId.Value == campusId.Value )
                .Select( g => new ListItemBag
                {
                    Text = g.CampusId.HasValue ? $"{g.Name} ({g.Campus.Name})" : $"{g.Name} (No Campus)",
                    Value = g.Guid.ToString()
                } )
                .ToList();

            var tempConnectionRequest = new ConnectionRequest
            {
                ConnectionOpportunityId = connectionOpportunity.Id
            };

            tempConnectionRequest.LoadAttributes();

            return new ConnectionOpportunityDetailBag
            {
                IdKey = IdHasher.Instance.GetHash( connectionOpportunity.Id ),
                DefaultConnectorPersonAliasGuid = defaultConnectorPersonAliasGuid,
                Connectors = connectors,
                PlacementGroups = placementGroups,
                ConnectionOpportunityRequestAttributes = tempConnectionRequest.GetPublicAttributesForEdit( RequestContext.CurrentPerson )
            };
        }

        private GroupingFieldBag GetGroupingFieldBag( int? id, string type, string label, int? order = null, string iconCssClass = null, PersonFieldBag person = null, string textColorCssClass = null )
        {
            if ( !id.HasValue )
            {
                if ( type == "person" )
                {
                    person = new PersonFieldBag
                    {
                        IdKey = string.Empty,
                        NickName = "Unassigned",
                        PhotoUrl = Rock.Model.Person.GetPersonNoPictureUrl( new Rock.Model.Person() ),
                    };
                }

                return new GroupingFieldBag
                {
                    Key = "unassigned",
                    Type = type,
                    Label = "Unassigned",
                    Person = person,
                    Order = order,
                    TextColorCssClass = textColorCssClass
                };
            }

            return new GroupingFieldBag
            {
                Key = IdHasher.Instance.GetHash( id.Value ),
                Type = type,
                Label = label,
                IconCssClass = iconCssClass,
                Person = person,
                Order = order,
                TextColorCssClass = textColorCssClass
            };
        }

        private string GetDueStatusTextColorCssClass( DueStatus dueStatus )
        {
            switch ( dueStatus )
            {
                case DueStatus.DueLater:
                    return "text-interface-strong";
                case DueStatus.DueSoon:
                    return "text-warning-strong";
                case DueStatus.Overdue:
                    return "text-danger-strong";
                default:
                    return "text-interface-strong";
            }
        }

        private string GetStateIconCssClass( ConnectionState state )
        {
            switch ( state )
            {
                case ConnectionState.Active:
                    return "ti ti-bolt";
                case ConnectionState.Inactive:
                    return "ti ti-bolt-off";
                case ConnectionState.FutureFollowUp:
                    return "ti ti-calendar-clock";
                case ConnectionState.Connected:
                    return "ti ti-circle-check-filled";
                default:
                    return "ti ti-bolt";
            }
        }

        private DueStatus GetDueStatus( DateTime? dueDate, DateTime? dueSoonDate )
        {
            var now = RockDateTime.Now.Date;

            if ( !dueDate.HasValue )
            {
                return DueStatus.DueLater;
            }

            var due = dueDate.Value.Date;

            if ( now > due )
            {
                return DueStatus.Overdue;
            }

            if ( dueSoonDate.HasValue && now >= dueSoonDate.Value.Date )
            {
                return DueStatus.DueSoon;
            }

            return DueStatus.DueLater;
        }

        private bool TryGetEntityForEditAction( string idKey, out ConnectionRequest entity, out BlockActionResult error )
        {
            var entityService = new ConnectionRequestService( RockContext );
            error = null;

            // Determine if we are editing an existing entity or creating a new one.
            if ( idKey.IsNotNullOrWhiteSpace() )
            {
                // If editing an existing entity then load it and make sure it
                // was found and can still be edited.
                entity = entityService.Get( idKey, !PageCache.Layout.Site.DisablePredictableIds );
            }
            else
            {
                entity = new ConnectionRequest();
                entity.ConnectionTypeId = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds )?.Id ?? 0;
                entityService.Add( entity );
            }

            if ( entity == null )
            {
                error = ActionBadRequest( $"{ConnectionRequest.FriendlyTypeName} not found." );
                return false;
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                error = ActionBadRequest( $"Not authorized to edit ${ConnectionRequest.FriendlyTypeName}." );
                return false;
            }

            return true;
        }

        private bool UpdateEntityFromBox( ConnectionRequest entity, ValidPropertiesBox<ConnectionRequestBag> box )
        {
            if ( box?.Bag == null || box.ValidProperties == null )
            {
                return false;
            }

            var isConnectionOpportunityValid = box.IfValidProperty( nameof( box.Bag.ConnectionOpportunityGuid ), () =>
            {
                var connectionOpportunityId = new ConnectionOpportunityService( RockContext ).GetId( box.Bag.ConnectionOpportunityGuid.AsGuid() );

                if ( !connectionOpportunityId.HasValue )
                {
                    return false;
                }

                entity.ConnectionOpportunityId = connectionOpportunityId.Value;
                return true;
            }, true);

            if ( !isConnectionOpportunityValid )
            {
                return false;
            }

            box.IfValidProperty( nameof( box.Bag.Requester ),
                () => entity.PersonAliasId = box.Bag.Requester.GetEntityId<PersonAlias>( RockContext ).Value );

            box.IfValidProperty( nameof( box.Bag.ConnectorPersonAliasGuid ),
                () => entity.ConnectorPersonAliasId = new PersonAliasService( RockContext ).GetId( box.Bag.ConnectorPersonAliasGuid.AsGuid() ) );

            box.IfValidProperty( nameof( box.Bag.ConnectionState ), () =>
            {
                var state = box.Bag.ConnectionState ?? Rock.Enums.Connection.ConnectionState.Active;

                entity.ConnectionState = ( ConnectionState ) ( int ) state;
            } );

            if ( entity.ConnectionState == ConnectionState.FutureFollowUp )
            {
                box.IfValidProperty( nameof( box.Bag.FollowUpDate ),
                    () => entity.FollowupDate = box.Bag.FollowUpDate?.DateTime );
            }

            var isConnectionStatusValid = box.IfValidProperty( nameof( box.Bag.ConnectionStatusGuid ), () =>
            {
                var connectionStatusId = new ConnectionStatusService( RockContext ).GetId( box.Bag.ConnectionStatusGuid.AsGuid() );

                if ( !connectionStatusId.HasValue )
                {
                    return false;
                }

                entity.ConnectionStatusId = connectionStatusId.Value;
                return true;
            }, true );

            if ( !isConnectionStatusValid )
            {
                return false;
            }

            box.IfValidProperty( nameof( box.Bag.PlacementGroupGuid ),
                () => entity.AssignedGroupId = GroupCache.GetId( box.Bag.PlacementGroupGuid.AsGuid() ) );

            box.IfValidProperty( nameof( box.Bag.GroupMemberRoleGuid ),
                () => entity.AssignedGroupMemberRoleId = GroupTypeRoleCache.GetId( box.Bag.GroupMemberRoleGuid.AsGuid() ) );

            box.IfValidProperty( nameof( box.Bag.GroupMemberStatus ),
                () => entity.AssignedGroupMemberStatus = box.Bag.GroupMemberStatus );

            box.IfValidProperty( nameof( box.Bag.Comments ),
                () => entity.Comments = box.Bag.Comments );

            box.IfValidProperty( nameof( box.Bag.RequestSourceGuid ),
                () => entity.ConnectionTypeSourceId = new ConnectionTypeSourceService( RockContext ).GetId( box.Bag.RequestSourceGuid.AsGuid() ) );

            box.IfValidProperty( nameof( box.Bag.ConnectionRequestAttributeValues ), () =>
            {
                entity.LoadAttributes( RockContext );
                entity.SetPublicAttributeValues( box.Bag.ConnectionRequestAttributeValues, RequestContext.CurrentPerson );
            } );

            box.IfValidProperty( nameof( box.Bag.PlacementGroupMemberAttributeValues ),
                () => entity.AssignedGroupMemberAttributeValues = GetGroupMemberAttributeValuesFromBag( box.Bag.PlacementGroupMemberAttributeValues, entity.AssignedGroupId, entity.AssignedGroupMemberRoleId, entity.AssignedGroupMemberStatus ) );

            return true;
        }

        /// <summary>
        /// Gets the group member attribute values.
        /// </summary>
        /// <returns></returns>
        private string GetGroupMemberAttributeValuesFromBag( Dictionary<string, string> attributeValues, int? groupId, int? groupMemberRoleId, GroupMemberStatus? groupMemberStatus )
        {
            var values = new Dictionary<string, string>();

            if ( !groupId.HasValue || !groupMemberRoleId.HasValue || !groupMemberStatus.HasValue )
            {
                return string.Empty;
            }

            var groupMember = new Rock.Model.GroupMember
            {
                GroupId = groupId.Value,
                GroupRoleId = groupMemberRoleId.Value,
                GroupMemberStatus = groupMemberStatus.Value
            };

            groupMember.LoadAttributes();
            groupMember.SetPublicAttributeValues( attributeValues, RequestContext.CurrentPerson );

            foreach ( var attrValue in groupMember.AttributeValues )
            {
                values.Add( attrValue.Key, attrValue.Value.Value );
            }

            return JsonConvert.SerializeObject( values, Formatting.None );
        }

        private bool CanEditConnectionRequest( ConnectionTypeCache connectionType, string connectionRequestIdKey, out ConnectionRequest connectionRequest, out BlockActionResult error )
        {
            var canEdit = CanEditConnectionRequests( connectionType, new List<string> { connectionRequestIdKey }, out var connectionRequests, out error );
            connectionRequest = connectionRequests.FirstOrDefault();
            return canEdit;
        }

        private bool CanEditConnectionRequests( ConnectionTypeCache connectionType, List<string> connectionRequestIdKeys, out List<ConnectionRequest> connectionRequests, out BlockActionResult error, Func<IQueryable<ConnectionRequest>, IQueryable<ConnectionRequest>> queryModifier = null )
        {
            error = null;
            var decodedIds = connectionRequestIdKeys.Select( key => Rock.Utility.IdHasher.Instance.GetId( key ) ).ToList();

            if ( decodedIds.Any( id => !id.HasValue ) )
            {
                connectionRequests = new List<ConnectionRequest>();
                error = ActionBadRequest( $"{ConnectionRequest.FriendlyTypeName} not found." );
            }

            var connectionRequestIds = decodedIds
                .Select( id => id.Value )
                .Distinct()
                .ToList();

            if ( !connectionRequestIds.Any() )
            {
                connectionRequests = new List<ConnectionRequest>();
                error = ActionBadRequest( $"{ConnectionRequest.FriendlyTypeName} not found." );
            }

            var connectionRequestsQry = new ConnectionRequestService( RockContext ).GetByIds( connectionRequestIds );

            if ( queryModifier != null )
            {
                connectionRequestsQry = queryModifier( connectionRequestsQry );
            }

            connectionRequests = connectionRequestsQry.ToList();

            if ( connectionRequests.Count != connectionRequestIds.Count )
            {
                error = ActionBadRequest( $"{ConnectionRequest.FriendlyTypeName} not found." );
            }

            bool userCanEditConnectionRequest = false;
            var opportunityIds = connectionRequests.Select( r => r.ConnectionOpportunityId )
                .Distinct()
                .ToList();

            if ( connectionType.EnableRequestSecurity )
            {
                userCanEditConnectionRequest = connectionRequests.All( cr => cr.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) );
            }
            else
            {
                var opportunities = new ConnectionOpportunityService( RockContext ).Queryable()
                    .AsNoTracking()
                    .Where( o => opportunityIds.Contains( o.Id ) )
                    .ToList();

                userCanEditConnectionRequest = opportunities.All( co => co.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) );
            }

            if ( !userCanEditConnectionRequest )
            {
                var connectorGroupsByOpportunity = new ConnectionOpportunityConnectorGroupService( RockContext ).Queryable()
                    .AsNoTracking()
                    .Where( g => opportunityIds.Contains( g.ConnectionOpportunityId ) )
                    .Where( g =>
                        g.ConnectorGroup != null &&
                        g.ConnectorGroup.Members.Any( m =>
                            m.PersonId == RequestContext.CurrentPerson.Id &&
                            m.GroupMemberStatus == GroupMemberStatus.Active ) )
                    .ToList()
                    .GroupBy( g => g.ConnectionOpportunityId )
                    .ToDictionary( g => g.Key, g => g.ToList() );

                var activeCampusCount = CampusCache.All().Count( c => c.IsActive ?? true );

                userCanEditConnectionRequest = connectionRequests.All( cr =>
                {
                    // TODO - test if this is causing performance issues.
                    if ( cr.ConnectorPersonAlias != null && cr.ConnectorPersonAlias.PersonId == RequestContext.CurrentPerson.Id )
                    {
                        return true;
                    }

                    if ( !connectorGroupsByOpportunity.TryGetValue( cr.ConnectionOpportunityId, out var groups ) )
                    {
                        return false;
                    }

                    // Non campus-specific connector groups
                    if ( activeCampusCount == 1 || groups.Any( g => !g.CampusId.HasValue ) )
                    {
                        return true;
                    }

                    // New Connection Request
                    if ( cr.Id == 0 )
                    {
                        return true;
                    }

                    // Campus-specific logic
                    if ( cr.CampusId.HasValue && groups.Any( g => g.CampusId == cr.CampusId.Value ) )
                    {
                        return true;
                    }

                    return false;

                } );
            }

            if ( !userCanEditConnectionRequest )
            {
                error = ActionBadRequest( $"Not authorized to edit ${ConnectionRequest.FriendlyTypeName}." );
            }

            return userCanEditConnectionRequest;
        }

        private List<int> GetDataViewValues( int dataViewId )
        {
            var dataView = DataViewCache.Get( dataViewId );
            if ( dataView == null )
            {
                return new List<int>();
            }

            if ( dataView.IsPersisted() && dataView.PersistedLastRefreshDateTime.HasValue )
            {
                return RockContext.Set<DataViewPersistedValue>().Select( e => e.EntityId ).ToList();
            }
            else
            {
                var dataViewGetQueryArgs = new Rock.Reporting.GetQueryableOptions { DbContext = RockContext, DatabaseTimeoutSeconds = 30 };
                return dataView.GetQuery( dataViewGetQueryArgs ).Select( e => e.Id ).ToList();
            }
        }

        private ConnectionTypeCache GetConnectionTypeCacheFromPageParameters()
        {
            ConnectionTypeCache connectionType;

            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( PageParameter( PageParameterKey.ConnectionOpportunity ), !PageCache.Layout.Site.DisablePredictableIds );

            if ( connectionOpportunity != null )
            {
                connectionType = ConnectionTypeCache.Get( connectionOpportunity.ConnectionTypeId );
            }
            else
            {
                connectionType = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds );
            }

            return connectionType;
        }

        #endregion Methods

        #region Block Actions

        [BlockAction]
        public BlockActionResult GetGridData()
        {
            ConnectionType connectionType;

            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).GetInclude( PageParameter( PageParameterKey.ConnectionOpportunity ), o => o.ConnectionType, !PageCache.Layout.Site.DisablePredictableIds );

            if ( connectionOpportunity != null )
            {
                connectionType = connectionOpportunity.ConnectionType;
            }
            else
            {
                connectionType = new ConnectionTypeService( RockContext ).GetInclude( PageParameter( PageParameterKey.ConnectionType ), a => a.ConnectionStatuses, !PageCache.Layout.Site.DisablePredictableIds );
            }

            if ( connectionType == null )
            {
                return ActionOk();
            }

            var reminderQry = new ReminderService( RockContext ).Queryable()
                .Include( r => r.PersonAlias )
                .AsNoTracking()
                .Where( r => !r.IsComplete && r.ReminderDate < RockDateTime.Now && r.PersonAlias.PersonId == RequestContext.CurrentPerson.Id );

            var connectionRequestsQry = new ConnectionRequestService( RockContext ).Queryable()
                .AsNoTracking()
                .Where( cr => cr.ConnectionOpportunity.ConnectionTypeId == connectionType.Id
                    && cr.ConnectionOpportunity.IsActive )
                .Select( a => new ConnectionRow
                {
                    ConnectionRequestId = a.Id,
                    ConnectionRequest = a,
                    ConnectorGroupingProjection = new GroupingProjection
                    {
                        Id = a.ConnectorPersonAliasId,
                        Label = a.ConnectorPersonAlias != null ? a.ConnectorPersonAlias.Person.NickName + " " + a.ConnectorPersonAlias.Person.LastName : string.Empty,
                        Order = ( int? ) null
                    },
                    OpportunityGroupingProjection = new GroupingProjection
                    {
                        Id = a.ConnectionOpportunityId,
                        Label = a.ConnectionOpportunity.Name,
                        Order = a.ConnectionOpportunity.Order
                    },
                    CampusGroupingProjection = new GroupingProjection
                    {
                        Id = a.CampusId,
                        Label = a.Campus != null ? a.Campus.Name : string.Empty,
                        Order = a.Campus != null ? a.Campus.Order : ( int? ) null
                    },
                    ConnectorPersonProjection = new PersonProjection
                    {
                        NickName = a.ConnectorPersonAlias.Person.NickName,
                        LastName = a.ConnectorPersonAlias.Person.LastName,
                        PhotoId = a.ConnectorPersonAlias.Person.PhotoId,
                        Age = a.ConnectorPersonAlias.Person.Age,
                        Gender = a.ConnectorPersonAlias.Person.Gender,
                        RecordTypeValueId = a.ConnectorPersonAlias.Person.RecordTypeValueId,
                        AgeClassification = a.ConnectorPersonAlias.Person.AgeClassification,
                        ConnectionStatusValueId = a.ConnectorPersonAlias.Person.ConnectionStatusValueId,
                        Id = a.ConnectorPersonAlias.Person.Id,
                    },
                    StatusGroupingProjection = new GroupingProjection
                    {
                        Id = a.ConnectionStatusId,
                        Label = a.ConnectionStatus != null ? a.ConnectionStatus.Name : string.Empty,
                        Order = a.ConnectionStatus != null ? a.ConnectionStatus.Order : ( int? ) null
                    },
                    ConnectionOpportunityId = a.ConnectionOpportunityId,
                    ConnectionOpportunityGuid = a.ConnectionOpportunity.Guid,
                    ConnectionOpportunity = a.ConnectionOpportunity.Name,
                    ConnectionOpportunityIcon = a.ConnectionOpportunity.IconCssClass,
                    ConnectionTypeSource = a.ConnectionTypeSource != null ? a.ConnectionTypeSource.Name : string.Empty,
                    CampusId = a.CampusId,
                    Campus = a.Campus != null ? a.Campus.Name : string.Empty,
                    GroupId = a.AssignedGroupId,
                    Group = a.AssignedGroup != null ? a.AssignedGroup.Name : string.Empty,
                    ConnectionStatusProjection = new ConnectionStatusProjection
                    {
                        Guid = a.ConnectionStatus.Guid,
                        Name = a.ConnectionStatus.Name, // TODO - Test what happens when a Connection Status is deleted.
                        Order = a.ConnectionStatus.Order,
                        HighlightColor = a.ConnectionStatus.HighlightColor,
                        IsNotRequiredOnCompletion = a.ConnectionStatus.IsNoteRequiredOnCompletion
                    },
                    ConnectionState = a.ConnectionState,
                    LastActivityDateTime = a.ConnectionRequestActivities.Select( cra => cra.CreatedDateTime )
                        .OrderByDescending( d => d )
                        .FirstOrDefault(),
                    ActivityCount = a.ConnectionRequestActivities.Count(),
                    FollowUpDate = a.FollowupDate,
                    CreatedDateTime = a.CreatedDateTime,
                    DueDate = a.DueDate,
                    DueSoonDate = a.DueSoonDate,
                    CelebrationText = a.CelebrationText,
                    PersonProjection = new PersonProjection
                    {
                        NickName = a.PersonAlias.Person.NickName,
                        LastName = a.PersonAlias.Person.LastName,
                        PhotoId = a.PersonAlias.Person.PhotoId,
                        Age = a.PersonAlias.Person.Age,
                        Gender = a.PersonAlias.Person.Gender,
                        RecordTypeValueId = a.PersonAlias.Person.RecordTypeValueId,
                        AgeClassification = a.PersonAlias.Person.AgeClassification,
                        ConnectionStatusValueId = a.PersonAlias.Person.ConnectionStatusValueId,
                        Id = a.PersonAlias.Person.Id,
                    },
                    RequesterPersonAliasGuid = a.PersonAlias.Guid,
                    ReminderCount = reminderQry.Count( r => r.EntityId == a.PersonAliasId )
                } );

            var campusContext = RequestContext.GetContextEntity<Campus>();
            if ( campusContext != null )
            {
                connectionRequestsQry = connectionRequestsQry.Where( c => c.CampusId == campusContext.Id );
            }

            var connectionOpportunityFilter = GetConnectionOpportunityFilter( IdHasher.Instance.GetHash( connectionType.Id ) );
            if ( connectionOpportunityFilter.HasValue )
            {
                connectionRequestsQry = connectionRequestsQry.Where( c => c.ConnectionOpportunityGuid == connectionOpportunityFilter.Value );
            }

            var opportunityContext = RequestContext.GetContextEntity<ConnectionOpportunity>();
            if ( opportunityContext != null )
            {
                connectionRequestsQry = connectionRequestsQry.Where( c => c.ConnectionOpportunityId == opportunityContext.Id );
            }

            var connectionRequests = connectionRequestsQry.ToList();

            foreach ( var request in connectionRequests )
            {
                request.ConnectionStatus = new ConnectionStatusBag
                {
                    Guid = request.ConnectionStatusProjection.Guid,
                    Name = request.ConnectionStatusProjection.Name,
                    Order = request.ConnectionStatusProjection.Order,
                    HighlightColor = request.ConnectionStatusProjection.HighlightColor,
                    IsNoteRequiredOnCompletion = request.ConnectionStatusProjection.IsNotRequiredOnCompletion
                };

                request.Person = new PersonFieldBag
                {
                    IdKey = IdHasher.Instance.GetHash( request.PersonProjection.Id.Value ),
                    NickName = request.PersonProjection.NickName,
                    LastName = request.PersonProjection.LastName
                };

                var initials = $"{request.Person.NickName.Truncate( 1, false )}{request.Person.LastName.Truncate( 1, false )}";
                request.Person.PhotoUrl = Rock.Model.Person.GetPersonPhotoUrl(
                    initials,
                    request.PersonProjection.PhotoId,
                    request.PersonProjection.Age,
                    request.PersonProjection.Gender ?? Gender.Unknown,
                    request.PersonProjection.RecordTypeValueId,
                    request.PersonProjection.AgeClassification
                );

                if ( request.PersonProjection.ConnectionStatusValueId.HasValue )
                {
                    var connectionStatusValue = DefinedValueCache.Get( request.PersonProjection.ConnectionStatusValueId.Value );
                    if ( connectionStatusValue != null )
                    {
                        request.Person.ConnectionStatus = connectionStatusValue.Value;
                    }
                }

                PersonFieldBag connectorPerson = null;

                if ( request.ConnectorPersonProjection.Id.HasValue )
                {
                    connectorPerson = new PersonFieldBag
                    {
                        IdKey = IdHasher.Instance.GetHash( request.ConnectorPersonProjection.Id.Value ),
                        NickName = request.ConnectorPersonProjection.NickName,
                        LastName = request.ConnectorPersonProjection.LastName
                    };


                    var connectorInitials = $"{connectorPerson.NickName.Truncate( 1, false )}{connectorPerson.LastName.Truncate( 1, false )}";
                    connectorPerson.PhotoUrl = Rock.Model.Person.GetPersonPhotoUrl(
                        connectorInitials,
                        request.ConnectorPersonProjection.PhotoId,
                        request.ConnectorPersonProjection.Age,
                        request.ConnectorPersonProjection.Gender ?? Gender.Unknown,
                        request.ConnectorPersonProjection.RecordTypeValueId,
                        request.ConnectorPersonProjection.AgeClassification
                    );

                    if ( request.ConnectorPersonProjection.ConnectionStatusValueId.HasValue )
                    {
                        var connectionStatusValue = DefinedValueCache.Get( request.ConnectorPersonProjection.ConnectionStatusValueId.Value );
                        if ( connectionStatusValue != null )
                        {
                            connectorPerson.ConnectionStatus = connectionStatusValue.Value;
                        }
                    }
                }

                var dueStatus = GetDueStatus( request.DueDate, request.DueSoonDate );
                request.DueStatus = dueStatus;
                request.ConnectorPerson = connectorPerson;

                request.ConnectorGrouping = GetGroupingFieldBag( request.ConnectorGroupingProjection.Id, "person", request.ConnectorGroupingProjection.Label, null, null, connectorPerson );
                request.OpportunityGrouping = GetGroupingFieldBag( request.OpportunityGroupingProjection.Id, "text", request.OpportunityGroupingProjection.Label, request.OpportunityGroupingProjection.Order, request.ConnectionOpportunityIcon );
                request.CampusGrouping = GetGroupingFieldBag( request.CampusGroupingProjection.Id, "text", request.CampusGroupingProjection.Label, request.CampusGroupingProjection.Order );
                request.StatusGrouping = GetGroupingFieldBag( request.StatusGroupingProjection.Id, "text", request.StatusGroupingProjection.Label, request.StatusGroupingProjection.Order );
                request.DueStatusGrouping = GetGroupingFieldBag( ( int ) dueStatus, "text", dueStatus.ToString(), dueStatus.GetOrder(), "ti ti-calendar", null, GetDueStatusTextColorCssClass( dueStatus ) );

                request.StateGrouping = new GroupingFieldBag
                {
                    Key = request.ConnectionState.ToString(),
                    Type = "text",
                    Label = request.ConnectionState.GetDescription() ?? request.ConnectionState.ToString(),
                    IconCssClass = GetStateIconCssClass( request.ConnectionState ),
                    Order = ( int ) request.ConnectionState
                };
            }

            // Load attribute values for the grid-selected attributes.
            GridAttributeLoader.LoadFor( connectionRequests, a => a.ConnectionRequest, GetGridAttributes(), RockContext );

            var gridDataBag = GetGridBuilder().Build( connectionRequests );
            return ActionOk( gridDataBag );
        }

        [BlockAction]
        public BlockActionResult FetchConnectionOpportunityDetails( string connectionOpportunityGuid )
        {
            // TODO - check if null exception is possible
            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( connectionOpportunityGuid.AsGuid() );

            if ( connectionOpportunity == null )
            {
                return ActionNotFound();
            }

            var bag = GetConnectionOpportunityDetailBag( connectionOpportunity );

            if ( bag == null )
            {
                return ActionNotFound();
            }

            return ActionOk( bag );
        }

        [BlockAction]
        public BlockActionResult FetchPlacementGroupDetails( string connectionOpportunityGuid, string placementGroupGuid )
        {
            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( connectionOpportunityGuid.AsGuid() );
            var placementGroup = new GroupService( RockContext ).Get( placementGroupGuid.AsGuid() );

            if ( connectionOpportunity == null || placementGroup == null )
            {
                return ActionNotFound();
            }

            var configs = new ConnectionOpportunityGroupConfigService( RockContext ).Queryable()
                .AsNoTracking()
                .Where( c =>
                    c.ConnectionOpportunityId == connectionOpportunity.Id &&
                    c.GroupTypeId == placementGroup.GroupTypeId )
                .Select( c => new
                {
                    Role = c.GroupMemberRole,
                    Status = c.GroupMemberStatus
                } )
                .ToList();

            var tempGroupMember = new Rock.Model.GroupMember
            {
                GroupId = placementGroup.Id
            };

            tempGroupMember.LoadAttributes();

            var bag = new PlacementGroupDetailsBag
            {
                GroupMemberRoles = configs
                    .DistinctBy( c => c.Role.Guid )
                    .Select( c => c.Role.ToListItemBag() )
                    .ToList(),

                GroupMemberStatuses = configs.GroupBy( c => c.Role.Guid.ToString() )
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy( c => c.Status )
                            .DistinctBy( c => c.Status )
                            .Select( c => new ListItemBag
                            {
                                Text = c.Status.ToString(),
                                Value = ( ( int ) c.Status ).ToString()
                            } )
                            .ToList()
                    ),


                Attributes = tempGroupMember.GetPublicAttributesForEdit( RequestContext.CurrentPerson )
            };

            return ActionOk( bag );
        }

        [BlockAction]
        public BlockActionResult SaveConnectionRequest( ValidPropertiesBox<ConnectionRequestBag> box )
        {
            var connectionType = GetConnectionTypeCacheFromPageParameters();

            // No edit mode at the moment.
            if ( !TryGetEntityForEditAction( null, out var entity, out var actionError ) )
            {
                return actionError;
            }

            // Update the entity instance from the information in the bag.
            if ( !UpdateEntityFromBox( entity, box ) )
            {
                return ActionBadRequest( "Invalid data." );
            }

            // If there is a campus filter then set the new Connection Request to that campus.
            var campusContext = RequestContext.GetContextEntity<Campus>();
            if ( campusContext != null )
            {
                entity.CampusId = campusContext.Id;
            }

            RockContext.WrapTransaction( () =>
            {
                RockContext.SaveChanges();
                entity.SaveAttributeValues( RockContext );
            } );

            return ActionOk();
        }

        [BlockAction]
        public BlockActionResult ReassignConnector( List<string> connectionRequestIdKeys, string connectorPersonAliasGuid )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();

            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequests = CanEditConnectionRequests( connectionType, connectionRequestIdKeys, out var connectionRequests, out var actionError );

            if ( !canEditRequests )
            {
                return actionError;
            }

            var newConnectorPersonAlias = new PersonAliasService( RockContext ).GetInclude( connectorPersonAliasGuid.AsGuid(), c => c.Person );

            List<ConnectionListGridUpdateBag> gridUpdateBags = new List<ConnectionListGridUpdateBag>();
            PersonFieldBag connectorPerson = null;

            if ( newConnectorPersonAlias != null )
            {
                connectorPerson = new PersonFieldBag
                {
                    IdKey = newConnectorPersonAlias.Person.IdKey,
                    NickName = newConnectorPersonAlias.Person.NickName,
                    LastName = newConnectorPersonAlias.Person.LastName,
                    PhotoUrl = newConnectorPersonAlias.Person.PhotoUrl
                };

                var connectionStatusValue = DefinedValueCache.Get( newConnectorPersonAlias.Person.ConnectionStatusValueId.Value );
                if ( connectionStatusValue != null )
                {
                    connectorPerson.ConnectionStatus = connectionStatusValue.Value;
                }
            }

            // Can't use Bulk Update becase we need the Save Hook logic to run.
            foreach ( var connectionRequest in connectionRequests )
            {
                connectionRequest.ConnectorPersonAliasId = newConnectorPersonAlias?.Id;

                gridUpdateBags.Add( new ConnectionListGridUpdateBag
                {
                    IdKey = connectionRequest.IdKey,
                    ConnectorGrouping = GetGroupingFieldBag( newConnectorPersonAlias?.Id, "person", newConnectorPersonAlias?.Person?.FullName, null, null, connectorPerson ),
                    PersonFieldBag = connectorPerson
                } );
            }

            RockContext.SaveChanges();

            return ActionOk( gridUpdateBags );
        }

        [BlockAction]
        public BlockActionResult UpdateRequestStatuses( List<ConnectionRequestUpdateBag> statusUpdateBags, List<string> completedRequestIdKeys )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var allRequestIdKeys = statusUpdateBags.Select( r => r.ConnectionRequestIdKey )
                .Concat( completedRequestIdKeys )
                .Distinct()
                .ToList();


            var canEditRequests = CanEditConnectionRequests( connectionType, allRequestIdKeys, out var connectionRequests, out var actionError );

            if ( !canEditRequests )
            {
                return actionError;
            }

            var connectionStatuses = new ConnectionStatusService( RockContext ).Queryable()
                .AsNoTracking()
                .Where( s => s.ConnectionTypeId == connectionType.Id )
                .ToList();
            var requestsByIdKey = connectionRequests.ToDictionary( r => r.IdKey );
            var statusBagByIdKey = statusUpdateBags.ToDictionary( b => b.ConnectionRequestIdKey );
            List<ConnectionListGridUpdateBag> gridUpdateBags = new List<ConnectionListGridUpdateBag>();

            foreach ( var request in connectionRequests )
            {
                var currentStatus = connectionStatuses.Where( s => s.Id == request.ConnectionStatusId ).FirstOrDefault();
                var dueStatus = GetDueStatus( request.DueDate, request.DueSoonDate );

                // If Completed request, then apply state update.
                if ( completedRequestIdKeys.Contains( request.IdKey ) )
                {
                    request.ConnectionState = ConnectionState.Connected;

                    gridUpdateBags.Add( new ConnectionListGridUpdateBag
                    {
                        IdKey = request.IdKey,
                        StateGrouping = new GroupingFieldBag
                        {
                            Key = request.ConnectionState.ToString(),
                            Type = "text",
                            Label = request.ConnectionState.GetDescription(),
                            IconCssClass = GetStateIconCssClass( request.ConnectionState ),
                            Order = ( int ) request.ConnectionState
                        },
                        StatusGrouping = GetGroupingFieldBag( currentStatus.Id, "text", currentStatus.Name, currentStatus.Order ),
                        ConnectionState = ( Rock.Enums.Connection.ConnectionState ) request.ConnectionState,
                        ConnectionStatusBag = new ConnectionStatusBag
                        {
                            Guid = currentStatus.Guid,
                            Order = currentStatus.Order,
                            Name = currentStatus.Name,
                            HighlightColor = currentStatus.HighlightColor,
                            IsNoteRequiredOnCompletion = currentStatus.IsNoteRequiredOnCompletion
                        },
                        DueStatusGrouping = GetGroupingFieldBag( ( int ) dueStatus, "text", dueStatus.ToString(), dueStatus.GetOrder(), "ti ti-calendar", null, GetDueStatusTextColorCssClass( dueStatus ) ),
                        DueStatus = dueStatus,
                        DueDate = request.DueDate,
                        DueSoonDate = request.DueSoonDate
                    } );
                    continue;
                }
                if ( !statusBagByIdKey.TryGetValue( request.IdKey, out var updateBag ) )
                {
                    continue;
                }
                var newStatus = connectionStatuses.Where( s => s.Guid == updateBag.ConnectionStatusGuid.AsGuid() ).FirstOrDefault();
                if ( newStatus == null )
                {
                    return ActionBadRequest( $"{ConnectionStatus.FriendlyTypeName} not found." );
                }
                if ( currentStatus.IsNoteRequiredOnCompletion &&
                        updateBag.Note.IsNullOrWhiteSpace() )
                {
                    return ActionBadRequest( "A note is required." );
                }

                request.ConnectionStatusId = newStatus.Id;
                // TODO: attach note

                gridUpdateBags.Add( new ConnectionListGridUpdateBag
                {
                    IdKey = request.IdKey,
                    StateGrouping = new GroupingFieldBag
                    {
                        Key = request.ConnectionState.ToString(),
                        Type = "text",
                        Label = request.ConnectionState.GetDescription(),
                        IconCssClass = GetStateIconCssClass( request.ConnectionState ),
                        Order = ( int ) request.ConnectionState
                    },
                    StatusGrouping = GetGroupingFieldBag( newStatus.Id, "text", newStatus.Name, newStatus.Order ),
                    ConnectionState = ( Rock.Enums.Connection.ConnectionState ) request.ConnectionState,
                    ConnectionStatusBag = new ConnectionStatusBag
                    {
                        Guid = newStatus.Guid,
                        Order = newStatus.Order,
                        Name = newStatus.Name,
                        HighlightColor = newStatus.HighlightColor,
                        IsNoteRequiredOnCompletion = newStatus.IsNoteRequiredOnCompletion
                    },
                    DueStatusGrouping = GetGroupingFieldBag( ( int ) dueStatus, "text", dueStatus.ToString(), dueStatus.GetOrder(), "ti ti-calendar", null, GetDueStatusTextColorCssClass( dueStatus ) ),
                    DueStatus = dueStatus,
                    DueDate = request.DueDate,
                    DueSoonDate = request.DueSoonDate
                } );
            }

            RockContext.SaveChanges();

            return ActionOk( gridUpdateBags );
        }

        [BlockAction]
        public BlockActionResult ChangeRequestStatus( ConnectionRequestUpdateBag bag )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequest = CanEditConnectionRequest( connectionType, bag.ConnectionRequestIdKey, out var connectionRequest, out var actionError );

            if ( !canEditRequest )
            {
                return actionError;
            }

            var connectionRequestStatus = new ConnectionStatusService( RockContext ).Get( bag.ConnectionStatusGuid );
            if ( connectionRequestStatus == null || connectionRequestStatus.ConnectionTypeId != connectionRequest.ConnectionTypeId )
            {
                return ActionBadRequest( "Invalid Connection Status" );
            }

            if ( connectionRequest.ConnectionStatus.IsNoteRequiredOnCompletion && bag.Note.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A note is required." );
            }

            // Save status history for previous status

            var connectionRequestStatusHistoryService = new ConnectionRequestStatusHistoryService( RockContext );

            // Update to new status
            connectionRequest.ConnectionStatusId = connectionRequestStatus.Id;

            RockContext.SaveChanges();

            var dueStatus = GetDueStatus( connectionRequest.DueDate, connectionRequest.DueSoonDate );

            var gridUpdateBag = new ConnectionListGridUpdateBag
            {
                IdKey = connectionRequest.IdKey,
                StatusGrouping = GetGroupingFieldBag( connectionRequestStatus.Id, "text", connectionRequestStatus.Name, connectionRequestStatus.Order ),
                ConnectionStatusBag = new ConnectionStatusBag
                {
                    Guid = connectionRequestStatus.Guid,
                    Order = connectionRequestStatus.Order,
                    Name = connectionRequestStatus.Name,
                    HighlightColor = connectionRequestStatus.HighlightColor,
                    IsNoteRequiredOnCompletion = connectionRequestStatus.IsNoteRequiredOnCompletion
                },
                DueStatusGrouping = GetGroupingFieldBag( ( int ) dueStatus, "text", dueStatus.ToString(), dueStatus.GetOrder(), "ti ti-calendar", null, GetDueStatusTextColorCssClass( dueStatus ) ),
                DueStatus = dueStatus,
                DueDate = connectionRequest.DueDate,
                DueSoonDate = connectionRequest.DueSoonDate
            };

            return ActionOk( gridUpdateBag );
        }

        [BlockAction]
        public BlockActionResult UpdateRequestStates( UpdateConnectionRequestStatesBag bag )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequest = CanEditConnectionRequests( connectionType, bag.ConnectionRequestIdKeys, out var connectionRequests, out var actionError );

            if ( !canEditRequest )
            {
                return actionError;
            }

            if ( bag.ConnectionState == Enums.Connection.ConnectionState.FutureFollowUp && !bag.FollowUpDate.HasValue )
            {
                return ActionBadRequest( "A Follow-Up Date is required." );
            }

            List<ConnectionListGridUpdateBag> gridUpdateBags = new List<ConnectionListGridUpdateBag>();

            foreach ( var request in connectionRequests )
            {
                if ( bag.ConnectionState == Enums.Connection.ConnectionState.FutureFollowUp )
                {
                    request.FollowupDate = bag.FollowUpDate?.DateTime;
                }

                request.ConnectionState = ( ConnectionState ) ( bag.ConnectionState );

                gridUpdateBags.Add( new ConnectionListGridUpdateBag
                {
                    IdKey = request.IdKey,
                    StateGrouping = new GroupingFieldBag
                    {
                        Key = request.ConnectionState.ToString(),
                        Type = "text",
                        Label = request.ConnectionState.GetDescription(),
                        IconCssClass = GetStateIconCssClass( request.ConnectionState ),
                        Order = ( int ) request.ConnectionState
                    },
                    ConnectionState = ( Rock.Enums.Connection.ConnectionState ) request.ConnectionState,
                    FollowUpDate = request.FollowupDate
                } );
            }

            RockContext.SaveChanges();
            return ActionOk( gridUpdateBags );
        }

        [BlockAction]
        public BlockActionResult UpsertCelebrationText( UpsertCelebrationBag bag )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequest = CanEditConnectionRequest( connectionType, bag.ConnectionRequestIdKey, out var connectionRequest, out var actionError );

            if ( !canEditRequest )
            {
                return actionError;
            }

            connectionRequest.CelebrationText = bag.CelebrationText;

            var gridUpdateBag = new ConnectionListGridUpdateBag
            {
                IdKey = connectionRequest.IdKey,
                CelebrationText = connectionRequest.CelebrationText
            };

            RockContext.SaveChanges();
            return ActionOk( gridUpdateBag );
        }

        [BlockAction]
        public BlockActionResult DeleteRequests( List<string> connectionRequestIdKeys )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequest = CanEditConnectionRequests( connectionType, connectionRequestIdKeys, out var connectionRequests, out var actionError );

            if ( !canEditRequest )
            {
                return actionError;
            }

            var connectionRequestService = new ConnectionRequestService( RockContext );

            foreach ( var request in connectionRequests )
            {
                if ( !connectionRequestService.CanDelete( request, out var errorMessage ) )
                {
                    return ActionBadRequest( errorMessage );
                }

                RockContext.WrapTransaction( () =>
                {
                    new ConnectionRequestActivityService( RockContext ).DeleteRange( request.ConnectionRequestActivities );
                    connectionRequestService.Delete( request );
                    RockContext.SaveChanges();
                } );
            }

            return ActionOk();
        }

        [BlockAction]
        public BlockActionResult AddActivityForRequests( AddActivityBag bag )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var canEditRequest = CanEditConnectionRequests( connectionType, bag.ConnectionRequestIdKeys, out var connectionRequests, out var actionError, q => q.Include( r => r.ConnectionRequestActivities ).Include( r => r.PersonAlias ) );

            if ( !canEditRequest )
            {
                return actionError;
            }

            var connectionRequestActivityService = new ConnectionRequestActivityService( RockContext );
            var noteService = new NoteService( RockContext );
            var connectorPersonAlias = new PersonAliasService( RockContext ).GetInclude( bag.ConnectorPersonAliasGuid.AsGuid(), c => c.Person );
            var activityType = new ConnectionActivityTypeService( RockContext ).Get( bag.ActivityTypeGuid.AsGuid() );

            if ( activityType == null )
            {
                return ActionBadRequest( "Invalid Activity Type" );
            }

            var shouldCreatePersonNote = activityType.PersonNoteCreationBehavior == PersonNoteCreationBehavior.AlwaysCreateAPersonNote || ( activityType.PersonNoteCreationBehavior == PersonNoteCreationBehavior.AskAtActivityCreation && bag.AddPersonNote );

            if ( shouldCreatePersonNote && !activityType.PersonNoteTypeId.HasValue )
            {
                return ActionBadRequest( "The selected Activity Type is missing a required Person Note Type." );
            }

            List<ConnectionListGridUpdateBag> gridUpdateBags = new List<ConnectionListGridUpdateBag>();

            foreach ( var request in connectionRequests )
            {
                var activity = new ConnectionRequestActivity();

                activity.ConnectionRequestId = request.Id;
                activity.ConnectorPersonAliasId = connectorPersonAlias?.Id;
                activity.Note = bag.Note;
                activity.ConnectionActivityTypeId = activityType.Id;
                activity.ConnectionOpportunityId = request.ConnectionOpportunityId;

                connectionRequestActivityService.Add( activity );

                if ( shouldCreatePersonNote )
                {
                    var personNote = new Note
                    {
                        NoteTypeId = activityType.PersonNoteTypeId.Value,
                        EntityId = request.PersonAlias.PersonId,
                        Caption = request.ConnectionOpportunity.Name,
                        Text = bag.Note,
                        CreatedByPersonAliasId = connectorPersonAlias?.Id,
                    };
                    noteService.Add( personNote );
                }

                gridUpdateBags.Add( new ConnectionListGridUpdateBag
                {
                    IdKey = request.IdKey,
                    LastActivityDateTime = DateTime.Now,
                    ActivityCount = request.ConnectionRequestActivities?.Count + 1 ?? 1
                } );
            }

            RockContext.SaveChanges();
            return ActionOk( gridUpdateBags );
        }

        [BlockAction]
        public BlockActionResult LaunchWorkflowForRequests( LaunchWorkflowBag bag )
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"{Rock.Model.ConnectionType.FriendlyTypeName} not found." );
            }

            var connectionWorkflow = new ConnectionWorkflowService( RockContext ).GetInclude( bag.ConnectionWorkflowGuid.AsGuid(), w => w.WorkflowType );

            if ( !connectionWorkflow.WorkflowTypeId.HasValue )
            {
                return ActionBadRequest( "Invalid Workflow." );
            }

            var workflowType = WorkflowTypeCache.Get( connectionWorkflow.WorkflowTypeId.Value );

            if ( workflowType == null  )
            {
                return ActionBadRequest( "Invalid Workflow Type." );
            }

            if ( !connectionWorkflow.WorkflowType.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
            {
                return ActionBadRequest( "You are not authorized to launch the selected workflow." );
            }

            if ( connectionWorkflow.TriggerType != ConnectionWorkflowTriggerType.Manual || !( connectionWorkflow.WorkflowType.IsActive ?? true ) ) // Mirroring Webforms by setting Is Active to true by default. TODO - Test Webforms.
            {
                return ActionBadRequest( "The selected Workflow must be active with a Manual Trigger Type." );
            }

            var workflow = Rock.Model.Workflow.Activate( workflowType, connectionWorkflow.WorkflowType.WorkTerm, RockContext );

            if ( workflow == null )
            {
                return ActionBadRequest( "An error occurred while activating the Workflow." );
            }

            var workflowService = new WorkflowService( RockContext );
            List<string> workflowErrors;
            List<int> includedDataViewValues = null;
            List<int> excludedDataViewValues = null;
            var connectionRequestWorkflowService = new ConnectionRequestWorkflowService( RockContext );

            if ( connectionWorkflow.IncludeDataViewId.HasValue )
            {
                includedDataViewValues = GetDataViewValues( connectionWorkflow.IncludeDataViewId.Value );
            }
            if ( connectionWorkflow.ExcludeDataViewId.HasValue )
            {
                excludedDataViewValues = GetDataViewValues( connectionWorkflow.ExcludeDataViewId.Value );
            }

            var decodedIds = bag.ConnectionRequestIdKeys.Select( key => Rock.Utility.IdHasher.Instance.GetId( key ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            var connectionRequests = new ConnectionRequestService( RockContext ).GetByIds( decodedIds ).Include( r => r.PersonAlias.Person ).ToList();
            var isSingleRequest = connectionRequests.Count() == 1;
            string statusMessage = string.Empty;
            var launchWorkflowResultBag = new LaunchWorkflowResultBag();
            var launchedWorkflowCout = 0;

            foreach ( var request in connectionRequests )
            {
                // If the manual workflow is not configured for this request status then skip it.
                if ( connectionWorkflow.ManualTriggerFilterConnectionStatusId.HasValue && connectionWorkflow.ManualTriggerFilterConnectionStatusId != request.ConnectionStatusId )
                {
                    continue;
                }

                var person = request.PersonAlias.Person;

                // If the manual workflow's configuration for age classification does not apply to the person on the connection request then skip it.
                if ( connectionWorkflow.AppliesToAgeClassification != AppliesToAgeClassification.All )
                {
                    if ( connectionWorkflow.AppliesToAgeClassification == AppliesToAgeClassification.Adults && person.AgeClassification != AgeClassification.Adult )
                    {
                        continue;
                    }

                    if ( connectionWorkflow.AppliesToAgeClassification == AppliesToAgeClassification.Children && person.AgeClassification != AgeClassification.Child )
                    {
                        continue;
                    }
                }

                // If the manual workflow's "Include Data View" configuration values do not contain the request person then skip it.
                if ( connectionWorkflow.IncludeDataViewId.HasValue && !includedDataViewValues.Contains( person.Id ) )
                {
                    continue;
                }

                // If the manual workflow's "Include Data View" configuration values contain the request person then skip it.
                if ( connectionWorkflow.ExcludeDataViewId.HasValue && excludedDataViewValues.Contains( person.Id ) )
                {
                    continue;
                }

                // TODO - Test this
                // Process the workflow and exit if any errors occur.
                if ( !workflowService.Process( workflow, request, out workflowErrors ) )
                {
                    return ActionBadRequest( "Workflow Processing Error(s):<ul><li>" + workflowErrors.AsDelimited( "</li><li>" ) + "</li></ul>" );
                }

                // If the workflow is persisted, create a link between the workflow and this connection request.
                if ( workflow.Id != 0 )
                {
                    connectionRequestWorkflowService.Add( new ConnectionRequestWorkflow
                    {
                        ConnectionRequestId = request.Id,
                        WorkflowId = workflow.Id,
                        ConnectionWorkflowId = connectionWorkflow.Id,
                        TriggerType = connectionWorkflow.TriggerType,
                        TriggerQualifier = connectionWorkflow.QualifierValue
                    } );
                }

                if ( isSingleRequest && workflow.HasActiveEntryForm( RequestContext.CurrentPerson ) )
                {
                    var qryParam = new Dictionary<string, string>
                    {
                        { "WorkflowType", workflowType.IdKey },
                        { "WorkflowGuid", workflow.Guid.ToString() }
                    };

                    launchWorkflowResultBag.WorkflowEntryPageUrl = this.GetLinkedPageUrl( AttributeKey.WorkflowEntryPage, qryParam );
                }

                launchedWorkflowCout++;
            }

            if ( launchedWorkflowCout == 0 )
            {
                statusMessage = $"The '{workflowType.Name}' workflow was not started for any of the selected connection requests due to its configuration.";
            }
            else if ( isSingleRequest )
            {
                statusMessage = $"A '{workflowType.Name}' workflow has been started. The new workflow has an active form that is ready for input.";
            }
            else if ( launchedWorkflowCout == connectionRequests.Count() )
            {
                statusMessage = $"The '{workflowType.Name}' workflow was successfully started for all selected connection requests.";
            }
            else
            {
                statusMessage = $"The '{workflowType.Name}' workflow was started for {launchedWorkflowCout} of the {connectionRequests.Count()} selected connection requests due to its configuration.";
            }

            launchWorkflowResultBag.StatusMessage = statusMessage;

            RockContext.SaveChanges();
            return ActionOk( launchWorkflowResultBag );
        }

        [BlockAction]
        public BlockActionResult FetchConnectionCampaigns()
        {
            ConnectionTypeCache connectionType = GetConnectionTypeCacheFromPageParameters();
            if ( connectionType == null )
            {
                // TODO - determine if we throw an exception
                return ActionOk();
            }

            var campaignConnectionItems = SystemSettings.GetValue( CampaignConnectionKey.CAMPAIGN_CONNECTION_CONFIGURATION ).FromJsonOrNull<List<CampaignItem>>() ?? new List<CampaignItem>();

            // Gets a filtered list of opportunity guids for the current Connection Type where the Current Person is a Connector on the opportunity.
            var opportunityGuids = new ConnectionOpportunityService( RockContext ).Queryable()
                .Where( o =>
                    o.ConnectionTypeId == connectionType.Id &&
                    o.ConnectionOpportunityConnectorGroups.Any( cg =>
                        cg.ConnectorGroup.Members.Any( gm => gm.PersonId == RequestContext.CurrentPerson.Id )
                    )
                )
                .Select( o => o.Guid )
                .ToList();


            campaignConnectionItems = campaignConnectionItems
                .Where( ci => opportunityGuids.Contains( ci.OpportunityGuid ) && ci.IsActive )
                .ToList();

            var opportunityPartitionedCampaigns = campaignConnectionItems
                .GroupBy( ci => ci.OpportunityGuid )
                .ToDictionary(
                    g => g.Key,
                    g => g.Select( ci => new ConnectionCampaignBag
                    {
                        Guid = ci.Guid,
                        Name = ci.Name,
                        PendingCount = CampaignConnectionHelper
                            .GetPendingConnectionCount( ci, RequestContext.CurrentPerson ),
                        DefaultNumberOfRequests = ci.DailyLimitAssigned
                    } ).ToList()
                );

            return ActionOk( opportunityPartitionedCampaigns );
        }

        [BlockAction]
        public BlockActionResult AssignConnectionRequestsFromCampaign( AssignFromCampaignBag bag )
        {
            var campaignConnectionItems = SystemSettings.GetValue( CampaignConnectionKey.CAMPAIGN_CONNECTION_CONFIGURATION ).FromJsonOrNull<List<CampaignItem>>() ?? new List<CampaignItem>();
            var selectedCampaignConnectionItem = campaignConnectionItems.Where( a => a.Guid == bag.ConnectionCampaignGuid.AsGuid() ).FirstOrDefault();

            if ( selectedCampaignConnectionItem == null )
            {
                // shouldn't happen
                return null;
            }

            // TODO - Do we need to check if the user is authorized to make these requests?

            // Serves no purpose.
            int numberOfRequestsRemaining;

            // Updates any existing requests that do not have a connector and creates new requests via the campaign.
            CampaignConnectionHelper.AddConnectionRequestsForPerson( selectedCampaignConnectionItem, RequestContext.CurrentPerson, bag.NumberOfRequests, out numberOfRequestsRemaining );

            // TODO - may need to add logic to recalculate pending people.
            return ActionOk( );
        }

        [BlockAction]
        public BlockActionResult GetSmsConfiguration( GetSmsConfigurationRequestBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest( "Request is required" );
            }

            if ( bag.ConnectionTypeIdKey.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "Connection Type is required" );
            }

            if ( bag.ConnectionRequestIdKeys == null )
            {
                return ActionBadRequest( "Connection Requests are required" );
            }

            var connectionType = new ConnectionTypeService( RockContext )
                .GetQueryableByKey( bag.ConnectionTypeIdKey )
                .FirstOrDefault();

            if ( connectionType == null )
            {
                return ActionBadRequest( "Invalid Connection Type." );
            }

            // Convert the connection request id keys to integer ids so we can query them.
            var connectionRequestIds = bag.ConnectionRequestIdKeys
                .Select( idKey => IdHasher.Instance.GetId( idKey ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            var connectionRequestService = new ConnectionRequestService( RockContext );
            var mobilePhoneDefinedValueId = DefinedValueCache.GetId( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            var personalDeviceQuery = new PersonalDeviceService( RockContext ).Queryable().AsNoTracking();
            
            var communicationRecipients = connectionRequestService
                .GetByIds( connectionRequestIds )
                .Where( cr => cr.ConnectionTypeId == connectionType.Id ) // Ensure these Connection Requests match the Connection Type.
                .AsNoTracking()
                .Select( cr => new
                {
                    PersonAliasGuid = cr.PersonAlias.Guid,
                    cr.PersonAlias.Person,
                    MobilePhone = cr
                        .PersonAlias
                        .Person
                        .PhoneNumbers
                        .FirstOrDefault( pn => pn.NumberTypeValueId == mobilePhoneDefinedValueId ),
                } )
                .ToList() // materialize query before projecting because some properties require the full Person entity.
                .Select( cr => new CommunicationRecipientBag
                {
                    IsDeceased = cr.Person.IsDeceased,
                    IsSmsAllowed = cr.MobilePhone != null
                        && cr.MobilePhone.Number.IsNotNullOrWhiteSpace() == true
                        && cr.MobilePhone.IsMessagingEnabled
                        && !cr.MobilePhone.IsMessagingOptedOut,
                    Name = cr.Person.FullName,
                    PersonAliasGuid = cr.PersonAliasGuid,
                    PhotoUrl = cr.Person.PhotoUrl,
                    SmsNumber = cr.MobilePhone?.NumberFormatted
                } )
                .ToList();

            var currentPerson = GetCurrentPerson();
            var currentPersonAliasIds = currentPerson?.Aliases?.Select( a => a.Id ).ToList() ?? new List<int>();
            var smsFromSystemPhoneNumbers = SystemPhoneNumberCache
                    .All( includeInactive: false )
                    .Where( spn => spn.IsAuthorized( Authorization.VIEW, currentPerson ) )
                    .OrderByDescending( spn => spn.AssignedToPersonAliasId.HasValue && currentPersonAliasIds.Contains( spn.AssignedToPersonAliasId.Value ) )
                    .ThenBy( spn => spn.Order )
                    .ThenBy( spn => spn.Name )
                    .ThenBy( spn => spn.Id )
                    .ToListItemBagList();

            var snippetTypeGuid = SystemGuid.SnippetType.SMS.AsGuid();
            var smsSnippetTypeId = new SnippetTypeService( RockContext )
                .Queryable()
                .Where( st => st.Guid == snippetTypeGuid )
                .Select( st => ( int? ) st.Id )
                .FirstOrDefault();

            var smsSnippetCategoryGuid = connectionType.GetCommunicationSettings()?.SmsSnippetCategoryGuid;
            var snippetCategoryId = smsSnippetCategoryGuid.HasValue
                    ? CategoryCache.GetId( smsSnippetCategoryGuid.Value )
                    : ( int? ) null;

            var smsSnippets = new SnippetService( RockContext )
                .GetAuthorizedSnippets(
                    currentPerson,
                    s => s.SnippetTypeId == smsSnippetTypeId // This will return an empty list if smsSnippetTypeId is null.
                        && ( !snippetCategoryId.HasValue || s.CategoryId == snippetCategoryId.Value )
                )
                .OrderBy( s => s.Order )
                .ThenBy( s => s.Name )
                .ThenBy( s => s.Id )
                .Select( s => new ListItemBag
                {
                    Text = s.Name,
                    Value = s.Content
                } )
                .ToList();

            return ActionOk( new GetSmsConfigurationResponseBag
            {
                CommunicationRecipients = communicationRecipients,
                SmsFromSystemPhoneNumbers = smsFromSystemPhoneNumbers,
                SmsSnippets = smsSnippets
            } );
        }

        [BlockAction]
        public BlockActionResult GetEmailConfiguration( GetEmailConfigurationRequestBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest( "Request is required" );
            }

            if ( bag.ConnectionTypeIdKey.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "Connection Type is required" );
            }

            if ( bag.ConnectionRequestIdKeys == null )
            {
                return ActionBadRequest( "Connection Requests are required" );
            }

            var connectionType = new ConnectionTypeService( RockContext )
                .GetQueryableByKey( bag.ConnectionTypeIdKey )
                .FirstOrDefault();

            if ( connectionType == null )
            {
                return ActionBadRequest( "Invalid Connection Type." );
            }

            // Convert the connection request id keys to integer ids so we can query them.
            var connectionRequestIds = bag.ConnectionRequestIdKeys
                .Select( idKey => IdHasher.Instance.GetId( idKey ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            var connectionRequestService = new ConnectionRequestService( RockContext );
            var mobilePhoneDefinedValueId = DefinedValueCache.GetId( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            //var personalDeviceQuery = new PersonalDeviceService( RockContext ).Queryable().AsNoTracking();
            
            var communicationRecipients = connectionRequestService
                .GetByIds( connectionRequestIds )
                .Where( cr => cr.ConnectionTypeId == connectionType.Id ) // Ensure these Connection Requests match the Connection Type.
                .AsNoTracking()
                .Select( cr => new
                {
                    PersonAliasGuid = cr.PersonAlias.Guid,
                    cr.PersonAlias.Person,
                } )
                .ToList() // materialize query before projecting because some properties require the full Person entity.
                .Select( cr => new CommunicationRecipientBag
                {
                    Email = cr.Person.Email,
                    EmailPreference = cr.Person.EmailPreference,
                    IsEmailAllowed = cr.Person.CanReceiveEmail( isBulk: false ),
                    IsEmailActive = cr.Person.IsEmailActive, // Used for customized error messages.
                    IsDeceased = cr.Person.IsDeceased,
                    Name = cr.Person.FullName,
                    PersonAliasGuid = cr.PersonAliasGuid,
                    PhotoUrl = cr.Person.PhotoUrl,
                } )
                .ToList();

            var currentPerson = GetCurrentPerson();
            var mergeFields = this.RequestContext.GetCommonMergeFields();
            var communicationTemplateGuid = connectionType.GetCommunicationSettings()?.CommunicationTemplateCategoryGuid;
            var communicationTemplates = new CommunicationTemplateService( RockContext )
                .Queryable()
                .AsNoTracking()
                .Include( ct => ct.Attachments.Select( b => b.BinaryFile ) )
                .Where( ct =>
                    ct.IsActive
                    && !ct.UsageType.HasValue // Exclude templates that have an explicit usage type (e.g., Communication Flow templates)
                    && ( !communicationTemplateGuid.HasValue || ct.Category.Guid == communicationTemplateGuid.Value )
                )
                .ToList()
                .Where( ct =>
                    ct.IsAuthorized( Authorization.VIEW, currentPerson )
                    && ct.HasEmailTemplate()
                    && !ct.SupportsEmailWizard() // Exclude wizard templates because we are showing a simple HTML editor that can't handle complex wizard templates.
                )
                .OrderBy( t => t.Name )
                .ThenBy( t => t.Id )
                .Select( t => new CommunicationTemplateBag
                {
                    IdKey = t.IdKey,
                    Name = t.Name,
                    FromEmail = t.FromEmail?.ResolveMergeFields( mergeFields ),
                    FromName = t.FromName?.ResolveMergeFields( mergeFields ),
                    Subject = t.Subject,
                    Message = t.Message?.ResolveMergeFields( mergeFields ),
                    EmailAttachmentBinaryFiles = t.GetAttachments( CommunicationType.Email )?.Select( cta => cta.BinaryFile )?.ToListItemBagList(),
                } )
                .ToList();

            return ActionOk( new GetEmailConfigurationResponseBag
            {
                CommunicationRecipients = communicationRecipients,
                CommunicationTemplates = communicationTemplates
            } );
        }

        [BlockAction]
        public BlockActionResult SendCommunication( CommunicationBag bag )
        {
            // TODO JMH Add validation.

            var communication = CreateCommunication( bag );

            var msg = new ProcessSendCommunication.Message
            {
                CommunicationId = communication.Id
            };
            msg.Send();

            return ActionOk();
        }

        #endregion Block Actions

        private Model.Communication CreateCommunication( CommunicationBag bag )
        {
            var senderPersonAliasId = GetCurrentPerson().PrimaryAliasId;
            var personAliasIds = bag.CommunicationRecipients
                .Select( r => r.PersonAliasGuid )
                .ToList();
            IQueryable<int> activeRecipientPersonAliasIdQuery = new PersonAliasService( RockContext )
                .Queryable()
                .Where( pa => personAliasIds.Contains( pa.Guid ) )
                .Select( pa => pa.Id );

            var communicationService = new CommunicationService( RockContext );
            var communicationTemplateService = new CommunicationTemplateService( RockContext );
            var binaryFileService = new BinaryFileService( RockContext );
            Model.Communication communication = null;

            if ( bag.CommunicationType == Enums.Communication.CommunicationType.Email )
            {
                var communicationTemplateId = new CommunicationTemplateService( RockContext )
                    .GetQueryableByKey( bag.CommunicationTemplateIdKey, !PageCache.Layout.Site.DisablePredictableIds )
                    .Select( ct => new
                    {
                        ct.Id
                    } )
                    .FirstOrDefault()?.Id;

                communication = communicationService.CreateEmailCommunication( new CommunicationService.CreateEmailCommunicationArgs
                {
                    BulkCommunication = true,
                    CommunicationTemplateId = communicationTemplateId,
                    FromAddress = bag.FromEmail,
                    FromName = bag.FromName,
                    FutureSendDateTime = null,
                    Message = bag.Message,
                    Name = bag.Subject,
                    RecipientPrimaryPersonAliasIds = activeRecipientPersonAliasIdQuery.ToList(),
                    RecipientStatus = CommunicationRecipientStatus.Pending,
                    ReplyTo = null,
                    SendDateTime = null, // This is actually the "sent" value and must be null here.
                    SenderPersonAliasId = senderPersonAliasId,
                    Subject = bag.Subject,
                    SystemCommunicationId = null
                } );

                var emailAttachmentGuids = bag.EmailAttachments
                    .Select( a => a.Value.AsGuidOrNull() )
                    .Where( g => g.HasValue )
                    .Select( g => g.Value )
                    .ToList();
                var attachmentIdMap = binaryFileService
                    .Queryable()
                    .Where( bf => emailAttachmentGuids.Contains( bf.Guid ) )
                    .Select( bf => new
                    {
                        bf.Id,
                        bf.Guid
                    } )
                    .ToList()
                    .ToDictionary( bf => bf.Guid, bf => bf.Id );

                foreach ( var attachment in bag.EmailAttachments )
                {
                    if ( attachmentIdMap.TryGetValue( attachment.Value.AsGuid(), out var id ) )
                    {
                        var newAttachment = new CommunicationAttachment
                        {
                            BinaryFileId = id,
                            CommunicationType = Model.CommunicationType.Email
                        };
                        communication.Attachments.Add( newAttachment );
                    }
                }
            }
            else if ( bag.CommunicationType == Enums.Communication.CommunicationType.SMS )
            {
                communication = communicationService.CreateSMSCommunication( new CommunicationService.CreateSMSCommunicationArgs
                {
                    CommunicationName = $"{bag.Subject}",
                    CommunicationTemplateId = null,
                    FromPrimaryPersonAliasId = senderPersonAliasId,
                    FromSystemPhoneNumber = SystemPhoneNumberCache.Get( bag.SmsFromSystemPhoneNumberGuid.Value ),
                    FutureSendDateTime = null,
                    Message = bag.SmsMessage,
                    ResponseCode = null,
                    SystemCommunicationId = null,
                    ToPrimaryPersonAliasIds = activeRecipientPersonAliasIdQuery.ToList()
                } );

                var smsAttachmentGuids = bag.SmsAttachments
                    .Select( a => a.Value.AsGuidOrNull() )
                    .Where( g => g.HasValue )
                    .Select( g => g.Value )
                    .ToList();
                var attachmentIdMap = binaryFileService
                    .Queryable()
                    .Where( bf => smsAttachmentGuids.Contains( bf.Guid ) )
                    .Select( bf => new
                    {
                        bf.Id,
                        bf.Guid
                    } )
                    .ToList()
                    .ToDictionary( bf => bf.Guid, bf => bf.Id );

                foreach ( var attachment in bag.SmsAttachments )
                {
                    if ( attachmentIdMap.TryGetValue( attachment.Value.AsGuid(), out var id ) )
                    {
                        var newAttachment = new CommunicationAttachment
                        {
                            BinaryFileId = id,
                            CommunicationType = Model.CommunicationType.SMS
                        };
                        communication.Attachments.Add( newAttachment );
                    }
                }
            }

            if ( communication != null )
            {
                // Always set Connection Request communications as NOT bulk
                // since they are being sent directly to each person to help
                // move their connection request along in its process,
                // not a bulk communication.
                communication.IsBulkCommunication = false;

                // Separate the CommunicationRecipients from the Communication
                // so the Communication can be saved without recipients.
                // Then we'll save the recipients using a bulk insert.
                var communicationRecipients = communication.Recipients;
                communication.Recipients = new List<CommunicationRecipient>();
                foreach ( var communicationRecipient in communicationRecipients )
                {
                    communicationRecipient.Communication = null;
                    communicationService.Context.Entry( communicationRecipient ).State = EntityState.Detached;
                }

                // Save the communication.
                communicationService.Add( communication );
                RockContext.SaveChanges();

                // Bulk insert the recipients for better performance.
                foreach ( var communicationRecipient in communicationRecipients )
                {
                    communicationRecipient.CommunicationId = communication.Id;
                }

                RockContext.BulkInsert( communicationRecipients );
            }

            return communication;
        }

        /// <summary>
        /// Gets the grid builder for the communication list grid.
        /// </summary>
        /// <returns>The grid builder for the communication list grid.</returns>
        private GridBuilder<ConnectionRow> GetGridBuilder()
        {
            return new GridBuilder<ConnectionRow>()
                .WithBlock( this )
                .AddField( "idKey", a => a.ConnectionRequestId.AsIdKey() )
                .AddField( "connectorGrouping", a => a.ConnectorGrouping )
                .AddField( "campusGrouping", a => a.CampusGrouping )
                .AddField( "opportunityGrouping", a => a.OpportunityGrouping )
                .AddField( "statusGrouping", a => a.StatusGrouping )
                .AddField( "stateGrouping", a => a.StateGrouping )
                .AddField( "dueStatusGrouping", a => a.DueStatusGrouping )
                .AddField( "connectorDetails", a => a.ConnectorPerson )
                .AddField( "requestDetails", a => a.Person )
                .AddField( "requesterPersonAliasGuid", a => a.RequesterPersonAliasGuid )
                .AddTextField( "connectionOpportunity", a => a.ConnectionOpportunity )
                .AddTextField( "connectionTypeSource", a => a.ConnectionTypeSource )
                .AddTextField( "campus", a => a.Campus )
                .AddTextField( "group", a => a.Group )
                .AddField( "connectionStatus", a => a.ConnectionStatus )
                .AddDateTimeField( "lastActivityDateTime", a => a.LastActivityDateTime )
                .AddField( "activityCount", a => a.ActivityCount )
                .AddDateTimeField( "createdDateTime", a => a.CreatedDateTime )
                .AddDateTimeField( "dueDate", a => a.DueDate )
                .AddDateTimeField( "dueSoonDate", a => a.DueSoonDate )
                .AddField( "dueStatus", a => a.DueStatus )
                .AddDateTimeField( "followUpDate", a => a.FollowUpDate )
                .AddField( "connectionState", a => a.ConnectionState )
                .AddTextField( "celebrationText", a => a.CelebrationText )
                .AddField( "reminderCount", a => a.ReminderCount )
                .AddAttributeFieldsFrom( a => a.ConnectionRequest, GetGridAttributes() );
        }

        /// <summary>
        /// Builds the list of grid attributes that should be included on the Grid.
        /// </summary>
        /// <remarks>
        /// The default implementation returns only attributes that are not qualified.
        /// </remarks>
        /// <returns>A list of <see cref="AttributeCache"/> objects.</returns>
        private List<AttributeCache> GetGridAttributes()
        {
            if ( _gridAttributes == null )
            {
                var availableAttributes = new List<AttributeCache>();
                var connectionTypeId = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds )?.Id;

                if ( connectionTypeId.HasValue )
                {
                    var entityTypeId = EntityTypeCache.Get<ConnectionRequest>( false )?.Id;
                    availableAttributes.AddRange( AttributeCache.GetOrderedGridAttributes( entityTypeId.Value, "ConnectionTypeId", connectionTypeId.Value.ToString() ) );
                }

                _gridAttributes = availableAttributes;
            }

            return _gridAttributes;
        }

        private List<AttributeCache> _gridAttributes = null;

        #region Supporting Classes

        public class ConnectionRow
        {
            public int ConnectionRequestId { get; set; }

            public ConnectionRequest ConnectionRequest { get; set; }

            public GroupingProjection ConnectorGroupingProjection { get; set; }

            public GroupingProjection OpportunityGroupingProjection { get; set; }

            public GroupingProjection CampusGroupingProjection { get; set; }

            public GroupingProjection StatusGroupingProjection { get; set; }

            public GroupingFieldBag ConnectorGrouping { get; set; }

            public GroupingFieldBag OpportunityGrouping { get; set; }

            public GroupingFieldBag CampusGrouping { get; set; }

            public GroupingFieldBag StateGrouping { get; set; }

            public GroupingFieldBag StatusGrouping { get; set; }

            public GroupingFieldBag DueStatusGrouping { get; set; }

            public PersonProjection ConnectorPersonProjection { get; set; }

            public PersonFieldBag ConnectorPerson { get; set; }

            public PersonProjection PersonProjection { get; set; }

            public PersonFieldBag Person { get; set; }

            public Guid RequesterPersonAliasGuid { get; set; }

            public int ConnectionOpportunityId { get; set; }

            public Guid ConnectionOpportunityGuid { get; set; }

            public string ConnectionOpportunity { get; set; }

            public string ConnectionOpportunityIcon { get; set; }

            public string ConnectionTypeSource { get; set; }

            public int? CampusId { get; set; }

            public string Campus { get; set; }

            public int? GroupId { get; set; }

            public string Group { get; set; }

            public ConnectionStatusProjection ConnectionStatusProjection { get; set; }

            public ConnectionStatusBag ConnectionStatus { get; set; }

            public ConnectionState ConnectionState { get; set; }

            public DateTime? LastActivityDateTime { get; set; }

            public int ActivityCount { get; set; }

            public DateTime? FollowUpDate { get; set; }

            public DateTime? CreatedDateTime { get; set; }

            public DateTime? DueDate { get; set; }

            public DateTime? DueSoonDate { get; set; }

            public DueStatus DueStatus { get; set; }

            public string CelebrationText { get; set; }

            public int ReminderCount { get; set; }
        }

        public class GroupingProjection
        {
            public int? Id { get; set; }

            public string Label { get; set; }

            public int? Order { get; set; }
        }

        public class ConnectionStatusProjection
        {
            public Guid Guid { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public string HighlightColor { get; set; }

            public bool IsNotRequiredOnCompletion { get; set; }
        }

        public class PersonProjection
        {
            public string NickName { get; set; }

            public string LastName { get; set; }

            public int? PhotoId { get; set; }

            public int? Age { get; set; }

            public Gender? Gender { get; set; }

            public int? RecordTypeValueId { get; set; }

            public AgeClassification? AgeClassification { get; set; }

            public int? ConnectionStatusValueId { get; set; }

            public int? Id { get; set; }
        }

        #endregion Supporting Classes
    }
}
