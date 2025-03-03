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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Lava;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents a Type of <see cref="Rock.Model.ContentChannelType"/>.
    /// </summary>
    [RockDomain( "CMS" )]
    [Table( "ContentChannelType" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "D5992F79-7FB8-49FF-82AB-E8CB2CEC1E74")]
    public partial class ContentChannelType : Model<ContentChannelType>, ICacheable
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this ContentType is part of the Rock core system/framework. 
        /// </summary>
        /// <value>
        ///   A <see cref="System.Boolean"/> flag that is <c>true</c> if this ContentChannelType is part of the Rock core system/framework; otherwise <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the name of the ContentType. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String" /> representing the name of the ContentType.
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="ContentChannelDateType"/> enumeration that represents the type of date range that this DateRangeTypeEnum supports.
        /// </summary>
        /// <value>
        /// A <see cref="Rock.Model.ContentChannelDateType"/> that represents the type of DateRangeTypeEnum is supported. When <c>DateRangeTypeEnum.SingleDate</c> a single date 
        /// will be supported; when <c>DateRangeTypeEnum.DateRange</c> a date range will be supported.
        /// </value>
        [DataMember]
        public ContentChannelDateType DateRangeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time should be included with the single or date range values
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include time]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IncludeTime { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [disable priority].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable priority]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool DisablePriority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [disable content field].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable content field]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool DisableContentField { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [disable status].
        /// If this is set to True, all of the ContentChannelItems are "Approved"
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable status]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool DisableStatus { get; set; }

        /// <summary>
        /// A flag indicating if a <see cref="Rock.Model.ContentChannel"/> of this ContentChannelType will be shown in the content channel list.
        /// When false, it means any 'Channel Types Include' settings MUST specifically include in order to show it.
        /// </summary>
        /// <value>
        ///   A <see cref="System.Boolean"/> value that is <c>true</c> if a <see cref="Rock.Model.Group"/> of this Content Channel Type will be shown in the Channel list; otherwise <c>false</c>.
        /// </value>
        [DataMember]
        public bool ShowInChannelList { get; set; } = true;

        #endregion Entity Properties

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.ContentChannel">channels</see>.
        /// </summary>
        /// <value>
        /// The channels.
        /// </value>
        [LavaVisible]
        public virtual ICollection<ContentChannel> Channels { get; set; }

        /// <summary>
        /// Provides a <see cref="Dictionary{TKey, TValue}"/> of actions that this model supports, and the description of each.
        /// </summary>
        [NotMapped]
        public override Dictionary<string, string> SupportedActions
        {
            get
            {
                var supportedActions = base.SupportedActions;
                supportedActions.AddOrReplace( Rock.Security.Authorization.APPROVE, "The roles and/or users that have access to approve." );
                supportedActions.AddOrReplace( Rock.Security.Authorization.INTERACT, "The roles and/or users that have access to interact with the channel item." );
                return supportedActions;
            }
        }

        #endregion Navigation Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentChannelType"/> class.
        /// </summary>
        public ContentChannelType()
        {
            Channels = new Collection<ContentChannel>();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion Methods
    }

    #region Entity Configuration

    /// <summary>
    /// 
    /// </summary>
    public partial class ContentTypeConfiguration : EntityTypeConfiguration<ContentChannelType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeConfiguration" /> class.
        /// </summary>
        public ContentTypeConfiguration()
        {
        }
    }

    #endregion Entity Configuration
}