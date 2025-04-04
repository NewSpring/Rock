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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.AI.Classes.ChatCompletions;
using Rock.Attribute;
using Rock.Data;
using Rock.Enums.Core;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Represents a type or category of <see cref="Rock.Model.Note">Notes</see> in Rock, and configures the type of entities that notes of this type apply to other settings
    /// specific to the type of note.
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "NoteType" )]
    [DataContract]
    [CodeGenerateRest]
    [Rock.SystemGuid.EntityTypeGuid( "337EED57-D4AB-4EED-BBDB-0CB3A467DBCC" )]
    public partial class NoteType : Model<NoteType>, IOrdered, ICacheable, IHasAdditionalSettings
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating that this NoteType is part of the Rock core system/framework. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if this NoteType is part of the core system/framework, otherwise <c>false</c>.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the Id of the <see cref="Rock.Model.EntityType"/> that this NoteType is used for.  A NoteType can only be associated with a single <see cref="Rock.Model.EntityType"/> and will 
        /// only contain notes for entities of this type. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.EntityType"/>
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public int EntityTypeId { get; set; }

        /// <summary>
        /// Gets or sets the name of the qualifier column/property on the <see cref="Rock.Model.EntityType"/> that this NoteType applies to. If this is not 
        /// provided, the note type can be used on all entities of the provided <see cref="Rock.Model.EntityType"/>.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the name of the qualifier column that this NoteType applies to.
        /// </value>
        [MaxLength( 50 )]
        [DataMember]
        public string EntityTypeQualifierColumn { get; set; }

        /// <summary>
        /// Gets or sets the qualifier value in the qualifier column that this note type applies to.  For instance this note type and related notes will only be applicable to entity 
        /// if the value in the EntityTypeQualiferColumn matches this value. This property should not be populated without also populating the EntityTypeQualifierColumn property.
        /// </summary>
        /// <value>
        /// Entity Type Qualifier Value.
        /// </value>
        [MaxLength( 200 )]
        [DataMember]
        public string EntityTypeQualifierValue { get; set; }

        /// <summary>
        /// Gets or sets the Name of the NoteType. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Name of the NoteType.
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the type is user selectable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [user selectable]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool UserSelectable { get; set; }

        /// <summary>
        /// Gets or sets the name of an icon CSS class. 
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the name of an icon CSS class
        /// </value>
        [MaxLength( 100 )]
        [DataMember]
        public string IconCssClass { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        [DataMember]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [requires approvals].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [requires approvals]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool RequiresApprovals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allows watching].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allows watching]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool AllowsWatching { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allows replies].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allows replies]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool AllowsReplies { get; set; }

        /// <summary>
        /// Gets or sets the maximum reply depth.
        /// </summary>
        /// <value>
        /// The maximum reply depth.
        /// </value>
        [DataMember]
        public int? MaxReplyDepth { get; set; }

        /// <summary>
        /// Gets or sets the base color to use when calculating the color pair for
        /// notes of this type.
        /// </summary>
        /// <value>The base color to use when calculating the color pair.</value>
        [DataMember]
        [MaxLength( 100 )]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the background color of each note
        /// </summary>
        /// <value>
        /// The color of the background.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        [Obsolete( "This property is no longer used and will be removed in the future." )]
        [RockObsolete( "1.16" )]
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the font color of the note text
        /// </summary>
        /// <value>
        /// The color of the font.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        [Obsolete( "This property is no longer used and will be removed in the future." )]
        [RockObsolete( "1.16" )]
        public string FontColor { get; set; }

        /// <summary>
        /// Gets or sets the border color of each note
        /// </summary>
        /// <value>
        /// The color of the border.
        /// </value>
        [DataMember]
        [MaxLength( 100 )]
        [Obsolete( "This property is no longer used and will be removed in the future." )]
        [RockObsolete( "1.16" )]
        public string BorderColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [send approval notifications].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [send approval notifications]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        [Obsolete( "This property is no longer used and will be removed in the future." )]
        [RockObsolete( "1.16" )]
        public bool SendApprovalNotifications { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [automatic watch authors].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [automatic watch authors]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool AutoWatchAuthors { get; set; }

        /// <summary>
        /// A optional Lava Template that can be used to general a URL where Notes of this type can be approved
        /// If this is left blank, the Approval URL will be a URL to the page (including a hash anchor to the note) where the note was originally created
        /// </summary>
        /// <value>
        /// The approval URL template.
        /// </value>
        [DataMember]
        [Obsolete( "This property is no longer used and will be removed in the future." )]
        [RockObsolete( "1.16" )]
        public string ApprovalUrlTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether attachments are allowed for this note type.
        /// </summary>
        /// <value>
        ///   <c>true</c> if attachments are allowed; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool AllowsAttachments { get; set; }

        /// <summary>
        /// Gets or sets the binary file type identifier used when saving attachments.
        /// </summary>
        /// <value>
        /// The binary file type identifier used when saving attachments.
        /// </value>
        [DataMember]
        public int? BinaryFileTypeId { get; set; }

        /// <summary>
        /// Gets or sets the format the note text is stored in.
        /// </summary>
        /// <value>The format the note text is stored in.</value>
        [DataMember]
        public NoteFormatType FormatType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether notes of this type allow
        /// person mentions to be embedded in the text.
        /// </summary>
        /// <value><c>true</c> if person mentions are enabled; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool IsMentionEnabled { get; set; }

        /// <summary>
        /// Gets or sets the additional settings json.
        /// </summary>
        /// <value>
        /// The additional settings json.
        /// </value>
        [DataMember]
        public string AdditionalSettingsJson { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.EntityType"/> of the entities that <see cref="Rock.Model.Note">Notes</see> of this NoteType 
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.EntityType"/> that this NoteType is associated with.
        /// </value>
        [DataMember]
        public virtual EntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.BinaryFileType"/> that will be used for attachments.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.BinaryFileType"/> that will be used for attachments.
        /// </value>
        [DataMember]
        public virtual BinaryFileType BinaryFileType { get; set; }

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
            return this.Name;
        }

        #endregion

        #region IHasAdditionalSettings Models

        /// <summary>
        /// AI Approval Settings
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <strong>This is an internal API</strong> that supports the Rock
        ///         infrastructure and not subject to the same compatibility standards
        ///         as public APIs. It may be changed or removed without notice in any
        ///         release and should therefore not be directly used in any plug-ins.
        ///     </para>
        /// </remarks>
        [RockInternal( "1.17.0" )]
        public class AIApprovalSettings
        {
            /// <summary>
            /// Determines whether AI Approvals are enabled for this note type.
            /// </summary>
            public bool EnabledAIApprovals { get; set; }

            /// <summary>
            /// The criteria for AI to automatically approve notes. 
            /// </summary>
            public string AIApprovalGuidelines { get; set; }

            /// <summary>
            /// The <see cref="Rock.Model.AIProvider"/> to use for AI Approvals.
            /// </summary>
            public int? AIProviderId { get; set; }

            /// <summary>
            /// Gets the System and User messages for the AI Approval Chat Completion request for the <paramref name="note"/>.
            /// </summary>
            /// <param name="note">The <see cref="Note"/> to generate the chat completion messages for.</param>
            /// <returns></returns>
            public List<ChatCompletionsRequestMessage> AIApprovalRequestMessages( Note note )
            {
                return new List<ChatCompletionsRequestMessage>
                {
                    new ChatCompletionsRequestMessage { Role = Enums.AI.ChatMessageRole.System, Content = "Please determine if the note delimited by ```Note Text``` should be approved based on the approval guidance delimited by ```Approval Guidance```. Please answer with only true to approve or false to not approve." },
                    new ChatCompletionsRequestMessage { Role = Enums.AI.ChatMessageRole.User, Content = $@"
```Approval Guidance```
{AIApprovalGuidelines}
```Approval Guidance```

```Note Text```
{note.Text}
```Note Text```" }
                };
            }
        }

        #endregion IHasAdditionalSettings Models
    }

    #region Entity Configuration    

    /// <summary>
    /// Note Type Configuration class.
    /// </summary>
    public partial class NoteTypeConfiguration : EntityTypeConfiguration<NoteType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoteTypeConfiguration"/> class.
        /// </summary>
        public NoteTypeConfiguration()
        {
            this.HasRequired( p => p.EntityType ).WithMany().HasForeignKey( p => p.EntityTypeId ).WillCascadeOnDelete( false );
            this.HasOptional( p => p.BinaryFileType ).WithMany().HasForeignKey( p => p.BinaryFileTypeId ).WillCascadeOnDelete( false );
        }
    }

    #endregion
}
