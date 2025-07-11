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
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;

using Rock.Attribute;
using Rock.Communication.Chat;
using Rock.Communication.Chat.DTO;
using Rock.Data;
using Rock.Model.Groups.Group.Options;
using Rock.Web.Cache;

using Z.EntityFramework.Plus;

namespace Rock.Model
{
    /*
    12/16/2024 - DSH

    The Group model participates in the the TPT (Table-Per-Type) pattern. This
    can cause some rare unexpected results. See the engineering note above the
    Group class for details.
    */

    /// <summary>
    /// Data access/service class for <see cref="Rock.Model.Group"/> objects.
    /// </summary>
    public partial class GroupService
    {
        /// <summary>
        /// Returns a queryable collection of <see cref="Rock.Model.Group">Groups</see>, excluding archived groups
        /// </summary>
        /// <returns></returns>
        public override IQueryable<Group> Queryable()
        {
            // override Group Queryable so that Archived groups are never included
            return base.Queryable().Where( a => a.IsArchived == false );
        }

        /// <summary>
        /// Returns a queryable collection of <see cref="Rock.Model.Group">Groups</see>, excluding archived groups,
        /// with eager loading of properties specified in includes
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        public override IQueryable<Group> Queryable( string includes )
        {
            // override Group Queryable so that Archived groups are never included
            return base.Queryable( includes ).Where( a => a.IsArchived == false );
        }

        /// <summary>
        /// Returns a queryable of archived groups
        /// </summary>
        /// <returns></returns>
        public IQueryable<Group> GetArchived()
        {
            return this.AsNoFilter().Where( a => a.IsArchived == true );
        }

        /// <summary>
        /// Returns an enumerable collection of <see cref="Rock.Model.Group"/> entities that by their <see cref="Rock.Model.GroupType"/> Id.
        /// </summary>
        /// <param name="groupTypeId">An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.GroupType"/> that they belong to.</param>
        /// <returns>An enumerable collection of <see cref="Rock.Model.Group">Groups</see> belong to a specific <see cref="Rock.Model.GroupType"/>.</returns>
        public IQueryable<Group> GetByGroupTypeId( int groupTypeId )
        {
            return Queryable().Where( t => t.GroupTypeId == groupTypeId );
        }

        /// <summary>
        /// Returns the <see cref="Rock.Model.Group"/> containing a Guid property that matches the provided value.
        /// </summary>
        /// <param name="guid">A <see cref="System.Guid"/> to find a <see cref="Rock.Model.Group"/> by.</param>
        /// <returns>The <see cref="Rock.Model.Group" /> who's Guid property matches the provided value.  If no match is found, returns null.</returns>
        public Group GetByGuid( Guid guid )
        {
            return Queryable().FirstOrDefault( t => t.Guid == guid );
        }

        /// <summary>
        /// Returns an enumerable collection of <see cref="Rock.Model.Group">Groups</see> by their IsSecurityRole flag. This is the same as calling
        /// <seealso cref="GroupServiceExtensions.IsSecurityRoleOrSecurityRoleGroupType" /> or <seealso cref="GroupServiceExtensions.IsNotSecurityRoleOrSecurityRoleGroupType" />
        /// </summary>
        /// <param name="isSecurityRole">A <see cref="System.Boolean"/> representing the IsSecurityRole flag value to search by.</param>
        /// <returns>An enumerable collection of <see cref="Rock.Model.Group">Groups</see> that contains a IsSecurityRole flag that matches the provided value.</returns>
        public IQueryable<Group> GetByIsSecurityRole( bool isSecurityRole )
        {
            if ( isSecurityRole )
            {
                return Queryable().IsSecurityRoleOrSecurityRoleGroupType();
            }
            else
            {
                return Queryable().IsNotSecurityRoleOrSecurityRoleGroupType();
            }
        }

        /// <summary>
        /// Returns a collection of <see cref="Rock.Model.Group">Groups</see> by the Id of its parent <see cref="Rock.Model.Group"/>. 
        /// </summary>
        /// <param name="parentGroupId">A <see cref="System.Int32" /> representing the Id of the parent <see cref="Rock.Model.Group"/> to search by. This value
        /// is nullable and a null value will search for <see cref="Rock.Model.Group">Groups</see> that do not inherit from other groups.</param>
        /// <returns>An enumerable collection of <see cref="Rock.Model.Group">Groups</see> who's ParentGroupId matches the provided value.</returns>
        public IQueryable<Group> GetByParentGroupId( int? parentGroupId )
        {
            return Queryable().Where( t => ( t.ParentGroupId == parentGroupId || ( parentGroupId == null && t.ParentGroupId == null ) ) );
        }

        /// <summary>
        /// Returns an enumerable collection of <see cref="Rock.Model.Group">Groups</see> by the Id of their parent <see cref="Rock.Model.Group"/> and by the Group's name.
        /// </summary>
        /// <param name="parentGroupId">An <see cref="System.Int32" /> representing the Id of the parent <see cref="Rock.Model.Group"/> to search by.</param>
        /// <param name="name">A <see cref="System.String"/> containing the Name of the <see cref="Rock.Model.Group"/> to search by.</param>
        /// <returns>An enumerable collection of <see cref="Rock.Model.Group">Groups</see> who's ParentGroupId and Name matches the provided values.</returns>
        public IQueryable<Group> GetByParentGroupIdAndName( int? parentGroupId, string name )
        {
            return Queryable().Where( t => ( t.ParentGroupId == parentGroupId || ( parentGroupId == null && t.ParentGroupId == null ) ) && t.Name == name );
        }

        /// <summary>
        /// Gets a Queryable of chat-specific <see cref="Group"/>s, regardless of whether they're currently chat-enabled.
        /// </summary>
        /// <param name="groupTypeId">The optional identifier of the <see cref="GroupType"/> for the <see cref="Group"/>s to query.</param>
        /// <returns>A Queryable of chat-specific <see cref="Group"/>s.</returns>
        /// <remarks>This will include archived and inactive, chat-specific <see cref="Group"/>s.</remarks>
        [RockInternal( "17.1", true )]
        public IQueryable<Group> GetChatChannelGroupsQuery( int? groupTypeId = null )
        {
            var qry = AsNoFilter()
                .Where( g =>
                    (
                        // Even if a group is not currently chat-enabled, include it if it has a chat channel key.
                        g.ChatChannelKey != null
                        && g.ChatChannelKey != string.Empty
                    )
                    || (
                        g.GroupType.IsChatAllowed
                        && (
                            g.GroupType.IsChatEnabledForAllGroups
                            || (
                                g.IsChatEnabledOverride.HasValue
                                && g.IsChatEnabledOverride.Value
                            )
                        )
                    )
                );

            if ( groupTypeId.HasValue )
            {
                qry = qry.Where( g => g.GroupTypeId == groupTypeId.Value );
            }

            return qry;
        }

        /// <summary>
        /// Gets a Queryable of all <see cref="Group"/>s that are currently chat-enabled.
        /// </summary>
        /// <param name="groupTypeId">The optional identifier of the <see cref="GroupType"/> for the <see cref="Group"/>s to query.</param>
        /// <returns>A Queryable of all <see cref="Group"/>s that are currently chat-enabled.</returns>
        /// <remarks>This will include archived and inactive, chat-enabled <see cref="Group"/>s.</remarks>
        internal IQueryable<Group> GetChatEnabledGroupsQuery( int? groupTypeId = null )
        {
            var qry = AsNoFilter()
                .Where( g =>
                    g.GroupType.IsChatAllowed
                    && (
                        g.GroupType.IsChatEnabledForAllGroups
                        || (
                            g.IsChatEnabledOverride.HasValue
                            && g.IsChatEnabledOverride.Value
                        )
                    )
                );

            if ( groupTypeId.HasValue )
            {
                qry = qry.Where( g => g.GroupTypeId == groupTypeId.Value );
            }

            return qry;
        }

        /// <summary>
        /// Gets the identifier of the group that represents a chat channel.
        /// </summary>
        /// <param name="chatChannelKey">The key that identifies the chat channel.</param>
        /// <returns>The group identifier or <see langword="null"/> if not found.</returns>
        public int? GetChatChannelGroupId( string chatChannelKey )
        {
            if ( chatChannelKey.IsNullOrWhiteSpace() )
            {
                return null;
            }

            // Check the key itself first, as the group ID might be embedded within.
            var groupId = ChatHelper.GetGroupId( chatChannelKey );
            if ( groupId.HasValue )
            {
                return groupId.Value;
            }

            // Next, check to see if we already have this mapping in the cache.
            var cacheKey = ChatHelper.GetChatChannelGroupIdCacheKey( chatChannelKey );
            groupId = RockCache.Get( cacheKey ) as int?;
            if ( groupId.HasValue )
            {
                return groupId.Value;
            }

            // Fall back to looking in the database and caching if we find it.
            // We always want to include archived groups, as they'll be considered inactive chat channels.
            groupId = AsNoFilter()
                .Where( g => g.ChatChannelKey == chatChannelKey )
                .Select( g => g.Id )
                .FirstOrDefault();

            if ( groupId.HasValue )
            {
                RockCache.AddOrUpdate( cacheKey, null, groupId.Value, RockDateTime.Now.AddSeconds( 300 ) );
            }

            return groupId;
        }

        #region Geospatial Queries

        /// <summary>
        /// Gets the family groups that are geofenced by any of the selected group's locations
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencedFamilies( int groupId )
        {
            // Get the geofences for the group
            var groupGeofences = this.Queryable().AsNoTracking()
                .Where( g => g.Id == groupId )
                .SelectMany( g => g.GroupLocations )
                .Where( g => g.Location.GeoFence != null )
                .Select( g => g.Location.GeoFence )
                .ToList();

            return GetGeofencedFamilies( groupGeofences );
        }

        /// <summary>
        /// Gets the family groups that are geofenced by any of the selected group's locations
        /// </summary>
        /// <param name="groupGuid">The group unique identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencedFamilies( Guid groupGuid )
        {
            // Get the geofences for the group
            var groupGeofences = this.Queryable().AsNoTracking()
                .Where( g => g.Guid.Equals( groupGuid ) )
                .SelectMany( g => g.GroupLocations )
                .Where( g => g.Location.GeoFence != null )
                .Select( g => g.Location.GeoFence )
                .ToList();

            return GetGeofencedFamilies( groupGeofences );
        }

        /// <summary>
        /// Gets the family groups that are geofenced by any of the selected geofences
        /// </summary>
        /// <param name="geofences">The geofences.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencedFamilies( List<DbGeography> geofences )
        {
            var rockContext = ( RockContext ) this.Context;
            var groupLocationService = new GroupLocationService( rockContext );

            var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ).Id;

            return groupLocationService.GetMappedLocationsByGeofences( geofences )
                .Where( l =>
                    l.Group != null &&
                    l.Group.GroupTypeId == familyGroupTypeId )
                .Select( l => l.Group );
        }

