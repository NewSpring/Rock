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

using Newtonsoft.Json;

using Rock.Data;
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// 
    /// </summary>
    [RockDomain( "Event" )]
    [Table( "RegistrationTemplateDiscount" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "88D94ECB-FCEE-4A00-ACB9-FF90BDBA7A17")]
    public partial class RegistrationTemplateDiscount : Model<RegistrationTemplateDiscount>, IOrdered
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.RegistrationTemplate"/> identifier.
        /// </summary>
        /// <value>
        /// The registration template identifier.
        /// </value>
        [DataMember]
        public int RegistrationTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the discount percentage.
        /// </summary>
        /// <value>
        /// The discount percentage.
        /// </value>
        [DataMember]
        public decimal DiscountPercentage { get; set; }

        /// <summary>
        /// Gets or sets the discount amount.
        /// </summary>
        /// <value>
        /// The discount amount.
        /// </value>
        [DataMember]
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        [DataMember]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of registrations that can use this discount code.
        /// </summary>
        /// <value>
        /// The maximum usage.
        /// </value>
        [DataMember]
        public int? MaxUsage { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of registrants per registration that the discount code can used for.
        /// </summary>
        /// <value>
        /// The maximum registrants.
        /// </value>
        [DataMember]
        public int? MaxRegistrants { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of registrants a registration is required to have in order to be able to use this discount code.
        /// </summary>
        /// <value>
        /// The minimum registrants.
        /// </value>
        [DataMember]
        public int? MinRegistrants { get; set; }

        /// <summary>
        /// Gets or sets the first day that the discount code can be used.
        /// </summary>
        /// <value>
        /// The start date.
        /// </value>
        [DataMember]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the last day that the discount code can be used
        /// </summary>
        /// <value>
        /// The end date.
        /// </value>
        [DataMember]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the discount applies automatically.
        /// </summary>
        /// <value>
        /// <c>true</c> if this discount applies automatically; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool AutoApplyDiscount { get; set; }

        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.RegistrationTemplate"/>.
        /// </summary>
        /// <value>
        /// The registration template.
        /// </value>
        [LavaVisible]
        public virtual RegistrationTemplate RegistrationTemplate { get; set; }

        #endregion Navigation Properties

        #region Methods

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Code;
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Configuration class.
    /// </summary>
    public partial class RegistrationTemplateDiscountConfiguration : EntityTypeConfiguration<RegistrationTemplateDiscount>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationTemplateDiscountConfiguration"/> class.
        /// </summary>
        public RegistrationTemplateDiscountConfiguration()
        {
            this.HasRequired( d => d.RegistrationTemplate ).WithMany( t => t.Discounts ).HasForeignKey( d => d.RegistrationTemplateId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}
