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
    /// Represents a EventItemAudience.
    /// </summary>
    [RockDomain( "Event" )]
    [Table( "EventItemAudience" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "22394315-E21B-40AF-AFDA-75D9F5DAD721")]
    public partial class EventItemAudience : Model<EventItemAudience>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.EventItem"/> that this EventItemAudience is associated with. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EventItem"/> that the EventItemAudience is associated with.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int EventItemId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.DefinedValue"/> that this EventItemAudience is associated with. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.DefinedValue"/> that the EventItemAudience is associated with.
        /// </value>
        [Required]
        [DataMember]
        public int DefinedValueId { get; set; }

        #endregion
        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventItem"/> that this EventItemAudience is associated with.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EventItem"/> that this EventItemAudience is associated with.
        /// </value>
        [DataMember]
        public virtual EventItem EventItem { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.DefinedValue"/> that this EventItemAudience is associated with.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.DefinedValue"/> that this EventItemAudience is associated with.
        /// </value>
        [DataMember]
        public virtual DefinedValue DefinedValue { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// EventItemAudience Configuration class.
    /// </summary>
    public partial class EventItemAudienceConfiguration : EntityTypeConfiguration<EventItemAudience>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventItemAudienceConfiguration" /> class.
        /// </summary>
        public EventItemAudienceConfiguration()
        {
            this.HasRequired( p => p.EventItem ).WithMany( p => p.EventItemAudiences ).HasForeignKey( p => p.EventItemId ).WillCascadeOnDelete( true );
            this.HasRequired( p => p.DefinedValue ).WithMany().HasForeignKey( p => p.DefinedValueId ).WillCascadeOnDelete( false );
        }
    }

    #endregion
}