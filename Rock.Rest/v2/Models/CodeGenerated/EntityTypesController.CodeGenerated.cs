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
    /// Provides data API endpoints for Entity Types.
    /// </summary>
    [RoutePrefix( "api/v2/models/entitytypes" )]
    [Rock.SystemGuid.RestControllerGuid( "b38d4334-22a0-54ed-9020-8c322309ca1c" )]
    public partial class EntityTypesController : ApiControllerBase
    {
        /// <summary>
        /// Gets a single item from the database.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <returns>The requested item.</returns>
        [HttpGet]
        [Route( "{id}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Rock.Model.EntityType ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "b5c8926f-6712-569a-a6d2-ab27cfb168a2" )]
        public IActionResult GetItem( string id )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            return helper.Get( id );
        }

        /// <summary>
        /// Creates a new item in the database.
        /// </summary>
        /// <param name="value">The item to be created.</param>
        /// <returns>An object that contains the new identifier values.</returns>
        [HttpPost]
        [Route( "" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ProducesResponseType( HttpStatusCode.Created, Type = typeof( CreatedAtResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "7177e546-fe8c-5433-9fd3-bfe882661fa6" )]
        public IActionResult PostItem( [FromBody] Rock.Model.EntityType value )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_WRITE );

            return helper.Create( value );
        }

        /// <summary>
        /// Performs a full update of the item. All property values must be
        /// specified.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <param name="value">The item that represents all the new values.</param>
        /// <returns>An empty response.</returns>
        [HttpPut]
        [Route( "{id}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "404adb8e-5de0-5bdf-95ea-8b9c8332994c" )]
        public IActionResult PutItem( string id, [FromBody] Rock.Model.EntityType value )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_WRITE );

            return helper.Update( id, value );
        }

        /// <summary>
        /// Performs a partial update of the item. Only specified property keys
        /// will be updated.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <param name="values">An object that identifies the properties and values to be updated.</param>
        /// <returns>An empty response.</returns>
        [HttpPatch]
        [Route( "{id}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "ce65e739-8a1c-5b09-b418-6f86aa39cbc0" )]
        public IActionResult PatchItem( string id, [FromBody] Dictionary<string, object> values )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_WRITE );

            return helper.Patch( id, values );
        }

        /// <summary>
        /// Deletes a single item from the database.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <returns>An empty response.</returns>
        [HttpDelete]
        [Route( "{id}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "d5e96f37-776c-5068-a2e2-f1ffd1d8b855" )]
        public IActionResult DeleteItem( string id )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_WRITE );

            return helper.Delete( id );
        }

        /// <summary>
        /// Gets all the attribute values for the specified item.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <returns>An array of objects that represent all the attribute values.</returns>
        [HttpGet]
        [Route( "{id}/attributevalues" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Dictionary<string, ModelAttributeValueBag> ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "1454621f-bf20-5d56-83b5-0dc3465737d5" )]
        public IActionResult GetAttributeValues( string id )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            return helper.GetAttributeValues( id );
        }

        /// <summary>
        /// Performs a partial update of attribute values for the item. Only
        /// attributes specified will be updated.
        /// </summary>
        /// <param name="id">The identifier as either an Id, Guid or IdKey value.</param>
        /// <param name="values">An object that identifies the attribute keys and raw values to be updated.</param>
        /// <returns>An empty response.</returns>
        [HttpPatch]
        [Route( "{id}/attributevalues" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "fe1e45a7-f699-550c-8e23-1a76d8147351" )]
        public IActionResult PatchAttributeValues( string id, [FromBody] Dictionary<string, string> values )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_WRITE );

            return helper.PatchAttributeValues( id, values );
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
        [SystemGuid.RestActionGuid( "9b229451-4ca8-5fc9-bfad-4e5b06d6e8ff" )]
        public IActionResult PostSearch( [FromBody] EntitySearchQueryBag query )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

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
        [SystemGuid.RestActionGuid( "449eb7ca-8a88-5ed3-9fc0-fd20cce71d9c" )]
        public IActionResult GetSearchByKey( string searchKey )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

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
        [SystemGuid.RestActionGuid( "64a09928-b1b1-5430-be82-8a4d1ffd3e17" )]
        public IActionResult PostSearchByKey( string searchKey, [FromBody] EntitySearchQueryBag query )
        {
            var helper = new CrudEndpointHelper<Rock.Model.EntityType, Rock.Model.EntityTypeService>( this );

            helper.IsSecurityIgnored = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            return helper.Search( searchKey, query );
        }
    }
}
