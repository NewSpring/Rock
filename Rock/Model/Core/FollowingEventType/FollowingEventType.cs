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
using Rock.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

namespace Rock.Model
{
    /// <summary>
    /// Represents a following event
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "FollowingEventType" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "8A0D208B-762D-403A-A972-3A0F079866D4")]
    public partial class FollowingEventType : Model<FollowingEventType>, IOrdered, IHasActiveFlag
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the (internal) Name of the FollowingEvent. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the (internal) name of the FollowingEvent.
        /// </value>
        [Required]
        [MaxLength( 50 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user defined description of the FollowingEvent.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the user defined description of the FollowingEvent.
        /// </value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the event MEF component identifier.
        /// </summary>
        /// <value>
        /// The event entity type identifier.
        /// </value>
        [DataMember]
        [EnableAttributeQualification]
        public int? EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the followed entity type identifier.
        /// </summary>
        /// <value>
        /// The followed entity type identifier.
        /// </value>
        [DataMember]
        public int? FollowedEntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        [DataMember]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [send on weekends].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [send on weekends]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool SendOnWeekends { get; set; }

        /// <summary>
        /// Gets or sets the last check.
        /// </summary>
        /// <value>
        /// The last check.
        /// </value>
        [DataMember]
        public DateTime? LastCheckDateTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this event is required. If not, followers will be able to optionally select if they want to be notified of this event
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is notice required; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsNoticeRequired { get; set; }

        /// <summary>
        /// Gets or sets how an entity should be formatted when included in the event notification to follower.
        /// </summary>
        /// <value>
        /// The item notification lava.
        /// </value>
        [DataMember]
        public string EntityNotificationFormatLava { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include non public requests].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include non public requests]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IncludeNonPublicRequests { get; set; }

        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the type of the event entity.
        /// </summary>
        /// <value>
        /// The type of the event entity.
        /// </value>
        [DataMember]
        public virtual EntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets the type of the followed entity.
        /// </summary>
        /// <value>
        /// The type of the followed entity.
        /// </value>
        [DataMember]
        public virtual EntityType FollowedEntityType { get; set; }

        #endregion Navigation Properties

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this FollowingEvent.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this FollowingEvent.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion Public Methods
    }

    #region Entity Configuration

    /// <summary>
    /// FollowingEvent Configuration class.
    /// </summary>
    public partial class FollowingEventConfiguration : EntityTypeConfiguration<FollowingEventType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FollowingEventConfiguration"/> class.
        /// </summary>
        public FollowingEventConfiguration()
        {
            this.HasOptional( g => g.EntityType).WithMany().HasForeignKey( a => a.EntityTypeId).WillCascadeOnDelete( false );
            this.HasOptional( g => g.FollowedEntityType ).WithMany().HasForeignKey( a => a.FollowedEntityTypeId ).WillCascadeOnDelete( false );
        }
    }

    #endregion Entity Configuration
}