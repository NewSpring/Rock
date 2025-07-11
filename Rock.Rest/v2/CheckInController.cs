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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Rock.CheckIn.v2;
using Rock.CheckIn.v2.Labels;
using Rock.Data;
using Rock.Rest.Filters;
using Rock.Utility;
using Rock.ViewModels.Rest.CheckIn;
using Rock.Web.Cache;
using Rock.ViewModels.CheckIn.Labels;
using Rock.Security;

#if WEBFORMS
using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
using FromQueryAttribute = System.Web.Http.FromUriAttribute;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using HttpDeleteAttribute = System.Web.Http.HttpDeleteAttribute;
using IActionResult = System.Web.Http.IHttpActionResult;
using RouteAttribute = System.Web.Http.RouteAttribute;
using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;
#endif

namespace Rock.Rest.v2
{
    /// <summary>
    /// Provides API interfaces for the Check-in system in Rock.
    /// </summary>
    /// <seealso cref="Rock.Rest.ApiControllerBase" />
    [RoutePrefix( "api/v2/checkin" )]
    [Rock.SystemGuid.RestControllerGuid( "52b3c68a-da8d-4374-a199-8bc8368a22bc" )]
    public sealed class CheckInController : ApiControllerBase
    {
        /// <summary>
        /// The database context to use for this request.
        /// </summary>
        private readonly RockContext _rockContext;

