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
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents an Achievement Attempts in Rock.
    /// </summary>
    [RockDomain( "Engagement" )]
    [Table( "AchievementAttempt" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.ACHIEVEMENT_ATTEMPT )]
    public partial class AchievementAttempt : Model<AchievementAttempt>
    {
        /* Custom Indexes:
         *
         * AchievementTypeId, AchieverEntityId
         *      This was added for Achievement components process methods
         */

        #region Entity Properties

        /// <summary>
        /// Gets or sets the achiever entity identifier. The type of AchieverEntity is determined by <see cref="AchievementType.AchieverEntityTypeId" />.
        /// NOTE: In the case of a Person achievement, this could either by PersonAliasId or PersonId (but probably PersonAliasId)
        /// depending on <seealso cref="AchievementType.AchievementEntityType"/>
        /// </summary>
        [DataMember( IsRequired = true )]
        [Required]
        public int AchieverEntityId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Model.AchievementType"/> to which this attempt belongs. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int AchievementTypeId { get; set; }

        /// <summary>
        /// Gets or sets the progress. This is a percentage so .25 is 25% and 1 is 100%.
        /// </summary>
        /// <value>
        /// The progress.
        /// </value>
        [DataMember]
        [DecimalPrecision( 18, 9 )]
        public decimal Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this attempt is closed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsClosed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this attempt was a success.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the achievement attempt start date time.
        /// </summary>
        /// <value>
        /// The achievement attempt start date time.
        /// </value>
        [DataMember( IsRequired = true )]
        [Required]
        public DateTime AchievementAttemptStartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the achievement attempt end date time.
        /// </summary>
        /// <value>
        /// The achievement attempt start date time.
        /// </value>
        [DataMember]
        public DateTime? AchievementAttemptEndDateTime { get; set; }

        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Model.AchievementType"/> of this attempt.
        /// </summary>
        [DataMember]
        public virtual AchievementType AchievementType { get; set; }

        #endregion Navigation Properties

        #region Entity Configuration

        /// <summary>
        /// Achievement Attempt Configuration class.
        /// </summary>
        public partial class AchievementAttemptConfiguration : EntityTypeConfiguration<AchievementAttempt>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AchievementAttemptConfiguration"/> class.
            /// </summary>
            public AchievementAttemptConfiguration()
            {
                HasRequired( aa => aa.AchievementType ).WithMany( s => s.Attempts ).HasForeignKey( aa => aa.AchievementTypeId ).WillCascadeOnDelete( true );
            }
        }

        #endregion Entity Configuration
    }
}
