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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents a step in Rock.
    /// </summary>
    [RockDomain( "Engagement" )]
    [Table( "Step" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.STEP )]
    public partial class Step : Model<Step>, IOrdered
    {
        /* Custom Indexes:
         *
         * PersonAliasId, StepTypeId
         *      Includes CompletedDateTime
         *      This was added for Step Program Achievement
         *      
         *  StepTypeId, PersonAliasId
         *      Includes CompletedDateTime
         *      This was added for Step Program Achievement
         */

        #region Entity Properties

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.StepType"/> to which this step belongs. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        [EnableAttributeQualification]
        public int StepTypeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.StepStatus"/> to which this step belongs.
        /// </summary>
        [DataMember]
        public int? StepStatusId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.PersonAlias"/> that identifies the Person associated with taking this step. This property is required.
        /// </summary>
        [Required]
        [DataMember( IsRequired = true )]
        public int PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.Campus"/> associated with this step.
        /// </summary>
        [DataMember]
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> associated with the completion of this step.
        /// </summary>
        [DataMember]
        public DateTime? CompletedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> associated with the start of this step.
        /// </summary>
        [DataMember]
        public DateTime? StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> associated with the end of this step.
        /// </summary>
        [DataMember]
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [DataMember]
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.StepProgramCompletion"/> to which this step belongs.
        /// </summary>
        [DataMember]
        public int? StepProgramCompletionId { get; set; }

        /// <summary>
        /// Gets the start date key.
        /// </summary>
        /// <value>
        /// The start date key.
        /// </value>
        [DataMember]
        [FieldType( Rock.SystemGuid.FieldType.DATE )]
        public int? StartDateKey
        {
            get => ( StartDateTime == null || StartDateTime.Value == default ) ?
                        ( int? ) null :
                        StartDateTime.Value.ToString( "yyyyMMdd" ).AsInteger();

            private set { }
        }

        /// <summary>
        /// Gets the end date key.
        /// </summary>
        /// <value>
        /// The end date key.
        /// </value>
        [DataMember]
        [FieldType( Rock.SystemGuid.FieldType.DATE )]
        public int? EndDateKey
        {
            get => ( EndDateTime == null || EndDateTime.Value == default ) ?
                        ( int? ) null :
                        EndDateTime.Value.ToString( "yyyyMMdd" ).AsInteger();

            private set { }
        }

        /// <summary>
        /// Gets the completed date key.
        /// </summary>
        /// <value>
        /// The completed date key.
        /// </value>
        [DataMember]
        [FieldType( Rock.SystemGuid.FieldType.DATE )]
        public int? CompletedDateKey
        {
            get => ( CompletedDateTime == null || CompletedDateTime.Value == default ) ?
                        ( int? ) null :
                        CompletedDateTime.Value.ToString( "yyyyMMdd" ).AsInteger();

            private set { }
        }

        #endregion

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
        /// Gets or sets the <see cref="Rock.Model.StepStatus"/>.
        /// </summary>
        [DataMember]
        public virtual StepStatus StepStatus { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.PersonAlias"/>.
        /// </summary>
        [DataMember]
        public virtual PersonAlias PersonAlias { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.Campus"/>.
        /// </summary>
        [DataMember]
        public virtual Campus Campus { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.StepProgramCompletion"/>.
        /// </summary>
        [DataMember]
        public virtual StepProgramCompletion StepProgramCompletion { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the <see cref="Rock.Model.StepWorkflow">StepWorkflows</see> that are of this step.
        /// </summary>
        [DataMember]
        public virtual ICollection<StepWorkflow> StepWorkflows
        {
            get => _stepWorkflows ?? ( _stepWorkflows = new Collection<StepWorkflow>() );
            set => _stepWorkflows = value;
        }

        private ICollection<StepWorkflow> _stepWorkflows;

        /// <summary>
        /// Gets or sets the start source date.
        /// </summary>
        /// <value>
        /// The start source date.
        /// </value>
        [DataMember]
        public virtual AnalyticsSourceDate StartSourceDate { get; set; }

        /// <summary>
        /// Gets or sets the end source date.
        /// </summary>
        /// <value>
        /// The end source date.
        /// </value>
        [DataMember]
        public virtual AnalyticsSourceDate EndSourceDate { get; set; }

        /// <summary>
        /// Gets or sets the completed source date.
        /// </summary>
        /// <value>
        /// The completed source date.
        /// </value>
        [DataMember]
        public virtual AnalyticsSourceDate CompletedSourceDate { get; set; }

        #endregion Navigation Properties

        #region Entity Configuration

        /// <summary>
        /// Step Configuration class.
        /// </summary>
        public partial class StepConfiguration : EntityTypeConfiguration<Step>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StepConfiguration"/> class.
            /// </summary>
            public StepConfiguration()
            {
                HasRequired( s => s.StepType ).WithMany( st => st.Steps ).HasForeignKey( s => s.StepTypeId ).WillCascadeOnDelete( true );
                HasRequired( s => s.PersonAlias ).WithMany().HasForeignKey( s => s.PersonAliasId ).WillCascadeOnDelete( true );

                HasOptional( s => s.Campus ).WithMany().HasForeignKey( s => s.CampusId ).WillCascadeOnDelete( false );
                HasOptional( s => s.StepStatus ).WithMany( ss => ss.Steps ).HasForeignKey( s => s.StepStatusId ).WillCascadeOnDelete( false );
                HasOptional( s => s.StepProgramCompletion ).WithMany( ss => ss.Steps ).HasForeignKey( s => s.StepProgramCompletionId ).WillCascadeOnDelete( true );

                // NOTE: When creating a migration for this, don't create the actual FK's in the database for this just in case there are outlier OccurrenceDates that aren't in the AnalyticsSourceDate table
                // and so that the AnalyticsSourceDate can be rebuilt from scratch as needed
                this.HasOptional( r => r.StartSourceDate ).WithMany().HasForeignKey( r => r.StartDateKey ).WillCascadeOnDelete( false );
                this.HasOptional( r => r.EndSourceDate ).WithMany().HasForeignKey( r => r.EndDateKey ).WillCascadeOnDelete( false );
                this.HasOptional( r => r.CompletedSourceDate ).WithMany().HasForeignKey( r => r.CompletedDateKey ).WillCascadeOnDelete( false );
            }
        }

        #endregion Entity Configuration
    }
}