        /// <summary>
        /// Gets the groups of a particular type that geofence the selected person's mapped location(s)
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencingGroups( int personId, int groupTypeId )
        {
            var rockContext = ( RockContext ) this.Context;
            var personService = new PersonService( rockContext );
            var personGeopoints = personService.GetGeopoints( personId );
            return GetGeofencingGroups( personGeopoints, groupTypeId );
        }

        /// <summary>
        /// Gets the groups of a particular type that geofence the selected person's mapped location(s)
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupTypeGuid">The group type unique identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencingGroups( int personId, Guid groupTypeGuid )
        {
            var rockContext = ( RockContext ) this.Context;
            var personService = new PersonService( rockContext );
            var personGeopoints = personService.GetGeopoints( personId );
            return GetGeofencingGroups( personGeopoints, groupTypeGuid );
        }

        /// <summary>
        /// Gets the groups of a selected type that have a geofence location that surrounds any of the
        /// selected points
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencingGroups( IQueryable<DbGeography> points, int groupTypeId )
        {
            // Get the groups that have a location that intersects with any of the family's locations
            return this.Queryable()
                .Where( g =>
                    g.GroupTypeId.Equals( groupTypeId ) &&
                    g.IsActive &&
                    g.GroupLocations.Any( l =>
                        l.Location.GeoFence != null &&
                        points.Any( p => p.Intersects( l.Location.GeoFence ) )
                    ) );
        }

        /// <summary>
        /// Gets the groups of a selected type that have a geofence location that surrounds any of the
        /// selected points
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="groupTypeGuid">The group type unique identifier.</param>
        /// <returns></returns>
        public IQueryable<Group> GetGeofencingGroups( IQueryable<DbGeography> points, Guid groupTypeGuid )
        {
            // Get the groups that have a location that intersects with any of the family's locations
            return this.Queryable()
                .Where( g =>
                    g.GroupType.Guid.Equals( groupTypeGuid ) &&
                    g.IsActive &&
                    g.GroupLocations.Any( l =>
                        l.Location.GeoFence != null &&
                        points.Any( p => p.Intersects( l.Location.GeoFence ) )
                    ) );
        }

