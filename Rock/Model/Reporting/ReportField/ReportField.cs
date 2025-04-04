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
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// 
    /// </summary>
    [RockDomain( "Reporting" )]
    [NotAudited]
    [Table( "ReportField" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "6B541BAA-44B7-48BA-937A-543866905689")]
    public partial class ReportField : Model<ReportField>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the report identifier.
        /// </summary>
        /// <value>
        /// The report identifier.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int ReportId { get; set; }

        /// <summary>
        /// Gets or sets the type of the report field.
        /// </summary>
        /// <value>
        /// The type of the report field.
        /// </value>
        [Required]
        [DataMember]
        public ReportFieldType ReportFieldType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show in grid].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [show in grid]; otherwise, <c>false</c>.
        /// </value>
        [Required]
        [DataMember]
        public bool ShowInGrid { get; set; }

        /// <summary>
        /// Gets or sets the EntityTypeId of the <see cref="Rock.Reporting.DataSelectComponent" /> that this report field is using.
        /// </summary>
        /// <value>
        /// The DataSelectComponent EntityTypeId
        /// </value>
        [DataMember]
        public int? DataSelectComponentEntityTypeId { get; set; }

        /// <summary>
        /// Selection is where the FieldType stores specific parameter values 
        /// If ReportFieldType is Column or Attribute, it is the Column or Attribute name
        /// If ReportFieldType is DataSelectComponent, it will be some values of whatever the DataSelectComponent implements for specific parameters
        /// </summary>
        /// <value>
        /// The selection.
        /// </value>
        [DataMember]
        public string Selection { get; set; }

        /// <summary>
        /// Gets or sets the column order of this field
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        [DataMember]
        public int ColumnOrder { get; set; }

        /// <summary>
        /// Gets or sets the sort order (ORDER BY). NULL means don't sort by this field
        /// </summary>
        /// <value>
        /// The sort order.
        /// </value>
        [DataMember]
        public int? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        /// <value>
        /// The sort direction.
        /// </value>
        [DataMember]
        public System.Web.UI.WebControls.SortDirection SortDirection { get; set; }

        /// <summary>
        /// Gets or sets the column header text.
        /// </summary>
        /// <value>
        /// The column header text.
        /// </value>
        [DataMember]
        public string ColumnHeaderText { get; set; }

        /// <summary>
        /// Gets or sets the is recipient field.
        /// </summary>
        /// <value>
        /// The is recipient field.
        /// </value>
        [DataMember]
        public bool? IsCommunicationRecipientField { get; set; }

        /// <summary>
        /// Gets or sets the is communication merge field.
        /// </summary>
        /// <value>
        /// The is communication merge field.
        /// </value>
        [DataMember]
        public bool? IsCommunicationMergeField { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the report.
        /// </summary>
        /// <value>
        /// The report.
        /// </value>
        [LavaVisible]
        public virtual Report Report { get; set; }

        /// <summary>
        /// Gets or sets the EntityType of the <see cref="Rock.Reporting.DataSelectComponent" /> that this report field is using.
        /// </summary>
        /// <value>
        /// The DataSelectComponent EntityType
        /// </value>
        [DataMember]
        public virtual EntityType DataSelectComponentEntityType { get; set; }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// 
    /// </summary>
    public partial class ReportFieldConfiguration : EntityTypeConfiguration<ReportField>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportFieldConfiguration"/> class.
        /// </summary>
        public ReportFieldConfiguration()
        {
            this.HasRequired( p => p.Report ).WithMany( p => p.ReportFields ).HasForeignKey( p => p.ReportId ).WillCascadeOnDelete( true );
            this.HasOptional( p => p.DataSelectComponentEntityType ).WithMany().HasForeignKey( p => p.DataSelectComponentEntityTypeId ).WillCascadeOnDelete( true );
        }
    }

    #endregion
}
