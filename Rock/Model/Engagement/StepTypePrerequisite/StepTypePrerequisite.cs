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
    /// Represents a step type in Rock.
    /// </summary>
    [RockDomain( "Engagement" )]
    [Table( "StepTypePrerequisite" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "F2181FCD-1423-4937-9137-099154E1C3EC")]
    public partial class StepTypePrerequisite : Model<StepTypePrerequisite>, IOrdered
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.StepType"/> to which this prerequisite. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int StepTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.StepType"/> that is a prerequisite. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int PrerequisiteStepTypeId { get; set; }

        #endregion Entity Properties

        #region IOrdered

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        [DataMember]
        public int Order { get; set; }

        #endregion IOrdered

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.StepType"/>.
        /// </summary>
        [DataMember]
        public virtual StepType StepType { get; set; }

        /// <summary>
        /// Gets or sets the Prerequisite <see cref="Rock.Model.StepType"/>.
        /// </summary>
        [DataMember]
        public virtual StepType PrerequisiteStepType { get; set; }

        #endregion Navigation Properties

        #region Entity Configuration

        /// <summary>
        /// Step Type Configuration class.
        /// </summary>
        public partial class StepTypePrerequisiteConfiguration : EntityTypeConfiguration<StepTypePrerequisite>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StepTypePrerequisiteConfiguration"/> class.
            /// </summary>
            public StepTypePrerequisiteConfiguration()
            {
                HasRequired( stp => stp.StepType ).WithMany( st => st.StepTypePrerequisites ).HasForeignKey( stp => stp.StepTypeId ).WillCascadeOnDelete( true );

                // This has to be false because otherwise SQL server doesn't like the possibility of dependency cycles
                HasRequired( stp => stp.PrerequisiteStepType ).WithMany( st => st.StepTypeDependencies ).HasForeignKey( stp => stp.PrerequisiteStepTypeId ).WillCascadeOnDelete( false );
            }
        }

        #endregion Entity Configuration
    }
}
