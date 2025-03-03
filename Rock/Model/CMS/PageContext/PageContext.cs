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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Rock.Data;
using Rock.Lava;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents a PageContext object in Rock.  A PageContext entity is an entity object that can be shared amongst all of the <see cref="Rock.Model.Block">Blocks</see> on a page.
    /// </summary>
    /// <remarks>
    /// A good example of this is a <see cref="Rock.Model.Person"/> that is shared amongst all of the <see cref="Rock.Model.Block">Blocks</see> on the Person Detail Page.
    /// </remarks>
    [RockDomain( "CMS" )]
    [Table( "PageContext" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "5C56B915-8A8B-4747-9D84-EBAF0BACC9A1")]
    public partial class PageContext : Model<PageContext>, ICacheable
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this PageContext is a part of the Rock core system/framework. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> that is <c>true</c> if the PageContext is part of the core system/framework, otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.Page"/> that this PageContext is used on. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> that represents the Id of the <see cref="Rock.Model.Page"/> that this PageContext is used on.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        [Index( "IX_PageId" )]
        [Index( "IX_PageIdEntityIdParameter", 0, IsUnique = true )]
        public int PageId { get; set; }

        /// <summary>
        /// Gets or sets the object type name of the entity object that is being shared through this PageContext. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the object type name of the entity object that is being shared through the PageContext.
        /// </value>
        [Required]
        [MaxLength( 200 )]
        [DataMember( IsRequired = true )]
        [Index( "IX_PageIdEntityIdParameter", 1, IsUnique = true )]
        public string Entity { get; set; }

        /// <summary>
        /// Gets or sets the name of the Page Attribute/Parameter that stores the Id of the shared entity object. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> containing the name of the Page Attribute/Parameter storing the Id of the entity object. 
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        [Index( "IX_PageIdEntityIdParameter", 2, IsUnique = true )]
        public string IdParameter { get; set; }

        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Page"/> that this PageContext is used on.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Page"/> that uses this PageContext.
        /// </value>
        [LavaVisible]
        public virtual Page Page { get; set; }

        #endregion Navigation Properties
    }

    #region Entity Configuration

    /// <summary>
    /// Page Route Configuration class.
    /// </summary>
    public partial class PageContextConfiguration : EntityTypeConfiguration<PageContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageContextConfiguration"/> class.
        /// </summary>
        public PageContextConfiguration()
        {
            this.HasRequired( p => p.Page ).WithMany( p => p.PageContexts ).HasForeignKey( p => p.PageId ).WillCascadeOnDelete( true );
        }
    }

    #endregion Entity Configuration
}
