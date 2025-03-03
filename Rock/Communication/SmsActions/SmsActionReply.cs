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
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Communication.SmsActions
{
    /// <summary>
    /// Processes an SMS Action by sending a Lava-processed response back to the sender.
    /// </summary>
    /// <seealso cref="Rock.Communication.SmsActions.SmsActionComponent" />
    [Description( "Sends a response to the message." )]
    [Export( typeof( SmsActionComponent ) )]
    [ExportMetadata( "ComponentName", "Reply" )]

    #region Attributes

    [TextValueFilterField( "Message",
        Description = "The message body content that will be filtered on.",
        Key = AttributeKeys.Message,
        IsRequired = false,
        Category = AttributeCategories.Filters,
        Order = 1 )]

    [MemoField( "Response",
        Description = "The response that will be sent. <span class='tip tip-lava'></span>",
        Key = AttributeKeys.Response,
        IsRequired = true,
        Category = AttributeCategories.Response,
        Order = 2 )]

    [FileField( Rock.SystemGuid.BinaryFiletype.DEFAULT,
        "Attachment",
        Description = "An attached file that will be sent. Note that when sending attachments with MMS; jpg, gif, and png images are supported for all carriers. Support for other file types is dependent upon each carrier and device. So make sure to test sending this to different carriers and phone types to see if it will work as expected.",
        IsRequired = false,
        Key = AttributeKeys.Attachment,
        Order = 3,
        Category = AttributeCategories.Response )]

    #endregion Attributes

    [Rock.SystemGuid.EntityTypeGuid( "029085A7-5750-4055-BC37-2272BD194E1D")]
    public class SmsActionReply : SmsActionComponent
    {
        #region Keys

        /// <summary>
        /// Keys for the attributes
        /// </summary>
        private static class AttributeKeys
        {
            /// <summary>
            /// The message filter.
            /// </summary>
            public const string Message = "Message";

            /// <summary>
            /// The response.
            /// </summary>
            public const string Response = "Response";

            /// <summary>
            /// The attachment.
            /// </summary>
            public const string Attachment = "Attachment";
        }

        /// <summary>
        /// Categories for the attributes
        /// </summary>
        protected class AttributeCategories : BaseAttributeCategories
        {
            /// <summary>
            /// The filters category
            /// </summary>
            public const string Response = "Response";
        }

        #endregion Keys

        #region Properties

        /// <summary>
        /// Gets the component title to be displayed to the user.
        /// </summary>
        /// <value>
        /// The component title to be displayed to the user.
        /// </value>
        public override string Title => "Reply";

        /// <summary>
        /// Gets the icon CSS class used to identify this component type.
        /// </summary>
        /// <value>
        /// The icon CSS class used to identify this component type.
        /// </value>
        public override string IconCssClass => "fa fa-comment-o";

        /// <summary>
        /// Gets the description of this SMS Action.
        /// </summary>
        /// <value>
        /// The description of this SMS Action.
        /// </value>
        public override string Description => "Sends a response back to the sender.";

        #endregion Properties

        #region Base Method Overrides

        /// <summary>
        /// Checks the attributes for this component and determines if the message
        /// should be processed.
        /// </summary>
        /// <param name="action">The action that contains the configuration for this component.</param>
        /// <param name="message">The message that is to be checked.</param>
        /// <param name="errorMessage">If there is a problem processing, this should be set</param>
        /// <returns>
        ///   <c>true</c> if the message should be processed.
        /// </returns>
        public override bool ShouldProcessMessage( SmsActionCache action, SmsMessage message, out string errorMessage )
        {
            // Give the base class a chance to check it's own settings to see if we
            // should process this message.
            if ( !base.ShouldProcessMessage( action, message, out errorMessage ) )
            {
                return false;
            }

            // If the (required) "Response" attribute does not have a value, skip
            // processing this action.
            var responseMessage = action.GetAttributeValue( AttributeKeys.Response );
            if ( string.IsNullOrWhiteSpace( responseMessage ) )
            {
                return false;
            }

            // Get the filter expression for the message body.
            var attribute = action.Attributes.ContainsKey( AttributeKeys.Message ) ? action.Attributes[AttributeKeys.Message] : null;
            var msg = GetAttributeValue( action, AttributeKeys.Message );
            var filter = ValueFilterFieldType.GetFilterExpression( attribute?.QualifierValues, msg );

            // Evaluate the message against the filter and return the match state.
            return filter != null ? filter.Evaluate( message, AttributeKeys.Message ) : true;
        }

        /// <summary>
        /// Processes the message that was received from the remote user.
        /// </summary>
        /// <param name="action">The action that contains the configuration for this component.</param>
        /// <param name="message">The message that was received by Rock.</param>
        /// <param name="errorMessage">If there is a problem processing, this should be set</param>
        /// <returns>An SmsMessage that will be sent as the response or null if no response should be sent.</returns>
        public override SmsMessage ProcessMessage( SmsActionCache action, SmsMessage message, out string errorMessage )
        {
            errorMessage = string.Empty;

            // Process the message with lava to get the response that should be sent back.
            var mergeObjects = new Dictionary<string, object>
            {
                { AttributeKeys.Message, message }
            };
            var responseMessage = action.GetAttributeValue( AttributeKeys.Response ).ResolveMergeFields( mergeObjects, message.FromPerson );

            // Add the attachment (if one was specified)
            var attachmentBinaryFileGuid = GetAttributeValue( action, "Attachment").AsGuidOrNull();
            BinaryFile binaryFile = null;

            if ( attachmentBinaryFileGuid.HasValue && attachmentBinaryFileGuid != Guid.Empty )
            {
                binaryFile = new BinaryFileService( new RockContext() ).Get( attachmentBinaryFileGuid.Value );
            }

            // If there is no response message then return null.
            if ( string.IsNullOrWhiteSpace( responseMessage ) )
            {
                return null;
            }

            var smsMessage = new SmsMessage
            {
                Message = responseMessage.Trim()
            };

            if (binaryFile != null )
            {
                smsMessage.Attachments.Add( binaryFile );
            }

            return smsMessage;
        }

        #endregion Base Method Overrides
    }
}
