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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Rock.Data;
using Rock.Lava;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents an implementation of a <see cref="Rock.Model.BlockType"/> in Rock. A block can be implemented on a Site's <see cref="Rock.Model.Layout"/> and appear on 
    /// all pages on the site that uses that template or on an individual <see cref="Rock.Model.Page"/>.  
    /// 
    /// An example of a Block being implemented on a layout template would be an implementation of a HTML Content Block Type in the footer zone of a layout that contains the site's copyright notice.  
    /// This Block will show on all <see cref="Rock.Model.Page">Pages</see> of the <see cref="Rock.Model.Site"/> that uses the layout.
    /// 
    /// An example of a Block being implemented on a page would be the New Account <see cref="Rock.Model.BlockType"/> being implemented on the New Account page.
    /// </summary>
    [RockDomain( "CMS" )]
    [Table( "Block" )]
    [DataContract]
    [CodeGenerateRest]
    [EnableAttributeQualification( nameof( Id ) )]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.BLOCK )]
    public partial class Block : Model<Block>, IOrdered, ICacheable
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this Block was created by and is a part of the Rock core system/framework. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> that is <c>true</c> if this Block is part of the Rock core system/framework, otherwise is <c>false</c>.
        /// </value>
        /// <example>
        /// True
        /// </example>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.Page"/> that this Block is implemented on. This property will only be populated
        /// if the Block is implemented on a <see cref="Rock.Model.Page"/>.
        /// Blocks that have a specific PageId will only be shown in the specified Page
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> that represents the Id of the <see cref="Rock.Model.Page"/> that this Block is implemented on.  This value will be null if this Block is implemented 
        /// as part of a <see cref="Rock.Model.Layout"/> or <see cref="Rock.Model.Site"/>.
        /// </value>
        [DataMember]
        public int? PageId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.Layout"/> that this Block is implemented on. This property will only be populated
        /// if the Block is implemented on a <see cref="Rock.Model.Layout"/>.
        /// Blocks that have a specific LayoutId will be shown on all pages on a site that have the specified LayoutId
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> that represents the Id of the <see cref="Rock.Model.Layout"/> that this Block is implemented on.  This value will be null if this Block is implemented 
        /// as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Site"/>.
        /// </value>
        [DataMember]
        public int? LayoutId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.Site"/> that this Block is implemented on. This property will only be populated
        /// if the Block is implemented on a <see cref="Rock.Model.Site"/>.
        /// Blocks that have a specific SiteId will be shown on all pages on a site
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> that represents the Id of the <see cref="Rock.Model.Site"/> that this Block is implemented on.  This value will be null if this Block is implemented 
        /// as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Layout"/> .
        /// </value>
        [DataMember]
        public int? SiteId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.BlockType"/> that this Block is implementing. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> that represents the <see cref="Rock.Model.BlockType"/> that this Block is implementing.
        /// </value>
        /// <example>
        /// 4
        /// </example>
        [Required]
        [DataMember( IsRequired = true )]
        [EnableAttributeQualification]
        public int BlockTypeId { get; set; }

        /// <summary>
        /// Gets or sets the name of the layout zone/section that this Block is being implemented on. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents the name of the Zone that this Block is implemented on.
        /// </value>
        /// <example>
        /// Content
        /// </example>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string Zone { get; set; }

        /// <summary>
        /// Gets or sets the order that this Block appears in the <see cref="Rock.Model.Page"/>/Layout zone that the Block is implemented in.  Blocks are 
        /// displayed/rendered in Ascending (1,2,3,...) order. The lower the number the higher in the Zone the Block will appear.  <see cref="Rock.Model.Page"/> Blocks have
        /// priority over layout Blocks, so they will appear higher in the Zone than <see cref="Rock.Model.Site"/>/Layout Blocks. This property is required
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> that represents the display order of the Block in a <see cref="Rock.Model.Page"/>/Layout Zone.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a user defined name of the block implementation. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents a user defined name for the Block.
        /// </value>
        [MaxLength( 100 )]
        [Required( ErrorMessage = "Name is required" )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets an optional CSS class to include when the block's parent container is rendered
        /// </summary>
        /// <value>
        /// The CSS class.
        /// </value>
        [MaxLength( 100 )]
        [DataMember]
        public string CssClass { get; set; }

        /// <summary>
        /// Gets or sets any HTML to be rendered before the block
        /// </summary>
        /// <value>
        /// The pre HTML.
        /// </value>
        [DataMember]
        public string PreHtml { get; set; }

        /// <summary>
        /// Gets or sets any HTML to be rendered after the block
        /// </summary>
        /// <value>
        /// The post HTML.
        /// </value>
        [DataMember]
        public string PostHtml { get; set; }

        /// <summary>
        /// Gets or sets the length of time (in minutes) that the Block's data is cached. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> that represents the length of time that the Block's data is cached.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int OutputCacheDuration { get; set; }

        /// <summary>
        /// Gets or sets the additional settings.
        /// </summary>
        /// <value>
        /// The additional settings.
        /// </value>
        [DataMember]
        public string AdditionalSettings { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.BlockType"/> entity that this Block is implementing.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.BlockType"/> that that is being implemented by this Block.
        /// </value>
        [DataMember]
        public virtual BlockType BlockType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Page"/> entity that this Block is implemented on. This 
        /// property will be null if this Block is being implemented on as part of a <see cref="Rock.Model.Layout"/> or <see cref="Rock.Model.Site"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Page"/> entity that this Block is being implemented on. This value will 
        /// be null if the Block is implemented as part of a <see cref="Rock.Model.Layout"/> or <see cref="Rock.Model.Site"/>.
        /// </value>
        [LavaVisible]
        public virtual Page Page { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Layout"/> entity that this Block is implemented on. This 
        /// property will be null if this Block is being implemented on as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Site"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Layout"/> entity that this Block is being implemented on. This value will 
        /// be null if the Block is implemented as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Site"/>.
        /// </value>
        [LavaVisible]
        public virtual Layout Layout { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Site"/> entity that this Block is implemented on. This 
        /// property will be null if this Block is being implemented on as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Layout"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Site"/> entity that this Block is being implemented on. This value will 
        /// be null if the Block is implemented as part of a <see cref="Rock.Model.Page"/> or <see cref="Rock.Model.Layout"/>.
        /// </value>
        [LavaVisible]
        public virtual Site Site { get; set; }

        /// <summary>
        /// Provides a <see cref="Dictionary{TKey, TValue}"/> of actions that this model supports, and the description of each.
        /// </summary>
        public override Dictionary<string, string> SupportedActions
        {
            get
            {
                if ( _supportedActions == null )
                {
                    _supportedActions = new Dictionary<string, string>();
                    _supportedActions.Add( Authorization.VIEW, "The roles and/or users that have access to view the block." );
                    _supportedActions.Add( Authorization.EDIT, "The roles and/or users that have access to edit content on the block." );
                    _supportedActions.Add( Authorization.ADMINISTRATE, "The roles and/or users that have access to administrate the block.  This includes setting properties of the block, setting security for the block, moving the block, and deleting block from the zone." );
                }

                return _supportedActions;
            }
        }

        private Dictionary<string, string> _supportedActions;

        #endregion Navigation Properties

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance. Returns the Name of the Block
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Block Instance Configuration class.
    /// </summary>
    public partial class BlockConfiguration : EntityTypeConfiguration<Block>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockConfiguration"/> class.
        /// </summary>
        public BlockConfiguration()
        {
            this.HasRequired( p => p.BlockType ).WithMany( p => p.Blocks ).HasForeignKey( p => p.BlockTypeId ).WillCascadeOnDelete( true );
            this.HasOptional( p => p.Page ).WithMany( p => p.Blocks ).HasForeignKey( p => p.PageId ).WillCascadeOnDelete( true );
            this.HasOptional( p => p.Layout ).WithMany( p => p.Blocks ).HasForeignKey( p => p.LayoutId ).WillCascadeOnDelete( true );

            // NOTE: Can't delete cascade due to 'may cause cycles or multiple cascade paths' warning
            this.HasOptional( p => p.Site ).WithMany( p => p.Blocks ).HasForeignKey( p => p.SiteId ).WillCascadeOnDelete( false );
        }
    }

    #endregion
}
