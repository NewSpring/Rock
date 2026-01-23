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

    

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "CEE15B88-3B23-4378-9CB1-E59A97A94D1B" )]
    [Rock.SystemGuid.BlockTypeGuid( "8674FB3A-9E0E-421C-821C-2DA862A20ED2" )]
    public class ConnectionsHub : RockBlockType
    {
        #region Keys

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
            var connectionType = new ConnectionTypeService( RockContext ).GetInclude( PageParameter( PageParameterKey.ConnectionType ), a => a.ConnectionStatuses, !PageCache.Layout.Site.DisablePredictableIds );

            if ( connectionType == null )
            {
                return options;
            }

            options.Title = connectionType.Name + " Requests";
            options.IconCssClass = connectionType.IconCssClass;

            var connectionTypeIdKey = IdHasher.Instance.GetHash( connectionType.Id );
            options.ConnectionTypeIdKey = connectionTypeIdKey;
            options.IsSequentialStatusMode = connectionType.IsSequentialStatusEnforced;

            //options.ConnectionStatusBags = connectionType.ConnectionStatuses
            //    .Select( cs => new ConnectionStatusBag
            //    {
            //        Guid = cs.Guid,
            //        Name = cs.Name,
            //        Order = cs.Order,
            //        HighlightColor = cs.HighlightColor
            //    } )
            //    .OrderBy( cs => cs.Order )
            //    .ToList();

            List<Rock.Enums.Connection.ConnectionState> ignoredConnectionStates = new List<Rock.Enums.Connection.ConnectionState>();

            if ( !connectionType.EnableFutureFollowup )
            {
                ignoredConnectionStates.Add( Rock.Enums.Connection.ConnectionState.FutureFollowUp );
            }

            // When we are in add mode then we ignore "Connected". This will need to be updated if this list is used elsewhere.
            // Moving this to client side...
            //ignoredConnectionStates.Add( ConnectionState.Connected );

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

            var workflowItems = new List<ListItemBag>();

            // Connection Type workflows
            workflowItems.AddRange(
                connectionType.ConnectionWorkflows
                    .Select( w => w.WorkflowType )
                    .ToListItemBagList()
            );

            // Connection Opportunity workflows
            workflowItems.AddRange(
                connectionType.ConnectionOpportunities
                    .SelectMany( o => o.ConnectionWorkflows )
                    .Select( w => w.WorkflowType )
                    .ToListItemBagList()
            );

            // Optional: de-dupe by WorkflowType Id
            options.WorkflowItems = workflowItems
                .GroupBy( w => w.Value )
                .Select( g => g.First() )
                .ToList();

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

            if ( connectionOpportunityFilter.HasValue )
            {
                options.ConnectionOpportunityDetailsFromFilter = GetConnectionOpportunityDetailBag( connectionOpportunityFilter.Value );
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

        private ConnectionOpportunityDetailBag GetConnectionOpportunityDetailBag( Guid connectionOpportunityGuid )
        {
            var connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( connectionOpportunityGuid );
            if ( connectionOpportunity == null )
            {
                return null;
            }

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

        private GroupingFieldBag GetGroupingFieldBag( int? id, string type, string label, int? order = null, string iconCssClass = null, PersonFieldBag person = null )
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
                    Order = order
                };
            }

            return new GroupingFieldBag
            {
                Key = IdHasher.Instance.GetHash( id.Value ),
                Type = type,
                Label = label,
                IconCssClass = iconCssClass,
                Person = person,
                Order = order
            };
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

        private bool CanUpdateConnectionRequest( List<int> connectionRequestIds, ConnectionTypeCache connectionType, out List<ConnectionRequest> connectionRequests )
        {
            var userCanEditConnectionRequest = false;
            var connectionOpportunityFilter = GetConnectionOpportunityFilter( connectionType.IdKey );
            connectionRequests = null;

            List<ConnectionOpportunity> connectionOpportunities = new List<ConnectionOpportunity>();
            if ( connectionOpportunityFilter.HasValue )
            {
                var connectionOpportunity = new ConnectionOpportunityService( RockContext ).Get( connectionOpportunityFilter.Value );
                connectionOpportunities.Add( connectionOpportunity );
            }
            else
            {
                // TODO - Check with Observability, this sql might be ugly. Potentially query for Connection Opportunities from Connection Requests first.
                // Get all the Connection Opportunities that have any of the Connection Requests in them.
                connectionOpportunities = new ConnectionOpportunityService( RockContext )
                    .Queryable()
                    .Where( o =>
                        o.ConnectionTypeId == connectionType.Id &&
                        o.ConnectionRequests.Any( r => connectionRequestIds.Contains( r.Id ) ) )
                    .ToList();
            }

            if ( connectionType != null && connectionType.EnableRequestSecurity )
            {
                connectionRequests = new ConnectionRequestService( RockContext ).GetByIds( connectionRequestIds ).ToList();
                userCanEditConnectionRequest = connectionRequests.All( cr => cr.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) );
            }
            else
            {
                userCanEditConnectionRequest = connectionOpportunities.All( co => co.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) );
            }

            // TODO - Mirroring Webforms logic where we will check by Connector Groups if user can't already edit.
            //if ( !userCanEditConnectionRequest )
            //{
            //    var campuses = CampusCache.All().Where( c => c.IsActive ?? true ).ToList();
            //}
            if ( !userCanEditConnectionRequest )
            {
                var connectionOpportunityConnectorGroups = connectionOpportunities
                    .SelectMany( o => o.ConnectionOpportunityConnectorGroups )
                    .Distinct()
                    .ToList();

                //var qryConnectionOpportunityConnectorGroups = new ConnectionOpportunityConnectorGroupService( new RockContext() )
                //    .Queryable()
                //    .AsNoTracking()
                //    .Where( a => a.ConnectionOpportunityId == connectionOpportunity.Id );
                var campuses = CampusCache.All().Where( c => c.IsActive ?? true ).ToList();

                // If there is only one campus OR the group is not set to a specific campus...

                // Grant edit access to any of those in a non campus-specific connector group
                userCanEditConnectionRequest = connectionOpportunityConnectorGroups
                    .Any( g =>
                        ( campuses.Count == 1 || !g.CampusId.HasValue ) &&
                        g.ConnectorGroup != null &&
                        g.ConnectorGroup.Members.Any( m => m.PersonId == RequestContext.CurrentPerson.Id && m.GroupMemberStatus == GroupMemberStatus.Active ) );

                //TODO - Cannot do this last step because of Bulk Connection Requests.
                //if ( !userCanEditConnectionRequest )
                //{
                //    // Current Person still has to be a Connector.
                //    // If this is a new request, grant edit access to any connector group. Otherwise, match the request's campus to the corresponding campus-specific connector group
                //    var groupCampuses = connectionOpportunityConnectorGroups
                //        .Where( g =>
                //            g.ConnectorGroup != null &&
                //            g.ConnectorGroup.Members.Any( m => m.PersonId == RequestContext.CurrentPerson.Id ) );

                //    if ( connectionRequest != null )
                //    {
                //        groupCampuses = groupCampuses.Where( g => ( connectionRequest.Id == 0 || ( connectionRequest.CampusId.HasValue && g.CampusId == connectionRequest.CampusId.Value ) ) );
                //    }

                //    // If the connetion request is new OR the group campus matches the connection request campus.
                //    foreach ( var groupCampus in groupCampuses )
                //    {
                //        userCanEditConnectionRequest = true;
                //        break;
                //    }
                //}
            }

            return userCanEditConnectionRequest;
        }

        private Rock.Model.ConnectionType GetConnectionType()
        {
            return new ConnectionTypeService( RockContext ).Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds );
        }

        #endregion Methods

        #region Block Actions

        [BlockAction]
        public BlockActionResult GetGridData()
        {
            var connectionType = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds );
            if ( connectionType == null )
            {
                return ActionOk();
            }

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
                    }
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
                request.ConnectorPerson = connectorPerson;

                request.ConnectorGrouping = GetGroupingFieldBag( request.ConnectorGroupingProjection.Id, "person", request.ConnectorGroupingProjection.Label, null, null, connectorPerson );
                request.OpportunityGrouping = GetGroupingFieldBag( request.OpportunityGroupingProjection.Id, "text", request.OpportunityGroupingProjection.Label, request.OpportunityGroupingProjection.Order, request.ConnectionOpportunityIcon );
                request.CampusGrouping = GetGroupingFieldBag( request.CampusGroupingProjection.Id, "text", request.CampusGroupingProjection.Label, request.CampusGroupingProjection.Order );
                request.StatusGrouping = GetGroupingFieldBag( request.StatusGroupingProjection.Id, "text", request.StatusGroupingProjection.Label, request.StatusGroupingProjection.Order );

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
            // TODO - check if this can get a null exception
            var bag = GetConnectionOpportunityDetailBag( connectionOpportunityGuid.AsGuid() );

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
            var connectionType = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds );
            if ( connectionType == null )
            {
                // TODO - determine if we throw an exception
                return ActionOk();
            }

            var connectionRequestIds = connectionRequestIdKeys
                .Select( key => Rock.Utility.IdHasher.Instance.GetId( key ) )
                .Where( id => id.HasValue )
                .Select( id => id.Value )
                .ToList();

            List<ConnectionRequest> connectionRequests = null;

            RockContext.SqlLogging( true );

            var canUpdateRequest = CanUpdateConnectionRequest( connectionRequestIds, connectionType, out connectionRequests );

            if ( !canUpdateRequest )
            {
                return ActionBadRequest( "Not authorized to reassign connector for one or more selected connection requests." );
            }

            var newConnectorPersonAliasId = new PersonAliasService( RockContext ).GetId( connectorPersonAliasGuid.AsGuid() );
            if ( !newConnectorPersonAliasId.HasValue )
            {
                return ActionBadRequest( "Invalid connector." );
            }

            if ( connectionRequests == null )
            {
                connectionRequests = new ConnectionRequestService( RockContext ).GetByIds( connectionRequestIds ).ToList();
            }

            // Can't use Bulk Update becase we need the Save Hook logic to run.
            foreach ( var connectionRequest in connectionRequests )
            {
                connectionRequest.ConnectorPersonAliasId = newConnectorPersonAliasId.Value;
            }

            RockContext.SaveChanges();

            RockContext.SqlLogging( false );

            return ActionOk();
        }

        [BlockAction]
        public BlockActionResult ChangeRequestStatus( ConnectionRequestUpdateBag bag )
        {
            var connectionRequest = new ConnectionRequestService( RockContext ).GetInclude( bag.ConnectionRequestIdKey, c => c.ConnectionStatus, !PageCache.Layout.Site.DisablePredictableIds );
            if ( connectionRequest == null )
            {
                // TODO - determine if we throw an exception
                return ActionOk();
            }

            // TODO - Security
            //var canUpdateRequest = CanUpdateConnectionRequest( connectionRequestIds, connectionType, out connectionRequests );

            //if ( !canUpdateRequest )
            //{
            //    return ActionBadRequest( "Not authorized to reassign connector for one or more selected connection requests." );
            //}

            var connectionRequestStatus = new ConnectionStatusService( RockContext ).Get( bag.ConnectionStatusGuid );
            if ( connectionRequestStatus == null || connectionRequestStatus.ConnectionTypeId != connectionRequest.ConnectionTypeId )
            {
                return ActionBadRequest( "Invalid Connection Status" );
            }

            if ( connectionRequest.ConnectionStatus.IsNoteRequiredOnCompletion && bag.Note.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A note is required." );
            }

            connectionRequest.ConnectionStatusId = connectionRequestStatus.Id;
            // TODO - Link note up.

            RockContext.SaveChanges();

            var gridUpdateBag = new GridUpdateBag
            {
                GroupingFieldBag = GetGroupingFieldBag( connectionRequestStatus.Id, "text", connectionRequestStatus.Name, connectionRequestStatus.Order ),
                ConnectionStatusBag = new ConnectionStatusBag
                {
                    Guid = connectionRequestStatus.Guid,
                    Order = connectionRequestStatus.Order,
                    Name = connectionRequestStatus.Name,
                    HighlightColor = connectionRequestStatus.HighlightColor,
                    IsNoteRequiredOnCompletion = connectionRequestStatus.IsNoteRequiredOnCompletion
                }
            };

            return ActionOk( gridUpdateBag );
        }

        [BlockAction]
        public BlockActionResult ChangeRequestState( ConnectionRequestUpdateBag bag )
        {
            var connectionRequest = new ConnectionRequestService( RockContext ).Get( bag.ConnectionRequestIdKey, !PageCache.Layout.Site.DisablePredictableIds );
            if ( connectionRequest == null )
            {
                // TODO - determine if we throw an exception
                return ActionOk();
            }

            // TODO - Security
            //var canUpdateRequest = CanUpdateConnectionRequest( connectionRequestIds, connectionType, out connectionRequests );

            //if ( !canUpdateRequest )
            //{
            //    return ActionBadRequest( "Not authorized to reassign connector for one or more selected connection requests." );
            //}

            connectionRequest.ConnectionState = ( ConnectionState ) bag.ConnectionState;

            RockContext.SaveChanges();

            var gridUpdateBag = new GridUpdateBag
            {
                GroupingFieldBag = new GroupingFieldBag
                {
                    Key = connectionRequest.ConnectionState.ToString(),
                    Type = "text",
                    Label = connectionRequest.ConnectionState.GetDescription(),
                    IconCssClass = GetStateIconCssClass( connectionRequest.ConnectionState ),
                    Order = ( int ) connectionRequest.ConnectionState
                },
                ConnectionState = ( Rock.Enums.Connection.ConnectionState ) connectionRequest.ConnectionState
            };

            return ActionOk( gridUpdateBag );
        }

        [BlockAction]
        public BlockActionResult FetchConnectionCampaigns()
        {
            var connectionType = ConnectionTypeCache.Get( PageParameter( PageParameterKey.ConnectionType ), !PageCache.Layout.Site.DisablePredictableIds );
            if ( connectionType == null )
            {
                // TODO - determine if we throw an exception
                return ActionOk();
            }

            var campaignConnectionItems = SystemSettings.GetValue( CampaignConnectionKey.CAMPAIGN_CONNECTION_CONFIGURATION ).FromJsonOrNull<List<CampaignItem>>() ?? new List<CampaignItem>();

            campaignConnectionItems = campaignConnectionItems
                .Where( ci => ci.ConnectionTypeGuid == connectionType.Guid )
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
                            .GetPendingConnectionCount( ci, RequestContext.CurrentPerson )
                    } ).ToList()
                );


            return ActionOk( opportunityPartitionedCampaigns );
        }

        #endregion Block Actions

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
                .AddField( "connectorDetails", a => a.ConnectorPerson )
                .AddField( "requestDetails", a => a.Person )
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
                .AddDateTimeField( "followUpDate", a => a.FollowUpDate )
                .AddField( "connectionState", a => a.ConnectionState )
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

            public PersonProjection ConnectorPersonProjection { get; set; }

            public PersonFieldBag ConnectorPerson { get; set; }

            public PersonProjection PersonProjection { get; set; }

            public PersonFieldBag Person { get; set; }

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
