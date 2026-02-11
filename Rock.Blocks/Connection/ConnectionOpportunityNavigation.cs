// <copyright>
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Constants;
using Rock.Model;
using Rock.Security;
using Rock.Utility;
using Rock.ViewModels.Blocks.Connection.ConnectionOpportunityNavigation;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace Rock.Blocks.Connection
{
    /// <summary>
    /// Displays metrics of a connection type's combined opportunities and provides easy navigation into each opportunity's connection requests.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockBlockType" />

    [DisplayName( "Connection Opportunity Navigation" )]
    [Category( "Connection" )]
    [Description( "Displays metrics of a connection type's combined opportunities and provides easy navigation into each opportunity's connection requests." )]
    [SupportedSiteTypes( Model.SiteType.Web )]
    [ContextAware( typeof( Campus ) )]

    #region Block Attributes

    [LinkedPage( "Connections List Page",
        Key = AttributeKey.ConnectionsListPage,
        Description = @"Select the page that the ""View Requests"" and list buttons should open to view the connections list.",
        DefaultValue = Rock.SystemGuid.Page.CONNECTIONS_LIST,
        Order = 0,
        IsRequired = true )]

    [LinkedPage( "Connection Board Page",
        Key = AttributeKey.ConnectionBoardPage,
        Description = "Select the page that the board and grid buttons should open to view the connection board in board or grid view.",
        DefaultValue = Rock.SystemGuid.Page.CONNECTIONS_BOARD,
        Order = 1,
        IsRequired = true )]

    [LinkedPage( "Operational Snapshot Page",
        Key = AttributeKey.OperationalSnapshotPage,
        Description = "Select the page that the snapshot button should open to view the operational snapshot.",
        DefaultValue = Rock.SystemGuid.Page.CONNECTIONS_OPERATIONAL_SNAPSHOT,
        Order = 2,
        IsRequired = true )]

    #endregion Block Attributes

    [Rock.SystemGuid.EntityTypeGuid( "6A3E1450-486E-45CF-8979-E280DACAEFEA" )]
    [Rock.SystemGuid.BlockTypeGuid( "91080C44-AFBF-4A02-AD0D-BD7E01F9D1DE" )]
    public class ConnectionOpportunityNavigation : RockBlockType
    {
        #region Keys

        private static class AttributeKey
        {
            public const string ConnectionsListPage = "ConnectionsListPage";
            public const string ConnectionBoardPage = "ConnectionBoardPage";
            public const string OperationalSnapshotPage = "OperationalSnapshotPage";
        }

        private static class NavigationUrlKey
        {
            // Connection Type-level URLs.
            public const string TypeConnectionsListPage = "TypeConnectionsListPage";
            public const string TypeOperationalSnapshotPage = "TypeOperationalSnapshotPage";

            // Connection Opportunity-level URLs.
            public const string OpportunityConnectionsListPage = "OpportunityConnectionsListPage";
            public const string OpportunityConnectionBoardPage = "OpportunityConnectionBoardPage";
        }

        private static class PageParameterKey
        {
            public const string ConnectionType = "ConnectionType";
            public const string ConnectionOpportunity = "ConnectionOpportunity";
        }

        private static class PersonPreferenceKey
        {
            public const string OnlyShowMyOpportunities = "only-show-my-opportunities";
        }

        #endregion Keys

        #region Properties

        /// <summary>
        /// Gets the block person preferences.
        /// </summary>
        private PersonPreferenceCollection BlockPersonPreferences => this.GetBlockPersonPreferences();

        /// <summary>
        /// Gets whether to show only they current person's opportunities.
        /// </summary>
        private bool OnlyShowMyOpportunities => BlockPersonPreferences
            .GetValue( PersonPreferenceKey.OnlyShowMyOpportunities )
            .AsBoolean( true );

        #endregion Properties

        #region RockBlockType Implementation

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new ConnectionOpportunityNavigationInitializationBox();

            var connectionType = GetConnectionTypeFromPageParameter();
            if ( connectionType == null )
            {
                // Return early if unable to find the connection type.
                box.ErrorMessage = $"Unable to find the specified {ConnectionType.FriendlyTypeName}.";
                return box;
            }

            if ( !GetIsAuthorizedToView( connectionType ) )
            {
                // Return early if the current person is not authorized to view the connection type.
                box.ErrorMessage = EditModeMessage.NotAuthorizedToView( ConnectionType.FriendlyTypeName );
                return box;
            }

            box.Name = connectionType.Name;
            box.IconCssClass = connectionType.IconCssClass;
            box.EnabledViews = connectionType.EnabledViews;
            box.AnalyticsAndSummaries = LoadAnalyticsAndSummaries( connectionType.Id );
            box.NavigationUrls = GetBoxNavigationUrls();

            return box;
        }

        #endregion RockBlockType Implementation

        #region Block Actions

        /// <summary>
        /// Gets the connection opportunity analytics and summaries.
        /// </summary>
        /// <returns>An object containing information about the connection opportunity analytics and summaries.</returns>
        [BlockAction]
        public BlockActionResult GetAnalyticsAndSummaries()
        {
            var connectionType = GetConnectionTypeFromPageParameter();
            if ( connectionType == null )
            {
                return ActionBadRequest( $"Unable to find the specified {ConnectionType.FriendlyTypeName}." );
            }

            if ( !GetIsAuthorizedToView( connectionType ) )
            {
                return ActionUnauthorized( EditModeMessage.NotAuthorizedToView( ConnectionType.FriendlyTypeName ) );
            }

            var response = LoadAnalyticsAndSummaries( connectionType.Id );

            return ActionOk( response );
        }

        #endregion Block Actions

        #region Private Methods

        /// <summary>
        /// Gets the <see cref="ConnectionTypeCache"/> based on the page parameter.
        /// </summary>
        /// <returns>An <see cref="ConnectionTypeCache"/> based on the page parameter.</returns>
        private ConnectionTypeCache GetConnectionTypeFromPageParameter()
        {
            var communicationKey = PageParameter( PageParameterKey.ConnectionType );

            return ConnectionTypeCache.Get( communicationKey, !PageCache.Layout.Site.DisablePredictableIds );
        }

        /// <summary>
        /// Gets whether the current person is authorized to view [or edit] the <see cref="ConnectionTypeCache"/>.
        /// </summary>
        /// <param name="connectionType">The <see cref="ConnectionTypeCache"/> to check.</param>
        /// <returns>Whether the current person is authorized to view [or edit] the <see cref="ConnectionTypeCache"/>.</returns>
        private bool GetIsAuthorizedToView( ConnectionTypeCache connectionType )
        {
            var currentPerson = GetCurrentPerson();
            return connectionType.IsAuthorized( Authorization.VIEW, currentPerson )
                || connectionType.IsAuthorized( Authorization.EDIT, currentPerson );
        }

        /// <summary>
        /// Loads connection opportunity analytics and summaries for the provided <paramref name="connectionTypeId"/>.
        /// </summary>
        /// <param name="connectionTypeId">
        /// The identifier of the <see cref="ConnectionType"/> for which to load opportunity analytics and summaries.
        /// </param>
        /// <returns>A <see cref="ConnectionOpportunityAnalyticsAndSummariesBag"/>.S</returns>
        private ConnectionOpportunityAnalyticsAndSummariesBag LoadAnalyticsAndSummaries( int connectionTypeId )
        {
            return new ConnectionOpportunityAnalyticsAndSummariesBag
            {
                ConnectionOpportunitySummaries = LoadConnectionOpportunitySummaries( connectionTypeId )
            };
        }

        /// <summary>
        /// Loads <see cref="ConnectionOpportunity"/> data from the database and uses this data to buld a list of
        /// <see cref="ConnectionOpportunitySummaryBag"/>s.
        /// </summary>
        /// <param name="connectionTypeId">
        /// The identifier of the <see cref="ConnectionType"/> for which to load <see cref="ConnectionOpportunity"/> data.
        /// </param>
        /// <returns>A list of <see cref="ConnectionOpportunitySummaryBag"/>s.</returns>
        private List<ConnectionOpportunitySummaryBag> LoadConnectionOpportunitySummaries( int connectionTypeId )
        {
            var personId = GetCurrentPerson()?.Id ?? 0;
            var campusId = RequestContext.GetContextEntity<Campus>()?.Id;
            var today = RockDateTime.Today;

            var connectionRequestQry = new ConnectionRequestService( RockContext )
                .Queryable()
                .Where( cr =>
                    cr.ConnectionOpportunity.ConnectionTypeId == connectionTypeId
                    && ( !campusId.HasValue || cr.CampusId == campusId.Value )
                    && (
                        cr.ConnectionState == ConnectionState.Active
                        || cr.ConnectionState == ConnectionState.FutureFollowUp
                    )
                );

            var connectionOpportunityQry = new ConnectionOpportunityService( RockContext )
                .Queryable()
                .Where( co => co.ConnectionTypeId == connectionTypeId );

            if ( OnlyShowMyOpportunities )
            {
                connectionRequestQry = connectionRequestQry
                    .Where( cr =>
                        cr.ConnectorPersonAliasId.HasValue
                        && cr.ConnectorPersonAlias.PersonId == personId
                    );

                connectionOpportunityQry = connectionOpportunityQry
                    .Where( co => connectionRequestQry.Any( cr =>
                            cr.ConnectionOpportunityId == co.Id
                        )
                    );
            }

            var requestCountsQry = connectionRequestQry
                .GroupBy( cr => cr.ConnectionOpportunityId )
                .Select( g => new
                {
                    ConnectionOpportunityId = g.Key,
                    ActiveRequestCount = g.Count( r => r.ConnectionState == ConnectionState.Active ),
                    OverdueRequestCount = g.Count( r => r.DueDate.HasValue && DbFunctions.TruncateTime( r.DueDate.Value ) < today ),
                    DueSoonRequestCount = g.Count( r => r.DueSoonDate.HasValue && DbFunctions.TruncateTime( r.DueSoonDate.Value ) >= today ),
                    UnassignedRequestCount = g.Count( r => !r.ConnectorPersonAliasId.HasValue ),
                    AssignedToYouRequestCount = g.Count( r => r.ConnectorPersonAliasId.HasValue && r.ConnectorPersonAlias.PersonId == personId )
                } );

            var summaries = connectionOpportunityQry
                .GroupJoin(
                    requestCountsQry,
                    co => co.Id,
                    counts => counts.ConnectionOpportunityId,
                    ( co, counts ) => new
                    {
                        ConnectionOpportunity = co,
                        RequestCounts = counts
                    }
                )
                .SelectMany(
                    x => x.RequestCounts.DefaultIfEmpty(),
                    ( x, counts ) => new ConnectionOpportunitySummaryBag
                    {
                        Id = x.ConnectionOpportunity.Id,
                        IconCssClass = x.ConnectionOpportunity.IconCssClass,
                        Name = x.ConnectionOpportunity.Name,
                        Summary = x.ConnectionOpportunity.Summary,
                        Order = x.ConnectionOpportunity.Order,
                        ActiveRequestCount = counts == null ? 0 : counts.ActiveRequestCount,
                        OverdueRequestCount = counts == null ? 0 : counts.OverdueRequestCount,
                        DueSoonRequestCount = counts == null ? 0 : counts.DueSoonRequestCount,
                        UnassignedRequestCount = counts == null ? 0 : counts.UnassignedRequestCount,
                        AssignedToYouRequestCount = counts == null ? 0 : counts.AssignedToYouRequestCount
                    }
                )
                .OrderBy( s => s.Order )
                .ThenBy( s => s.Name )
                .ToList();

            summaries.ForEach( s => s.TranslateIdToIdKey() );

            return summaries;
        }

        /// <summary>
        /// Gets the box navigation URLs required for the page to operate.
        /// </summary>
        /// <returns>A dictionary of key names and URL values.</returns>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            var connectionTypeKey = RequestContext.GetPageParameter( PageParameterKey.ConnectionType );

            var opportunityQueryParams = new Dictionary<string, string>
            {
                { PageParameterKey.ConnectionType, connectionTypeKey },
                { PageParameterKey.ConnectionOpportunity, "((Key))" }
            };

            return new Dictionary<string, string>
            {
                // Connection Type-level URLs.
                [NavigationUrlKey.TypeConnectionsListPage] = this.GetLinkedPageUrl( AttributeKey.ConnectionsListPage, PageParameterKey.ConnectionType, connectionTypeKey ),
                [NavigationUrlKey.TypeOperationalSnapshotPage] = this.GetLinkedPageUrl( AttributeKey.OperationalSnapshotPage, PageParameterKey.ConnectionType, connectionTypeKey ),

                // Connection Opportunity-level URLs.
                [NavigationUrlKey.OpportunityConnectionsListPage] = this.GetLinkedPageUrl( AttributeKey.ConnectionsListPage, opportunityQueryParams ),
                [NavigationUrlKey.OpportunityConnectionBoardPage] = this.GetLinkedPageUrl( AttributeKey.ConnectionBoardPage, opportunityQueryParams )
            };
        }

        #endregion Private Methods
    }
}
