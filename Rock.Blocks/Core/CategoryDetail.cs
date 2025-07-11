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
using System.Linq;

using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Obsidian.UI;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Core.CategoryDetail;
using Rock.ViewModels.Cms;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.Core
{
    /// <summary>
    /// Displays the details of a particular category.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockDetailBlockType" />

    [DisplayName( "Category Detail" )]
    [Category( "Core" )]
    [Description( "Displays the details of a particular category." )]
    [IconCssClass( "fa fa-question" )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes
    [EntityTypeField( "Entity Type",
        Description = "The type of entity to associate category with",
        Key = AttributeKey.EntityType )]

    [TextField( "Entity Type Qualifier Property",
        IsRequired = false,
        Key = AttributeKey.EntityTypeQualifierProperty )]

    [TextField( "Entity Type Qualifier Value",
        IsRequired = false,
        Key = AttributeKey.EntityTypeQualifierValue )]

    [CategoryField( "Root Category",
        Description = "Select the root category to use as a starting point for the parent category picker.",
        AllowMultiple = false,
        IsRequired = false,
        Category = "CustomSetting",
        Key = AttributeKey.RootCategory )]

    [CategoryField( "Exclude Categories",
        Description = "Select any category that you need to exclude from the parent category picker",
        AllowMultiple = true,
        IsRequired = false,
        Category = "CustomSetting",
        Key = AttributeKey.ExcludeCategories )]
    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "2889352c-52ba-45f6-8ee1-9afa61211582" )]
    [Rock.SystemGuid.BlockTypeGuid( "515dc5c2-4fbd-4eea-9d8e-a807409defde" )]
    public class CategoryDetail : RockEntityDetailBlockType<Category, CategoryBag>, IHasCustomActions
    {
        #region Keys

        private static class AttributeKey
        {
            public const string EntityType = "EntityType";

            public const string EntityTypeQualifierProperty = "EntityTypeQualifierProperty";

            public const string EntityTypeQualifierValue = "EntityTypeQualifierValue";

            public const string RootCategory = "RootCategory";

            public const string ExcludeCategories = "ExcludeCategories";
        }

        private static class PageParameterKey
        {
            public const string CategoryId = "CategoryId";
            public const string ParentCategoryId = "ParentCategoryId";
        }

        private static class NavigationUrlKey
        {
            public const string CurrentPageTemplate = "CurrentPageTemplate";
            public const string ParentPage = "ParentPage";
        }

        #endregion Keys

        #region Fields

        /// <summary>
        /// Child Categories should be filtered to the EntityType that's selected in the EntityType block setting.
        /// </summary>
        private const string ChildCategoryQualifierColumn = "EntityTypeId";

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new DetailBlockBox<CategoryBag, CategoryDetailOptionsBag>();

            SetBoxInitialEntityState( box );

            box.NavigationUrls = GetBoxNavigationUrls();
            box.Options = GetBoxOptions( box.IsEditable );

            return box;
        }

        /// <summary>
        /// Gets the box options required for the component to render the view
        /// or edit the entity.
        /// </summary>
        /// <param name="isEditable"><c>true</c> if the entity is editable; otherwise <c>false</c>.</param>
        /// <returns>The options that provide additional details to the block.</returns>
        private CategoryDetailOptionsBag GetBoxOptions( bool isEditable )
        {
            var options = new CategoryDetailOptionsBag();

            options.ShowBlock = PageParameter( PageParameterKey.CategoryId )?.Length > 0;

            return options;
        }

        /// <summary>
        /// Validates the Category for any final information that might not be
        /// valid after storing all the data from the client.
        /// </summary>
        /// <param name="category">The Category to be validated.</param
        /// <param name="errorMessage">On <c>false</c> return, contains the error message.</param>
        /// <returns><c>true</c> if the Category is valid, <c>false</c> otherwise.</returns>
        private bool ValidateCategory( Category category, out string errorMessage )
        {
            errorMessage = null;

            if ( category.EntityTypeId == 0 )
            {
                errorMessage = "An EntityType was not configured for this block. Please contact your system administrator for assistance. <br />";
                return false;
            }

            // if the category IsValid is false, and the UI controls didn't report any errors, it is probably because the custom rules of category didn't pass.
            // So, make sure a message is displayed in the validation summary
            if ( !category.IsValid )
            {
                errorMessage = category.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the initial entity state of the box. Populates the Entity or
        /// ErrorMessage properties depending on the entity and permissions.
        /// </summary>
        /// <param name="box">The box to be populated.</param>
        private void SetBoxInitialEntityState( DetailBlockBox<CategoryBag, CategoryDetailOptionsBag> box )
        {
            var entity = GetInitialEntity();

            if ( entity == null )
            {
                box.ErrorMessage = $"The {Category.FriendlyTypeName} was not found.";
                return;
            }

            var isViewable = entity.IsAuthorized( Rock.Security.Authorization.VIEW, RequestContext.CurrentPerson );
            box.IsEditable = entity.IsAuthorized( Rock.Security.Authorization.EDIT, RequestContext.CurrentPerson );

            if ( entity.Id == 0 )
            {
                var entityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuidOrNull();
                if ( entityTypeGuid.HasValue )
                {
                    entity.EntityTypeId = EntityTypeCache.Get( entityTypeGuid.Value ).Id;
                }
            }
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
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToView( Category.FriendlyTypeName );
                }
            }
            else
            {
                // New entity is being created, prepare for edit mode by default.
                if ( box.IsEditable )
                {
                    box.Entity = GetEntityBagForEdit( entity );

                    // To support Category Tree View Add Category from the same page
                    // (e.g. Prayer Category page) - also include the valid properties.
                    // The Category Tree View Add Category click will redirect to the same page
                    // replacing the categoryId parameter only so we can't use autoEdit.
                    box.ValidProperties = box.Entity.GetType().GetProperties().Select( p => p.Name ).ToList();
                }
                else
                {
                    box.ErrorMessage = EditModeMessage.NotAuthorizedToEdit( Category.FriendlyTypeName );
                }
            }

            PrepareDetailBox( box, entity );
        }

        /// <summary>
        /// Gets the entity bag that is common between both view and edit modes.
        /// </summary>
        /// <param name="entity">The entity to be represented as a bag.</param>
        /// <returns>A <see cref="CategoryBag"/> that represents the entity.</returns>
        private CategoryBag GetCommonEntityBag( Category entity )
        {
            if ( entity == null )
            {
                return null;
            }

            return new CategoryBag
            {
                IdKey = entity.IdKey,
                CategoryId = entity.Id,
                Description = entity.Description,
                EntityType = entity.EntityType.ToListItemBag(),
                EntityTypeQualifierColumn = entity.EntityTypeQualifierColumn,
                EntityTypeQualifierValue = entity.EntityTypeQualifierValue,
                HighlightColor = entity.HighlightColor,
                IconCssClass = entity.IconCssClass,
                IsSystem = entity.IsSystem,
                Name = entity.Name,
                ParentCategory = entity.ParentCategory.ToListItemBag(),
                RootCategoryGuid = GetAttributeValue( AttributeKey.RootCategory ).AsGuidOrNull()
            };
        }

        /// <inheritdoc/>
        protected override CategoryBag GetEntityBagForView( Category entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );

            if ( bag != null )
            {
                var categoryService = new CategoryService( RockContext );
                bag.IsDeletable = categoryService.CanDelete( entity, out var _ );
            }

            bag.LoadAttributesAndValuesForPublicView( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            return bag;
        }

        /// <inheritdoc/>
        protected override CategoryBag GetEntityBagForEdit( Category entity )
        {
            if ( entity == null )
            {
                return null;
            }

            var bag = GetCommonEntityBag( entity );

            if ( entity.Id == 0 )
            {
                bag.EntityTypeQualifierColumn = GetAttributeValue( AttributeKey.EntityTypeQualifierProperty );
                bag.EntityTypeQualifierValue = GetAttributeValue( AttributeKey.EntityTypeQualifierValue );
                var entityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuidOrNull();
                if ( entityTypeGuid.HasValue )
                {
                    var entityType = EntityTypeCache.Get( entityTypeGuid.Value );
                    bag.EntityType = new ViewModels.Utility.ListItemBag
                    {
                        Text = entityType?.Name,
                        Value = entityType?.Guid.ToString()
                    };
                }

                var parentCategory = PageParameter( PageParameterKey.ParentCategoryId );
                if ( parentCategory.IsNotNullOrWhiteSpace() )
                {
                    bag.ParentCategory = CategoryCache.Get( parentCategory, !PageCache.Layout.Site.DisablePredictableIds ).ToListItemBag();
                }
            }

            bag.LoadAttributesAndValuesForPublicEdit( entity, RequestContext.CurrentPerson, enforceSecurity: true );

            return bag;
        }

        /// <inheritdoc/>
        protected override bool UpdateEntityFromBox( Category entity, ValidPropertiesBox<CategoryBag> box )
        {
            if ( box.ValidProperties == null )
            {
                return false;
            }

            box.IfValidProperty( nameof( box.Bag.Description ),
                () => entity.Description = box.Bag.Description );

            box.IfValidProperty( nameof( box.Bag.HighlightColor ),
                () => entity.HighlightColor = box.Bag.HighlightColor );

            box.IfValidProperty( nameof( box.Bag.IconCssClass ),
                () => entity.IconCssClass = box.Bag.IconCssClass );

            box.IfValidProperty( nameof( box.Bag.IsSystem ),
                () => entity.IsSystem = box.Bag.IsSystem );

            box.IfValidProperty( nameof( box.Bag.Name ),
                () => entity.Name = box.Bag.Name );

            box.IfValidProperty( nameof( box.Bag.ParentCategory ),
                () => entity.ParentCategoryId = box.Bag.ParentCategory.GetEntityId<Category>( RockContext ) );

            box.IfValidProperty( nameof( box.Bag.AttributeValues ),
                () =>
                {
                    entity.LoadAttributes( RockContext );
                    entity.SetPublicAttributeValues( box.Bag.AttributeValues, RequestContext.CurrentPerson, enforceSecurity: true );
                } );

            return true;
        }

        /// <inheritdoc/>
        protected override Category GetInitialEntity()
        {
            return GetInitialEntity<Category, CategoryService>( RockContext, PageParameterKey.CategoryId );
        }

        /// <summary>
        /// Gets the box navigation URLs required for the page to operate.
        /// </summary>
        /// <returns>A dictionary of key names and URL values.</returns>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            var routeWithCategoryId = this.PageCache.PageRoutes
                .FirstOrDefault( r =>
                    r.Parameters.Count == 1
                    && r.Parameters.FirstOrDefault().Equals( "CategoryId", StringComparison.OrdinalIgnoreCase ) );

            if ( routeWithCategoryId?.Route?.IsNotNullOrWhiteSpace() == true )
            {
                var templateUrl = $"{RequestContext.RootUrlPath}/{routeWithCategoryId?.Route}";
                return new Dictionary<string, string>
                {
                    [NavigationUrlKey.ParentPage] = this.GetParentPageUrl(),
                    [NavigationUrlKey.CurrentPageTemplate] = templateUrl
                };
            }
            else
            {
                return new Dictionary<string, string>
                {
                    [NavigationUrlKey.ParentPage] = this.GetParentPageUrl()
                };
            }
        }

        // <inheritdoc/>
        protected override bool TryGetEntityForEditAction( string idKey, out Category entity, out BlockActionResult error )
        {
            var entityService = new CategoryService( RockContext );
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
                entity = new Category();

                var entityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuidOrNull();
                if ( entityTypeGuid.HasValue )
                {
                    entity.EntityTypeId = EntityTypeCache.Get( entityTypeGuid.Value ).Id;
                }

                entityService.Add( entity );
            }

            if ( entity == null )
            {
                error = ActionBadRequest( $"{Category.FriendlyTypeName} not found." );
                return false;
            }

            if ( !entity.IsAuthorized( Rock.Security.Authorization.EDIT, RequestContext.CurrentPerson ) )
            {
                error = ActionBadRequest( $"Not authorized to edit ${Category.FriendlyTypeName}." );
                return false;
            }

            return true;
        }

        #endregion

        #region IHasCustomActions

        /// <inheritdoc/>
        List<BlockCustomActionBag> IHasCustomActions.GetCustomActions( bool canEdit, bool canAdministrate )
        {
            var actions = new List<BlockCustomActionBag>();

            if ( canAdministrate )
            {
                actions.Add( new BlockCustomActionBag
                {
                    IconCssClass = "fa fa-edit",
                    Tooltip = "Settings",
                    ComponentFileUrl = "/Obsidian/Blocks/Core/categoryDetailCustomSettings.obs"
                } );
            }

            return actions;
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

            return ActionOk( new ValidPropertiesBox<CategoryBag>
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
        public BlockActionResult Save( ValidPropertiesBox<CategoryBag> box )
        {
            var entityService = new CategoryService( RockContext );

            if ( !TryGetEntityForEditAction( box.Bag.IdKey, out var entity, out var actionError ) )
            {
                return actionError;
            }

            // Update the entity instance from the information in the bag.
            if ( !UpdateEntityFromBox( entity, box ) )
            {
                return ActionBadRequest( "Invalid data." );
            }

            var isNew = entity.Id == 0;
            if ( isNew )
            {
                if ( Guid.TryParse( GetAttributeValue( AttributeKey.EntityType ), out Guid entityTypeGuid ) )
                {
                    entity.EntityTypeId = EntityTypeCache.Get( entityTypeGuid ).Id;
                }
                entity.EntityTypeQualifierColumn = GetAttributeValue( AttributeKey.EntityTypeQualifierProperty );
                entity.EntityTypeQualifierValue = GetAttributeValue( AttributeKey.EntityTypeQualifierValue );
                int nextOrder = 0;

                if ( entity.ParentCategoryId.HasValue && entity.ParentCategoryId > 0 )
                {
                    var parentGuid = entityService.GetSelect( entity.ParentCategoryId.Value, c => c.Guid );

                    // Get the current max order for any sibling category and
                    // convert to a nullable int since there may be no siblings.
                    var maxOrder = entityService
                        .GetChildCategoryQuery( new Rock.Model.Core.Category.Options.ChildCategoryQueryOptions
                        {
                            ParentGuid = parentGuid
                        } )
                        .Max( siblingCategory => ( int? ) siblingCategory.Order );

                    nextOrder = ( maxOrder ?? -1 ) + 1;
                }

                entity.Order = nextOrder;
            }

            // Ensure everything is valid before saving.
            if ( !ValidateCategory( entity, out var validationMessage ) )
            {
                return ActionBadRequest( validationMessage );
            }

            RockContext.WrapTransaction( () =>
            {
                RockContext.SaveChanges();

                if ( box.Bag.DeleteAttributeValues )
                {
                    var attributeIds = entity.AttributeValues.Values.ToList().Select( a => a.AttributeId );
                    var attributeValueService = new AttributeValueService( RockContext );
                    var attributeValues = attributeValueService.GetByAttributeIdsAndEntityId( attributeIds, entity.Id );
                    attributeValueService.DeleteRange( attributeValues );
                    RockContext.SaveChanges();
                }
                else
                {
                    entity.SaveAttributeValues( RockContext );
                }
            } );

            if ( isNew )
            {
                return ActionContent( System.Net.HttpStatusCode.Created, this.GetCurrentPageUrl( new Dictionary<string, string>
                {
                    [PageParameterKey.CategoryId] = entity.Id.ToString()
                } ) );
            }

            // Ensure navigation properties will work now.
            entity = entityService.Get( entity.Id );
            entity.LoadAttributes( RockContext );

            var bag = GetEntityBagForView( entity );

            return ActionOk( new ValidPropertiesBox<CategoryBag>
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
            var entityService = new CategoryService( RockContext );

            if ( !TryGetEntityForEditAction( key, out var entity, out var actionError ) )
            {
                return actionError;
            }

            if ( !entityService.CanDelete( entity, out var errorMessage ) )
            {
                return ActionBadRequest( errorMessage );
            }

            entityService.Delete( entity );
            RockContext.SaveChanges();

            var pageReference = new Rock.Web.PageReference( this.PageCache.Guid.ToString(), new Dictionary<string, string>() );

            if ( pageReference.PageId > 0 )
            {
                return ActionOk( pageReference.BuildUrl() );
            }
            else
            {
                return ActionOk( this.GetCurrentPageUrl() );
            }
        }

        /// <summary>
        /// Gets the values and all other required details that will be needed
        /// to display the custom settings modal.
        /// </summary>
        /// <returns>A box that contains the custom settings values and additional data.</returns>
        [BlockAction]
        public BlockActionResult GetCustomSettings()
        {
            if ( !BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
            {
                return ActionForbidden( "Not authorized to edit block settings." );
            }

            var options = new CustomSettingsOptionsBag();
            var rootCategoryGuid = GetAttributeValue( AttributeKey.RootCategory ).AsGuidOrNull();
            var excludeCategoryGuids = GetAttributeValue( AttributeKey.ExcludeCategories ).SplitDelimitedValues().AsGuidList();
            var categoryGuids = excludeCategoryGuids.ToList();
            if ( rootCategoryGuid.HasValue )
            {
                categoryGuids.Add( rootCategoryGuid.Value );
            }
            var categories = new CategoryService( RockContext ).GetByGuids( categoryGuids );
            var settings = new CustomSettingsBag
            {
                RootCategory = rootCategoryGuid.HasValue ? categories.FirstOrDefault( a => a.Guid == rootCategoryGuid.Value ).ToListItemBag() : null,
                ExcludeCategories = categories.Where( a => excludeCategoryGuids.Contains( a.Guid ) ).ToListItemBagList(),
                EntityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuidOrNull()
            };

            return ActionOk( new CustomSettingsBox<CustomSettingsBag, CustomSettingsOptionsBag>
            {
                Settings = settings,
                Options = options,
                SecurityGrantToken = new Rock.Security.SecurityGrant().ToToken()
            } );
        }

        /// <summary>
        /// Saves the updates to the custom setting values for this block.
        /// </summary>
        /// <param name="box">The box that contains the setting values.</param>
        /// <returns>A response that indicates if the save was successful or not.</returns>
        [BlockAction]
        public BlockActionResult SaveCustomSettings( CustomSettingsBox<CustomSettingsBag, CustomSettingsOptionsBag> box )
        {
            if ( !BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
            {
                return ActionForbidden( "Not authorized to edit block settings." );
            }

            var block = new BlockService( RockContext ).Get( BlockId );
            block.LoadAttributes( RockContext );

            box.IfValidProperty( nameof( box.Settings.RootCategory ),
                () => block.SetAttributeValue( AttributeKey.RootCategory, box.Settings.RootCategory?.Value ) );

            box.IfValidProperty( nameof( box.Settings.ExcludeCategories ),
                () => block.SetAttributeValue( AttributeKey.ExcludeCategories, box.Settings.ExcludeCategories.Select( a => a.Value ).ToList().AsDelimited( "," ) ) );

            block.SaveAttributeValues( RockContext );

            return ActionOk();
        }

        /// <summary>
        /// Changes the ordered position of a single child category.
        /// </summary>
        /// <param name="key">The guid of the item that will be moved.</param>
        /// <param name="beforeKey">The guid of the item it will be placed before.</param>
        /// <returns>An empty result that indicates if the operation succeeded.</returns>
        [BlockAction]
        public BlockActionResult ReorderChildCategory( string parentCategoryIdKey, string idKey, string beforeIdKey )
        {
            using ( var rockContext = new RockContext() )
            {
                // Get the queryable and make sure it is ordered correctly.
                var items = OrderedChildCategories( parentCategoryIdKey, rockContext );

                if ( !items.ReorderEntity( idKey, beforeIdKey ) )
                {
                    return ActionBadRequest( "Invalid reorder attempt." );
                }

                foreach ( var item in items )
                {
                    rockContext.Entry( item ).State = System.Data.Entity.EntityState.Modified;
                }

                rockContext.SaveChanges();

                // Clear cached content for the changed items.
                CategoryCache.Remove( parentCategoryIdKey );
                CategoryCache.Remove( idKey );

                if ( beforeIdKey?.Length > 0 )
                {
                    CategoryCache.Remove( beforeIdKey );
                }

                return ActionOk();
            }
        }

        /// <summary>
        /// Gets a list of Categories that are a direct child of specified Category.Guid.
        /// </summary>
        /// <returns>A List of Categories.</returns>
        [BlockAction]
        public BlockActionResult GetChildCategoriesGridDefinition()
        {
            var entityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuid();
            var entityTypeId = EntityTypeCache.GetId( entityTypeGuid ).ToStringSafe();

            return ActionOk( ChildCategoriesGridBuilder( entityTypeId ).BuildDefinition() );
        }

        /// <summary>
        /// Gets a list of Categories that are direct children of specified category identifier.
        /// </summary>
        /// <returns>A list of categories.</returns>
        [BlockAction]
        public BlockActionResult GetChildCategories( string idKey )
        {
            var entityTypeGuid = GetAttributeValue( AttributeKey.EntityType ).AsGuid();
            var entityTypeId = EntityTypeCache.GetId( entityTypeGuid ).ToStringSafe();

            return ActionOk( ChildCategoriesGridBuilder( entityTypeId ).Build( OrderedChildCategories( idKey, RockContext ) ) );
        }

        /// <summary>
        /// Gets a list of ordered child categories for the specified Category <paramref name="idKey"/>.
        /// </summary>
        /// <param name="idKey">The parent id key hash to use for getting the list of child categories.</param>
        /// <returns>A list of <see cref="Category"/>.</returns>
        private List<Category> OrderedChildCategories( string idKey, RockContext rockContext )
        {
            var categoryService = new CategoryService( rockContext );
            var parentGuid = categoryService.GetSelect( idKey, c => c.Guid );

            var categories = categoryService
                .GetChildCategoryQuery( new Rock.Model.Core.Category.Options.ChildCategoryQueryOptions
                {
                    ParentGuid = parentGuid
                } )
                .ToList()
                .OrderBy( c => c.Order )
                .ThenBy( c => c.Name )
                .ThenBy( c => c.Id )
                .ToList();

            // Need to load attributes in case there are any grid attributes to display.
            categories.LoadAttributes( rockContext );

            return categories;
        }

        /// <summary>
        /// Gets the <see cref="GridBuilder"/> for the child categories list.
        /// </summary>
        /// <returns>a <see cref="GridBuilder{Category}"/> for the child categories grid.</returns>
        private GridBuilder<Category> ChildCategoriesGridBuilder( string qualifierValue )
        {
            var entityTypeId = EntityTypeCache.Get<Category>()?.Id;

            // Get the Show on Grid attributes for the child categories
            // for QualifierColumn = 'EntityType' and QualifierValue = EntityType in the Block setting.
            var gridAttributes = AttributeCache.GetOrderedGridAttributes( entityTypeId, ChildCategoryQualifierColumn, qualifierValue );

            return new GridBuilder<Category>()
                .AddField( "categoryId", a => a.Id )
                .AddTextField( "idKey", a => a.IdKey )
                .AddTextField( "name", a => a.Name )
                .AddField( "isSystem", a => a.IsSystem )
                .AddAttributeFields( gridAttributes );
        }

        #endregion
    }
}
