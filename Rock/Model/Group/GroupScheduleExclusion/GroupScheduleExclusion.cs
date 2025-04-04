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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents A collection of <see cref="Rock.Model.GroupScheduleExclusion"/> entities. This is used to 
    /// specify date ranges that groups occurrences do not occur for groups of a particular group type.
    /// </summary>
    [RockDomain( "Group" )]
    [Table( "GroupScheduleExclusion" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "047D57EE-1B06-455F-86EA-D96B8325C77D")]
    public partial class GroupScheduleExclusion : Model<GroupScheduleExclusion>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.GroupType"/> identifier.
        /// </summary>
        /// <value>
        /// The group type identifier.
        /// </value>
        [DataMember( IsRequired = true )]
        public int GroupTypeId { get; set; }

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>
        /// The start date.
        /// </value>
        [DataMember]
        [Column( TypeName = "Date" )]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>
        /// The end date.
        /// </value>
        [DataMember]
        [Column( TypeName = "Date" )]
        public DateTime? EndDate { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.GroupType"/> that this Group is a member of.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.GroupType"/> that this Group is a member of.
        /// </value>
        [DataMember]
        public virtual GroupType GroupType { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Group Configuration class.
    /// </summary>
    public partial class GroupScheduleExclusionConfiguration : EntityTypeConfiguration<GroupScheduleExclusion>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupConfiguration"/> class.
        /// </summary>
        public GroupScheduleExclusionConfiguration()
        {
            this.HasRequired( p => p.GroupType ).WithMany( g => g.GroupScheduleExclusions).HasForeignKey( p => p.GroupTypeId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}
