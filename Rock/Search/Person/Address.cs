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
using System.ComponentModel.Composition;
using System.Linq;

using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Search.Person
{
    /// <summary>
    /// Searches for people with matching address
    /// </summary>
    [Description( "Person Address Search" )]
    [Export(typeof(SearchComponent))]
    [ExportMetadata("ComponentName", "Person Address")]
    [Rock.SystemGuid.EntityTypeGuid( "C2A24344-014E-4A45-BC38-08DDBD9521C3")]
    public class Address : SearchComponent
    {
        /// <summary>
        /// Gets the attribute value defaults.
        /// </summary>
        /// <value>
        /// The attribute defaults.
        /// </value>
        public override Dictionary<string, string> AttributeValueDefaults
        {
            get
            {
                var defaults = new Dictionary<string, string>();
                defaults.Add( "SearchLabel", "Address" );
                return defaults;
            }
        }

        /// <inheritdoc/>
        public override IOrderedQueryable<object> SearchQuery( string searchTerm )
        {
            if ( searchTerm.IsSingleSpecialCharacter() )
            {
                return Enumerable.Empty<object>().AsQueryable().OrderBy( _ => true );
            }

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );

            var personIdQry = groupMemberService.GetPersonIdsByHomeAddress( searchTerm );

            return personService.Queryable()
                .Where( p => personIdQry.Contains( p.Id ) )
                .OrderBy( p => p.NickName )
                .ThenBy( p => p.LastName );
        }

        /// <summary>
        /// Returns a list of matching people
        /// </summary>
        /// <param name="searchterm"></param>
        /// <returns></returns>
        public override IQueryable<string> Search( string searchterm )
        {
            if ( searchterm.IsSingleSpecialCharacter() )
            {
                return Enumerable.Empty<string>().AsQueryable();
            }

            var rockContext = new RockContext();

            Guid groupTypefamilyGuid = new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            Guid homeAddressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
            var homeAddressTypeValueId = DefinedValueCache.Get( homeAddressTypeGuid, rockContext ).Id;

            var service = new GroupMemberService( rockContext );
            return service.Queryable()
                .Where( m => m.Group.GroupType.Guid == groupTypefamilyGuid )
                .SelectMany( g => g.Group.GroupLocations )
                .Where( gl => gl.GroupLocationTypeValueId == homeAddressTypeValueId &&
                    gl.Location.Street1.Contains( searchterm ) )
                .Select( gl => gl.Location.Street1 )
                .Distinct();
        }
    }
}