        /// <summary>
        /// The logger to use when writing messages.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInController"/> class.
        /// </summary>
        /// <param name="rockContext">The database context to use for this request.</param>
        /// <param name="logger">The logger to use when writing messages.</param>
        public CheckInController( RockContext rockContext, ILogger<CheckInController> logger )
        {
            _rockContext = rockContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets the configuration items available to be selected.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the configuration items.</returns>
        [HttpPost]
        [Route( "Configuration" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ConfigurationResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "200dd82f-6532-4437-9ba4-a289408b0eb8" )]
        public IActionResult PostConfiguration( [FromBody] ConfigurationOptionsBag options )
        {
            var helper = new CheckInDirector( _rockContext );
            DeviceCache kiosk = null;

            if ( options.KioskId.IsNotNullOrWhiteSpace() )
            {
                kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            try
            {
                return Ok( new ConfigurationResponseBag
                {
                    Templates = helper.GetConfigurationTemplateBags(),
                    Areas = helper.GetCheckInAreaSummaries( kiosk, null )
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Gets current status that the check-in kiosk should be in as well
        /// as when it should open or close.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains the status.</returns>
        [HttpPost]
        [Route( "KioskStatus" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( KioskStatusResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "7fb87711-1ecf-49ca-90cb-3e2e1b02a933" )]
        public IActionResult PostKioskStatus( [FromBody] KioskStatusOptionsBag options )
        {
            var director = new CheckInDirector( _rockContext );
            var kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );
            List<GroupTypeCache> areas = null;

            if ( options.AreaIds != null )
            {
                var areaIdNumbers = options.AreaIds
                    .Select( id => IdHasher.Instance.GetId( id ) )
                    .Where( id => id.HasValue )
                    .Select( id => id.Value )
                    .ToList();

                areas = GroupTypeCache.GetMany( areaIdNumbers, _rockContext ).ToList();
            }

            if ( kiosk == null )
            {
                return BadRequest( "Kiosk was not found." );
            }

            if ( areas == null )
            {
                return BadRequest( "Area list cannot be null." );
            }

            return Ok( new KioskStatusResponseBag
            {
                Status = director.GetKioskStatus( areas, kiosk, null )
            } );
        }

        /// <summary>
        /// Performs a search for matching families that are valid for check-in.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the matched families.</returns>
        [HttpPost]
        [Route( "SearchForFamilies" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( SearchForFamiliesResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "2c587733-0e08-4e93-8f2b-3e2518362768" )]
        public IActionResult PostSearchForFamilies( [FromBody] SearchForFamiliesOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.ConfigurationTemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            CampusCache sortByCampus = null;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( options.KioskId.IsNotNullOrWhiteSpace() && options.PrioritizeKioskCampus )
            {
                var kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }

                var campusId = kiosk.GetCampusId();

                if ( campusId.HasValue )
                {
                    sortByCampus = CampusCache.Get( campusId.Value, _rockContext );
                }
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );
                var families = session.SearchForFamilies( options.SearchTerm,
                    options.SearchType,
                    sortByCampus );

                return Ok( new SearchForFamiliesResponseBag
                {
                    Families = families
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Performs a search for matching families that are valid for check-in.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the matched families.</returns>
        [HttpPost]
        [Route( "FamilyMembers" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( FamilyMembersResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "2bd5afdf-da57-48bb-a6db-7dd9ad1ab8da" )]
        public IActionResult PostFamilyMembers( [FromBody] FamilyMembersOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.ConfigurationTemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            var kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );
            var areas = options.AreaIds.Select( id => GroupTypeCache.GetByIdKey( id, _rockContext ) ).ToList();

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( kiosk == null )
            {
                return BadRequest( "Kiosk was not found." );
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );

                if ( options.OverridePinCode.IsNotNullOrWhiteSpace() )
                {
                    if ( director.TryAuthenticatePin( options.OverridePinCode, out var errorMessage ) )
                    {
                        session.IsOverrideEnabled = true;
                    }
                    else
                    {
                        return BadRequest( errorMessage );
                    }
                }

                session.LoadAndPrepareAttendeesForFamily( options.FamilyId, areas, kiosk, null );

                return Ok( new FamilyMembersResponseBag
                {
                    FamilyId = options.FamilyId,
                    PossibleSchedules = session.GetAllPossibleScheduleBags(),
                    People = session.GetAttendeeBags(),
                    CurrentlyCheckedInAttendances = session.GetCurrentAttendanceBags( areas, kiosk, null )
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Gets the available check-in opportunities for a single attendee.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the opportunities.</returns>
        [HttpPost]
        [Route( "AttendeeOpportunities" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( AttendeeOpportunitiesResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "6e77e23d-cccb-46b7-a8e9-95706bbb269a" )]
        public IActionResult PostAttendeeOpportunities( [FromBody] AttendeeOpportunitiesOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.ConfigurationTemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            var areas = options.AreaIds.Select( id => GroupTypeCache.GetByIdKey( id, _rockContext ) ).ToList();
            var kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( kiosk == null )
            {
                return BadRequest( "Kiosk was not found." );
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );

                if ( options.OverridePinCode.IsNotNullOrWhiteSpace() )
                {
                    if ( director.TryAuthenticatePin( options.OverridePinCode, out var errorMessage ) )
                    {
                        session.IsOverrideEnabled = true;
                    }
                    else
                    {
                        return BadRequest( errorMessage );
                    }
                }

                session.LoadAndPrepareAttendeesForPerson( options.PersonId, options.FamilyId, areas, kiosk, null );

                if ( session.Attendees.Count == 0 )
                {
                    return BadRequest( "Individual was not found or is not available for check-in." );
                }

                return Ok( new AttendeeOpportunitiesResponseBag
                {
                    Opportunities = session.GetOpportunityCollectionBag( session.Attendees[0].Opportunities )
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Saves the attendance for the specified requests.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>The results from the save operation.</returns>
        [HttpPost]
        [Route( "SaveAttendance" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( SaveAttendanceResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "7ef059cb-99ba-4cf1-b7d5-3723eb320a99" )]
        public async Task<IActionResult> PostSaveAttendance( [FromBody] SaveAttendanceOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.TemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            DeviceCache kiosk = null;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( options.KioskId.IsNotNullOrWhiteSpace() )
            {
                kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );
                var sessionRequest = new AttendanceSessionRequest( options.Session );
                List<ClientLabelBag> clientLabelBags = null;

                var result = session.SaveAttendance( sessionRequest, options.Requests, kiosk, RockRequestContext.ClientInformation.IpAddress );

                if ( !options.Session.IsPending )
                {
                    var cts = new CancellationTokenSource( 5000 );
                    var clientLabels = await director.LabelProvider.RenderAndPrintCheckInLabelsAsync( result, kiosk, new LabelPrintProvider(), cts.Token );

                    clientLabelBags = clientLabels
                        .Where( l => l.Data != null && l.Error.IsNullOrWhiteSpace() )
                        .Select( l => new ClientLabelBag
                        {
                            PrinterAddress = l.PrintTo?.IPAddress,
                            Data = Convert.ToBase64String( l.Data )
                        } )
                        .ToList();
                }

                return Ok( new SaveAttendanceResponseBag
                {
                    Messages = result.Messages,
                    Attendances = result.Attendances,
                    Labels = clientLabelBags
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Confirms the pending attendance records for a session.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>The results from the confirm operation.</returns>
        [HttpPost]
        [Route( "ConfirmAttendance" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ConfirmAttendanceResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "52070226-289b-442d-a8fe-a8323c0f922c" )]
        public async Task<IActionResult> PostConfirmAttendance( [FromBody] ConfirmAttendanceOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.TemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            DeviceCache kiosk = null;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( options.KioskId.IsNotNullOrWhiteSpace() )
            {
                kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );

                var result = session.ConfirmAttendance( options.SessionGuid );

                var cts = new CancellationTokenSource( 5000 );
                var clientLabels = await director.LabelProvider.RenderAndPrintCheckInLabelsAsync( result, kiosk, new LabelPrintProvider(), cts.Token );

                var clientLabelBags = clientLabels
                    .Where( l => l.Data != null && l.Error.IsNullOrWhiteSpace() )
                    .Select( l => new ClientLabelBag
                    {
                        PrinterAddress = l.PrintTo?.IPAddress,
                        Data = Convert.ToBase64String( l.Data )
                    } )
                    .ToList();

                return Ok( new ConfirmAttendanceResponseBag
                {
                    Messages = result.Messages,
                    Attendances = result.Attendances,
                    Labels = clientLabelBags
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Saves the attendance for the specified requests.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>The results from the save operation.</returns>
        [HttpPost]
        [Route( "Checkout" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CheckoutResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "733be2ee-dec6-4f7f-92bd-df367c20543d" )]
        public async Task<IActionResult> PostCheckout( [FromBody] CheckoutOptionsBag options )
        {
            var configuration = GroupTypeCache.GetByIdKey( options.TemplateId, _rockContext )?.GetCheckInConfiguration( _rockContext );
            DeviceCache kiosk = null;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( options.KioskId.IsNotNullOrWhiteSpace() )
            {
                kiosk = DeviceCache.GetByIdKey( options.KioskId, _rockContext );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            try
            {
                var director = new CheckInDirector( _rockContext );
                var session = director.CreateSession( configuration );
                var sessionRequest = new AttendanceSessionRequest( options.Session );

                var result = session.Checkout( sessionRequest, options.AttendanceIds, kiosk );

                var cts = new CancellationTokenSource( 5000 );
                await director.LabelProvider.RenderAndPrintCheckoutLabelsAsync( result, kiosk, new LabelPrintProvider(), cts.Token );

                return Ok( result );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Deletes the pending attendance records for a session.
        /// </summary>
        /// <param name="sessionGuid">The unique identifier of the session to delete attendance records for.</param>
        /// <returns>The results from the delete operation.</returns>
        [HttpDelete]
        [Route( "PendingAttendance/{sessionGuid}" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_WRITE )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "f914ffc3-8587-493b-9c8a-ae196b5fe028" )]
        public IActionResult DeletePendingAttendance( Guid sessionGuid )
        {
            try
            {
                var director = new CheckInDirector( _rockContext );

                director.DeletePendingAttendance( sessionGuid );

                return Ok();
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Establishes a connection from the printer proxy service to this
        /// Rock instance.
        /// </summary>
        /// <param name="deviceId">The identifier of the proxy Device in Rock as either a Guid or an IdKey.</param>
        /// <param name="name">The name of the proxy for UI presentation.</param>
        /// <returns>The result of the operation.</returns>
        [HttpGet]
        [Route( "CloudPrint/{deviceId}" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.SwitchingProtocols )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [SystemGuid.RestActionGuid( "1b4b1d0d-a872-40f7-a49d-666092cf8816" )]
        public IActionResult GetPrinterProxy( string deviceId, [FromQuery] string name = null )
        {
            if ( !System.Web.HttpContext.Current.IsWebSocketRequest )
            {
                return BadRequest( "This API may only be used with websocket connections." );
            }

            DeviceCache device = null;

            if ( IdHasher.Instance.TryGetId( deviceId, out var deviceIdNumber ) )
            {
                device = DeviceCache.Get( deviceIdNumber, _rockContext );
            }
            else if ( Guid.TryParse( deviceId, out var deviceGuid ) )
            {
                device = DeviceCache.Get( deviceGuid, _rockContext );
            }

            if ( device == null )
            {
                return BadRequest( "Device not found." );
            }

            System.Web.HttpContext.Current.AcceptWebSocketRequest( ctx =>
            {
                var address = RockRequestContext.ClientInformation.IpAddress;
                var proxy = new CloudPrintSocket( ctx.WebSocket, device.Id, name ?? device.Name, address );

                return proxy.RunAsync( CancellationToken.None );
            } );

            return ResponseMessage( Request.CreateResponse( HttpStatusCode.SwitchingProtocols ) );
        }

        /// <summary>
        /// Notifies server that an individual has entered or left the range
        /// of one or more proximity beacons.
        /// </summary>
        /// <param name="proximity">The data that describes the detected beacons.</param>
        /// <returns>The result of the operation.</returns>
        [HttpPost]
        [Route( "ProximityCheckIn" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [SystemGuid.RestActionGuid( "2e0e2704-8730-4949-b726-05401930b0e0" )]
        public IActionResult PostProximityCheckIn( [FromBody] ProximityCheckInOptionsBag proximity )
        {
            if ( RockRequestContext.CurrentPerson == null )
            {
                return Unauthorized();
            }

            var beacon = proximity?.Beacons?.FirstOrDefault();

            if ( beacon == null )
            {
                return BadRequest( "No beacons were detected." );
            }

            if ( _logger.IsEnabled( LogLevel.Information ) )
            {
                var beacons = ( proximity.Beacons ?? new List<ProximityBeaconBag>() )
                    .Select( b => $"{{Major={b.Major}, Minor={b.Minor}, Rssi={b.Rssi}, Accuracy={b.Accuracy}}}" );

                _logger.LogInformation( "ProximityCheckin Uuid={uuid}, Present={present}, PersonalDeviceGuid={personalDeviceGuid}, Beacons=[{beacons:l}]",
                    proximity.ProximityGuid,
                    proximity.IsPresent,
                    proximity.PersonalDeviceGuid,
                    string.Join( ", ", beacons ) );
            }

            var proximityDirector = new ProximityDirector( _rockContext );

            if ( proximity.IsPresent )
            {
                if ( !proximityDirector.CheckIn( RockRequestContext.CurrentPerson, beacon ) )
                {
                    return BadRequest( "No location was available for check-in." );
                }
            }
            else
            {
                if ( !proximityDirector.Checkout( RockRequestContext.CurrentPerson, beacon ) )
                {
                    return BadRequest( "No location was available for checkout." );
                }
            }

            return NoContent();
        }
    }
}
