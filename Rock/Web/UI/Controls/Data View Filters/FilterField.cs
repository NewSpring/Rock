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
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Reporting;
using Rock.Reporting.DataFilter;
using Rock.Security;
using Rock.Security.SecurityGrantRules;
using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// DataView Filter control
    /// </summary>
    [ToolboxData( "<{0}:FilterField runat=server></{0}:FilterField>" )]
    public class FilterField : CompositeControl
    {
        Dictionary<string, Dictionary<string, string>> AuthorizedComponents;

        /// <summary>
        /// The filter type dropdown
        /// </summary>
        protected RockDropDownList ddlFilterType;

        /// <summary>
        /// The database filter error
        /// </summary>
        protected NotificationBox nbFilterError;

        /// <summary>
        /// Gets or sets a value indicating whether [filter has error].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [filter has error]; otherwise, <c>false</c>.
        /// </value>
        public bool HasFilterError { get; private set; } = false;

        /// <summary>
        /// The delte button
        /// </summary>
        protected LinkButton lbDelete;

        /// <summary>
        /// The hidden field for tracking expanded
        /// </summary>
        protected HiddenField hfExpanded;

        /// <summary>
        /// The optional checkbox which can be used to disable/enable the filter for the current run of the report
        /// </summary>
        public RockCheckBox cbIncludeFilter;

        /// <summary>
        /// If the component has a Description this will be rendered with a description
        /// </summary>
        protected NotificationBox nbComponentDescription;

        /// <summary>
        /// The filter controls
        /// </summary>
        protected Control[] filterControls;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Gets or sets the name of entity type that is being filtered.
        /// NOTE: This is not to be confused with FilterEntityTypeName which is the DataFilter.Type.
        /// </summary>
        /// <value>
        /// The name of the filtered entity type.
        /// </value>
        public string FilteredEntityTypeName
        {
            get
            {
                return ViewState["FilteredEntityTypeName"] as string;
            }

            set
            {
                ViewState["FilteredEntityTypeName"] = value;

                AuthorizedComponents = null;

                if ( !string.IsNullOrWhiteSpace( value ) )
                {
                    string itemKey = "FilterFieldComponents:" + value;
                    if ( HttpContext.Current.Items.Contains( itemKey ) )
                    {
                        AuthorizedComponents = HttpContext.Current.Items[itemKey] as Dictionary<string, Dictionary<string, string>>;
                    }
                    else
                    {
                        AuthorizedComponents = new Dictionary<string, Dictionary<string, string>>();
                        RockPage rockPage = this.Page as RockPage;
                        if ( rockPage != null )
                        {
                            foreach ( var component in DataFilterContainer.GetComponentsByFilteredEntityName( value ).OrderBy( c => c.Order ).ThenBy( c => c.Section ).ThenBy( c => c.GetTitle( FilteredEntityType ) ) )
                            {
                                if ( component.IsAuthorized( Authorization.VIEW, rockPage.CurrentPerson ) )
                                {
                                    if ( !AuthorizedComponents.ContainsKey( component.Section ) )
                                    {
                                        AuthorizedComponents.Add( component.Section, new Dictionary<string, string>() );
                                    }

                                    AuthorizedComponents[component.Section].Add( component.TypeName, component.GetTitle( FilteredEntityType ) );
                                }
                            }

                        }

                        HttpContext.Current.Items.Add( itemKey, AuthorizedComponents );
                    }
                }

                RecreateChildControls();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the filterType dropdownlist should allow a search when used for single select
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enhance for long list]; otherwise, <c>false</c>.
        /// </value>
        public bool IsFilterTypeEnhancedForLongLists
        {
            get { return ViewState["IsFilterTypeEnhancedForLongLists"] as bool? ?? false; }
            set { ViewState["IsFilterTypeEnhancedForLongLists"] = value; }
        }

        /// <summary>
        /// Gets or sets the data view filter unique identifier.
        /// </summary>
        /// <value>
        /// The data view filter unique identifier.
        /// </value>
        public Guid DataViewFilterGuid
        {
            get
            {
                return ViewState["DataViewFilterGuid"] as Guid? ?? Guid.NewGuid();
            }

            set
            {
                ViewState["DataViewFilterGuid"] = value;
            }
        }

        /// <summary>
        /// Gets the type of the filtered entity.
        /// </summary>
        /// <value>
        /// The type of the filtered entity.
        /// </value>
        public Type FilteredEntityType
        {
            get
            {
                var entityTypeCache = EntityTypeCache.Get( FilteredEntityTypeName );
                if ( entityTypeCache != null )
                {
                    return entityTypeCache.GetEntityType();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the filter entity type.  This is a DataFilter type
        /// that applies to the FilteredEntityType
        /// NOTE: This is not to be confused with FilteredEntityTypeName which is the Rock.Data.EntityType
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public string FilterEntityTypeName
        {
            get
            {
                return ViewState["FilterEntityTypeName"] as string ?? "Rock.Reporting.DataFilter.PropertyFilter";
            }
            set
            {
                ViewState["FilterEntityTypeName"] = value;
                RecreateChildControls();
            }
        }

        /// <summary>
        /// Gets or sets optional key/value filter options.
        /// </summary>
        /// <value>
        /// The filter options.
        /// </value>
        [Obsolete]
        [RockObsolete( "17.0" )]
        public Dictionary<string, object> FilterOptions
        {
            get
            {
                return ViewState["FilterOptions"] as Dictionary<string, object>;
            }

            set
            {
                ViewState["FilterOptions"] = value;
                RecreateChildControls();
            }
        }

        /// <summary>
        /// Gets or sets the excluded filter types.
        /// </summary>
        /// <value>
        /// The excluded filter types.
        /// </value>
        public string[] ExcludedFilterTypes
        {
            get
            {
                return ViewState["ExcludedFilterTypes"] as string[] ?? new string[] { };
            }
            set
            {
                ViewState["ExcludedFilterTypes"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter mode (Advanced Filter or Simple Filter)
        /// </summary>
        /// <value>
        /// The filter mode.
        /// </value>
        public FilterMode FilterMode
        {
            get
            {
                return ViewState["FilterMode"] as FilterMode? ?? FilterMode.AdvancedFilter;
            }
            set
            {
                ViewState["FilterMode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [hide filter criteria].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [hide filter criteria]; otherwise, <c>false</c>.
        /// </value>
        public bool HideDescription
        {
            get
            {
                return ViewState["HideDescription"] as bool? ?? false;
            }
            set
            {
                ViewState["HideDescription"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [hide filter criteria].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [hide filter criteria]; otherwise, <c>false</c>.
        /// </value>
        public bool HideFilterCriteria
        {
            get
            {
                return ViewState["HideFilterCriteria"] as bool? ?? false;
            }
            set
            {
                ViewState["HideFilterCriteria"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a checkbox that enables/disables the filter for the current run
        /// </summary>
        /// <value>
        ///   <c>true</c> if [show checkbox]; otherwise, <c>false</c>.
        /// </value>
        public bool ShowCheckbox
        {
            get
            {
                return ViewState["ShowCheckbox"] as bool? ?? false;
            }
            set
            {
                ViewState["ShowCheckbox"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string ValidationGroup
        {
            get
            {
                return ViewState["ValidationGroup"] as string;
            }

            set
            {
                ViewState["ValidationGroup"] = value;
                SetFilterControlsValidationGroup( value );
            }
        }

        /// <summary>
        /// Sets the filter controls validation group.
        /// </summary>
        /// <param name="validationGroup">The validation group.</param>
        private void SetFilterControlsValidationGroup( string validationGroup )
        {
            var rockBlock = this.RockBlock();
            if ( filterControls != null && rockBlock != null && validationGroup != null )
            {
                rockBlock.SetValidationGroup( filterControls, validationGroup );
            }
        }

        /// <summary>
        /// Gets whether the Checkbox is checked or not (not factoring in if it is showing)
        /// </summary>
        /// <value>
        /// The CheckBox checked.
        /// </value>
        public bool? CheckBoxChecked
        {
            get
            {
                if ( cbIncludeFilter != null )
                {
                    return cbIncludeFilter.Checked;
                }

                return null;
            }
        }

        /// <summary>
        /// Sets the CheckBox checked (if it is showing)
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetCheckBoxChecked( bool value )
        {
            EnsureChildControls();

            if ( cbIncludeFilter != null )
            {
                cbIncludeFilter.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        public string Label
        {
            get
            {
                return ViewState["Label"] as string;
            }
            set
            {
                ViewState["Label"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the pre HTML.
        /// </summary>
        /// <value>
        /// The pre HTML.
        /// </value>
        public string PreHtml
        {
            get
            {
                return ViewState["PreHtml"] as string;
            }

            set
            {
                ViewState["PreHtml"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the post HTML.
        /// </summary>
        /// <value>
        /// The post HTML.
        /// </value>
        public string PostHtml
        {
            get
            {
                return ViewState["PostHtml"] as string;
            }

            set
            {
                ViewState["PostHtml"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FilterField" /> is expanded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expanded; otherwise, <c>false</c>.
        /// </value>
        public bool Expanded
        {
            get
            {
                EnsureChildControls();

                bool expanded = true;
                if ( !bool.TryParse( hfExpanded.Value, out expanded ) )
                    expanded = true;
                return expanded;
            }
            set
            {
                EnsureChildControls();
                hfExpanded.Value = value.ToString();
            }
        }

        /// <summary>
        /// Configures the field to use the Obsidian component if it is supported.
        /// </summary>
        [RockInternal( "17.0", true )]
        public bool UseObsidian
        {
            get => ( ViewState[nameof( UseObsidian )] as bool? ) ?? false;
            set => ViewState[nameof( UseObsidian )] = value;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="value">The value.</param>
        public void SetSelection( string value )
        {
            EnsureChildControls();

            var component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );
            if ( component != null )
            {
                if ( UseObsidian && component.ObsidianFileUrl != null )
                {
                    if ( component.ObsidianFileUrl.Length > 0 )
                    {
                        var obsidianWrapper = ( ObsidianDataComponentWrapper ) filterControls[0];
                        var requestContext = this.RockBlock()?.RockPage?.RequestContext;

                        using ( var rockContext = new RockContext() )
                        {
                            obsidianWrapper.ComponentData = component.GetObsidianComponentData( FilteredEntityType, value, rockContext, requestContext );
                        }
                    }
                }
                else
                {
                    component.SetSelection( FilteredEntityType, filterControls, value, this.FilterMode );
                }
            }
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <returns></returns>
        public string GetSelection()
        {
            EnsureChildControls();

            var component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );
            if ( component != null )
            {
                if ( UseObsidian && component.ObsidianFileUrl != null )
                {
                    if ( component.ObsidianFileUrl.Length > 0 )
                    {
                        var obsidianWrapper = ( ObsidianDataComponentWrapper ) filterControls[0];
                        var requestContext = this.RockBlock()?.RockPage?.RequestContext;

                        using ( var rockContext = new RockContext() )
                        {
                            return component.GetSelectionFromObsidianComponentData( FilteredEntityType, obsidianWrapper.ComponentData, rockContext, requestContext );
                        }
                    }
                }
                else
                {
                    return component.GetSelection( FilteredEntityType, filterControls, this.FilterMode );
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the related data view identifier.
        /// </summary>
        /// <returns></returns>
        public int? GetRelatedDataViewId()
        {
            EnsureChildControls();

            var component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );

            using ( var rockContext = new RockContext() )
            {
                var relatedDataViewId = component.GetRelatedDataViewId( FilteredEntityType, GetSelection(), rockContext );

                if ( relatedDataViewId.HasValue )
                {
                    return relatedDataViewId.Value;
                }
            }

            if ( component.ObsidianFileUrl != null )
            {
                return null;
            }

            if ( component is IRelatedChildDataView relatedDataViewComponent )
            {
                var relatedDataViewId = relatedDataViewComponent.GetRelatedDataViewId( filterControls );

                if ( relatedDataViewId.HasValue && relatedDataViewId > 0 )
                {
                    return relatedDataViewId;
                }
            }

            return null;
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            Controls.Clear();

            ddlFilterType = new RockDropDownList();
            Controls.Add( ddlFilterType );
            ddlFilterType.ID = this.ID + "_ddlFilter";
            ddlFilterType.EnhanceForLongLists = IsFilterTypeEnhancedForLongLists;

            nbFilterError = new NotificationBox();
            nbFilterError.ID = this.ID + "_nbFilterError";
            nbFilterError.Visible = false;
            HasFilterError = false;

            var filterEntityType = EntityTypeCache.Get( FilterEntityTypeName );
            var component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );
            if ( component != null )
            {
#pragma warning disable CS0612 // Type or member is obsolete
                component.Options = FilterOptions;
#pragma warning restore CS0612 // Type or member is obsolete
                if ( UseObsidian && component.ObsidianFileUrl != null )
                {
                    if ( component.ObsidianFileUrl.Length > 0 )
                    {
                        var obsidianWrapper = new ObsidianDataComponentWrapper
                        {
                            ID = $"{ID}_obsidianComponentWrapper",
                            ComponentUrl = ResolveUrl( component.ObsidianFileUrl ),
                            ComponentProperties = new Dictionary<string, object>
                            {
                                ["filterMode"] = FilterMode
                            }
                        };

                        Controls.Add( obsidianWrapper );
                        filterControls = new Control[1] { obsidianWrapper };
                    }
                    else
                    {
                        filterControls = new Control[0];
                    }
                }
                else
                {
                    filterControls = component.CreateChildControls( FilteredEntityType, this, this.FilterMode );
                }
            }
            else
            {
                nbFilterError.NotificationBoxType = NotificationBoxType.Danger;
                nbFilterError.Text = $"Unable to determine filter component for {FilterEntityTypeName}. ";
                nbFilterError.Visible = true;
                HasFilterError = true;
                Controls.Add( nbFilterError );
                filterControls = new Control[0];
            }

            SetFilterControlsValidationGroup( this.ValidationGroup );

            ddlFilterType.AutoPostBack = true;
            ddlFilterType.SelectedIndexChanged += ddlFilterType_SelectedIndexChanged;

            ddlFilterType.Items.Clear();
            if ( HasFilterError )
            {
                // if there is a FilterError, the filter component might not be listed, so it shows that nothing is selected if filtertype can't be found
                ddlFilterType.Items.Add( new ListItem() );
            }

            if ( AuthorizedComponents != null )
            {
                foreach ( var section in AuthorizedComponents )
                {
                    foreach ( var item in section.Value )
                    {
                        if ( !this.ExcludedFilterTypes.Any( a => a == item.Key ) )
                        {
                            ListItem li = new ListItem( item.Value, item.Key );

                            if ( !string.IsNullOrWhiteSpace( section.Key ) )
                            {
                                li.Attributes.Add( "optiongroup", section.Key );
                            }

                            var filterComponent = Rock.Reporting.DataFilterContainer.GetComponent( item.Key );
                            if ( filterComponent != null )
                            {
                                string description = Reflection.GetDescription( filterComponent.GetType() );
                                if ( !string.IsNullOrWhiteSpace( description ) )
                                {
                                    li.Attributes.Add( "title", description );
                                }
                            }

                            li.Selected = item.Key == FilterEntityTypeName;
                            ddlFilterType.Items.Add( li );
                        }
                    }
                }
            }

            hfExpanded = new HiddenField();
            Controls.Add( hfExpanded );
            hfExpanded.ID = this.ID + "_hfExpanded";
            hfExpanded.Value = "True";

            lbDelete = new LinkButton();
            Controls.Add( lbDelete );
            lbDelete.ID = this.ID + "_lbDelete";
            lbDelete.CssClass = "btn btn-xs btn-square btn-danger";
            lbDelete.Click += lbDelete_Click;
            lbDelete.CausesValidation = false;

            var iDelete = new HtmlGenericControl( "i" );
            lbDelete.Controls.Add( iDelete );
            iDelete.AddCssClass( "fa fa-times" );

            cbIncludeFilter = new RockCheckBox();
            cbIncludeFilter.ContainerCssClass = "filterfield-checkbox";
            cbIncludeFilter.TextCssClass = "control-label";
            Controls.Add( cbIncludeFilter );
            cbIncludeFilter.ID = this.ID + "_cbIncludeFilter";

            nbComponentDescription = new NotificationBox();
            nbComponentDescription.NotificationBoxType = NotificationBoxType.Info;
            nbComponentDescription.ID = this.ID + "_nbComponentDescription";
            Controls.Add( nbComponentDescription );
        }

        /// <summary>
        /// Writes the <see cref="T:System.Web.UI.WebControls.CompositeControl" /> content to the specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object, for display on the client.
        /// </summary>
        /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( !string.IsNullOrEmpty( PreHtml ) )
            {
                writer.Write( PreHtml );
            }

            DataFilterComponent component = null;
            string clientFormatString = string.Empty;
            if ( !string.IsNullOrWhiteSpace( FilterEntityTypeName ) )
            {
                component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );
                if ( component != null )
                {
                    clientFormatString =
                       string.Format( "if ($(this).find('.filter-view-state').children('i').hasClass('fa-chevron-up')) {{ var $article = $(this).parents('article').first(); var $content = $article.children('div.panel-body'); $article.find('div.filter-item-description').first().html({0}); }}", component.GetClientFormatSelection( FilteredEntityType ) );
                }

                if ( component?.ObsidianFileUrl != null )
                {
                    var entityTypeGuid = EntityTypeCache.Get( FilteredEntityTypeName )?.Guid;
                    var filterType = EntityTypeCache.Get( FilterEntityTypeName );

                    // This really shouldn't happen, but just in case something
                    // goes horribly wrong lets make sure we at least show
                    // something in the title bar.
                    if ( !entityTypeGuid.HasValue || filterType == null )
                    {
                        clientFormatString = $@"if ($(this).find('.filter-view-state').children('i').hasClass('fa-chevron-up')) {{
    var $article = $(this).parents('article').first();
    var $content = $article.children('div.panel-body');
    var $description = $article.find('div.filter-item-description').first();
    $description.text('{FilterEntityTypeName}');
}}".Replace( "\r\n", " " ).Replace( "\n", " " );
                    }
                    else
                    {
                        // We have to check access on the EntityType record because the
                        // component is not an IEntity so it will not work.
                        var securityGrantToken = new SecurityGrant()
                            .AddRule( new EntitySecurityGrantRule( filterType.CachedEntityTypeId, filterType.Id ) )
                            .ToToken();

                        clientFormatString = $@"if ($(this).find('.filter-view-state').children('i').hasClass('fa-chevron-up')) {{
    var $article = $(this).parents('article').first();
    var $content = $article.children('div.panel-body');
    var $description = $article.find('div.filter-item-description').first();
    var $icon = $(this).find('.filter-view-state').children('i');
    var data = $content.find('input[id$=\'_hfData\']').val();
    var json = JSON.stringify({{ securityGrantToken: '{securityGrantToken}', entityTypeGuid: '{entityTypeGuid}', filterTypeGuid: '{filterType.Guid}', componentData: data }});

    fetch('/api/v2/controls/DataFilterFormatSelection', {{ method: 'POST', body: json, headers: {{ 'Content-Type': 'application/json'}}}})
        .then(res => {{ if (res.status === 200) {{ return res.json(); }} else {{ return '{FilterEntityTypeName}'; }} }})
        .then(res => {{ if (!$icon.hasClass('fa-chevron-up')) {{ $description.text(res); }} }})
}}".Replace( "\r\n", " " ).Replace( "\n", " " );
                    }
                }
            }

            if ( component == null || HasFilterError )
            {
                writer.Write( "<a name='filtererror'></a>" );
                hfExpanded.Value = "True";
            }

            bool showFilterTypePicker = this.FilterMode == FilterMode.AdvancedFilter;

            if ( showFilterTypePicker )
            {
                // only render this stuff if the filter type picker is shown
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel panel-widget filter-item" );

                writer.RenderBeginTag( "article" );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-heading clearfix" );
                if ( !string.IsNullOrEmpty( clientFormatString ) )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Onclick, clientFormatString );
                }

                writer.RenderBeginTag( "header" );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "filter-expanded" );
                hfExpanded.RenderControl( writer );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-left" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "filter-item-description" );
                if ( Expanded )
                {
                    writer.AddStyleAttribute( HtmlTextWriterStyle.Display, "none" );
                }
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                string filterHeaderSelectionHtml;
                if ( HasFilterError )
                {
                    filterHeaderSelectionHtml = "<span class='label label-danger'>Filter has an error</span>";
                }
                else if ( component != null )
                {
                    filterHeaderSelectionHtml = component.FormatSelection( FilteredEntityType, this.GetSelection() );
                }
                else
                {
                    filterHeaderSelectionHtml = "Select Filter";
                }

                writer.Write( filterHeaderSelectionHtml );
                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "filter-item-select" );
                if ( !Expanded )
                {
                    writer.AddStyleAttribute( HtmlTextWriterStyle.Display, "none" );
                }
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                writer.RenderBeginTag( HtmlTextWriterTag.Span );
                writer.Write( "Filter Type " );
                writer.RenderEndTag();

                ddlFilterType.RenderControl( writer );
                writer.RenderEndTag();

                writer.RenderEndTag();

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-right" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                writer.AddAttribute( HtmlTextWriterAttribute.Class, "btn btn-link btn-xs filter-view-state" );
                writer.RenderBeginTag( HtmlTextWriterTag.A );
                writer.AddAttribute( HtmlTextWriterAttribute.Class, Expanded ? "fa fa-chevron-up" : "fa fa-chevron-down" );
                writer.RenderBeginTag( HtmlTextWriterTag.I );
                writer.RenderEndTag();
                writer.RenderEndTag();
                writer.Write( " " );
                lbDelete.Visible = ( this.DeleteClick != null );
                lbDelete.RenderControl( writer );
                writer.RenderEndTag();

                writer.RenderEndTag();

                writer.AddAttribute( "class", "panel-body" );
                if ( !Expanded )
                {
                    writer.AddStyleAttribute( HtmlTextWriterStyle.Display, "none" );
                }
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
            }

            writer.AddAttribute( "class", "js-filter-row filterfield" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            if ( ShowCheckbox )
            {
                //// EntityFieldFilter renders the checkbox itself (see EntityFieldFilter.cs),
                //// so only render the checkbox if we are hiding filter criteria and it isn't an entity field filter
                if ( !( component is Rock.Reporting.DataFilter.EntityFieldFilter ) || HideFilterCriteria )
                {
                    cbIncludeFilter.Text = this.Label;
                    cbIncludeFilter.RenderControl( writer );
                }
            }
            else if ( !string.IsNullOrWhiteSpace( this.Label ) )
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "control-label" );
                writer.AddAttribute( HtmlTextWriterAttribute.For, this.ClientID );
                writer.RenderBeginTag( HtmlTextWriterTag.Label );
                writer.Write( Label );
                writer.RenderEndTag();  // label
            }

            if ( HasFilterError )
            {
                nbFilterError.RenderControl( writer );
            }

            if ( component != null && !HideFilterCriteria )
            {
                if ( !string.IsNullOrEmpty( component.Description ) && !HideDescription )
                {
                    nbComponentDescription.Text = component.Description;
                    nbComponentDescription.CssClass = "filter-field-description";
                    nbComponentDescription.RenderControl( writer );
                }

                if ( UseObsidian && component.ObsidianFileUrl != null )
                {
                    if ( component.ObsidianFileUrl.Length > 0 )
                    {
                        filterControls[0].RenderControl( writer );
                    }
                }
                else
                {
                    component.RenderControls( FilteredEntityType, this, writer, filterControls, this.FilterMode );
                }
            }

            writer.RenderEndTag(); // "js-filter-row filter-row"

            if ( showFilterTypePicker )
            {
                writer.RenderEndTag();

                writer.RenderEndTag();
            }

            if ( !string.IsNullOrEmpty( PostHtml ) )
            {
                writer.Write( PostHtml );
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlFilterType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ddlFilterType_SelectedIndexChanged( object sender, EventArgs e )
        {
            FilterEntityTypeName = ( ( DropDownList ) sender ).SelectedValue;

            // If this is an Obsidian control then we need to set the selection
            // so it has a chance to prepare initial values.
            var component = Rock.Reporting.DataFilterContainer.GetComponent( FilterEntityTypeName );
            if ( component != null && UseObsidian && component.ObsidianFileUrl != null )
            {
                SetSelection( string.Empty );
            }

            if ( SelectionChanged != null )
            {
                SelectionChanged( this, e );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void lbDelete_Click( object sender, EventArgs e )
        {
            if ( DeleteClick != null )
            {
                DeleteClick( this, e );
            }
        }

        /// <summary>
        /// Occurs when [delete click].
        /// </summary>
        public event EventHandler DeleteClick;

        /// <summary>
        /// Occurs when [selection changed].
        /// </summary>
        public event EventHandler SelectionChanged;
    }
}