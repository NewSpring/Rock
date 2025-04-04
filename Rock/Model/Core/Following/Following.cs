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
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents an instance where a <see cref="Rock.Model.Person"/> followed an instance of an entity
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "Following" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "D9AD7A30-92F2-467B-A3F9-37CA246F90BD")]
    public partial class Following : Model<Following>
    {
        #region Properties

        /// <summary>
        /// Gets or sets the entity type identifier.
        /// </summary>
        /// <value>
        /// The entity type identifier.
        /// </value>
        [DataMember]
        public int EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>
        /// The entity identifier.
        /// </value>
        [DataMember]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the PersonAliasId of the person that is following the Entity
        /// </summary>
        /// <value>
        /// The person alias identifier.
        /// </value>
        [DataMember]
        public int PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the purpose key.
        /// </summary>
        /// <value>
        /// The purpose key.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        public string PurposeKey { get; set; }

        #endregion Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        /// <value>
        /// The type of the entity.
        /// </value>
        [DataMember]
        public virtual EntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets the person alias.
        /// </summary>
        /// <value>
        /// The person alias.
        /// </value>
        [DataMember]
        public virtual PersonAlias PersonAlias { get; set; }

        #endregion Navigation Properties
    }

    #region Entity Configuration

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class FollowingConfiguration : EntityTypeConfiguration<Following>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FollowingConfiguration"/> class.
        /// </summary>
        public FollowingConfiguration()
        {
            this.HasRequired( f => f.EntityType ).WithMany().HasForeignKey( f => f.EntityTypeId ).WillCascadeOnDelete( true );
            this.HasRequired( f => f.PersonAlias ).WithMany().HasForeignKey( f => f.PersonAliasId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}