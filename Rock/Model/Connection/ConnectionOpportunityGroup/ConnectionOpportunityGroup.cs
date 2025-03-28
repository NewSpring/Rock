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
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// Represents a connection opportunity group
    /// </summary>
    [RockDomain( "Engagement" )]
    [Table( "ConnectionOpportunityGroup" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY_GROUP )]
    public partial class ConnectionOpportunityGroup : Model<ConnectionOpportunityGroup>
    {

        #region Entity Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ConnectionOpportunity"/> identifier.
        /// </summary>
        /// <value>
        /// The connection opportunity identifier.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int ConnectionOpportunityId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Group"/> identifier.
        /// </summary>
        /// <value>
        /// The group identifier.
        /// </value>
        [Required]
        [DataMember]
        public int GroupId { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ConnectionOpportunity"/>.
        /// </summary>
        /// <value>
        /// The connection opportunity.
        /// </value>
        [LavaVisible]
        public virtual ConnectionOpportunity ConnectionOpportunity { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Group"/>.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        [DataMember]
        public virtual Group Group { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// ConnectionOpportunityGroup Configuration class.
    /// </summary>
    public partial class ConnectionOpportunityGroupConfiguration : EntityTypeConfiguration<ConnectionOpportunityGroup>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionOpportunityGroupConfiguration" /> class.
        /// </summary>
        public ConnectionOpportunityGroupConfiguration()
        {
            this.HasRequired( p => p.ConnectionOpportunity ).WithMany( p => p.ConnectionOpportunityGroups ).HasForeignKey( p => p.ConnectionOpportunityId ).WillCascadeOnDelete( true );
            this.HasRequired( p => p.Group ).WithMany().HasForeignKey( p => p.GroupId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}