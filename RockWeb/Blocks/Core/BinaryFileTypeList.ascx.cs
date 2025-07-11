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

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Administration
{
    [DisplayName( "Binary File Type List" )]
    [Category( "Core" )]
    [Description( "Displays a list of all binary file types." )]

    [LinkedPage( "Detail Page",
        Key = AttributeKey.DetailPage )]

    [Rock.SystemGuid.BlockTypeGuid( "0926B82C-CBA2-4943-962E-F788C8A80037" )]
    public partial class BinaryFileTypeList : RockBlock, ICustomGridColumns
    {
        public static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
        }

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        private static class PageParameterKey
        {
            public const string BinaryFileTypeId = "BinaryFileTypeId";
            public const string ReturnUrl = "returnUrl";
            public const string AutoEdit = "autoEdit";
        }

        #endregion PageParameterKey

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gBinaryFileType.DataKeyNames = new string[] { "Id" };
            gBinaryFileType.Actions.ShowAdd = true;
            gBinaryFileType.Actions.AddClick += gBinaryFileType_Add;
            gBinaryFileType.GridRebind += gBinaryFileType_GridRebind;

            // Block Security and special attributes (RockPage takes care of View)
            bool canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gBinaryFileType.Actions.ShowAdd = canAddEditDelete;
            gBinaryFileType.IsDeleteEnabled = canAddEditDelete;

            SecurityField securityField = gBinaryFileType.Columns.OfType<SecurityField>().FirstOrDefault();
            securityField.EntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.BinaryFileType ) ).Id;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Events (main grid)

        /// <summary>
        /// Handles the Add event of the gBinaryFileType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gBinaryFileType_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.DetailPage, "BinaryFileTypeId", 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gBinaryFileType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gBinaryFileType_Edit( object sender, RowEventArgs e )
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.AddOrReplace( PageParameterKey.BinaryFileTypeId, e.RowKeyId.ToString() );
            queryParams.AddOrReplace( PageParameterKey.AutoEdit, "true" );
            queryParams.AddOrReplace( PageParameterKey.ReturnUrl, Request.RawUrl );
            NavigateToLinkedPage( AttributeKey.DetailPage, queryParams );
        }

        /// <summary>
        /// Handles the Delete event of the gBinaryFileType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gBinaryFileType_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            BinaryFileTypeService binaryFileTypeService = new BinaryFileTypeService( rockContext );
            BinaryFileType binaryFileType = binaryFileTypeService.Get( e.RowKeyId );

            if ( binaryFileType != null )
            {
                string errorMessage;
                if ( !binaryFileTypeService.CanDelete( binaryFileType, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                binaryFileTypeService.Delete( binaryFileType );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gBinaryFileType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gBinaryFileType_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            BinaryFileTypeService binaryFileTypeService = new BinaryFileTypeService( rockContext );
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );

            SortProperty sortProperty = gBinaryFileType.SortProperty;

            // join so we can both get BinaryFileCount quickly and be able to sort by it (having SQL do all the work)
            var qry = from ft in binaryFileTypeService.Queryable()
                      join bf in binaryFileService.Queryable().GroupBy( b => b.BinaryFileTypeId )
                      on ft.Id equals bf.Key into joinResult
                      from x in joinResult.DefaultIfEmpty()
                      select new
                      {
                          ft.Id,
                          ft.Name,
                          ft.Description,
                          BinaryFileCount = x.Key == null ? 0 : x.Count(),
                          StorageEntityType = ft.StorageEntityType != null ? ft.StorageEntityType.FriendlyName : string.Empty,
                          ft.IsSystem,
                          ft.CacheToServerFileSystem,
                          RequiresViewSecurity = ft.RequiresViewSecurity
                      };

            if ( sortProperty != null )
            {
                gBinaryFileType.DataSource = qry.Sort( sortProperty ).ToList();
            }
            else
            {
                gBinaryFileType.DataSource = qry.OrderBy( p => p.Name ).ToList();
            }

            gBinaryFileType.EntityTypeId = EntityTypeCache.Get<Rock.Model.BinaryFileType>().Id;
            gBinaryFileType.DataBind();
        }

        #endregion
    }
}