        /// <summary>
        /// Gets the nearest group.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <returns></returns>
        public Group GetNearestGroup( int personId, int groupTypeId )
        {
            var rockContext = ( RockContext ) this.Context;
            var personService = new PersonService( rockContext );
            var personGeopoint = personService.GetGeopoints( personId ).FirstOrDefault();
            if ( personGeopoint != null )
            {
                var groupLocation = this.Queryable()
                    .Where( g =>
                        g.GroupTypeId.Equals( groupTypeId ) )
                    .SelectMany( g =>
                        g.GroupLocations
                            .Where( gl =>
                                gl.Location != null &&
                                gl.Location.GeoPoint != null
                            )
                    )
                    .OrderBy( gl => gl.Location.GeoPoint.Distance( personGeopoint ) )
                    .FirstOrDefault();

                if ( groupLocation != null )
                {
                    return groupLocation.Group;
                }
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Gets immediate navigation children of a group (id) or a rootGroupId. Specify 0 for both Id and rootGroupId to get top level groups limited 
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="rootGroupId">The root group identifier.</param>
        /// <param name="limitToSecurityRoleGroups">if set to <c>true</c> [limit to security role groups].</param>
        /// <param name="groupTypeIncludedIds">The group type included ids.</param>
        /// <param name="groupTypeExcludedIds">The group type excluded ids.</param>
        /// <param name="includeInactiveGroups">if set to <c>true</c> [include inactive groups].</param>
        /// <returns></returns>
        public IQueryable<Group> GetNavigationChildren( int id, int rootGroupId, bool limitToSecurityRoleGroups, List<int> groupTypeIncludedIds, List<int> groupTypeExcludedIds, bool includeInactiveGroups = true )
        {
            return this.GetChildren( id, rootGroupId, limitToSecurityRoleGroups, groupTypeIncludedIds, groupTypeExcludedIds, includeInactiveGroups, true );
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="rootGroupId">The root group identifier.</param>
        /// <param name="limitToSecurityRoleGroups">if set to <c>true</c> [limit to security role groups].</param>
        /// <param name="groupTypeIncludedIds">The group type included ids.</param>
        /// <param name="groupTypeExcludedIds">The group type excluded ids.</param>
        /// <param name="includeInactiveGroups">if set to <c>true</c> [include inactive groups].</param>
        /// <param name="limitToShowInNavigation">if set to <c>true</c> [limit to show in navigation].</param>
        /// <returns></returns>
        public IQueryable<Group> GetChildren( int id, int rootGroupId, bool limitToSecurityRoleGroups, List<int> groupTypeIncludedIds, List<int> groupTypeExcludedIds, bool includeInactiveGroups, bool limitToShowInNavigation )
        {
            return this.GetChildren( id, rootGroupId, limitToSecurityRoleGroups, groupTypeIncludedIds, groupTypeExcludedIds, includeInactiveGroups, limitToShowInNavigation, 0, false, false );
        }

        /// <summary>
        /// Gets immediate children of a group (id) or a rootGroupId. Specify 0 for both Id and rootGroupId to get top level groups limited
        /// </summary>
        /// <param name="id">The ID of the Group to get the children of (or 0 to use rootGroupId)</param>
        /// <param name="rootGroupId">The root group ID</param>
        /// <param name="limitToSecurityRoleGroups">if set to <c>true</c> [limit to security role groups].</param>
        /// <param name="groupTypeIncludedIds">The group type included ids.</param>
        /// <param name="groupTypeExcludedIds">The group type excluded ids.</param>
        /// <param name="includeInactiveGroups">if set to <c>true</c> [include inactive groups].</param>
        /// <param name="limitToShowInNavigation">if set to <c>true</c> [limit to show in navigation].</param>
        /// <param name="limitToPublic">if set to <c>true</c> [limit to public groups].</param>
        /// <returns></returns>
        public IQueryable<Group> GetChildren( int id, int rootGroupId, bool limitToSecurityRoleGroups, List<int> groupTypeIncludedIds, List<int> groupTypeExcludedIds, bool includeInactiveGroups, bool limitToShowInNavigation, bool limitToPublic = false )
        {
            return this.GetChildren( id, rootGroupId, limitToSecurityRoleGroups, groupTypeIncludedIds, groupTypeExcludedIds, includeInactiveGroups, limitToShowInNavigation, 0, false, limitToPublic );
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="rootGroupId">The root group identifier.</param>
        /// <param name="limitToSecurityRoleGroups">if set to <c>true</c> [limit to security role groups].</param>
        /// <param name="groupTypeIncludedIds">The group type included ids.</param>
        /// <param name="groupTypeExcludedIds">The group type excluded ids.</param>
        /// <param name="includeInactiveGroups">if set to <c>true</c> [include inactive groups].</param>
        /// <param name="limitToShowInNavigation">if set to <c>true</c> [limit to show in navigation].</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="includeNoCampus">if set to <c>true</c> [include no campus].</param>
        /// <returns></returns>
        public IQueryable<Group> GetChildren( int id, int rootGroupId, bool limitToSecurityRoleGroups, List<int> groupTypeIncludedIds, List<int> groupTypeExcludedIds, bool includeInactiveGroups, bool limitToShowInNavigation, int campusId, bool includeNoCampus )
        {
            return this.GetChildren( id, rootGroupId, limitToSecurityRoleGroups, groupTypeIncludedIds, groupTypeExcludedIds, includeInactiveGroups, limitToShowInNavigation, 0, includeNoCampus, false );
        }

        /// <summary>
        /// Gets immediate children of a group (id) or a rootGroupId. Specify 0 for both Id and rootGroupId to get top level groups limited
        /// </summary>
        /// <param name="id">The ID of the Group to get the children of (or 0 to use rootGroupId)</param>
        /// <param name="rootGroupId">The root group ID</param>
        /// <param name="limitToSecurityRoleGroups">if set to <c>true</c> [limit to security role groups].</param>
        /// <param name="groupTypeIncludedIds">The group type included ids.</param>
        /// <param name="groupTypeExcludedIds">The group type excluded ids.</param>
        /// <param name="includeInactiveGroups">if set to <c>true</c> [include inactive groups].</param>
        /// <param name="limitToShowInNavigation">if set to <c>true</c> [limit to show in navigation].</param>
        /// <param name="campusId">if set it will filter groups based on campus</param>
        /// <param name="includeNoCampus">if campus set and set to <c>true</c> [include groups with no campus].</param>
        /// <param name="limitToPublic">if set to <c>true</c> [limit to public groups].</param>
        /// <returns></returns>
        public IQueryable<Group> GetChildren( int id, int rootGroupId, bool limitToSecurityRoleGroups, List<int> groupTypeIncludedIds, List<int> groupTypeExcludedIds, bool includeInactiveGroups, bool limitToShowInNavigation, int campusId, bool includeNoCampus, bool limitToPublic = false )
        {
            var qry = Queryable();

            if ( id == 0 )
            {
                if ( rootGroupId != 0 )
                {
                    qry = qry.Where( a => a.ParentGroupId == rootGroupId );
                }
                else
                {
                    qry = qry.Where( a => a.ParentGroupId == null );
                }
            }
            else
            {
                qry = qry.Where( a => a.ParentGroupId == id );
            }

            if ( !includeInactiveGroups )
            {
                qry = qry.Where( a => a.IsActive );
            }

            if ( limitToPublic )
            {
                qry = qry.Where( a => a.IsPublic );
            }

            if ( limitToSecurityRoleGroups )
            {
                qry = qry.Where( a => a.IsSecurityRole );
            }

            if ( campusId > 0 )
            {
                if ( includeNoCampus )
                {
                    qry = qry.Where( a => a.CampusId == campusId || a.Campus == null );
                }
                else
                {
                    qry = qry.Where( a => a.CampusId == campusId );
                }
            }

            if ( groupTypeIncludedIds != null && groupTypeIncludedIds.Any() )
            {
                // if groupTypeIncludedIds is specified, only get grouptypes that are in the groupTypeIncludedIds
                // NOTE: no need to factor in groupTypeExcludedIds since included would take precendance and the excluded ones would already not be included
                qry = qry.Where( a => groupTypeIncludedIds.Contains( a.GroupTypeId ) );
            }
            else if ( groupTypeExcludedIds != null && groupTypeExcludedIds.Any() )
            {
                qry = qry.Where( a => !groupTypeExcludedIds.Contains( a.GroupTypeId ) );
            }

            if ( limitToShowInNavigation )
            {
                qry = qry.Where( a => a.GroupType.ShowInNavigation == true );
            }

            return qry;
        }

        /// <summary>
        /// Gets the group descendants Common Table Expression.
        /// </summary>
        /// <param name="parentGroupId">The parent group identifier.</param>
        /// <param name="includeInactiveChildGroups">if set to <c>true</c> [include inactive child groups].</param>
        /// <returns></returns>
        private string GetGroupDescendentsCTESql( int parentGroupId, bool includeInactiveChildGroups )
        {
            StringBuilder cteBuilder = new StringBuilder( "with CTE as" );
            cteBuilder.AppendLine( "(" );
            cteBuilder.AppendLine( $"select * from [Group] where [ParentGroupId]={parentGroupId} and [IsArchived] = 0" );
            if ( !includeInactiveChildGroups )
            {
                cteBuilder.AppendLine( " and [IsActive] = 1" );
            }
            cteBuilder.AppendLine( "union all" );
            cteBuilder.AppendLine( "select a.* from [Group] [a]" );



            cteBuilder.AppendLine( "inner join CTE pcte on pcte.Id = [a].[ParentGroupId] where [a].[IsArchived] = 0" );
            if ( !includeInactiveChildGroups )
            {
                cteBuilder.AppendLine( " and a.[IsActive] = 1" );
            }
            cteBuilder.AppendLine( ")" );


            return cteBuilder.ToString();
        }

        /// <summary>
        /// Returns a list of <see cref="Rock.Model.Group">Groups</see> that are descendents of a specified group.
        /// </summary>
        /// <param name="parentGroupId">An <see cref="System.Int32" /> representing the Id of the <see cref="Rock.Model.Group" /> to retrieve descendents for.</param>
        /// <param name="includeInactiveChildGroups">if set to <c>true</c> [include inactive child groups].</param>
        /// <returns>
        /// An enumerable collection of <see cref="Rock.Model.Group">Groups</see> that are descendents of referenced group.
        /// </returns>
        public List<Group> GetAllDescendentGroups( int parentGroupId, bool includeInactiveChildGroups )
        {
            var cte = GetGroupDescendentsCTESql( parentGroupId, includeInactiveChildGroups );

            var sql = $@"
                {cte}
                select * from CTE";

            return this.ExecuteQuery( sql ).ToList();
        }

        /// <summary>
        /// Gets all descendent group ids that are descendents of a specified group.
        /// </summary>
        /// <param name="parentGroupId">The parent group identifier.</param>
        /// <param name="includeInactiveChildGroups">if set to <c>true</c> [include inactive child groups].</param>
        /// <returns></returns>
        public List<int> GetAllDescendentGroupIds( int parentGroupId, bool includeInactiveChildGroups )
        {
            var cte = GetGroupDescendentsCTESql( parentGroupId, includeInactiveChildGroups );

            var sql = $@"
                {cte}
                select Id from CTE";

            return ( this.Context as RockContext ).Database.SqlQuery<int>( sql ).ToList();
        }

        /// <summary>
        /// Determines if the specified group has descendants at all or active descendants (based on param).
        /// </summary>
        /// <param name="parentGroupId">The parent group identifier.</param>
        /// <param name="includeInactiveChildGroups">if set to <c>true</c> [include inactive child groups].</param>
        /// <returns></returns>
        public bool HasDescendantGroups( int parentGroupId, bool includeInactiveChildGroups )
        {
            var cte = GetGroupDescendentsCTESql( parentGroupId, includeInactiveChildGroups );

            var sql = $@"
                {cte}
                SELECT 1 WHERE EXISTS( SELECT [Id] from CTE );";

            return ( Context as RockContext ).Database.SqlQuery<int>( sql ).Any();
        }

        /// <summary>
        /// Returns a List of <see cref="GroupTypeCache">Group Types</see> of the groups that are descendents of the specified parentGroupId
        /// </summary>
        /// <param name="parentGroupId">The parent group identifier.</param>
        /// <param name="includeInactiveChildGroups">if set to <c>true</c> [include inactive child groups].</param>
        /// <returns></returns>
        public List<GroupTypeCache> GetAllDescendentsGroupTypes( int parentGroupId, bool includeInactiveChildGroups )
        {
            var cte = GetGroupDescendentsCTESql( parentGroupId, includeInactiveChildGroups );

            var sql = $@"
                {cte}
                select distinct GroupTypeId from CTE";

            var groupTypeIds = ( this.Context as RockContext ).Database.SqlQuery<int>( sql );


            return groupTypeIds.Select( a => GroupTypeCache.Get( a ) ).ToList();
        }

        /// <summary>
        /// Returns an enumerable collection of the <see cref="Rock.Model.Group" /> Ids that are ancestors of a specified groupId sorted starting with the most immediate parent
        /// </summary>
        /// <param name="childGroupId">The child group identifier.</param>
        /// <returns>
        /// An enumerable collection of the group Ids that are descendents of referenced groupId.
        /// </returns>
        public IOrderedEnumerable<int> GetAllAncestorIds( int childGroupId )
        {
            var result = this.Context.Database.SqlQuery<int>(
                @"
                with CTE as (
                select *, 0 as [Level] from [Group] where [Id]={0} and [IsArchived] = 0
                union all
                select [a].*, [Level] + 1 as [Level] from [Group] [a]
                inner join CTE pcte on pcte.ParentGroupId = [a].[Id]  and a.[IsArchived] = 0
                )
                select Id from CTE where Id != {0} order by Level
                ", childGroupId );

            // already ordered within the sql, so do a dummy order by to get IOrderedEnumerable
            return result.OrderBy( a => 0 );
        }

        /// <summary>
        /// Get all the group ids that have RSVP enabled,including all ancenstorsOfThoseGroups.
        /// Use this to detect if a group has RSVP enabled, or a group has a child group with RSVP enabled
        /// </summary>
        /// <returns></returns>
        public List<int> GetGroupIdsWithRSVPEnabledWithAncestors()
        {
            var rsvpEnabledGroupTypeIds = GroupTypeCache.All().Where( a => a.EnableRSVP ).Select( a => a.Id ).ToList();

            if ( !rsvpEnabledGroupTypeIds.Any() )
            {
                return new List<int>();
            }

            var sql = $@" ;with CTE as (
                select g1.*
                    from [Group] g1
                    where g1.GroupTypeId in ({rsvpEnabledGroupTypeIds.AsDelimited( "," )})
                    and g1.[IsArchived] = 0
                union all
                select [a].* from [Group] [a]
                inner join CTE pcte on pcte.ParentGroupId = [a].[Id]  and a.[IsArchived] = 0
                )
                select distinct Id from CTE";

            var groupsWithRSVPEnabled = this.Context.Database.SqlQuery<int>( sql ).ToList();

            return groupsWithRSVPEnabled;
        }

        /// <summary>
        /// Get all the group ids that have scheduling enabled,including all ancenstorsOfThoseGroups.
        /// Use this to detect if a group has scheduling enabled, or a group has a child group with scheduling enabled
        /// </summary>
        /// <returns></returns>
        public List<int> GetGroupIdsWithSchedulingEnabledWithAncestors()
        {
            var schedulingEnabledGroupTypeIds = GroupTypeCache.All().Where( a => a.IsSchedulingEnabled ).Select( a => a.Id ).ToList();

            if ( !schedulingEnabledGroupTypeIds.Any() )
            {
                return new List<int>();
            }

            var sql = $@" ;with CTE as (
                select g1.*
                    from [Group] g1 
                    where g1.GroupTypeId in ({schedulingEnabledGroupTypeIds.AsDelimited( "," )})
                    and g1.DisableScheduling != 1  and [IsArchived] = 0
                union all
                select [a].* from [Group] [a]
                inner join CTE pcte on pcte.ParentGroupId = [a].[Id]  and a.[IsArchived] = 0
                )
                select distinct Id from CTE";

            var groupsWithSchedulingEnabled = this.Context.Database.SqlQuery<int>( sql ).ToList();

            return groupsWithSchedulingEnabled;
        }

        /// <summary>
        /// Groups the name of the ancestor path.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <returns></returns>
        public string GroupAncestorPathName( int groupId )
        {
            return this.Context.Database.SqlQuery<string>( @"
                WITH CTE AS 
                (
	                SELECT [ParentGroupId], CAST ( [Name] AS VARCHAR(MAX) ) AS [Name]
	                FROM [Group] 
	                WHERE [Id] = {0} and [IsArchived] = 0
	
	                UNION ALL
	
	                SELECT G.[ParentGroupId], CAST ( G.[Name] + ' > ' + CTE.[Name] AS VARCHAR(MAX) )
	                FROM [Group] G
	                INNER JOIN CTE ON CTE.[ParentGroupId] = G.[Id] where g.[IsArchived] = 0
                )

                SELECT [Name]
                FROM CTE
                WHERE [ParentGroupId] IS NULL
", groupId ).FirstOrDefault();

        }

        /// <summary>
        /// Check if the group has the person as a member.
        /// Returns false if the group is not found or if the person id is null.
        /// </summary>
        /// <param name="groupGuid">The group unique identifier.</param>
        /// <param name="personId">The person identifier.</param>
        /// <returns></returns>
        public bool GroupHasMember( Guid groupGuid, int? personId )
        {
            if ( personId == null )
            {
                return false;
            }

            Group group = this.GetByGuid( groupGuid );
            if ( group == null )
            {
                return false;
            }

            return group.Members.Where( m => m.PersonId == personId ).Any();
        }

        #region Group Requirement Queries

        /// <summary>
        /// Groups the members not meeting requirements.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="includeWarnings">if set to <c>true</c> [include warnings].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns></returns>
        public Dictionary<GroupMember, Dictionary<PersonGroupRequirementStatus, DateTime>> GroupMembersNotMeetingRequirements( Group group, bool includeWarnings, bool includeInactive = false )
        {
            Dictionary<GroupMember, Dictionary<PersonGroupRequirementStatus, DateTime>> results = new Dictionary<GroupMember, Dictionary<PersonGroupRequirementStatus, DateTime>>();

            var rockContext = this.Context as RockContext;
            var groupRequirementService = new GroupRequirementService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );
            var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );

            var qryGroupRequirements = groupRequirementService.Queryable().Where( a => ( a.GroupId.HasValue && a.GroupId == group.Id ) || ( a.GroupTypeId.HasValue && a.GroupTypeId == group.GroupTypeId ) ).ToList();
            bool hasGroupRequirements = qryGroupRequirements.Any();
            if ( !hasGroupRequirements )
            {
                // if no group requirements, then there are no members that don't meet the requirements, so return an empty dictionary
                return new Dictionary<GroupMember, Dictionary<PersonGroupRequirementStatus, DateTime>>();
            }

            var qryGroupMembers = groupMemberService.Queryable().Where( a => a.GroupId == group.Id );
            var groupMemberRequirementList = GetGroupMemberRequirementList( group );

            if ( !includeInactive )
            {
                qryGroupMembers = qryGroupMembers.Where( a => a.GroupMemberStatus == GroupMemberStatus.Active );
            }

            var groupMemberList = qryGroupMembers.Include( a => a.GroupMemberRequirements ).ToList();

            // get a list of group member ids that don't meet all the requirements
            List<int> groupMemberIdsThatLackGroupRequirementsList = groupMemberList
                .Where( a =>
                    !qryGroupRequirements
                        .Where( r => !r.GroupRoleId.HasValue || r.GroupRoleId.Value == a.GroupRoleId )
                        .Where( r => r.AppliesToAgeClassification == AppliesToAgeClassification.All || r.AppliesToAgeClassification.ConvertToInt() == a.Person.AgeClassification.ConvertToInt() )
                        .Select( x => x.Id )
                        .All( r =>
                            a.GroupMemberRequirements
                                .Where( mr => mr.RequirementMetDateTime.HasValue || mr.WasOverridden )
                                .Select( x => x.GroupRequirementId )
                                .Contains( r ) ) )
                .Select( a => a.Id )
                .ToList();

            IEnumerable<GroupMember> membersWithIssuesList;

            if ( includeWarnings )
            {
                List<int> groupMemberIdsWithRequirementWarningsList = GroupMemberIdsWithRequirementWarnings( group, groupMemberRequirementList );
                membersWithIssuesList = groupMemberList.Where( a => groupMemberIdsThatLackGroupRequirementsList.Contains( a.Id ) || groupMemberIdsWithRequirementWarningsList.Contains( a.Id ) );
            }
            else
            {
                membersWithIssuesList = groupMemberList.Where( a => groupMemberIdsThatLackGroupRequirementsList.Contains( a.Id ) );
            }

            var groupMemberWithIssuesList = membersWithIssuesList.Select( a => new
            {
                GroupMember = a,
                GroupRequirementStatuses = groupMemberRequirementList.Where( x => x.GroupMemberId == a.Id )
            } ).ToList();

            var currentDateTime = RockDateTime.Now;

            foreach ( var groupMemberWithIssues in groupMemberWithIssuesList )
            {
                Dictionary<PersonGroupRequirementStatus, DateTime> statuses = new Dictionary<PersonGroupRequirementStatus, DateTime>();

                // populate where the status is known
                foreach ( var requirementStatus in groupMemberWithIssues.GroupRequirementStatuses )
                {
                    PersonGroupRequirementStatus status = new PersonGroupRequirementStatus();
                    status.GroupRequirement = requirementStatus.GroupRequirement;
                    status.PersonId = groupMemberWithIssues.GroupMember.PersonId;

                    DateTime occurrenceDate = new DateTime();

                    if ( requirementStatus.RequirementMetDateTime == null )
                    {
                        status.MeetsGroupRequirement = MeetsGroupRequirement.NotMet;
                        occurrenceDate = requirementStatus.RequirementFailDateTime ?? currentDateTime;
                    }
                    else if ( requirementStatus.RequirementWarningDateTime.HasValue )
                    {
                        status.MeetsGroupRequirement = MeetsGroupRequirement.MeetsWithWarning;
                        occurrenceDate = requirementStatus.RequirementWarningDateTime.Value;
                    }
                    else
                    {
                        status.MeetsGroupRequirement = MeetsGroupRequirement.Meets;
                        occurrenceDate = requirementStatus.RequirementMetDateTime.Value;
                    }

                    statuses.Add( status, occurrenceDate );
                }

                // also add any groupRequirements that they don't have statuses for (and therefore haven't met)
                foreach ( var groupRequirement in qryGroupRequirements )
                {
                    if ( !statuses.Any( x => x.Key.GroupRequirement.Id == groupRequirement.Id ) )
                    {
                        PersonGroupRequirementStatus status = new PersonGroupRequirementStatus();
                        status.GroupRequirement = groupRequirement;
                        status.PersonId = groupMemberWithIssues.GroupMember.PersonId;
                        status.MeetsGroupRequirement = MeetsGroupRequirement.NotMet;
                        statuses.Add( status, currentDateTime );
                    }
                }

                var statusesWithIssues = statuses.Where( a => a.Key.MeetsGroupRequirement != MeetsGroupRequirement.Meets ).ToDictionary( k => k.Key, v => v.Value );

                if ( statusesWithIssues.Any() )
                {
                    results.Add( groupMemberWithIssues.GroupMember, statusesWithIssues );
                }
            }

            return results;
        }

        /// <summary>
        /// Internal DTO class for GroupRequirements.
        /// </summary>
        private class GroupRequirementViewModel
        {
            public int GroupMemberId;
            public DateTime? RequirementWarningDateTime;
            public DateTime? RequirementFailDateTime;
            public DateTime? RequirementMetDateTime;
            public GroupRequirement GroupRequirement;
        }
        /// <summary>
        /// Gets a list of <see cref="GroupRequirementViewModel"/>s for the group.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private List<GroupRequirementViewModel> GetGroupMemberRequirementList( Group group )
        {
            var rockContext = this.Context as RockContext;
            var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
            var groupMemberRequirementQuery = groupMemberRequirementService.Queryable().Where( a => a.GroupMember.GroupId == group.Id );

            return groupMemberRequirementQuery
                .Select( a => new GroupRequirementViewModel()
                {
                    GroupMemberId = a.GroupMemberId,
                    RequirementWarningDateTime = a.RequirementWarningDateTime,
                    RequirementFailDateTime = a.RequirementFailDateTime,
                    RequirementMetDateTime = a.RequirementMetDateTime,
                    GroupRequirement = a.GroupRequirement
                } ).ToList();
        }

        /// <summary>
        /// Returns a list of Group Member Ids that have group requirement warnings.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public List<int> GroupMemberIdsWithRequirementWarnings( Group group )
        {
            var groupMemberRequirementList = GetGroupMemberRequirementList( group );
            return GroupMemberIdsWithRequirementWarnings( group, groupMemberRequirementList );
        }

        /// <summary>
        /// Internal method for GroupMemberIdsWithRequirementWarnings.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="groupMemberRequirementList">The list of <see cref="GroupRequirementViewModel"/>s.</param>
        /// <returns></returns>
        private List<int> GroupMemberIdsWithRequirementWarnings( Group group, List<GroupRequirementViewModel> groupMemberRequirementList )
        {
            return groupMemberRequirementList
                .Where( a => a.RequirementWarningDateTime != null
                        || a.RequirementFailDateTime != null )
                .Select( a => a.GroupMemberId )
                .Distinct().ToList();
        }

        /// <summary>
        /// Returns an IEnumerable list of Group Members from primary group that are people in secondary group.
        /// </summary>
        /// <remarks>For example, "this" group can be a family, and secondaryGroup can be the fundraising group the family member is in, so we can gather the other members of the same family that are in the fundraising group.</remarks>
        /// <param name="primaryGroup"></param>
        /// <param name="secondaryGroup"></param>
        /// <returns></returns>
        public IEnumerable<GroupMember> GroupMembersInAnotherGroup( Group primaryGroup, Group secondaryGroup )
        {
            // Do not allow the same group in both parameters.
            if ( primaryGroup.Guid == secondaryGroup.Guid )
            {
                return null;
            }

            var primaryMembers = primaryGroup.Members.Select( m => m.PersonId );
            return secondaryGroup.Members.Where( m => primaryMembers.Contains( m.PersonId ) );
        }

        #endregion Group Requirement Queries

        /// <summary>
        /// Calculates the family salutation.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="calculateFamilySalutationArgs">The calculate family salutation arguments.</param>
        /// <returns>
        /// string.
        /// </returns>
        public static string CalculateFamilySalutation( Group group, Person.CalculateFamilySalutationArgs calculateFamilySalutationArgs )
        {
            calculateFamilySalutationArgs = calculateFamilySalutationArgs ?? new Person.CalculateFamilySalutationArgs( false );
            var _familyType = GroupTypeCache.GetFamilyGroupType();
            var _adultRole = _familyType.Roles.FirstOrDefault( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );
            var _childRole = _familyType.Roles.FirstOrDefault( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) );

            var finalSeparator = calculateFamilySalutationArgs.FinalSeparator;
            var separator = calculateFamilySalutationArgs.Separator;
            var includeInactive = calculateFamilySalutationArgs.IncludeInactive;
            var includeChildren = calculateFamilySalutationArgs.IncludeChildren;
            var useFormalNames = calculateFamilySalutationArgs.UseFormalNames;
            var limitToPersonIds = calculateFamilySalutationArgs.LimitToPersonIds;

            // clean up the separators
            finalSeparator = $" {finalSeparator} "; // add spaces before and after
            if ( separator == "," )
            {
                separator = $"{separator} "; // add space after
            }
            else
            {
                separator = $" {separator} "; // add spaces before and after
            }

            List<string> familyMemberNames = new List<string>();
            string primaryLastName = string.Empty;

            var groupMemberService = new GroupMemberService( calculateFamilySalutationArgs.RockContext ?? new RockContext() );
            var groupId = group.Id;

            var familyMembersQry = groupMemberService.Queryable( false ).Where( a => a.GroupId == groupId );

            if ( limitToPersonIds != null && limitToPersonIds.Length > 0 )
            {
                familyMembersQry = familyMembersQry.Where( m => limitToPersonIds.Contains( m.PersonId ) );
            }

            // just in case there are no active members of the family, have this query ready
            var familyMembersQryIncludeInactive = familyMembersQry;

            // Filter for inactive.
            if ( !includeInactive )
            {
                var activeRecordStatusId = DefinedValueCache.GetId( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
                if ( activeRecordStatusId.HasValue )
                {
                    familyMembersQry = familyMembersQry.Where( f => f.Person.RecordStatusValueId.HasValue && f.Person.RecordStatusValueId.Value == activeRecordStatusId.Value );
                }
            }

            // just in case there are no Adults, have this query ready
            var familyMembersIncludingChildrenQry = familyMembersQry;

            // Filter out kids if not needed.
            if ( !includeChildren )
            {
                familyMembersQry = familyMembersQry.Where( f => f.GroupRoleId == _adultRole.Id );
            }

            var familyMembersList = familyMembersQry.Select( s => new
            {
                LastName = s.Person.LastName,
                NickName = s.Person.NickName,
                FirstName = s.Person.FirstName,
                Gender = s.Person.Gender,
                s.Person.BirthDate,
                s.Person.DeceasedDate,
                GroupRoleId = s.GroupRoleId
            } ).ToList();

            //  There are a couple of cases where there would be no familyMembers
            // 1) There are no adults in the family, and includeChildren=false .
            // 2) All the members of the family are deceased/inactive.
            // 3) The person somehow isn't in a family [Group] (which shouldn't happen)
            if ( !familyMembersList.Any() )
            {
                familyMembersList = familyMembersIncludingChildrenQry.Select( s => new
                {
                    LastName = s.Person.LastName,
                    NickName = s.Person.NickName,
                    FirstName = s.Person.FirstName,
                    Gender = s.Person.Gender,
                    s.Person.BirthDate,
                    s.Person.DeceasedDate,
                    GroupRoleId = s.GroupRoleId
                } ).ToList();

                if ( !familyMembersList.Any() )
                {
                    // no active adults or children, so see if there is at least one member of the family, regardless active status
                    familyMembersList = familyMembersQryIncludeInactive.Select( s => new
                    {
                        LastName = s.Person.LastName,
                        NickName = s.Person.NickName,
                        FirstName = s.Person.FirstName,
                        Gender = s.Person.Gender,
                        s.Person.BirthDate,
                        s.Person.DeceasedDate,
                        GroupRoleId = s.GroupRoleId
                    } ).ToList();
                }

                if ( !familyMembersList.Any() )
                {
                    // This should only happen if all members of the family are deceased.
                    return group.Name;
                }
            }

            // Determine if more than one last name is at play.
            var multipleLastNamesExist = familyMembersList.Select( f => f.LastName ).Distinct().Count() > 1;

            // Add adults and children separately as adults need to be sorted by gender and children by age.

            // Adults:
            var adults = familyMembersList.Where( f => f.GroupRoleId == _adultRole.Id ).OrderBy( f => f.Gender );

            if ( adults.Count() > 0 )
            {
                primaryLastName = adults.First().LastName;

                foreach ( var adult in adults )
                {
                    var firstName = adult.NickName;

                    if ( useFormalNames )
                    {
                        firstName = adult.FirstName;
                    }

                    if ( !multipleLastNamesExist )
                    {
                        familyMemberNames.Add( firstName );
                    }
                    else
                    {
                        familyMemberNames.Add( $"{firstName} {adult.LastName}" );
                    }
                }
            }

            // Children:
            if ( includeChildren || !adults.Any() )
            {
                var children = familyMembersList.Where( f => f.GroupRoleId == _childRole.Id ).OrderByDescending( f => Person.GetAge( f.BirthDate, f.DeceasedDate ) );

                if ( children.Count() > 0 )
                {
                    if ( primaryLastName.IsNullOrWhiteSpace() )
                    {
                        primaryLastName = children.First().LastName;
                    }

                    foreach ( var child in children )
                    {
                        var firstName = child.NickName;

                        if ( useFormalNames )
                        {
                            firstName = child.FirstName;
                        }

                        if ( !multipleLastNamesExist )
                        {
                            familyMemberNames.Add( firstName );
                        }
                        else
                        {
                            familyMemberNames.Add( $"{firstName} {child.LastName}" );
                        }
                    }
                }
            }

            var familySalutation = string.Join( separator, familyMemberNames ).ReplaceLastOccurrence( separator, finalSeparator );

            if ( !multipleLastNamesExist )
            {
                familySalutation = familySalutation + " " + primaryLastName;
            }

            return familySalutation;
        }

        /// <summary>
        /// Updates the family's Group Solution fields and <see cref="Data.DbContext.SaveChanges()">saves changes</see> to the database.
        /// Returns true if any changes were made.
        /// See <seealso cref="Group.GroupSalutation" /> and <seealso cref="Group.GroupSalutationFull" />
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>
        /// System.Int32.
        /// </returns>
        public static bool UpdateGroupSalutations( int groupId, RockContext rockContext )
        {
            var group = new GroupService( rockContext ).Get( groupId );
            var groupSalutation = GroupService.CalculateFamilySalutation( group, new Person.CalculateFamilySalutationArgs( false ) { RockContext = rockContext } ).Truncate( 250 );
            var groupSalutationFull = GroupService.CalculateFamilySalutation( group, new Person.CalculateFamilySalutationArgs( true ) { RockContext = rockContext } ).Truncate( 250 );
            if ( ( group.GroupSalutation != groupSalutation ) || ( group.GroupSalutationFull != groupSalutationFull ) )
            {
                group.GroupSalutation = groupSalutation;
                group.GroupSalutationFull = groupSalutationFull;
                group.ModifiedDateTime = RockDateTime.Now;

                // save changes without pre/post processing so we don't get stuck in recursion
                rockContext.SaveChanges( new SaveChangesArgs { DisablePrePostProcessing = true } );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Saves the new family to the database
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="familyMembers">The family members.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="savePersonAttributes">if set to <c>true</c> [save person attributes].</param>
        /// <returns></returns>
        public static Group SaveNewFamily( RockContext rockContext, List<GroupMember> familyMembers, int? campusId, bool savePersonAttributes )
        {
            var familyGroupType = GroupTypeCache.GetFamilyGroupType();
            string familyName = familyMembers.FirstOrDefault().Person.LastName + " Family";
            return SaveNewGroup( rockContext, familyGroupType.Id, null, familyName, familyMembers, campusId, savePersonAttributes );
        }

        /// <summary>
        /// Saves the new group to the database
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <param name="parentGroupGuid">The parent group unique identifier.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="groupMembers">The group members.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="savePersonAttributes">if set to <c>true</c> [save person attributes].</param>
        /// <returns></returns>
        public static Group SaveNewGroup( RockContext rockContext, int groupTypeId, Guid? parentGroupGuid, string groupName, List<GroupMember> groupMembers, int? campusId, bool savePersonAttributes )
        {
            var groupType = GroupTypeCache.Get( groupTypeId );

            if ( groupType != null )
            {
                var isFamilyGroupType = groupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );

                var groupService = new GroupService( rockContext );

                var group = new Group();

                group.GroupTypeId = groupType.Id;

                if ( parentGroupGuid.HasValue )
                {
                    var parentGroup = groupService.Get( parentGroupGuid.Value );
                    if ( parentGroup != null )
                    {
                        group.ParentGroupId = parentGroup.Id;
                    }
                }

                group.Name = groupName;

                if ( isFamilyGroupType )
                {
                    group.CampusId = campusId;
                }

                int? adultRoleId = null;
                var adultRole = new GroupTypeRoleService( rockContext ).Get( new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT ) );
                if ( adultRole != null )
                {
                    adultRoleId = adultRole.Id;
                }

                foreach ( var groupMember in groupMembers )
                {
                    var person = groupMember.Person;
                    if ( person != null )
                    {
                        person.FirstName = person.FirstName.FixCase();
                        person.NickName = person.NickName.FixCase();
                        person.MiddleName = person.MiddleName.FixCase();
                        person.LastName = person.LastName.FixCase();

                        group.Members.Add( groupMember );
                        groupMember.Group = group;
                    }

                    if ( !groupMember.IsValidGroupMember( rockContext ) )
                    {
                        throw new GroupMemberValidationException( groupMember.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" ) );
                    }
                }

                groupService.Add( group );
                rockContext.SaveChanges();

                var personService = new PersonService( rockContext );

                foreach ( var groupMember in groupMembers )
                {
                    var person = groupMember.Person;

                    if ( savePersonAttributes )
                    {
                        var newValues = person.AttributeValues;

                        person.LoadAttributes();
                        foreach ( var attributeCache in person.Attributes.Select( a => a.Value ) )
                        {
                            string oldValue = person.GetAttributeValue( attributeCache.Key ) ?? string.Empty;
                            string newValue = string.Empty;
                            if ( newValues != null &&
                                newValues.ContainsKey( attributeCache.Key ) &&
                                newValues[attributeCache.Key] != null )
                            {
                                newValue = newValues[attributeCache.Key].Value ?? string.Empty;
                            }

                            // Since we're adding a new entity/group we don't want to include empty attribute values.
                            // The oldValue could be an Attribute.DefaultValue while the newValue could be empty.
                            if ( !oldValue.Equals( newValue ) && newValue.IsNotNullOrWhiteSpace() )
                            {
                                Rock.Attribute.Helper.SaveAttributeValue( person, attributeCache, newValue, rockContext );
                            }
                        }
                    }

                    person = personService.Get( groupMember.PersonId );
                    if ( person != null )
                    {
                        bool updateRequired = false;
                        if ( groupMember.GroupRoleId == adultRoleId )
                        {
                            person.GivingGroupId = group.Id;
                            updateRequired = true;
                        }

                        if ( updateRequired )
                        {
                            rockContext.SaveChanges();
                        }

                        int? modifiedByPersonAliasId = person.ModifiedAuditValuesAlreadyUpdated ? person.ModifiedByPersonAliasId : ( int? ) null;
                    }
                }

                return group;
            }

            return null;
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="street1">The street1.</param>
        /// <param name="street2">The street2.</param>
        /// <param name="city">The city.</param>
        /// <param name="state">The state.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="country">The country.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            string street1, string street2, string city, string state, string postalCode, string country )
        {
            AddNewGroupAddress( rockContext, group, locationTypeGuid, street1, street2, city, state, postalCode, country, false );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="street1">The street1.</param>
        /// <param name="street2">The street2.</param>
        /// <param name="city">The city.</param>
        /// <param name="state">The state.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="country">The country.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            string street1, string street2, string city, string state, string postalCode, string country, bool moveExistingToPrevious )
        {
            var isMappedMailing = locationTypeGuid != SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS; // Mapped and Mailing = true unless location type is Previous
            AddNewGroupAddress( rockContext, group, locationTypeGuid, street1, street2, city, state, postalCode, country, moveExistingToPrevious, "", isMappedMailing, isMappedMailing );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="street1">The street1.</param>
        /// <param name="street2">The street2.</param>
        /// <param name="city">The city.</param>
        /// <param name="state">The state.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="country">The country.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        /// <param name="modifiedBy">The description of the page or process that called the function.</param>
        /// <param name="isMailingLocation">Sets the Is Mailing option on the new address.</param>
        /// <param name="isMappedLocation">Sets the Is Mapped option on the new address.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            string street1, string street2, string city, string state, string postalCode, string country, bool moveExistingToPrevious,
            string modifiedBy, bool isMailingLocation, bool isMappedLocation )
        {
            if ( !string.IsNullOrWhiteSpace( street1 ) ||
                 !string.IsNullOrWhiteSpace( street2 ) ||
                 !string.IsNullOrWhiteSpace( city ) ||
                 !string.IsNullOrWhiteSpace( postalCode ) ||
                 !string.IsNullOrWhiteSpace( country ) )
            {
                var location = new LocationService( rockContext ).Get( street1, street2, city, state, postalCode, country, group, true );
                AddNewGroupAddress( rockContext, group, locationTypeGuid, location, moveExistingToPrevious, modifiedBy, isMailingLocation, isMappedLocation );
            }
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="locationId">The location identifier.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid, int? locationId )
        {
            AddNewGroupAddress( rockContext, group, locationTypeGuid, locationId, false );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            int? locationId, bool moveExistingToPrevious )
        {
            var isMappedMailing = locationTypeGuid != SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS; // Mapped and Mailing = true unless location type is Previous
            AddNewGroupAddress( rockContext, group, locationTypeGuid, locationId, moveExistingToPrevious, "", isMappedMailing, isMappedMailing );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        /// <param name="modifiedBy">The modified by.</param>
        /// <param name="isMailingLocation">Sets the Is Mailing option on the new address.</param>
        /// <param name="isMappedLocation">Sets the Is Mapped option on the new address.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            int? locationId, bool moveExistingToPrevious, string modifiedBy, bool isMailingLocation, bool isMappedLocation )
        {
            if ( locationId.HasValue )
            {
                var location = new LocationService( rockContext ).Get( locationId.Value );
                AddNewGroupAddress( rockContext, group, locationTypeGuid, location, moveExistingToPrevious, modifiedBy, isMailingLocation, isMappedLocation );
            }
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="location">The location.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid, Location location )
        {
            AddNewGroupAddress( rockContext, group, locationTypeGuid, location, false );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="location">The location.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid, Location location, bool moveExistingToPrevious )
        {
            var isMappedMailing = locationTypeGuid != SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS; // Mapped and Mailing = true unless location type is Previous
            AddNewGroupAddress( rockContext, group, locationTypeGuid, location, moveExistingToPrevious, "", isMappedMailing, isMappedMailing );
        }

        /// <summary>
        /// Adds the new group address (it is doesn't already exist) and saves changes to the database.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="location">The location.</param>
        /// <param name="moveExistingToPrevious">if set to <c>true</c> [move existing to previous].</param>
        /// <param name="modifiedBy">The description of the page or process that called the function.</param>
        /// <param name="isMailingLocation">Sets the Is Mailing option on the new address.</param>
        /// <param name="isMappedLocation">Sets the Is Mapped option on the new address.</param>
        public static void AddNewGroupAddress( RockContext rockContext, Group group, string locationTypeGuid,
            Location location, bool moveExistingToPrevious, string modifiedBy, bool isMailingLocation, bool isMappedLocation )
        {
            if ( location != null )
            {
                var groupType = GroupTypeCache.Get( group.GroupTypeId );
                if ( groupType != null )
                {
                    var locationType = groupType.LocationTypeValues.FirstOrDefault( l => l.Guid.Equals( locationTypeGuid.AsGuid() ) );
                    if ( locationType != null )
                    {
                        var groupLocationService = new GroupLocationService( rockContext );
                        if ( !groupLocationService.Queryable()
                            .Where( gl =>
                                gl.GroupId == group.Id &&
                                gl.GroupLocationTypeValueId == locationType.Id &&
                                gl.LocationId == location.Id )
                            .Any() )
                        {
                            if ( moveExistingToPrevious )
                            {
                                var prevLocationType = groupType.LocationTypeValues.FirstOrDefault( l => l.Guid.Equals( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() ) );
                                if ( prevLocationType != null )
                                {
                                    foreach ( var prevLoc in groupLocationService.Queryable( "Location,GroupLocationTypeValue" )
                                        .Where( gl =>
                                            gl.GroupId == group.Id &&
                                            gl.GroupLocationTypeValueId == locationType.Id ) )
                                    {
                                        prevLoc.GroupLocationTypeValueId = prevLocationType.Id;
                                        prevLoc.IsMailingLocation = false;
                                        prevLoc.IsMappedLocation = false;
                                    }
                                }
                            }

                            string addressChangeField = locationType.Value;

                            var groupLocation = groupLocationService.Queryable()
                                .Where( gl =>
                                    gl.GroupId == group.Id &&
                                    gl.LocationId == location.Id )
                                .FirstOrDefault();
                            if ( groupLocation == null )
                            {
                                groupLocation = new GroupLocation();
                                groupLocation.Location = location;
                                groupLocation.IsMailingLocation = isMailingLocation;
                                groupLocation.IsMappedLocation = isMappedLocation;
                                group.GroupLocations.Add( groupLocation );
                            }
                            groupLocation.GroupLocationTypeValueId = locationType.Id;

                            rockContext.SaveChanges();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes or Archives (Soft-Deletes) Group record depending on GroupType.EnableGroupHistory and if the Group has history snapshots. Returns a boolean flag indicating if the deletion was successful.
        /// </summary>
        /// <param name="item">The <see cref="Rock.Model.Group" /> to delete.</param>
        /// <returns>
        /// A <see cref="System.Boolean" /> that indicates if the <see cref="Rock.Model.Group" /> was deleted successfully.
        /// </returns>
        public override bool Delete( Group item )
        {
            var groupTypeCache = GroupTypeCache.Get( item.GroupTypeId );
            if ( groupTypeCache?.EnableGroupHistory == true )
            {
                var rockContext = this.Context as RockContext;
                var groupHistoricalService = new GroupHistoricalService( rockContext );
                var groupMemberHistoricalService = new GroupMemberHistoricalService( rockContext );
                if ( groupHistoricalService.Queryable().Any( a => a.GroupId == item.Id ) || groupMemberHistoricalService.Queryable().Any( a => a.GroupId == item.Id ) )
                {
                    // if this group's GroupType has GroupHistory enabled, and this group has group or group member history snapshots, then we need to Archive instead of Delete
                    this.Archive( item, null, false );
                    return true;
                }
            }

            string message;
            if ( !CanDelete( item, out message, true ) )
            {
                return false;
            }

            // As discussed in https://github.com/SparkDevNetwork/Rock/issues/3640, we are going to delete
            // the association from any registrations that have a reference to this group (as long as there
            // no RegistrationRegistrant's tied to the group -- which was checked in the local CanDelete below).
            var registrationService = new RegistrationService( this.Context as RockContext );
            foreach ( var registration in registrationService.Queryable().Where( a => a.GroupId == item.Id ) )
            {
                registration.GroupId = null;
            }

            return base.Delete( item );
        }

        /// <summary>
        /// Determines whether the specified group can be deleted.
        /// Performs some additional checks that are missing from the
        /// auto-generated GroupService.CanDelete().
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="includeSecondLvl">If set to true, verifies that the item is not referenced by any second level relationships.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( Group item, out string errorMessage, bool includeSecondLvl )
        {
            errorMessage = string.Empty;

            bool canDelete = CanDelete( item, out errorMessage );

            if ( canDelete && includeSecondLvl )
            {
                if ( new Service<RegistrationRegistrant>( this.Context ).Queryable().Any( r => r.GroupMember.GroupId == item.Id ) )
                {
                    errorMessage = string.Format( "This {0} is assigned to a {1}.", Group.FriendlyTypeName, RegistrationRegistrant.FriendlyTypeName );
                    return false;
                }

                if ( new Service<EventItemOccurrence>( this.Context ).Queryable().Any( o => o.Linkages.Any( l => l.GroupId == item.Id ) ) )
                {
                    errorMessage += string.Format( "This {0} is assigned to a {1} linkage.", Group.FriendlyTypeName, EventItemOccurrence.FriendlyTypeName );
                    return false;
                }
            }

            return canDelete;
        }

        /// <summary>
        /// Deletes or Archives (Soft-Deletes) Group record depending on GroupType.EnableGroupHistory and if the Group has history snapshots, with an option to
        /// remove it from Auth if it is a security role
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="removeFromAuthTables">if set to <c>true</c> [remove from authentication tables].</param>
        public void Delete( Group group, bool removeFromAuthTables )
        {
            bool isSecurityRoleGroup = group.IsActive && ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) );
            if ( removeFromAuthTables && isSecurityRoleGroup )
            {
                AuthService authService = new AuthService( this.Context as RockContext );

                foreach ( var auth in authService.Queryable().Where( a => a.GroupId == group.Id ).ToList() )
                {
                    authService.Delete( auth );
                }

                Rock.Security.Authorization.Clear();
            }

            this.Delete( group );
        }

        /// <summary>
        /// Archives the specified group and removes it from Auth if it is a security role
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="currentPersonAliasId">The current person alias identifier.</param>
        /// <param name="removeFromAuthTables">if set to <c>true</c> remove from auth if this group is a security role.</param>
        public void Archive( Group group, int? currentPersonAliasId, bool removeFromAuthTables )
        {
            group.IsArchived = true;
            group.ArchivedByPersonAliasId = currentPersonAliasId;
            group.ArchivedDateTime = RockDateTime.Now;

            bool isSecurityRoleGroup = group.IsActive && ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) );
            if ( removeFromAuthTables && isSecurityRoleGroup )
            {
                AuthService authService = new AuthService( this.Context as RockContext );

                foreach ( var auth in authService.Queryable().Where( a => a.GroupId == group.Id ).ToList() )
                {
                    authService.Delete( auth );
                }

                Rock.Security.Authorization.Clear();
            }
        }

        /// <summary>
        /// Checks to see if there is an Archived Member of the group for the specified personId and groupRoleId
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <param name="archivedGroupMember">The archived group member record (if there are multiple, this will be the most recently archived record</param>
        /// <returns></returns>
        public bool ExistsAsArchived( Group group, int personId, int groupRoleId, out GroupMember archivedGroupMember )
        {
            archivedGroupMember = GetArchivedGroupMember( group, personId, groupRoleId );
            return archivedGroupMember != null;
        }

        /// <summary>
        /// If there is an Archived Member of the group for the specified personId and groupRoleId, returns the archived group member record, otherwise returns null
        /// (if there are multiple, this will return the most recently archived record)
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <returns></returns>
        public GroupMember GetArchivedGroupMember( Group group, int personId, int groupRoleId )
        {
            GroupMember archivedGroupMember;
            var groupMemberService = new GroupMemberService( this.Context as RockContext );
            archivedGroupMember = groupMemberService.GetArchived().Where( a => a.GroupId == group.Id && a.PersonId == personId && a.GroupRoleId == groupRoleId ).OrderByDescending( a => a.ArchivedDateTime ).FirstOrDefault();
            return archivedGroupMember;
        }

        /// <summary>
        /// Returns true if duplicate group members are allowed in groups
        /// Normally this is false, but there is a web.config option to allow it
        /// </summary>
        /// <returns></returns>
        public static bool AllowsDuplicateMembers()
        {
            var allowDuplicateGroupMembers = System.Configuration.ConfigurationManager.AppSettings["AllowDuplicateGroupMembers"].AsBoolean();
            return allowDuplicateGroupMembers;
        }

        /// <summary>
        /// Checks to see if there is an (unarchived) member of the group for the specified personId and groupRoleId
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <param name="groupMember">The group member.</param>
        /// <returns></returns>
        public bool ExistsAsMember( Group group, int personId, int groupRoleId, out GroupMember groupMember )
        {
            var groupMemberService = new GroupMemberService( this.Context as RockContext );
            groupMember = groupMemberService.AsNoFilter().Where( a => a.IsArchived == false && a.GroupId == group.Id && a.PersonId == personId && a.GroupRoleId == groupRoleId ).FirstOrDefault();
            return groupMember != null;
        }

        #region Group Copy Methods

        /// <summary>
        /// Copies the group (and optionally child groups) along with related entities:
        /// custom attributes, attribute values, qualifiers, auths, group requirements and group syncs.
        /// </summary>
        /// <param name="copyGroupOptions">The options to use for the copy operation.</param>
        /// <returns>The Id of the new group.</returns>
        public static int? CopyGroup( CopyGroupOptions copyGroupOptions )
        {
            if ( copyGroupOptions == null || copyGroupOptions.GroupId == 0 )
            {
                return null;
            }

            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );

            var group = groupService.Queryable()
                .Include( g => g.GroupType )
                .FirstOrDefault( g => g.Id == copyGroupOptions.GroupId );

            if ( group == null )
            {
                return null;
            }

            Group newGroup = null;

            // Clone the group and related entities inside a transaction.
            rockContext.WrapTransaction( () =>
            {
                // Track the original and new Guids to ensure Attribute references point to the new groups.
                Dictionary<Guid, Guid> groupGuidDictionary = new Dictionary<Guid, Guid>();
                newGroup = GenerateGroupCopy( rockContext, copyGroupOptions, groupGuidDictionary );

                GenerateGroupAttributeValues( groupGuidDictionary, rockContext, copyGroupOptions.CreatedByPersonAliasId );

                rockContext.SaveChanges();
            } );

            Rock.Security.Authorization.Clear();

            return newGroup?.Id;
        }

        /// <summary>
        ///     Generates a copy of the given group along with any custom attributes, attribute values, qualifiers,
        ///     locations (except member addresses), auths, group requirements and group syncs.
        ///     Child groups will be recursively copied when specified.
        /// </summary>
        /// <param name="rockContext"> The RockContext to be used for the copy operation.</param>
        /// <param name="copyGroupOptions">
        ///     The <see cref="Rock.Model.Groups.Group.Options.CopyGroupOptions"/> to use for the copy operation.
        /// </param>
        /// <param name="groupGuidDictionary">
        ///     The dictionary containing the original group guids and the corresponding copied group guids.
        ///     This provides the caller with a mapping of all cloned group Guids where
        ///     the key is the source/copied group and the value is the new target/new group.
        /// </param>
        /// <param name="parentGroupId">The parent group when a recursive call is made otherwise null for the root/source of the copy operation.</param>
        /// <returns>A new Group which is copy of the <see cref="Rock.Model.Group"/> specified by the GroupId in <seealso cref="Rock.Model.Groups.Group.Options.CopyGroupOptions"/></returns>
        private static Group GenerateGroupCopy( RockContext rockContext, CopyGroupOptions copyGroupOptions, Dictionary<Guid, Guid> groupGuidDictionary, int? parentGroupId = null )
        {
            var authService = new AuthService( rockContext );
            var groupService = new GroupService( rockContext );

            var sourceGroup = groupService.Get( copyGroupOptions.GroupId );
            sourceGroup.LoadAttributes( rockContext );

            var targetGroup = sourceGroup.CloneWithoutIdentity();
            targetGroup.CreatedByPersonAliasId = copyGroupOptions.CreatedByPersonAliasId;
            targetGroup.ModifiedByPersonAliasId = copyGroupOptions.CreatedByPersonAliasId;
            targetGroup.IsSystem = false;

            groupGuidDictionary.Add( sourceGroup.Guid, targetGroup.Guid );

            if ( parentGroupId == null )
            {
                // The root of the copy operation.
                targetGroup.Name = sourceGroup.Name + " - Copy";
            }
            else
            {
                // A child group whose parent group should reflect a copied group.
                targetGroup.ParentGroupId = parentGroupId;
            }

            // Copy any group locations and their schedules, but maintain the reference to the Location itself.
            // Note: Exclude members address locations ( GroupMemberPersonAliasId is not null ). 
            var copiableLocations = sourceGroup.GroupLocations.Where( l => l.GroupMemberPersonAliasId == null );
            foreach ( var groupLocation in copiableLocations )
            {
                var targetGroupLocation = groupLocation.CloneWithoutIdentity();

                foreach ( var locationSchedule in groupLocation.Schedules )
                {
                    targetGroupLocation.Schedules.Add( GetSchedule( locationSchedule ) );
                }

                targetGroup.GroupLocations.Add( targetGroupLocation );
            }

            if ( sourceGroup.ScheduleId.HasValue )
            {
                targetGroup.Schedule = GetSchedule( sourceGroup.Schedule );
            }

            // Need to add the Group and SaveContext here so the new GroupId can be populated for relating to other entities.
            groupService.Add( targetGroup );
            rockContext.SaveChanges();

            targetGroup.LoadAttributes( rockContext );

            Rock.Attribute.Helper.CopyAttributes( sourceGroup, targetGroup, rockContext );
            CopyGroupMemberAttributesAndQualifiers( rockContext, sourceGroup, targetGroup );

            // Set the attribute.EntityId to the new group Id.
            foreach ( var targetAttributeValue in targetGroup?.AttributeValues )
            {
                if ( targetAttributeValue.Value == null )
                {
                    // Skip empty values.
                    continue;
                }

                targetAttributeValue.Value.EntityId = targetGroup.Id;
            }

            targetGroup.SaveAttributeValues( rockContext );

            var groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;

            // Get all auths either for the GroupId (where this group is allowed to access another entity)
            // or the EntityId and EntityTypeId (where rules exist to access this group).
            // NOTE: We're copying auths where the old group was used as the role for access to another entity to
            // maintain existing the behavior. We've added behavior to copy the auths for actions on this group.
            var auths = authService.Queryable().Where(
                a => a.GroupId == copyGroupOptions.GroupId ||
                ( a.EntityId == copyGroupOptions.GroupId && a.EntityTypeId == groupEntityTypeId )
            ).OrderBy( a => a.Order );

            // Copy Auths (replacing Ids where applicable).
            foreach ( var auth in auths )
            {
                var newAuth = auth.CloneWithoutIdentity();

                // Auths for actions on this group.
                if ( newAuth.EntityTypeId == groupEntityTypeId && newAuth.EntityId == copyGroupOptions.GroupId )
                {
                    newAuth.EntityId = targetGroup.Id;
                }

                // Auths where this group is used to allow/deny access.
                if ( newAuth.GroupId == copyGroupOptions.GroupId )
                {
                    newAuth.GroupId = targetGroup.Id;
                }

                authService.Add( newAuth );
            }

            // Copy group sync settings.
            foreach ( var sourceSyncSetting in sourceGroup.GroupSyncs )
            {
                var targetSyncSetting = sourceSyncSetting.CloneWithoutIdentity();
                targetGroup.GroupSyncs.Add( targetSyncSetting );
            }

            // Copy the group-specific requirements.
            foreach ( var groupRequirement in sourceGroup.GroupRequirements )
            {
                GroupRequirement newGroupRequirement = groupRequirement.CloneWithoutIdentity();
                targetGroup.GroupRequirements.Add( newGroupRequirement );
            }

            if ( copyGroupOptions.IncludeChildGroups )
            {
                foreach ( var childGroup in sourceGroup.Groups )
                {
                    var copyChildGroupOptions = new CopyGroupOptions
                    {
                        GroupId = childGroup.Id,
                        IncludeChildGroups = copyGroupOptions.IncludeChildGroups,
                        CreatedByPersonAliasId = copyGroupOptions.CreatedByPersonAliasId
                    };

                    targetGroup.Groups.Add( GenerateGroupCopy( rockContext, copyChildGroupOptions, groupGuidDictionary, targetGroup.Id ) );
                }
            }

            return targetGroup;
        }

        /// <summary>
        /// Copies the group member attributes and qualifiers from one group to another.
        /// The qualifier value is set to the targetGroup.Id.
        /// </summary>
        /// <param name="rockContext">The rockContext to use for the operation.</param>
        /// <param name="sourceGroup">The group from which to copy.</param>
        /// <param name="targetGroup">The group to copy to.</param>
        /// <returns>A List of Attributes whose identifiers and qualifiers have been replaced with the target group identifier.</returns>
        private static void CopyGroupMemberAttributesAndQualifiers( RockContext rockContext, Group sourceGroup, Group targetGroup )
        {
            var attributeService = new AttributeService( rockContext );

            // Get the attributes for inherited and current group members.
            var sourceGroupMemberAttributes = attributeService.GetGroupMemberAttributesCombined( sourceGroup.Id, sourceGroup.GroupTypeId );

            foreach ( var attribute in sourceGroupMemberAttributes )
            {
                var newAttribute = attribute.Clone( false );
                newAttribute.Id = 0;
                newAttribute.Guid = Guid.NewGuid();
                newAttribute.IsSystem = false;
                newAttribute.EntityTypeQualifierValue = targetGroup.Id.ToString();

                foreach ( var qualifier in attribute.AttributeQualifiers )
                {
                    var newQualifier = qualifier.Clone( false );
                    newQualifier.Id = 0;
                    newQualifier.Guid = Guid.NewGuid();
                    newQualifier.IsSystem = false;

                    newAttribute.AttributeQualifiers.Add( newQualifier );
                }

                attributeService.Add( newAttribute );
            }
        }

        /// <summary>
        /// Creates copies of group attribute values for the specified source and target <seealso cref="Rock.Model.Group"/> Guids in the provided dictionary.
        /// </summary>
        /// <param name="groupGuidDictionary">The dictionary containing the original group guids and the corresponding copied group guids.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="currentPersonAliasId">The current person alias identifier.</param>
        private static void GenerateGroupAttributeValues( Dictionary<Guid, Guid> groupGuidDictionary, RockContext rockContext, int? currentPersonAliasId = null )
        {
            var attributeValueService = new AttributeValueService( rockContext );
            var groupService = new GroupService( rockContext );
            var groupEntityTypeGuid = Rock.SystemGuid.EntityType.GROUP.AsGuid();
            var groupFieldTypeGuid = Rock.SystemGuid.FieldType.GROUP.AsGuid();

            // Get the list of attribute values for the source instances.
            var attributeValues = attributeValueService.Queryable().Where( a =>
                a.Attribute.EntityType.Guid == groupEntityTypeGuid && groupGuidDictionary.Values.Contains( a.Guid ) )
                .ToList();

            foreach ( var attributeValue in attributeValues )
            {
                var attribute = AttributeCache.Get( attributeValue.AttributeId );
                var fieldType = attribute?.FieldType.Field as Field.FieldType;
                var currentValue = attributeValue;

                var newAttributeValue = attributeValue.CloneWithoutIdentity();
                newAttributeValue.CreatedByPersonAliasId = currentPersonAliasId;
                newAttributeValue.ModifiedByPersonAliasId = currentPersonAliasId;

                if ( fieldType != null && currentValue != null && currentValue.Value != null )
                {
                    newAttributeValue.Value = fieldType.GetCopyValue( currentValue.Value, rockContext );
                }

                if ( attributeValue.Attribute.FieldType.Guid == groupFieldTypeGuid )
                {
                    if ( groupGuidDictionary.ContainsKey( attributeValue.Value.AsGuid() ) )
                    {
                        newAttributeValue.Value = groupGuidDictionary[attributeValue.Value.AsGuid()].ToString();
                    }
                }

                attributeValueService.Add( newAttributeValue );
            }
        }

        /// <summary>
        /// Contains the logic for copying a schedule to a new group, location etc.
        /// </summary>
        /// <param name="sourceSchedule">The schedule to be copied.</param>
        /// <returns>
        ///     <para>The source schedule when <seealso cref="Rock.Model.ScheduleType.Named"/>.</para>
        ///     A new Schedule with matching iCalendarContent, WeeklyDayOfWeek and WeeklyTimeOfDay for other ScheduleTypes.
        /// </returns>
        private static Schedule GetSchedule( Schedule sourceSchedule )
        {
            // It should be safe to reference the same schedule when it's named.
            // This should merely relate the schedule instead of creating a new record.
            if ( sourceSchedule.ScheduleType == ScheduleType.Named )
            {
                return sourceSchedule;
            }

            var newSchedule = new Schedule();

            // Note: Schedule Name should be set to null to indicate that it is a Custom or Weekly schedule and not a "Named" schedule.
            newSchedule.Name = null;
            newSchedule.iCalendarContent = sourceSchedule.iCalendarContent;
            newSchedule.WeeklyDayOfWeek = sourceSchedule.WeeklyDayOfWeek;
            newSchedule.WeeklyTimeOfDay = sourceSchedule.WeeklyTimeOfDay;

            return newSchedule;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Deletes a Security Role Group.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        public static void DeleteSecurityRoleGroup( int groupId )
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                // Get the target group.
                var groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                if ( group == null )
                {
                    return;
                }

                // Verify that the group represents a Security Role.
                var isSecurityRoleGroup = group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );
                if ( !isSecurityRoleGroup )
                {
                    throw new Exception( $"Action DeleteSecurityRoleGroup failed. The specified group is not a Security Role. [GroupId={groupId}]" );
                }

                // Remove authorizations.
                // Using the BulkDelete method bypasses the Auth.SaveHook() to avoid creating new AuthAuditLog entries for the deleted Group.
                var authService = new AuthService( rockContext );
                var authsToDelete = authService.Queryable().Where( a => a.GroupId == groupId );
                if ( authsToDelete.Any() )
                {
                    rockContext.BulkDelete( authsToDelete );
                }

                // Remove authorization audit records.
                var authAuditLogService = new AuthAuditLogService( rockContext );
                var authAuditLogsToDelete = authAuditLogService.Queryable().Where( a => a.GroupId == groupId );
                if ( authAuditLogsToDelete.Any() )
                {
                    rockContext.BulkDelete( authAuditLogsToDelete );
                }

                // Clear the authorizations cache.
                Rock.Security.Authorization.Clear();

                // Remove the group.
                groupService.Delete( group );

                rockContext.SaveChanges();
            } );
        }

        #endregion
    }

    #region Extension Methods
    /// <summary>
    /// 
    /// </summary>
    public static class GroupServiceExtensions
    {
        /// <summary>
        /// Given an IQueryable of Groups, returns a queryable of just the heads of households for those groups
        /// </summary>
        /// <param name="groups">The groups.</param>
        /// <returns></returns>
        public static IQueryable<Person> HeadOfHouseholds( this IQueryable<Group> groups )
        {
            return groups
                .SelectMany( f => f.Members )
                .GroupBy( m =>
                    m.GroupId,
                    ( key, g ) => g
                        .OrderBy( m => m.GroupRole.Order )
                        .ThenBy( m => m.Person.Gender )
                        .ThenBy( m => m.Person.BirthYear )
                        .ThenBy( m => m.Person.BirthMonth )
                        .ThenBy( m => m.Person.BirthDay )
                        .FirstOrDefault() )
                .Select( m => m.Person );
        }

        /// <summary>
        /// Given an IQueryable of members (i.e. family members), returns the head of household for those members
        /// </summary>
        /// <param name="members">The members.</param>
        /// <returns></returns>
        public static Person HeadOfHousehold( this IQueryable<GroupMember> members )
        {
            return GetHeadOfHousehold<Person>( members, s => s.Person );
        }

        /// <summary>
        /// Given an IQueryable of members (i.e. family members), returns selected properties of the head of household for those members
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="members">The members.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static TResult GetHeadOfHousehold<TResult>( this IQueryable<GroupMember> members, System.Linq.Expressions.Expression<Func<GroupMember, TResult>> selector )
        {
            return members
                .OrderBy( m => m.GroupRole.Order )
                .ThenBy( m => m.Person.Gender )
                .ThenBy( m => m.Person.BirthYear )
                .ThenBy( m => m.Person.BirthMonth )
                .ThenBy( m => m.Person.BirthDay )
                .Select( selector )
                .FirstOrDefault();
        }

        /// <summary>
        /// For the group, gets a family member that matches the given person. A match
        /// is found if the nickname matches the nickname or first name, the last name matches, and,
        /// if there is a birth date on the potential match, the birth date matches.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="personToMatch">The person to match.</param>
        /// <returns>a person (if a match was found) otherwise null</returns>
        public static Person MatchingFamilyMember( this Group group, Person personToMatch )
        {
            return group.Members
            .Where( m =>
                ( m.Person.NickName == personToMatch.NickName || m.Person.FirstName == personToMatch.NickName ) &&
                m.Person.LastName == personToMatch.LastName &&
                m.Person.BirthDate.HasValue &&
                m.Person.BirthDate.Value == personToMatch.BirthDate.Value )
            .Select( m => m.Person )
            .FirstOrDefault();
        }

        /// <summary>
        /// Gets the active members.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public static IEnumerable<GroupMember> ActiveMembers( this Group group )
        {
            return group.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active );
        }

        /// <summary>
        /// Returns a queryable of Groups that have active leaders.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<Group> HasActiveLeader( this IQueryable<Group> groupQuery )
        {
            return groupQuery
                    .Where( g => g.Members.Any( m =>
                                    m.GroupMemberStatus == GroupMemberStatus.Active &&
                                    m.GroupRole.IsLeader ) );
        }

        /// <summary>
        /// Returns a queryable of groups that are Security role based on either <see cref="Group.IsSecurityRole" />
        /// or if <see cref="Group.GroupTypeId"/> is the Security Role Group Type.
        /// </summary>
        public static IQueryable<Group> IsSecurityRoleOrSecurityRoleGroupType( this IQueryable<Group> groupQuery )
        {
            var groupTypeIdSecurityRole = GroupTypeCache.GetSecurityRoleGroupType()?.Id ?? 0;
            return groupQuery.Where( g => g.IsSecurityRole || g.GroupTypeId == groupTypeIdSecurityRole );
        }

        /// <summary>
        /// Returns a queryable of groups that are not Security role based on either <see cref="Group.IsSecurityRole" />
        /// or if <see cref="Group.GroupTypeId"/> is the Security Role Group Type.
        /// </summary>
        public static IQueryable<Group> IsNotSecurityRoleOrSecurityRoleGroupType( this IQueryable<Group> groupQuery )
        {
            var groupTypeIdSecurityRole = GroupTypeCache.GetSecurityRoleGroupType()?.Id ?? 0;
            return groupQuery.Where( g => !g.IsSecurityRole && g.GroupTypeId != groupTypeIdSecurityRole );
        }

        /// <summary>
        /// Returns a queryable of Groups with the specified Group Type Id.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <returns></returns>
        public static IQueryable<Group> IsGroupType( this IQueryable<Group> groupQuery, int groupTypeId )
        {
            return groupQuery
                    .Where( g => g.GroupTypeId == groupTypeId );
        }

        /// <summary>
        /// Returns a queryable of Groups that are active.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<Group> IsActive( this IQueryable<Group> groupQuery )
        {
            return groupQuery
                    .Where( g => g.IsActive );
        }

        /// <summary>
        /// Returns a queryable of Groups that have a Schedule Id.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<Group> HasSchedule( this IQueryable<Group> groupQuery )
        {
            return groupQuery
                    .Where( g => g.ScheduleId != null );
        }

        /// <summary>
        /// Returns a queryable of Groups have that Group Scheduling Enabled
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<Group> HasSchedulingEnabled( this IQueryable<Group> groupQuery )
        {
            return groupQuery
                    .Where( g => g.GroupType.IsSchedulingEnabled && g.DisableScheduling == false );
        }

        /// <summary>
        /// Returns a queryable of group scheduling <see cref="Schedule">Schedules</see> associated with group scheduling for the specified groupQuery.
        /// Only schedules for groups that have group scheduling enabled, and have active group locations will be returned.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<Schedule> GetGroupSchedulingSchedules( this IQueryable<Group> groupQuery )
        {
            var groupsWithSchedulingEnabledQuery = groupQuery.HasSchedulingEnabled();

            var groupLocationsQuery = groupsWithSchedulingEnabledQuery.SelectMany( a => a.GroupLocations );

            var schedulesQuery = groupLocationsQuery
                .Where( gl => gl.Location.IsActive )
                .SelectMany( gl => gl.Schedules )
                .Distinct()
                .Where( s => s.IsActive );

            return schedulesQuery;
        }

        /// <summary>
        /// Returns a queryable of group scheduling <see cref="GroupLocation">group locations</see> associated with group scheduling for the specified groupQuery.
        /// Only group locations for groups that have group scheduling enabled, and have active group locations will be returned.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        /// <returns></returns>
        public static IQueryable<GroupLocation> GetGroupSchedulingGroupLocations( this IQueryable<Group> groupQuery )
        {
            var groupsWithSchedulingEnabledQuery = groupQuery.HasSchedulingEnabled();
            var groupLocationsQuery = groupsWithSchedulingEnabledQuery.SelectMany( a => a.GroupLocations );

            groupLocationsQuery = groupLocationsQuery
                .Where( a => groupQuery.Any( x => x.Id == a.GroupId ) )
                .Where( a => a.Group.GroupType.IsSchedulingEnabled == true && a.Group.DisableScheduling == false )
                .Where( gl => gl.Location.IsActive )
                .Distinct();

            return groupLocationsQuery;
        }

        /// <summary>
        /// Returns a queryable of Groups with the Communication List Group Type Id.
        /// </summary>
        /// <param name="groupQuery">The group query.</param>
        public static IQueryable<Group> IsCommunicationList( this IQueryable<Group> groupQuery )
        {
            var groupTypeId = GroupTypeCache.GetId( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ) ?? -1;
            return IsGroupType( groupQuery, groupTypeId );
        }
    }

    #endregion
}
