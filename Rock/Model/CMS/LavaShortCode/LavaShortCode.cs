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
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// A Lava Shortcode represents a small snippet of Lava code for use within the
    /// content of a web page, email, or other Lava-enabled areas. 
    /// </summary>
    [RockDomain( "CMS" )]
    [Table( "LavaShortcode" )]
    [DataContract]
    [CodeGenerateRest( Enums.CodeGenerateRestEndpoint.ReadOnly, DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "7574A473-3326-4973-8DF6-C7BF5F64EB36" )]
    public partial class LavaShortcode : Model<LavaShortcode>, ICacheable
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the public name of the shortcode.
        /// </summary>
        /// <value>
        /// The public name of the shortcode.
        /// </value>
        [Required]
        [MaxLength( 50 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Description of the Lava Shortcode.
        /// </summary>
        /// <value>
        /// The description of the shortcode. This is used as a public description.
        /// </value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the documentation. This serves as the technical description of the internals of the shortcode.
        /// </summary>
        /// <value>
        /// The technical description that serves as documentation.
        /// </value>
        [DataMember]
        public string Documentation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is system.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is system; otherwise, <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        /// <value>
        /// The name of the tag.
        /// </value>
        [Required]
        [MaxLength( 50 )]
        [DataMember( IsRequired = true )]
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public string Markup { get; set; }

        /// <summary>
        /// Gets or sets the type of the tag (inline or block). A tag type of block requires an end tag.
        /// </summary>
        /// <value>
        /// The type of the tag.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public TagType TagType { get; set; }

        /// <summary>
        /// Gets or sets a comma-delimited list of enabled LavaCommands
        /// </summary>
        /// <value>
        /// The enabled lava commands.
        /// </value>
        [MaxLength( 500 )]
        public string EnabledLavaCommands { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        [MaxLength( 2500 )]
        public string Parameters { get; set; }
        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the collection of <see cref="Rock.Model.Category">Categories</see> that this <see cref="LavaShortcode"/> is associated with.
        /// NOTE: Since changes to Categories isn't tracked by ChangeTracker, set the ModifiedDateTime if Categories are modified.
        /// </summary>
        /// <value>
        /// A collection of <see cref="Rock.Model.Category">Categories</see> that this <see cref="LavaShortcode"/> is associated with.
        /// </value>
        [DataMember]
        public virtual ICollection<Category> Categories
        {
            get { return _categories ?? ( _categories = new Collection<Category>() ); }
            set { _categories = value; }
        }

        private ICollection<Category> _categories;

        #endregion Navigation Properties
    }

    #region Entity Configuration

    /// <summary>
    /// Lava Shortcode Configuration class.
    /// </summary>
    public partial class LavaShortcodeConfiguration : EntityTypeConfiguration<LavaShortcode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LavaShortcodeConfiguration"/> class.
        /// </summary>
        public LavaShortcodeConfiguration()
        {
            this.HasMany( a => a.Categories )
                .WithMany()
                .Map( a =>
                {
                    a.MapLeftKey( "LavaShortcodeId" );
                    a.MapRightKey( "CategoryId" );
                    a.ToTable( "LavaShortcodeCategory" );
                } );
        }
    }

    #endregion Entity Configuration
}