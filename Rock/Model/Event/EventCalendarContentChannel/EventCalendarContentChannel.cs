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

namespace Rock.Model
{
    /// <summary>
    /// Represents an event calendar content channel.
    /// </summary>
    [RockDomain( "Event" )]
    [Table( "EventCalendarContentChannel" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "B8631058-DAC3-4164-9A50-9E732B0C3882")]
    public partial class EventCalendarContentChannel : Model<EventCalendarContentChannel>, ISecured
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarContentChannel belongs to. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarContentChannel is a member of.
        /// </value>
        [Required]
        [HideFromReporting]
        [DataMember( IsRequired = true )]
        public int EventCalendarId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.ContentChannel"/>. This property is required.
        /// </summary>
        /// <value>
        /// An <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EventItem"/> that this EventCalendarContentChannel is a member of.
        /// </value>
        [Required]
        [HideFromReporting]
        [DataMember( IsRequired = true )]
        public int ContentChannelId { get; set; }

        #endregion
        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventCalendar"/> that this EventCalendarContentChannel is a member of.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EventCalendar"/> that this EventCalendarContentChannel is a member of.
        /// </value>
        [DataMember]
        public virtual EventCalendar EventCalendar { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ContentChannel"/> that this EventCalendarContentChannel is a member of.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EventItem"/> that this EventCalendarContentChannel is a member of.
        /// </value>
        [DataMember]
        public virtual ContentChannel ContentChannel { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// EventCalendarItem Configuration class.
    /// </summary>
    public partial class EventCalendarContentChannelConfiguration : EntityTypeConfiguration<EventCalendarContentChannel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCalendarItemConfiguration" /> class.
        /// </summary>
        public EventCalendarContentChannelConfiguration()
        {
            this.HasRequired( p => p.EventCalendar ).WithMany( c => c.ContentChannels ).HasForeignKey( p => p.EventCalendarId ).WillCascadeOnDelete( true );
            this.HasRequired( p => p.ContentChannel ).WithMany().HasForeignKey( p => p.ContentChannelId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}