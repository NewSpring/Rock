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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Finance.FinancialAccountDetail;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.Finance
{
    /// <summary>
    /// Displays the details of a particular financial account.
    /// </summary>

    [DisplayName( "Account Detail" )]
    [Category( "Finance" )]
    [Description( "Displays the details of the given financial account." )]
    [IconCssClass( "fa fa-question" )]
    [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "76d45d23-1291-4829-a1fd-d3680dcc7db1" )]
    [Rock.SystemGuid.BlockTypeGuid( "c0c464c0-2c72-449f-b46f-8e31c1daf29b" )]
    public class FinancialAccountDetail : RockEntityDetailBlockType<FinancialAccount, FinancialAccountBag>
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string FinancialAccountId = "AccountId";
            public const string ExpandedIds = "ExpandedIds";
            public const string ParentAccountId = "ParentAccountId";
        }

        private static class NavigationUrlKey
        {
            public const string ParentPage = "ParentPage";
        }

        #endregion Keys

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new DetailBlockBox<FinancialAccountBag, FinancialAccountDetailOptionsBag>();

            SetBoxInitialEntityState( box );

            box.NavigationUrls = GetBoxNavigationUrls();
            box.Options = GetBoxOptions();

            return box;
        }

        /// <summary>
        /// Gets the box options required for the component to render the view
        /// or edit the entity.
        /// </summary>
        /// <returns>The options that provide additional details to the block.</returns>
        private FinancialAccountDetailOptionsBag GetBoxOptions()
        {
            var options = new FinancialAccountDetailOptionsBag();
            options.PurposeKeyOptions = new List<ListItemBag>()
            {
                new ListItemBag() { Text = RelatedEntityPurposeKey.GetPurposeKeyFriendlyName( RelatedEntityPurposeKey.FinancialAccountGivingAlert ), Value = RelatedEntityPurposeKey.FinancialAccountGivingAlert }
            };
            return options;
        }

        /// <summary>
        /// Validates the FinancialAccount for any final information that might not be
        /// valid after storing all the data from the client.
        /// </summary>
        /// <param name="financialAccount">The FinancialAccount to be validated.</param>
        /// <param name="errorMessage">On <c>false</c> return, contains the error message.</param>
        /// <returns><c>true</c> if the FinancialAccount is valid, <c>false</c> otherwise.</returns>
        private bool ValidateFinancialAccount( FinancialAccount financialAccount, out string errorMessage )
        {
            errorMessage = null;

            if ( !financialAccount.IsValid )
            {
                errorMessage = financialAccount.ValidationResults.ConvertAll( a => a.ErrorMessage ).AsDelimited( "<br />" );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the initial entity state of the box. Populates the Entity or
        /// ErrorMessage properties depending on the entity and permissions.
        /// </summary>
        /// <param name="box">The box to be populated.</param>
        private void SetBoxInitialEntityState( DetailBlockBox<FinancialAccountBag, FinancialAccountDetailOptionsBag> box )
        {
            var entity = GetInitialEntity();

            if ( entity == null )
            {
                box.ErrorMessage = $"The {FinancialAccount.FriendlyTypeName} was not found.";
                return;
            }

            var isViewable = entity.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson );
            box.IsEditable = entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );

            entity.LoadAttributes( RockContext );

            if ( entity.Id != 0 )
            {
                // Existing entity was found, prepare for view mode by default.
                if ( isViewable )
                {
                    box.Entity = GetEntityBagForView( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToView( FinancialAccount.FriendlyTypeName );
                }
            }
            else
            {
                // New entity is being created, prepare for edit mode by default.
                if ( box.IsEditable )
                {
                    box.Entity = GetEntityBagForEdit( entity );
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToEdit( FinancialAccount.FriendlyTypeName );
                }
            }

            PrepareDetailBox( box, entity );
        }

        /// <summary>
        /// Gets the entity bag that is common between both view and edit modes.
        /// </summary>
        /// <param name="entity">The entity to be represented as a bag.</param>
        /// <returns>A <see cref="FinancialAccountBag"/> that represents the entity.</returns>
        private FinancialAccountBag GetCommonEntityBag( FinancialAccount entity )
        {
            if ( entity == null )
            {
                return null;
            }

            return new FinancialAccountBag
            {
                IdKey = entity.IdKey,
                AccountTypeValue = entity.AccountTypeValue.ToListItemBag(),
                Campus = entity.Campus.ToListItemBag(),
                Description = entity.Description,
                EndDate = entity.EndDate,
                GlCode = entity.GlCode,
                IsActive = entity.IsActive,
                IsPublic = entity.IsPublic,
                IsTaxDeductible = entity.IsTaxDeductible,
                Name = entity.Name,
                ParentAccount = entity.ParentAccount.ToListItemBag(),
                PublicDescription = entity.PublicDescription,
                PublicName = entity.PublicName,
                StartDate = entity.StartDate,
                Url = entity.Url,
                UsesCampusChildAccounts = entity.UsesCampusChildAccounts,
                AccountParticipants = GetAccountParticipantStateFromDatabase( entity.Id ),
                ImageBinaryFile = entity.ImageBinaryFile.ToListItemBag(),
            };
        }

        /// <inheritdoc/>
        protected override FinancialAccountBag GetEntityBagForView( FinancialAccount entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );

            bag.ImageUrl = entity.ImageBinaryFileId.HasValue ? RequestContext.ResolveRockUrl( $"~/GetImage.ashx?id={entity.ImageBinaryFileId}" ) : string.Empty;
            bag.LoadAttributesAndValuesForPublicView( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            return bag;
        }

        /// <inheritdoc/>
        protected override FinancialAccountBag GetEntityBagForEdit( FinancialAccount entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );

            bag.LoadAttributesAndValuesForPublicEdit( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            return bag;
        }

        /// <inheritdoc/>
        protected override bool UpdateEntityFromBox( FinancialAccount entity, ValidPropertiesBox<FinancialAccountBag> box )
        {
            if ( box.ValidProperties == null )
            {
                return false;
            }

            box.IfValidProperty( nameof( box.Bag.AccountTypeValue ),
                () => entity.AccountTypeValueId = box.Bag.AccountTypeValue.GetEntityId<DefinedValue>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.Campus ),
                () => entity.CampusId = box.Bag.Campus.GetEntityId<Campus>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.Description ),
                () => entity.Description = box.Bag.Description );

            box.IfValidProperty( nameof( box.Bag.EndDate ),
                () => entity.EndDate = box.Bag.EndDate );

            box.IfValidProperty( nameof( box.Bag.GlCode ),
                () => entity.GlCode = box.Bag.GlCode );

            box.IfValidProperty( nameof( box.Bag.IsActive ),
                () => entity.IsActive = box.Bag.IsActive );

            box.IfValidProperty( nameof( box.Bag.IsPublic ),
                () => entity.IsPublic = box.Bag.IsPublic );

            box.IfValidProperty( nameof( box.Bag.IsTaxDeductible ),
                () => entity.IsTaxDeductible = box.Bag.IsTaxDeductible );

            box.IfValidProperty( nameof( box.Bag.Name ),
                () => entity.Name = box.Bag.Name );

            box.IfValidProperty( nameof( box.Bag.ParentAccount ),
                () => entity.ParentAccountId = box.Bag.ParentAccount.GetEntityId<FinancialAccount>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.PublicDescription ),
                () => entity.PublicDescription = box.Bag.PublicDescription );

            box.IfValidProperty( nameof( box.Bag.PublicName ),
                () => entity.PublicName = box.Bag.PublicName );

            box.IfValidProperty( nameof( box.Bag.StartDate ),
                () => entity.StartDate = box.Bag.StartDate );

            box.IfValidProperty( nameof( box.Bag.Url ),
                () => entity.Url = box.Bag.Url );

            box.IfValidProperty( nameof( box.Bag.UsesCampusChildAccounts ),
                () => entity.UsesCampusChildAccounts = box.Bag.UsesCampusChildAccounts );

            box.IfValidProperty( nameof( box.Bag.ImageBinaryFile ),
                () => entity.ImageBinaryFileId = box.Bag.ImageBinaryFile.GetEntityId<BinaryFile>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.AttributeValues ),
                () =>
                {
                    entity.LoadAttributes( RockContext );

                    entity.SetPublicAttributeValues( box.Bag.AttributeValues, RequestContext.CurrentPerson, enforceSecurity: true );
                } );

            // It is not valid for a financial account to have a campus and
            // be set to use campus child accounts.
            if ( entity.UsesCampusChildAccounts && entity.CampusId.HasValue )
            {
                entity.CampusId = null;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override FinancialAccount GetInitialEntity()
        {
            var entity = GetInitialEntity<FinancialAccount, FinancialAccountService>( RockContext, PageParameterKey.FinancialAccountId );

            var parentAccountId = PageParameter( PageParameterKey.ParentAccountId ).AsIntegerOrNull();
            if ( entity != null && entity.Id == 0 && parentAccountId.HasValue )
            {
                entity.ParentAccount = new FinancialAccountService( RockContext ).Get( parentAccountId.Value );
            }

            return entity;
        }

        /// <summary>
        /// Gets the box navigation URLs required for the page to operate.
        /// </summary>
        /// <returns>A dictionary of key names and URL values.</returns>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            var parentAccountId = PageParameter( PageParameterKey.ParentAccountId ).AsIntegerOrNull();
            if ( parentAccountId.HasValue )
            {
                var qryParams = new Dictionary<string, string>();
                if ( parentAccountId != 0 )
                {
                    qryParams["AccountId"] = parentAccountId.ToString();
                }

                qryParams["ExpandedIds"] = PageParameter( "ExpandedIds" );
                return new Dictionary<string, string>
                {
                    [NavigationUrlKey.ParentPage] = this.GetCurrentPageUrl( qryParams )
                };
            }
            else
            {
                return new Dictionary<string, string> { [NavigationUrlKey.ParentPage] = this.GetCurrentPageUrl() };
            }
        }

        /// <inheritdoc/>
        protected override bool TryGetEntityForEditAction( string idKey, out FinancialAccount entity, out BlockActionResult error )
        {
            var entityService = new FinancialAccountService( RockContext );
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
                // Create a new entity.
                entity = new FinancialAccount();
                entityService.Add( entity );
            }

            if ( entity == null )
            {
                error = ActionBadRequest( $"{FinancialAccount.FriendlyTypeName} not found." );
                return false;
            }

            if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                error = ActionBadRequest( $"Not authorized to edit ${FinancialAccount.FriendlyTypeName}." );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the account participants state from database.
        /// </summary>
        /// <returns>List&lt;AccountParticipantInfo&gt;.</returns>
        private List<FinancialAccountParticipantBag> GetAccountParticipantStateFromDatabase( int accountId )
        {
            var financialAccountService = new FinancialAccountService( new RockContext() );
            var accountParticipantsQuery = financialAccountService.GetAccountParticipantsAndPurpose( accountId );

            var participantsState = accountParticipantsQuery
                .AsEnumerable()
                .Select( a => new FinancialAccountParticipantBag
                {
                    PersonAlias = a.PersonAlias.ToListItemBag(),
                    PersonFullName = a.PersonAlias.Person.FullName,
                    PurposeKey = a.PurposeKey,
                    PurposeKeyDescription = RelatedEntityPurposeKey.GetPurposeKeyFriendlyName( a.PurposeKey )
                } ).ToList();

            return participantsState;
        }

        /// <summary>
        /// Saves the participants.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="entity">The entity.</param>
        private void SaveParticipants( ValidPropertiesBox<FinancialAccountBag> box, FinancialAccount entity, bool isNew )
        {
            var entityService = new FinancialAccountService( RockContext );

            if ( box.Bag.AccountParticipants.Count > 0 )
            {
                var accountParticipantsPersonAliasIdsByPurposeKey = box.Bag.AccountParticipants
                    .GroupBy( a => a.PurposeKey )
                    .ToDictionary( k =>
                        k.Key,
                        v => v.Select( x => x.PersonAlias.GetEntityId<PersonAlias>( RockContext ) ?? 0 )
                    .ToList() );

                foreach ( var purposeKey in accountParticipantsPersonAliasIdsByPurposeKey.Keys )
                {
                    var accountParticipantsPersonAliasIds = accountParticipantsPersonAliasIdsByPurposeKey.GetValueOrNull( purposeKey );
                    if ( accountParticipantsPersonAliasIds?.Any() == true )
                    {
                        var accountParticipants = new PersonAliasService( RockContext ).GetByIds( accountParticipantsPersonAliasIds ).ToList();
                        entityService.SetAccountParticipants( entity.Id, accountParticipants, purposeKey );
                    }
                }
            }
            else if ( !isNew )
            {
                // If this is an update and no participants were sent back from the client delete existing participants if any.
                var existingParticipants = GetAccountParticipantStateFromDatabase( entity.Id )
                    .GroupBy( a => a.PurposeKey )
                    .ToDictionary( k =>
                        k.Key,
                        v => v.Select( x => x.PersonAlias.GetEntityId<PersonAlias>( RockContext ) ?? 0 )
                    .ToList() );

                foreach ( var purposeKey in existingParticipants.Keys )
                {
                    entityService.SetAccountParticipants( entity.Id, new List<PersonAlias>(), purposeKey );
                }
            }
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the box that will contain all the information needed to begin
        /// the edit operation.
        /// </summary>
        /// <param name="key">The identifier of the entity to be edited.</param>
        /// <returns>A box that contains the entity and any other information required.</returns>
        [BlockAction]
        public BlockActionResult Edit( string key )
        {
            if ( !TryGetEntityForEditAction( key, out var entity, out var actionError ) )
            {
                return actionError;
            }

            entity.LoadAttributes( RockContext );

            var bag = GetEntityBagForEdit( entity );

            return ActionOk( new ValidPropertiesBox<FinancialAccountBag>
            {
                Bag = bag,
                ValidProperties = bag.GetType().GetProperties().Select( p => p.Name ).ToList()
            } );
        }

        /// <summary>
        /// Saves the entity contained in the box.
        /// </summary>
        /// <param name="box">The box that contains all the information required to save.</param>
        /// <returns>A new entity bag to be used when returning to view mode, or the URL to redirect to after creating a new entity.</returns>
        [BlockAction]
        public BlockActionResult Save( ValidPropertiesBox<FinancialAccountBag> box )
        {
            var entityService = new FinancialAccountService( RockContext );

            if ( !TryGetEntityForEditAction( box.Bag.IdKey, out var entity, out var actionError ) )
            {
                return actionError;
            }

            // Update the entity instance from the information in the bag.
            if ( !UpdateEntityFromBox( entity, box ) )
            {
                return ActionBadRequest( "Invalid data." );
            }

            // Ensure everything is valid before saving.
            if ( !ValidateFinancialAccount( entity, out var validationMessage ) )
            {
                return ActionBadRequest( new { isValidationError = true, message = validationMessage }.ToCamelCaseJson( false, true ) );
            }

            var isNew = entity.Id == 0;

            RockContext.WrapTransaction( () =>
            {
                RockContext.SaveChanges();
                entity.SaveAttributeValues( RockContext );
            } );

            SaveParticipants( box, entity, isNew );

            RockContext.SaveChanges();

            if ( isNew )
            {
                return ActionContent( System.Net.HttpStatusCode.Created, this.GetCurrentPageUrl( new Dictionary<string, string>
                {
                    [PageParameterKey.FinancialAccountId] = entity.IdKey,
                    [PageParameterKey.ExpandedIds] = PageParameter( PageParameterKey.ExpandedIds ),
                } ) );
            }

            // Ensure navigation properties will work now.
            entity = entityService.Get( entity.Id );
            entity.LoadAttributes( RockContext );

            var bag = GetEntityBagForView( entity );

            return ActionOk( new ValidPropertiesBox<FinancialAccountBag>
            {
                Bag = bag,
                ValidProperties = bag.GetType().GetProperties().Select( p => p.Name ).ToList()
            } );
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        /// <param name="key">The identifier of the entity to be deleted.</param>
        /// <returns>A string that contains the URL to be redirected to on success.</returns>
        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            var entityService = new FinancialAccountService( RockContext );

            if ( !TryGetEntityForEditAction( key, out var entity, out var actionError ) )
            {
                return actionError;
            }

            if ( !entityService.CanDelete( entity, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            var parentAccountId = entity.ParentAccountId;
            entityService.Delete( entity );
            RockContext.SaveChanges();

            var qryParams = new Dictionary<string, string>();
            if ( parentAccountId != null )
            {
                qryParams["AccountId"] = parentAccountId.ToString();
            }
            else
            {
                qryParams["AccountId"] = "0";
            }

            qryParams["ExpandedIds"] = PageParameter( "ExpandedIds" );

            return ActionOk( this.GetCurrentPageUrl( qryParams ) );
        }

        #endregion
    }
}
