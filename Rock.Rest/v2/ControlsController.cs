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
using System.Collections.Specialized;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;

using Rock.Attribute;
using Rock.Badge;
using Rock.ClientService.Core.Category;
using Rock.ClientService.Core.Category.Options;
using Rock.Communication;
using Rock.Configuration;
using Rock.Data;
using Rock.Enums.Controls;
using Rock.Extension;
using Rock.Field;
using Rock.Field.Types;
using Rock.Financial;
using Rock.Lava;
using Rock.Media;
using Rock.Model;
using Rock.Rest.Controllers;
using Rock.Rest.Filters;
using Rock.Security;
using Rock.Security.SecurityGrantRules;
using Rock.Storage;
using Rock.Storage.AssetStorage;
using Rock.Utility;
using Rock.Utility.CaptchaApi;
using Rock.ViewModels.Controls;
using Rock.ViewModels.Crm;
using Rock.ViewModels.Rest.Controls;
using Rock.ViewModels.Utility;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.Cache.Entities;
using Rock.Web.UI.Controls;
using Rock.Workflow;

using Authorization = Rock.Security.Authorization;

#if WEBFORMS
using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
using FromUriAttribute = System.Web.Http.FromUriAttribute;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using IActionResult = System.Web.Http.IHttpActionResult;
using RouteAttribute = System.Web.Http.RouteAttribute;
using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;
#endif

namespace Rock.Rest.v2
{
    /// <summary>
    /// Provides API endpoints for the Controls controller.
    /// </summary>
    [RoutePrefix( "api/v2/controls" )]
    [Rock.SystemGuid.RestControllerGuid( "815B51F0-B552-47FD-8915-C653EEDD5B67" )]
    public class ControlsController : ApiControllerBase
    {
        #region Account Picker

        /// <summary>
        /// Gets the accounts that can be displayed in the account picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the accounts.</returns>
        [HttpPost]
        [Route( "AccountPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "5052e4a9-8cc3-4937-a2d3-9cfec07ed070" )]
        public IActionResult AccountPickerGetChildren( [FromBody] AccountPickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                return Ok( AccountPickerGetChildrenData( options, rockContext ) );
            }
        }

        /// <summary>
        /// Gets the accounts that can be displayed in the account picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <param name="rockContext">DB context.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the accounts.</returns>
        private List<TreeItemBag> AccountPickerGetChildrenData( AccountPickerGetChildrenOptionsBag options, RockContext rockContext )
        {
            var financialAccountService = new FinancialAccountService( rockContext );

            IQueryable<FinancialAccount> qry;

            if ( options.ParentGuid == Guid.Empty )
            {
                qry = financialAccountService.Queryable().AsNoTracking()
                    .Where( f => f.ParentAccountId.HasValue == false );
            }
            else
            {
                qry = financialAccountService.Queryable().AsNoTracking()
                    .Where( f => f.ParentAccount != null && f.ParentAccount.Guid == options.ParentGuid );
            }

            if ( !options.IncludeInactive )
            {
                qry = qry
                    .Where( f => f.IsActive == true );
            }

            var accountList = qry
                .OrderBy( f => f.Order )
                .ThenBy( f => f.Name )
                .ToList();

            var accountTreeViewItems = accountList
                .Select( a => new TreeItemBag
                {
                    Value = a.Guid.ToString(),
                    Text = options.DisplayPublicName ? a.PublicName : a.Name,
                    IsActive = a.IsActive,
                    IconCssClass = "fa fa-file-o"
                } ).ToList();

            var resultIds = accountList.Select( f => f.Id ).ToList();

            if ( options.LoadFullTree )
            {
                foreach ( var accountTreeViewItem in accountTreeViewItems )
                {
                    var newOptions = new AccountPickerGetChildrenOptionsBag
                    {
                        DisplayPublicName = options.DisplayPublicName,
                        IncludeInactive = options.IncludeInactive,
                        LoadFullTree = options.LoadFullTree,
                        ParentGuid = new Guid( accountTreeViewItem.Value ),
                        SecurityGrantToken = options.SecurityGrantToken
                    };
                    accountTreeViewItem.Children = AccountPickerGetChildrenData( newOptions, rockContext );
                    int childrenCount = accountTreeViewItem.Children.Count;

                    accountTreeViewItem.HasChildren = childrenCount > 0;
                    accountTreeViewItem.IsFolder = accountTreeViewItem.HasChildren;
                    accountTreeViewItem.ChildCount = childrenCount;

                    if ( !accountTreeViewItem.HasChildren )
                    {
                        accountTreeViewItem.Children = null;
                    }
                }
            }
            else
            {
                var childQry = financialAccountService.Queryable().AsNoTracking()
                    .Where( f =>
                    f.ParentAccountId.HasValue && resultIds.Contains( f.ParentAccountId.Value )
                    );

                if ( !options.IncludeInactive )
                {
                    childQry = childQry.Where( f => f.IsActive == true );
                }

                var childrenList = childQry.Select( f => f.ParentAccount.Guid.ToString() )
                    .ToList();

                foreach ( var accountTreeViewItem in accountTreeViewItems )
                {
                    int childrenCount = 0;
                    childrenCount = ( childrenList?.Count( v => v == accountTreeViewItem.Value ) ).GetValueOrDefault( 0 );

                    accountTreeViewItem.HasChildren = childrenCount > 0;
                    accountTreeViewItem.IsFolder = childrenCount > 0;

                    if ( accountTreeViewItem.HasChildren )
                    {
                        accountTreeViewItem.ChildCount = childrenCount;
                    }
                }
            }

            return accountTreeViewItems;
        }

        /// <summary>
        /// Gets the accounts that can be displayed in the account picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A collection of unique identifiers that represent the parent accounts.</returns>
        [HttpPost]
        [Route( "AccountPickerGetParentGuids" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( HashSet<Guid> ) )]
        [Rock.SystemGuid.RestActionGuid( "007512c6-0147-4683-a3fe-3fdd1da275c2" )]
        public IActionResult AccountPickerGetParentGuids( [FromBody] AccountPickerGetParentGuidsOptionsBag options )
        {
            var results = new HashSet<Guid>();

            foreach ( var guid in options.Guids )
            {
                var result = FinancialAccountCache.Get( guid )?
                    .GetAncestorFinancialAccounts()?
                    .OrderBy( a => 0 )?
                    .Reverse()?
                    .Select( a => a.Guid );

                foreach ( var resultGuid in result )
                {
                    results.Add( resultGuid );
                }
            }

            return Ok( results );
        }

        /// <summary>
        /// Gets the accounts that match the given search terms.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the accounts that match the search.</returns>
        [HttpPost]
        [Route( "AccountPickerGetSearchedAccounts" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "69fd94cc-f049-4cee-85d1-13e573e30586" )]
        public IActionResult AccountPickerGetSearchedAccounts( [FromBody] AccountPickerGetSearchedAccountsOptionsBag options )
        {
            IQueryable<FinancialAccount> qry;

            if ( options.SearchTerm.IsNullOrWhiteSpace() )
            {
                return BadRequest( "Search Term is required" );
            }

            using ( var rockContext = new RockContext() )
            {
                var financialAccountService = new FinancialAccountService( rockContext );
                qry = financialAccountService.GetAccountsBySearchTerm( options.SearchTerm );

                if ( !options.IncludeInactive )
                {
                    qry = qry.Where( f => f.IsActive == true );
                }

                var accountList = qry
                    .OrderBy( f => f.Order )
                    .ThenBy( f => f.Name )
                    .ToList()
                    .Select( a => new ListItemBag
                    {
                        Value = a.Guid.ToString(),
                        Text = ( options.DisplayPublicName ? a.PublicName : a.Name ) + ( a.GlCode.IsNotNullOrWhiteSpace() ? $" ({a.GlCode})" : "" ),
                        Category = financialAccountService.GetDelimitedAccountHierarchy( a, FinancialAccountService.AccountHierarchyDirection.CurrentAccountToParent )
                    } )
                    .ToList();

                return Ok( accountList );
            }
        }

        /// <summary>
        /// Gets the full account information of the selected accounts for the "preview" view
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the selected accounts.</returns>
        [HttpPost]
        [Route( "AccountPickerGetPreviewItems" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "b080e9d6-207a-412d-acf5-d811fdec30a3" )]
        public IActionResult AccountPickerGetPreviewItems( [FromBody] AccountPickerGetPreviewItemsOptionsBag options )
        {
            IQueryable<FinancialAccount> qry;

            if ( options.SelectedGuids.Count == 0 )
            {
                return Ok( new List<ListItemBag>() );
            }

            using ( var rockContext = new RockContext() )
            {
                var financialAccountService = new FinancialAccountService( rockContext );
                qry = financialAccountService.Queryable().AsNoTracking()
                    .Where( f => options.SelectedGuids.Contains( f.Guid ) );

                var accountList = qry
                    .OrderBy( f => f.Order )
                    .ThenBy( f => f.Name )
                    .ToList()
                    .Select( a => new ListItemBag
                    {
                        Value = a.Guid.ToString(),
                        Text = options.DisplayPublicName ? a.PublicName : a.Name,
                        Category = financialAccountService.GetDelimitedAccountHierarchy( a, FinancialAccountService.AccountHierarchyDirection.CurrentAccountToParent )
                    } )
                    .ToList();

                return Ok( accountList );
            }
        }

        /// <summary>
        /// Gets whether or not to allow account picker to Select All based on how many accounts exist
        /// </summary>
        /// <returns>A boolean value indicating if there are few enough accounts</returns>
        [HttpPost]
        [Route( "AccountPickerGetAllowSelectAll" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( bool ) )]
        [Rock.SystemGuid.RestActionGuid( "4a13b6ea-3031-48c2-9cdb-be183ccad9a2" )]
        public IActionResult AccountPickerGetAllowSelectAll()
        {
            using ( var rockContext = new RockContext() )
            {
                var financialAccountService = new FinancialAccountService( rockContext );
                var count = financialAccountService.Queryable().Count();

                return Ok( count < 1500 );
            }
        }

        #endregion

        #region Achievement Type Picker

        /// <summary>
        /// Gets the achievement types that can be displayed in the achievement type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the achievement types.</returns>
        [HttpPost]
        [Route( "AchievementTypePickerGetAchievementTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "F98E3033-C652-4031-94B3-E7C44ECA51AA" )]
        public IActionResult AchievementTypePickerGetAchievementTypes( [FromBody] AchievementTypePickerGetAchievementTypesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var items = AchievementTypeCache.All( rockContext )
                    .Select( t => new ListItemBag
                    {
                        Value = t.Guid.ToString(),
                        Text = t.Name,
                        Category = t.Category?.Name
                    } )
                    .ToList();

                return Ok( items );
            }
        }

        #endregion

        #region Adaptive Message Picker

        /// <summary>
        /// Gets the adaptive messages and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which data views to load.</param>
        /// <returns>A collection of <see cref="TreeItemBag"/> objects that represent a tree of adaptive messages.</returns>
        [HttpPost]
        [Route( "AdaptiveMessagePickerGetAdaptiveMessages" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "3484A62B-8A52-423A-8154-909D9176E4B6" )]
        public IActionResult AdaptiveMessagePickerGetAdaptiveMessages( [FromBody] UniversalItemTreePickerOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var ccService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var amcService = new AdaptiveMessageCategoryService( rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var items = GetAdaptiveMessageChildren( options.ParentValue.AsGuidOrNull(), ccService, amcService, grant );

                return Ok( items );
            }
        }

        private List<TreeItemBag> GetAdaptiveMessageChildren( Guid? parent, CategoryClientService ccService, AdaptiveMessageCategoryService amcService, SecurityGrant grant )
        {
            var items = ccService.GetCategorizedTreeItems( new CategoryItemTreeOptions
            {
                ParentGuid = parent,
                GetCategorizedItems = true,
                EntityTypeGuid = EntityTypeCache.Get<Rock.Model.AdaptiveMessageCategory>().Guid,
                IncludeUnnamedEntityItems = true,
                IncludeCategoriesWithoutChildren = false,
                DefaultIconCssClass = "fa fa-list-ol",
                LazyLoad = true,
                SecurityGrant = grant
            } );

            var messages = new List<TreeItemBag>();

            // Not a folder, so is actually an AdaptiveMessage, except it was loaded as an
            // AdaptiveMessageCategory so we need to get the Guid of the actual AdaptiveMessage
            foreach ( var item in items )
            {
                if ( !item.IsFolder )
                {
                    item.Type = "Item";
                    // Load the AdaptiveMessageCategory.
                    var category = amcService.Get( item.Value.AsGuid() );
                    if ( category != null )
                    {
                        // Swap the Guid to the AdaptiveMessage Guid
                        item.Value = category.AdaptiveMessage.Guid.ToString();
                    }
                }
                else
                {
                    item.Type = "Category";
                }

                // Get Children
                if ( item.HasChildren )
                {
                    item.Children = new List<TreeItemBag>();
                    item.Children.AddRange( GetAdaptiveMessageChildren( item.Value.AsGuid(), ccService, amcService, grant ) );
                }

                messages.Add( item );
            }

            return messages;
        }

        #endregion

        #region Address Control

        /// <summary>
        /// Gets the required information needed to properly display an address control.
        /// </summary>
        /// <param name="options">The options that describe how the address control will be used.</param>
        /// <returns>The configuration data required to display an address control.</returns>
        [HttpPost]
        [Route( "AddressControlGetConfiguration" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( AddressControlConfigurationBag ) )]
        [Rock.SystemGuid.RestActionGuid( "b477fb6d-4a35-45ec-ac98-b6b5c3727375" )]
        public IActionResult AddressControlGetConfiguration( [FromBody] AddressControlGetConfigurationOptionsBag options )
        {
            var globalAttributesCache = GlobalAttributesCache.Get();
            var showCountrySelection = globalAttributesCache.GetValue( "SupportInternationalAddresses" ).AsBooleanOrNull() ?? false;

            var orgCountryCode = globalAttributesCache.OrganizationCountry;
            var defaultCountryCode = string.IsNullOrWhiteSpace( orgCountryCode ) ? "US" : orgCountryCode;
            var countryCode = options.CountryCode.IsNullOrWhiteSpace() ? defaultCountryCode : options.CountryCode;

            var orgStateCode = globalAttributesCache.OrganizationState;
            var defaultStateCode = countryCode == orgCountryCode ? orgStateCode : string.Empty;

            // Generate List of Countries
            var countries = new List<ListItemBag>();
            var countryValues = DefinedTypeCache.Get( SystemGuid.DefinedType.LOCATION_COUNTRIES.AsGuid() )
                .DefinedValues
                .OrderBy( v => v.Order )
                .ThenBy( v => v.Value )
                .ToList();

            // Move default country to the top of the list
            if ( !string.IsNullOrWhiteSpace( defaultCountryCode ) )
            {
                var defaultCountry = countryValues
                    .Where( v => v.Value.Equals( defaultCountryCode, StringComparison.OrdinalIgnoreCase ) )
                    .FirstOrDefault();
                if ( defaultCountry != null )
                {
                    countries.Add( new ListItemBag { Text = "Countries", Value = string.Empty } );
                    countries.Add( new ListItemBag { Text = options.UseCountryAbbreviation ? defaultCountry.Value : defaultCountry.Description, Value = defaultCountry.Value } );
                    countries.Add( new ListItemBag { Text = "------------------------", Value = "------------------------" } );
                }
            }

            foreach ( var country in countryValues )
            {
                countries.Add( new ListItemBag { Text = options.UseCountryAbbreviation ? country.Value : country.Description, Value = country.Value } );
            }

            // Generate List of States
            string countryGuid = DefinedTypeCache.Get( new Guid( SystemGuid.DefinedType.LOCATION_COUNTRIES ) )
                .DefinedValues
                .Where( v => v.Value.Equals( countryCode, StringComparison.OrdinalIgnoreCase ) )
                .Select( v => v.Guid )
                .FirstOrDefault()
                .ToString();

            List<ListItemBag> states = null;
            var hasStateList = false;

            if ( countryGuid.IsNotNullOrWhiteSpace() )
            {
                var definedType = DefinedTypeCache.Get( new Guid( SystemGuid.DefinedType.LOCATION_ADDRESS_STATE ) );

                states = definedType
                    .DefinedValues
                    .Where( v =>
                        (
                            v.AttributeValues.ContainsKey( "Country" ) &&
                            v.AttributeValues["Country"] != null &&
                            v.AttributeValues["Country"].Value.Equals( countryGuid, StringComparison.OrdinalIgnoreCase )
                        ) ||
                        (
                            ( !v.AttributeValues.ContainsKey( "Country" ) || v.AttributeValues["Country"] == null ) &&
                            v.Attributes.ContainsKey( "Country" ) &&
                            v.Attributes["Country"].DefaultValue.Equals( countryGuid, StringComparison.OrdinalIgnoreCase )
                        ) )
                    .OrderBy( v => v.Order )
                    .ThenBy( v => v.Value )
                    .Select( v => new ListItemBag { Value = v.Value, Text = v.Value } )
                    .ToList();

                hasStateList = states.Any();
            }

            // Get Labels and Validation Rules
            string cityLabel = null;
            string localityLabel = null;
            string stateLabel = null;
            string postalCodeLabel = null;
            DataEntryRequirementLevelSpecifier addressLine1Requirement = DataEntryRequirementLevelSpecifier.Optional;
            DataEntryRequirementLevelSpecifier addressLine2Requirement = DataEntryRequirementLevelSpecifier.Optional;
            DataEntryRequirementLevelSpecifier cityRequirement = DataEntryRequirementLevelSpecifier.Optional;
            DataEntryRequirementLevelSpecifier localityRequirement = DataEntryRequirementLevelSpecifier.Optional;
            DataEntryRequirementLevelSpecifier stateRequirement = DataEntryRequirementLevelSpecifier.Optional;
            DataEntryRequirementLevelSpecifier postalCodeRequirement = DataEntryRequirementLevelSpecifier.Optional;

            var countryValue = DefinedTypeCache.Get( new Guid( SystemGuid.DefinedType.LOCATION_COUNTRIES ) )
                .DefinedValues
                .Where( v => v.Value.Equals( countryCode, StringComparison.OrdinalIgnoreCase ) )
                .FirstOrDefault();

            if ( countryValue != null )
            {
                cityLabel = countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressCityLabel ).ToStringOrDefault( "City" );
                localityLabel = countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressLocalityLabel ).ToStringOrDefault( "Locality" );
                stateLabel = countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressStateLabel ).ToStringOrDefault( "State" );
                postalCodeLabel = countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressPostalCodeLabel ).ToStringOrDefault( "Postal Code" );

                var requirementField = new DataEntryRequirementLevelFieldType();

                addressLine1Requirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressLine1Requirement ), DataEntryRequirementLevelSpecifier.Optional );
                addressLine2Requirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressLine2Requirement ), DataEntryRequirementLevelSpecifier.Optional );
                cityRequirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressCityRequirement ), DataEntryRequirementLevelSpecifier.Optional );
                localityRequirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressLocalityRequirement ), DataEntryRequirementLevelSpecifier.Optional );
                stateRequirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressStateRequirement ), DataEntryRequirementLevelSpecifier.Optional );
                postalCodeRequirement = requirementField.GetDeserializedValue( countryValue.GetAttributeValue( SystemKey.CountryAttributeKey.AddressPostalCodeRequirement ), DataEntryRequirementLevelSpecifier.Optional );
            }

            return Ok( new AddressControlConfigurationBag
            {
                ShowCountrySelection = showCountrySelection,
                DefaultCountry = defaultCountryCode,
                DefaultState = defaultStateCode,
                Countries = countries,
                States = states,

                HasStateList = hasStateList,
                SelectedCountry = countryCode,

                CityLabel = cityLabel,
                LocalityLabel = localityLabel,
                StateLabel = stateLabel,
                PostalCodeLabel = postalCodeLabel,

                AddressLine1Requirement = ( RequirementLevel ) addressLine1Requirement,
                AddressLine2Requirement = ( RequirementLevel ) addressLine2Requirement,
                CityRequirement = ( RequirementLevel ) cityRequirement,
                LocalityRequirement = ( RequirementLevel ) localityRequirement,
                StateRequirement = ( RequirementLevel ) stateRequirement,
                PostalCodeRequirement = ( RequirementLevel ) postalCodeRequirement,
            } );
        }

        /// <summary>
        /// Validates the given address and returns the string representation of the address.
        /// </summary>
        /// <param name="options">Address details to validate</param>
        /// <returns>Validation information and a single string representation of the address.</returns>
        [HttpPost]
        [System.Web.Http.Route( "AddressControlValidateAddress" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( AddressControlValidateAddressResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "ff879ea7-07dd-43ec-a5de-26f55e9f073a" )]
        public IActionResult AddressControlValidateAddress( [FromBody] AddressControlValidateAddressOptionsBag options )
        {
            var editedLocation = new Location();
            string errorMessage = null;
            string addressString = null;

            var globalAttributesCache = GlobalAttributesCache.Get();
            var orgCountryCode = globalAttributesCache.OrganizationCountry;
            var defaultCountryCode = string.IsNullOrWhiteSpace( orgCountryCode ) ? "US" : orgCountryCode;

            editedLocation.Street1 = options.Street1;
            editedLocation.Street2 = options.Street2;
            editedLocation.City = options.City;
            editedLocation.State = options.State;
            editedLocation.PostalCode = options.PostalCode;
            editedLocation.County = options.Locality;
            editedLocation.Country = options.Country.IsNotNullOrWhiteSpace() ? options.Country : defaultCountryCode;

            var locationService = new LocationService( new RockContext() );

            string validationMessage;

            var isValid = LocationService.ValidateLocationAddressRequirements( editedLocation, out validationMessage );

            if ( !isValid )
            {
                errorMessage = validationMessage;
            }
            else
            {
                editedLocation = locationService.Get( editedLocation.Street1, editedLocation.Street2, editedLocation.City, editedLocation.State, editedLocation.County, editedLocation.PostalCode, editedLocation.Country, null );
                addressString = editedLocation.GetFullStreetAddress().ConvertCrLfToHtmlBr();
            }

            return Ok( new AddressControlValidateAddressResultsBag
            {
                ErrorMessage = errorMessage,
                IsValid = isValid,
                AddressString = addressString,
                Address = new AddressControlBag
                {
                    Street1 = editedLocation.Street1,
                    Street2 = editedLocation.Street2,
                    City = editedLocation.City,
                    State = editedLocation.State,
                    PostalCode = editedLocation.PostalCode,
                    Locality = editedLocation.County,
                    Country = editedLocation.Country
                }
            } );
        }

        /// <summary>
        /// Converts an address to a human readable format that can be displayed
        /// in HTML.
        /// </summary>
        /// <param name="address">Address to be displayed.</param>
        /// <returns>A single string that represents the HTML to display the address.</returns>
        [HttpPost]
        [Route( "AddressControlGetStreetAddressString" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [Rock.SystemGuid.RestActionGuid( "9258BA75-F922-4607-A2C0-036141621F0E" )]
        public IActionResult AddressControlGetStreetAddressString( [FromBody] AddressControlBag address )
        {
            Location editedLocation;
            var globalAttributesCache = GlobalAttributesCache.Get();
            var orgCountryCode = globalAttributesCache.OrganizationCountry;
            var defaultCountryCode = string.IsNullOrWhiteSpace( orgCountryCode ) ? "US" : orgCountryCode;

            var locationService = new LocationService( new RockContext() );
            editedLocation = locationService.Get( address.Street1, address.Street2, address.City, address.State, address.Locality, address.PostalCode, address.Country.IsNotNullOrWhiteSpace() ? address.Country : defaultCountryCode, null );

            if ( editedLocation == null )
            {
                editedLocation = new Location
                {
                    Street1 = address.Street1.FixCase(),
                    Street2 = address.Street2.FixCase(),
                    City = address.City.FixCase(),
                    State = address.State,
                    County = address.Locality,
                    PostalCode = address.PostalCode,
                    Country = address.Country
                };
            }

            return Ok( editedLocation.GetFullStreetAddress().ConvertCrLfToHtmlBr() );
        }

        #endregion

        #region AI Provider Picker

        /// <summary>
        /// Gets the AI providers that can be displayed in the AI provider picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the AI providers.</returns>
        [HttpPost]
        [Route( "AIProviderPickerGetAIProviders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "A9403C3A-E66F-4051-B857-30B89C3A65B3" )]
        public IActionResult AIProviderPickerGetAIProviders( [FromBody] AIProviderPickerGetAIProviderOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var items = AIProviderCache.All()
                    .Where( a => a.EntityTypeId.HasValue && a.IsActive )
                    .OrderBy( a => a.Name )
                    .ToListItemBagList();

                return Ok( items );
            }
        }

        #endregion

        #region Assessment Type Picker

        /// <summary>
        /// Gets the assessment types that can be displayed in the assessment type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the assessment types.</returns>
        [HttpPost]
        [System.Web.Http.Route( "AssessmentTypePickerGetAssessmentTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "B47DCE1B-89D7-4DD5-88A7-B3C393D49A7C" )]
        public IActionResult AssessmentTypePickerGetEntityTypes( [FromBody] AssessmentTypePickerGetAssessmentTypesOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var items = AssessmentTypeCache.All( rockContext )
                    .Where( at => options.isInactiveIncluded == true || at.IsActive )
                    .Where( at => at.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( at, Authorization.VIEW ) == true )
                    .OrderBy( at => at.Title )
                    .ThenBy( at => at.Id )
                    .Select( at => new ListItemBag
                    {
                        Value = at.Guid.ToString(),
                        Text = at.Title
                    } )
                    .ToList();

                return Ok( items );
            }
        }

        #endregion

        #region Asset Manager

        /// <summary>
        /// Gets the root storage providers and/or the root File Browser folder.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="AssetManagerTreeItemBag"/> objects that represent the asset storage providers/folders.</returns>
        [HttpPost]
        [Route( "AssetManagerGetRootFolders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<AssetManagerTreeItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [Rock.SystemGuid.RestActionGuid( "9A96E14F-99DB-4F9A-95EB-DF17D3B5EE25" )]
        public IActionResult AssetManagerGetRootFolders( [FromBody] AssetManagerGetRootFoldersOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.VIEW ) ?? false ) )
            {
                return Unauthorized();
            }

            var expandedFolders = new List<string>();
            var tree = new List<TreeItemBag>();
            var updatedExpandedFolders = new List<string>();
            var selectedFolder = options.SelectedFolder.IsNotNullOrWhiteSpace() ? ParseAssetKey( options.SelectedFolder ) : null;

            // Decrypt the root folder of the ExpandedFolders so we actually know which folders to expand
            if ( ( options.ExpandedFolders?.Count ?? 0 ) > 0 )
            {
                foreach ( var folder in options.ExpandedFolders )
                {
                    var parsedAsset = ParseAssetKey( folder );
                    expandedFolders.Add( $"{parsedAsset.ProviderId},{parsedAsset.FullPath}" );
                }
            }

            if ( options.EnableFileManager )
            {
                var (folder, expandedFileFolders) = GetRootFolder( options.RootFolder, expandedFolders, selectedFolder, options.UserSpecificRoot );

                tree.Add( folder );
                if ( expandedFileFolders != null )
                {
                    updatedExpandedFolders.AddRange( expandedFileFolders );
                }

                // If only file manager is enabled, we want the folder expanded immediately
                if ( !options.EnableAssetManager && !( folder.Children?.Any() ?? false ) )
                {
                    var (children, expanded) = GetChildFolders( ParseAssetKey( folder.Value ), expandedFolders );
                    folder.Children = children;
                    updatedExpandedFolders.Add( folder.Value );
                }
            }

            if ( options.EnableAssetManager )
            {
                var (assetTree, expandedAssetFolders) = GetAssetStorageProviders( expandedFolders, selectedFolder );

                tree.AddRange( assetTree );
                updatedExpandedFolders.AddRange( expandedAssetFolders );

                var folder = assetTree?.ElementAt( 0 );

                // If only asset manager is enabled and only one asset provider exists, we want it expanded immediately
                if ( !options.EnableFileManager && ( assetTree?.Count ?? 0 ) == 1 && !( folder.Children?.Any() ?? false ) )
                {
                    var (children, expanded) = GetChildrenOfAsset( ParseAssetKey( folder.Value ), expandedFolders );
                    folder.Children = children;
                    updatedExpandedFolders.Add( folder.Value );
                }
            }

            return Ok( new
            {
                Tree = tree,
                UpdatedExpandedFolders = updatedExpandedFolders,
            } );
        }

        /// <summary>
        /// Gets a list of folders of a given parent folder.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="AssetManagerTreeItemBag"/> objects that represent the asset storage child folders.</returns>
        [HttpPost]
        [Route( "AssetManagerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<AssetManagerTreeItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [Rock.SystemGuid.RestActionGuid( "68C50BAE-C50C-4143-B37F-58C80BF5E1BF" )]
        public IActionResult AssetManagerGetChildren( [FromBody] AssetManagerBaseOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.VIEW ) ?? false ) )
            {
                return Unauthorized();
            }

            var parsedAsset = ParseAssetKey( options.AssetFolderId );

            if ( parsedAsset.ProviderId == null || parsedAsset.FullPath == null )
            {
                return Ok( new List<AssetManagerTreeItemBag>() );
            }

            if ( parsedAsset.IsAssetProviderAsset )
            {
                var (tree, updatedExpandedFolders) = GetChildrenOfAsset( parsedAsset, new List<string>() );
                return Ok( tree );
            }
            else if ( parsedAsset.IsLocalAsset )
            {
                var (tree, updatedExpandedFolders) = GetChildFolders( parsedAsset, new List<string>() );
                return Ok( tree );
            }

            return Ok( new List<AssetManagerTreeItemBag>() );
        }

        /// <summary>
        /// Gets the files present in a asset provider folder
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="Asset"/> objects that represent the files.</returns>
        [HttpPost]
        [Route( "AssetManagerGetFiles" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Asset ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "D45422C0-5FCA-44C4-B9E1-4BA05E8D534D" )]
        public IActionResult AssetManagerGetFiles( [FromBody] AssetManagerGetFilesOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.VIEW ) ?? false ) )
            {
                return Unauthorized();
            }

            var asset = ParseAssetKey( options.AssetFolderId );

            if ( asset.ProviderId == null || asset.FullPath == null )
            {
                return BadRequest();
            }

            if ( asset.IsLocalAsset )
            {
                var files = GetFilesInFolder( asset, options.BrowseMode );

                return Ok( files );
            }

            if ( asset.IsAssetProviderAsset )
            {
                var (provider, component) = GetAssetStorageProvider( asset.ProviderId.Value );

                if ( provider == null || component == null )
                {
                    return BadRequest();
                }

                List<Asset> files = component.ListFilesInFolder( provider.ToEntity(), new Asset { Key = asset.FullPath, Type = AssetType.Folder, AssetStorageProviderId = asset.ProviderId.Value } );

                // TODO: filter on browse mode

                return Ok( new AssetManagerGetFilesResultsBag<Asset>
                {
                    Files = files,
                    IsFolderUploadRestricted = true
                } );
            }

            return BadRequest();
        }

        /// <summary>
        /// Delete a folder within a asset storage provider
        /// </summary>
        /// <param name="options">The options that describe which folder to delete.</param>
        /// <returns>True if successful, false otherwise.</returns>
        [HttpPost]
        [Route( "AssetManagerDeleteFolder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( bool ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "7625091B-D70A-4564-97C8-ED77AE5DB738" )]
        public IActionResult AssetManagerDeleteFolder( [FromBody] AssetManagerBaseOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.DELETE ) ?? false ) )
            {
                return Unauthorized();
            }

            var asset = ParseAssetKey( options.AssetFolderId );

            if ( asset.ProviderId == null || asset.FullPath == null )
            {
                return BadRequest();
            }

            if ( asset.IsLocalAsset )
            {
                try
                {
                    var physicalFolder = System.Web.HttpContext.Current.Server.MapPath( asset.FullPath );
                    Directory.Delete( physicalFolder, true );
                }
                catch ( Exception ex )
                {
                    return InternalServerError( ex );
                }

                return Ok( true );
            }
            else if ( asset.IsAssetProviderAsset )
            {
                var (provider, component) = GetAssetStorageProvider( asset.ProviderId.Value );

                if ( provider == null || component == null )
                {
                    return BadRequest();
                }

                return Ok( component.DeleteAsset( provider.ToEntity(), new Asset { Key = asset.FullPath, Type = AssetType.Folder } ) );
            }

            return BadRequest();
        }

        /// <summary>
        /// Creates a new folder in the specified location on the file system or asset provider.
        /// </summary>
        /// <param name="options">The options that describe the name of the new folder and where it should be.</param>
        /// <returns>An <see cref="AssetManagerTreeItemBag"/> object that represents the new folder.</returns>
        [HttpPost]
        [Route( "AssetManagerAddFolder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( AssetManagerTreeItemBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "B90D9215-57A4-45D3-9B70-A44AA2C9FE7B" )]
        public IActionResult AssetManagerAddFolder( [FromBody] AssetManagerAddFolderOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.EDIT ) ?? false ) )
            {
                return Unauthorized();
            }

            if ( !IsValidAssetFolderName( options.NewFolderName ) || options.NewFolderName.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var parsedAsset = ParseAssetKey( options.AssetFolderId );

            if ( parsedAsset == null || parsedAsset.ProviderId == null || parsedAsset.FullPath == null )
            {
                return BadRequest();
            }

            if ( parsedAsset.IsLocalAsset )
            {
                var physicalFolder = System.Web.HttpContext.Current.Server.MapPath( parsedAsset.FullPath );
                Directory.CreateDirectory( Path.Combine( physicalFolder, options.NewFolderName ) );

                return Ok( new AssetManagerTreeItemBag
                {
                    Text = options.NewFolderName,
                    Value = $"0,{parsedAsset.EncryptedRoot},{Path.Combine( parsedAsset.SubPath, options.NewFolderName )}",
                    IconCssClass = "fa fa-folder",
                    HasChildren = false,
                    UnencryptedRoot = parsedAsset.Root
                } );
            }
            else if ( parsedAsset.IsAssetProviderAsset )
            {
                var (provider, component) = GetAssetStorageProvider( parsedAsset.ProviderId.Value );

                if ( provider == null || component == null )
                {
                    return BadRequest();
                }

                var asset = new Asset { Type = AssetType.Folder };

                // Selecting the root does not put a value for the selected folder, so we have to make sure
                // if it does not have a value that we use name instead of key so the root folder is used
                // by the component.
                if ( parsedAsset.FullPath.IsNotNullOrWhiteSpace() )
                {
                    asset.Key = parsedAsset.FullPath + options.NewFolderName;
                }
                else
                {
                    asset.Name = options.NewFolderName;
                }

                if ( component.CreateFolder( provider.ToEntity(), asset ) )
                {
                    return Ok( new AssetManagerTreeItemBag
                    {
                        Text = options.NewFolderName,
                        Value = $"{provider.Id},{parsedAsset.EncryptedRoot},{Path.Combine( parsedAsset.SubPath, options.NewFolderName )}/",
                        IconCssClass = "fa fa-folder",
                        HasChildren = false,
                        UnencryptedRoot = parsedAsset.Root
                    } );
                }
            }

            return null;
        }

        /// <summary>
        /// Rename a given folder to the specified name.
        /// </summary>
        /// <param name="options">The options that describe which folder to rename and its new name.</param>
        /// <returns>The new key string for the folder that was renamed.</returns>
        [HttpPost]
        [Route( "AssetManagerRenameFolder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to edit the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "8DF6054E-6F52-4A08-A7F5-C11F44B8465C" )]
        public IActionResult AssetManagerRenameFolder( [FromBody] AssetManagerRenameFolderOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.EDIT ) ?? false ) )
            {
                return Unauthorized();
            }

            try
            {
                var asset = ParseAssetKey( options.AssetFolderId );
                var physicalPath = System.Web.HttpContext.Current.Server.MapPath( asset.FullPath );
                var renamedPath = Path.Combine( Path.GetDirectoryName( physicalPath.TrimEnd( '/', '\\' ) ), options.NewFolderName );
                Directory.Move( physicalPath, renamedPath );

                var newKey = $"0,{asset.EncryptedRoot},{Path.Combine( Path.GetDirectoryName( asset.SubPath.TrimEnd( '/', '\\' ) ), options.NewFolderName ).Replace( "\\", "/" ).TrimEnd( '/', '\\' ) + "/"}";

                return Ok( newKey );
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }
        }

        /// <summary>
        /// Move a folder to another location on the file system.
        /// </summary>
        /// <param name="options">The options that describe which folder to move and where to move it to.</param>
        /// <returns>The new key string for the folder that was moved.</returns>
        [HttpPost]
        [Route( "AssetManagerMoveFolder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to edit the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "87A139A7-78B8-4CC9-8A3B-146A338A291F" )]
        public IActionResult AssetManagerMoveFolder( [FromBody] AssetManagerMoveFolderOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.EDIT ) ?? false ) )
            {
                return Unauthorized();
            }

            try
            {
                var asset = ParseAssetKey( options.AssetFolderId );
                var baseFolderName = Path.GetFileName( asset.FullPath.TrimEnd( '/', '\\' ) );
                var currentPhysicalPath = System.Web.HttpContext.Current.Server.MapPath( asset.FullPath );
                var targetRootRelativePath = Path.Combine( asset.Root, options.TargetFolder.TrimStart( '/', '\\' ), baseFolderName );
                var targetPhyicalPath = System.Web.HttpContext.Current.Server.MapPath( targetRootRelativePath );

                if ( !Directory.Exists( targetPhyicalPath ) && !File.Exists( targetPhyicalPath ) )
                {
                    Directory.Move( currentPhysicalPath, targetPhyicalPath );
                }
                else
                {
                    return BadRequest( "Invalid target location. Something already exists there with the same name." );
                }

                var newKey = $"0,{asset.EncryptedRoot},{Path.Combine( options.TargetFolder, baseFolderName ).Replace( "\\", "/" ).TrimEnd( '/' ) + "/"}";

                return Ok( newKey );
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }
        }

        /// <summary>
        /// Deletes the specified file(s) from the server/asset provider
        /// </summary>
        /// <param name="options">The options that describe which file(s) to delete.</param>
        /// <returns>True if every file was deleted successfully.</returns>
        [HttpPost]
        [Route( "AssetManagerDeleteFiles" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( bool ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to delete the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "55ADD16B-0FC1-4F33-BB0A-03C29018866F" )]
        public IActionResult AssetManagerDeleteFiles( [FromBody] AssetManagerDeleteFilesOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.DELETE ) ?? false ) )
            {
                return Unauthorized();
            }

            try
            {
                if ( options.AssetStorageProviderId == 0 )
                {
                    foreach ( string file in options.Files )
                    {
                        var physicalPath = System.Web.HttpContext.Current.Server.MapPath( file );
                        File.Delete( physicalPath );
                    }
                }
                else
                {
                    var (provider, component) = GetAssetStorageProvider( options.AssetStorageProviderId );

                    if ( provider == null || component == null )
                    {
                        return BadRequest();
                    }

                    foreach ( string file in options.Files )
                    {
                        component.DeleteAsset( provider.ToEntity(), new Asset { Key = file, Type = AssetType.File } );
                    }
                }
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }

            return Ok( true );
        }

        /// <summary>
        /// Downloads the specified file via a stream.
        /// </summary>
        /// <param name="options">The options that describe which file to download.</param>
        /// <returns>A stream for the download of the specified file.</returns>
        [HttpGet]
        [Route( "AssetManagerDownloadFile" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "The content of the file being requested." )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "C810774B-8B15-42D0-BAC2-85503AB23BC0" )]
        public IActionResult AssetManagerDownloadFile( [FromUri] AssetManagerDownloadFileOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.VIEW ) ?? false ) )
            {
                return Unauthorized();
            }

            Stream stream;
            string fileName;

            try
            {
                if ( options.AssetStorageProviderId == 0 )
                {
                    var physicalPath = System.Web.HttpContext.Current.Server.MapPath( options.File );
                    fileName = Path.GetFileName( physicalPath );
                    stream = File.Open( physicalPath, FileMode.Open );
                }
                else
                {
                    var (provider, component) = GetAssetStorageProvider( options.AssetStorageProviderId );

                    if ( provider == null || component == null || options.File.IsNullOrWhiteSpace() )
                    {
                        return BadRequest( "Invalid Asset Storage Provider ID or file key." );
                    }

                    Asset asset = component.GetObject( provider.ToEntity(), new Asset { Key = options.File, Type = AssetType.File }, false );
                    fileName = asset.Name;
                    byte[] bytes = asset.AssetStream.ReadBytesToEnd();
                    stream = new MemoryStream( bytes );
                }
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }

#if WEBFORMS
            var result = new System.Net.Http.HttpResponseMessage( System.Net.HttpStatusCode.OK )
            {
                Content = new System.Net.Http.StreamContent( stream )
            };

            result.Content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );
            result.Content.Headers.Add( "content-disposition", "attachment; filename=" + System.Web.HttpUtility.UrlEncode( fileName ) );

            return new System.Web.Http.Results.ResponseMessageResult( result );
#else
            return new Microsoft.AspNetCore.Mvc.FileStreamResult( stream, "application/octet-stream" )
            {
                FileDownloadName = fileName
            };
#endif
        }

        /// <summary>
        /// Rename a file.
        /// </summary>
        /// <param name="options">The options that describe which file to rename and what to rename it.</param>
        /// <returns>True if successful, false otherwise.</returns>
        [HttpPost]
        [Route( "AssetManagerRenameFile" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( bool ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to edit the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "150AAF48-33C5-47F8-BD53-2CF3A75F88FB" )]
        public IActionResult AssetManagerRenameFile( [FromBody] AssetManagerRenameFileOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.EDIT ) ?? false ) )
            {
                return Unauthorized();
            }

            try
            {
                if ( options.AssetStorageProviderId == 0 )
                {
                    var physicalPath = System.Web.HttpContext.Current.Server.MapPath( options.File );
                    var renamedPath = Path.Combine( Path.GetDirectoryName( physicalPath ), options.NewFileName );
                    File.Move( physicalPath, renamedPath );

                    return Ok( true );
                }
                else
                {
                    var (provider, component) = GetAssetStorageProvider( options.AssetStorageProviderId );

                    if ( provider == null || component == null || options.File.IsNullOrWhiteSpace() || options.NewFileName.IsNullOrWhiteSpace() )
                    {
                        return BadRequest();
                    }

                    return Ok( component.RenameAsset( provider.ToEntity(), new Asset { Key = options.File, Type = AssetType.File }, options.NewFileName ) );
                }
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }
        }

        /// <summary>
        /// Extract a given zip archive into its folder.
        /// </summary>
        /// <param name="options">The options that describe which file to extract.</param>
        /// <returns>True if the operation worked.</returns>
        [HttpPost]
        [Route( "AssetManagerExtractFile" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( bool ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to edit the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "07CECA87-B9F9-4130-AC09-584AC9DBBE8C" )]
        public IActionResult AssetManagerExtractFile( [FromBody] AssetManagerExtractFileOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.EDIT ) ?? false ) )
            {
                return Unauthorized();
            }

            if ( options == null || options.EncryptedRoot.IsNullOrWhiteSpace() || options.FileName.IsNullOrWhiteSpace() )
            {
                return BadRequest();
            }

            var root = Rock.Security.Encryption.DecryptString( options.EncryptedRoot );
            var fullPath = Path.Combine( root, options.FileName );
            var physicalZipFile = System.Web.HttpContext.Current.Server.MapPath( fullPath );
            var directoryPath = Path.GetDirectoryName( physicalZipFile );

            try
            {
                if ( File.Exists( physicalZipFile ) )
                {
                    FileInfo fileInfo = new FileInfo( physicalZipFile );
                    if ( fileInfo.Extension.Equals( ".zip", StringComparison.OrdinalIgnoreCase ) )
                    {
                        using ( ZipArchive archive = ZipFile.OpenRead( physicalZipFile ) )
                        {
                            foreach ( ZipArchiveEntry file in archive.Entries )
                            {
                                string completeFileName = Path.Combine( directoryPath, file.FullName );
                                if ( file.Name == string.Empty )
                                {
                                    // Assuming Empty for Directory
                                    Directory.CreateDirectory( Path.GetDirectoryName( completeFileName ) );
                                    continue;
                                }

                                file.ExtractToFile( completeFileName, true );
                            }
                        }
                    }
                    else
                    {
                        File.Delete( physicalZipFile );
                        throw new Exception( "Invalid File Uploaded." );
                    }
                    File.Delete( physicalZipFile );
                }
                else
                {
                    throw new Exception( "Error Extracting the File." );
                }

                return Ok( true );
            }
            catch ( Exception ex )
            {
                File.Delete( physicalZipFile );
                return InternalServerError( ex );
            }
        }

        /// <summary>
        /// Get a flat list of all the folders and subfolders in a given root folder, excluding a given folder and its children.
        /// </summary>
        /// <param name="options">The options that describe which folders to load and not load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent all the folders.</returns>
        [HttpPost]
        [Route( "AssetManagerGetListOfAllFolders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized, Description = "Not authorized to view the asset manager tree information." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "1008C9C5-E33E-43F6-BB02-D1BDF2CCE205" )]
        public IActionResult AssetManagerGetListOfAllFolders( [FromBody] AssetManagerGetListOfAllFoldersOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( !( grant?.IsAccessGranted( AssetAndFileManagerSecurityGrantRule.AssetAndFileManagerAccess.Instance, Security.Authorization.VIEW ) ?? false ) )
            {
                return Unauthorized();
            }

            if ( options == null || options.EncryptedRoot.IsNullOrWhiteSpace() || options.SelectedFolder.IsNullOrWhiteSpace() )
            {
                return BadRequest();
            }

            try
            {
                var root = Rock.Security.Encryption.DecryptString( options.EncryptedRoot );

                if ( options.UserSpecificRoot )
                {
                    var username = RockRequestContext.CurrentUser.UserName;
                    root = root.EnsureTrailingForwardslash() + username.EnsureTrailingForwardslash();
                }

                var physicalRootFolder = System.Web.HttpContext.Current.Server.MapPath( root );
                var physicalSelectedFolder = System.Web.HttpContext.Current.Server.MapPath( options.SelectedFolder ).TrimEnd( '/', '\\' );
                var folders = GetRecursiveFolders( physicalRootFolder, physicalRootFolder, physicalSelectedFolder );

                if ( folders != null )
                {
                    var folderOptions = folders.Select( folderName => new ListItemBag { Text = folderName, Value = folderName } ).ToList();

                    return Ok( folderOptions );
                }

                return Ok( new List<ListItemBag>() );
            }
            catch ( Exception ex )
            {
                return InternalServerError( ex );
            }
        }

        /// <summary>
        /// Gets the asset storage provider [cache] and associated asset storage component using the ID stored in the hidden field.
        /// </summary>
        /// <returns>The asset storage provider and component.</returns>
        private (AssetStorageProviderCache provider, AssetStorageComponent component) GetAssetStorageProvider( int assetStorageProviderId )
        {
            var provider = AssetStorageProviderCache.Get( assetStorageProviderId );
            var component = provider?.AssetStorageComponent;

            return (provider, component);
        }

        /// <summary>
        /// Parse a string ID in the form of "${assetStorageProviderID},${encryptedRootPath},${subPath}" and pull out its individual parts.
        /// </summary>
        /// <param name="assetItemKey">A key string that identifies an asset in the form of "${assetStorageProviderID},${encryptedRootPath},${subPath}".</param>
        /// <returns>An <see cref="AssetManagerAsset"/> object that represents all the data of an asset.</returns>
        private AssetManagerAsset ParseAssetKey( string assetItemKey )
        {
            try
            {
                var assetParts = assetItemKey.Split( ',' );
                int assetStorageProviderId;

                if ( assetParts.Length < 3 )
                {
                    return null;
                }
                else
                {
                    assetStorageProviderId = assetParts[0].AsInteger();
                    var encryptedRoot = assetParts[1].Trim();
                    var root = Rock.Security.Encryption.DecryptString( encryptedRoot );

                    // Verify all local roots start with "~/"
                    if ( assetStorageProviderId == 0 && !root.StartsWith( "~/" ) )
                    {
                        root = "~/" + root;
                    }

                    // Verify root ends with "/"
                    root = root.EnsureTrailingForwardslash();

                    var partialPath = assetParts[2].Trim();

                    if ( partialPath != string.Empty )
                    {
                        // Verify path doesn't start with a "/" and does end with a "/"
                        partialPath = partialPath.TrimStart( '/', '\\' ).EnsureTrailingForwardslash();
                    }

                    return new AssetManagerAsset
                    {
                        ProviderId = assetStorageProviderId,
                        EncryptedRoot = encryptedRoot,
                        Root = root,
                        SubPath = partialPath
                    };
                }
            }
            catch ( Exception )
            {
                return null;
            }
        }

        /// <summary>
        /// Get a (tree) list of all the folders in the given root folder of the local file system, along with their children if they are in the given list of expanded folders.
        /// </summary>
        /// <returns>A (tree) list of all the child folders and an updated version of the given expanded folders list.</returns>
        private (AssetManagerTreeItemBag, List<string> updatedExpandedFolders) GetRootFolder( string encryptedRootFolder, List<string> expandedFolders, AssetManagerAsset selectedFolder, bool userSpecificRoot )
        {
            if ( encryptedRootFolder.IsNullOrWhiteSpace() )
            {
                // Set root to default
                encryptedRootFolder = Rock.Security.Encryption.EncryptString( "~/Content/" );
            }

            var rootAssetKey = $"0,{encryptedRootFolder},,True";
            var parsedAsset = ParseAssetKey( rootAssetKey );

            if ( parsedAsset.Root.IsNullOrWhiteSpace() )
            {
                return (null, null);
            }

            // ensure that the folder is formatted to be relative to web root
            if ( !parsedAsset.Root.StartsWith( "~/" ) )
            {
                parsedAsset.Root = "~/" + parsedAsset.Root;
            }

            if ( userSpecificRoot )
            {
                var username = RockRequestContext.CurrentUser.UserName;
                parsedAsset.Root = parsedAsset.Root.EnsureTrailingForwardslash() + username.EnsureTrailingForwardslash();
                encryptedRootFolder = Rock.Security.Encryption.EncryptString( parsedAsset.Root );
                parsedAsset.EncryptedRoot = encryptedRootFolder;
            }

            // If the selected folder is using this asset provider, then use its existing encrypted folder value instead or re-encrypting
            if ( selectedFolder != null && selectedFolder.ProviderId == 0 && selectedFolder.Root == parsedAsset.Root )
            {
                encryptedRootFolder = selectedFolder.EncryptedRoot;
                parsedAsset.EncryptedRoot = selectedFolder.EncryptedRoot;
            }

            var localRoot = System.Web.HttpContext.Current.Server.MapPath( parsedAsset.Root );

            if ( !Directory.Exists( localRoot ) )
            {
                try
                {
                    Directory.CreateDirectory( localRoot );
                }
                catch
                {
                    // intentionally ignore the exception. It'll be handled later.
                }
            }

            if ( Directory.Exists( localRoot ) && !IsHiddenFolder( localRoot ) )
            {
                var updatedExpandedFolders = new List<string>();

                var folder = new DirectoryInfo( localRoot );
                var hasChildren = false;
                var folderKey = $"0,{encryptedRootFolder},,True";

                try
                {
                    var subDirectoryList = Directory.GetDirectories( localRoot )
                        .Where( dir => !IsHiddenFolder( dir ) )
                        .ToList();
                    hasChildren = subDirectoryList.Any();
                }
                catch ( Exception )
                {
                    // Empty. Just mark as having no children.
                }

                var folderBag = new AssetManagerTreeItemBag
                {
                    Text = folder.Name,
                    Value = folderKey,
                    IconCssClass = "fa fa-folder",
                    HasChildren = hasChildren,
                    UnencryptedRoot = parsedAsset.Root
                };

                if ( hasChildren && expandedFolders.Contains( $"0,{parsedAsset.Root}" ) )
                {
                    updatedExpandedFolders.Add( folderKey );
                    var (children, exFolders) = GetChildFolders( parsedAsset, expandedFolders );

                    folderBag.Children = children;

                    if ( exFolders?.Any() ?? false )
                    {
                        updatedExpandedFolders.AddRange( exFolders );
                    }
                }

                return (folderBag, updatedExpandedFolders);
            }

            return (null, null);
        }

        /// <summary>
        /// Get a list of all the folders in the given folder of the local file system, along with their children if they are in the given list of expanded folders.
        /// </summary>
        /// <returns>A list of all the child folders and an updated version of the given expanded folders list.</returns>
        private (List<TreeItemBag>, List<string> updatedExpandedFolders) GetChildFolders( AssetManagerAsset asset, List<string> expandedFolders )
        {
            var tree = new List<TreeItemBag>();
            var updatedExpandedFolders = new List<string>();

            if ( asset.Root.IsNullOrWhiteSpace() )
            {
                return (null, null);
            }

            // ensure that the folder is formatted to be relative to web root
            if ( !asset.Root.StartsWith( "~/" ) )
            {
                asset.Root = "~/" + asset.Root;
            }

            var localRoot = System.Web.HttpContext.Current.Server.MapPath( asset.Root );
            var localPath = System.Web.HttpContext.Current.Server.MapPath( asset.FullDirectoryPath );
            var subDirectories = Directory.GetDirectories( localPath ).OrderBy( a => a ).ToList();

            foreach ( var subDir in subDirectories )
            {
                if ( !IsHiddenFolder( subDir ) )
                {
                    var subDirInfo = new DirectoryInfo( subDir );
                    var hasChildren = false;
                    var subDirKey = $"0,{asset.EncryptedRoot},{subDir.Replace( localRoot, string.Empty ).Replace( "\\", "/" )}/";
                    var subDirAsset = ParseAssetKey( subDirKey );

                    try
                    {
                        var childDirectories = Directory.GetDirectories( subDir )
                            .Where( dir => !IsHiddenFolder( dir ) )
                            .ToList();
                        hasChildren = childDirectories.Any();
                    }
                    catch ( Exception )
                    {
                        // Empty. Just mark as having no children.
                    }

                    var subDirItemBag = new AssetManagerTreeItemBag
                    {
                        Text = subDirInfo.Name,
                        Value = subDirKey,
                        IconCssClass = "fa fa-folder",
                        HasChildren = hasChildren,
                        UnencryptedRoot = asset.Root
                    };

                    if ( hasChildren && expandedFolders.Contains( $"0,{subDirAsset.FullDirectoryPath}" ) )
                    {
                        updatedExpandedFolders.Add( subDirKey );

                        var (children, exFolders) = GetChildFolders( subDirAsset, expandedFolders );

                        subDirItemBag.Children = children;

                        if ( exFolders?.Any() ?? false )
                        {
                            updatedExpandedFolders.AddRange( exFolders );
                        }
                    }

                    tree.Add( subDirItemBag );
                }
            }

            return (tree, updatedExpandedFolders);
        }

        /// <summary>
        /// Get a list of all files in the given folder. If browseMode is "image", only return the images. Also return any restrictions for the folder.
        /// </summary>
        /// <param name="asset">An asset model representing the folder that the files are in.</param>
        /// <param name="browseMode">"image" or "doc", which determines whether to filter for only images or get all files.</param>
        /// <returns>An object containing the list of files as well as flags for restrictions on the folder.</returns>
        private AssetManagerGetFilesResultsBag<Asset> GetFilesInFolder( AssetManagerAsset asset, string browseMode /* image or doc */ )
        {
            var physicalRootFolder = System.Web.HttpContext.Current.Server.MapPath( asset.Root );
            var physicalFolder = System.Web.HttpContext.Current.Server.MapPath( asset.FullDirectoryPath );

            var fileTypeWhiteList = "*.*";
            var fileList = new List<string>();
            var files = new List<Asset>();

            if ( browseMode == "image" )
            {
                string imageFileTypeWhiteList = GlobalAttributesCache.Get().GetValue( "ContentImageFiletypeWhitelist" );
                if ( imageFileTypeWhiteList.IsNotNullOrWhiteSpace() )
                {
                    fileTypeWhiteList = imageFileTypeWhiteList;
                }
            }

            // Directory.GetFiles doesn't support multiple patterns, so we'll do one at a time
            List<string> fileFilters = fileTypeWhiteList.Split( new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s = "*." + s.TrimStart( new char[] { '*', ' ' } ).TrimStart( '.' ) ) // ensure that the filter starts with '*.'
                .ToList();

            foreach ( var filter in fileFilters )
            {
                fileList.AddRange( Directory.GetFiles( physicalFolder, filter ).OrderBy( a => a ).ToList() );
            }

            var dir = new DirectoryInfo( physicalFolder );
            var fileInfoList = dir.GetFiles();


            foreach ( var filePath in fileList )
            {
                var file = new Asset();

                var fileInfo = new FileInfo( Path.Combine( physicalFolder, filePath ) );
                string fileName = Path.GetFileName( filePath ).Replace( "'", "&#39;" );
                string relativeFilePath = filePath.Replace( physicalRootFolder, string.Empty );
                string rootRelativePath = asset.Root.TrimEnd( '/', '\\' ) + "/" + relativeFilePath.TrimStart( '/', '\\' ).Replace( "\\", "/" );
                string thumbUrl = RockApp.Current.ResolveRockUrl( "~/api/FileBrowser/GetFileThumbnail?relativeFilePath=" + System.Web.HttpUtility.UrlEncode( rootRelativePath ) );
                string downloadUrl = RockApp.Current.ResolveRockUrl( rootRelativePath );

                file = new Asset
                {
                    AssetStorageProviderId = 0,
                    Name = fileInfo.Name,
                    Key = rootRelativePath,
                    Uri = downloadUrl,
                    Type = AssetType.File,
                    IconPath = thumbUrl,
                    LastModifiedDateTime = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length
                };

                files.Add( file );
            }

            return new AssetManagerGetFilesResultsBag<Asset>
            {
                Files = files,
                IsFolderRestricted = IsRestrictedFolder( asset.FullPath ),
                IsFolderUploadRestricted = IsUploadRestrictedFolder( asset.FullPath )
            };
        }

        /// <summary>
        /// Get a tree of all the asset storage providers
        /// </summary>
        /// <returns>A tree of all the asset storage providers</returns>
        private (List<TreeItemBag>, List<string> updatedExpandedFolders) GetAssetStorageProviders( List<string> expandedFolders, AssetManagerAsset selectedFolder )
        {
            var providers = AssetStorageProviderCache.All()
                .Where( a => a.EntityTypeId.HasValue && a.IsActive );
            var tree = new List<TreeItemBag>();
            var updatedExpandedFolders = new List<string>();

            foreach ( var provider in providers )
            {
                var component = provider.AssetStorageComponent;
                var rootFolder = component.GetRootFolder( provider.ToEntity() );
                var encryptedRootFolder = Rock.Security.Encryption.EncryptString( rootFolder );

                // If the selected folder is using this asset provider, then use its existing encrypted folder value instead or re-encrypting
                if ( selectedFolder != null && selectedFolder.ProviderId == provider.Id && selectedFolder.Root == rootFolder )
                {
                    encryptedRootFolder = selectedFolder.EncryptedRoot;
                }

                var providerBag = new AssetManagerTreeItemBag
                {
                    Text = provider.Name,
                    Value = $"{provider.Id},{encryptedRootFolder},,{true}",
                    IconCssClass = component.IconCssClass,
                    HasChildren = true,
                    UnencryptedRoot = rootFolder
                };

                if ( expandedFolders.Contains( $"{provider.Id},{rootFolder}" ) )
                {
                    var (children, exFolders) = GetChildrenOfAsset( ParseAssetKey( providerBag.Value ), expandedFolders );
                    providerBag.Children = children;
                    providerBag.ChildCount = children?.Count ?? 0;

                    if ( providerBag.ChildCount == 0 )
                    {
                        providerBag.HasChildren = false;
                    }
                    else
                    {
                        updatedExpandedFolders.Add( providerBag.Value );
                        updatedExpandedFolders.AddRange( exFolders );
                    }
                }

                tree.Add( providerBag );
            }

            return (tree, updatedExpandedFolders);
        }

        /// <summary>
        /// Get the child folders of the given parent folder asset.
        /// </summary>
        /// <param name="parentAsset">Parent of the children we want to get.</param>
        /// <param name="expandedFolders">A list of expanded folders that gets updated with the correct encrypted roots.</param>
        /// <returns>The child folders and a list of folders that got updated from the expandedFolders list.</returns>
        private (List<TreeItemBag> children, List<string> updatedExpandedFolders) GetChildrenOfAsset( AssetManagerAsset parentAsset, List<string> expandedFolders )
        {
            var tree = new List<TreeItemBag>();
            var updatedExpandedFolders = new List<string>();

            var (provider, component) = GetAssetStorageProvider( parentAsset.ProviderId.Value );

            if ( provider == null || component == null )
            {
                return (tree, updatedExpandedFolders);
            }

            var compontentRoot = component.GetRootFolder( provider.ToEntity() );
            compontentRoot = compontentRoot.TrimEnd( '/', '\\' ) + "/";

            // Cannot request assets from a root other than the configured one
            if ( compontentRoot != parentAsset.Root )
            {
                return (tree, updatedExpandedFolders);
            }

            var asset = new Asset { Type = AssetType.Folder, Key = parentAsset.FullPath.TrimStart( '/', '\\' ) ?? string.Empty };
            var folders = component.ListFoldersInFolder( provider.ToEntity(), asset );

            foreach ( Asset folder in folders )
            {
                // If there's a folder with a name that has slashes, exclude it because it causes issues with trying to load its children
                // and it's invalid according to our folder name rules
                if ( folder.Name.Contains( "/" ) )
                {
                    continue;
                }

                var folderBag = new AssetManagerTreeItemBag
                {
                    Text = folder.Name,
                    Value = $"{parentAsset.ProviderId},{parentAsset.EncryptedRoot},{Path.Combine( parentAsset.SubPath, folder.Name ).TrimEnd( '/', '\\' ) + "/"}",
                    IconCssClass = "fa fa-folder",
                    // Verifying if it has any children is slow, so we just say true and it gets fixed
                    // on the client when attempting to expand children
                    HasChildren = true,
                    UnencryptedRoot = parentAsset.Root
                };


                if ( expandedFolders?.Contains( $"{parentAsset.ProviderId},{folder.Key}" ) ?? false )
                {
                    var (children, exFolders) = GetChildrenOfAsset( ParseAssetKey( folderBag.Value ), expandedFolders );
                    folderBag.Children = children;
                    folderBag.ChildCount = children?.Count ?? 0;

                    if ( folderBag.ChildCount == 0 )
                    {
                        folderBag.HasChildren = false;
                    }
                    else
                    {
                        updatedExpandedFolders.Add( folderBag.Value );
                        updatedExpandedFolders.AddRange( exFolders );
                    }
                }

                tree.Add( folderBag );
            }

            return (tree, updatedExpandedFolders);
        }

        /// <summary>
        /// Recursively gets a list of every folder that is in the given directory.
        /// </summary>
        /// <param name="directoryPath">The folder we want to get the children of.</param>
        /// <param name="physicalRootFolder">The root folder where this list was started from.</param>
        /// <param name="excludedFolder">The name of a folder that should be excluded from the list (and its children).</param>
        /// <returns>A list of <see cref="ListItemBag"/> items of each child directory.</returns>
        private List<string> GetRecursiveFolders( string directoryPath, string physicalRootFolder, string excludedFolder )
        {
            // If this is a hidden folder, don't show it.
            if ( IsHiddenFolder( directoryPath ) || directoryPath == excludedFolder )
            {
                return new List<string>();
            }

            DirectoryInfo directoryInfo = new DirectoryInfo( directoryPath );
            string relativeFolderPath = directoryPath.Replace( physicalRootFolder, string.Empty );

            var folders = new List<string> { string.IsNullOrEmpty( relativeFolderPath ) ? "/" : relativeFolderPath.Replace( "\\", "/" ) };

            List<string> subDirectoryList = Directory.GetDirectories( directoryPath ).OrderBy( a => a ).ToList();

            foreach ( var subDirectoryPath in subDirectoryList )
            {
                folders.AddRange( GetRecursiveFolders( subDirectoryPath, physicalRootFolder, excludedFolder ) );
            }

            return folders;
        }

        /// <summary>
        /// Determines whether the given folder name is a valid folder name.
        /// </summary>
        /// <param name="folderName">Name of the rename folder.</param>
        /// <returns>True if folderName is valid, false otherwise</returns>
        private bool IsValidAssetFolderName( string folderName )
        {
            Regex regularExpression = new Regex( @"^[^*/><?\\\\|:,~]+$" );
            var isValid = regularExpression.IsMatch( folderName );

            var invalidChars = Path.GetInvalidPathChars().ToList();
            invalidChars.Add( '\\' );
            invalidChars.Add( '/' );
            invalidChars.Add( '~' );

            isValid = isValid && !( folderName.ToList().Any( a => invalidChars.Contains( a ) ) || folderName.StartsWith( ".." ) || folderName.EndsWith( "." ) );

            return isValid;
        }

        /// <summary>
        /// Whether the folder at the given path should be hidden from the file manager.
        /// </summary>
        /// <param name="localPathName"></param>
        /// <returns>True if the folder at the given path should be hidden from the file manager, otherwise false.</returns>
        private bool IsHiddenFolder( string localPathName )
        {
            var HiddenFolders = new List<string> { "Content\\ASM_Thumbnails" };
            return HiddenFolders.Any( a => localPathName.IndexOf( a, StringComparison.OrdinalIgnoreCase ) > -1 );
        }

        /// <summary>
        /// Whether the folder at the given path name is restricted from certain actions.
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns>True if the folder at the given path name is restricted from certain actions, otherwise false.</returns>
        private bool IsRestrictedFolder( string pathName )
        {
            var restrictedFolders = new List<string>()
            {
                "Bin",
                "App_Data",
                "App_Code",
                "App_Browsers",
                "Assets",
                "Blocks",
                "Content",
                "Plugins",
                "Scripts",
                "SqlServerTypes",
                "Styles",
                "Themes",
                "Webhooks"
            };

            pathName = pathName.TrimStart( '~' ).TrimStart( '/', '\\' ).TrimEnd( '/', '\\' );

            return restrictedFolders.Contains( pathName, StringComparer.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Whether the folder at the given path name is restricted from certain types of uploads.
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns>True if the folder at the given path name is restricted from certain types of uploads, otherwise false.</returns>
        private bool IsUploadRestrictedFolder( string pathName )
        {
            var restrictedFolders = new List<string>()
            {
                "Bin",
                "App_Code"
            };

            pathName = pathName.TrimStart( '~' ).TrimStart( '/', '\\' ).TrimEnd( '/', '\\' );

            return restrictedFolders.Any( a => pathName.StartsWith( a, StringComparison.OrdinalIgnoreCase ) );
        }

        #endregion

        #region Asset Storage Provider Picker

        /// <summary>
        /// Gets the asset storage providers that can be displayed in the asset storage provider picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the asset storage providers.</returns>
        [HttpPost]
        [Route( "AssetStorageProviderPickerGetAssetStorageProviders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [Rock.SystemGuid.RestActionGuid( "665EDE0C-1FEA-4421-B355-4D4F72B7E26E" )]
        public IActionResult AssetStorageProviderPickerGetAssetStorageProviders( [FromBody] AssetStorageProviderPickerGetAssetStorageProvidersOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var items = AssetStorageProviderCache.All()
                    .Where( a => a.EntityTypeId.HasValue && a.IsActive )
                    .OrderBy( a => a.Name )
                    .ToListItemBagList();

                return Ok( items );
            }
        }

        #endregion

        #region Attribute Matrix Editor

        /// <summary>
        /// Take the public edit values for a given matrix item and convert them to public viewing values.
        /// </summary>
        /// <param name="options">The options that describe the attributes and their public edit values.</param>
        /// <returns>The public edit values and the public viewing values.</returns>
        [HttpPost]
        [Route( "AttributeMatrixEditorNormalizeEditValue" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK )]
        [Rock.SystemGuid.RestActionGuid( "1B7BA1CB-6D3F-4DE7-AC02-EAAADF89C7ED" )]
        public IActionResult AttributeMatrixEditorNormalizeEditValue( [FromBody] AttributeMatrixEditorNormalizeEditValueOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var publicValues = options.Attributes.ToDictionary( a => a.Key, a =>
                {
                    var attr = AttributeCache.Get( a.Value.AttributeGuid );

                    var privateValue = PublicAttributeHelper.GetPrivateValue( attr, options.AttributeValues.GetValueOrDefault( a.Key, "" ) );
                    var publicValue = PublicAttributeHelper.GetPublicValueForView( attr, privateValue );
                    return publicValue;
                } );

                return Ok( new
                {
                    ViewValues = publicValues,
                    EditValues = options.AttributeValues
                } );
            }
        }

        #endregion

        #region Audit Detail

        /// <summary>
        /// Gets the audit details about the entity.
        /// </summary>
        /// <param name="options">The options that describe which entity to be audited.</param>
        /// <returns>A <see cref="EntityAuditBag"/> that contains the requested information.</returns>
        [HttpPost]
        [Route( "AuditDetailGetAuditDetails" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EntityAuditBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "714D83C9-96E4-49D7-81AF-2EED7D5CCD56" )]
        public IActionResult AuditDetailGetAuditDetails( [FromBody] AuditDetailGetAuditDetailsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                // Get the entity type identifier to use to lookup the entity.
                var entityType = EntityTypeCache.Get( options.EntityTypeGuid )?.GetEntityType();

                if ( entityType == null )
                {
                    return NotFound();
                }

                var entity = Reflection.GetIEntityForEntityType( entityType, options.EntityKey, rockContext ) as IModel;

                if ( entity == null )
                {
                    return NotFound();
                }

                // If the entity can be secured, ensure the person has access to it.
                if ( entity is ISecured securedEntity )
                {
                    var isAuthorized = securedEntity.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( entity, Security.Authorization.VIEW ) == true;

                    if ( !isAuthorized )
                    {
                        return Unauthorized();
                    }
                }

                return Ok( entity.GetEntityAuditBag() );
            }
        }

        #endregion

        #region Badge Component Picker

        /// <summary>
        /// Gets the badge components that can be displayed in the badge component picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the badge components.</returns>
        [HttpPost]
        [Route( "BadgeComponentPickerGetBadgeComponents" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "ABDFC10F-BCCC-4AF1-8DB3-88A26862485D" )]
        public IActionResult BadgeComponentPickerGetEntityTypes( [FromBody] BadgeComponentPickerGetBadgeComponentsOptionsBag options )
        {
            var componentsList = GetComponentListItems( "Rock.Badge.BadgeContainer, Rock", ( Component component ) =>
            {
                var badgeComponent = component as BadgeComponent;
                var entityType = EntityTypeCache.Get( options.EntityTypeGuid.GetValueOrDefault() )?.Name;

                return badgeComponent != null && badgeComponent.DoesApplyToEntityType( entityType );
            } );

            return Ok( componentsList );
        }

        #endregion

        #region Badge Control

        /// <summary>
        /// Get a rendered badge matching the given options
        /// </summary>
        /// <param name="options">The options that describe the badge to load.</param>
        /// <returns>The HTML of a specified badge.</returns>
        [HttpPost]
        [Route( "BadgeControlGetBadge" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( RenderedBadgeBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "D9840506-7251-4F41-A1B2-D3168FB3AFDA" )]
        public IActionResult BadgeControlGetBadge( [FromBody] BadgeControlGetBadgeOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeCache = EntityTypeCache.Get( options.EntityTypeGuid, rockContext );
                var entityType = entityTypeCache?.GetEntityType();
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                // Verify that we found the entity type.
                if ( entityType == null )
                {
                    return BadRequest( "Unknown entity type." );
                }

                // Load the entity and verify we got one.
                var entity = Rock.Reflection.GetIEntityForEntityType( entityType, options.EntityKey );

                if ( entity == null )
                {
                    return NotFound();
                }

                // If the entity can be secured, ensure the person has access to it.
                if ( entity is ISecured securedEntity )
                {
                    var isAuthorized = securedEntity.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( entity, Security.Authorization.VIEW ) == true;

                    if ( !isAuthorized )
                    {
                        return Unauthorized();
                    }
                }

                BadgeCache badge = BadgeCache.Get( options.BadgeTypeGuid );

                if ( badge == null || badge.EntityTypeId.Value != entityTypeCache.Id )
                {
                    return NotFound();
                }

                var isBadgeAuthorized = badge.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                    || grant?.IsAccessGranted( badge, Security.Authorization.VIEW ) == true;

                if ( !isBadgeAuthorized )
                {
                    return Unauthorized();
                }

                return Ok( badge.RenderBadge( entity ) );
            }
        }

        #endregion

        #region Badge List

        /// <summary>
        /// Get the rendered badge information for a specific entity.
        /// </summary>
        /// <param name="options">The options that describe which badges to render.</param>
        /// <returns>A collection of <see cref="RenderedBadgeBag"/> objects.</returns>
        [HttpPost]
        [Route( "BadgeListGetBadges" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( RenderedBadgeBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "34387B98-BF7E-4000-A28A-24EA08605285" )]
        public IActionResult BadgeListGetBadges( [FromBody] BadgeListGetBadgesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeCache = EntityTypeCache.Get( options.EntityTypeGuid, rockContext );
                var entityType = entityTypeCache?.GetEntityType();
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                // Verify that we found the entity type.
                if ( entityType == null )
                {
                    return BadRequest( "Unknown entity type." );
                }

                // Load the entity and verify we got one.
                var entity = Rock.Reflection.GetIEntityForEntityType( entityType, options.EntityKey );

                if ( entity == null )
                {
                    return NotFound();
                }

                // If the entity can be secured, ensure the person has access to it.
                if ( entity is ISecured securedEntity )
                {
                    var isAuthorized = securedEntity.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( entity, Security.Authorization.VIEW ) == true;

                    if ( !isAuthorized )
                    {
                        return Unauthorized();
                    }
                }

                List<BadgeCache> badges;

                // Load the list of badges that were requested or all badges
                // if no specific badges were requested.
                if ( options.BadgeTypeGuids != null && options.BadgeTypeGuids.Any() )
                {
                    badges = options.BadgeTypeGuids
                        .Select( g => BadgeCache.Get( g ) )
                        .Where( b => b != null )
                        .ToList();
                }
                else
                {
                    badges = BadgeCache.All()
                        .Where( b => !b.EntityTypeId.HasValue || b.EntityTypeId.Value == entityTypeCache.Id )
                        .ToList();
                }

                // Filter out any badges that don't apply to the entity or are not
                // authorized by the person to be viewed.
                badges = badges.Where( b => b.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( b, Security.Authorization.VIEW ) == true )
                    .ToList();

                // Render all the badges and then filter out any that are empty.
                var badgeResults = badges.Select( b => b.RenderBadge( entity ) )
                    .Where( b => b.Html.IsNotNullOrWhiteSpace() || b.JavaScript.IsNotNullOrWhiteSpace() )
                    .ToList();

                return Ok( badgeResults );
            }
        }

        #endregion

        #region Badge Picker

        /// <summary>
        /// Get the list of Badge types for use in a Badge Picker.
        /// </summary>
        /// <returns>A list of badge types.</returns>
        [HttpPost]
        [Route( "BadgePickerGetBadges" )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "6D50B8E4-985E-4AC6-B491-74B827108882" )]
        public IActionResult BadgePickerGetBadges( [FromBody] BadgePickerGetBadgesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var badges = BadgeCache.All().ToList();

                // Filter out any badges that don't apply to the entity or are not
                // authorized by the person to be viewed.
                var badgeList = badges.Where( b => b.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( b, Security.Authorization.VIEW ) == true )
                    .Select( b => new ListItemBag { Text = b.Name, Value = b.Guid.ToString() } )
                    .ToList();

                return Ok( badgeList );
            }
        }

        #endregion

        #region Binary File Picker

        /// <summary>
        /// Gets the binary files that can be displayed in the binary file picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the binary files.</returns>
        [HttpPost]
        [Route( "BinaryFilePickerGetBinaryFiles" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "9E5F190E-91FD-4E50-9F00-8B4F9DBD874C" )]
        public IActionResult BinaryFilePickerGetBinaryFiles( [FromBody] BinaryFilePickerGetBinaryFilesOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                // Check that user has access to view the BinaryFileType.
                var authorizedFileTypeGuids = GetAuthorizedBinaryFileTypes( rockContext ).Select( t => t.Guid );
                if ( !authorizedFileTypeGuids.Contains( options.BinaryFileTypeGuid ) )
                {
                    return NotFound();
                }

                var binaryFileTypeCache = BinaryFileTypeCache.Get( options.BinaryFileTypeGuid, rockContext );

                // For additional security we require an explicit grant rule on
                // the binary file type to enable this picker.
                if ( binaryFileTypeCache == null || grant?.IsAccessGranted( binaryFileTypeCache, Authorization.VIEW ) != true )
                {
                    return Unauthorized();
                }

                var items = new BinaryFileService( new RockContext() )
                    .Queryable()
                    .Where( f => f.BinaryFileType.Guid == options.BinaryFileTypeGuid && !f.IsTemporary )
                    .OrderBy( f => f.FileName )
                    .Select( t => new ListItemBag
                    {
                        Value = t.Guid.ToString(),
                        Text = t.FileName
                    } )
                    .ToList();

                return Ok( items );
            }
        }

        #endregion

        #region Binary File Type Picker

        /// <summary>
        /// Gets the binary file types that can be displayed in the binary file type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the binary file types.</returns>
        [HttpPost]
        [Route( "BinaryFileTypePickerGetBinaryFileTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "C93E5A06-82DE-4475-88B8-B173C03BFB50" )]
        public IActionResult BinaryFileTypePickerGetBinaryFileTypes( [FromBody] BinaryFileTypePickerGetBinaryFileTypesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var items = GetAuthorizedBinaryFileTypes( rockContext )
                    .OrderBy( f => f.Name )
                    .Select( t => new ListItemBag
                    {
                        Value = t.Guid.ToString(),
                        Text = t.Name
                    } )
                    .ToList();

                return Ok( items );
            }
        }

        /// <summary>
        /// Gets a list of <see cref="BinaryFileType"/>s that do not require view security and/or the current authenticated user is permitted to view.
        /// </summary>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <returns></returns>
        private List<BinaryFileType> GetAuthorizedBinaryFileTypes( RockContext rockContext )
        {
            var fileTypeQry = new BinaryFileTypeService( rockContext ).Queryable();
            var fileTypesWithoutViewSecurity = fileTypeQry.Where( t => !t.RequiresViewSecurity ).ToList();

            var person = GetPerson( rockContext );
            if ( person == null )
            {
                return fileTypesWithoutViewSecurity;
            }

            var fileTypesWithViewSecurity = fileTypeQry
                .Where( t => t.RequiresViewSecurity )
                .ToList()
                .Where( t => t.IsAuthorized( Authorization.VIEW, person ) )
                .ToList();

            return fileTypesWithViewSecurity.Concat( fileTypesWithoutViewSecurity ).ToList();
        }

        #endregion

        #region Block Template Picker

        /// <summary>
        /// Gets the templates that can be displayed in the block template picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="BlockTemplatePickerGetBlockTemplatesResultsBag"/> objects that represent the block templates.</returns>
        [HttpPost]
        [Route( "BlockTemplatePickerGetBlockTemplates" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<BlockTemplatePickerGetBlockTemplatesResultsBag> ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "f52a9356-9f05-42f4-a568-a2fc4baef2de" )]
        public IActionResult BlockTemplatePickerGetBlockTemplates( [FromBody] BlockTemplatePickerGetBlockTemplatesOptionsBag options )
        {
            if ( !options.TemplateBlockValueGuid.HasValue )
            {
                return BadRequest( "Provide a Template Block Guid" );
            }

            var items = new List<BlockTemplatePickerGetBlockTemplatesResultsBag>();
            var blockTemplateDefinedValue = DefinedValueCache.Get( options.TemplateBlockValueGuid.Value );

            if ( blockTemplateDefinedValue != null )
            {
                var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.TEMPLATE );
                definedType.DefinedValues.LoadAttributes();

                foreach ( var item in definedType.DefinedValues )
                {
                    if ( item.GetAttributeValue( "TemplateBlock" ).AsGuid() == blockTemplateDefinedValue.Guid )
                    {
                        var iconGuid = item.GetAttributeValue( "Icon" ).AsGuid();
                        var imageUrl = FileUrlHelper.GetImageUrl( iconGuid );

                        items.Add( new BlockTemplatePickerGetBlockTemplatesResultsBag { Guid = item.Guid, Name = item.Value, IconUrl = RockRequestContext.ResolveRockUrl( imageUrl ), Template = item.Description } );
                    }
                }

                return Ok( items );
            }

            return BadRequest( "Provided GUID does not match a Template Block" );
        }

        #endregion

        #region Campus Picker

        /// <summary>
        /// Gets the campuses that can be displayed in the campus picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="CampusPickerItemBag"/> objects that represent the campuses.</returns>
        [HttpPost]
        [Route( "CampusPickerGetCampuses" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CampusPickerItemBag ) )]
        [Rock.SystemGuid.RestActionGuid( "3D2E0AF9-9E1A-47BD-A1C5-008B6D2A5B22" )]
        public IActionResult CampusPickerGetCampuses( [FromBody] CampusPickerGetCampusesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                return Ok( GetCampuses( options, rockContext ) );
            }
        }

        private List<CampusPickerItemBag> GetCampuses( CampusPickerGetCampusesOptionsBag options, RockContext rockContext )
        {
            var items = new CampusService( rockContext )
                .Queryable()
                .OrderBy( f => f.Order )
                .ThenBy( f => f.Name )
                .Select( c => new CampusPickerItemBag
                {
                    Value = c.Guid.ToString(),
                    Text = c.Name,
                    IsActive = c.IsActive ?? true,
                    CampusStatus = c.CampusStatusValue.Guid,
                    CampusType = c.CampusTypeValue.Guid
                } )
                .ToList();

            return items;
        }

        #endregion

        #region Campus Account Amount Picker

        /// <summary>
        /// Gets the accounts that can be displayed in the campus account amount picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="CampusAccountAmountPickerGetAccountsResultItemBag"/> objects that represent the campus accounts.</returns>
        [HttpPost]
        [Route( "CampusAccountAmountPickerGetAccounts" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<CampusAccountAmountPickerGetAccountsResultItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "9833fcd3-30cf-4bab-840a-27ee497ebfb8" )]
        public IActionResult CampusAccountAmountPickerGetAccounts( [FromBody] CampusAccountAmountPickerGetAccountsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                IQueryable<FinancialAccount> accountsQry;
                var financialAccountService = new FinancialAccountService( rockContext );

                if ( options.SelectableAccountGuids.Any() )
                {
                    accountsQry = financialAccountService.GetByGuids( options.SelectableAccountGuids );
                }
                else
                {
                    accountsQry = financialAccountService.Queryable();
                }

                accountsQry = accountsQry.Where( f =>
                    f.IsActive &&
                    f.IsPublic.HasValue &&
                    f.IsPublic.Value &&
                    ( f.StartDate == null || f.StartDate <= RockDateTime.Today ) &&
                    ( f.EndDate == null || f.EndDate >= RockDateTime.Today ) )
                .OrderBy( f => f.Order );

                var accountsList = accountsQry.AsNoTracking().ToList();

                string accountHeaderTemplate = options.AccountHeaderTemplate;
                if ( accountHeaderTemplate.IsNullOrWhiteSpace() )
                {
                    accountHeaderTemplate = "{{ Account.PublicName }}";
                }

                if ( options.OrderBySelectableAccountsIndex )
                {
                    accountsList = accountsList.OrderBy( x => options.SelectableAccountGuids.IndexOf( x.Guid ) ).ToList();
                }

                var items = new List<CampusAccountAmountPickerGetAccountsResultItemBag>();
                var campuses = CampusCache.All();

                foreach ( var account in accountsList )
                {
                    var mergeFields = LavaHelper.GetCommonMergeFields( null, null, new CommonMergeFieldsOptions() );
                    mergeFields.Add( "Account", account );
                    var accountAmountLabel = accountHeaderTemplate.ResolveMergeFields( mergeFields );
                    items.Add( new CampusAccountAmountPickerGetAccountsResultItemBag
                    {
                        Name = accountAmountLabel,
                        Value = account.Guid
                    } );
                }

                return Ok( items );
            }
        }

        #endregion

        #region Captcha

        /// <summary>
        /// Gets the configuration data to use when rendering the Captcha control.
        /// </summary>
        [HttpPost]
        [Route( "CaptchaControlGetConfiguration" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CaptchaControlConfigurationBag ) )]
        [Rock.SystemGuid.RestActionGuid( "9e066058-13d9-4b4d-8457-07ba8e2cacd3" )]
        public IActionResult CaptchaControlGetConfiguration()
        {
            var bag = new CaptchaControlConfigurationBag()
            {
                SiteKey = Rock.Web.SystemSettings.GetValue( Rock.SystemKey.SystemSetting.CAPTCHA_SITE_KEY )
            };

            return Ok( bag );
        }

        /// <summary>
        /// Checks a captcha token to see if it is valid.
        /// </summary>
        /// <param name="options">The options that contain the information to be validated.</param>
        [HttpPost]
        [Route( "CaptchaControlValidateToken" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CaptchaControlTokenValidateTokenResultBag ) )]
        [Rock.SystemGuid.RestActionGuid( "8f373592-d745-4d69-944a-729e15c3f941" )]
        public IActionResult CaptchaControlValidateToken( [FromBody] CaptchaControlValidateTokenOptionsBag options )
        {
            var api = new CloudflareApi();

            var isTokenValid = api.IsTurnstileTokenValid( options.Token );

            var result = new CaptchaControlTokenValidateTokenResultBag()
            {
                IsTokenValid = isTokenValid
            };

            return Ok( result );
        }

        #endregion

        #region Categorized Value Picker

        /// <summary>
        /// Gets the child items that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>An object that contains the individual items.</returns>
        [HttpPost]
        [Route( "CategorizedValuePickerGetTree" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CategorizedValuePickerGetTreeResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "9294f070-e8c8-48da-bd50-076f26200d75" )]
        public IActionResult CategorizedValuePickerGetTree( [FromBody] CategorizedValuePickerGetTreeOptionsBag options )
        {
            // NO Parent -> get roots using DefinedTypeGuid
            // Parent -> get children of ParentGuid
            // Eliminate values not in the LimitTo list

            if ( options.DefinedTypeGuid == null )
            {
                return BadRequest( "Please provide a Defined Type GUID" );
            }

            // Get the Defined Type and associated values.
            var definedType = DefinedTypeCache.Get( options.DefinedTypeGuid );

            if ( definedType == null || !definedType.IsActive )
            {
                return BadRequest( "Please provide a valid Defined Type GUID" );
            }

            if ( !definedType.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var definedValueService = new DefinedValueService( rockContext );
                var definedValues = definedValueService.GetByDefinedTypeGuid( options.DefinedTypeGuid )
                    .Where( x => x.IsActive )
                    .OrderBy( x => x.Order )
                    .ToList();

                // Filter the selectable values.
                if ( options.OnlyIncludeGuids != null && options.OnlyIncludeGuids.Any() )
                {
                    definedValues = definedValues.Where( x => options.OnlyIncludeGuids.Contains( x.Guid ) ).ToList();
                }

                if ( !definedValues.Any() )
                {
                    return NotFound();
                }

                // Get a list of the Categories associated with the Defined Values.
                var categories = new Dictionary<int, Category>();
                var definedValueCategoryIdList = new List<int>();

                foreach ( var definedValue in definedValues )
                {
                    if ( definedValue.CategoryId != null )
                    {
                        if ( !definedValueCategoryIdList.Contains( definedValue.CategoryId.Value ) )
                        {
                            definedValueCategoryIdList.Add( definedValue.CategoryId.Value );
                        }
                    }
                }

                // Retrieve the Category details, including any parent categories required to build the selection tree.
                var categoryService = new CategoryService( rockContext );

                foreach ( var categoryId in definedValueCategoryIdList )
                {
                    // If this category already exists in the categories list, ignore it as an ancestor of a previous category.
                    if ( categories.ContainsKey( categoryId ) )
                    {
                        continue;
                    }

                    var ancestors = categoryService.GetAllAncestors( categoryId ).ToList();
                    foreach ( var ancestor in ancestors )
                    {
                        if ( !categories.ContainsKey( ancestor.Id ) )
                        {
                            categories.Add( ancestor.Id, ancestor );
                        }
                    }
                }

                var categoryItems = new List<CategorizedValuePickerNodeBag>();

                // Create a selection tree structure from the Categories.
                // Categories are created with a placeholder label which will be replaced by applying the naming rules.
                foreach ( var category in categories.Values )
                {
                    var listItem = new CategorizedValuePickerNodeBag
                    {
                        Value = category.Guid.ToString(),
                        Text = category.Name,
                        ChildCategories = new List<CategorizedValuePickerNodeBag>(),
                        ChildValues = new List<CategorizedValuePickerNodeBag>()
                    };

                    categoryItems.Add( listItem );
                }

                var root = new CategorizedValuePickerNodeBag
                {
                    Value = null,
                    Text = definedType.Name,
                    ChildCategories = new List<CategorizedValuePickerNodeBag>(),
                    ChildValues = new List<CategorizedValuePickerNodeBag>()
                };

                // Go through the categories and add child categories as children of their parents
                foreach ( var category in categories.Values )
                {
                    var listItem = categoryItems.Find( c => c.Value == category.Guid.ToString() );

                    // No parent? Throw it at the root of the list
                    if ( category.ParentCategory == null )
                    {
                        root.ChildCategories.Add( listItem );
                    }
                    // Has a parent. Add it as a child of its parent
                    else
                    {
                        var parent = categoryItems.Find( c => c.Value == category.ParentCategory.Guid.ToString() );

                        parent.ChildCategories.Add( listItem );
                    }
                }

                // Go through the defined values and add them as children of their categories
                foreach ( var definedValue in definedValues )
                {
                    var listItem = new CategorizedValuePickerNodeBag
                    {
                        Value = definedValue.Guid.ToString(),
                        Text = definedValue.Value
                    };

                    // No category? Throw it at the root of the list
                    if ( definedValue.Category == null )
                    {
                        AddDefinedValueToCategoryAndChildCategories( listItem, root );
                    }
                    // Has a category. Add it as a child of its category
                    else
                    {
                        var category = categoryItems.Find( c => c.Value == definedValue.Category.Guid.ToString() );
                        AddDefinedValueToCategoryAndChildCategories( listItem, category );
                    }
                }

                return Ok( new CategorizedValuePickerGetTreeResultsBag
                {
                    Tree = root,
                    DefinedType = definedType.Name
                } );
            }
        }

        /// <summary>
        /// Adds the defined value to its category and all child categories of that category. It's added to the children
        /// to facilitate the picker showing values from ancestors.
        /// </summary>
        /// <param name="definedValue">The defined value.</param>
        /// <param name="category">The category node.</param>
        private void AddDefinedValueToCategoryAndChildCategories( CategorizedValuePickerNodeBag definedValue, CategorizedValuePickerNodeBag category )
        {
            category.ChildValues.Add( definedValue );
            foreach ( var childCat in category.ChildCategories )
            {
                AddDefinedValueToCategoryAndChildCategories( definedValue, childCat );
            }
        }

        #endregion

        #region Category Picker

        private static readonly Regex QualifierValueLookupRegex = new Regex( "^{EL:((?:[a-f\\d]{8})-(?:[a-f\\d]{4})-(?:[a-f\\d]{4})-(?:[a-f\\d]{4})-(?:[a-f\\d]{12})):((?:[a-f\\d]{8})-(?:[a-f\\d]{4})-(?:[a-f\\d]{4})-(?:[a-f\\d]{4})-(?:[a-f\\d]{12}))}$", RegexOptions.IgnoreCase );

        /// <summary>
        /// Gets the child items that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A collection of view models that represent the tree items.</returns>
        [HttpPost]
        [Route( "CategoryPickerChildTreeItems" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "A1D07211-6C50-463B-98ED-1622DC4D73DD" )]
        public IActionResult CategoryPickerChildTreeItems( [FromBody] CategoryPickerChildTreeItemsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                var items = clientService.GetCategorizedTreeItems( new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.GetCategorizedItems,
                    EntityTypeGuid = options.EntityTypeGuid,
                    EntityTypeQualifierColumn = options.EntityTypeQualifierColumn,
                    EntityTypeQualifierValue = GetQualifierValueLookupResult( options.EntityTypeQualifierValue, rockContext ),
                    IncludeUnnamedEntityItems = options.IncludeUnnamedEntityItems,
                    IncludeCategoriesWithoutChildren = options.IncludeCategoriesWithoutChildren,
                    DefaultIconCssClass = options.DefaultIconCssClass,
                    IncludeInactiveItems = options.IncludeInactiveItems,
                    ItemFilterPropertyName = options.ItemFilterPropertyName,
                    ItemFilterPropertyValue = options.ItemFilterPropertyValue,
                    LazyLoad = options.LazyLoad,
                    SecurityGrant = grant
                } );

                return Ok( items );
            }
        }

        /// <summary>
        /// Checks if the qualifier value is a lookup and if so translate it to the
        /// identifier from the unique identifier. Otherwise returns the original
        /// value.
        /// </summary>
        /// <remarks>
        /// At some point this needs to be moved into a ClientService layer, but
        /// I'm not sure where since it isn't related to any one service.
        /// </remarks>
        /// <param name="value">The value to be translated.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>The qualifier value to use.</returns>
        private static string GetQualifierValueLookupResult( string value, RockContext rockContext )
        {
            if ( value == null )
            {
                return null;
            }

            var m = QualifierValueLookupRegex.Match( value );

            if ( m.Success )
            {
                int? id = null;

                if ( Guid.TryParse( m.Groups[1].Value, out var g1 ) && Guid.TryParse( m.Groups[2].Value, out var g2 ) )
                {
                    id = Rock.Reflection.GetEntityIdForEntityType( g1, g2, rockContext );
                }

                return id?.ToString() ?? "0";
            }
            else
            {
                return value;
            }
        }

        #endregion

        #region Component Picker

        /// <summary>
        /// Gets the components that can be displayed in the component picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the components.</returns>
        [HttpPost]
        [Route( "ComponentPickerGetComponents" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "75DA0671-38E2-4FF9-B334-CC0C88B559D0" )]
        public IActionResult ComponentPickerGetEntityTypes( [FromBody] ComponentPickerGetComponentsOptionsBag options )
        {
            var componentsList = GetComponentListItems( options.ContainerType );

            return Ok( componentsList );
        }

        #endregion

        #region Connection Request Picker

        /// <summary>
        /// Gets the data views and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which data views to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent a tree of data views.</returns>
        [HttpPost]
        [Route( "ConnectionRequestPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "5316914b-cf47-4dac-9e10-71767fdf1eb9" )]
        public IActionResult ConnectionRequestPickerGetChildren( [FromBody] ConnectionRequestPickerGetChildrenOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( ConnectionRequestPickerSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                string service = null;

                /*
                 * Determine what type of resource the GUID we received is so we know what types of
                 * children to query for.
                 */
                if ( options.ParentGuid == null )
                {
                    // Get the root Connection Types
                    service = "type";
                }
                else
                {
                    var conOpp = new ConnectionOpportunityService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( op => op.Guid == options.ParentGuid )
                        .ToList()
                        .Where( op => op.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( op, Security.Authorization.VIEW ) == true );

                    if ( conOpp.Any() )
                    {
                        // Get the Connection Requests
                        service = "request";
                    }
                    else
                    {
                        var conType = new ConnectionTypeService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( t => t.Guid == options.ParentGuid )
                            .ToList()
                            .Where( t => t.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( t, Security.Authorization.VIEW ) == true );

                        if ( conType.Any() )
                        {
                            // Get the Connection Opportunities
                            service = "opportunity";
                        }
                    }
                }

                /*
                 * Fetch the children
                 */
                var list = new List<TreeItemBag>();

                if ( service == "type" )
                {
                    // Get the Connection Types
                    var connectionTypes = new ConnectionTypeService( rockContext )
                        .Queryable().AsNoTracking()
                        .OrderBy( ct => ct.Name )
                        .ToList()
                        .Where( ct => ct.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( ct, Security.Authorization.VIEW ) == true );

                    foreach ( var connectionType in connectionTypes )
                    {
                        var item = new TreeItemBag();
                        item.Value = connectionType.Guid.ToString();
                        item.Text = connectionType.Name;
                        item.HasChildren = connectionType.ConnectionOpportunities.Any();
                        item.IconCssClass = connectionType.IconCssClass;
                        list.Add( item );
                    }
                }
                else if ( service == "opportunity" )
                {
                    // Get the Connection Opportunities
                    var opportunities = new ConnectionOpportunityService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( op => op.ConnectionType.Guid == options.ParentGuid )
                        .OrderBy( op => op.Name )
                        .ToList()
                        .Where( op => op.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( op, Security.Authorization.VIEW ) == true );

                    foreach ( var opportunity in opportunities )
                    {
                        var item = new TreeItemBag();
                        item.Value = opportunity.Guid.ToString();
                        item.Text = opportunity.Name;
                        item.HasChildren = opportunity.ConnectionRequests
                            .Any( r =>
                                r.ConnectionState == ConnectionState.Active ||
                                r.ConnectionState == ConnectionState.FutureFollowUp );
                        item.IconCssClass = opportunity.IconCssClass;
                        list.Add( item );
                    }
                }
                else if ( service == "request" )
                {
                    var requests = new ConnectionRequestService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( r =>
                            r.ConnectionOpportunity.Guid == options.ParentGuid &&
                            r.PersonAlias != null &&
                            r.PersonAlias.Person != null )
                        .OrderBy( r => r.PersonAlias.Person.LastName )
                        .ThenBy( r => r.PersonAlias.Person.NickName )
                        .ToList()
                        .Where( op => op.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( op, Security.Authorization.VIEW ) == true );

                    foreach ( var request in requests )
                    {
                        var item = new TreeItemBag();
                        item.Value = request.Guid.ToString();
                        item.Text = request.PersonAlias.Person.FullName;
                        item.HasChildren = false;
                        item.IconCssClass = "fa fa-user";
                        list.Add( item );
                    }
                }
                else
                {
                    // service type wasn't set, so we don't know where to look
                    return NotFound();
                }

                return Ok( list );
            }
        }

        #endregion

        #region Content Channel Item Picker

        /// <summary>
        /// Gets the content channel items that can be displayed in the content channel item picker.
        /// </summary>
        /// <returns>A collection of <see cref="ListItemBag"/> objects that represent the content channel items.</returns>
        [HttpPost]
        [Route( "ContentChannelItemPickerGetContentChannels" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "2182388d-ccae-44df-a0de-597b8d123666" )]
        public IActionResult ContentChannelItemPickerGetContentChannels()
        {
            var contentChannels = ContentChannelCache.All()
                .Where( cc => cc.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                .OrderBy( cc => cc.Name )
                .Select( cc => new ListItemBag { Text = cc.Name, Value = cc.Guid.ToString() } )
                .ToList();

            return Ok( contentChannels );
        }

        /// <summary>
        /// Gets the content channel items that can be displayed in the content channel item picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the content channel items.</returns>
        [HttpPost]
        [Route( "ContentChannelItemPickerGetContentChannelItems" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "e1f6ad6b-c3f5-4a1a-abc2-46726732daee" )]
        public IActionResult ContentChannelItemPickerGetContentChannelItems( [FromBody] ContentChannelItemPickerGetContentChannelItemsOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var contentChannel = new ContentChannelService( rockContext ).GetInclude( options.ContentChannelGuid, cc => cc.Items );

                if ( contentChannel == null )
                {
                    return BadRequest( "Invalid content channel." );
                }

                if ( !contentChannel.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) && grant?.IsAccessGranted( contentChannel, Security.Authorization.VIEW ) != true )
                {
                    return Unauthorized();
                }

                return Ok( ContentChannelItemPickerGetContentChannelItemsForContentChannel( contentChannel, options.ExcludeContentChannelItems ) );
            }
        }

        /// <summary>
        /// Gets the content channel items and content channel information based on a selected content channel item.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>All the data for the selected role, selected type, and all of the content channel items</returns>
        [HttpPost]
        [Route( "ContentChannelItemPickerGetAllForContentChannelItem" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ContentChannelItemPickerGetAllForContentChannelItemResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "ef6d055f-38b1-4225-b95f-cfe703f4d425" )]
        public IActionResult ContentChannelItemPickerGetAllForContentChannelItem( [FromBody] ContentChannelItemPickerGetAllForContentChannelItemOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                List<Guid> excludeContentChannelItems = options.ExcludeContentChannelItems;

                var contentChannelItemService = new Rock.Model.ContentChannelItemService( rockContext );
                var contentChannelItem = contentChannelItemService.Queryable()
                    .Where( cc => cc.Guid == options.ContentChannelItemGuid )
                    .First();

                if ( contentChannelItem == null || !contentChannelItem.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                {
                    return BadRequest( "Invalid content channel item." );
                }

                var contentChannel = contentChannelItem.ContentChannel;

                if ( !contentChannel.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) && grant?.IsAccessGranted( contentChannel, Security.Authorization.VIEW ) != true )
                {
                    return Unauthorized();
                }

                var contentChannelItems = ContentChannelItemPickerGetContentChannelItemsForContentChannel( contentChannel, excludeContentChannelItems );

                return Ok( new ContentChannelItemPickerGetAllForContentChannelItemResultsBag
                {
                    SelectedContentChannelItem = new ListItemBag { Text = contentChannelItem.Title, Value = contentChannelItem.Guid.ToString() },
                    SelectedContentChannel = new ListItemBag { Text = contentChannel.Name, Value = contentChannel.Guid.ToString() },
                    ContentChannelItems = contentChannelItems
                } );
            }
        }

        /// <summary>
        /// Gets the content channel items that can be displayed in the content channel item picker.
        /// </summary>
        /// <param name="contentChannel">The content channel to get the items from.</param>
        /// <param name="excludeContentChannelItems">Do not include these items in the result</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the content channel items.</returns>
        private List<ListItemBag> ContentChannelItemPickerGetContentChannelItemsForContentChannel( ContentChannel contentChannel, List<Guid> excludeContentChannelItems )
        {
            var contentChannelitems = contentChannel.Items
                .Where( cci => !excludeContentChannelItems.Contains( cci.Guid ) )
                .OrderBy( a => a.Title )
                .Select( cci => new ListItemBag { Text = cci.Title, Value = cci.Guid.ToString() } )
                .ToList();

            return contentChannelitems;
        }

        #endregion

        #region Currency Box

        /// <summary>
        /// Gets the currency info for the currency box matching the given currency code defined value Guid.
        /// </summary>
        /// <param name="options">The options that describe how the currency will be displayed.</param>
        /// <returns>The currency symbol and decimal places.</returns>
        [HttpPost]
        [Route( "CurrencyBoxGetCurrencyInfo" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( CurrencyBoxGetCurrencyInfoResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "6E8D0B48-EB88-4028-B03F-064A690902D4" )]
        public IActionResult CurrencyBoxGetCurrencyInfo( [FromBody] CurrencyBoxGetCurrencyInfoOptionsBag options )
        {
            Guid currencyCodeGuid = options.CurrencyCodeGuid;
            RockCurrencyCodeInfo currencyInfo = null;

            if ( !currencyCodeGuid.IsEmpty() )
            {
                var currencyCodeDefinedValueCache = DefinedValueCache.Get( currencyCodeGuid );
                if ( currencyCodeDefinedValueCache != null )
                {
                    currencyInfo = new RockCurrencyCodeInfo( currencyCodeDefinedValueCache.Id );
                }
            }

            if ( currencyInfo == null )
            {
                currencyInfo = new RockCurrencyCodeInfo();
            }

            return Ok( new CurrencyBoxGetCurrencyInfoResultsBag
            {
                Symbol = currencyInfo.Symbol,
                DecimalPlaces = currencyInfo.DecimalPlaces
            } );
        }

        #endregion

        #region Data Filter

        /// <summary>
        /// Gets the formatted string that describes the data filter from the
        /// selection values.
        /// </summary>
        /// <param name="options">The options that describe the filter and selection.</param>
        /// <returns>A string of text.</returns>
        [HttpPost]
        [Route( "DataFilterFormatSelection" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [Rock.SystemGuid.RestActionGuid( "149fcd94-cd27-4017-9d4b-a1bc39e2d575" )]
        public IActionResult DataFilterFormatSelection( [FromBody] DataFilterFormatSelectionOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var componentData = options.ComponentData.FromJsonOrNull<Dictionary<string, string>>() ?? new Dictionary<string, string>();

                if ( options.EntityTypeGuid == Guid.Empty || options.FilterTypeGuid == Guid.Empty )
                {
                    return BadRequest( "Invalid request." );
                }

                var filterEntityType = EntityTypeCache.Get( options.FilterTypeGuid, rockContext );
                var entityType = EntityTypeCache.Get( options.EntityTypeGuid, rockContext );

                if ( filterEntityType == null )
                {
                    return BadRequest( "Invalid request." );
                }

                // We have to check access on the EntityType record because the
                // component is not an IEntity so it will not work.
                if ( !grant.IsAccessGranted( filterEntityType, Security.Authorization.VIEW ) )
                {
                    return BadRequest( "Security grant token is not valid." );
                }

                var filterComponent = Rock.Reporting.DataFilterContainer.GetComponent( filterEntityType.Name );

                if ( filterComponent == null )
                {
                    return BadRequest( "Invalid request." );
                }

                if ( !filterComponent.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                {
                    return BadRequest( "Not authorized to access this filter." );
                }

                var selection = filterComponent.GetSelectionFromObsidianComponentData( entityType.GetEntityType(), componentData, rockContext, RockRequestContext );

                return Ok( filterComponent.FormatSelection( entityType.GetEntityType(), selection ) );
            }
        }

        #endregion

        #region Data View Picker

        /// <summary>
        /// Gets the data views and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which data views to load.</param>
        /// <returns>A collection of <see cref="TreeItemBag"/> objects that represent a tree of data views.</returns>
        [HttpPost]
        [Route( "DataViewPickerGetDataViews" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "1E079A57-9B44-4365-9C9C-2383A9A3F45B" )]
        public IActionResult DataViewPickerGetDataViews( [FromBody] DataViewPickerGetDataViewsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                Func<DataView, bool> filterMethod = null;
                if ( options.DisplayPersistedOnly )
                {
                    filterMethod = d => d.IsPersisted();
                }

                var items = clientService.GetCategorizedTreeItems( new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.GetCategorizedItems,
                    EntityTypeGuid = EntityTypeCache.Get<Rock.Model.DataView>().Guid,
                    IncludeUnnamedEntityItems = options.IncludeUnnamedEntityItems,
                    IncludeCategoriesWithoutChildren = options.IncludeCategoriesWithoutChildren,
                    DefaultIconCssClass = options.DefaultIconCssClass,
                    ItemFilterPropertyName = options.EntityTypeGuidFilter.HasValue ? "EntityTypeId" : null,
                    ItemFilterPropertyValue = options.EntityTypeGuidFilter.HasValue ? EntityTypeCache.GetId( options.EntityTypeGuidFilter.Value ).ToString() : "",
                    LazyLoad = options.LazyLoad,
                    SecurityGrant = grant
                }, filterMethod );

                return Ok( items );
            }
        }

        #endregion

        #region Defined Value Editor

        /// <summary>
        /// Get the attributes for the given Defined Type
        /// </summary>
        /// <param name="options">The options needed to find the attributes for the defined type</param>
        /// <returns>A list of attributes in a form the Attribute Values Container can use</returns>
        [HttpPost]
        [Route( "DefinedValueEditorGetAttributes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( DefinedValueEditorGetAttributesResultsBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "E2601583-94D5-4C21-96FA-309B9FB7E11F" )]
        public IActionResult DefinedValueEditorGetAttributes( DefinedValueEditorGetAttributesOptionsBag options )
        {
            if ( RockRequestContext.CurrentPerson == null )
            {
                return Unauthorized();
            }

            var definedType = DefinedTypeCache.Get( options.DefinedTypeGuid );

            if ( definedType == null || !definedType.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
            {
                return Unauthorized();
            }

            var definedValue = new DefinedValue
            {
                Id = 0,
                DefinedTypeId = definedType.Id
            };

            definedValue.LoadAttributes();

            var attributes = definedValue.Attributes.ToDictionary( a => a.Key, a =>
            {
                return PublicAttributeHelper.GetPublicAttributeForEdit( a.Value );
            } );

            var defaultValues = definedValue.Attributes.ToDictionary( a => a.Key, a =>
            {
                var config = a.Value.ConfigurationValues;
                var fieldType = a.Value.FieldType.Field;
                return fieldType.GetPublicEditValue( a.Value.DefaultValue, config );
            } );

            return Ok( new DefinedValueEditorGetAttributesResultsBag
            {
                Attributes = attributes,
                DefaultValues = defaultValues
            } );
        }

        /// <summary>
        /// Save a new Defined Value.
        /// </summary>
        /// <param name="options">The options the new defined value.</param>
        /// <returns>A <see cref="ListItemBag"/> representing the new defined value.</returns>
        [HttpPost]
        [Route( "DefinedValueEditorSaveNewValue" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "E1AB17E0-CF28-4032-97A8-2A4279C5815A" )]
        public IActionResult DefinedValueEditorSaveNewValue( DefinedValueEditorSaveNewValueOptionsBag options )
        {
            if ( RockRequestContext.CurrentPerson == null )
            {
                return Unauthorized();
            }

            var securityGrant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( options.DefinedTypeGuid );
                var definedValue = new DefinedValue
                {
                    Id = 0,
                    DefinedTypeId = definedType.Id,
                    IsSystem = false,
                    Value = options.Value,
                    Description = options.Description
                };

                if ( securityGrant?.IsAccessGranted( definedValue, Authorization.EDIT ) != true )
                {
                    return Unauthorized();
                }

                DefinedValueService definedValueService = new DefinedValueService( rockContext );
                var orders = definedValueService.Queryable()
                    .Where( d => d.DefinedTypeId == definedType.Id )
                    .Select( d => d.Order )
                    .ToList();

                definedValue.Order = orders.Any() ? orders.Max() + 1 : 0;

                // Assign Attributes
                Attribute.Helper.LoadAttributes( definedValue );

                foreach ( KeyValuePair<string, AttributeValueCache> attr in definedValue.AttributeValues )
                {
                    definedValue.AttributeValues[attr.Key].Value = options.AttributeValues.GetValueOrNull( attr.Key );
                }

                if ( !definedValue.IsValid )
                {
                    return InternalServerError();
                }

                // Save the new value
                rockContext.WrapTransaction( () =>
                {
                    if ( definedValue.Id.Equals( 0 ) )
                    {
                        definedValueService.Add( definedValue );
                    }

                    rockContext.SaveChanges();

                    definedValue.SaveAttributeValues( rockContext );
                } );

                // Update the attribute configuration if requested.
                var updateAttribute = options.UpdateAttributeGuid.HasValue
                    ? AttributeCache.Get( options.UpdateAttributeGuid.Value )
                    : null;

                if ( updateAttribute?.FieldType?.Field is DefinedValueFieldType )
                {
                    var needSave = DefinedValueFieldType.AddValueToAttributeConfiguration( updateAttribute.Id, definedValue.Id, rockContext );

                    if ( needSave )
                    {
                        rockContext.SaveChanges();
                    }
                }

                return Ok( new ListItemBag { Text = definedValue.Value, Value = definedValue.Guid.ToString() } );
            }
        }

        #endregion

        #region Defined Value Picker

        /// <summary>
        /// Gets the defined values and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which defined values to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent a tree of defined values.</returns>
        [HttpPost]
        [Route( "DefinedValuePickerGetDefinedValues" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "1E4A1812-8A2C-4266-8F39-3004C1DEBC9F" )]
        public IActionResult DefinedValuePickerGetDefinedValues( DefinedValuePickerGetDefinedValuesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var definedType = DefinedTypeCache.Get( options.DefinedTypeGuid );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                if ( definedType == null || !definedType.IsAuthorized( Rock.Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                {
                    return NotFound();
                }

                var definedValues = definedType.DefinedValues
                    .Where( v => ( v.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( v, Security.Authorization.VIEW ) == true )
                        && ( options.IncludeInactive || v.IsActive ) )
                    .OrderBy( v => v.Order )
                    .ThenBy( v => v.Value )
                    .Select( v => new ListItemBag
                    {
                        Value = v.Guid.ToString(),
                        Text = v.Value
                    } )
                    .ToList();

                return Ok( definedValues );
            }
        }

        #endregion

        #region Email Editor

        /// <summary>
        /// Creates an email section.
        /// </summary>
        /// <param name="options">The email section to create.</param>
        /// <returns>A <see cref="EmailEditorEmailSectionBag"/> that represents the new email section.</returns>
        [HttpPost]
        [Route( "EmailEditorCreateEmailSection" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.Created, Type = typeof( EmailEditorEmailSectionBag ) )]
        [ProducesResponseType( HttpStatusCode.Conflict, Description = "The email section already exists." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "67998BF6-CFEE-4740-8C11-195AF9C91F83" )]
        public IActionResult EmailEditorCreateEmailSection( [FromBody] EmailEditorEmailSectionBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                if ( options == null )
                {
                    return BadRequest( "Email Section is required." );
                }

                var emailSectionService = new EmailSectionService( rockContext );

                if ( !options.Guid.IsEmpty() )
                {
                    // Make sure there isn't an email section with the same unique identifier.
                    var isExistingEmailSection = emailSectionService.Queryable().AsNoTracking().Any( es => es.Guid == options.Guid );

                    if ( isExistingEmailSection )
                    {
                        return Conflict();
                    }
                }

                var categoryGuid = options.Category?.Value.AsGuidOrNull();
                var category = categoryGuid.HasValue ? new CategoryService( rockContext ).Get( categoryGuid.Value ) : null;

                var thumbnailBinaryFileGuid = options.ThumbnailBinaryFile?.Value.AsGuidOrNull();
                var thumbnailBinaryFile = thumbnailBinaryFileGuid.HasValue ? new BinaryFileService( rockContext ).Get( thumbnailBinaryFileGuid.Value ) : null;

                var emailSection = new EmailSection
                {
                    Category = category,
                    Guid = options.Guid,
                    IsSystem = options.IsSystem,
                    Name = options.Name,
                    SourceMarkup = options.SourceMarkup,
                    ThumbnailBinaryFile = thumbnailBinaryFile,
                    UsageSummary = options.UsageSummary
                };

                if ( !emailSection.IsValid )
                {
                    return BadRequest( string.Join( ", ", emailSection.ValidationResults.Select( r => r.ErrorMessage ) ) );
                }

                emailSectionService.Add( emailSection );

                // Ensure the binary file is no longer temporary.
                emailSection.ThumbnailBinaryFile.IsTemporary = false;

                System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                rockContext.SaveChanges();

                return Content( HttpStatusCode.Created, GetEmailSectionBagFromEmailSection( emailSection ) );
            }
        }

        /// <summary>
        /// Gets the email section for the given id.
        /// </summary>
        /// <param name="options">The options to get an email section.</param>
        /// <returns>A <see cref="EmailEditorEmailSectionBag"/> that represents the email section.</returns>
        [HttpPost]
        [Route( "EmailEditorGetEmailSection" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EmailEditorEmailSectionBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "23350465-88EC-472E-80DF-5445D84062EA" )]
        public IActionResult EmailEditorGetEmailSection( [FromBody] EmailEditorGetEmailSectionOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var emailSectionService = new EmailSectionService( rockContext );
                var emailSection = emailSectionService.Queryable().AsNoTracking()
                    .Include( es => es.Category )
                    .Include( es => es.ThumbnailBinaryFile )
                    .Where( es => es.Guid == options.EmailSectionGuid )
                    .ToList()
                    .Where( es => es.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                    .Select( es => GetEmailSectionBagFromEmailSection( es ) )
                    .FirstOrDefault();

                if ( emailSection == null )
                {
                    return NotFound();
                }
                else
                {
                    return Ok( emailSection );
                }
            }
        }

        /// <summary>
        /// Gets all email sections.
        /// </summary>
        /// <returns>A <see cref="List{EmailEditorEmailSectionBag}"/> that represents the email sections.</returns>
        [HttpPost]
        [Route( "EmailEditorGetAllEmailSections" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<EmailEditorEmailSectionBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "4966E119-918B-47A8-AFD0-A6EB01EDD8C9" )]
        public IActionResult EmailEditorGetAllEmailSections( [FromBody] EmailEditorGetAllEmailSectionsOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var emailSectionService = new EmailSectionService( rockContext );
                var emailSection = emailSectionService.Queryable().AsNoTracking()
                    .Include( es => es.Category )
                    .Include( es => es.ThumbnailBinaryFile )
                    .ToList()
                    .Where( es => es.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                    .Select( es => GetEmailSectionBagFromEmailSection( es ) )
                    .ToList();

                return Ok( emailSection );
            }
        }

        /// <summary>
        /// Updates an email section or creates one if it doesn't exist.
        /// </summary>
        /// <param name="options">The email section to update or create.</param>
        /// <returns>A <see cref="EmailEditorEmailSectionBag"/> that represents the updated or new email section.</returns>
        [HttpPost]
        [Route( "EmailEditorUpdateEmailSection" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EmailEditorEmailSectionBag ) )]
        [ProducesResponseType( HttpStatusCode.Forbidden )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "E2250994-58D5-40BD-AB86-F02C40CB36A9" )]
        public IActionResult EmailEditorUpdateEmailSection( [FromBody] EmailEditorEmailSectionBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                if ( options == null )
                {
                    return BadRequest( "Email Section is required." );
                }

                var emailSectionService = new EmailSectionService( rockContext );
                EmailSection emailSection = null;

                if ( !options.Guid.IsEmpty() )
                {
                    emailSection = emailSectionService.Get( options.Guid );

                    if ( emailSection != null && ( emailSection.IsSystem || !emailSection.IsAuthorized( Rock.Security.Authorization.EDIT, this.RockRequestContext.CurrentPerson ) ) )
                    {
                        // The logged in person is not allowed to edit this email section.
                        return StatusCode( HttpStatusCode.Forbidden );
                    }
                }

                if ( emailSection == null )
                {
                    // Create a new email section.
                    emailSection = new EmailSection
                    {
                        Guid = options.Guid
                    };

                    emailSectionService.Add( emailSection );
                }

                var categoryGuid = options.Category?.Value.AsGuidOrNull();
                var category = categoryGuid.HasValue ? new CategoryService( rockContext ).Get( categoryGuid.Value ) : null;

                var thumbnailBinaryFileGuid = options.ThumbnailBinaryFile?.Value.AsGuidOrNull();
                var thumbnailBinaryFile = thumbnailBinaryFileGuid.HasValue ? new BinaryFileService( rockContext ).Get( thumbnailBinaryFileGuid.Value ) : null;

                emailSection.Category = category;
                emailSection.Guid = options.Guid;
                emailSection.Name = options.Name;
                emailSection.SourceMarkup = options.SourceMarkup;
                emailSection.ThumbnailBinaryFile = thumbnailBinaryFile;
                emailSection.UsageSummary = options.UsageSummary;

                if ( !emailSection.IsValid )
                {
                    // The email section is invalid.
                    return BadRequest( string.Join( ", ", emailSection.ValidationResults.Select( r => r.ErrorMessage ) ) );
                }

                // Ensure the binary file is no longer temporary.
                emailSection.ThumbnailBinaryFile.IsTemporary = false;

                System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                rockContext.SaveChanges();

                return Ok( GetEmailSectionBagFromEmailSection( emailSection ) );
            }
        }

        /// <summary>
        /// Deletes the email section for the given id.
        /// </summary>
        /// <param name="options">The options to delete an email section.</param>
        [HttpPost]
        [Route( "EmailEditorDeleteEmailSection" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [ProducesResponseType( HttpStatusCode.Forbidden )]
        [Rock.SystemGuid.RestActionGuid( "66B74F97-85D7-45F5-AD3E-0425903000AF" )]
        public IActionResult EmailEditorDeleteEmailSection( [FromBody] EmailEditorDeleteEmailSectionOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var emailSectionService = new EmailSectionService( rockContext );
                var emailSection = emailSectionService.Get( options.EmailSectionGuid );

                if ( emailSection == null )
                {
                    // The email section was already deleted so return early.
                    return NoContent();
                }

                if ( emailSection.IsSystem || !emailSection.IsAuthorized( Security.Authorization.EDIT, RockRequestContext.CurrentPerson ) )
                {
                    // The logged in person is not allowed to delete the email section.
                    return StatusCode( HttpStatusCode.Forbidden );
                }

                emailSectionService.Delete( emailSection );
                rockContext.SaveChanges();

                return NoContent();
            }
        }

        /// <summary>
        /// Creates attendance records if they don't exist for a designated occurrence and list of person IDs.
        /// </summary>
        /// <param name="options">The options to delete an email section.</param>
        [HttpPost]
        [Route( "EmailEditorRegisterRsvpRecipients" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.NoContent )]
        [Rock.SystemGuid.RestActionGuid( "FFE635FE-3988-4286-AEC6-0ADFAC162A58" )]
        public IActionResult EmailEditorRegisterRsvpRecipients( [FromBody] EmailEditorRegisterRsvpRecipientsOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );

                attendanceService.RegisterRSVPRecipients( options.OccurrenceId, options.PersonIds );

                return NoContent();
            }
        }

        /// <summary>
        /// Creates attendance records if they don't exist for a designated occurrence and list of person IDs.
        /// </summary>
        /// <param name="options">The options to delete an email section.</param>
        [HttpPost]
        [Route( "EmailEditorGetAttendanceOccurrence" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EmailEditorAttendanceOccurrenceBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "C8450C3D-4DD9-45D3-8020-8980D0E7CA02" )]
        public IActionResult EmailEditorGetAttendanceOccurrence( [FromBody] EmailEditorGetAttendanceOccurrenceOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var attendanceOccurrence = new AttendanceOccurrenceService( rockContext ).Get( options.OccurrenceId );

                if ( attendanceOccurrence == null )
                {
                    return NotFound();
                }
                else
                {
                    return Ok( OccurrenceAsBag( attendanceOccurrence ) );
                }
            }
        }

        /// <summary>
        /// Gets all the occurrences for a group for the selected dates, location and schedule, sorted by occurrence data in ascending order.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route( "EmailEditorGetFutureAttendanceOccurrences" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "25C14E2A-36A2-46C6-8B22-848D83A6D2C9" )]
        public IActionResult EmailEditorGetFutureAttendanceOccurrences( EmailEditorGetFutureAttendanceOccurrencesOptionsBag bag )
        {
            var grant = SecurityGrant.FromToken( bag.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var previousProxyCreationEnabled = rockContext.Configuration.ProxyCreationEnabled;
                rockContext.Configuration.ProxyCreationEnabled = false;

                var group = new GroupService( rockContext ).Get( bag.GroupGuid );
                var occurrences = new AttendanceOccurrenceService( rockContext )
                    .GetFutureGroupOccurrences( group, null )
                    .Select( OccurrenceAsListItemBag )
                    .ToList();

                rockContext.Configuration.ProxyCreationEnabled = previousProxyCreationEnabled;

                return Ok( occurrences );
            }
        }

        /// <summary>
        /// Creates a new attendance occurrence for a group.
        /// </summary>
        [HttpPost]
        [Route( "EmailEditorCreateAttendanceOccurrence" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EmailEditorAttendanceOccurrenceBag ) )]
        [Rock.SystemGuid.RestActionGuid( "2A8A1319-3A64-4449-876D-480FD500EAEC" )]
        public IActionResult EmailEditorCreateAttendanceOccurrence( [FromBody] EmailEditorCreateAttendanceOccurrenceOptionsBag bag )
        {
            var grant = SecurityGrant.FromToken( bag.SecurityGrantToken );

            // Default security for ConnectionRequest is world view. So we decided
            // to require a custom security grant in order to use the picker so that
            // the API was not just open to the world.
            if ( grant?.IsAccessGranted( EmailEditorSecurityGrantRule.AccessInstance, Authorization.VIEW ) != true )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var occurrence = new AttendanceOccurrenceService( rockContext )
                    .GetOrAdd( bag.OccurrenceDate, bag.GroupId, bag.LocationId, bag.ScheduleId );

                return Ok( OccurrenceAsBag( occurrence ) );
            }
        }

        private static EmailEditorEmailSectionBag GetEmailSectionBagFromEmailSection( EmailSection emailSection )
        {
            return emailSection == null ? null : new EmailEditorEmailSectionBag
            {
                Category = emailSection.Category.ToListItemBag(),
                Guid = emailSection.Guid,
                IsSystem = emailSection.IsSystem,
                Name = emailSection.Name,
                SourceMarkup = emailSection.SourceMarkup,
                ThumbnailBinaryFile = emailSection.ThumbnailBinaryFile.ToListItemBag(),
                UsageSummary = emailSection.UsageSummary
            };
        }

        private static ListItemBag OccurrenceAsListItemBag( AttendanceOccurrence attendanceOccurrence )
        {
            return attendanceOccurrence == null ? null : new ListItemBag
            {
                // Need to return the integer ID here to work with the RsvpResponse block.
                Value = $"{attendanceOccurrence.Id}|{attendanceOccurrence.GroupId}|{attendanceOccurrence.LocationId}|{attendanceOccurrence.ScheduleId}|{attendanceOccurrence.OccurrenceDate:s}",
                Text = attendanceOccurrence.OccurrenceDate.ToString( "dddd, MMMM d, yyyy" )
            };
        }

        private static EmailEditorAttendanceOccurrenceBag OccurrenceAsBag( AttendanceOccurrence attendanceOccurrence )
        {
            return attendanceOccurrence == null ? null : new EmailEditorAttendanceOccurrenceBag
            {
                // Need to return the integer ID here to work with the RsvpResponse block.
                OccurrenceId = attendanceOccurrence.Id,
                GroupId = attendanceOccurrence.GroupId,
                LocationId = attendanceOccurrence.LocationId,
                ScheduleId = attendanceOccurrence.ScheduleId,
                OccurrenceDate = $"{attendanceOccurrence.OccurrenceDate:s}"
            };
        }

        #endregion

        #region Entity Tag List

        /// <summary>
        /// Gets the tags that are currently specified for the given entity.
        /// </summary>
        /// <param name="options">The options that describe which tags to load.</param>
        /// <returns>A collection of <see cref="EntityTagListTagBag"/> that represent the tags.</returns>
        [HttpPost]
        [Route( "EntityTagListGetEntityTags" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<EntityTagListTagBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "7542D4B3-17DC-4640-ACBD-F02784130401" )]
        public IActionResult EntityTagListGetEntityTags( [FromBody] EntityTagListGetEntityTagsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );
                var entityGuid = Reflection.GetEntityGuidForEntityType( options.EntityTypeGuid, options.EntityKey, false, rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                if ( !entityTypeId.HasValue || !entityGuid.HasValue )
                {
                    return NotFound();
                }

                var taggedItemService = new TaggedItemService( rockContext );
                var items = taggedItemService.Get( entityTypeId.Value, options.EntityQualifierColumn, options.EntityQualifierValue, RockRequestContext.CurrentPerson?.Id, entityGuid.Value, options.CategoryGuid, options.ShowInactiveTags )
                    .Include( ti => ti.Tag.Category )
                    .Select( ti => ti.Tag )
                    .ToList()
                    .Where( t => t.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( t, Security.Authorization.VIEW ) == true )
                    .Select( t => GetTagBagFromTag( t ) )
                    .ToList();

                return Ok( items );
            }
        }

        /// <summary>
        /// Gets the tags that are available for the given entity.
        /// </summary>
        /// <param name="options">The options that describe which tags to load.</param>
        /// <returns>A collection of list item bags that represent the tags.</returns>
        [HttpPost]
        [Route( "EntityTagListGetAvailableTags" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<EntityTagListTagBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "91890D39-6E3E-4623-AAD7-F32E686C784E" )]
        public IActionResult EntityTagListGetAvailableTags( [FromBody] EntityTagListGetAvailableTagsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );
                var entityGuid = Reflection.GetEntityGuidForEntityType( options.EntityTypeGuid, options.EntityKey, false, rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                if ( !entityTypeId.HasValue || !entityGuid.HasValue )
                {
                    return NotFound();
                }

                var tagService = new TagService( rockContext );
                var items = tagService.Get( entityTypeId.Value, options.EntityQualifierColumn, options.EntityQualifierValue, RockRequestContext.CurrentPerson?.Id, options.CategoryGuid, options.ShowInactiveTags )
                    .Where( t => t.Name.StartsWith( options.Name )
                        && !t.TaggedItems.Any( i => i.EntityGuid == entityGuid ) )
                    .ToList()
                    .Where( t => t.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( t, Security.Authorization.VIEW ) == true )
                    .Select( t => GetTagBagFromTag( t ) )
                    .ToList();

                return Ok( items );
            }
        }

        /// <summary>
        /// Create a new personal tag for the EntityTagList control.
        /// </summary>
        /// <param name="options">The options that describe the tag to be created.</param>
        /// <returns>An instance of <see cref="EntityTagListTagBag"/> that represents the tag.</returns>
        [HttpPost]
        [Route( "EntityTagListCreatePersonalTag" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.Created, Type = typeof( EntityTagListTagBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [ProducesResponseType( HttpStatusCode.Conflict, Type = typeof( EntityTagListTagBag ) )]
        [Rock.SystemGuid.RestActionGuid( "8CCB7B8D-5D5C-4AA6-A12C-ED062C7AFA05" )]
        public IActionResult EntityTagListCreatePersonalTag( [FromBody] EntityTagListCreatePersonalTagOptionsBag options )
        {
            if ( RockRequestContext.CurrentPerson == null )
            {
                return Unauthorized();
            }

            using ( var rockContext = new RockContext() )
            {
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );

                if ( !entityTypeId.HasValue )
                {
                    return NotFound();
                }

                var tagService = new TagService( rockContext );
                var tag = tagService.Get( entityTypeId.Value, options.EntityQualifierColumn, options.EntityQualifierValue, RockRequestContext.CurrentPerson?.Id, options.Name, options.CategoryGuid, true );

                // If the personal tag already exists, use a 409 to indicate
                // it already exists and return the existing tag.
                if ( tag != null && tag.OwnerPersonAliasId.HasValue )
                {
                    // If the personal tag isn't active, make it active.
                    if ( !tag.IsActive )
                    {
                        tag.IsActive = true;
                        System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                        rockContext.SaveChanges();
                    }

                    return Content( System.Net.HttpStatusCode.Conflict, GetTagBagFromTag( tag ) );
                }

                // At this point tag either doesn't exist or was an organization
                // tag so we need to create a new personal tag.
                tag = new Tag
                {
                    EntityTypeId = entityTypeId,
                    OwnerPersonAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( RockRequestContext.CurrentPerson.Id ),
                    Name = options.Name
                };

                if ( options.CategoryGuid.HasValue )
                {
                    var category = new CategoryService( rockContext ).Get( options.CategoryGuid.Value );

                    if ( category == null || ( !category.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) && grant?.IsAccessGranted( category, Security.Authorization.VIEW ) != true ) )
                    {
                        return NotFound();
                    }

                    // Set the category as well so we can properly convert to a bag.
                    tag.Category = category;
                    tag.CategoryId = category.Id;
                }

                tagService.Add( tag );

                System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                rockContext.SaveChanges();

                return Content( System.Net.HttpStatusCode.Created, GetTagBagFromTag( tag ) );
            }
        }

        /// <summary>
        /// Adds a tag on the given entity.
        /// </summary>
        /// <param name="options">The options that describe the tag and the entity to be tagged.</param>
        /// <returns>An instance of <see cref="EntityTagListTagBag"/> that represents the tag applied to the entity.</returns>
        [HttpPost]
        [Route( "EntityTagListAddEntityTag" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EntityTagListTagBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "C9CACC7F-68DE-4765-8967-B50EE2949062" )]
        public IActionResult EntityTagListAddEntityTag( [FromBody] EntityTagListAddEntityTagOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityType = EntityTypeCache.Get( options.EntityTypeGuid, rockContext );

                if ( entityType == null )
                {
                    return BadRequest( "Invalid entity type." );
                }

                var entity = Reflection.GetIEntityForEntityType( entityType.GetEntityType(), options.EntityKey, false, rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                if ( entity == null )
                {
                    return NotFound();
                }

                if ( entity is ISecured secured && !secured.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                {
                    return Unauthorized();
                }

                var tagService = new TagService( rockContext );
                var tag = tagService.Get( options.TagKey );

                if ( tag == null || ( !tag.IsAuthorized( Security.Authorization.TAG, RockRequestContext.CurrentPerson ) && grant?.IsAccessGranted( tag, Security.Authorization.VIEW ) != true ) )
                {
                    return NotFound();
                }

                // If the entity is not already tagged, then tag it.
                var taggedItem = tag.TaggedItems.FirstOrDefault( i => i.EntityGuid.Equals( entity.Guid ) );

                if ( taggedItem == null )
                {
                    taggedItem = new TaggedItem
                    {
                        Tag = tag,
                        EntityTypeId = entityType.Id,
                        EntityGuid = entity.Guid
                    };

                    tag.TaggedItems.Add( taggedItem );

                    System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                    rockContext.SaveChanges();
                }

                return Ok( GetTagBagFromTag( tag ) );
            }
        }

        /// <summary>
        /// Removes a tag from the given entity.
        /// </summary>
        /// <param name="options">The options that describe the tag and the entity to be untagged.</param>
        /// <returns>A response code that indicates success or failure.</returns>
        [HttpPost]
        [Route( "EntityTagListRemoveEntityTag" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "The tag was removed." )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "6A78D538-87DB-43FE-9150-4E9A3F276AFE" )]
        public IActionResult EntityTagListRemoveEntityTag( [FromBody] EntityTagListRemoveEntityTagOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );
                var entityGuid = Reflection.GetEntityGuidForEntityType( options.EntityTypeGuid, options.EntityKey, false, rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var tagService = new TagService( rockContext );
                var taggedItemService = new TaggedItemService( rockContext );

                if ( !entityTypeId.HasValue || !entityGuid.HasValue )
                {
                    return NotFound();
                }

                var tag = tagService.Get( options.TagKey );

                if ( tag == null || ( !tag.IsAuthorized( Security.Authorization.TAG, RockRequestContext.CurrentPerson ) && grant?.IsAccessGranted( tag, Security.Authorization.VIEW ) != true ) )
                {
                    return NotFound();
                }

                // If the entity is tagged, then untag it.
                var taggedItem = taggedItemService.Queryable()
                    .FirstOrDefault( ti => ti.TagId == tag.Id && ti.EntityGuid == entityGuid.Value );

                if ( taggedItem != null )
                {
                    taggedItemService.Delete( taggedItem );

                    System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                    rockContext.SaveChanges();
                }

                return Ok();
            }
        }

        /// <summary>
        /// Removes a tag from the given entity.
        /// </summary>
        /// <param name="options">The options that describe the tag and the entity to be untagged.</param>
        /// <returns>A collection of tags that were created.</returns>
        [HttpPost]
        [Route( "EntityTagListSaveTagValues" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<EntityTagListTagBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "02886e54-6088-40ea-98be-9157ec2a3369" )]
        public IActionResult EntityTagListSaveTagValues( [FromBody] EntityTagListSaveTagValuesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                Person currentPerson = RockRequestContext.CurrentPerson;
                int? currentPersonId = currentPerson.Id;
                var entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );
                var entityGuid = Reflection.GetEntityGuidForEntityType( options.EntityTypeGuid, options.EntityKey, false, rockContext );

                if ( entityGuid != Guid.Empty && entityGuid != null )
                {
                    var tagService = new TagService( rockContext );
                    var taggedItemService = new TaggedItemService( rockContext );
                    var person = currentPersonId.HasValue ? new PersonService( rockContext ).Get( currentPersonId.Value ) : null;

                    // Get the existing tagged items for this entity
                    var existingTaggedItems = new List<TaggedItem>();
                    foreach ( var taggedItem in taggedItemService.Get( entityTypeId ?? 0, options.EntityQualifierColumn, options.EntityQualifierValue, currentPersonId, entityGuid.Value, options.CategoryGuid, options.ShowInactiveTags ) )
                    {
                        if ( taggedItem.IsAuthorized( Security.Authorization.VIEW, person ) )
                        {
                            existingTaggedItems.Add( taggedItem );
                        }
                    }

                    // Get tag values after user edit
                    var currentTags = new List<Tag>();
                    foreach ( var tagBag in options.Tags )
                    {
                        var tagName = tagBag.Name;

                        if ( tagName.IsNullOrWhiteSpace() )
                        {
                            continue;
                        }

                        // Only if this is a new tag, create it
                        var tag = tagService.Get( entityTypeId ?? 0, options.EntityQualifierColumn, options.EntityQualifierValue, currentPersonId, tagName, options.CategoryGuid, options.ShowInactiveTags );

                        if ( currentPerson.PrimaryAlias != null && tag == null )
                        {
                            var cat = CategoryCache.Get( options.CategoryGuid ?? Guid.Empty );
                            var categoryId = cat != null ? cat.Id : ( int? ) null;

                            tag = new Tag();
                            tag.EntityTypeId = entityTypeId;
                            tag.CategoryId = categoryId;
                            tag.EntityTypeQualifierColumn = options.EntityQualifierColumn;
                            tag.EntityTypeQualifierValue = options.EntityQualifierValue;
                            tag.OwnerPersonAliasId = currentPerson.PrimaryAlias.Id;
                            tag.Name = tagName;
                            tagService.Add( tag );
                        }

                        if ( tag != null )
                        {
                            currentTags.Add( tag );
                        }
                    }

                    rockContext.SaveChanges();

                    var currentNames = currentTags.Select( t => t.Name ).ToList();
                    var existingNames = existingTaggedItems.Select( t => t.Tag.Name ).ToList();

                    // Delete any tagged items that user removed
                    foreach ( var taggedItem in existingTaggedItems )
                    {
                        if ( !currentNames.Contains( taggedItem.Tag.Name, StringComparer.OrdinalIgnoreCase ) && taggedItem.IsAuthorized( Rock.Security.Authorization.TAG, person ) )
                        {
                            existingNames.Remove( taggedItem.Tag.Name );
                            taggedItemService.Delete( taggedItem );
                        }
                    }
                    rockContext.SaveChanges();

                    // Add any tagged items that user added
                    foreach ( var tag in currentTags )
                    {
                        // If the tagged item was not already there, and (it's their personal tag OR they are authorized to use it) then add it.
                        if ( !existingNames.Contains( tag.Name, StringComparer.OrdinalIgnoreCase ) &&
                             (
                                ( tag.OwnerPersonAliasId != null && tag.OwnerPersonAliasId == currentPerson.PrimaryAlias.Id ) ||
                                tag.IsAuthorized( Rock.Security.Authorization.TAG, person )
                             )
                           )
                        {
                            var taggedItem = new TaggedItem();
                            taggedItem.TagId = tag.Id;
                            taggedItem.EntityTypeId = entityTypeId ?? 0;
                            taggedItem.EntityGuid = entityGuid.Value;
                            taggedItemService.Add( taggedItem );
                        }
                    }
                    rockContext.SaveChanges();

                    var currentTagBags = currentTags.Select( t => GetTagBagFromTag( t ) ).ToList();
                    return Ok( currentTagBags );
                }

                return BadRequest( "Cannot get entity guid from given entity key and entity type GUID." );
            }
        }

        private static EntityTagListTagBag GetTagBagFromTag( Tag tag )
        {
            return new EntityTagListTagBag
            {
                IdKey = tag.IdKey,
                BackgroundColor = tag.BackgroundColor,
                Category = tag.Category != null
                    ? new ListItemBag
                    {
                        Value = tag.Category.Guid.ToString(),
                        Text = tag.Category.ToString()
                    }
                    : null,
                EntityTypeGuid = tag.EntityTypeId.HasValue ? EntityTypeCache.Get( tag.EntityTypeId.Value ).Guid : Guid.Empty,
                IconCssClass = tag.IconCssClass,
                IsPersonal = tag.OwnerPersonAliasId.HasValue,
                Name = tag.Name
            };
        }

        #endregion

        #region Entity Picker

        /// <summary>
        /// Gets the entity type GUIDs to be displayed in the entity type picker part of the entity picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of unique identifiers of the entity types.</returns>
        [HttpPost]
        [Route( "EntityPickerGetEntityTypeGuids" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<string> ) )]
        [Rock.SystemGuid.RestActionGuid( "8E92F72E-235A-4192-9C09-742F94849D62" )]
        public IActionResult EntityPickerGetEntityTypeGuids( [FromBody] EntityPickerGetEntityTypeGuidsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityTypeGuids = new EntityTypeService( new RockContext() )
                    .Queryable()
                    .Where( e => e.IsEntity == true && e.SingleValueFieldTypeId.HasValue )
                    .Select( e => e.Guid.ToString() )
                    .ToList();

                return Ok( entityTypeGuids );
            }
        }

        /// <summary>
        /// Gets the single value field type Guid of the given entity type
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A GUID of the field type</returns>
        [HttpPost]
        [Route( "EntityPickerGetFieldTypeConfiguration" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EntityPickerGetFieldTypeConfigurationResultsBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "6BDA28C3-E6D7-42EB-9011-0C076455D4A7" )]
        public IActionResult EntityPickerGetFieldTypeConfiguration( [FromBody] EntityPickerGetFieldTypeConfigurationOptionsBag options )
        {
            if ( options.EntityTypeGuid == null )
            {
                return NotFound();
            }

            var entityType = EntityTypeCache.Get( options.EntityTypeGuid );

            if ( entityType == null )
            {
                return NotFound();
            }

            var fieldType = entityType.SingleValueFieldType;
            var fieldTypeGuid = fieldType.Guid;

            if ( fieldType == null || fieldTypeGuid == null )
            {
                return NotFound();
            }

            var field = fieldType.Field;

            var entityValue = options.EntityValue ?? "";
            var privateValue = field.GetPrivateEditValue( entityValue, new Dictionary<string, string>() );
            var configurationValues = field.GetPublicConfigurationValues( new Dictionary<string, string>(), Field.ConfigurationValueUsage.Edit, privateValue );

            return Ok( new EntityPickerGetFieldTypeConfigurationResultsBag
            {
                FieldTypeGuid = fieldTypeGuid,
                FieldTypeName = fieldType.Name.Replace( " ", string.Empty ),
                FieldTypePluralName = fieldType.Name.Replace( " ", string.Empty ).Pluralize(),
                ConfigurationValues = configurationValues
            } );
        }

        #endregion

        #region Entity Type Picker

        /// <summary>
        /// Gets the entity types that can be displayed in the entity type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the entity types.</returns>
        [HttpPost]
        [Route( "EntityTypePickerGetEntityTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "AFDD3D40-5856-478B-A41A-0539127F0631" )]
        public IActionResult EntityTypePickerGetEntityTypes( [FromBody] EntityTypePickerGetEntityTypesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var itemQuery = EntityTypeCache.All( rockContext )
                    .Where( t => t.IsEntity );

                if ( options.EntityTypeGuids != null && options.EntityTypeGuids.Any() )
                {
                    itemQuery = itemQuery.Where( t => options.EntityTypeGuids.Contains( t.Guid ) );
                }

                var items = itemQuery
                    .OrderByDescending( t => t.IsCommon )
                    .ThenBy( t => t.FriendlyName )
                    .Select( t => new ListItemBag
                    {
                        Value = t.Guid.ToString(),
                        Text = t.FriendlyName,
                        Category = t.IsCommon ? "Common" : "All Entities"
                    } )
                    .ToList();

                return Ok( items );
            }
        }

        #endregion

        #region Ethnicity Picker

        /// <summary>
        /// Gets the ethnicities that can be displayed in the ethnicity picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the ethnicities and the label for the control.</returns>
        [HttpPost]
        [Route( "EthnicityPickerGetEthnicities" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( EthnicityPickerGetEthnicitiesResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "a04bddf8-4169-47f8-8b03-ee8e2f110b35" )]
        public IActionResult EthnicityPickerGetEthnicities()
        {
            var ethnicities = DefinedTypeCache.Get( SystemGuid.DefinedType.PERSON_ETHNICITY ).DefinedValues
                .Select( e => new ListItemBag { Text = e.Value, Value = e.Guid.ToString() } )
                .ToList();

            return Ok( new EthnicityPickerGetEthnicitiesResultsBag
            {
                Ethnicities = ethnicities,
                Label = Rock.Web.SystemSettings.GetValue( Rock.SystemKey.SystemSetting.PERSON_ETHNICITY_LABEL )
            } );
        }

        #endregion

        #region Event Calendar Picker

        /// <summary>
        /// Gets the event calendars that can be displayed in the event calendar picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag" /> objects that represent the event calendars.</returns>
        [HttpPost]
        [Route( "EventCalendarPickerGetEventCalendars" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "92d88be0-2971-441a-b582-eec304ce4bc9" )]
        public IActionResult EventCalendarPickerGetEventCalendars()
        {
            using ( var rockContext = new RockContext() )
            {
                var calendars = EventCalendarCache.All();
                var calendarList = new List<ListItemBag>();

                foreach ( EventCalendarCache eventCalendar in calendars )
                {
                    if ( !eventCalendar.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                    {
                        continue;
                    }

                    calendarList.Add( new ListItemBag { Text = eventCalendar.Name, Value = eventCalendar.Guid.ToString() } );
                }

                return Ok( calendarList );
            }
        }

        #endregion

        #region Event Item Picker

        /// <summary>
        /// Gets the event items that can be displayed in the event item picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the event items.</returns>
        [HttpPost]
        [Route( "EventItemPickerGetEventItems" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "1D558F8A-08C9-4B62-A3A9-853C9F66B748" )]
        public IActionResult EventItemPickerGetEventItems( [FromBody] EventItemPickerGetEventItemsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var eventItems = new EventCalendarItemService( rockContext ).Queryable()
                    .Include( eci => eci.EventCalendar )
                    .Where( i => options.IncludeInactive ? true : i.EventItem.IsActive )
                    .ToList()
                    .Where( eci => eci.EventCalendar.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                    .Select( i => new ListItemBag
                    {
                        Category = i.EventCalendar.Name,
                        Value = i.EventItem.Guid.ToString(),
                        Text = i.EventItem.Name
                    } )
                    .OrderBy( i => i.Category )
                    .ThenBy( i => i.Text )
                    .ToList();

                return Ok( eventItems );
            }
        }

        #endregion

        #region Field Type Editor

        /// <summary>
        /// Gets the available field types for the current person.
        /// </summary>
        /// <param name="options">The options that provide details about the request.</param>
        /// <returns>A collection <see cref="ListItemBag"/> that represents the field types that are available.</returns>
        [HttpPost]
        [Route( "FieldTypeEditorGetAvailableFieldTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "FEDEF3F7-FCB0-4538-9629-177C7D2AE06F" )]
        public IActionResult FieldTypeEditorGetAvailableFieldTypes( [FromBody] FieldTypeEditorGetAvailableFieldTypesOptionsBag options )
        {
            var fieldTypes = FieldTypeCache.All()
                .Where( f => f.Platform.HasFlag( Rock.Utility.RockPlatform.Obsidian ) )
                .ToList();

            var fieldTypeItems = fieldTypes
                .Select( f => new ListItemBag
                {
                    Text = f.Name,
                    Value = f.Guid.ToString(),
                    Category = f.Field?.GetType().GetCustomAttribute<UniversalFieldTypeGuidAttribute>()?.Guid.ToString()
                } )
                .ToList();

            return Ok( fieldTypeItems );
        }

        /// <summary>
        /// Gets the attribute configuration information provided and returns a new
        /// set of configuration data.
        /// </summary>
        /// <remarks>
        /// This is used by the attribute editor control when a field type makes
        /// a change that requires new data to be retrieved in order for it to
        /// continue editing the attribute.
        /// </remarks>
        /// <param name="options">The view model that contains the update request.</param>
        /// <returns>An instance of <see cref="FieldTypeEditorUpdateAttributeConfigurationResultBag"/> that represents the state of the attribute configuration.</returns>
        [HttpPost]
        [Route( "FieldTypeEditorUpdateAttributeConfiguration" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( FieldTypeEditorUpdateAttributeConfigurationResultBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "AFDF0EC4-5D17-4278-9FA6-3F859F38E3B5" )]
        public IActionResult FieldTypeEditorUpdateAttributeConfiguration( [FromBody] FieldTypeEditorUpdateAttributeConfigurationOptionsBag options )
        {
            var fieldType = Rock.Web.Cache.FieldTypeCache.Get( options.FieldTypeGuid )?.Field;

            if ( fieldType == null )
            {
                return BadRequest( "Unknown field type." );
            }

            // Convert the public configuration options into our private
            // configuration options (values).
            var configurationValues = fieldType.GetPrivateConfigurationValues( options.ConfigurationValues );

            // Convert the default value from the public value into our
            // private internal value.
            var privateDefaultValue = fieldType.GetPrivateEditValue( options.DefaultValue, configurationValues );

            // Get the new configuration properties from the currently selected
            // options.
            var configurationProperties = fieldType.GetPublicEditConfigurationProperties( configurationValues );

            // Get the public configuration options from the internal options (values).
            var publicAdminConfigurationValues = fieldType.GetPublicConfigurationValues( configurationValues, Field.ConfigurationValueUsage.Configure, null );

            // Get the public configuration options from the internal options (values).
            var publicEditConfigurationValues = fieldType.GetPublicConfigurationValues( configurationValues, Field.ConfigurationValueUsage.Edit, options.DefaultValue );

            return Ok( new FieldTypeEditorUpdateAttributeConfigurationResultBag
            {
                ConfigurationProperties = configurationProperties,
                AdminConfigurationValues = publicAdminConfigurationValues,
                EditConfigurationValues = publicEditConfigurationValues,
                DefaultValue = fieldType.GetPublicEditValue( privateDefaultValue, configurationValues )
            } );
        }

        #endregion

        #region Field Type Picker

        /// <summary>
        /// Gets the field types that can be displayed in the field type picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the field types.</returns>
        [HttpPost]
        [Route( "FieldTypePickerGetFieldTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "AB53509A-C8A9-481B-839F-DA53232A698A" )]
        public IActionResult FieldTypePickerGetFieldTypes()
        {
            List<ListItemBag> items = new List<ListItemBag> { };

            foreach ( var item in FieldTypeCache.All() )
            {
                items.Add( new ListItemBag { Text = item.Name, Value = item.Guid.ToString() } );
            }

            return Ok( items );
        }

        #endregion

        #region Financial Gateway Picker

        /// <summary>
        /// Gets the financial gateways that can be displayed in the financial gateway picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the financial gateways.</returns>
        [HttpPost]
        [Route( "FinancialGatewayPickerGetFinancialGateways" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "DBF12D3D-09BF-419F-A315-E3B6C0206344" )]
        public IActionResult FinancialGatewayPickerGetFinancialGateways( [FromBody] FinancialGatewayPickerGetFinancialGatewaysOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                List<ListItemBag> items = new List<ListItemBag> { };

                var gateways = new FinancialGatewayService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( g => g.EntityTypeId.HasValue )
                    .OrderBy( g => g.Name )
                    .ToList();

                foreach ( var gateway in gateways )
                {
                    var entityType = EntityTypeCache.Get( gateway.EntityTypeId.Value );
                    GatewayComponent component = GatewayContainer.GetComponent( entityType.Name );

                    // TODO: Need to see if the gateway is selected e.g. gateway.Guid == options.selectedGuid
                    // Add the gateway if the control is configured to show all of the gateways.
                    if ( options.IncludeInactive && options.ShowAllGatewayComponents )
                    {
                        items.Add( new ListItemBag { Text = gateway.Name, Value = gateway.Guid.ToString() } );
                        continue;
                    }

                    // Do not add if the component or gateway is not active and the controls has ShowInactive set to false.
                    if ( options.IncludeInactive == false && ( gateway.IsActive == false || component == null || component.IsActive == false ) )
                    {
                        continue;
                    }

                    if ( options.ShowAllGatewayComponents == false && ( component == null || component.SupportsRockInitiatedTransactions == false ) )
                    {
                        continue;
                    }

                    items.Add( new ListItemBag { Text = gateway.Name, Value = gateway.Guid.ToString() } );
                }

                return Ok( items );
            }
        }

        #endregion

        #region Financial Statement Template Picker

        /// <summary>
        /// Gets the financial statement templates that can be displayed in the financial statement template picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the financial statement templates.</returns>
        [HttpPost]
        [Route( "FinancialStatementTemplatePickerGetFinancialStatementTemplates" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "4E10F2DC-BD7C-4F75-919C-B3F71868ED24" )]
        public IActionResult FinancialStatementTemplatePickerGetFinancialStatementTemplates()
        {
            using ( var rockContext = new RockContext() )
            {
                List<ListItemBag> items = new FinancialStatementTemplateService( rockContext )
                    .Queryable()
                    .Where( s => s.IsActive == true )
                    .Select( i => new ListItemBag
                    {
                        Value = i.Guid.ToString(),
                        Text = i.Name
                    } )
                    .OrderBy( a => a.Text )
                    .ToList();

                return Ok( items );
            }
        }

        #endregion

        #region Following

        /// <summary>
        /// Determines if the entity is currently being followed by the logged in person.
        /// </summary>
        /// <param name="options">The options that describe which entity to be checked.</param>
        /// <returns>A <see cref="FollowingGetFollowingResponseBag"/> that contains the followed state of the entity.</returns>
        [HttpPost]
        [Route( "FollowingGetFollowing" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( FollowingGetFollowingResponseBag ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "FA1CC136-A994-4870-9507-818EA7A70F01" )]
        public IActionResult FollowingGetFollowing( [FromBody] FollowingGetFollowingOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                if ( RockRequestContext.CurrentPerson == null )
                {
                    return Unauthorized();
                }

                // Get the entity type identifier to use to lookup the entity.
                int? entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );

                if ( !entityTypeId.HasValue )
                {
                    return NotFound();
                }

                int? entityId = null;

                // Special handling for a person record, need to translate it to
                // a person alias record.
                if ( entityTypeId.Value == EntityTypeCache.GetId<Person>() )
                {
                    entityTypeId = EntityTypeCache.GetId<PersonAlias>();
                    entityId = new PersonService( rockContext ).Get( options.EntityKey, true )?.PrimaryAliasId;
                }
                else
                {
                    // Get the entity identifier to use for the following query.
                    entityId = Reflection.GetEntityIdForEntityType( entityTypeId.Value, options.EntityKey, true, rockContext );
                }

                if ( !entityId.HasValue )
                {
                    return NotFound();
                }

                var purposeKey = options.PurposeKey ?? string.Empty;

                // Look for any following objects that match the criteria.
                var followings = new FollowingService( rockContext ).Queryable()
                    .Where( f =>
                        f.EntityTypeId == entityTypeId.Value &&
                        f.EntityId == entityId.Value &&
                        f.PersonAlias.PersonId == RockRequestContext.CurrentPerson.Id &&
                        ( ( f.PurposeKey == null && purposeKey == "" ) || f.PurposeKey == purposeKey ) );

                return Ok( new FollowingGetFollowingResponseBag
                {
                    IsFollowing = followings.Any()
                } );
            }
        }

        /// <summary>
        /// Sets the following state of the entity for the logged in person.
        /// </summary>
        /// <param name="options">The options that describe which entity to be followed or unfollowed.</param>
        /// <returns>An HTTP status code that indicates if the request was successful.</returns>
        [HttpPost]
        [Route( "FollowingSetFollowing" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "An empty response indicates the following was set successfully." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "8CA2EAFB-E577-4F65-8D96-F42D8D5AAE7A" )]
        public IActionResult FollowingSetFollowing( [FromBody] FollowingSetFollowingOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var followingService = new FollowingService( rockContext );

                if ( RockRequestContext.CurrentPerson == null )
                {
                    return Unauthorized();
                }

                // Get the entity type identifier to use to lookup the entity.
                int? entityTypeId = EntityTypeCache.GetId( options.EntityTypeGuid );

                if ( !entityTypeId.HasValue )
                {
                    return NotFound();
                }

                int? entityId = null;

                // Special handling for a person record, need to translate it to
                // a person alias record.
                if ( entityTypeId.Value == EntityTypeCache.GetId<Person>() )
                {
                    entityTypeId = EntityTypeCache.GetId<PersonAlias>();
                    entityId = new PersonService( rockContext ).Get( options.EntityKey, true )?.PrimaryAliasId;
                }
                else
                {
                    // Get the entity identifier to use for the following query.
                    entityId = Reflection.GetEntityIdForEntityType( entityTypeId.Value, options.EntityKey, true, rockContext );
                }

                if ( !entityId.HasValue )
                {
                    return NotFound();
                }

                var purposeKey = options.PurposeKey ?? string.Empty;

                // Look for any following objects that match the criteria.
                var followings = followingService.Queryable()
                    .Where( f =>
                        f.EntityTypeId == entityTypeId.Value &&
                        f.EntityId == entityId.Value &&
                        f.PersonAlias.PersonId == RockRequestContext.CurrentPerson.Id &&
                        ( ( f.PurposeKey == null && purposeKey == "" ) || f.PurposeKey == purposeKey ) );

                if ( options.IsFollowing )
                {
                    // Already following, don't need to add a new record.
                    if ( followings.Any() )
                    {
                        return Ok();
                    }

                    var following = new Following
                    {
                        EntityTypeId = entityTypeId.Value,
                        EntityId = entityId.Value,
                        PersonAliasId = RockRequestContext.CurrentPerson.PrimaryAliasId.Value,
                        PurposeKey = purposeKey
                    };

                    followingService.Add( following );

                    if ( !following.IsValid )
                    {
                        return BadRequest( string.Join( ", ", following.ValidationResults.Select( r => r.ErrorMessage ) ) );
                    }
                }
                else
                {
                    foreach ( var following in followings )
                    {
                        // Don't check security here because a person is allowed
                        // to un-follow/delete something they previously followed.
                        followingService.Delete( following );
                    }
                }

                System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );

                rockContext.SaveChanges();

                return Ok();
            }
        }

        #endregion

        #region Geo Picker

        /// <summary>
        /// Retrieve the configuration settings for Google Maps.
        /// </summary>
        /// <returns>The object that describes the configuration required to display a Google Map.</returns>
        [HttpPost]
        [Route( "GeoPickerGetGoogleMapSettings" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( GeoPickerGoogleMapSettingsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "a3e0af9b-36d3-4ec8-a983-0087488c553d" )]
        public IActionResult GeoPickerGetGoogleMapSettings( [FromBody] GeoPickerGetGoogleMapSettingsOptionsBag options )
        {
            // Map Styles
            Guid MapStyleValueGuid = options.MapStyleValueGuid == null || options.MapStyleValueGuid.IsEmpty() ? Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK.AsGuid() : options.MapStyleValueGuid;
            string mapStyle = "null";
            string markerColor = "";
            string mapId = string.Empty;

            try
            {
                DefinedValueCache dvcMapStyle = DefinedValueCache.Get( MapStyleValueGuid );
                if ( dvcMapStyle != null )
                {
                    var dynamicMapStyle = dvcMapStyle.GetAttributeValue( "DynamicMapStyle" );
                    if ( dynamicMapStyle.IsNotNullOrWhiteSpace() )
                    {
                        mapStyle = dynamicMapStyle;
                    }
                    mapId = dvcMapStyle.GetAttributeValue( "core_GoogleMapId" );
                    var colors = dvcMapStyle.GetAttributeValue( "Colors" ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                    if ( colors.Any() )
                    {
                        markerColor = colors.First().Replace( "#", "" );
                    }
                }
            }
            catch { } // oh well...

            // Google API Key
            string googleApiKey = GlobalAttributesCache.Get().GetValue( "GoogleAPIKey" );

            // Default map location
            double? centerLatitude = null;
            double? centerLongitude = null;
            Guid guid = GlobalAttributesCache.Get().GetValue( "OrganizationAddress" ).AsGuid();

            if ( !guid.Equals( Guid.Empty ) )
            {
                var location = new Rock.Model.LocationService( new Rock.Data.RockContext() ).Get( guid );
                if ( location != null && location.GeoPoint != null && location.GeoPoint.Latitude != null && location.GeoPoint.Longitude != null )
                {
                    centerLatitude = location.GeoPoint.Latitude;
                    centerLongitude = location.GeoPoint.Longitude;
                }
            }

            return Ok( new GeoPickerGoogleMapSettingsBag
            {
                MapStyle = mapStyle,
                MarkerColor = markerColor,
                GoogleApiKey = googleApiKey,
                CenterLatitude = centerLatitude,
                CenterLongitude = centerLongitude,
                GoogleMapId = mapId
            } );
        }

        #endregion

        #region Grade Picker

        /// <summary>
        /// Gets the school grades that can be displayed in the grade picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the grades.</returns>
        [HttpPost]
        [Route( "GradePickerGetGrades" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "2C8F0B8E-F54D-460D-91DB-97B34A9AA174" )]
        public IActionResult GradePickerGetGrades( GradePickerGetGradesOptionsBag options )
        {
            var schoolGrades = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() );

            if ( schoolGrades == null )
            {
                return NotFound();
            }

            var list = new List<ListItemBag>();

            foreach ( var schoolGrade in schoolGrades.DefinedValues.OrderByDescending( a => a.Value.AsInteger() ) )
            {
                ListItemBag listItem = new ListItemBag();
                if ( options.UseAbbreviation )
                {
                    string abbreviation = schoolGrade.GetAttributeValue( "Abbreviation" );
                    listItem.Text = string.IsNullOrWhiteSpace( abbreviation ) ? schoolGrade.Description : abbreviation;
                }
                else
                {
                    listItem.Text = schoolGrade.Description;
                }

                listItem.Value = options.UseGuidAsValue ? schoolGrade.Guid.ToString() : schoolGrade.Value;

                list.Add( listItem );
            }

            return Ok( list );
        }

        #endregion

        #region Group and Role Picker

        /// <summary>
        /// Gets the roles that can be displayed in the group and role picker for the specified group.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the groups.</returns>
        [HttpPost]
        [Route( "GroupAndRolePickerGetRoles" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "285de6f4-0bf0-47e4-bda5-bcaa5a18b990" )]
        public IActionResult GroupAndRolePickerGetRoles( [FromBody] GroupAndRolePickerGetRolesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupRoles = new List<ListItemBag>();
                if ( options.GroupTypeGuid != Guid.Empty )
                {
                    var groupTypeRoleService = new Rock.Model.GroupTypeRoleService( rockContext );
                    groupRoles = groupTypeRoleService.Queryable()
                        .Where( r => r.GroupType.Guid == options.GroupTypeGuid )
                        .OrderBy( r => r.Order )
                        .ThenBy( r => r.Name )
                        .Select( r => new ListItemBag { Text = r.Name, Value = r.Guid.ToString() } )
                        .ToList();
                }

                return Ok( groupRoles );
            }
        }

        #endregion

        #region Group Member Picker

        /// <summary>
        /// Gets the group members that can be displayed in the group member picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group members.</returns>
        [HttpPost]
        [Route( "GroupMemberPickerGetGroupMembers" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "E0A893FD-0275-4251-BA6E-F669F110D179" )]
        public IActionResult GroupMemberPickerGetGroupMembers( [FromBody] GroupMemberPickerGetGroupMembersOptionsBag options )
        {
            Rock.Model.Group group;

            if ( !options.GroupGuid.HasValue )
            {
                return NotFound();
            }

            group = new GroupService( new RockContext() ).Get( options.GroupGuid.Value );

            if ( group == null || !group.Members.Any() )
            {
                return NotFound();
            }

            var list = new List<ListItemBag>();

            foreach ( var groupMember in group.Members.OrderBy( m => m.Person.FullName ) )
            {
                var li = new ListItemBag
                {
                    Text = groupMember.Person.FullName,
                    Value = groupMember.Guid.ToString()
                };

                list.Add( li );
            }

            return Ok( list );
        }

        #endregion

        #region Group Member Requirement Card

        /// <summary>
        /// Get the data needed to properly display the GroupMemberRequirementCard control
        /// </summary>
        /// <param name="options">The options that describe which data to load.</param>
        /// <returns>A <see cref="GroupMemberRequirementCardGetConfigResultsBag"/> containing everything the card needs to be displayed.</returns>
        [HttpPost]
        [Route( "GroupMemberRequirementCardGetConfig" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( GroupMemberRequirementCardGetConfigResultsBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "E3981034-6A58-48CB-85ED-F9900AA99934" )]
        public IActionResult GroupMemberRequirementCardGetConfig( [FromBody] GroupMemberRequirementCardGetConfigOptionsBag options )
        {
            if ( options.GroupRequirementGuid.IsEmpty() || options.GroupMemberRequirementGuid.IsEmpty() )
            {
                return BadRequest( "GroupRequirementGuid and GroupMemberRequirementGuid are required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var currentPerson = RockRequestContext.CurrentPerson;
                var groupRequirement = new GroupRequirementService( rockContext ).Get( options.GroupRequirementGuid );
                var groupMemberRequirement = new GroupMemberRequirementService( rockContext ).Get( options.GroupMemberRequirementGuid );

                if ( groupMemberRequirement == null || groupRequirement == null )
                {
                    return NotFound();
                }

                var LabelKey = new
                {
                    RequirementMet = " Requirement Met",
                    RequirementNotMet = " Requirement Not Met",
                    RequirementMetWithWarning = "Requirement Met With Warning"
                };
                var groupRequirementType = groupRequirement.GroupRequirementType;
                var results = new GroupMemberRequirementCardGetConfigResultsBag();

                if (
                    groupRequirement.GroupRequirementType.RequirementCheckType == RequirementCheckType.Manual
                    && options.MeetsGroupRequirement != MeetsGroupRequirement.Meets
                )
                {
                    results.ManualRequirementControl = new GroupMemberRequirementCardSubControlConfigBag
                    {
                        Enabled = true,
                        Label = groupRequirement.GroupRequirementType.CheckboxLabel.IsNotNullOrWhiteSpace()
                            ? groupRequirement.GroupRequirementType.CheckboxLabel
                            : groupRequirement.GroupRequirementType.Name,
                        Icon = "fa fa-square-o fa-fw"
                    };
                }

                if ( options.CanOverride && options.MeetsGroupRequirement != MeetsGroupRequirement.Meets )
                {
                    results.OverrideRequirementControl = new GroupMemberRequirementCardSubControlConfigBag
                    {
                        Enabled = true,
                        Label = "Mark as Met",
                        Icon = "fa fa-check-circle-o fa-fw"
                    };
                }

                var hasNotMetWorkflow = groupRequirementType.WarningWorkflowTypeId.HasValue
                    && !groupRequirementType.ShouldAutoInitiateDoesNotMeetWorkflow
                    && options.MeetsGroupRequirement == MeetsGroupRequirement.NotMet;

                var hasWarningWorkflow = groupRequirementType.WarningWorkflowTypeId.HasValue
                    && !groupRequirementType.ShouldAutoInitiateWarningWorkflow
                    && options.MeetsGroupRequirement == MeetsGroupRequirement.MeetsWithWarning;

                if ( hasNotMetWorkflow )
                {
                    results.NotMetWorkflowControl = new GroupMemberRequirementCardSubControlConfigBag
                    {
                        Enabled = true,
                        Label = groupRequirementType.DoesNotMeetWorkflowLinkText.IsNotNullOrWhiteSpace()
                            ? groupRequirementType.DoesNotMeetWorkflowLinkText
                            : "Requirement Not Met",
                        Icon = "fa fa-play-circle-o fa-fw"
                    };
                }

                if ( hasWarningWorkflow )
                {
                    results.WarningWorkflowControl = new GroupMemberRequirementCardSubControlConfigBag
                    {
                        Enabled = true,
                        Label = groupRequirementType.WarningWorkflowLinkText.IsNotNullOrWhiteSpace() ?
                            groupRequirementType.WarningWorkflowLinkText :
                            "Requirement Met With Warning",
                        Icon = "fa fa-play-circle-o fa-fw"
                    };
                }

                if ( groupMemberRequirement.WasOverridden )
                {
                    results.IsOverridden = true;
                    results.OverriddenBy = groupMemberRequirement.OverriddenByPersonAlias.Person.FullName;
                    results.OverriddenAt = groupMemberRequirement.OverriddenDateTime.ToShortDateString();
                }

                results.Message = "Issue With Requirement.";

                switch ( options.MeetsGroupRequirement )
                {
                    case MeetsGroupRequirement.Meets:
                        results.Message = groupRequirementType.PositiveLabel.IsNotNullOrWhiteSpace() ? groupRequirementType.PositiveLabel : LabelKey.RequirementMet;
                        break;
                    case MeetsGroupRequirement.NotMet:
                        results.Message = groupRequirementType.NegativeLabel.IsNotNullOrWhiteSpace() ? groupRequirementType.NegativeLabel : LabelKey.RequirementNotMet;
                        break;
                    case MeetsGroupRequirement.MeetsWithWarning:
                        results.Message = groupRequirementType.WarningLabel.IsNotNullOrWhiteSpace() ? groupRequirementType.WarningLabel : LabelKey.RequirementMetWithWarning;
                        break;
                }

                results.Summary = groupRequirementType.Summary;

                return Ok( results );
            }
        }

        /// <summary>
        /// Manually mark a GroupMemberRequirement as met
        /// </summary>
        /// <param name="options">The options that describe which item to mark met</param>
        [HttpPost]
        [Route( "GroupMemberRequirementCardMarkMetManually" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "An empty response indicates success." )]
        [Rock.SystemGuid.RestActionGuid( "AE5A418A-645C-4EA5-A870-AA74F7109354" )]
        public IActionResult GroupMemberRequirementCardMarkMetManually( [FromBody] GroupMemberRequirementCardMarkMetManuallyOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
                var groupMemberRequirement = groupMemberRequirementService.Get( options.GroupMemberRequirementGuid );
                if ( groupMemberRequirement == null && !options.GroupRequirementGuid.IsEmpty() && !options.GroupMemberGuid.IsEmpty() )
                {
                    // Couldn't find the GroupMemberRequirement, so build a new one and mark it completed
                    var groupRequirementService = new GroupRequirementService( rockContext );
                    var groupRequirement = groupRequirementService.Get( options.GroupRequirementGuid );

                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMember = groupMemberService.Get( options.GroupMemberGuid );

                    if ( groupRequirement != null && groupMember != null )
                    {
                        groupMemberRequirement = new GroupMemberRequirement
                        {
                            GroupRequirementId = groupRequirement.Id,
                            GroupMemberId = groupMember.Id
                        };
                        groupMemberRequirementService.Add( groupMemberRequirement );
                    }
                }

                groupMemberRequirement.WasManuallyCompleted = true;
                groupMemberRequirement.ManuallyCompletedByPersonAliasId = RockRequestContext.CurrentPerson?.PrimaryAliasId;
                groupMemberRequirement.ManuallyCompletedDateTime = RockDateTime.Now;
                groupMemberRequirement.RequirementMetDateTime = RockDateTime.Now;

                rockContext.SaveChanges();

                return Ok();
            }
        }

        /// <summary>
        /// Manually override a GroupMemberRequirement as met
        /// </summary>
        /// <param name="options">The options that describe which item to override</param>
        [HttpPost]
        [Route( "GroupMemberRequirementCardOverrideMarkMet" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "An empty response indicates success." )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "DA54A9CE-840F-4629-B270-7FCBAC86312C" )]
        public IActionResult GroupMemberRequirementCardOverrideMarkMet( [FromBody] GroupMemberRequirementCardMarkMetManuallyOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
                var groupMemberRequirement = groupMemberRequirementService.Get( options.GroupMemberRequirementGuid );
                var groupRequirementService = new GroupRequirementService( rockContext );
                var groupRequirement = groupRequirementService.Get( options.GroupRequirementGuid );
                var currentPerson = RockRequestContext.CurrentPerson;

                // Determine if current person is authorized to override
                if ( currentPerson == null )
                {
                    return Unauthorized();
                }

                var currentPersonIsLeaderOfCurrentGroup = new GroupMemberService( rockContext )
                    .GetByGroupId( groupRequirement.Group.Id )
                    .Where( m => m.GroupRole.IsLeader )
                    .Select( m => m.PersonId )
                    .Contains( currentPerson.Id );

                bool currentPersonCanOverride = groupRequirement.AllowLeadersToOverride && currentPersonIsLeaderOfCurrentGroup;
                var hasPermissionToOverride = groupRequirement.GroupRequirementType.IsAuthorized( Rock.Security.Authorization.OVERRIDE, currentPerson );

                if ( !( currentPersonCanOverride || hasPermissionToOverride ) )
                {
                    return Unauthorized();
                }

                if ( groupMemberRequirement == null && !options.GroupRequirementGuid.IsEmpty() && !options.GroupMemberGuid.IsEmpty() )
                {
                    // Couldn't find the GroupMemberRequirement, so build a new one and mark it completed
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMember = groupMemberService.Get( options.GroupMemberGuid );

                    if ( groupRequirement != null && groupMember != null )
                    {
                        groupMemberRequirement = new GroupMemberRequirement
                        {
                            GroupRequirementId = groupRequirement.Id,
                            GroupMemberId = groupMember.Id
                        };
                        groupMemberRequirementService.Add( groupMemberRequirement );
                    }
                }

                groupMemberRequirement.WasOverridden = true;
                groupMemberRequirement.OverriddenByPersonAliasId = currentPerson.PrimaryAliasId;
                groupMemberRequirement.OverriddenDateTime = RockDateTime.Now;
                groupMemberRequirement.RequirementMetDateTime = RockDateTime.Now;

                rockContext.SaveChanges();

                return Ok();
            }
        }

        /// <summary>
        /// Run the "not met" workflow for the given GroupMemberRequirement
        /// </summary>
        /// <param name="options">The options that describe which requirement to run the workflow on</param>
        [HttpPost]
        [Route( "GroupMemberRequirementCardRunNotMetWorkflow" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "Custom result data to indicate what is displayed next." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "A9202026-CAF8-4B68-BE95-263FAE77F92D" )]
        public IActionResult GroupMemberRequirementCardRunNotMetWorkflow( [FromBody] GroupMemberRequirementCardRunWorkflowOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
                var groupMemberRequirement = groupMemberRequirementService.Get( options.GroupMemberRequirementGuid );
                var groupRequirementType = groupMemberRequirement.GroupRequirement.GroupRequirementType;

                if ( !groupRequirementType.DoesNotMeetWorkflowTypeId.HasValue )
                {
                    return BadRequest();
                }

                // Begin the workflow.
                var workflowType = WorkflowTypeCache.Get( groupRequirementType.DoesNotMeetWorkflowTypeId.Value );

                // If a workflow type is not persisted, let the user know that it did not work.
                if ( !workflowType.IsPersisted )
                {
                    return Ok( new
                    {
                        Alert = $"The Workflow Type '{workflowType.Name}' is not configured to be automatically persisted, and could not be started."
                    } );
                }

                if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                {
                    // If there is a workflow ID in the group member requirement, navigate to that workflow entry page, otherwise, activate one.
                    Rock.Model.Workflow workflow;
                    if ( groupMemberRequirement != null && groupMemberRequirement.DoesNotMeetWorkflowId.HasValue )
                    {
                        workflow = new Rock.Model.WorkflowService( new RockContext() ).Get( groupMemberRequirement.DoesNotMeetWorkflowId.Value );
                        var qryParams = new Dictionary<string, string>
                            {
                                { "WorkflowTypeGuid", workflowType.Guid.ToString() },
                                { "WorkflowGuid", workflow.Guid.ToString() }
                            };
                        var workflowLink = new PageReference( options.WorkflowEntryLinkedPageValue, qryParams );

                        return Ok( new
                        {
                            GoTo = workflowLink.BuildUrl()
                        } );
                    }
                    else
                    {
                        if ( groupMemberRequirement == null && !options.GroupRequirementGuid.IsEmpty() && !options.GroupMemberGuid.IsEmpty() )
                        {
                            // Couldn't find the GroupMemberRequirement, so build a new one and mark it completed
                            var groupRequirementService = new GroupRequirementService( rockContext );
                            var groupRequirement = groupRequirementService.Get( options.GroupRequirementGuid );

                            var groupMemberService = new GroupMemberService( rockContext );
                            var groupMember = groupMemberService.Get( options.GroupMemberGuid );

                            if ( groupRequirement != null && groupMember != null )
                            {
                                groupMemberRequirement = new GroupMemberRequirement
                                {
                                    GroupRequirementId = groupRequirement.Id,
                                    GroupMemberId = groupMember.Id
                                };
                                groupMemberRequirementService.Add( groupMemberRequirement );
                            }
                        }

                        workflow = Rock.Model.Workflow.Activate( workflowType, workflowType.Name );
                        workflow.SetAttributeValue( "Person", groupMemberRequirement?.GroupMember.Person.PrimaryAlias.Guid );
                        var processed = new Rock.Model.WorkflowService( new RockContext() ).Process( workflow, groupMemberRequirement, out List<string> workflowErrors );

                        if ( processed )
                        {
                            // Update the group member requirement with the workflow - could potentially overwrite an existing workflow ID, but that is expected.
                            groupMemberRequirement.DoesNotMeetWorkflowId = workflow.Id;
                            groupMemberRequirement.RequirementFailDateTime = RockDateTime.Now;
                            rockContext.SaveChanges();

                            if ( workflow.HasActiveEntryForm( RockRequestContext.CurrentPerson ) )
                            {
                                var message = $"A '{workflowType.Name}' workflow has been started.<br /><br />The new workflow has an active form that is ready for input.";

                                var qryParams = new Dictionary<string, string>
                                {
                                    { "WorkflowTypeGuid", workflowType.Guid.ToString() },
                                    { "WorkflowGuid", workflow.Guid.ToString() }
                                };

                                var workflowLink = new PageReference( options.WorkflowEntryLinkedPageValue, qryParams );

                                return Ok( new
                                {
                                    Open = workflowLink.BuildUrl(),
                                    Alert = message
                                } );
                            }
                            else
                            {
                                return Ok( new
                                {
                                    Alert = $"A '{workflowType.Name}' workflow was started."
                                } );
                            }
                        }
                    }
                }

                return Ok();
            }
        }

        /// <summary>
        /// Run the "warning" workflow for the given GroupMemberRequirement
        /// </summary>
        /// <param name="options">The options that describe which requirement to run the workflow on</param>
        [HttpPost]
        [Route( "GroupMemberRequirementCardRunWarningWorkflow" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "Custom result data to indicate what is displayed next." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "CD7F3FAF-975D-4BDA-8A2D-5E236E7942DD" )]
        public IActionResult GroupMemberRequirementCardRunWarningWorkflow( [FromBody] GroupMemberRequirementCardRunWorkflowOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupMemberRequirementService = new GroupMemberRequirementService( rockContext );
                var groupMemberRequirement = groupMemberRequirementService.Get( options.GroupMemberRequirementGuid );
                var groupRequirementType = groupMemberRequirement.GroupRequirement.GroupRequirementType;

                if ( !groupRequirementType.WarningWorkflowTypeId.HasValue )
                {
                    return BadRequest();
                }

                // Begin the workflow.
                var workflowType = WorkflowTypeCache.Get( groupRequirementType.WarningWorkflowTypeId.Value );

                // If a workflow type is not persisted, let the user know that it did not work.
                if ( !workflowType.IsPersisted )
                {
                    return Ok( new
                    {
                        Alert = $"The Workflow Type '{workflowType.Name}' is not configured to be automatically persisted, and could not be started."
                    } );
                }

                if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                {
                    // If there is a workflow ID in the group member requirement, navigate to that workflow entry page, otherwise, activate one.
                    Rock.Model.Workflow workflow;
                    if ( groupMemberRequirement != null && groupMemberRequirement.WarningWorkflowId.HasValue )
                    {
                        workflow = new Rock.Model.WorkflowService( new RockContext() ).Get( groupMemberRequirement.WarningWorkflowId.Value );
                        var qryParams = new Dictionary<string, string>
                            {
                                { "WorkflowTypeGuid", workflowType.Guid.ToString() },
                                { "WorkflowGuid", workflow.Guid.ToString() }
                            };
                        var workflowLink = new PageReference( options.WorkflowEntryLinkedPageValue, qryParams );

                        return Ok( new
                        {
                            GoTo = workflowLink.BuildUrl()
                        } );
                    }
                    else
                    {
                        if ( groupMemberRequirement == null && !options.GroupRequirementGuid.IsEmpty() && !options.GroupMemberGuid.IsEmpty() )
                        {
                            // Couldn't find the GroupMemberRequirement, so build a new one and mark it completed
                            var groupRequirementService = new GroupRequirementService( rockContext );
                            var groupRequirement = groupRequirementService.Get( options.GroupRequirementGuid );

                            var groupMemberService = new GroupMemberService( rockContext );
                            var groupMember = groupMemberService.Get( options.GroupMemberGuid );

                            if ( groupRequirement != null && groupMember != null )
                            {
                                groupMemberRequirement = new GroupMemberRequirement
                                {
                                    GroupRequirementId = groupRequirement.Id,
                                    GroupMemberId = groupMember.Id
                                };
                                groupMemberRequirementService.Add( groupMemberRequirement );
                            }
                        }

                        workflow = Rock.Model.Workflow.Activate( workflowType, workflowType.Name );
                        workflow.SetAttributeValue( "Person", groupMemberRequirement?.GroupMember.Person.PrimaryAlias.Guid );
                        var processed = new Rock.Model.WorkflowService( new RockContext() ).Process( workflow, groupMemberRequirement, out List<string> workflowErrors );

                        if ( processed )
                        {
                            // Update the group member requirement with the workflow - could potentially overwrite an existing workflow ID, but that is expected.
                            groupMemberRequirement.WarningWorkflowId = workflow.Id;
                            groupMemberRequirement.RequirementWarningDateTime = RockDateTime.Now;
                            rockContext.SaveChanges();

                            if ( workflow.HasActiveEntryForm( RockRequestContext.CurrentPerson ) )
                            {
                                var message = $"A '{workflowType.Name}' workflow has been started.<br /><br />The new workflow has an active form that is ready for input.";

                                var qryParams = new Dictionary<string, string>
                                {
                                    { "WorkflowTypeGuid", workflowType.Guid.ToString() },
                                    { "WorkflowGuid", workflow.Guid.ToString() }
                                };

                                var workflowLink = new PageReference( options.WorkflowEntryLinkedPageValue, qryParams );

                                return Ok( new
                                {
                                    Open = workflowLink.BuildUrl(),
                                    Alert = message
                                } );
                            }
                            else
                            {
                                return Ok( new
                                {
                                    Alert = $"A '{workflowType.Name}' workflow was started."
                                } );
                            }
                        }
                    }
                }

                return Ok();
            }
        }

        #endregion

        #region Group Member Requirements Container

        /// <summary>
        /// Get the data for each of the cards in the GroupMemberRequirementContainer
        /// </summary>
        /// <param name="options">The options that describe which data to load.</param>
        /// <returns>A <see cref="GroupMemberRequirementContainerGetDataResultsBag"/> containing everything the cards need to be displayed.</returns>
        [HttpPost]
        [Route( "GroupMemberRequirementContainerGetData" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( GroupMemberRequirementContainerGetDataResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "B1F29337-BD8B-4F62-A68E-F67C32E8CFDE" )]
        public IActionResult GroupMemberRequirementContainerGetData( [FromBody] GroupMemberRequirementContainerGetDataOptionsBag options )
        {
            var results = new GroupMemberRequirementContainerGetDataResultsBag
            {
                Errors = new List<GroupMemberRequirementErrorBag>(),
                CategorizedRequirements = new List<GroupMemberRequirementCategoryBag>()
            };

            using ( var rockContext = new RockContext() )
            {
                var groupRole = new GroupTypeRoleService( rockContext ).Get( options.GroupRoleGuid );
                var person = new PersonService( rockContext ).Get( options.PersonGuid );
                var group = new GroupService( rockContext ).Get( options.GroupGuid );

                var currentPerson = RockRequestContext.CurrentPerson;

                // Determine whether the current person is a leader of the chosen group.
                var groupMemberQuery = new GroupMemberService( rockContext ).GetByGroupGuid( options.GroupGuid );
                var currentPersonIsLeaderOfCurrentGroup = currentPerson == null ? false :
                    groupMemberQuery.Where( m => m.GroupRole.IsLeader ).Select( m => m.PersonId ).Contains( currentPerson.Id );

                IEnumerable<GroupRequirementStatus> groupRequirementStatuses = group.PersonMeetsGroupRequirements( rockContext, person?.Id ?? 0, groupRole.Id );

                // This collects the statuses by their requirement type category with empty / no category requirement types first, then it is by category name.
                var requirementCategories = groupRequirementStatuses
                .Select( s => new
                {
                    CategoryId = s.GroupRequirement.GroupRequirementType.CategoryId,
                    Name = s.GroupRequirement.GroupRequirementType.CategoryId.HasValue ? s.GroupRequirement.GroupRequirementType.Category.Name : string.Empty,
                    RequirementResults = groupRequirementStatuses.Where( gr => gr.GroupRequirement.GroupRequirementType.CategoryId == s.GroupRequirement.GroupRequirementType.CategoryId ),
                } ).OrderBy( a => a.CategoryId.HasValue ).ThenBy( a => a.Name ).DistinctBy( a => a.CategoryId );

                var requirementsWithErrors = groupRequirementStatuses.Where( a => a.MeetsGroupRequirement == MeetsGroupRequirement.Error ).ToList();

                if ( requirementsWithErrors.Any() )
                {
                    var nbRequirementErrors = new GroupMemberRequirementErrorBag
                    {
                        Text = string.Format( "An error occurred in one or more of the requirement calculations" ),
                        Details = requirementsWithErrors.Select( a => string.Format( "{0}: {1}", a.GroupRequirement.GroupRequirementType.Name, a.CalculationException.Message ) ).ToList().AsDelimited( Environment.NewLine )
                    };
                    results.Errors.Add( nbRequirementErrors );
                }

                foreach ( var requirementCategory in requirementCategories )
                {
                    var newCategory = new GroupMemberRequirementCategoryBag
                    {
                        Id = requirementCategory.CategoryId,
                        Name = requirementCategory.Name,
                        MemberRequirements = new List<GroupMemberRequirementCardConfigBag>()
                    };

                    // Set up Security or Override access.

                    // Add the Group Member Requirement Cards here.
                    foreach ( var requirementStatus in requirementCategory.RequirementResults.OrderBy( r => r.GroupRequirement.GroupRequirementType.Name ) )
                    {
                        bool leaderCanOverride = requirementStatus.GroupRequirement.AllowLeadersToOverride && currentPersonIsLeaderOfCurrentGroup;
                        var hasPermissionToOverride = requirementStatus.GroupRequirement.GroupRequirementType.IsAuthorized( Rock.Security.Authorization.OVERRIDE, currentPerson );
                        var isAuthorized = requirementStatus.GroupRequirement.GroupRequirementType.IsAuthorized( Rock.Security.Authorization.VIEW, currentPerson );
                        var groupMemberRequirement = new GroupMemberRequirementService( rockContext ).Get( requirementStatus.GroupMemberRequirementId ?? 0 );

                        // Do not render cards where the current person is not authorized, or the status is "Not Applicable" or "Error".
                        if ( isAuthorized && requirementStatus.MeetsGroupRequirement != MeetsGroupRequirement.NotApplicable && requirementStatus.MeetsGroupRequirement != MeetsGroupRequirement.Error )
                        {
                            var card = new GroupMemberRequirementCardConfigBag
                            {
                                Title = requirementStatus.GroupRequirement.GroupRequirementType.Name,
                                TypeIconCssClass = requirementStatus.GroupRequirement.GroupRequirementType.IconCssClass,
                                MeetsGroupRequirement = requirementStatus.MeetsGroupRequirement,
                                GroupRequirementGuid = requirementStatus.GroupRequirement.Guid,
                                GroupRequirementTypeGuid = requirementStatus.GroupRequirement.GroupRequirementType.Guid,
                                GroupMemberRequirementGuid = groupMemberRequirement.Guid,
                                GroupMemberRequirementDueDate = requirementStatus.RequirementDueDate?.ToShortDateString(),
                                CanOverride = leaderCanOverride || hasPermissionToOverride
                            };

                            newCategory.MemberRequirements.Add( card );
                        }
                    }

                    results.CategorizedRequirements.Add( newCategory );
                }
                return Ok( results );
            }
        }

        #endregion

        #region Group Type Group Picker

        /// <summary>
        /// Gets the groups that can be displayed in the group type group picker for the specified group type.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the groups.</returns>
        [HttpPost]
        [Route( "GroupTypeGroupPickerGetGroups" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "f07ac6f8-128c-4881-a4ec-c245b8f10f9e" )]
        public IActionResult GroupTypeGroupPickerGetGroups( [FromBody] GroupTypeGroupPickerGetGroupsOptionsBag options )
        {
            var groups = new List<ListItemBag>();
            if ( options.GroupTypeGuid != Guid.Empty )
            {
                var groupService = new Rock.Model.GroupService( new RockContext() );
                groups = groupService.Queryable()
                    .Where( g => g.GroupType.Guid == options.GroupTypeGuid )
                    .OrderBy( g => g.Name )
                    .Select( g => new ListItemBag { Text = g.Name, Value = g.Guid.ToString() } )
                    .ToList();
            }

            return Ok( groups );
        }

        /// <summary>
        /// Gets the groups that can be displayed in the group type group picker for the specified group type.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the groups.</returns>
        [HttpPost]
        [Route( "GroupTypeGroupPickerGetGroupTypeOfGroup" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "984ce064-6073-4b8d-b670-338a3049e13b" )]
        public IActionResult GroupTypeGroupPickerGetGroupTypeOfGroup( [FromBody] GroupTypeGroupPickerGetGroupTypeOfGroupOptionsBag options )
        {
            if ( options.GroupGuid != Guid.Empty )
            {
                var groupService = new Rock.Model.GroupService( new RockContext() );
                var group = groupService.Get( options.GroupGuid );

                if ( group == null )
                {
                    return NotFound();
                }

                return Ok( new ListItemBag { Text = group.GroupType.Name, Value = group.GroupType.Guid.ToString() } );
            }

            return NotFound();
        }

        #endregion

        #region Group Type Picker

        /// <summary>
        /// Gets the group types that can be displayed in the group type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group types.</returns>
        [HttpPost]
        [Route( "GroupTypePickerGetGroupTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "b0e07419-0e3c-4235-b5d4-4262fd63e050" )]
        public IActionResult GroupTypePickerGetGroupTypes( [FromBody] GroupTypePickerGetGroupTypesOptionsBag options )
        {
            var groupTypes = new List<GroupTypeCache>();
            var results = new List<ListItemBag>();

            if ( options.GroupTypes == null || options.GroupTypes.Count < 1 )
            {
                groupTypes = GroupTypeCache.All();
            }
            else
            {
                foreach ( var groupTypeGuid in options.GroupTypes )
                {
                    var groupType = GroupTypeCache.Get( groupTypeGuid );
                    groupTypes.Add( groupType );
                }
            }

            if ( options.OnlyGroupListItems )
            {
                // get all group types that have the ShowInGroupList flag set
                groupTypes = groupTypes.Where( a => a.ShowInGroupList ).ToList();
            }

            if ( options.IsSortedByName )
            {
                groupTypes = groupTypes.OrderBy( gt => gt.Name ).ToList();
            }
            else
            {
                groupTypes = groupTypes.OrderBy( gt => gt.Order ).ThenBy( gt => gt.Name ).ToList();
            }

            foreach ( var gt in groupTypes )
            {
                results.Add( new ListItemBag { Text = gt.Name, Value = gt.Guid.ToString() } );
            }

            return Ok( results );
        }

        #endregion

        #region Group Picker

        /// <summary>
        /// Gets the groups that can be displayed in the group picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent the groups.</returns>
        [HttpPost]
        [Route( "GroupPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "c4f5432a-eb1e-4235-a5cd-bde37cc324f7" )]
        public IActionResult GroupPickerGetChildren( GroupPickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var groupService = new GroupService( rockContext );

                List<int> includedGroupTypeIds = options.IncludedGroupTypeGuids
                    .Select( ( guid ) =>
                    {
                        var gt = GroupTypeCache.Get( guid );

                        if ( gt != null )
                        {
                            return gt.Id;
                        }

                        return 0;
                    } )
                    .ToList();

                // if specific group types are specified, show the groups regardless of ShowInNavigation
                bool limitToShowInNavigation = !includedGroupTypeIds.Any();

                Rock.Model.Group parentGroup = groupService.GetByGuid( options.Guid ?? Guid.Empty );
                int id = parentGroup == null ? 0 : parentGroup.Id;

                Rock.Model.Group rootGroup = groupService.GetByGuid( options.RootGroupGuid ?? Guid.Empty );
                int rootGroupId = rootGroup == null ? 0 : rootGroup.Id;

                var qry = groupService
                    .GetChildren( id, rootGroupId, false, includedGroupTypeIds, new List<int>(), options.IncludeInactiveGroups, limitToShowInNavigation, 0, false, false )
                    .AsNoTracking();

                List<Rock.Model.Group> groupList = new List<Rock.Model.Group>();
                List<TreeItemBag> groupNameList = new List<TreeItemBag>();

                var person = GetPerson();

                if ( parentGroup == null )
                {
                    parentGroup = rootGroup;
                }

                List<int> groupIdsWithSchedulingEnabledWithAncestors = null;
                List<int> groupIdsWithRSVPEnabledWithAncestors = null;

                var listOfChildGroups = qry.ToList().OrderBy( g => g.Order ).ThenBy( g => g.Name ).ToList();
                if ( listOfChildGroups.Any() )
                {
                    if ( options.LimitToSchedulingEnabled )
                    {
                        groupIdsWithSchedulingEnabledWithAncestors = groupService.GetGroupIdsWithSchedulingEnabledWithAncestors();
                    }

                    if ( options.LimitToRSVPEnabled )
                    {
                        groupIdsWithRSVPEnabledWithAncestors = groupService.GetGroupIdsWithRSVPEnabledWithAncestors();
                    }
                }

                foreach ( var group in listOfChildGroups )
                {
                    // we already have the ParentGroup record, so lets set it for each group to avoid a database round-trip during Auth
                    group.ParentGroup = parentGroup;

                    var groupType = GroupTypeCache.Get( group.GroupTypeId );

                    //// Before checking Auth, filter based on the limitToSchedulingEnabled and limitToRSVPEnabled option.
                    //// Auth takes longer to check, so if we can rule the group out sooner, that will save a bunch of time

                    if ( options.LimitToSchedulingEnabled )
                    {
                        var includeGroup = false;
                        if ( groupType?.IsSchedulingEnabled == true )
                        {
                            // if this group's group type has scheduling enabled, we will include this group
                            includeGroup = true;
                        }
                        else
                        {
                            // if this group's group type does not have scheduling enabled, we will need to include it if any of its children
                            // have scheduling enabled

                            if ( groupIdsWithSchedulingEnabledWithAncestors != null )
                            {
                                bool hasChildScheduledEnabledGroups = groupIdsWithSchedulingEnabledWithAncestors.Contains( group.Id );
                                if ( hasChildScheduledEnabledGroups )
                                {
                                    includeGroup = true;
                                }
                            }
                        }

                        if ( !includeGroup )
                        {
                            continue;
                        }
                    }

                    if ( options.LimitToRSVPEnabled )
                    {
                        var includeGroup = false;
                        if ( groupType?.EnableRSVP == true )
                        {
                            // if this group's group type has RSVP enabled, we will include this group
                            includeGroup = true;
                        }
                        else
                        {
                            if ( groupIdsWithRSVPEnabledWithAncestors != null )
                            {
                                bool hasChildRSVPEnabledGroups = groupIdsWithRSVPEnabledWithAncestors.Contains( group.Id );
                                if ( hasChildRSVPEnabledGroups )
                                {
                                    includeGroup = true;
                                }
                            }
                        }

                        if ( !includeGroup )
                        {
                            continue;
                        }
                    }

                    bool groupIsAuthorized = group.IsAuthorized( Rock.Security.Authorization.VIEW, person );
                    if ( !groupIsAuthorized )
                    {
                        continue;
                    }

                    groupList.Add( group );
                    var treeViewItem = new TreeItemBag();
                    treeViewItem.Value = group.Guid.ToString();
                    treeViewItem.Text = group.Name;
                    treeViewItem.IsActive = group.IsActive;

                    // if there a IconCssClass is assigned, use that as the Icon.
                    treeViewItem.IconCssClass = groupType?.IconCssClass;

                    groupNameList.Add( treeViewItem );
                }

                // try to quickly figure out which items have Children
                List<int> resultIds = groupList.Select( a => a.Id ).ToList();
                var qryHasChildren = groupService.Queryable().AsNoTracking()
                    .Where( g =>
                        g.ParentGroupId.HasValue &&
                        resultIds.Contains( g.ParentGroupId.Value ) );

                if ( includedGroupTypeIds.Any() )
                {
                    qryHasChildren = qryHasChildren.Where( a => includedGroupTypeIds.Contains( a.GroupTypeId ) );
                }

                var qryHasChildrenList = qryHasChildren
                    .Select( g => g.ParentGroup.Guid )
                    .Distinct()
                    .ToList();

                foreach ( var g in groupNameList )
                {
                    Guid groupGuid = g.Value.AsGuid();
                    g.HasChildren = qryHasChildrenList.Any( a => a == groupGuid );
                }

                return Ok( groupNameList );
            }
        }

        #endregion

        #region Group Role Picker

        /// <summary>
        /// Gets the group types that can be displayed in the group role picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group types.</returns>
        [HttpPost]
        [Route( "GroupRolePickerGetGroupTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "56891c9b-f714-4083-8252-4c73b358aa02" )]
        public IActionResult GroupRolePickerGetGroupTypes()
        {
            using ( var rockContext = new RockContext() )
            {
                var groupTypeService = new Rock.Model.GroupTypeService( rockContext );

                // get all group types that have at least one role
                var groupTypes = groupTypeService.Queryable()
                    .Where( a => a.Roles.Any() )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name )
                    .Select( g => new ListItemBag { Text = g.Name, Value = g.Guid.ToString() } )
                    .ToList();

                return Ok( groupTypes );
            }
        }

        /// <summary>
        /// Gets the group roles that can be displayed in the group role picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group roles.</returns>
        [HttpPost]
        [Route( "GroupRolePickerGetGroupRoles" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "968033ab-2596-4b0c-b06e-2c9cf59949c5" )]
        public IActionResult GroupRolePickerGetGroupRoles( [FromBody] GroupRolePickerGetGroupRolesOptionsBag options )
        {
            return Ok( GroupRolePickerGetGroupRolesForGroupType( options.GroupTypeGuid, options.ExcludeGroupRoles ) );
        }

        /// <summary>
        /// Gets the group roles and group type information based on a selected group role.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>All the data for the selected role, selected type, and all of the group roles</returns>
        [HttpPost]
        [Route( "GroupRolePickerGetAllForGroupRole" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( GroupRolePickerGetAllForGroupRoleResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "e55374dd-7715-4392-a162-c40f09d25fc9" )]
        public IActionResult GroupRolePickerGetAllForGroupRole( [FromBody] GroupRolePickerGetAllForGroupRoleOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                List<Guid> excludeGroupRoles = options.ExcludeGroupRoles;

                var groupRoleService = new Rock.Model.GroupTypeRoleService( rockContext );
                var groupRole = groupRoleService.Queryable()
                    .Where( r => r.Guid == options.GroupRoleGuid )
                    .First();

                var groupType = groupRole.GroupType;

                var groupRoles = GroupRolePickerGetGroupRolesForGroupType( groupType.Guid, excludeGroupRoles, rockContext );

                return Ok( new GroupRolePickerGetAllForGroupRoleResultsBag
                {
                    SelectedGroupRole = new ListItemBag { Text = groupRole.Name, Value = groupRole.Guid.ToString() },
                    SelectedGroupType = new ListItemBag { Text = groupType.Name, Value = groupType.Guid.ToString() },
                    GroupRoles = groupRoles
                } );
            }
        }

        /// <summary>
        /// Gets the group roles that can be displayed in the group role picker.
        /// </summary>
        /// <param name="groupTypeGuid">Load group roles of this type</param>
        /// <param name="excludeGroupRoles">Do not include these roles in the result</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group roles.</returns>
        private List<ListItemBag> GroupRolePickerGetGroupRolesForGroupType( Guid groupTypeGuid, List<Guid> excludeGroupRoles )
        {
            using ( var rockContext = new RockContext() )
            {
                return GroupRolePickerGetGroupRolesForGroupType( groupTypeGuid, excludeGroupRoles, rockContext );
            }
        }

        /// <summary>
        /// Gets the group roles that can be displayed in the group role picker.
        /// </summary>
        /// <param name="groupTypeGuid">Load group roles of this type</param>
        /// <param name="excludeGroupRoles">Do not include these roles in the result</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group roles.</returns>
        private List<ListItemBag> GroupRolePickerGetGroupRolesForGroupType( Guid groupTypeGuid, List<Guid> excludeGroupRoles, RockContext rockContext )
        {
            var groupRoleService = new Rock.Model.GroupTypeRoleService( rockContext );

            var groupRoles = groupRoleService.Queryable()
                .Where( r =>
                    r.GroupType.Guid == groupTypeGuid &&
                    !excludeGroupRoles.Contains( r.Guid ) )
                .OrderBy( r => r.Name )
                .Select( r => new ListItemBag { Text = r.Name, Value = r.Guid.ToString() } )
                .ToList();

            return groupRoles;
        }

        #endregion

        #region In Group Filter (Reporting Data Filter)

        /// <summary>
        /// Gets the group roles for the provided groups and/or their children.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the group roles.</returns>
        [HttpPost]
        [Route( "InGroupFilterGetGroupRolesForGroups" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "EAC8814B-8FAD-408F-B76F-703A3F529197" )]
        public IActionResult InGroupFilterGetGroupRolesForGroups( [FromBody] InGroupFilterGetGroupRolesForGroupsOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var groupRoles = GetGroupTypeRolesForSelectedGroups(
                    options.GroupGuids,
                    options.IncludeChildGroups,
                    options.IncludeSelectedGroups,
                    options.IncludeAllDescendants,
                    options.IncludeInactiveGroups,
                    rockContext
                );

                return Ok( groupRoles );
            }
        }

        /// <summary>
        /// Gets a list of group type roles that should be available for
        /// selection in the filter settings UI.
        /// </summary>
        /// <param name="groupGuids">The integer identifiers of the selected groups.</param>
        /// <param name="includeChildGroups">If <c>true</c> then child groups will be included when determining which group types to include.</param>
        /// <param name="includeSelectedGroups">If <paramref name="includeChildGroups"/> and this are <c>true</c> then the originally selected groups will be included along with the child groups when determining the group types.</param>
        /// <param name="includeAllDescendants">If <paramref name="includeChildGroups"/> and this are <c>true</c> then all descendant groups will be included when determining the group types.</param>
        /// <param name="includeInactiveGroups">If <paramref name="includeChildGroups"/> and this are <c>true</c> then inactive groups will be included when determining the group types.</param>
        /// <param name="rockContext">The context to use when accessing the database.</param>
        /// <returns>A list of <see cref="ListItemBag"/> objects that represent the options to display to the individual.</returns>
        private List<ListItemBag> GetGroupTypeRolesForSelectedGroups( List<Guid> groupGuids, bool includeChildGroups, bool includeSelectedGroups, bool includeAllDescendants, bool includeInactiveGroups, RockContext rockContext )
        {
            var groupService = new GroupService( rockContext );
            var groupTypeRoleService = new GroupTypeRoleService( rockContext );
            var qryGroupTypeRoles = groupTypeRoleService.Queryable();

            var selectedGroups = groupService.GetByGuids( groupGuids )
                .Select( s => new
                {
                    s.Id,
                    s.GroupTypeId
                } )
                .ToList();

            var selectedGroupTypeIds = selectedGroups.Select( a => a.GroupTypeId )
                .Distinct()
                .ToList();

            if ( includeChildGroups )
            {
                var childGroupTypeIds = new List<int>();

                foreach ( var groupId in selectedGroups.Select( a => a.Id ).ToList() )
                {
                    if ( includeAllDescendants )
                    {
                        // Get all children and descendants of the selected group(s).
                        var descendantGroupTypes = groupService.GetAllDescendentsGroupTypes( groupId, includeInactiveGroups );

                        childGroupTypeIds.AddRange( descendantGroupTypes.Select( a => a.Id ).ToList() );
                    }
                    else
                    {
                        // Get only immediate children of the selected group(s).
                        var childGroups = groupService.Queryable().Where( a => a.ParentGroupId == groupId );

                        if ( !includeInactiveGroups )
                        {
                            childGroups = childGroups.Where( a => a.IsActive == true );
                        }

                        childGroupTypeIds.AddRange( childGroups.Select( a => a.GroupTypeId ).Distinct().ToList() );
                    }
                }

                childGroupTypeIds = childGroupTypeIds.Distinct().ToList();

                if ( includeSelectedGroups )
                {
                    qryGroupTypeRoles = qryGroupTypeRoles.Where( a => a.GroupTypeId.HasValue
                        && ( selectedGroupTypeIds.Contains( a.GroupTypeId.Value ) || childGroupTypeIds.Contains( a.GroupTypeId.Value ) ) );
                }
                else
                {
                    qryGroupTypeRoles = qryGroupTypeRoles.Where( a => a.GroupTypeId.HasValue
                        && childGroupTypeIds.Contains( a.GroupTypeId.Value ) );
                }
            }
            else
            {
                qryGroupTypeRoles = qryGroupTypeRoles.Where( a => a.GroupTypeId.HasValue
                    && selectedGroupTypeIds.Contains( a.GroupTypeId.Value ) );
            }

            return qryGroupTypeRoles.OrderBy( a => a.GroupType.Order )
                .ThenBy( a => a.GroupType.Name )
                .ThenBy( a => a.Order )
                .ThenBy( a => a.Name )
                .Select( a => new
                {
                    a.Guid,
                    a.Name,
                    GroupTypeName = a.GroupType.Name
                } )
                .ToList()
                .Select( a => new ListItemBag
                {
                    Value = a.Guid.ToString(),
                    Text = $"{a.Name} ({a.GroupTypeName})"
                } )
                .ToList();
        }

        #endregion

        #region Interaction Channel Interaction Component Picker

        /// <summary>
        /// Gets the interaction channel that the given interaction component is a part of.
        /// </summary>
        /// <returns>A <see cref="ListItemBag"/> object that represents the interaction channel.</returns>
        [HttpPost]
        [Route( "InteractionChannelInteractionComponentPickerGetChannelFromComponent" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "ebef7cb7-f20d-40d9-9f70-1f30aff1cd8f" )]
        public IActionResult InteractionChannelInteractionComponentPickerGetChannelFromComponent( [FromBody] InteractionChannelInteractionComponentPickerGetChannelFromComponentOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var interactionComponentService = new InteractionComponentService( rockContext );
                var component = interactionComponentService.Get( options.InteractionComponentGuid );

                if ( component == null )
                {
                    return NotFound();
                }

                var channel = component.InteractionChannel;

                return Ok( new ListItemBag { Text = $"{channel.Name} ({channel.ChannelTypeMediumValue.Value ?? string.Empty})", Value = channel.Guid.ToString() } );
            }
        }

        #endregion

        #region Interaction Channel Picker

        /// <summary>
        /// Gets the interaction channels that can be displayed in the interaction channel picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the interaction channels.</returns>
        [HttpPost]
        [Route( "InteractionChannelPickerGetInteractionChannels" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "2F855DC7-7C20-4C09-9CB1-FFC1E022385B" )]
        public IActionResult InteractionChannelPickerGetInteractionChannels()
        {
            var items = new List<ListItemBag>();
            var rockContext = new RockContext();
            var interactionChannelService = new InteractionChannelService( rockContext );
            var channels = interactionChannelService.Queryable().AsNoTracking()
                .Include( "ChannelTypeMediumValue" )
                .Where( ic => ic.IsActive )
                .OrderBy( ic => ic.Name )
                .Select( ic => new
                {
                    ic.Name,
                    ic.Guid,
                    Medium = ic.ChannelTypeMediumValue.Value
                } )
                .ToList();

            foreach ( var channel in channels )
            {
                ListItemBag li;

                if ( channel.Medium.IsNullOrWhiteSpace() )
                {
                    li = new ListItemBag { Text = channel.Name, Value = channel.Guid.ToString() };
                }
                else
                {
                    li = new ListItemBag { Text = $"{channel.Name} ({channel.Medium ?? string.Empty})", Value = channel.Guid.ToString() };
                }

                items.Add( li );
            }

            return Ok( items );
        }

        #endregion

        #region Interaction Component Picker

        /// <summary>
        /// Gets the interection components that can be displayed in the interection component picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the interection components.</returns>
        [HttpPost]
        [Route( "InteractionComponentPickerGetInteractionComponents" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "BD61A390-39F9-4FDE-B9AD-02E53B5F2073" )]
        public IActionResult InteractionComponentPickerGetInteractionComponents( [FromBody] InteractionComponentPickerGetInteractionComponentsOptionsBag options )
        {
            if ( !options.InteractionChannelGuid.HasValue )
            {
                return NotFound();
            }

            int interactionChannelId = InteractionChannelCache.GetId( options.InteractionChannelGuid.Value ) ?? 0;
            var rockContext = new RockContext();
            var interactionComponentService = new InteractionComponentService( rockContext );

            var components = interactionComponentService.Queryable().AsNoTracking()
                .Where( ic => ic.InteractionChannelId == interactionChannelId )
                .OrderBy( ic => ic.Name )
                .Select( ic => new ListItemBag
                {
                    Text = ic.Name,
                    Value = ic.Guid.ToString()
                } )
                .ToList();

            return Ok( components );
        }

        #endregion

        #region Lava Command Picker

        /// <summary>
        /// Gets the lava commands that can be displayed in the lava command picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the lava commands.</returns>
        [HttpPost]
        [Route( "LavaCommandPickerGetLavaCommands" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "9FD03EE7-49E8-4C64-AC25-648422579F28" )]
        public IActionResult LavaCommandPickerGetLavaCommands()
        {
            var items = new List<ListItemBag>();

            items.Add( new ListItemBag { Text = "All", Value = "All" } );

            foreach ( var command in Rock.Lava.LavaHelper.GetLavaCommands() )
            {
                items.Add( new ListItemBag { Text = command.SplitCase(), Value = command } );
            }

            return Ok( items );
        }

        #endregion

        #region Learning Class Activity Picker

        /// <summary>
        /// Gets the lava commands that can be displayed in the lava command picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the lava commands.</returns>
        [HttpPost]
        [Route( "LearningClassActivityPickerGetLearningClassActivities" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "C37F74D9-BB42-4544-AC3B-F48543F497E1" )]
        public IActionResult LearningClassActivityPickerGetLearningClassActivities( [FromBody] LearningClassActivityPickerGetLearningClassActivitiesOptionsBag options )
        {
            if ( !options.LearningClassGuid.HasValue )
            {
                return NotFound();
            }

            var selectedClassGuid = options.LearningClassGuid.Value;
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
            var items = new List<ListItemBag>();

            var learningClasses = new LearningClassActivityService( new RockContext() )
                .Queryable()
                .Where( lca => lca.LearningClass.Guid == selectedClassGuid )
                .OrderBy( lca => lca.Order )
                .ToList();

            foreach ( var lca in learningClasses )
            {
                if ( lca.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || ( grant?.IsAccessGranted( lca, Security.Authorization.VIEW ) == true ) )
                {
                    items.Add( new ListItemBag { Text = lca.Name, Value = lca.Guid.ToString() } );
                }
            }

            return Ok( items );
        }

        #endregion

        #region Learning Class Picker

        /// <summary>
        /// Gets the lava commands that can be displayed in the lava command picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the lava commands.</returns>
        [HttpPost]
        [Route( "LearningClassPickerGetLearningClasses" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "C5739387-B814-4ED5-9182-CD204529E8BB" )]
        public IActionResult LearningClassPickerGetLearningClasses( [FromBody] LearningClassPickerGetLearningClassesOptionsBag options )
        {
            if ( !options.LearningCourseGuid.HasValue )
            {
                return NotFound();
            }

            var selectedCourseGuid = options.LearningCourseGuid.Value;
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
            var items = new List<ListItemBag>();

            var learningClasses = new LearningClassService( new RockContext() )
                .Queryable()
                .Where( lc => lc.LearningCourse.Guid == selectedCourseGuid )
                .OrderBy( lc => lc.Order )
                .ToList();

            foreach ( var lc in learningClasses )
            {
                if ( lc.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || ( grant?.IsAccessGranted( lc, Security.Authorization.VIEW ) == true ) )
                {
                    items.Add( new ListItemBag { Text = lc.Name, Value = lc.Guid.ToString() } );
                }
            }

            return Ok( items );
        }

        #endregion

        #region Location Item Picker

        /// <summary>
        /// Gets the child locations, excluding inactive items.
        /// </summary>
        /// <param name="options">The options that describe which child locations to retrieve.</param>
        /// <returns>A collection of <see cref="TreeItemBag"/> objects that represent the child locations.</returns>
        [HttpPost]
        [Route( "LocationItemPickerGetActiveChildren" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "E57312EC-92A7-464C-AA7E-5320DDFAEF3D" )]
        public IActionResult LocationItemPickerGetActiveChildren( [FromBody] LocationItemPickerGetActiveChildrenOptionsBag options )
        {
            IQueryable<Location> qry;

            using ( var rockContext = new RockContext() )
            {
                var locationService = new LocationService( rockContext );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                if ( options.Guid == Guid.Empty )
                {
                    qry = locationService.Queryable().AsNoTracking().Where( a => a.ParentLocationId == null );
                    if ( options.RootLocationGuid != Guid.Empty )
                    {
                        qry = qry.Where( a => a.Guid == options.RootLocationGuid );
                    }
                }
                else
                {
                    qry = locationService.Queryable().AsNoTracking().Where( a => a.ParentLocation.Guid == options.Guid );
                }

                // limit to only active locations.
                qry = qry.Where( a => a.IsActive );

                // limit to only Named Locations (don't show home addresses, etc)
                qry = qry.Where( a => a.Name != null && a.Name != string.Empty );

                List<Location> locationList = new List<Location>();
                List<TreeItemBag> locationNameList = new List<TreeItemBag>();

                var person = GetPerson();

                foreach ( var location in qry.OrderBy( l => l.Name ) )
                {
                    if ( location.IsAuthorized( Security.Authorization.VIEW, person ) || grant?.IsAccessGranted( location, Security.Authorization.VIEW ) == true )
                    {
                        locationList.Add( location );
                        var treeViewItem = new TreeItemBag();
                        treeViewItem.Value = location.Guid.ToString();
                        treeViewItem.Text = location.Name;
                        locationNameList.Add( treeViewItem );
                    }
                }

                // try to quickly figure out which items have Children
                List<int> resultIds = locationList.Select( a => a.Id ).ToList();

                var qryHasChildren = locationService.Queryable().AsNoTracking()
                    .Where( l =>
                        l.ParentLocationId.HasValue &&
                        resultIds.Contains( l.ParentLocationId.Value ) &&
                        l.IsActive
                    )
                    .Select( l => l.ParentLocation.Guid )
                    .Distinct()
                    .ToList();

                var qryHasChildrenList = qryHasChildren.ToList();

                foreach ( var item in locationNameList )
                {
                    var locationGuid = item.Value.AsGuid();
                    item.IsFolder = qryHasChildrenList.Any( a => a == locationGuid );
                    item.HasChildren = item.IsFolder;
                }

                return Ok( locationNameList );
            }
        }

        #endregion

        #region Location List

        /// <summary>
        /// Gets the child locations, excluding inactive items.
        /// </summary>
        /// <param name="options">The options that describe which child locations to retrieve.</param>
        /// <returns>A collection of <see cref="ListItemBag"/> objects that represent the child locations.</returns>
        [HttpPost]
        [Route( "LocationListGetLocations" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "DA17BFF5-B9B8-4CD1-AAB4-2F703EDBEF46" )]
        public IActionResult LocationListGetLocations( [FromBody] LocationListGetLocationsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                List<ListItemBag> locations = null;
                var locationService = new LocationService( rockContext );
                int parentLocationId = 0;
                int locationTypeValueId = 0;

                if ( options.ParentLocationGuid != null )
                {
                    var parentLocation = locationService.Get( options.ParentLocationGuid );
                    parentLocationId = parentLocation == null ? 0 : parentLocation.Id;
                }

                if ( options.LocationTypeValueGuid != null )
                {
                    var locationTypeDefinedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.LOCATION_TYPE.AsGuid() );
                    var locationTypeValue = DefinedValueCache.Get( options.LocationTypeValueGuid );

                    // Verify the given GUID is a LocationType GUID
                    if ( locationTypeValue != null && locationTypeDefinedType.Equals( locationTypeValue.DefinedType ) )
                    {
                        locationTypeValueId = locationTypeValue.Id;
                    }
                }

                var locationQuery = locationService
                    .Queryable()
                    .AsNoTracking()
                    .Where( l => l.IsActive )
                    .Where( l => locationTypeValueId == 0 || l.LocationTypeValueId == locationTypeValueId )
                    .Where( l => parentLocationId == 0 || l.ParentLocationId == parentLocationId )
                    .Select( l => new { l.Name, l.City, l.State, l.Guid } )
                    .ToList()
                    .OrderBy( l => l.Name );

                if ( options.ShowCityState )
                {
                    locations = locationQuery
                        .Select( l => new ListItemBag { Text = $"{l.Name} ({l.City}, {l.State})", Value = l.Guid.ToString() } )
                        .ToList();
                }
                else
                {
                    locations = locationQuery
                        .Where( l => l.Name.IsNotNullOrWhiteSpace() )
                        .Select( l => new ListItemBag { Text = $"{l.Name}", Value = l.Guid.ToString() } )
                        .ToList();
                }

                return Ok( locations );
            }
        }

        /// <summary>
        /// Get the attributes for Locations
        /// </summary>
        /// <returns>A list of attributes in a form the Attribute Values Container can use</returns>
        [HttpPost]
        [Route( "LocationListGetAttributes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<PublicAttributeBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "e2b28b2f-a46d-40cd-a48d-7e5351383de5" )]
        public IActionResult LocationListGetAttributes()
        {
            if ( RockRequestContext.CurrentPerson == null )
            {
                return Unauthorized();
            }

            return Ok( GetAttributes( new Location { Id = 0 } ) );
        }

        /// <summary>
        /// Save a new Location
        /// </summary>
        /// <param name="options">The data for the new Location</param>
        /// <returns>A <see cref="ListItemBag"/> representing the new Location.</returns>
        [HttpPost]
        [Route( "LocationListSaveNewLocation" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [Rock.SystemGuid.RestActionGuid( "f8342fdb-3e19-4f17-804c-c14fdee87a2b" )]
        public IActionResult LocationListSaveNewLocation( LocationListSaveNewLocationOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var locationService = new LocationService( rockContext );

                // Create and save new location with data from client
                var location = new Location
                {
                    Name = options.Name,
                    IsActive = true,
                };

                if ( options.Address != null )
                {
                    location.Street1 = options.Address.Street1;
                    location.Street2 = options.Address.Street2;
                    location.City = options.Address.City;
                    location.County = options.Address.Locality;
                    location.State = options.Address.State;
                    location.Country = options.Address.Country;
                    location.PostalCode = options.Address.PostalCode;
                }

                if ( options.ParentLocationGuid != null )
                {
                    Location parentLocation = locationService.Get( options.ParentLocationGuid );
                    location.ParentLocation = parentLocation;
                }

                if ( options.LocationTypeValueGuid != null )
                {
                    var locationTypeDefinedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.LOCATION_TYPE.AsGuid() );
                    var locationTypeValue = DefinedValueCache.Get( options.LocationTypeValueGuid );

                    // Verify the given GUID is a LocationType GUID
                    if ( locationTypeValue != null && locationTypeDefinedType.Equals( locationTypeValue.DefinedType ) )
                    {
                        location.LocationTypeValueId = locationTypeValue.Id;
                    }
                }

                locationService.Add( location );

                rockContext.SaveChanges();

                // Load up the new location's attributes and save those
                location.LoadAttributes();

                foreach ( KeyValuePair<string, AttributeValueCache> attr in location.AttributeValues )
                {
                    location.AttributeValues[attr.Key].Value = options.AttributeValues.GetValueOrNull( attr.Key );
                }

                if ( !location.IsValid )
                {
                    return InternalServerError();
                }

                location.SaveAttributeValues( rockContext );

                // Return a representation of the location so it can be used right away on the client
                return Ok( new ListItemBag
                {
                    Text = options.ShowCityState ? $"{location.Name} ({location.City}, {location.State})" : location.Name,
                    Value = location.Guid.ToString()
                } );
            }
        }

        #endregion

        #region Media Element Picker

        /// <summary>
        /// Gets the media accounts that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <returns>A collection of <see cref="ListItemBag" /> objects that represent media accounts.</returns>
        [HttpPost]
        [Route( "MediaElementPickerGetMediaAccounts" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "849e3ac3-f1e1-4efa-b0c8-1a79c4a666c7" )]
        public IActionResult MediaElementPickerGetMediaAccounts()
        {
            using ( var rockContext = new RockContext() )
            {
                return Ok( GetMediaAccounts( rockContext ) );
            }
        }

        /// <summary>
        /// Gets the media folders that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which media folders to load.</param>
        /// <returns>A collection of <see cref="ListItemBag"/> objects that represent media folders.</returns>
        [HttpPost]
        [Route( "MediaElementPickerGetMediaFolders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "a68493aa-8f41-404f-90dd-fbb2df0309a0" )]
        public IActionResult MediaElementPickerGetMediaFolders( [FromBody] MediaElementPickerGetMediaFoldersOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var mediaAccount = GetMediaAccountByGuid( options.MediaAccountGuid, rockContext );

                if ( mediaAccount == null )
                {
                    return NotFound();
                }

                return Ok( GetMediaFoldersForAccount( mediaAccount, rockContext ) );
            }
        }

        /// <summary>
        /// Gets the media elements that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which media elements to load.</param>
        /// <returns>A collection of <see cref="ListItemBag"/> objects that represent media elements.</returns>
        [HttpPost]
        [Route( "MediaElementPickerGetMediaElements" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "9b922b7e-95b4-4ecf-a6ec-f61b45f5e210" )]
        public IActionResult MediaElementPickerGetMediaElements( [FromBody] MediaElementPickerGetMediaElementsOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var mediaFolder = GetMediaFolderByGuid( options.MediaFolderGuid, rockContext );

                if ( mediaFolder == null )
                {
                    return NotFound();
                }

                return Ok( GetMediaElementsForFolder( mediaFolder, rockContext ) );
            }
        }

        /// <summary>
        /// Get all of the list items and the account/folder/element, depending on what the deepest given item is.
        /// </summary>
        /// <param name="options">The options that describe which media element picker data to load.</param>
        /// <returns>All of the picker lists, and individual picker selections that could be derived from the given options.</returns>
        [HttpPost]
        [Route( "MediaElementPickerGetMediaTree" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( MediaElementPickerGetMediaTreeResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "2cc15018-201e-4f22-b116-06846c70ad0b" )]
        public IActionResult MediaElementPickerGetMediaTree( [FromBody] MediaElementPickerGetMediaTreeOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var accounts = new List<ListItemBag>();
                var folders = new List<ListItemBag>();
                var elements = new List<ListItemBag>();

                MediaAccount mediaAccount = null;
                MediaFolder mediaFolder = null;
                MediaElement mediaElement = null;

                ListItemBag mediaAccountItem = null;
                ListItemBag mediaFolderItem = null;
                ListItemBag mediaElementItem = null;

                // If a media element is specified, get everything based on that
                if ( options.MediaElementGuid.HasValue )
                {
                    mediaElement = GetMediaElementByGuid( ( Guid ) options.MediaElementGuid, rockContext );
                    mediaFolder = mediaElement.MediaFolder;
                    mediaAccount = mediaFolder.MediaAccount;

                    mediaAccountItem = new ListItemBag { Text = mediaAccount.Name, Value = mediaAccount.Guid.ToString() };
                    mediaFolderItem = new ListItemBag { Text = mediaFolder.Name, Value = mediaFolder.Guid.ToString() };
                    mediaElementItem = new ListItemBag { Text = mediaElement.Name, Value = mediaElement.Guid.ToString() };

                    accounts = GetMediaAccounts( rockContext );
                    folders = GetMediaFoldersForAccount( mediaAccount, rockContext );
                    elements = GetMediaElementsForFolder( mediaFolder, rockContext );
                }
                // Otherwise, if a media folder is specified, get everything based on that, not getting a media element
                else if ( options.MediaFolderGuid.HasValue )
                {
                    mediaFolder = GetMediaFolderByGuid( ( Guid ) options.MediaFolderGuid, rockContext );
                    mediaAccount = mediaFolder.MediaAccount;

                    mediaAccountItem = new ListItemBag { Text = mediaAccount.Name, Value = mediaAccount.Guid.ToString() };
                    mediaFolderItem = new ListItemBag { Text = mediaFolder.Name, Value = mediaFolder.Guid.ToString() };

                    accounts = GetMediaAccounts( rockContext );
                    folders = GetMediaFoldersForAccount( mediaAccount, rockContext );
                    elements = GetMediaElementsForFolder( mediaFolder, rockContext );
                }
                // Otherwise, if a media account is specified, get the account and the lists of accounts and folders
                else if ( options.MediaAccountGuid.HasValue )
                {
                    mediaAccount = GetMediaAccountByGuid( ( Guid ) options.MediaAccountGuid, rockContext );

                    mediaAccountItem = new ListItemBag { Text = mediaAccount.Name, Value = mediaAccount.Guid.ToString() };

                    accounts = GetMediaAccounts( rockContext );
                    folders = GetMediaFoldersForAccount( mediaAccount, rockContext );
                }

                // Some things might be null, but we pass back everything we have
                return Ok( new MediaElementPickerGetMediaTreeResultsBag
                {
                    MediaAccount = mediaAccountItem,
                    MediaFolder = mediaFolderItem,
                    MediaElement = mediaElementItem,

                    MediaAccounts = accounts,
                    MediaFolders = folders,
                    MediaElements = elements
                } );
            }
        }

        /// <summary>
        /// Retrieve a MediaAccount object based on its Guid
        /// </summary>
        /// <param name="guid">The Media Account's Guid</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>The MediaAccount with that Guid</returns>
        private MediaAccount GetMediaAccountByGuid( Guid guid, RockContext rockContext )
        {
            // Get the media folder from the given GUID so we can filter elements by folder
            var mediaAccountService = new Rock.Model.MediaAccountService( rockContext );
            var mediaAccount = mediaAccountService.Queryable()
                .Where( a => a.Guid == guid )
                .First();

            return mediaAccount;
        }

        /// <summary>
        /// Retrieve a MediaFolder object based on its Guid
        /// </summary>
        /// <param name="guid">The Media Folder's Guid</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>The MediaFolder with that Guid</returns>
        private MediaFolder GetMediaFolderByGuid( Guid guid, RockContext rockContext )
        {
            // Get the media folder from the given GUID so we can filter elements by folder
            var mediaFolderService = new Rock.Model.MediaFolderService( rockContext );
            var mediaFolder = mediaFolderService.Queryable()
                .Where( a => a.Guid == guid )
                .First();

            return mediaFolder;
        }

        /// <summary>
        /// Retrieve a MediaElement object based on its Guid
        /// </summary>
        /// <param name="guid">The Media Element's Guid</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>The MediaElement with that Guid</returns>
        private MediaElement GetMediaElementByGuid( Guid guid, RockContext rockContext )
        {
            // Get the media folder from the given GUID so we can filter elements by folder
            var mediaElementService = new Rock.Model.MediaElementService( rockContext );
            var mediaElement = mediaElementService.Queryable()
                .Where( a => a.Guid == guid )
                .First();

            return mediaElement;
        }

        /// <summary>
        /// Get a list of all the Media Accounts
        /// </summary>
        /// <param name="rockContext">DB context</param>
        /// <returns>List of ListItemBags representing all of the Media Accounts</returns>
        private List<ListItemBag> GetMediaAccounts( RockContext rockContext )
        {
            var mediaAccountService = new Rock.Model.MediaAccountService( rockContext );

            // Get all media accounts that are active.
            var mediaAccounts = mediaAccountService.Queryable()
                .Where( ma => ma.IsActive )
                .OrderBy( ma => ma.Name )
                .Select( ma => new ListItemBag { Text = ma.Name, Value = ma.Guid.ToString() } )
                .ToList();

            return mediaAccounts;
        }

        /// <summary>
        /// Get a list of all the Media Folders for the given Media Account
        /// </summary>
        /// <param name="mediaAccount">MediaAccount object we want to get the child Media Folders of</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>List of ListItemBags representing all of the Media Folders for the given Media Account</returns>
        private List<ListItemBag> GetMediaFoldersForAccount( MediaAccount mediaAccount, RockContext rockContext )
        {
            // Get all media folders
            var mediaFolderService = new Rock.Model.MediaFolderService( rockContext );
            var mediaFolders = mediaFolderService.Queryable()
                .Where( mf => mf.MediaAccountId == mediaAccount.Id )
                .OrderBy( mf => mf.Name )
                .Select( mf => new ListItemBag
                {
                    Text = mf.Name,
                    Value = mf.Guid.ToString()
                } )
                .ToList();

            return mediaFolders;
        }

        /// <summary>
        /// Get a list of all the Media Elements for the given Media Account
        /// </summary>
        /// <param name="mediaFolder">The media folder.</param>
        /// <param name="rockContext">DB context</param>
        /// <returns>List of ListItemBags representing all of the Media Elements for the given Media Folder</returns>
        private List<ListItemBag> GetMediaElementsForFolder( MediaFolder mediaFolder, RockContext rockContext )
        {
            var mediaElementService = new Rock.Model.MediaElementService( rockContext );
            var mediaElements = mediaElementService.Queryable()
                .Where( me => me.MediaFolderId == mediaFolder.Id )
                .OrderBy( me => me.Name )
                .Select( me => new ListItemBag
                {
                    Text = me.Name,
                    Value = me.Guid.ToString()
                } )
                .ToList();

            return mediaElements;
        }

        #endregion

        #region Media Player

        /// <summary>
        /// Gets the media accounts that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <returns>An object that contains all the options that should be used when playing the media item.</returns>
        [HttpPost]
        [Route( "MediaPlayerGetPlayerOptions" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( MediaPlayerOptions ) )]
        [Rock.SystemGuid.RestActionGuid( "85EF9540-0B5E-4816-9A13-9B09BF1ECA4F" )]
        public IActionResult MediaPlayerGetPlayerOptions( [FromBody] MediaPlayerGetPlayerOptionsOptionsBag options )
        {
            if ( options.PlayerOptions == null || options.MediaElementGuid == null )
            {
                return BadRequest( "Player Options and MediaElementGuid are Required." );
            }

            var playerOptions = new MediaPlayerOptions
            {
                Autoplay = options.PlayerOptions.Autoplay,
                Autopause = options.PlayerOptions.Autopause,
                ClickToPlay = options.PlayerOptions.ClickToPlay,
                Controls = options.PlayerOptions.Controls,
                Debug = options.PlayerOptions.Debug,
                HideControls = options.PlayerOptions.HideControls,
                MediaUrl = options.PlayerOptions.MediaUrl,
                Muted = options.PlayerOptions.Muted,
                PosterUrl = options.PlayerOptions.PosterUrl,
                RelatedEntityId = options.PlayerOptions.RelatedEntityId,
                RelatedEntityTypeId = options.PlayerOptions.RelatedEntityTypeId,
                SeekTime = options.PlayerOptions.SeekTime,
                TrackProgress = options.PlayerOptions.TrackProgress,
                Type = options.PlayerOptions.Type,
                Volume = options.PlayerOptions.Volume,
                WriteInteraction = options.PlayerOptions.WriteInteraction
            };

            playerOptions.UpdateValuesFromMedia( null, options.MediaElementGuid, options.AutoResumeInDays, options.CombinePlayStatisticsInDays, RockRequestContext.CurrentPerson, RockRequestContext.CurrentVisitorId );

            return Ok( playerOptions );
        }

        #endregion

        #region Merge Field Picker

        /// <summary>
        /// Gets the merge fields and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which merge fields to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of merge fields.</returns>
        [HttpPost]
        [Route( "MergeFieldPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "f6722f7a-64ed-401a-9dea-c64fa9738b75" )]
        public IActionResult MergeFieldPickerGetChildren( [FromBody] MergeFieldPickerGetChildrenOptionsBag options )
        {
            var children = MergeFieldPickerGetChildren( options.Id, options.AdditionalFields, RockRequestContext.CurrentPerson );

            var treeItemChildren = children?.Select( convertTreeViewItemToTreeItemBag ).ToList();

            return Ok( treeItemChildren );
        }

        /// <summary>
        /// Gets the merge fields that match the given search terms.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the merge fields that match the search.</returns>
        [HttpPost]
        [Route( "MergeFieldPickerGetSearchedMergeFields" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "f7dd9588-9eff-4f08-ae0e-674de8dcb592" )]
        public IActionResult MergePickerGetSearchedMergeFields( [FromBody] MergeFieldPickerGetSearchedMergedFieldsOptionsBag options )
        {
            if ( options.SearchTerm.IsNullOrWhiteSpace() )
            {
                return BadRequest( "Search Term is required" );
            }

            var searchedFields = GetMergeFields( options.AdditionalFields, GetPerson() )
                .Where( mf => mf.Text.Contains( options.SearchTerm ) )
                .ToList();

            return Ok( searchedFields );
        }

        /// <summary>
        /// Formats a selected Merge Field value as Lava.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// ***NOTE***: Also implemented in Rock.Web.UI.Controls.MergeFieldPicker's FormatSelectedValue method.
        /// Any changes here should also be made there
        /// </remarks>
        /// <param name="options">The options that contain the selected value</param>
        /// <returns>A string of Lava</returns>
        [HttpPost]
        [Route( "MergeFieldPickerFormatSelectedValue" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "ffe018c4-c088-4057-b28b-4980541f16d5" )]
        public IActionResult MergeFieldPickerFormatSelectedValue( [FromBody] MergeFieldPickerFormatSelectedValueOptionsBag options )
        {
            if ( options.SelectedValue == null )
            {
                return BadRequest();
            }

            var idParts = options.SelectedValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            if ( idParts.Count > 0 )
            {
                if ( idParts.Count == 2 && idParts[0] == "GlobalAttribute" )
                {
                    return Ok( string.Format( "{{{{ 'Global' | Attribute:'{0}' }}}}", idParts[1] ) );
                }

                if ( idParts.Count == 1 && idParts[0].StartsWith( "AdditionalMergeField" ) )
                {
                    string mFields = idParts[0].Replace( "AdditionalMergeField_", "" ).Replace( "AdditionalMergeFields_", "" );
                    if ( mFields.IsNotNullOrWhiteSpace() )
                    {
                        string beginFor = "{% for field in AdditionalFields %}";
                        string endFor = "{% endfor %}";
                        var mergeFields = String.Join( "", mFields.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries )
                            .Select( f => "{{ field." + f + "}}" ) );

                        return Ok( $"{beginFor}{mergeFields}{endFor}" );
                    }
                }

                if ( idParts.Count == 1 )
                {
                    if ( idParts[0] == "Campuses" )
                    {
                        return Ok( @"
{% for campus in Campuses %}
<p>
    Name: {{ campus.Name }}<br/>
    Description: {{ campus.Description }}<br/>
    Is Active: {{ campus.IsActive }}<br/>
    Short Code: {{ campus.ShortCode }}<br/>
    Url: {{ campus.Url }}<br/>
    Phone Number: {{ campus.PhoneNumber }}<br/>
    Service Times:
    {% for serviceTime in campus.ServiceTimes %}
        {{ serviceTime.Day }} {{ serviceTime.Time }},
    {% endfor %}
    <br/>
{% endfor %}
" );
                    }

                    if ( idParts[0] == "Date" )
                    {
                        return Ok( "{{ 'Now' | Date:'MM/dd/yyyy' }}" );
                    }

                    if ( idParts[0] == "Time" )
                    {
                        return Ok( "{{ 'Now' | Date:'hh:mm:ss tt' }}" );
                    }

                    if ( idParts[0] == "DayOfWeek" )
                    {
                        return Ok( "{{ 'Now' | Date:'dddd' }}" );
                    }

                    if ( idParts[0] == "PageParameter" )
                    {
                        return Ok( "{{ PageParameter.[Enter Page Parameter Name Here] }}" );
                    }
                }

                var workingParts = new List<string>();

                // Get the root type
                int pathPointer = 0;
                EntityTypeCache entityType = null;
                while ( entityType == null && pathPointer < idParts.Count() )
                {
                    string item = idParts[pathPointer];
                    string[] itemParts = item.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );

                    string itemName = itemParts.Length > 1 ? itemParts[0] : string.Empty;
                    string mergeFieldId = itemParts.Length > 1 ? itemParts[1] : item;

                    var entityTypeInfo = MergeFieldPicker.GetEntityTypeInfoFromMergeFieldId( mergeFieldId );
                    entityType = entityTypeInfo?.EntityType;

                    workingParts.Add( entityType != null ?
                        ( itemName != string.Empty ? itemName : entityType.FriendlyName.Replace( " ", string.Empty ) ) :
                        idParts[pathPointer] );
                    pathPointer++;
                }

                if ( entityType != null )
                {
                    Type type = entityType.GetEntityType();

                    var formatString = "{0}";

                    // Traverse the Property path
                    bool itemIsCollection = false;
                    bool lastItemIsProperty = true;

                    while ( idParts.Count > pathPointer )
                    {
                        string propertyName = idParts[pathPointer];
                        workingParts.Add( propertyName );

                        var childProperty = type.GetProperty( propertyName );
                        if ( childProperty != null )
                        {
                            lastItemIsProperty = true;
                            type = childProperty.PropertyType;

                            if ( type.IsGenericType &&
                                type.GetGenericTypeDefinition() == typeof( ICollection<> ) &&
                                type.GetGenericArguments().Length == 1 )
                            {
                                string propertyNameSingularized = propertyName.Singularize();
                                string forString = string.Format( "<% for {0} in {1} %> {{0}} <% endfor %>", propertyNameSingularized, workingParts.AsDelimited( "." ) );
                                workingParts.Clear();
                                workingParts.Add( propertyNameSingularized );
                                formatString = string.Format( formatString, forString );

                                type = type.GetGenericArguments()[0];

                                itemIsCollection = true;
                            }
                            else
                            {
                                itemIsCollection = false;
                            }
                        }
                        else
                        {
                            lastItemIsProperty = false;
                        }

                        pathPointer++;
                    }

                    string itemString = string.Empty;
                    if ( !itemIsCollection )
                    {
                        if ( lastItemIsProperty )
                        {
                            itemString = string.Format( "<< {0} >>", workingParts.AsDelimited( "." ) );
                        }
                        else
                        {
                            string partPath = workingParts.Take( workingParts.Count - 1 ).ToList().AsDelimited( "." );
                            var partItem = workingParts.Last();
                            if ( type == typeof( Rock.Model.Person ) && partItem == "Campus" )
                            {
                                itemString = string.Format( "{{{{ {0} | Campus | Property:'Name' }}}}", partPath );
                            }
                            else
                            {
                                itemString = string.Format( "{{{{ {0} | Attribute:'{1}' }}}}", partPath, partItem );
                            }
                        }
                    }

                    return Ok( string.Format( formatString, itemString ).Replace( "<", "{" ).Replace( ">", "}" ) );
                }

                return Ok( string.Format( "{{{{ {0} }}}}", idParts.AsDelimited( "." ) ) );
            }

            return Ok( string.Empty );
        }

        /// <summary>
        /// Gets the child merge fields available to the given user.
        /// NOTE: This is used by the legacy MergeFieldsController and was copied from there
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="additionalFields">The additional fields.</param>
        /// <param name="person">The current user</param>
        /// <returns></returns>
        internal static IQueryable<TreeViewItem> MergeFieldPickerGetChildren( string id, string additionalFields, Person person )
        {
            List<TreeViewItem> items = new List<TreeViewItem>();

            switch ( id )
            {
                case "0":
                    {
                        if ( !string.IsNullOrWhiteSpace( additionalFields ) )
                        {
                            foreach ( string fieldInfo in additionalFields.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                            {
                                string[] parts = fieldInfo.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );

                                string fieldId = parts.Length > 0 ? parts[0] : string.Empty;

                                if ( fieldId == "AdditionalMergeFields" )
                                {
                                    if ( parts.Length > 1 )
                                    {
                                        var fieldsTv = new TreeViewItem
                                        {
                                            Id = $"AdditionalMergeFields_{parts[1]}",
                                            Name = "Additional Fields",
                                            HasChildren = true,
                                            Children = new List<TreeViewItem>()
                                        };

                                        foreach ( string fieldName in parts[1].Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries ) )
                                        {
                                            fieldsTv.Children.Add( new TreeViewItem
                                            {
                                                Id = $"AdditionalMergeField_{fieldName}",
                                                Name = fieldName.SplitCase(),
                                                HasChildren = false
                                            } );
                                        }
                                        items.Add( fieldsTv );
                                    }
                                }
                                else
                                {
                                    string[] idParts = fieldId.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );

                                    string mergeFieldId = idParts.Length > 1 ? idParts[1] : fieldId;

                                    var entityTypeInfo = MergeFieldPicker.GetEntityTypeInfoFromMergeFieldId( mergeFieldId );
                                    if ( entityTypeInfo?.EntityType != null )
                                    {
                                        items.Add( new TreeViewItem
                                        {
                                            Id = fieldId.UrlEncode(),
                                            Name = parts.Length > 1 ? parts[1] : entityTypeInfo.EntityType.FriendlyName,
                                            HasChildren = true
                                        } );
                                    }
                                    else
                                    {
                                        items.Add( new TreeViewItem
                                        {
                                            Id = fieldId,
                                            Name = parts.Length > 1 ? parts[1] : mergeFieldId.SplitCase(),
                                            HasChildren = mergeFieldId == "GlobalAttribute"
                                        } );
                                    }
                                }
                            }
                        }

                        break;
                    }

                case "GlobalAttribute":
                    {
                        var globalAttributes = GlobalAttributesCache.Get();

                        foreach ( var attributeCache in globalAttributes.Attributes.OrderBy( a => a.Key ) )
                        {
                            if ( attributeCache.IsAuthorized( Security.Authorization.VIEW, person ) )
                            {
                                items.Add( new TreeViewItem
                                {
                                    Id = "GlobalAttribute|" + attributeCache.Key,
                                    Name = attributeCache.Name,
                                    HasChildren = false
                                } );
                            }
                        }

                        break;
                    }

                default:
                    {
                        // In this scenario, the id should be a concatenation of a root qualified entity name and then the property path
                        var idParts = id.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                        if ( idParts.Count > 0 )
                        {
                            // Get the root type
                            int pathPointer = 0;
                            EntityTypeCache entityType = null;
                            MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier[] entityTypeQualifiers = null;
                            while ( entityType == null && pathPointer < idParts.Count() )
                            {
                                string item = idParts[pathPointer];
                                string[] itemParts = item.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                                string entityTypeMergeFieldId = itemParts.Length > 1 ? itemParts[1] : item;
                                MergeFieldPicker.EntityTypeInfo entityTypeInfo = MergeFieldPicker.GetEntityTypeInfoFromMergeFieldId( entityTypeMergeFieldId );
                                entityType = entityTypeInfo?.EntityType;
                                entityTypeQualifiers = entityTypeInfo?.EntityTypeQualifiers;
                                pathPointer++;
                            }

                            if ( entityType != null )
                            {
                                Type type = entityType.GetEntityType();

                                // Traverse the Property path
                                while ( idParts.Count > pathPointer )
                                {
                                    var childProperty = type.GetProperty( idParts[pathPointer] );
                                    if ( childProperty != null )
                                    {
                                        type = childProperty.PropertyType;

                                        if ( type.IsGenericType &&
                                            type.GetGenericTypeDefinition() == typeof( ICollection<> ) &&
                                            type.GetGenericArguments().Length == 1 )
                                        {
                                            type = type.GetGenericArguments()[0];
                                        }
                                    }

                                    pathPointer++;
                                }

                                entityType = EntityTypeCache.Get( type );

                                // Add the tree view items
                                foreach ( var propInfo in Rock.Lava.LavaHelper.GetLavaProperties( type ) )
                                {
                                    var treeViewItem = new TreeViewItem
                                    {
                                        Id = id + "|" + propInfo.Name,
                                        Name = propInfo.Name.SplitCase()
                                    };

                                    Type propertyType = propInfo.PropertyType;

                                    if ( propertyType.IsGenericType &&
                                        propertyType.GetGenericTypeDefinition() == typeof( ICollection<> ) &&
                                        propertyType.GetGenericArguments().Length == 1 )
                                    {
                                        treeViewItem.Name += " (Collection)";
                                        propertyType = propertyType.GetGenericArguments()[0];
                                    }

                                    bool hasChildren = false;
                                    if ( EntityTypeCache.Get( propertyType.FullName, false ) != null )
                                    {
                                        hasChildren = Rock.Lava.LavaHelper.GetLavaProperties( propertyType ).Any();
                                    }

                                    treeViewItem.HasChildren = hasChildren;

                                    items.Add( treeViewItem );
                                }

                                if ( type == typeof( Rock.Model.Person ) )
                                {
                                    items.Add( new TreeViewItem
                                    {
                                        Id = id + "|" + "Campus",
                                        Name = "Campus"
                                    } );
                                }

                                if ( entityType.IsEntity )
                                {
                                    var attributeList = new AttributeService( new Rock.Data.RockContext() ).GetByEntityTypeId( entityType.Id, false ).ToAttributeCacheList();
                                    if ( entityTypeQualifiers?.Any() == true )
                                    {
                                        var qualifiedAttributeList = new List<AttributeCache>();
                                        foreach ( var entityTypeQualifier in entityTypeQualifiers )
                                        {
                                            var qualifierAttributes = attributeList.Where( a =>
                                                 a.EntityTypeQualifierColumn.Equals( entityTypeQualifier.Column, StringComparison.OrdinalIgnoreCase )
                                                 && a.EntityTypeQualifierValue.Equals( entityTypeQualifier.Value, StringComparison.OrdinalIgnoreCase ) ).ToList();

                                            qualifiedAttributeList.AddRange( qualifierAttributes );
                                        }

                                        attributeList = qualifiedAttributeList;
                                    }
                                    else
                                    {
                                        // Only include attributes without a qualifier since we weren't specified a qualifiercolumn/value
                                        attributeList = attributeList.Where( a => a.EntityTypeQualifierColumn.IsNullOrWhiteSpace() && a.EntityTypeQualifierValue.IsNullOrWhiteSpace() ).ToList();
                                    }

                                    foreach ( var attribute in attributeList )
                                    {
                                        if ( attribute.IsAuthorized( Security.Authorization.VIEW, person ) )
                                        {
                                            items.Add( new TreeViewItem
                                            {
                                                Id = id + "|" + attribute.Key,
                                                Name = attribute.Name
                                            } );
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            return items.OrderBy( i => i.Name ).AsQueryable();
        }

        internal static IQueryable<ListItemBag> GetMergeFields( string additionalFields, Person person )
        {
            var rootItems = new List<ListItemBag>();
            var items = new List<ListItemBag>();

            if ( !string.IsNullOrWhiteSpace( additionalFields ) )
            {
                foreach ( string fieldInfo in additionalFields.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                {
                    string[] parts = fieldInfo.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );

                    string fieldId = parts.Length > 0 ? parts[0] : string.Empty;

                    if ( fieldId == "AdditionalMergeFields" )
                    {
                        if ( parts.Length > 1 )
                        {
                            var fieldsTv = new ListItemBag
                            {
                                Value = $"AdditionalMergeFields_{parts[1]}",
                                Text = "Additional Fields"
                            };

                            foreach ( string fieldName in parts[1].Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries ) )
                            {
                                items.Add( new ListItemBag
                                {
                                    Value = $"AdditionalMergeField_{fieldName}",
                                    Text = fieldName.SplitCase()
                                } );
                            }

                            rootItems.Add( fieldsTv );
                        }
                    }
                    else
                    {
                        string[] idParts = fieldId.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );

                        string mergeFieldId = idParts.Length > 1 ? idParts[1] : fieldId;

                        var entityTypeInfo = MergeFieldPicker.GetEntityTypeInfoFromMergeFieldId( mergeFieldId );
                        if ( entityTypeInfo?.EntityType != null )
                        {
                            rootItems.Add( new ListItemBag
                            {
                                Value = fieldId.UrlEncode(),
                                Text = parts.Length > 1 ? parts[1] : entityTypeInfo.EntityType.FriendlyName
                            } );
                        }
                        else
                        {
                            rootItems.Add( new ListItemBag
                            {
                                Value = fieldId,
                                Text = parts.Length > 1 ? parts[1] : mergeFieldId.SplitCase()
                            } );
                        }
                    }
                }
            }

            foreach ( var id in rootItems.ConvertAll( i => i.Value ) )
            {
                var category = rootItems.FirstOrDefault( i => i.Value == id )?.Text;

                if ( id == "GlobalAttribute" )
                {
                    var globalAttributes = GlobalAttributesCache.Get();

                    foreach ( var attributeCache in globalAttributes.Attributes.Where( a => a.IsAuthorized( Security.Authorization.VIEW, person ) ).OrderBy( a => a.Key ) )
                    {
                        items.Add( new ListItemBag
                        {
                            Value = "GlobalAttribute|" + attributeCache.Key,
                            Text = attributeCache.Name,
                            Category = category
                        } );
                    }
                }
                else
                {
                    // In this scenario, the id should be a concatenation of a root qualified entity name and then the property path
                    var idParts = id.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                    if ( idParts.Count > 0 )
                    {
                        // Get the root type
                        int pathPointer = 0;
                        EntityTypeCache entityType = null;
                        MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier[] entityTypeQualifiers = null;
                        while ( entityType == null && pathPointer < idParts.Count() )
                        {
                            string item = idParts[pathPointer];
                            string[] itemParts = item.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                            string entityTypeMergeFieldId = itemParts.Length > 1 ? itemParts[1] : item;
                            MergeFieldPicker.EntityTypeInfo entityTypeInfo = MergeFieldPicker.GetEntityTypeInfoFromMergeFieldId( entityTypeMergeFieldId );
                            entityType = entityTypeInfo?.EntityType;
                            entityTypeQualifiers = entityTypeInfo?.EntityTypeQualifiers;
                            pathPointer++;
                        }

                        if ( entityType != null )
                        {
                            Type type = entityType.GetEntityType();

                            // Traverse the Property path
                            while ( idParts.Count > pathPointer )
                            {
                                var childProperty = type.GetProperty( idParts[pathPointer] );
                                if ( childProperty != null )
                                {
                                    type = childProperty.PropertyType;

                                    if ( type.IsGenericType &&
                                        type.GetGenericTypeDefinition() == typeof( ICollection<> ) &&
                                        type.GetGenericArguments().Length == 1 )
                                    {
                                        type = type.GetGenericArguments()[0];
                                    }
                                }

                                pathPointer++;
                            }

                            entityType = EntityTypeCache.Get( type );

                            // Add the tree view items
                            foreach ( var propInfo in Rock.Lava.LavaHelper.GetLavaProperties( type ) )
                            {
                                GetPropertyMergeFields( items, id, category, propInfo, new List<Type>() );
                            }

                            if ( type == typeof( Rock.Model.Person ) )
                            {
                                items.Add( new ListItemBag
                                {
                                    Value = $"{id}|Campus",
                                    Text = "Campus",
                                    Category = category
                                } );
                            }

                            if ( entityType.IsEntity )
                            {
                                var attributeList = new AttributeService( new Rock.Data.RockContext() ).GetByEntityTypeId( entityType.Id, false ).ToAttributeCacheList();
                                if ( entityTypeQualifiers?.Any() == true )
                                {
                                    var qualifiedAttributeList = new List<AttributeCache>();
                                    foreach ( var entityTypeQualifier in entityTypeQualifiers )
                                    {
                                        var qualifierAttributes = attributeList.Where( a =>
                                             a.EntityTypeQualifierColumn.Equals( entityTypeQualifier.Column, StringComparison.OrdinalIgnoreCase )
                                             && a.EntityTypeQualifierValue.Equals( entityTypeQualifier.Value, StringComparison.OrdinalIgnoreCase ) ).ToList();

                                        qualifiedAttributeList.AddRange( qualifierAttributes );
                                    }

                                    attributeList = qualifiedAttributeList;
                                }
                                else
                                {
                                    // Only include attributes without a qualifier since we weren't specified a qualifiercolumn/value
                                    attributeList = attributeList.Where( a => a.EntityTypeQualifierColumn.IsNullOrWhiteSpace() && a.EntityTypeQualifierValue.IsNullOrWhiteSpace() ).ToList();
                                }

                                foreach ( var attribute in attributeList )
                                {
                                    if ( attribute.IsAuthorized( Security.Authorization.VIEW, person ) )
                                    {
                                        items.Add( new ListItemBag
                                        {
                                            Value = $"{id}|{attribute.Key}",
                                            Text = attribute.Name,
                                            Category = category
                                        } );
                                    }
                                }
                            }
                        }
                    }
                }
            }

            items.AddRange( rootItems );

            return items.OrderBy( i => i.Text ).AsQueryable();
        }

        private static void GetPropertyMergeFields( List<ListItemBag> items, string parentId, string category, PropertyInfo propInfo, List<Type> parentTypes )
        {
            var id = parentId + "|" + propInfo.Name;
            if ( !parentTypes.Contains( propInfo.DeclaringType ) )
            {
                parentTypes.Add( propInfo.DeclaringType );
            }

            var mergeFieldItem = new ListItemBag
            {
                Value = id,
                Text = propInfo.Name.SplitCase(),
                Category = category
            };

            Type propertyType = propInfo.PropertyType;

            if ( propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof( ICollection<> ) &&
                propertyType.GetGenericArguments().Length == 1 )
            {
                mergeFieldItem.Text += " (Collection)";
                mergeFieldItem.Category = $"{category} > {mergeFieldItem.Text}";
                propertyType = propertyType.GetGenericArguments()[0];
            }

            List<PropertyInfo> childProperties = new List<PropertyInfo>();

            if ( EntityTypeCache.Get( propertyType.FullName, false ) != null )
            {
                childProperties = Rock.Lava.LavaHelper.GetLavaProperties( propertyType ).Where( p => !parentTypes.Contains( p.PropertyType ) ).ToList();
            }

            if ( childProperties.Count > 0 && !parentTypes.Contains( propertyType ) )
            {
                foreach ( var item in childProperties )
                {
                    GetPropertyMergeFields( items, id, $"{category} > {mergeFieldItem.Text}", item, parentTypes );
                }
            }

            items.Add( mergeFieldItem );
        }

        #endregion

        #region Merge Template Picker

        /// <summary>
        /// Gets the merge templates and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which merge templates to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of merge templates.</returns>
        [HttpPost]
        [Route( "MergeTemplatePickerGetMergeTemplates" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "2e486da8-927f-4474-8ba8-00a68d261403" )]
        public IActionResult MergeTemplatePickerGetMergeTemplates( [FromBody] MergeTemplatePickerGetMergeTemplatesOptionsBag options )
        {
            List<Guid> include = null;
            List<Guid> exclude = null;

            if ( options.MergeTemplateOwnership == Rock.Enums.Controls.MergeTemplateOwnership.Global )
            {
                exclude = new List<Guid>();
                exclude.Add( Rock.SystemGuid.Category.PERSONAL_MERGE_TEMPLATE.AsGuid() );
            }
            else if ( options.MergeTemplateOwnership == Rock.Enums.Controls.MergeTemplateOwnership.Personal )
            {
                include = new List<Guid>();
                include.Add( Rock.SystemGuid.Category.PERSONAL_MERGE_TEMPLATE.AsGuid() );
            }

            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var queryOptions = new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.ParentGuid.HasValue,
                    EntityTypeGuid = EntityTypeCache.Get<MergeTemplate>().Guid,
                    IncludeUnnamedEntityItems = false,
                    IncludeCategoriesWithoutChildren = false,
                    IncludeCategoryGuids = include,
                    ExcludeCategoryGuids = exclude,
                    DefaultIconCssClass = options.DefaultIconCssClass,
                    ItemFilterPropertyName = null,
                    ItemFilterPropertyValue = "",
                    LazyLoad = true,
                    SecurityGrant = grant
                };

                var items = clientService.GetCategorizedTreeItems( queryOptions );

                return Ok( items );
            }
        }

        #endregion

        #region Metric Category Picker

        /// <summary>
        /// Gets the metric categories and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which metric categories to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of metric categories.</returns>
        [HttpPost]
        [Route( "MetricCategoryPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "92a11376-6bcd-4299-a54d-946cbde7566b" )]
        public IActionResult MetricCategoryPickerGetChildren( [FromBody] MetricCategoryPickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var queryOptions = new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.ParentGuid.HasValue,
                    EntityTypeGuid = EntityTypeCache.Get<MetricCategory>().Guid,
                    IncludeUnnamedEntityItems = true,
                    IncludeCategoriesWithoutChildren = false,
                    DefaultIconCssClass = options.DefaultIconCssClass,
                    ItemFilterPropertyName = null,
                    ItemFilterPropertyValue = "",
                    LazyLoad = true,
                    SecurityGrant = grant
                };

                var items = clientService.GetCategorizedTreeItems( queryOptions );

                return Ok( items );
            }
        }

        #endregion

        #region Metric Item Picker

        /// <summary>
        /// Gets the metric items and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which metric items to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of metric items.</returns>
        [HttpPost]
        [Route( "MetricItemPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "c8e8f26e-a7cd-445a-8d72-6d4484a8ee59" )]
        public IActionResult MetricItemPickerGetChildren( [FromBody] MetricItemPickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var items = GetMetricItemPickerChildren( options, rockContext );

                if ( items == null || items.Count == 0 )
                {
                    return NotFound();
                }

                return Ok( items );
            }
        }

        /// <summary>
        /// Gets the metric items and their categories that match the options given.
        /// </summary>
        /// <param name="options">The options that describe which metric items to load.</param>
        /// <param name="rockContext">Context for performing DB queries.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of metric items.</returns>
        private List<TreeItemBag> GetMetricItemPickerChildren( [FromBody] MetricItemPickerGetChildrenOptionsBag options, RockContext rockContext )
        {
            var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
            var queryOptions = new CategoryItemTreeOptions
            {
                ParentGuid = options.ParentGuid,
                GetCategorizedItems = options.ParentGuid.HasValue,
                EntityTypeGuid = EntityTypeCache.Get<MetricCategory>().Guid,
                IncludeUnnamedEntityItems = true,
                IncludeCategoriesWithoutChildren = false,
                DefaultIconCssClass = options.DefaultIconCssClass,
                LazyLoad = true,
                SecurityGrant = grant,
                IncludeCategoryGuids = options.IncludeCategoryGuids
            };

            var metricCategories = clientService.GetCategorizedTreeItems( queryOptions );
            var metricCategoryService = new MetricCategoryService( new RockContext() );
            var convertedMetrics = new List<TreeItemBag>();

            // Translate from MetricCategory to Metric.
            foreach ( var categoryItem in metricCategories )
            {
                if ( !categoryItem.IsFolder )
                {
                    // Load the MetricCategory.
                    var metricCategory = metricCategoryService.Get( categoryItem.Value.AsGuid() );
                    if ( metricCategory != null )
                    {
                        // Swap the Id to the Metric Guid (instead of MetricCategory.Guid).
                        categoryItem.Value = metricCategory.Metric.Guid.ToString();
                    }
                }

                if ( categoryItem.HasChildren )
                {
                    categoryItem.Children = new List<TreeItemBag>();
                    categoryItem.Children.AddRange( GetMetricItemPickerChildren( new MetricItemPickerGetChildrenOptionsBag
                    {
                        ParentGuid = categoryItem.Value.AsGuid(),
                        DefaultIconCssClass = options.DefaultIconCssClass,
                        SecurityGrantToken = options.SecurityGrantToken,
                        IncludeCategoryGuids = options.IncludeCategoryGuids
                    }, rockContext ) );
                }

                convertedMetrics.Add( categoryItem );
            }

            return convertedMetrics;
        }

        #endregion

        #region Note Editor

        /// <summary>
        /// Searches for possible mention candidates to display that match the request.
        /// </summary>
        /// <param name="options">The options that describe the mention sources to search for.</param>
        /// <returns>An instance of <see cref="NoteEditorMentionSearchResultsBag"/> that contains the possible matches.</returns>
        [HttpPost]
        [Route( "NoteEditorMentionSearch" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( NoteEditorMentionSearchResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "dca338b6-9749-427e-8238-1686c9587d16" )]
        public IActionResult NoteEditorMentionSearch( [FromBody] NoteEditorMentionSearchOptionsBag options )
        {
            var isFullSearchAllowed = IsCurrentPersonAuthorized( Security.Authorization.EXECUTE_UNRESTRICTED_READ );

            using ( var rockContext = new RockContext() )
            {
                var searchComponent = Rock.Search.SearchContainer.GetComponent( typeof( Rock.Search.Person.Name ).FullName );
                var allowFirstNameOnly = searchComponent?.GetAttributeValue( "FirstNameSearch" ).AsBoolean() ?? false;
                var personService = new PersonService( rockContext );

                var personSearchOptions = new PersonService.PersonSearchOptions
                {
                    Name = options.Name,
                    AllowFirstNameOnly = allowFirstNameOnly,
                    IncludeBusinesses = false,
                    IncludeDeceased = false
                };

                // Prepare the basic person search filter that wil be used
                // for both the "full database" search as well as the priority
                // list search.
                var basicPersonSearchQry = personService.Search( personSearchOptions ).AsNoTracking();

                // Get the query to run for a full-database search. The where
                // clause will make it so we get no results unless full search
                // is allowed.
                var searchQry = basicPersonSearchQry
                    .Where( p => isFullSearchAllowed || p.Id == 0 )
                    .Select( p => new
                    {
                        Person = p,
                        Priority = false
                    } );

                // This is intentionally commented out since we don't support
                // this just yet. But it is here to see the pattern of how to
                // provide priority search results based on values in the token.
                //if ( DecryptedToken.GroupId.HasValue )
                //{
                //    var groupPersonIdQry = new GroupMemberService( rockContext ).Queryable()
                //        .Where( gm => gm.GroupId == DecryptedToken.GroupId.Value )
                //        .Select( gm => gm.PersonId );

                //    var prioritySearchQry = basicPersonSearchQry
                //        .Where( p => groupPersonIdQry.Contains( p.Id ) )
                //        .Select( p => new
                //        {
                //            Person = p,
                //            Priority = true
                //        } );

                //    searchQry = searchQry.Union( prioritySearchQry );
                //}

                // We want the priority people first and then after that sort by
                // view count in descending order.
                //
                // Then take 50 total items, put it in C# memory and then get the
                // distinct ones and finally limit to our final 25 people. This
                // is done because if we do a Distinct() in SQL it will lose the
                // sorting, but we can't do the sorting after a SQL .Distinct().
                var people = searchQry
                    .OrderByDescending( p => p.Priority )
                    .ThenByDescending( p => p.Person.ViewedCount )
                    .ThenBy( p => p.Person.Id )
                    .Select( p => p.Person )
                    .Take( 50 )
                    .ToList()
                    .DistinctBy( p => p.Id )
                    .Take( 25 )
                    .ToList();

                var hasMultipleCampuses = CampusCache.All().Count( c => c.IsActive == true ) > 1;

                // Convert the list of people into a collection of mention items.
                var items = people
                    .Select( p => new NoteMentionItemBag
                    {
                        CampusName = p.PrimaryCampusId.HasValue && hasMultipleCampuses
                            ? CampusCache.Get( p.PrimaryCampusId.Value )?.CondensedName
                            : string.Empty,
                        DisplayName = p.FullName,
                        Email = p.Email,
                        Identifier = IdHasher.Instance.GetHash( p.PrimaryAliasId ?? 0 ),
                        ImageUrl = p.PhotoUrl
                    } )
                    .ToList();

                return Ok( new NoteEditorMentionSearchResultsBag
                {
                    Items = items
                } );
            }
        }

        #endregion

        #region Page Picker

        /// <summary>
        /// Gets the tree list of pages
        /// </summary>
        /// <param name="options">The options that describe which pages to retrieve.</param>
        /// <returns>A collection of <see cref="TreeItemBag"/> objects that represent the pages.</returns>
        [HttpPost]
        [Route( "PagePickerGetChildren" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "EE9AB2EA-EE01-4D0F-B626-02D1C8D1ABF4" )]
        public IActionResult PagePickerGetChildren( [FromBody] PagePickerGetChildrenOptionsBag options )
        {
            var service = new Service<Page>( new RockContext() ).Queryable().AsNoTracking();
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
            IQueryable<Page> qry;

            if ( options.Guid.IsEmpty() )
            {
                qry = service.Where( a => a.ParentPage.Guid == options.RootPageGuid );
            }
            else
            {
                qry = service.Where( a => a.ParentPage.Guid == options.Guid );
            }

            if ( options.SiteType != null )
            {
                qry = qry.Where( p => ( int ) p.Layout.Site.SiteType == options.SiteType.Value );
            }

            var hidePageGuids = options.HidePageGuids ?? new List<Guid>();

            List<Page> pageList = qry
                .Where( p => !hidePageGuids.Contains( p.Guid ) )
                .OrderBy( p => p.Order )
                .ThenBy( p => p.InternalName )
                .ToList()
                .Where( p => p.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( p, Security.Authorization.VIEW ) == true )
                .ToList();
            List<TreeItemBag> pageItemList = new List<TreeItemBag>();
            Func<Page, string> getTreeItemValue = new Func<Page, string>( p => p.Guid.ToString() );
            foreach ( var page in pageList )
            {
                var pageItem = new TreeItemBag();
                pageItem.Value = getTreeItemValue( page );
                pageItem.Text = page.InternalName;

                pageItemList.Add( pageItem );
            }

            // try to quickly figure out which items have Children
            List<int> resultIds = pageList.Select( a => a.Id ).ToList();

            var qryHasChildren = service
                .Where( p =>
                    p.ParentPageId.HasValue &&
                    resultIds.Contains( p.ParentPageId.Value ) );

            var pageIdentifiersWithChildren = qryHasChildren.Select( p => p.ParentPage.Guid.ToString() ).Distinct().ToList();

            foreach ( var g in pageItemList )
            {
                var hasChildren = pageIdentifiersWithChildren.Any( a => a == g.Value );
                g.HasChildren = hasChildren;
                g.IsFolder = hasChildren;
                g.IconCssClass = "fa fa-file-o";
            }

            return Ok( pageItemList.AsQueryable() );
        }

        /// <summary>
        /// Gets the list of pages in the hierarchy going from the root to the given page
        /// </summary>
        /// <param name="options">The options that describe which pages to retrieve.</param>
        /// <returns>A collection of unique identifiers that represent the pages.</returns>
        [HttpPost]
        [Route( "PagePickerGetSelectedPageHierarchy" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<string> ) )]
        [Rock.SystemGuid.RestActionGuid( "e74611a0-1711-4a0b-b3bd-df242d344679" )]
        public IActionResult PagePickerGetSelectedPageHierarchy( [FromBody] PagePickerGetSelectedPageHierarchyOptionsBag options )
        {
            var parentPageGuids = new List<string>();
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            foreach ( Guid pageGuid in options.SelectedPageGuids )
            {
                var page = PageCache.Get( pageGuid );

                if ( page == null )
                {
                    continue;
                }

                var parentPage = page.ParentPage;

                while ( parentPage != null )
                {
                    if ( !parentPageGuids.Contains( parentPage.Guid.ToString() ) && ( parentPage.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || ( grant?.IsAccessGranted( parentPage, Security.Authorization.VIEW ) == true ) ) )
                    {
                        parentPageGuids.Insert( 0, parentPage.Guid.ToString() );
                    }
                    else
                    {
                        // infinite recursion
                        break;
                    }

                    parentPage = parentPage.ParentPage;
                }
            }

            return Ok( parentPageGuids );
        }

        /// <summary>
        /// Gets the internal name of the page with the given Guid
        /// </summary>
        /// <param name="options">The options that contains the Guid of the page</param>
        /// <returns>A string internal name of the page with the given Guid.</returns>
        [HttpPost]
        [Route( "PagePickerGetPageName" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "20d219bd-3635-4cbc-b79f-250972ae6b97" )]
        public IActionResult PagePickerGetPageName( [FromBody] PagePickerGetPageNameOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
            var page = PageCache.Get( options.PageGuid );

            if ( page == null )
            {
                return NotFound();
            }

            var isAuthorized = page.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( page, Security.Authorization.VIEW ) == true;

            if ( !isAuthorized )
            {
                return Unauthorized();
            }

            return Ok( page.InternalName );
        }

        /// <summary>
        /// Gets the list of routes to the given page
        /// </summary>
        /// <param name="options">The options that describe which routes to retrieve.</param>
        /// <returns>A collection of <see cref="ListItemBag"/> that represent the routes.</returns>
        [HttpPost]
        [Route( "PagePickerGetPageRoutes" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "858209a4-7715-43e6-aff5-00b82773f241" )]
        public IActionResult PagePickerGetPageRoutes( [FromBody] PagePickerGetPageRoutesOptionsBag options )
        {
            var page = PageCache.Get( options.PageGuid );
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            if ( page == null )
            {
                return NotFound();
            }

            var isAuthorized = page.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( page, Security.Authorization.VIEW ) == true;

            if ( !isAuthorized )
            {
                return Unauthorized();
            }

            var routes = page.PageRoutes
                .Select( r => new ListItemBag
                {
                    Text = r.Route,
                    Value = r.Guid.ToString()
                } )
                .ToList();

            return Ok( routes );
        }

        #endregion

        #region Page Nav Buttons

        /// <summary>
        /// Gets the tree list of pages
        /// </summary>
        /// <param name="options">The options that describe which pages to retrieve.</param>
        /// <returns>A collection of <see cref="ListItemBag"/> objects that represent the links.</returns>
        [HttpPost]
        [Route( "PageNavButtonsGetLinks" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "49F4C35C-5528-44F3-9057-DCD3C387C8A5" )]
        public IActionResult PageNavButtonsGetLinks( [FromBody] PageNavButtonsGetLinksOptionsBag options )
        {
            if ( options.RootPageGuid == null || options.RootPageGuid.IsEmpty() )
            {
                return BadRequest( "Please provide a valid Root Page Guid." );
            }

            Person currentPerson = RockRequestContext.CurrentPerson;
            PageCache rootPage = PageCache.Get( options.RootPageGuid );
            List<ListItemBag> linkList = new List<ListItemBag>();

            if ( rootPage == null )
            {
                return BadRequest( "Root Page Does Not Exist" );
            }

            foreach ( PageCache page in GetPageNavButtonsChildPages( rootPage, currentPerson ) )
            {
                // href
                var pageReference = new PageReference( page.Id );
                if ( options.Parameters != null )
                {
                    pageReference.Parameters = options.Parameters;
                }

                if ( options.QueryString != null )
                {
                    var nvcQueryString = new NameValueCollection();

                    foreach ( var kvp in options.QueryString )
                    {
                        nvcQueryString.Add( kvp.Key.ToString(), kvp.Value.ToString() );
                    }

                    pageReference.QueryString = nvcQueryString;
                }

                var item = new ListItemBag
                {
                    Value = pageReference.BuildUrl(),
                    Text = page.PageTitle,
                };

                // class
                if ( page.Guid.Equals( options.CurrentPageGuid ) )
                {
                    item.Category = "active";
                }

                linkList.Add( item );
            }

            return Ok( linkList );
        }

        /// <summary>
        /// Gets the child pages of the given root.
        /// </summary>
        /// <param name="rootPage">The root page.</param>
        /// <param name="currentPerson">The current person.</param>
        private List<PageCache> GetPageNavButtonsChildPages( PageCache rootPage, Person currentPerson )
        {
            var pages = new List<PageCache>();

            using ( var rockContext = new RockContext() )
            {
                foreach ( PageCache page in rootPage.GetPages( rockContext ) )
                {
                    // IsAuthorized() knows how to handle a null person argument.
                    if ( page.DisplayInNavWhen == DisplayInNavWhen.WhenAllowed && !page.IsAuthorized( Security.Authorization.VIEW, currentPerson ) )
                    {
                        continue;
                    }

                    if ( page.DisplayInNavWhen == DisplayInNavWhen.Never )
                    {
                        continue;
                    }

                    pages.Add( page );
                }
            }

            return pages;
        }

        #endregion

        #region Person Link

        /// <summary>
        /// Gets the popup HTML for the selected person.
        /// </summary>
        /// <param name="options">The data needed to get the person's popup HTML.</param>
        /// <returns>A string containing the popup markups.</returns>
        [HttpPost]
        [Route( "PersonLinkGetPopupHtml" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( string ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "39f44203-9944-4dbd-87ca-d23657e0daa5" )]
        public IActionResult PersonLinkGetPopupHtml( [FromBody] PersonLinkGetPopupHtmlOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var result = "No Details Available";
                var html = new StringBuilder();
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                // Create new service (need ProxyServiceEnabled)
                var person = new PersonService( rockContext ).Queryable( "ConnectionStatusValue, PhoneNumbers" )
                    .Where( p => p.Id == options.PersonId )
                    .FirstOrDefault();

                if ( person != null )
                {
                    // If the entity can be secured, ensure the person has access to it.
                    if ( person is ISecured securedEntity )
                    {
                        var isAuthorized = securedEntity.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson )
                            || grant?.IsAccessGranted( person, Security.Authorization.VIEW ) == true;

                        if ( !isAuthorized )
                        {
                            return Unauthorized();
                        }
                    }

                    var appPath = System.Web.VirtualPathUtility.ToAbsolute( "~" );
                    html.AppendFormat(
                        "<header>{0} <h3>{1}<small>{2}</small></h3></header>",
                        Person.GetPersonPhotoImageTag( person, 65, 65 ),
                        person.FullName,
                        person.ConnectionStatusValue != null ? person.ConnectionStatusValue.Value : string.Empty );

                    html.Append( "<div class='body'>" );

                    var spouse = person.GetSpouse( rockContext );
                    if ( spouse != null )
                    {
                        html.AppendFormat(
                            "<div><strong>Spouse</strong> {0}</div>",
                            spouse.LastName == person.LastName ? spouse.FirstName : spouse.FullName );
                    }

                    int? age = person.Age;
                    if ( age.HasValue )
                    {
                        html.AppendFormat( "<div><strong>Age</strong> {0}</div>", age );
                    }

                    if ( !string.IsNullOrWhiteSpace( person.Email ) )
                    {
                        html.AppendFormat( "<div style='text-overflow: ellipsis; white-space: nowrap; overflow:hidden; width: 245px;'><strong>Email</strong> {0}</div>", person.GetEmailTag( RockRequestContext.ResolveRockUrl( "~/" ) ) );
                    }

                    foreach ( var phoneNumber in person.PhoneNumbers.Where( n => n.IsUnlisted == false && n.NumberTypeValueId.HasValue ).OrderBy( n => n.NumberTypeValue.Order ) )
                    {
                        html.AppendFormat( "<div><strong>{0}</strong> {1}</div>", phoneNumber.NumberTypeValue.Value, phoneNumber.ToString() );
                    }

                    html.Append( "</div>" );

                    result = html.ToString();
                }

                return Ok( result );
            }
        }

        #endregion

        #region Person Picker

        /// <summary>
        /// Searches for people that match the given search options and returns
        /// those matches.
        /// </summary>
        /// <param name="options">The options that describe how the search should be performed.</param>
        /// <returns>A collection of <see cref="PersonSearchResult"/> objects.</returns>
        [HttpPost]
        [Route( "PersonPickerSearch" )]
        [Authenticate]
        [Secured( Security.Authorization.EXECUTE_READ )]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( PersonSearchResult ) )]
        [Rock.SystemGuid.RestActionGuid( "1947578D-B28F-4956-8666-DCC8C0F2B945" )]
        public IActionResult PersonPickerSearch( [FromBody] PersonPickerSearchOptionsBag options )
        {
            var rockContext = new RockContext();

            // Chain to the v1 controller.
            var results = Rock.Rest.Controllers.PeopleController.SearchForPeople( rockContext, options.Name, options.Address, options.Phone, options.Email, options.IncludeDetails, options.IncludeBusinesses, options.IncludeDeceased, false );

            return Ok( results.ToList() );
        }

        #endregion

        #region Phone Number Box

        /// <summary>
        /// Get the phone number configuration related to country codes and number formats
        /// </summary>
        /// <returns>The configurations in the form of <see cref="ViewModels.Rest.Controls.PhoneNumberBoxGetConfigurationResultsBag"/>.</returns>
        [HttpPost]
        [Route( "PhoneNumberBoxGetConfiguration" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( PhoneNumberBoxGetConfigurationResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "2f15c4a2-92c7-4bd3-bf48-7eb11a644142" )]
        public IActionResult PhoneNumberBoxGetConfiguration( [FromBody] PhoneNumberBoxGetConfigurationOptionsBag options )
        {
            var countryCodeRules = new Dictionary<string, List<PhoneNumberCountryCodeRulesConfigurationBag>>();
            var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.COMMUNICATION_PHONE_COUNTRY_CODE.AsGuid() );
            string defaultCountryCode = null;
            var countryCodes = new List<string>();

            if ( definedType != null )
            {
                var definedValues = definedType.DefinedValues;

                foreach ( var countryCode in definedValues.OrderBy( v => v.Order ).Select( v => v.Value ).Distinct() )
                {
                    var rules = new List<PhoneNumberCountryCodeRulesConfigurationBag>();

                    if ( defaultCountryCode == null )
                    {
                        defaultCountryCode = countryCode;
                    }

                    foreach ( var definedValue in definedValues.Where( v => v.Value == countryCode ).OrderBy( v => v.Order ) )
                    {
                        string match = definedValue.GetAttributeValue( "MatchRegEx" );
                        string replace = definedValue.GetAttributeValue( "FormatRegEx" );
                        if ( !string.IsNullOrWhiteSpace( match ) && !string.IsNullOrWhiteSpace( replace ) )
                        {
                            rules.Add( new PhoneNumberCountryCodeRulesConfigurationBag { Match = match, Format = replace } );
                        }
                    }

                    countryCodeRules.Add( countryCode, rules );
                    countryCodes.Add( countryCode );
                }
            }

            if ( options?.ShowSmsOptIn ?? false )
            {
                return Ok( new PhoneNumberBoxGetConfigurationResultsBag
                {
                    Rules = countryCodeRules,
                    DefaultCountryCode = defaultCountryCode,
                    CountryCodes = countryCodes,
                    SmsOptInText = Rock.Web.SystemSettings.GetValue( Rock.SystemKey.SystemSetting.SMS_OPT_IN_MESSAGE_LABEL )
                } );
            }

            return Ok( new PhoneNumberBoxGetConfigurationResultsBag
            {
                Rules = countryCodeRules,
                CountryCodes = countryCodes,
                DefaultCountryCode = defaultCountryCode
            } );
        }

        #endregion

        #region Race Picker

        /// <summary>
        /// Gets the races that can be displayed in the race picker.
        /// </summary>
        /// <returns>A object that represents the races and the label for the control.</returns>
        [HttpPost]
        [Route( "RacePickerGetRaces" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( RacePickerGetRacesResultsBag ) )]
        [Rock.SystemGuid.RestActionGuid( "126eec10-7a19-49af-9646-909bd92ea516" )]
        public IActionResult RacePickerGetRaces()
        {
            var races = DefinedTypeCache.Get( SystemGuid.DefinedType.PERSON_RACE ).DefinedValues
                .Select( e => new ListItemBag { Text = e.Value, Value = e.Guid.ToString() } )
                .ToList();

            return Ok( new RacePickerGetRacesResultsBag
            {
                Races = races,
                Label = Rock.Web.SystemSettings.GetValue( Rock.SystemKey.SystemSetting.PERSON_RACE_LABEL )
            } );
        }

        #endregion

        #region Registration Instance Picker

        /// <summary>
        /// Gets the instances that can be displayed in the registration instance picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the registration instances for the control.</returns>
        [HttpPost]
        [Route( "RegistrationInstancePickerGetRegistrationInstances" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "26ecd3a7-9c55-4052-afc9-b59e84ab890b" )]
        public IActionResult RegistrationInstancePickerGetRegistrationInstances( [FromBody] RegistrationInstancePickerGetRegistrationInstancesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrationInstanceService = new Rock.Model.RegistrationInstanceService( new RockContext() );
                var registrationInstances = registrationInstanceService.Queryable()
                    .Where( ri => ri.RegistrationTemplate.Guid == options.RegistrationTemplateGuid && ri.IsActive )
                    .OrderBy( ri => ri.Name )
                    .ToList()
                    .Where( ri => ri.IsAuthorized( Security.Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                    .Select( ri => new ListItemBag { Text = ri.Name, Value = ri.Guid.ToString() } )
                    .ToList();

                return Ok( registrationInstances );
            }
        }

        /// <summary>
        /// Gets the registration template that the given instance uses.
        /// </summary>
        /// <returns>A <see cref="ListItemBag"/> object that represents the registration template.</returns>
        [HttpPost]
        [Route( "RegistrationInstancePickerGetRegistrationTemplateForInstance" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "acbccf4f-54d6-4c7c-8201-07fdefe87352" )]
        public IActionResult RegistrationInstancePickerGetRegistrationTemplateForInstance( [FromBody] RegistrationInstancePickerGetRegistrationTemplateForInstanceOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var registrationInstance = new Rock.Model.RegistrationInstanceService( rockContext ).Get( options.RegistrationInstanceGuid );
                if ( registrationInstance == null )
                {
                    return NotFound();
                }

                return Ok( new ListItemBag { Text = registrationInstance.RegistrationTemplate.Name, Value = registrationInstance.RegistrationTemplate.Guid.ToString() } );
            }
        }

        #endregion

        #region Registration Template Picker

        /// <summary>
        /// Gets the registration templates and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which registration templates to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of registration templates.</returns>
        [HttpPost]
        [Route( "RegistrationTemplatePickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "41eac873-20f3-4456-9fb4-746a1363807e" )]
        public IActionResult RegistrationTemplatePickerGetChildren( [FromBody] RegistrationTemplatePickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var queryOptions = new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.ParentGuid.HasValue,
                    EntityTypeGuid = EntityTypeCache.Get<RegistrationTemplate>().Guid,
                    IncludeUnnamedEntityItems = false,
                    IncludeCategoriesWithoutChildren = false,
                    DefaultIconCssClass = "fa fa-list-ol",
                    LazyLoad = true,
                    SecurityGrant = grant
                };

                var items = clientService.GetCategorizedTreeItems( queryOptions );

                return Ok( items );
            }
        }

        #endregion

        #region Reminder Button

        /// <summary>
        /// Gets the data needed for the initial state of the Reminder modal.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A Bag of data useful to initialize the Reminder Button.</returns>
        [HttpPost]
        [Route( "ReminderButtonGetReminders" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ReminderButtonGetRemindersResultsBag ) )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [Rock.SystemGuid.RestActionGuid( "4D015951-9B44-4E70-99AD-8D52728ADF3E" )]
        public IActionResult ReminderButtonGetReminders( [FromBody] ReminderButtonGetRemindersOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderViewModels = ReminderButtonGetReminders( options.EntityTypeGuid, options.EntityGuid, rockContext );
                var currentPerson = GetPerson();
                var currentPersonId = currentPerson?.Id;

                if ( currentPersonId == null || options.EntityTypeGuid.IsEmpty() || options.EntityGuid.IsEmpty() )
                {
                    return BadRequest();
                }

                var entityType = EntityTypeCache.Get( options.EntityTypeGuid );
                var contextEntity = Rock.Reflection.GetIEntityForEntityType( entityType.GetEntityType(), options.EntityGuid );

                // Load reminder types for this context entity.
                var reminderTypeService = new ReminderTypeService( rockContext );
                var reminderTypes = reminderTypeService.GetReminderTypesForEntityType( contextEntity.TypeId, currentPerson ).ToListItemBagList();

                var entityTypeName = EntityTypeCache.Get( contextEntity.TypeId ).FriendlyName;
                if ( contextEntity.TypeId == EntityTypeCache.GetId<PersonAlias>() )
                {
                    // Show "Person" instead of "Person Alias".
                    entityTypeName = EntityTypeCache.Get<Person>().FriendlyName;
                }

                var viewUrl = "";
                var editUrl = "";

                var pageReference = new PageReference( options.ViewRemindersPage.ToStringSafe() );
                if ( pageReference.PageId > 0 )
                {
                    viewUrl = pageReference.BuildUrl();
                }

                pageReference = new PageReference( options.EditReminderPage.ToStringSafe() );
                if ( pageReference.PageId > 0 )
                {
                    editUrl = pageReference.BuildUrl();
                }


                return Ok( new ReminderButtonGetRemindersResultsBag
                {
                    Reminders = reminderViewModels,
                    EntityName = contextEntity.ToString(),
                    EntityTypeName = entityTypeName,
                    ReminderTypes = reminderTypes,
                    ViewUrl = viewUrl,
                    EditUrl = editUrl,
                } );
            }
        }

        /// <summary>
        /// Add a new reminder.
        /// </summary>
        /// <param name="options">The data for the reminder to save.</param>
        /// <returns>The status of the insertion: successful or failed.</returns>
        [HttpPost]
        [Route( "ReminderButtonAddReminder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Description = "A 200 response indicates the reminder was added." )]
        [ProducesResponseType( HttpStatusCode.BadRequest )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "58DC4454-ED33-4871-9BD1-2AC9118340E2" )]
        public IActionResult ReminderButtonAddReminder( [FromBody] ReminderButtonAddReminderOptionsBag options )
        {
            if ( options.EntityTypeGuid.IsEmpty() || options.EntityGuid.IsEmpty() || options.ReminderTypeGuid.IsEmpty() )
            {
                return BadRequest();
            }

            using ( var rockContext = new RockContext() )
            {
                var entityType = EntityTypeCache.Get( options.EntityTypeGuid );
                var contextEntity = Rock.Reflection.GetIEntityForEntityType( entityType.GetEntityType(), options.EntityGuid );

                var reminderTypeService = new ReminderTypeService( new RockContext() );
                var reminderType = reminderTypeService.Get( options.ReminderTypeGuid );

                if ( reminderType == null )
                {
                    return BadRequest();
                }

                if ( !reminderType.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) )
                {
                    return Unauthorized();
                }

                var reminder = new Reminder
                {
                    EntityId = contextEntity.Id,
                    ReminderTypeId = reminderType.Id,
                    ReminderDate = DateTime.Parse( options.ReminderDate ),
                    Note = options.Note,
                    IsComplete = false,
                    RenewPeriodDays = options.RenewPeriodDays,
                    RenewMaxCount = options.RenewMaxCount,
                    RenewCurrentCount = 0
                };

                var person = GetPerson();
                PersonAlias personAlias = null;

                if ( !options.AssignedToGuid.IsEmpty() )
                {
                    person = new PersonService( rockContext ).Get( options.AssignedToGuid );
                }
                // Person might not be found because the given Guid might be a PersonAlias Guid
                if ( person == null && !options.AssignedToGuid.IsEmpty() )
                {
                    personAlias = new PersonAliasService( rockContext ).Get( options.AssignedToGuid );
                }

                if ( personAlias != null )
                {
                    reminder.PersonAliasId = personAlias.Id;
                }
                else
                {
                    reminder.PersonAliasId = person.PrimaryAliasId.Value;
                }

                var reminderService = new ReminderService( rockContext );
                reminderService.Add( reminder );
                rockContext.SaveChanges();

                return Ok( "ok" );
            }

        }

        /// <summary>
        /// Mark a reminder as complete.
        /// </summary>
        /// <param name="options">The data to determine which reminder to mark complete.</param>
        /// <returns>The list of reminders.</returns>
        [HttpPost]
        [Route( "ReminderButtonCompleteReminder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ReminderButtonGetRemindersReminderBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "D0720DE1-8417-4E01-8163-A17AB5D7F0BF" )]
        public IActionResult ReminderButtonCompleteReminder( [FromBody] ReminderButtonReminderActionOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderService = new ReminderService( rockContext );
                var reminder = reminderService.GetInclude( options.ReminderGuid, r => r.PersonAlias );

                if ( reminder.PersonAlias.PersonId != RockRequestContext.CurrentPerson?.Id )
                {
                    return Unauthorized();
                }

                reminder.CompleteReminder();
                rockContext.SaveChanges();

                var reminders = ReminderButtonGetReminders( options.EntityTypeGuid, options.EntityGuid, rockContext );

                return Ok( reminders );
            }
        }

        /// <summary>
        /// Delete a reminder.
        /// </summary>
        /// <param name="options">The data to determine which reminder to delete.</param>
        /// <returns>The list of reminders.</returns>
        [HttpPost]
        [Route( "ReminderButtonDeleteReminder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ReminderButtonGetRemindersReminderBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "52CF7D4D-E604-4B2E-B64E-DE865E2E0DF9" )]
        public IActionResult ReminderButtonDeleteReminder( [FromBody] ReminderButtonReminderActionOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderService = new ReminderService( rockContext );
                var reminder = reminderService.GetInclude( options.ReminderGuid, r => r.PersonAlias );

                if ( reminder.PersonAlias.PersonId != RockRequestContext.CurrentPerson?.Id )
                {
                    return Unauthorized();
                }

                reminderService.Delete( reminder );
                rockContext.SaveChanges();

                var reminders = ReminderButtonGetReminders( options.EntityTypeGuid, options.EntityGuid, rockContext );

                return Ok( reminders );
            }
        }

        /// <summary>
        /// Cancel the reoccurance of a reminder.
        /// </summary>
        /// <param name="options">The data to determine which reminder to cancel.</param>
        /// <returns>The list of reminders.</returns>
        [HttpPost]
        [Route( "ReminderButtonCancelReminder" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ReminderButtonGetRemindersReminderBag> ) )]
        [ProducesResponseType( HttpStatusCode.Unauthorized )]
        [Rock.SystemGuid.RestActionGuid( "2B3F7D40-2AD2-432E-8B77-C9F40AC45D2D" )]
        public IActionResult ReminderButtonCancelReminder( [FromBody] ReminderButtonReminderActionOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var reminderService = new ReminderService( rockContext );
                var reminder = reminderService.GetInclude( options.ReminderGuid, r => r.PersonAlias );

                if ( reminder.PersonAlias.PersonId != RockRequestContext.CurrentPerson?.Id )
                {
                    return Unauthorized();
                }

                reminder.CancelReoccurrence();
                rockContext.SaveChanges();

                var reminders = ReminderButtonGetReminders( options.EntityTypeGuid, options.EntityGuid, rockContext );

                return Ok( reminders );
            }
        }

        /// <summary>
        /// Fetch a list of the current person's 3 most pressing reminders for the given entity.
        /// </summary>
        /// <param name="entityTypeGuid"></param>
        /// <param name="entityGuid"></param>
        /// <param name="rockContext"></param>
        /// <returns>A List of <see cref="ReminderButtonGetRemindersReminderBag"/> objects that represent the reminders.</returns>
        private List<ReminderButtonGetRemindersReminderBag> ReminderButtonGetReminders( Guid entityTypeGuid, Guid entityGuid, RockContext rockContext )
        {
            var reminderViewModels = new List<ReminderButtonGetRemindersReminderBag>();
            var currentPerson = GetPerson();
            var currentPersonId = currentPerson?.Id;

            var entityType = EntityTypeCache.Get( entityTypeGuid );
            var contextEntity = Rock.Reflection.GetIEntityForEntityType( entityType.GetEntityType(), entityGuid );

            var reminderService = new ReminderService( rockContext );

            if ( contextEntity is PersonAlias personAlias )
            {
                var personAliasService = new PersonAliasService( rockContext );
                var personAliasIds = personAlias.Person.Aliases.Select( a => a.Id ).ToList();

                var reminders = reminderService
                    .GetReminders( currentPersonId.Value, contextEntity.TypeId, null, null )
                    .Where( r => personAliasIds.Contains( r.EntityId ) && !r.IsComplete && r.ReminderDate < RockDateTime.Now ) // only get active reminders for this person.
                    .OrderByDescending( r => r.ReminderDate )
                    .Take( 3 ); // We're only interested in two reminders plus one more just to determine if there are more than 2.

                foreach ( var reminder in reminders.ToList() )
                {
                    reminderViewModels.Add( new ReminderButtonGetRemindersReminderBag
                    {
                        Guid = reminder.Guid,
                        Id = reminder.Id,
                        ReminderDate = reminder.ReminderDate.ToShortDateString(),
                        HighlightColor = reminder.ReminderType.HighlightColor,
                        ReminderTypeName = reminder.ReminderType.Name,
                        Note = reminder.Note,
                        IsRenewing = reminder.IsRenewing
                    } );
                }
            }
            else
            {
                var entityTypeService = new EntityTypeService( rockContext );
                var reminders = reminderService
                    .GetReminders( currentPersonId.Value, contextEntity.TypeId, contextEntity.Id, null )
                    .Where( r => !r.IsComplete && r.ReminderDate < RockDateTime.Now ) // only get active reminders.
                    .OrderByDescending( r => r.ReminderDate )
                    .Take( 3 ); // We're only interested in two reminders plus one more just to determine if there are more than 2.

                foreach ( var reminder in reminders.ToList() )
                {
                    reminderViewModels.Add( new ReminderButtonGetRemindersReminderBag
                    {
                        Guid = reminder.Guid,
                        Id = reminder.Id,
                        ReminderDate = reminder.ReminderDate.ToShortDateString(),
                        HighlightColor = reminder.ReminderType.HighlightColor,
                        ReminderTypeName = reminder.ReminderType.Name,
                        Note = reminder.Note,
                        IsRenewing = reminder.IsRenewing
                    } );
                }
            }

            return reminderViewModels;
        }

        #endregion

        #region Reminder Type Picker

        /// <summary>
        /// Gets the reminder types that can be displayed in the reminder type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the reminder types.</returns>
        [HttpPost]
        [Route( "ReminderTypePickerGetReminderTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "c1c338d2-6364-4217-81ec-7fc34e9218b6" )]
        public IActionResult ReminderTypePickerGetReminderTypes( [FromBody] ReminderTypePickerGetReminderTypesOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var reminderTypesQuery = new ReminderTypeService( rockContext ).Queryable();

                if ( options.EntityTypeGuid != null )
                {
                    reminderTypesQuery = reminderTypesQuery.Where( t => t.EntityType.Guid == options.EntityTypeGuid );
                }

                var orderedReminderTypes = reminderTypesQuery
                    .OrderBy( t => t.EntityType.FriendlyName )
                    .ThenBy( t => t.Name )
                    .ToList()
                    .Where( t => t.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson )
                        || grant?.IsAccessGranted( t, Authorization.VIEW ) == true )
                    .Select( t => new ListItemBag
                    {
                        Value = t.Guid.ToString(),
                        Text = t.EntityType.FriendlyName + " - " + t.Name
                    } )
                    .ToList();

                return Ok( orderedReminderTypes );
            }
        }

        #endregion

        #region Remote Auths Picker

        /// <summary>
        /// Gets the remote auths that can be displayed in the remote auths picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the remote auths.</returns>
        [HttpPost]
        [Route( "RemoteAuthsPickerGetRemoteAuths" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "844D17E3-45FF-4A63-8BC7-32956A11CC94" )]
        public IActionResult RemoteAuthsPickerGetRemoteAuths()
        {
            var items = new List<ListItemBag>();

            foreach ( var serviceEntry in AuthenticationContainer.Instance.Components )
            {
                var component = serviceEntry.Value.Value;

                if ( component.IsActive && component.RequiresRemoteAuthentication )
                {
                    var entityType = EntityTypeCache.Get( component.GetType() );
                    if ( entityType != null )
                    {
                        items.Add( new ListItemBag { Text = entityType.FriendlyName, Value = entityType.Guid.ToString() } );
                    }
                }
            }

            return Ok( items );
        }

        #endregion

        #region Report Picker

        /// <summary>
        /// Gets the reports and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which reports to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of reports.</returns>
        [HttpPost]
        [Route( "ReportPickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "59545f7f-a27b-497c-8376-c85dfc360c11" )]
        public IActionResult ReportPickerGetChildren( [FromBody] ReportPickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var queryOptions = new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.ParentGuid.HasValue,
                    EntityTypeGuid = EntityTypeCache.Get<Report>().Guid,
                    IncludeUnnamedEntityItems = false,
                    IncludeCategoriesWithoutChildren = false,
                    IncludeCategoryGuids = options.IncludeCategoryGuids == null || options.IncludeCategoryGuids.Count == 0 ? null : options.IncludeCategoryGuids,
                    ItemFilterPropertyName = options.EntityTypeGuid.HasValue ? "EntityTypeId" : null,
                    ItemFilterPropertyValue = options.EntityTypeGuid.HasValue ? EntityTypeCache.Get( options.EntityTypeGuid.Value ).Id.ToString() : "",
                    DefaultIconCssClass = "fa fa-list-ol",
                    LazyLoad = true,
                    SecurityGrant = grant
                };

                var items = clientService.GetCategorizedTreeItems( queryOptions );

                return Ok( items );
            }
        }

        #endregion

        #region Save Financial Account Form

        /// <summary>
        /// Saves the financial account.
        /// </summary>
        /// <param name="options">The options that describe what account should be saved.</param>
        /// <returns>An object that represents the result of the operation.</returns>
        [HttpPost]
        [Route( "SaveFinancialAccountFormSaveAccount" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( SaveFinancialAccountFormSaveAccountResultBag ) )]
        [Rock.SystemGuid.RestActionGuid( "544B6302-A9E0-430E-A1C1-7BCBC4A6230C" )]
        public SaveFinancialAccountFormSaveAccountResultBag SaveFinancialAccountFormSaveAccount( [FromBody] SaveFinancialAccountFormSaveAccountOptionsBag options )
        {
            // Validate the arguments
            if ( options?.TransactionCode.IsNullOrWhiteSpace() != false )
            {
                return new SaveFinancialAccountFormSaveAccountResultBag
                {
                    Title = "Sorry",
                    Detail = "The account information cannot be saved as there's not a valid transaction code to reference",
                    IsSuccess = false
                };
            }

            if ( options.SavedAccountName.IsNullOrWhiteSpace() )
            {
                return new SaveFinancialAccountFormSaveAccountResultBag
                {
                    Title = "Missing Account Name",
                    Detail = "Please enter a name to use for this account",
                    IsSuccess = false
                };
            }

            var currentPerson = GetPerson();
            var isAnonymous = currentPerson == null;

            using ( var rockContext = new RockContext() )
            {
                if ( isAnonymous )
                {
                    if ( options.Username.IsNullOrWhiteSpace() || options.Password.IsNullOrWhiteSpace() )
                    {
                        return new SaveFinancialAccountFormSaveAccountResultBag
                        {
                            Title = "Missing Information",
                            Detail = "A username and password are required when saving an account",
                            IsSuccess = false
                        };
                    }

                    var userLoginService = new UserLoginService( rockContext );

                    if ( userLoginService.GetByUserName( options.Username ) != null )
                    {
                        return new SaveFinancialAccountFormSaveAccountResultBag
                        {
                            Title = "Invalid Username",
                            Detail = "The selected Username is already being used. Please select a different Username",
                            IsSuccess = false
                        };
                    }

                    if ( !UserLoginService.IsPasswordValid( options.Password ) )
                    {
                        return new SaveFinancialAccountFormSaveAccountResultBag
                        {
                            Title = "Invalid Password",
                            Detail = UserLoginService.FriendlyPasswordRules(),
                            IsSuccess = false
                        };
                    }
                }

                // Load the gateway from the database
                var financialGatewayService = new FinancialGatewayService( rockContext );
                var financialGateway = financialGatewayService.Get( options.GatewayGuid );
                var gateway = financialGateway?.GetGatewayComponent();

                if ( gateway is null )
                {
                    return new SaveFinancialAccountFormSaveAccountResultBag
                    {
                        Title = "Invalid Gateway",
                        Detail = "Sorry, the financial gateway information is not valid.",
                        IsSuccess = false
                    };
                }

                // Load the transaction from the database
                var financialTransactionService = new FinancialTransactionService( rockContext );
                var transaction = financialTransactionService.GetByTransactionCode( financialGateway.Id, options.TransactionCode );
                var transactionPersonAlias = transaction?.AuthorizedPersonAlias;
                var transactionPerson = transactionPersonAlias?.Person;
                var paymentDetail = transaction?.FinancialPaymentDetail;

                if ( transactionPerson is null || paymentDetail is null )
                {
                    return new SaveFinancialAccountFormSaveAccountResultBag
                    {
                        Title = "Invalid Transaction",
                        Detail = "Sorry, the account information cannot be saved as there's not a valid transaction to reference",
                        IsSuccess = false
                    };
                }

                // Create the login if needed
                if ( isAnonymous )
                {
                    var user = UserLoginService.Create(
                        rockContext,
                        transactionPerson,
                        AuthenticationServiceType.Internal,
                        EntityTypeCache.Get( SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                        options.Username,
                        options.Password,
                        false );

                    var mergeFields = Lava.LavaHelper.GetCommonMergeFields( null, currentPerson );
                    // TODO mergeFields.Add( "ConfirmAccountUrl", RootPath + "ConfirmAccount" );
                    mergeFields.Add( "Person", transactionPerson );
                    mergeFields.Add( "User", user );

                    var emailMessage = new RockEmailMessage( SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT.AsGuid() );
                    emailMessage.AddRecipient( new RockEmailMessageRecipient( transactionPerson, mergeFields ) );
                    // TODO emailMessage.AppRoot = ResolveRockUrl( "~/" );
                    // TODO emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                    emailMessage.CreateCommunicationRecord = false;
                    emailMessage.Send();
                }

                var savedAccount = new FinancialPersonSavedAccount
                {
                    PersonAliasId = transactionPersonAlias.Id,
                    ReferenceNumber = options.TransactionCode,
                    GatewayPersonIdentifier = options.GatewayPersonIdentifier,
                    Name = options.SavedAccountName,
                    TransactionCode = options.TransactionCode,
                    FinancialGatewayId = financialGateway.Id,
                    FinancialPaymentDetail = new FinancialPaymentDetail
                    {
                        AccountNumberMasked = paymentDetail.AccountNumberMasked,
                        CurrencyTypeValueId = paymentDetail.CurrencyTypeValueId,
                        CreditCardTypeValueId = paymentDetail.CreditCardTypeValueId,
                        NameOnCard = paymentDetail.NameOnCard,
                        ExpirationMonth = paymentDetail.ExpirationMonth,
                        ExpirationYear = paymentDetail.ExpirationYear,
                        BillingLocationId = paymentDetail.BillingLocationId
                    }
                };

                var financialPersonSavedAccountService = new FinancialPersonSavedAccountService( rockContext );
                financialPersonSavedAccountService.Add( savedAccount );

                System.Web.HttpContext.Current.AddOrReplaceItem( "CurrentPerson", RockRequestContext.CurrentPerson );
                rockContext.SaveChanges();

                return new SaveFinancialAccountFormSaveAccountResultBag
                {
                    Title = "Success",
                    Detail = "The account has been saved for future use",
                    IsSuccess = true
                };
            }
        }

        #endregion

        #region Schedule Picker

        /// <summary>
        /// Gets the schedules and their categories that match the options sent in the request body.
        /// This endpoint returns items formatted for use in a tree view control.
        /// </summary>
        /// <param name="options">The options that describe which schedules to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of schedules.</returns>
        [HttpPost]
        [Route( "SchedulePickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "60447abf-18f5-4ad1-a191-3a614408653b" )]
        public IActionResult SchedulePickerGetChildren( [FromBody] SchedulePickerGetChildrenOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );
                var queryOptions = new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = options.ParentGuid.HasValue,
                    EntityTypeGuid = EntityTypeCache.Get<Schedule>().Guid,
                    IncludeUnnamedEntityItems = false,
                    IncludeCategoriesWithoutChildren = false,
                    IncludeInactiveItems = options.IncludeInactiveItems,
                    DefaultIconCssClass = "fa fa-list-ol",
                    LazyLoad = true,
                    SecurityGrant = grant
                };

                if ( options.includePublicItemsOnly )
                {
                    queryOptions.ItemFilterPropertyName = "IsPublic";
                    queryOptions.ItemFilterPropertyValue = true.ToTrueFalse();
                }

                var items = clientService.GetCategorizedTreeItems( queryOptions );

                return Ok( items );
            }
        }

        #endregion

        #region Search Field

        /// <summary>
        /// Gets the search filters available for the Search Field control
        /// </summary>
        /// <returns>A Dictionary of <see cref="ListItemBag"/> objects that represent all of the availabe filters.</returns>
        [HttpPost]
        [Route( "SearchFieldGetSearchFilters" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( Dictionary<string, ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "6FF52C9E-985B-46C3-B5A5-E69312D189CB" )]
        public IActionResult SearchFieldGetSearchFilters()
        {
            var searchExtensions = new Dictionary<string, ListItemBag>();

            var currentPerson = RockRequestContext.CurrentPerson;
            if ( currentPerson != null )
            {
                foreach ( KeyValuePair<int, Lazy<Rock.Search.SearchComponent, Rock.Extension.IComponentData>> service in Rock.Search.SearchContainer.Instance.Components )
                {
                    var searchComponent = service.Value.Value;
                    if ( searchComponent.IsAuthorized( Security.Authorization.VIEW, currentPerson ) )
                    {
                        if ( !searchComponent.AttributeValues.ContainsKey( "Active" ) || bool.Parse( searchComponent.AttributeValues["Active"].Value ) )
                        {
                            var item = new ListItemBag
                            {
                                Value = searchComponent.ResultUrl,
                                Text = searchComponent.SearchLabel,

                            };
                            searchExtensions.Add( service.Key.ToString(), item );
                        }
                    }
                }
            }

            return Ok( searchExtensions );
        }

        #endregion

        #region Step Program Picker

        /// <summary>
        /// Gets the step programs that can be displayed in the step program picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the step programs.</returns>
        [HttpPost]
        [Route( "StepProgramPickerGetStepPrograms" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "6C7816B0-D41D-4081-B998-0B42B542111F" )]
        public IActionResult StepProgramPickerGetStepPrograms()
        {
            var items = new List<ListItemBag>();

            var stepProgramService = new StepProgramService( new RockContext() );
            var stepPrograms = stepProgramService.Queryable().AsNoTracking()
                .Where( sp => sp.IsActive )
                .OrderBy( sp => sp.Order )
                .ThenBy( sp => sp.Name )
                .ToList()
                .Where( sp => sp.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) );

            foreach ( var stepProgram in stepPrograms )
            {
                var li = new ListItemBag { Text = stepProgram.Name, Value = stepProgram.Guid.ToString() };
                items.Add( li );
            }

            return Ok( items );
        }

        #endregion

        #region Step Status Picker

        /// <summary>
        /// Gets the step statuses that can be displayed in the step status picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the step statuses.</returns>
        [HttpPost]
        [Route( "StepStatusPickerGetStepStatuses" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "5B4E7419-266C-4235-93B7-8D0DE0E80D2B" )]
        public IActionResult StepStatusPickerGetStepStatuses( [FromBody] StepStatusPickerGetStepStatusesOptionsBag options )
        {
            if ( !options.StepProgramGuid.HasValue )
            {
                return NotFound();
            }

            var items = new List<ListItemBag>();
            int stepProgramId = StepProgramCache.GetId( options.StepProgramGuid.Value ) ?? 0;
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            var stepStatusService = new StepStatusService( new RockContext() );
            var statuses = stepStatusService.Queryable().AsNoTracking()
                .Where( ss =>
                    ss.StepProgramId == stepProgramId &&
                    ss.IsActive )
                .OrderBy( ss => ss.Order )
                .ThenBy( ss => ss.Name )
                .ToList()
                .Where( ss => ss.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson )
                    || grant?.IsAccessGranted( ss, Authorization.VIEW ) == true );

            foreach ( var status in statuses )
            {
                var li = new ListItemBag { Text = status.Name, Value = status.Guid.ToString() };
                items.Add( li );
            }

            return Ok( items );
        }

        #endregion

        #region Step Type Picker

        /// <summary>
        /// Gets the step types that can be displayed in the step type picker.
        /// </summary>
        /// <param name="options">The options that describe which items to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the step types.</returns>
        [HttpPost]
        [Route( "StepTypePickerGetStepTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "9BC4C3BA-573E-4FB4-A4FC-938D40BED2BE" )]
        public IActionResult StepTypePickerGetStepTypes( [FromBody] StepTypePickerGetStepTypesOptionsBag options )
        {
            if ( !options.StepProgramGuid.HasValue )
            {
                return NotFound();
            }

            var items = new List<ListItemBag>();
            int stepProgramId = StepProgramCache.GetId( options.StepProgramGuid.Value ) ?? 0;
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            var stepTypeService = new StepTypeService( new RockContext() );
            var stepTypes = stepTypeService.Queryable().AsNoTracking()
                .Where( st =>
                    st.StepProgramId == stepProgramId &&
                    st.IsActive )
                .OrderBy( st => st.Order )
                .ThenBy( st => st.Name )
                .ToList()
                .Where( st => st.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson )
                    || grant?.IsAccessGranted( st, Authorization.VIEW ) == true );

            foreach ( var stepType in stepTypes )
            {
                var li = new ListItemBag { Text = stepType.Name, Value = stepType.Guid.ToString() };
                items.Add( li );
            }

            return Ok( items );
        }

        #endregion

        #region Streak Type Picker

        /// <summary>
        /// Gets the streak types that can be displayed in the streak type picker.
        /// </summary>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent the streak types.</returns>
        [HttpPost]
        [Route( "StreakTypePickerGetStreakTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "78D0A6D1-317E-4CB7-98BB-AF9194AD3C94" )]
        public IActionResult StreakTypePickerGetStreakTypes()
        {
            var items = new List<ListItemBag>();

            var streakTypes = StreakTypeCache.All()
                .Where( st => st.IsActive )
                .OrderBy( st => st.Name )
                .ThenBy( st => st.Id )
                .ToList()
                .Where( st => st.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) );

            foreach ( var streakType in streakTypes )
            {
                var li = new ListItemBag { Text = streakType.Name, Value = streakType.Guid.ToString() };
                items.Add( li );
            }

            return Ok( items );
        }

        #endregion

        #region Structured Content Editor

        /// <summary>
        /// Gets the structured content editor configuration.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The structured content editor configuration.</returns>
        [HttpPost]
        [Route( "StructuredContentEditorGetConfiguration" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( StructuredContentEditorConfigurationBag ) )]
        [Rock.SystemGuid.RestActionGuid( "71AD8E7A-3B38-4FC0-A4C7-95DB77F070F6" )]
        public IActionResult StructuredContentEditorGetConfiguration( [FromBody] StructuredContentEditorGetConfigurationOptionsBag options )
        {
            var structuredContentToolsConfiguration = string.Empty;
            if ( options.StructuredContentToolsValueGuid.HasValue )
            {
                var structuredContentToolsValue = DefinedValueCache.Get( options.StructuredContentToolsValueGuid.Value );
                if ( structuredContentToolsValue != null )
                {
                    structuredContentToolsConfiguration = structuredContentToolsValue.Description;
                }
            }

            if ( structuredContentToolsConfiguration.IsNullOrWhiteSpace() )
            {
                var structuredContentToolsValue = DefinedValueCache.Get( SystemGuid.DefinedValue.STRUCTURE_CONTENT_EDITOR_DEFAULT );
                if ( structuredContentToolsValue != null )
                {
                    structuredContentToolsConfiguration = structuredContentToolsValue.Description;
                }
            }

            return Ok( new StructuredContentEditorConfigurationBag
            {
                ToolsScript = structuredContentToolsConfiguration
            } );
        }

        #endregion

        #region Workflow Action Type Picker

        /// <summary>
        /// Gets the workflow action types and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which workflow action types to load.</param>
        /// <returns>A List of <see cref="TreeItemBag"/> objects that represent a tree of workflow action types.</returns>
        [HttpPost]
        [Route( "WorkflowActionTypePickerGetChildren" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "4275ae7f-16ab-4720-a79f-bf7b5ca979e8" )]
        public IActionResult WorkflowActionTypePickerGetChildren( [FromBody] WorkflowActionTypePickerGetChildrenOptionsBag options )
        {
            var list = new List<TreeItemBag>();

            // Folders
            if ( options.ParentId == 0 )
            {
                // Root
                foreach ( var category in ActionContainer.Instance.Categories )
                {
                    var item = new TreeItemBag();
                    item.Value = category.Key.ToString();
                    item.Text = category.Value;
                    item.HasChildren = true;
                    item.IconCssClass = "fa fa-folder";
                    list.Add( item );
                }
            }
            // Action Types
            else if ( options.ParentId < 0 && ActionContainer.Instance.Categories.ContainsKey( options.ParentId ) )
            {
                string categoryName = ActionContainer.Instance.Categories[options.ParentId];
                var categorizedActions = GetCategorizedWorkflowActions();
                if ( categorizedActions.ContainsKey( categoryName ) )
                {
                    foreach ( var entityType in categorizedActions[categoryName].OrderBy( e => e.FriendlyName ) )
                    {
                        var item = new TreeItemBag();
                        item.Value = entityType.Guid.ToString();
                        item.Text = ActionContainer.GetComponentName( entityType.Name );
                        item.HasChildren = false;
                        item.IconCssClass = "fa fa-cube";
                        list.Add( item );
                    }
                }
            }

            return Ok( list.OrderBy( i => i.Text ) );
        }

        /// <summary>
        /// Gets the categorized workflow actions.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<EntityTypeCache>> GetCategorizedWorkflowActions()
        {
            var categorizedActions = new Dictionary<string, List<EntityTypeCache>>();

            foreach ( var action in ActionContainer.Instance.Dictionary.Select( d => d.Value.Value ) )
            {
                string categoryName = "Uncategorized";

                var actionType = action.GetType();
                var obj = actionType.GetCustomAttributes( typeof( ActionCategoryAttribute ), true ).FirstOrDefault();
                if ( obj != null )
                {
                    var actionCategory = obj as ActionCategoryAttribute;
                    if ( actionCategory != null )
                    {
                        categoryName = actionCategory.CategoryName;
                    }
                }

                // "HideFromUser" is a special category name that is used to hide
                // workflow actions from showing up to the user. System user only.
                if ( !categoryName.Equals( "HideFromUser", System.StringComparison.OrdinalIgnoreCase ) )
                {
                    categorizedActions.TryAdd( categoryName, new List<EntityTypeCache>() );
                    categorizedActions[categoryName].Add( action.EntityType );
                }
            }

            return categorizedActions;
        }

        #endregion

        #region Workflow Picker

        /// <summary>
        /// Gets the workflows and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which workflows to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent a tree of workflows.</returns>
        [HttpPost]
        [Route( "WorkflowPickerGetWorkflows" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<ListItemBag> ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "93024bbe-4941-4f84-a5e7-754cf30c03d3" )]
        public IActionResult WorkflowPickerGetWorkflows( [FromBody] WorkflowPickerGetWorkflowsOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                if ( options.WorkflowTypeGuid == null )
                {
                    return NotFound();
                }

                var workflowType = WorkflowTypeCache.Get( options.WorkflowTypeGuid );

                if ( workflowType == null )
                {
                    return Ok( Array.Empty<ListItemBag>() );
                }

                // Default security for WorkflowType is world view. And a person needs
                // to have view access to launch a workflow. So require that an explicit
                // security grant exists for the workflow type.
                if ( !workflowType.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( workflowType, Authorization.VIEW ) != true )
                {
                    return Unauthorized();
                }

                var workflowService = new Rock.Model.WorkflowService( rockContext );
                var workflows = workflowService.Queryable()
                    .Where( w =>
                        w.WorkflowType.Guid == options.WorkflowTypeGuid &&
                        w.ActivatedDateTime.HasValue &&
                        !w.CompletedDateTime.HasValue )
                    .OrderBy( w => w.Name )
                    .Select( w => new ListItemBag { Value = w.Guid.ToString(), Text = w.Name } )
                    .ToList();

                return Ok( workflows );
            }
        }

        /// <summary>
        /// Gets the workflow type that the given instance uses.
        /// </summary>
        /// <returns>A <see cref="ListItemBag"/> object that represents the workflow type.</returns>
        [HttpPost]
        [Route( "WorkflowPickerGetWorkflowTypeForWorkflow" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListItemBag ) )]
        [ProducesResponseType( HttpStatusCode.NotFound )]
        [Rock.SystemGuid.RestActionGuid( "a41c755c-ffcb-459c-a67a-f0311158976a" )]
        public IActionResult WorkflowPickerGetWorkflowTypeForWorkflow( [FromBody] WorkflowPickerGetWorkflowTypeForWorkflowOptionsBag options )
        {
            var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

            using ( var rockContext = new RockContext() )
            {
                var workflow = new Rock.Model.WorkflowService( rockContext ).Get( options.WorkflowGuid );
                if ( workflow == null )
                {
                    return NotFound();
                }

                // Default security for WorkflowType is world view. And a person needs
                // to have view access to launch a workflow. So require that an explicit
                // security grant exists for the workflow type.
                if ( !workflow.IsAuthorized( Authorization.VIEW, RockRequestContext.CurrentPerson ) || grant?.IsAccessGranted( workflow.WorkflowType, Authorization.VIEW ) != true )
                {
                    return Unauthorized();
                }

                return Ok( new ListItemBag { Text = workflow.WorkflowType.Name, Value = workflow.WorkflowType.Guid.ToString() } );
            }
        }

        #endregion

        #region Workflow Type Picker

        /// <summary>
        /// Gets the workflow types and their categories that match the options sent in the request body.
        /// </summary>
        /// <remarks>
        /// This endpoint returns items formatted for use in a tree view control.
        /// </remarks>
        /// <param name="options">The options that describe which workflow types to load.</param>
        /// <returns>A List of <see cref="ListItemBag"/> objects that represent a tree of workflow types.</returns>
        [HttpPost]
        [Route( "WorkflowTypePickerGetWorkflowTypes" )]
        [Authenticate]
        [ExcludeSecurityActions( Security.Authorization.EXECUTE_READ, Security.Authorization.EXECUTE_WRITE, Security.Authorization.EXECUTE_UNRESTRICTED_READ, Security.Authorization.EXECUTE_UNRESTRICTED_WRITE )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( List<TreeItemBag> ) )]
        [Rock.SystemGuid.RestActionGuid( "622EE929-7A18-46BE-9AEA-9E0725293612" )]
        public IActionResult WorkflowTypePickerGetWorkflowTypes( [FromBody] WorkflowTypePickerGetWorkflowTypesOptionsBag options )
        {
            using ( var rockContext = new RockContext() )
            {
                var clientService = new CategoryClientService( rockContext, GetPerson( rockContext ) );
                var grant = SecurityGrant.FromToken( options.SecurityGrantToken );

                var items = clientService.GetCategorizedTreeItems( new CategoryItemTreeOptions
                {
                    ParentGuid = options.ParentGuid,
                    GetCategorizedItems = true,
                    EntityTypeGuid = Rock.SystemGuid.EntityType.WORKFLOW_TYPE.AsGuid(),
                    IncludeUnnamedEntityItems = true,
                    IncludeCategoriesWithoutChildren = false,
                    IncludeInactiveItems = options.IncludeInactiveItems,
                    LazyLoad = false,
                    SecurityGrant = grant
                } );

                return Ok( items );
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Retrieve a list of ListItems representing components for the given container type
        /// </summary>
        /// <param name="containerType"></param>
        /// <returns>A list of ListItems representing components</returns>
        private List<ListItemBag> GetComponentListItems( string containerType )
        {
            return GetComponentListItems( containerType, ( x ) => true );
        }

        /// <summary>
        /// Retrieve a list of ListItemBags representing components for the given container type. Filters any components
        /// out that don't pass the given validator
        /// </summary>
        /// <param name="containerType"></param>
        /// <param name="isValidComponentChecker"></param>
        /// <returns>A list of ListItemBags representing components</returns>
        private List<ListItemBag> GetComponentListItems( string containerType, Func<Component, bool> isValidComponentChecker )
        {
            if ( containerType.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var resolvedContainerType = Container.ResolveContainer( containerType );

            if ( resolvedContainerType == null )
            {
                return null;
            }

            var instanceProperty = resolvedContainerType.GetProperty( "Instance" );

            if ( instanceProperty == null )
            {
                return null;
            }

            var container = instanceProperty.GetValue( null, null ) as IContainer;
            var componentDictionary = container?.Dictionary;

            var items = new List<ListItemBag>();

            foreach ( var component in componentDictionary )
            {
                var componentValue = component.Value.Value;
                var entityType = EntityTypeCache.Get( componentValue.GetType() );

                if ( !componentValue.IsActive || entityType == null || !isValidComponentChecker( componentValue ) )
                {
                    continue;
                }

                var componentName = Rock.Reflection.GetDisplayName( entityType.GetEntityType() );

                // If it has a DisplayName use it as is, otherwise use the original logic
                if ( string.IsNullOrWhiteSpace( componentName ) )
                {
                    componentName = component.Value.Key;
                    // If the component name already has a space then trust
                    // that they are using the exact name formatting they want.
                    if ( !componentName.Contains( ' ' ) )
                    {
                        componentName = componentName.SplitCase();
                    }
                }

                items.Add( new ListItemBag
                {
                    Text = componentName,
                    Value = entityType.Guid.ToString().ToUpper()
                } );
            }

            return items.OrderBy( a => a.Text ).ToList();
        }

        /// <summary>
        /// Get the attributes for the given object
        /// </summary>
        /// <param name="model">The object to find the attributes of</param>
        /// <returns>A list of attributes in a form the Attribute Values Container can use</returns>
        private List<PublicAttributeBag> GetAttributes( IHasInheritedAttributes model )
        {
            using ( var rockContext = new RockContext() )
            {
                Type entityType = model.GetType();
                if ( entityType.IsDynamicProxyType() )
                {
                    entityType = entityType.BaseType;
                }

                var attributes = new List<AttributeCache>();

                var entityTypeCache = EntityTypeCache.Get( entityType );

                List<AttributeCache> allAttributes = null;
                Dictionary<int, List<int>> inheritedAttributes = null;

                // If this entity can provide inherited attribute information then
                // load that data now. If they don't provide any then generate empty lists.
                if ( model is IHasInheritedAttributes entityWithInheritedAttributes )
                {
                    allAttributes = entityWithInheritedAttributes.GetInheritedAttributes( rockContext );
                    inheritedAttributes = entityWithInheritedAttributes.GetAlternateEntityIdsByType( rockContext );
                }

                allAttributes = allAttributes ?? new List<AttributeCache>();
                inheritedAttributes = inheritedAttributes ?? new Dictionary<int, List<int>>();

                // Get all the attributes that apply to this entity type and this entity's
                // properties match any attribute qualifiers.
                var entityTypeId = entityTypeCache?.Id;

                if ( entityTypeCache != null )
                {
                    var entityTypeAttributesList = AttributeCache.GetByEntityType( entityTypeCache.Id );
                    if ( entityTypeAttributesList.Any() )
                    {
                        var entityTypeQualifierColumnPropertyNames = entityTypeAttributesList.Select( a => a.EntityTypeQualifierColumn ).Distinct().Where( a => !string.IsNullOrWhiteSpace( a ) ).ToList();
                        Dictionary<string, object> propertyValues = new Dictionary<string, object>( StringComparer.OrdinalIgnoreCase );
                        foreach ( var propertyName in entityTypeQualifierColumnPropertyNames )
                        {
                            System.Reflection.PropertyInfo propertyInfo = entityType.GetProperty( propertyName ) ?? entityType.GetProperties().Where( a => a.Name.Equals( propertyName, StringComparison.OrdinalIgnoreCase ) ).FirstOrDefault();
                            if ( propertyInfo != null )
                            {
                                propertyValues.TryAdd( propertyName, propertyInfo.GetValue( model, null ) );
                            }
                        }

                        var entityTypeAttributesForQualifier = entityTypeAttributesList.Where( x =>
                          string.IsNullOrEmpty( x.EntityTypeQualifierColumn ) ||
                                 ( propertyValues.ContainsKey( x.EntityTypeQualifierColumn ) &&
                                 ( string.IsNullOrEmpty( x.EntityTypeQualifierValue ) ||
                                 ( propertyValues[x.EntityTypeQualifierColumn] ?? "" ).ToString() == x.EntityTypeQualifierValue ) ) );

                        attributes.AddRange( entityTypeAttributesForQualifier );
                    }
                }

                // Append these attributes to our inherited attributes, in order.
                foreach ( var attribute in attributes.OrderBy( a => a.Order ) )
                {
                    allAttributes.Add( attribute );
                }
                var attributeList = allAttributes
                    .Where( a => a.IsActive )
                    .Select( a => new PublicAttributeBag
                    {
                        AttributeGuid = a.Guid,
                        FieldTypeGuid = FieldTypeCache.Get( a.FieldTypeId ).Guid,
                        Name = a.Name,
                        Key = a.Key,
                        Description = a.Description,
                        IsRequired = a.IsRequired,
                        Order = a.Order,
                        ConfigurationValues = a.ConfigurationValues
                    } )
                    .ToList();

                return attributeList;
            }
        }

        /// <summary>
        /// Converts the TreeViewItem to TreeItemBag.
        /// </summary>
        /// <param name="item">The TreeViewItem to be converted.</param>
        /// <returns>The item as a TreeItemBag</returns>
        private TreeItemBag convertTreeViewItemToTreeItemBag( TreeViewItem item )
        {
            return new TreeItemBag
            {
                Value = item.Id,
                Text = item.Name,
                IsFolder = item.HasChildren,
                HasChildren = item.HasChildren,
                IconCssClass = item.IconCssClass,
                IsActive = item.IsActive,
                Children = item.Children?.Select( convertTreeViewItemToTreeItemBag ).ToList()
            };
        }

        #endregion
    }
}
