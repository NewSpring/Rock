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
            options.ConnectionStatusBags = connectionType.ConnectionStatuses
                .Select( cs => new ConnectionStatusBag
                {
                    IdKey = IdHasher.Instance.GetHash( cs.Id ),
                    Name = cs.Name,
                    Order = cs.Order,
                    HighlightColor = cs.HighlightColor
                } )
                .OrderBy( cs => cs.Order )
                .ToList();

            return options;
        }

        private GroupingFieldBag GetGroupingFieldBag( int? id, string type, string label, string iconCssClass = null, PersonFieldBag person = null )
        {
            if ( !id.HasValue )
            {
                if ( type == "person" )
                {
                    person = new PersonFieldBag
                    {
                        IdKey = string.Empty,
                        NickName = "Unassigned",
                        PhotoUrl = Rock.Model.Person.GetPersonNoPictureUrl( new Rock.Model.Person() )
                    };
                }

                return new GroupingFieldBag
                {
                    Key = "unassigned",
                    Type = type,
                    Label = "Unassigned",
                    Person = person
                };
            }

            return new GroupingFieldBag
            {
                Key = IdHasher.Instance.GetHash( id.Value ),
                Type = type,
                Label = label,
                IconCssClass = iconCssClass,
                Person = person
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
                .Where( cr => cr.ConnectionOpportunity.ConnectionTypeId == connectionType.Id )
                .Select( a => new ConnectionRow
                {
                    ConnectionRequestId = a.Id,
                    ConnectorGroupingProjection = new GroupingProjection
                    {
                        Id = a.ConnectorPersonAliasId,
                        Label = a.ConnectorPersonAlias != null ? a.ConnectorPersonAlias.Person.NickName + " " + a.ConnectorPersonAlias.Person.LastName : string.Empty
                    },
                    OpportunityGroupingProjection = new GroupingProjection
                    {
                        Id = a.ConnectionOpportunityId,
                        Label = a.ConnectionOpportunity.Name
                    },
                    CampusGroupingProjection = new GroupingProjection
                    {
                        Id = a.CampusId,
                        Label = a.Campus != null ? a.Campus.Name : string.Empty
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
                        Label = a.ConnectionStatus != null ? a.ConnectionStatus.Name : string.Empty
                    },
                    ConnectionOpportunityId = a.ConnectionOpportunityId,
                    ConnectionOpportunity = a.ConnectionOpportunity.Name,
                    ConnectionOpportunityIcon = a.ConnectionOpportunity.IconCssClass,
                    ConnectionTypeSource = a.ConnectionTypeSource != null ? a.ConnectionTypeSource.Name : string.Empty,
                    CampusId = a.CampusId,
                    Campus = a.Campus != null ? a.Campus.Name : string.Empty,
                    GroupId = a.AssignedGroupId,
                    Group = a.AssignedGroup != null ? a.AssignedGroup.Name : string.Empty,
                    ConnectionStatusProjection = new ConnectionStatusProjection
                    {
                        Id = a.ConnectionStatusId,
                        Name = a.ConnectionStatus.Name, // TODO - Test what happens when a Connection Status is deleted.
                        Order = a.ConnectionStatus.Order,
                        HighlightColor = a.ConnectionStatus.HighlightColor
                    },
                    ConnectionState = a.ConnectionState,
                    LastActivityDateTime = a.ConnectionRequestActivities.Select( cra => cra.CreatedDateTime )
                        .OrderByDescending( d => d )
                        .FirstOrDefault(),
                    ActivityCount = a.ConnectionRequestActivities.Count(),
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
                    IdKey = IdHasher.Instance.GetHash( request.ConnectionStatusProjection.Id ),
                    Name = request.ConnectionStatusProjection.Name,
                    Order = request.ConnectionStatusProjection.Order,
                    HighlightColor = request.ConnectionStatusProjection.HighlightColor
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

                request.ConnectorGrouping = GetGroupingFieldBag( request.ConnectorGroupingProjection.Id, "person", request.ConnectorGroupingProjection.Label, null, connectorPerson );
                request.OpportunityGrouping = GetGroupingFieldBag( request.OpportunityGroupingProjection.Id, "text", request.OpportunityGroupingProjection.Label, request.ConnectionOpportunityIcon );
                request.CampusGrouping = GetGroupingFieldBag( request.CampusGroupingProjection.Id, "text", request.CampusGroupingProjection.Label );
                request.StatusGrouping = GetGroupingFieldBag( request.StatusGroupingProjection.Id, "text", request.StatusGroupingProjection.Label );

                request.StateGrouping = new GroupingFieldBag
                {
                    Key = request.ConnectionState.ToString(),
                    Type = "text",
                    Label = request.ConnectionState.ToString(),
                    IconCssClass = GetStateIconCssClass( request.ConnectionState )
                };
            }

            var gridDataBag = GetGridBuilder().Build( connectionRequests );
            return ActionOk( gridDataBag );
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
                .AddDateTimeField( "dueDate", a => a.DueDate )
                .AddDateTimeField( "dueSoonDate", a => a.DueSoonDate )
                .AddField( "connectionState", a => a.ConnectionState );
        }

        #region Supporting Classes

        public class ConnectionRow
        {
            public int ConnectionRequestId { get; set; }

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

            public DateTime? DueDate { get; set; }

            public DateTime? DueSoonDate { get; set; }
        }

        public class GroupingProjection
        {
            public int? Id { get; set; }

            public string Label { get; set; }
        }

        public class ConnectionStatusProjection
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Order { get; set; }
            public string HighlightColor { get; set; }
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
