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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Administration
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Security" )]
    [Category( "Administration" )]
    [Description( "Displays security settings for a specific entity." )]
    [Rock.SystemGuid.BlockTypeGuid( "20474B3D-0DE7-4B63-B7B9-E042DBEF788C" )]
    public partial class Security : RockBlock
    {
        #region Fields

        private ISecured iSecured;

        /// <summary>
        /// Gets or sets the current action.
        /// </summary>
        /// <value>
        /// The current action.
        /// </value>
        protected string CurrentAction
        {
            get
            {
                object currentAction = ViewState["CurrentAction"];
                return currentAction != null ? currentAction.ToString() : Authorization.VIEW;
            }

            set
            {
                ViewState["CurrentAction"] = value;
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            int? entityTypeId = PageParameter( "EntityTypeId" ).AsIntegerOrNull();
            string entityTypeName = string.Empty;
            Type type = null;

            // If we didn't find it by Id, check if it is a Guid and translate.
            if ( !entityTypeId.HasValue )
            {
                var entityTypeGuid = PageParameter( "EntityTypeId" ).AsGuidOrNull();

                if ( entityTypeGuid.HasValue )
                {
                    entityTypeId = EntityTypeCache.GetId( entityTypeGuid.Value );
                }
            }

            // Get Entity Type
            if ( entityTypeId.HasValue )
            {
                var entityType = EntityTypeCache.Get( entityTypeId.Value );
                if ( entityType != null )
                {
                    entityTypeName = entityType.FriendlyName;
                    type = entityType.GetEntityType();
                }
            }

            // Get object type
            if ( type != null )
            {
                var entityId = PageParameter( "EntityId" );
                if ( entityId.IsNotNullOrWhiteSpace() && entityId != "0" )
                {
                    iSecured = Rock.Reflection.GetIEntityForEntityType( type, entityId ) as ISecured;
                }

                if ( iSecured == null )
                {
                    iSecured = ( ISecured ) Activator.CreateInstance( type );
                }

                var block = iSecured as Rock.Model.Block;
                if ( block != null )
                {
                    // If the entity is a block, get any actions that were updated or added by the block type using
                    // one or more SecurityActionAttributes.
                    var blockCache = BlockCache.Get( block.Id );
                    if ( blockCache != null && blockCache.BlockType != null )
                    {
                        foreach ( var action in blockCache.BlockType.SecurityActions )
                        {
                            if ( block.SupportedActions.ContainsKey( action.Key ) )
                            {
                                block.SupportedActions[action.Key] = action.Value;
                            }
                            else
                            {
                                block.SupportedActions.Add( action.Key, action.Value );
                            }
                        }
                    }

                    iSecured = block;
                }

                if ( iSecured != null )
                {
                    if ( iSecured.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) )
                    {
                        if ( iSecured.SupportedActions.Any() )
                        {
                            // If we the selected action doesn't exist in the
                            // set of supported actions then select the first
                            // one.
                            if ( !iSecured.SupportedActions.ContainsKey( CurrentAction ) )
                            {
                                // Convert the dictionary of actions into something we
                                // can have a concrete order to.
                                var supportedActions = iSecured.SupportedActions
                                    .Select( a => a.Key )
                                    .ToList();

                                // If there is an Administrate action, move it to the end.
                                var administrateIndex = supportedActions.FindIndex( a => a == "Administrate" );
                                if ( administrateIndex != -1 )
                                {
                                    var administrateAction = supportedActions[administrateIndex];
                                    supportedActions.RemoveAt( administrateIndex );
                                    supportedActions.Add( administrateAction );
                                }

                                CurrentAction = supportedActions.FirstOrDefault();
                            }

                            lActionDescription.Text = iSecured.SupportedActions[CurrentAction];
                        }

                        BindActions();

                        rGrid.DataKeyNames = new string[] { "Id" };
                        rGrid.GridReorder += new GridReorderEventHandler( rGrid_GridReorder );
                        rGrid.GridRebind += new GridRebindEventHandler( rGrid_GridRebind );
                        rGrid.RowDataBound += new GridViewRowEventHandler( rGrid_RowDataBound );
                        rGrid.ShowHeaderWhenEmpty = false;
                        rGrid.EmptyDataText = string.Empty;
                        rGrid.ShowActionRow = false;

                        rGridParentRules.DataKeyNames = new string[] { "Id" };
                        rGridParentRules.ShowHeaderWhenEmpty = false;
                        rGridParentRules.EmptyDataText = string.Empty;
                        rGridParentRules.ShowActionRow = false;

                        BindRoles();

                        string scriptFormat = @"
                    Sys.Application.add_load(function () {{
                        $('#modal-popup div.modal-header h3 small', window.parent.document).html('{0}');
                    }});
                ";
                        string script = string.Format( scriptFormat, HttpUtility.JavaScriptStringEncode( iSecured.ToString() ) );

                        this.Page.ClientScript.RegisterStartupScript( this.GetType(), string.Format( "set-html-{0}", this.ClientID ), script, true );
                    }
                    else
                    {
                        nbMessage.Text = "Unfortunately, you are not able to edit security because you do not belong to a role that has been configured to allow administration of this item.";
                    }
                }
                else
                {
                    nbMessage.Text = "The item you are trying to secure does not exist or does not implement ISecured.";
                }
            }
            else
            {
                nbMessage.Text = string.Format( "The requested entity type ('{0}') could not be loaded to determine security attributes.", entityTypeName );
            }

            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( iSecured != null && iSecured.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) )
            {
                if ( !Page.IsPostBack )
                {
                    BindGrid();
                }
            }
            else
            {
                divActions.Visible = false;
                divContent.Visible = false;
                nbMessage.Visible = true;
            }

            base.OnLoad( e );
        }

        #endregion

        #region Events

        #region Grid Events

        /// <summary>
        /// Handles the GridReorder event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridReorderEventArgs"/> instance containing the event data.</param>
        protected void rGrid_GridReorder( object sender, GridReorderEventArgs e )
        {
            int entityTypeId = iSecured.TypeId;

            var rockContext = new RockContext();
            var authService = new Rock.Model.AuthService( rockContext );
            List<Rock.Model.Auth> rules = authService.GetAuths( iSecured.TypeId, iSecured.Id, CurrentAction ).ToList();
            authService.Reorder( rules, e.OldIndex, e.NewIndex );
            rockContext.SaveChanges();

            Authorization.RefreshAction( iSecured.TypeId, iSecured.Id, CurrentAction );

            BindGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void rGrid_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var myAuthRule = ( AuthRule ) e.Row.DataItem;
                RadioButtonList rbl = ( RadioButtonList ) e.Row.FindControl( "rblAllowDeny" );
                rbl.SelectedValue = myAuthRule.AllowOrDeny.ToString();
            }
        }

        /// <summary>
        /// Handles the Delete event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void rGrid_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            var authService = new Rock.Model.AuthService( rockContext );
            Rock.Model.Auth auth = authService.Get( e.RowKeyId );
            if ( auth != null )
            {
                authService.Delete( auth );
                rockContext.SaveChanges();

                Authorization.RefreshAction( iSecured.TypeId, iSecured.Id, CurrentAction );
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the rGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rGrid_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        /// <summary>
        /// Handles the Click event of the lbAction control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAction_Click( object sender, EventArgs e )
        {
            LinkButton lb = sender as LinkButton;
            if ( lb != null )
            {
                CurrentAction = lb.Text.Replace( " ", "" );

                BindActions();

                SetRoleActions();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblAllowDeny control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblAllowDeny_SelectedIndexChanged( object sender, EventArgs e )
        {
            RadioButtonList rblAllowDeny = ( RadioButtonList ) sender;
            GridViewRow selectedRow = rblAllowDeny.NamingContainer as GridViewRow;
            if ( selectedRow != null )
            {
                int id = ( int ) rGrid.DataKeys[selectedRow.RowIndex]["Id"];

                var rockContext = new RockContext();
                var authService = new Rock.Model.AuthService( rockContext );
                Rock.Model.Auth auth = authService.Get( id );
                if ( auth != null )
                {
                    auth.AllowOrDeny = rblAllowDeny.SelectedValue;
                    rockContext.SaveChanges();

                    Authorization.RefreshAction( iSecured.TypeId, iSecured.Id, CurrentAction );
                }
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbShowRole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbShowRole_Click( object sender, EventArgs e )
        {
            SetRoleActions();
            phList.Visible = false;
            pnlAddRole.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the lbShowUser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbShowUser_Click( object sender, EventArgs e )
        {
            phList.Visible = false;
            pnlAddUser.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the lbCancelAdd control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancelAdd_Click( object sender, EventArgs e )
        {
            pnlAddRole.Visible = false;
            pnlAddUser.Visible = false;
            phList.Visible = true;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlRoles control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlRoles_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetRoleActions();
        }

        /// <summary>
        /// Handles the Click event of the lbAddRole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddRole_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var authService = new AuthService( rockContext );

                foreach ( ListItem li in cblRoleActionList.Items )
                {
                    if ( li.Selected )
                    {
                        Rock.Model.SpecialRole specialRole = Rock.Model.SpecialRole.None;
                        int? groupId = ddlRoles.SelectedValue.AsIntegerOrNull();

                        switch ( groupId )
                        {
                            case -1:
                                specialRole = Rock.Model.SpecialRole.AllUsers;
                                break;
                            case -2:
                                specialRole = Rock.Model.SpecialRole.AllAuthenticatedUsers;
                                break;
                            case -3:
                                specialRole = Rock.Model.SpecialRole.AllUnAuthenticatedUsers;
                                break;
                            default:
                                specialRole = Rock.Model.SpecialRole.None;
                                break;
                        }

                        if ( groupId < 0 )
                        {
                            groupId = null;
                        }

                        var existingAuths = authService.GetAuths( iSecured.TypeId, iSecured.Id, li.Text ).ToList();
                        if ( !existingAuths.Any( a => a.SpecialRole == specialRole && a.GroupId.Equals( groupId ) ) )
                        {
                            int order = existingAuths.Count > 0 ? existingAuths.Last().Order + 1 : 0;

                            Rock.Model.Auth auth = new Rock.Model.Auth();
                            auth.EntityTypeId = iSecured.TypeId;
                            auth.EntityId = iSecured.Id;
                            auth.Action = li.Text;
                            auth.AllowOrDeny = "A";
                            auth.SpecialRole = specialRole;
                            auth.GroupId = groupId;
                            auth.Order = order;
                            authService.Add( auth );

                            rockContext.SaveChanges();

                            Authorization.RefreshAction( iSecured.TypeId, iSecured.Id, li.Text );
                        }
                    }
                }
            }

            pnlAddRole.Visible = false;
            phList.Visible = true;

            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbAddUser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddUser_Click( object sender, EventArgs e )
        {

            if ( ppUser.PersonId.HasValue )
            {
                int? personAliasId = ppUser.PersonAliasId;
                if ( personAliasId.HasValue )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var authService = new Rock.Model.AuthService( rockContext );
                        var existingAuths = authService.GetAuths( iSecured.TypeId, iSecured.Id, CurrentAction ).ToList();

                        if ( !existingAuths.Any( a => a.PersonAliasId.HasValue && a.PersonAliasId.Value == personAliasId.Value ) )
                        {
                            int order = existingAuths.Count > 0 ? existingAuths.Last().Order + 1 : 0;

                            Rock.Model.Auth auth = new Rock.Model.Auth();
                            auth.EntityTypeId = iSecured.TypeId;
                            auth.EntityId = iSecured.Id;
                            auth.Action = CurrentAction;
                            auth.AllowOrDeny = "A";
                            auth.SpecialRole = Rock.Model.SpecialRole.None;
                            auth.PersonAliasId = personAliasId;
                            auth.Order = order;
                            authService.Add( auth );

                            rockContext.SaveChanges();

                            Authorization.RefreshAction( iSecured.TypeId, iSecured.Id, CurrentAction );
                        }
                    }
                }
            }

            pnlAddUser.Visible = false;
            phList.Visible = true;

            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            using ( var rockContext = new RockContext() )
            {
                var authService = new AuthService( rockContext );

                var itemRules = new List<AuthRule>();
                foreach ( var auth in authService.GetAuths( iSecured.TypeId, iSecured.Id, CurrentAction ) )
                {
                    itemRules.Add( new AuthRule( auth ) );
                }

                rGrid.DataSource = itemRules;
                rGrid.DataBind();

                var parentRules = new List<MyAuthRule>();
                AddParentRules( authService, itemRules, parentRules, iSecured.ParentAuthorityPre, CurrentAction, false );
                AddParentRules( authService, itemRules, parentRules, iSecured.ParentAuthority, CurrentAction, true );
                rGridParentRules.DataSource = parentRules;
                rGridParentRules.DataBind();

                var hasAllUsersEntry = itemRules.Any( r => r.SpecialRole == SpecialRole.AllUsers )
                    || parentRules.Any( r => r.AuthRule.SpecialRole == SpecialRole.AllUsers );

                if ( hasAllUsersEntry )
                {
                    nbNoMatchSecurityMessage.Visible = false;
                }
                else
                {
                    var allowedOrDenied = iSecured.IsAllowedByDefault( CurrentAction )
                        ? "allowed"
                        : "denied";

                    nbNoMatchSecurityMessage.Text = string.Format( "The permission list does not include an \"All Users\" entry. Non-matching people will be {0} access.", allowedOrDenied );
                    nbNoMatchSecurityMessage.Visible = true;
                }
            }
        }

        /// <summary>
        /// Adds the parent rules.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="itemRules">The item rules.</param>
        /// <param name="parentRules">The parent rules.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="action">The action.</param>
        /// <param name="recurse">if set to <c>true</c> [recurse].</param>
        private void AddParentRules( AuthService authService, List<AuthRule> itemRules, List<MyAuthRule> parentRules, ISecured parent, string action, bool recurse )
        {
            if ( parent != null )
            {
                var entityType = EntityTypeCache.Get( parent.TypeId );
                foreach ( var auth in authService.GetAuths( parent.TypeId, parent.Id, action ) )
                {
                    var rule = new AuthRule( auth );

                    if ( !itemRules.Exists( r =>
                            r.SpecialRole == rule.SpecialRole &&
                            r.PersonId == rule.PersonId &&
                            r.GroupId == rule.GroupId ) &&
                        !parentRules.Exists( r =>
                            r.AuthRule.SpecialRole == rule.SpecialRole &&
                            r.AuthRule.PersonId == rule.PersonId &&
                            r.AuthRule.GroupId == rule.GroupId ) )
                    {
                        var myRule = new MyAuthRule( rule );
                        myRule.EntityTitle = string.Format( "{0} <small>({1})</small>", parent.ToString(), entityType.FriendlyName ?? entityType.Name ).TrimStart();
                        parentRules.Add( myRule );
                    }
                }

                if ( recurse )
                {
                    AddParentRules( authService, itemRules, parentRules, parent.ParentAuthority, action, true );
                }
            }
        }

        private void BindActions()
        {
            // Convert the dictionary of actions into something we
            // can have a concrete order to.
            var supportedActions = iSecured.SupportedActions
                .Select( a => new
                {
                    a.Key,
                    a.Value
                } )
                .ToList();

            // If there is an Administrate action, move it to the end.
            var administrateIndex = supportedActions.FindIndex( a => a.Key == "Administrate" );
            if ( administrateIndex != -1 )
            {
                var administrateAction = supportedActions[administrateIndex];
                supportedActions.RemoveAt( administrateIndex );
                supportedActions.Add( administrateAction );
            }

            rptActions.DataSource = supportedActions;
            rptActions.DataBind();
        }

        /// <summary>
        /// Binds the roles.
        /// </summary>
        private void BindRoles()
        {
            ddlRoles.Items.Clear();

            ddlRoles.Items.Add( new ListItem( "[All Users]", "-1" ) );
            ddlRoles.Items.Add( new ListItem( "[All Authenticated Users]", "-2" ) );
            ddlRoles.Items.Add( new ListItem( "[All Un-Authenticated Users]", "-3" ) );

            var securityRoleType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );

            foreach ( var role in RoleCache.AllRoles() )
            {
                string name = role.IsSecurityTypeGroup ? role.Name : "GROUP - " + role.Name;
                ddlRoles.Items.Add( new ListItem( name, role.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Gets the tab class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected string GetTabClass( object action )
        {
            if ( action.ToString() == CurrentAction )
            {
                return "active";
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Splits the case.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected string SplitCase( object action )
        {
            return action.ToString().SplitCase();
        }

        /// <summary>
        /// Sets the role actions.
        /// </summary>
        private void SetRoleActions()
        {
            cblRoleActionList.Items.Clear();

            using ( var rockContext = new RockContext() )
            {
                var authService = new AuthService( rockContext );

                var actions = iSecured.SupportedActions;
                foreach ( var action in actions )
                {
                    if ( action.Key == CurrentAction )
                    {
                        lActionDescription.Text = action.Value;

                        ListItem roleItem = new ListItem( action.Key );
                        roleItem.Selected = true;
                        cblRoleActionList.Items.Add( roleItem );
                    }
                    else
                    {
                        Rock.Model.SpecialRole specialRole = Rock.Model.SpecialRole.None;
                        int? groupId = ddlRoles.SelectedValue.AsIntegerOrNull();

                        switch ( groupId )
                        {
                            case -1:
                                specialRole = Rock.Model.SpecialRole.AllUsers;
                                break;
                            case -2:
                                specialRole = Rock.Model.SpecialRole.AllAuthenticatedUsers;
                                break;
                            case -3:
                                specialRole = Rock.Model.SpecialRole.AllUnAuthenticatedUsers;
                                break;
                            default:
                                specialRole = Rock.Model.SpecialRole.None;
                                break;
                        }

                        if ( groupId < 0 )
                        {
                            groupId = null;
                        }

                        if ( !authService.GetAuths( iSecured.TypeId, iSecured.Id, action.Key )
                            .Any( a => a.SpecialRole == specialRole && a.GroupId == groupId ) )
                        {
                            cblRoleActionList.Items.Add( new ListItem( action.Key ) );
                        }
                    }
                }
            }
        }

        #endregion
    }

    #region MyAuthRule class

    /// <summary>
    /// 
    /// </summary>
    public class MyAuthRule
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the entity title.
        /// </summary>
        /// <value>
        /// The entity title.
        /// </value>
        public string EntityTitle { get; set; }

        /// <summary>
        /// Gets or sets the authentication rule.
        /// </summary>
        /// <value>
        /// The authentication rule.
        /// </value>
        public AuthRule AuthRule { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAuthRule"/> class.
        /// </summary>
        /// <param name="rule">The rule.</param>
        public MyAuthRule( AuthRule rule )
        {
            Id = rule.Id;
            AuthRule = rule;
        }
    }

    #endregion
}