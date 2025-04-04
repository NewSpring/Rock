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

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// Represents the linkage between event campus, registration instance, and group.
    /// </summary>
    [RockDomain( "Event" )]
    [Table( "EventItemOccurrenceChannelItem" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "378A9559-BD86-45A8-B218-2C5D4CF3D770")]
    public partial class EventItemOccurrenceChannelItem : Model<EventItemOccurrenceChannelItem>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventItemOccurrence" /> identifier.
        /// </summary>
        /// <value>
        /// The event item occurrence identifier.
        /// </value>
        [DataMember]
        public int EventItemOccurrenceId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ContentChannelItem" />.
        /// </summary>
        /// <value>
        /// The content channel item identifier.
        /// </value>
        [DataMember]
        public int ContentChannelItemId { get; set; }

        #endregion
        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EventItemOccurrence" />.
        /// </summary>
        /// <value>
        /// The event item occurrence.
        /// </value>
        [LavaVisible]
        public virtual EventItemOccurrence EventItemOccurrence { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ContentChannelItem" />.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        [DataMember]
        public virtual ContentChannelItem ContentChannelItem { get; set; }

        #endregion
    }
    #region Entity Configuration

    /// <summary>
    /// EventItemOccurrenceGroupMap Configuration class.
    /// </summary>
    public partial class EventItemOccurrenceChannelItemConfiguration : EntityTypeConfiguration<EventItemOccurrenceChannelItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventItemOccurrenceGroupMapConfiguration" /> class.
        /// </summary>
        public EventItemOccurrenceChannelItemConfiguration()
        {
            this.HasRequired( p => p.EventItemOccurrence ).WithMany( e => e.ContentChannelItems ).HasForeignKey( p => p.EventItemOccurrenceId ).WillCascadeOnDelete( true );
            this.HasRequired( p => p.ContentChannelItem ).WithMany( i => i.EventItemOccurrences ).HasForeignKey( p => p.ContentChannelItemId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}