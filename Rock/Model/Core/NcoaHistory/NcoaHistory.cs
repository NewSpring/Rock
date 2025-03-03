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

namespace Rock.Model
{
    /// <summary>
    /// Represents a NCOA History.
    /// </summary>
    [RockDomain( "Core" )]
    [NotAudited]
    [Table( "NcoaHistory" )]
    [DataContract]
    [CodeGenerateRest( Enums.CodeGenerateRestEndpoint.ReadOnly, DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( "1F20AC90-C57E-4DD1-A71B-06312110E56F")]
    public partial class NcoaHistory : Model<NcoaHistory>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the person alias identifier.
        /// </summary>
        /// <value>
        /// The person alias identifier.
        /// </value>
        [DataMember]
        public int PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the family group identifier.
        /// </summary>
        /// <value>
        /// The family group identifier.
        /// </value>
        [DataMember]
        public int FamilyId { get; set; }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        /// <value>
        /// The location identifier.
        /// </value>
        [DataMember]
        public int? LocationId { get; set; }

        /// <summary>
        /// Gets or sets the move type value identifier.
        /// </summary>
        /// <value>
        /// The move type value identifier.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public MoveType MoveType { get; set; }

        /// <summary>
        /// Gets or sets the NCOA type.
        /// </summary>
        /// <value>
        /// The NCOA type.
        /// </value>
        [DataMember]
        public NcoaType NcoaType { get; set; }

        /// <summary>
        /// Gets or sets the address status.
        /// </summary>
        /// <value>
        /// The address status.
        /// </value>
        [DataMember]
        public AddressStatus AddressStatus { get; set; }

        /// <summary>
        /// Gets or sets the address invalid reason.
        /// </summary>
        /// <value>
        /// The address invalid reason.
        /// </value>
        [DataMember]
        public AddressInvalidReason AddressInvalidReason { get; set; }

        /// <summary>
        /// Gets or sets the Original street 1.
        /// </summary>
        /// <value>
        /// The original street 1.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        public string OriginalStreet1 { get; set; }

        /// <summary>
        /// Gets or sets the Original street 2.
        /// </summary>
        /// <value>
        /// The original street 2.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        public string OriginalStreet2 { get; set; }

        /// <summary>
        /// Gets or sets the Original city.
        /// </summary>
        /// <value>
        /// The original city.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string OriginalCity { get; set; }

        /// <summary>
        /// Gets or sets the Original state.
        /// </summary>
        /// <value>
        /// The original state.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string OriginalState { get; set; }

        /// <summary>
        /// Gets or sets the Original postal code.
        /// </summary>
        /// <value>
        /// The original postal code.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string OriginalPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the Updated street 1.
        /// </summary>
        /// <value>
        /// The updated street 1.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        public string UpdatedStreet1 { get; set; }

        /// <summary>
        /// Gets or sets the Updated street 2.
        /// </summary>
        /// <value>
        /// The updated street 2.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        public string UpdatedStreet2 { get; set; }

        /// <summary>
        /// Gets or sets the Updated city.
        /// </summary>
        /// <value>
        /// The updated city.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string UpdatedCity { get; set; }

        /// <summary>
        /// Gets or sets the Updated state.
        /// </summary>
        /// <value>
        /// The updated state.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string UpdatedState { get; set; }

        /// <summary>
        /// Gets or sets the Updated postal code.
        /// </summary>
        /// <value>
        /// The updated postal code.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string UpdatedPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the Updated country.
        /// </summary>
        /// <value>
        /// The updated country.
        /// </value>
        [DataMember]
        [MaxLength( 50 )]
        public string UpdatedCountry { get; set; }

        /// <summary>
        /// Gets or sets the Updated barcode.
        /// </summary>
        /// <value>
        /// The updated barcode.
        /// </value>
        [DataMember]
        [MaxLength( 40 )]
        public string UpdatedBarcode { get; set; }

        /// <summary>
        /// Gets or sets the Updated address type.
        /// </summary>
        /// <value>
        /// The updated address type.
        /// </value>
        [DataMember]
        public UpdatedAddressType UpdatedAddressType { get; set; }

        /// <summary>
        /// Gets or sets the date when moved.
        /// </summary>
        /// <value>
        /// The move date.
        /// </value>
        [DataMember]
        public DateTime? MoveDate { get; set; }

        /// <summary>
        /// Gets or sets the moving distance.
        /// </summary>
        /// <value>
        /// The moving distance.
        /// </value>
        [DataMember]
        public decimal? MoveDistance { get; set; }

        /// <summary>
        /// Gets or sets the match flag.
        /// </summary>
        /// <value>
        /// The match flag.
        /// </value>
        [DataMember]
        public MatchFlag MatchFlag { get; set; }

        /// <summary>
        /// Gets or sets the processed.
        /// </summary>
        /// <value>
        /// The processed.
        /// </value>
        [DataMember]
        public Processed Processed { get; set; }

        /// <summary>
        /// Gets or sets the date and time for NCOA Run.
        /// </summary>
        /// <value>
        /// The date and time for NCOA Run.
        /// </value>
        [DataMember]
        public DateTime NcoaRunDateTime { get; set; }

        /// <summary>
        /// Gets or sets the note for NCOA.
        /// </summary>
        /// <value>
        /// The note for NCOA.
        /// </value>
        [DataMember]
        public string NcoaNote { get; set; }

        #endregion

        #region Navigation Properties

        // removed this to eliminate a hard link between the NCOA and the person alias, since this data leaves the system and comes
        // back at a later date it's best not to rely on the person alias being there

        /*/// <summary>
        /// Gets or sets the person alias.
        /// </summary>
        /// <value>
        /// The person alias.
        /// </value>
        [LavaInclude]
        public virtual PersonAlias PersonAlias { get; set; }*/

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Entity Change Configuration class.
    /// </summary>
    public partial class NcoaHistoryConfiguration : EntityTypeConfiguration<NcoaHistory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTypeConfiguration" /> class.
        /// </summary>
        public NcoaHistoryConfiguration()
        {
            // removed this to eliminate a hard link between the NCOA and the person alias, since this data leaves the system and comes
            // back at a later date it's best not to rely on the person alias being there
            // this.HasRequired( p => p.PersonAlias ).WithMany().HasForeignKey( p => p.PersonAliasId ).WillCascadeOnDelete( false );
            this.Property( p => p.MoveDistance ).HasPrecision( 6, 2 );
        }
    }

    #endregion
}
