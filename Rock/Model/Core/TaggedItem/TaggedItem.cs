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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents an entity object that belongs to a Tag. The same entity object can belong to multiple tags.
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "TaggedItem" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "AB97403A-206E-4E0C-AC42-856A010FA6DD")]
    public partial class TaggedItem : Model<TaggedItem>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this TaggedItem is part of the Rock core system/framework.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> that is <c>true</c> if this TaggedItem is part of the Rock core system/framework; otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the TagId of the <see cref="Rock.Model.Tag"/> that this TaggedItem is tagged with.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the TagId of the <see cref="Rock.Model.Tag"/> that this TaggedItem is tagged with.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int TagId { get; set; }

        /// <summary>
        /// Gets or sets the entity type identifier.
        /// </summary>
        /// <value>
        /// The entity type identifier.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the GUID identifier of the tagged entity.
        /// </summary>
        /// <value>
        /// A <see cref="System.Guid"/> representing the GUID identifier of the tagged entity.
        /// </value>
        [DataMember]
        public Guid EntityGuid { get; set; }

        /// <summary>
        /// Gets or sets the quantity.  Used if tagging the same entity multiple times is supported.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        [DataMember]
        public int Quantity { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the Tag that this TaggedItem belongs to.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.Tag"/> that this TaggedItem belongs to.
        /// </value>
        [DataMember]
        public virtual Tag Tag { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EntityType"/> of this item.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EntityType"/> of this item.
        /// </value>
        [DataMember]
        public virtual Model.EntityType EntityType { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this TagItem.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this TagItem.
        /// </returns>
        public override string ToString()
        {
            return this.Tag.ToStringSafe();
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// <see cref="TaggedItem"/> Configuration class.
    /// </summary>
    public partial class TaggedItemConfiguration : EntityTypeConfiguration<TaggedItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValueConfiguration"/> class.
        /// </summary>
        public TaggedItemConfiguration()
        {
            this.HasRequired( p => p.Tag ).WithMany( p => p.TaggedItems ).HasForeignKey( p => p.TagId ).WillCascadeOnDelete(true);
            this.HasRequired( p => p.EntityType ).WithMany().HasForeignKey( p => p.EntityTypeId ).WillCascadeOnDelete( false );
        }
    }

    #endregion

}
