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
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents an attribute or configuration setting in Rock
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "Attribute" )]
    [DataContract]
    [CodeGenerateRest( ~( Enums.CodeGenerateRestEndpoint.ReadAttributeValues | Enums.CodeGenerateRestEndpoint.UpdateAttributeValues ) )]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.ATTRIBUTE )]
    public partial class Attribute : Model<Attribute>, IOrdered, ICacheable
    {
        /// <summary>
        /// The Qualifier on null entity types to distinguish a 'System Setting' from a Global Attribute
        /// </summary>
        public const string SYSTEM_SETTING_QUALIFIER = "SystemSetting";

        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this Attribute is part of the Rock core system/framework. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if this Attribute is part of the Rock core system/framework; otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the FieldTypeId of the <see cref="Rock.Model.FieldType"/> that is used to select/set the <see cref="Rock.Model.AttributeValue"/> for this Attribute setting.
        /// The FieldType can also be used to enforce formatting of the attribute setting. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the FieldTyepId of the <see cref="Rock.Model.FieldType"/> that is used to select/set the <see cref="Rock.Model.AttributeValue"/>
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int FieldTypeId { get; set; }

        /// <summary>
        /// Gets or sets the EntityTypeId of the <see cref="Rock.Model.EntityType"/> that this Attribute is used to configure. This property will not be populated if the Attribute is a Global (system) Attribute.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the EntityTypeId of the <see cref="Rock.Model.EntityType"/> that this Attribute is used to configure. This property will be null if the Attribute is a Global (system)
        /// Attribute.
        /// </value>
        [DataMember]
        public int? EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the entity type qualifier column that contains the value (see <see cref="EntityTypeQualifierValue"/>) that is used narrow the scope of the Attribute to a subset or specific instance of an EntityType.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the name of the Qualifier Column/Property that contains the <see cref="EntityTypeQualifierValue"/> that is used to narrow the scope of the Attribute.
        /// </value>
        [MaxLength( 50 )]
        [DataMember]
        public string EntityTypeQualifierColumn { get; set; }

        /// <summary>
        /// Gets or sets the entity type qualifier value that is used to narrow the scope of the Attribute to a subset or specific instance of an EntityType.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents the value that is used to narrow the scope of the Attribute.
        /// </value>
        [MaxLength( 200 )]
        [DataMember]
        public string EntityTypeQualifierValue { get; set; }

        /// <summary>
        /// Gets sets the Key value  that is used to reference and call the Attribute. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents the Key value that is used to reference and call the Attribute.
        /// </value>
        [Required]
        [MaxLength( 1000 )]
        [DataMember( IsRequired = true )]
        [TypeScriptType( "string" )]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the Name of the Attribute. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents the name of the Attribute.
        /// </value>
        [Required]
        [MaxLength( 1000 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the Attribute.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that represents the description of the Attribute.
        /// </value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the display order of the attribute.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the display order of the Attribute.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if this Attribute is a Grid Column?
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if this Attribute is a grid column; otherwise <c>false</c>
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsGridColumn { get; set; }

        /// <summary>
        /// Gets or sets the Attribute's default value.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Attribute's default value.
        /// </value>
        [DataMember]
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets the value checksum. This is a hash of <see cref="DefaultValue"/> that
        /// is automatically calculated by the database.
        /// </summary>
        /// <value>The value checksum.</value>
        [DataMember]
        [DatabaseGenerated( DatabaseGeneratedOption.Computed )]
        [LavaHidden]
        public int DefaultValueChecksum { get; private set; }

        /// <summary>
        /// Gets or sets the default persisted text value.
        /// </summary>
        /// <value>The persisted text value.</value>
        [DataMember]
        public string DefaultPersistedTextValue { get; set; }

        /// <summary>
        /// Gets or sets the default persisted HTML value.
        /// </summary>
        /// <value>The persisted HTML value.</value>
        [DataMember]
        public string DefaultPersistedHtmlValue { get; set; }

        /// <summary>
        /// Gets or sets the default persisted condensed text value.
        /// </summary>
        /// <value>The persisted condensed text value.</value>
        [DataMember]
        public string DefaultPersistedCondensedTextValue { get; set; }

        /// <summary>
        /// Gets or sets the default persisted condensed HTML value.
        /// </summary>
        /// <value>The persisted condensed HTML value.</value>
        [DataMember]
        public string DefaultPersistedCondensedHtmlValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the persisted values are
        /// considered dirty. If the values are dirty then it should be assumed
        /// that they are not in sync with the <see cref="DefaultValue"/> property.
        /// </summary>
        /// <value><c>true</c> if the persisted values are considered dirty; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool IsDefaultPersistedValueDirty { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the Attribute supports multiple values.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> that is <c>true</c> if the attribute supports multiple values; otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsMultiValue { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if a value is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if a value is required, otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the name of the icon CSS class. This property is only used for CSS based icons.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the name of the icon CSS class. This property will be null if a file based icon is being used.
        /// </value>
        [MaxLength( 100 )]
        [DataMember]
        public string IconCssClass { get; set; }

        /// <summary>
        /// The color to visually distinguish the attribute. For example, <see cref="AttributeColor"/> might be used to set the color for the <seealso cref="IconCssClass"/> of the icon.
        /// </summary>
        /// <value>The color of the attribute.</value>
        [MaxLength( 100 )]
        [DataMember]
        public string AttributeColor { get; set; }

        /// <summary>
        /// Gets or sets whether this Attribute should be used in 'search by attribute value' UIs. 
        /// For example, if you had a UI where you would allow the user to find people based on a list of attributes
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allow search]; otherwise, <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool AllowSearch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is index enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is index enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsIndexEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is analytic.
        /// NOTE: Only applies if this is an Attribute on an Entity that implements IAnalytic and has an [AnalyticAttributes] Attribute
        /// If this is true, the Analytic table for this entity should include a field for this attribute
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is analytic; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsAnalytic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is analytic history.
        /// Only applies if this is an Attribute on an Entity that implements IAnalyticHistorical and IsAnalytic is True
        /// If this is true and IsAnalytic is also true, a change in value of this Attribute on the Entity makes the CurrentRowIndicator=1 record
        /// to become CurrentRowIndicator=0, sets the ExpireDate, then a new row with CurrentRowIndicator=1 to be created
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is analytic history; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsAnalyticHistory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this attribute is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether changes to this attribute's attribute values should be logged in AttributeValueHistorical
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable history]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool EnableHistory { get; set; } = false;

        /// <summary>
        /// Gets or sets any HTML to be rendered before the attribute's edit control 
        /// </summary>
        /// <value>
        /// The pre HTML.
        /// </value>
        [DataMember]
        public string PreHtml { get; set; }

        /// <summary>
        /// Gets or sets any HTML to be rendered after the attribute's edit control 
        /// </summary>
        /// <value>
        /// The post HTML.
        /// </value>
        [DataMember]
        public string PostHtml { get; set; }

        /// <summary>
        /// Indicates whether or not this attribute should be displayed in public contexts (e.g., responding to an RSVP without logging in).
        /// </summary>
        /// <value>
        /// A boolean value.
        /// </value>
        [DataMember]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if this attribute shows when doing a bulk entry form.
        /// </summary>
        [DataMember]
        public bool ShowOnBulk { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if changes to the attribute values should be recorded into the generic History log table.
        /// </summary>
        [DataMember]
        public bool IsSuppressHistoryLogging { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EntityType"/> that this Attribute is used to configure. This property will not be populated if the Attribute is a Global (system) Attribute.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EntityType"/> that this Attribute is used to configure. This property will be null if the Attribute is a Global (system) Attribute.
        /// </value>
        [DataMember]
        public virtual Model.EntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the <see cref="Rock.Model.AttributeQualifier">AttributeQualifiers</see> for this Attribute.
        /// </summary>
        /// <value>
        /// Collection of <see cref="Rock.Model.AttributeQualifier">AttributeQualifiers</see> for this Attribute.
        /// </value>
        [DataMember]
        public virtual ICollection<AttributeQualifier> AttributeQualifiers
        {
            get { return _attributeQualifiers ?? ( _attributeQualifiers = new Collection<AttributeQualifier>() ); }
            set { _attributeQualifiers = value; }
        }

        private ICollection<AttributeQualifier> _attributeQualifiers;

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.FieldType"/> that is used to get/capture the value of the Attribute
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.FieldType"/> that is used to get/capture the value of the Attribute.
        /// </value>
        [DataMember]
        public virtual FieldType FieldType { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="Rock.Model.Category">Categories</see> that this Attribute is associated with.
        /// NOTE: Since changes to Categories isn't tracked by ChangeTracker, set the ModifiedDateTime if Categories are modified.
        /// </summary>
        /// <value>
        /// A collection of <see cref="Rock.Model.Category">Categories</see> that this Attribute is associated with.
        /// </value>
        [DataMember]
        public virtual ICollection<Category> Categories
        {
            get { return _categories ?? ( _categories = new Collection<Category>() ); }
            set { _categories = value; }
        }

        private ICollection<Category> _categories;

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Key;
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Attribute Configuration class.
    /// </summary>
    public partial class AttributeConfiguration : EntityTypeConfiguration<Attribute>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeConfiguration"/> class.
        /// </summary>
        public AttributeConfiguration()
        {
            this.HasRequired( a => a.FieldType ).WithMany().HasForeignKey( a => a.FieldTypeId ).WillCascadeOnDelete( false );
            this.HasOptional( a => a.EntityType ).WithMany().HasForeignKey( a => a.EntityTypeId ).WillCascadeOnDelete( false );
            this.HasMany( a => a.Categories ).WithMany().Map( a => { a.MapLeftKey( "AttributeId" ); a.MapRightKey( "CategoryId" ); a.ToTable( "AttributeCategory" ); } );
        }
    }

    #endregion
}
