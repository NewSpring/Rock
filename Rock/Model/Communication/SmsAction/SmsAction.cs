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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents a single action for processing an incoming SMS message.
    /// </summary>
    [RockDomain( "Communication" )]
    [Table( "SmsAction" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "1F5E26BE-0ED4-4250-8FFC-1DED5E9EACF0")]
    public partial class SmsAction : Model<SmsAction>, IOrdered, ICacheable
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        /// <value>
        /// The name of the action.
        /// </value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an interaction should be recorded after this action is processed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if an interaction should be logged; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsInteractionLoggedAfterProcessing { get; set; }

        /// <summary>
        /// Gets or sets the order of this action in the system.
        /// </summary>
        /// <value>
        /// The order of this action in the system.
        /// </value>
        [DataMember]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the entity type that handles this action's logic.
        /// </summary>
        /// <value>
        /// The identifier for the entity type that handles this action's logic.
        /// </value>
        [DataMember]
        [EnableAttributeQualification]
        public int SmsActionComponentEntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the SMS pipeline identifier.
        /// </summary>
        /// <value>
        /// The SMS pipeline identifier.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int SmsPipelineId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether further actions should be processed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if further actions should be processed; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool ContinueAfterProcessing { get; set; }

        /// <summary>
        /// Gets or sets when to expire the <see cref="SmsAction"/>
        /// </summary>
        /// <value>
        /// The expire date.
        /// </value>
        [DataMember]
        public DateTime? ExpireDate { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.SmsPipeline"/> representing the SmsPipeline.
        /// </summary>
        /// <value>
        /// A <see cref="Rock.Model.SmsPipeline"/> representing the SmsPipeline for this SmsAction.
        /// </value>
        [DataMember]
        public virtual Model.SmsPipeline SmsPipeline { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class SmsActionConfiguration : EntityTypeConfiguration<SmsAction>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsActionConfiguration"/> class.
        /// </summary>
        public SmsActionConfiguration()
        {
            this.HasRequired( p => p.SmsPipeline ).WithMany( p => p.SmsActions ).HasForeignKey( p => p.SmsPipelineId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}
