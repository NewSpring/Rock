//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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
using System.Net;

using Microsoft.AspNetCore.Mvc;

using Rock.Rest.Filters;
using Rock.Security;
using Rock.ViewModels.Core;
using Rock.ViewModels.Rest.Models;

namespace Rock.Rest.v2.Models
{
#if WEBFORMS
    using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
    using IActionResult = System.Web.Http.IHttpActionResult;
    using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;
    using RouteAttribute = System.Web.Http.RouteAttribute;
    using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
    using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
    using HttpPutAttribute = System.Web.Http.HttpPutAttribute;
    using HttpPatchAttribute = System.Web.Http.HttpPatchAttribute;
    using HttpDeleteAttribute = System.Web.Http.HttpDeleteAttribute;
#endif

    /// <summary>
    /// Provides data API endpoints for Analytics Source Campuses.
    /// </summary>
    [RoutePrefix( "api/v2/models/analyticssourcecampuses" )]
    [Rock.SystemGuid.RestControllerGuid( "a60c2f06-ecb5-583a-a836-46a2e8f3f29c" )]
    public partial class AnalyticsSourceCampusesController : ApiControllerBase
    {
        /// <summary>
        /// Gets a single item from the database.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <returns>The requested item.</returns>
        [HttpGet]
        [Route( "{id}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Rock.Model.AnalyticsSourceCampus ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "79c612a7-077b-5c32-9bb6-6ed3f1056e7f" )]
        public IActionResult GetItem( string id )
        {
            var helper = new CrudEndpointHelper<Rock.Model.AnalyticsSourceCampus, Rock.Model.AnalyticsSourceCampusService>( this );

            helper.IsSecurityIgnored = true;

            return helper.Get( id );
        }

        /// <summary>
        /// Gets all the attribute values for the specified item.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <returns>An array of objects that represent all the attribute values.</returns>
        [HttpGet]
        [Route( "{id}/attributevalues" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Dictionary<string, ModelAttributeValueBag> ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "7d095d0b-98ab-5213-874b-a00fa52939a2" )]
        public IActionResult GetAttributeValues( string id )
        {
            var helper = new CrudEndpointHelper<Rock.Model.AnalyticsSourceCampus, Rock.Model.AnalyticsSourceCampusService>( this );

            helper.IsSecurityIgnored = true;

            return helper.GetAttributeValues( id );
        }

        /// <summary>
        /// Performs a search of items using the specified user query.
        /// </summary>
        /// <param name="query">Query options to be applied.</param>
        /// <returns>An array of objects returned by the query.</returns>
        [HttpPost]
        [Route( "search" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( object ) )]
        [SystemGuid.RestActionGuid( "0e2eec62-52c5-554a-9ee1-6efd095c6ffa" )]
        public IActionResult PostSearch( [FromBody] EntitySearchQueryBag query )
        {
            var helper = new CrudEndpointHelper<Rock.Model.AnalyticsSourceCampus, Rock.Model.AnalyticsSourceCampusService>( this );

            return helper.Search( query );
        }

        /// <summary>
        /// Performs a search of items using the specified system query.
        /// </summary>
        /// <param name="searchKey">The key that identifies the entity search query to execute.</param>
        /// <returns>An array of objects returned by the query.</returns>
        [HttpGet]
        [Route( "search/{searchKey}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( object ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "5fdf9e0c-8d6a-5b85-8ec7-b3093c2c682f" )]
        public IActionResult GetSearchByKey( string searchKey )
        {
            var helper = new CrudEndpointHelper<Rock.Model.AnalyticsSourceCampus, Rock.Model.AnalyticsSourceCampusService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            return helper.Search( searchKey, null );
        }

        /// <summary>
        /// Performs a search of items using the specified system query.
        /// </summary>
        /// <param name="query">Additional query refinement options to be applied.</param>
        /// <param name="searchKey">The key that identifies the entity search query to execute.</param>
        /// <returns>An array of objects returned by the query.</returns>
        [HttpPost]
        [Route( "search/{searchKey}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( object ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "f7cfa5e1-847e-5b29-93f6-a17209b6b14a" )]
        public IActionResult PostSearchByKey( string searchKey, [FromBody] EntitySearchQueryBag query )
        {
            var helper = new CrudEndpointHelper<Rock.Model.AnalyticsSourceCampus, Rock.Model.AnalyticsSourceCampusService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            return helper.Search( searchKey, query );
        }
    }
}
