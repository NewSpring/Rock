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
using Rock.Security;
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// Represents an event calendar item.
    /// </summary>
    [RockDomain( "Event" )]
    [Table( "EventCalendarItem" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "E37FB26F-03F6-48DA-8E96-F412616F5EE4")]
    public partial class EventCalendarItem : Model<EventCalendarItem>, ISecured
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarItem belongs to. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarItem is a member of.
        /// </value>
        [Required]
        [HideFromReporting]
        [DataMember( IsRequired = true )]
        [EnableAttributeQualification]
        public int EventCalendarId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.EventItem"/> that this EventCalendarItem belongs to. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EventItem"/> that this EventCalendarItem is a member of.
        /// </value>
        [Required]
        [HideFromReporting]
        [DataMember( IsRequired = true )]
        public int EventItemId { get; set; }

        #endregion
        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarItem is a member of.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EventCalendar"/> that this EventCalendarItem is a member of.
        /// </value>
        [LavaVisible]
        public virtual EventCalendar EventCalendar { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventItem"/> that this EventCalendarItem is a member of.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EventItem"/> that this EventCalendarItem is a member of.
        /// </value>
        [LavaVisible]
        public virtual EventItem EventItem { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// EventCalendarItem Configuration class.
    /// </summary>
    public partial class EventCalendarItemConfiguration : EntityTypeConfiguration<EventCalendarItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCalendarItemConfiguration" /> class.
        /// </summary>
        public EventCalendarItemConfiguration()
        {
            this.HasRequired( p => p.EventCalendar ).WithMany( p => p.EventCalendarItems ).HasForeignKey( p => p.EventCalendarId ).WillCascadeOnDelete( true );
            this.HasRequired( p => p.EventItem ).WithMany( p => p.EventCalendarItems ).HasForeignKey( p => p.EventItemId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}