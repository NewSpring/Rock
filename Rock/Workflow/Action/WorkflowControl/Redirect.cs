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
using System.Linq;
using System.Web;

using Rock.Attribute;
using Rock.Data;
using Rock.Enums.Workflow;
using Rock.Model;
using Rock.Net;
using Rock.ViewModels.Workflow;

namespace Rock.Workflow.Action
{
    /// <summary>
    /// Redirects the user to a different page.
    /// </summary>
    [ActionCategory( "Workflow Control" )]
    [Description( "Redirects the user to a different page." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Redirect to Page" )]


    [WorkflowTextOrAttribute( "Url",
        "Url Attribute",
        Description = "The full URL to redirect to, for example: http://www.rockrms.com  <span class='tip tip-lava'></span>",
        Key = AttributeKey.Url,
        IsRequired = true,
        DefaultValue = "",
        Category = "",
        FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.UrlLinkFieldType", "Rock.Field.Types.AudioUrlFieldType", "Rock.Field.Types.VideoUrlFieldType" },
        Order = 0 )]

    [CustomDropdownListField(
        "Processing Options",
        Key = AttributeKey.ProcessingOptions,
        Description = "Select how the workflow should proceed based on the action's outcome: <ul><li>Always Continue: proceed regardless of redirect status,</li><li>Only Continue on Redirect: continue workflow only on successful redirects, or</li><li>Fail and Stop: halt the Workflow and mark the Action as failed unless skipped by the 'Run If' filter.</li></ul>",
        ListSource = "0^Always Continue,1^Only Continue on Redirect,2^Fail and Stop",
        IsRequired = true,
        DefaultValue = "0",
        Category = "",
        Order = 1 )]

    [Rock.SystemGuid.EntityTypeGuid( "E2F3DFC1-415D-45C9-B84E-D91562139FDA")]
    public class Redirect : ActionComponent, IInteractiveAction
    {

        #region Attribute Keys

        private static class AttributeKey
        {
            public const string Url = "Url";
            public const string ProcessingOptions = "ProcessingOptions";
        }

        #endregion Attribute Keys

        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( IsObsidianBlock() )
            {
                return false;
            }

            string url = GetAttributeValue( action, AttributeKey.Url );
            Guid guid = url.AsGuid();
            if ( guid.IsEmpty() )
            {
                url = url.ResolveMergeFields( GetMergeFields( action ) );
            }
            else
            {
                url = action.GetWorkflowAttributeValue( guid );
            }

            var processOpt = GetAttributeValue( action, AttributeKey.ProcessingOptions );
            bool canSendRedirect = !string.IsNullOrWhiteSpace( url ) && HttpContext.Current != null;

            if ( canSendRedirect )
            {
                HttpContext.Current.Response.Redirect( url, false );
            }

            if ( processOpt == "1" )
            {
                // 1) if HttpContext.Current is null, this workflow action might be running as a job (there is no browser session associated), so Redirect couldn't have been sent to a browser
                // 2) if there was no url specified, the redirect wouldn't have executed either
                return canSendRedirect;
            }
            else
            {
                return processOpt != "2";
            }
        }

        /// <summary>
        /// Determines if this action is being executed in the context of the
        /// new Obsidian block.
        /// </summary>
        /// <returns><c>true</c> if being executed in the context of the new Obsidian block; <c>false</c> if legacy WebForms.</returns>
        private bool IsObsidianBlock()
        {
            // If we are being processed by the new Obsidian Workflow Entry
            // block, then don't continue processing this action as it will be
            // handled by the block. This specific case handles the scenario of
            // a block action submitting a form and then eventually hitting
            // this action.
            if ( RockRequestContextAccessor.Current?.RequestUri?.AbsolutePath?.StartsWith( "/api/v2/BlockActions", StringComparison.OrdinalIgnoreCase ) == true )
            {
                return true;
            }

            if ( HttpContext.Current?.Handler is System.Web.UI.Page page )
            {
                var obsidianWorkflowEntryBlock = page.ControlsOfTypeRecursive<Rock.Web.UI.RockBlockTypeWrapper>()
                    .Where( bw => bw.BlockCache?.BlockType?.Guid == new Guid( "9116AAD8-CF16-4BCE-B0CF-5B4D565710ED" ) )
                    .FirstOrDefault();

                // If we are being processed by the new Obsidian Workflow
                // Entry block, then don't continue processing this action
                // as it will be handled by the block. This specific case
                // handles the scenario of an initial page load causing the
                // workflow to process and hit here.
                if ( obsidianWorkflowEntryBlock != null )
                {
                    return true;
                }
            }

            return false;
        }

        #region IInteractiveAction

        /// <inheritdoc/>
        InteractiveActionResult IInteractiveAction.StartAction( WorkflowAction action, RockContext rockContext, RockRequestContext requestContext )
        {
            var mergeFields = GetMergeFields( action, requestContext );
            var url = GetAttributeValue( action, AttributeKey.Url, true ).ResolveMergeFields( mergeFields );
            var processOpt = GetAttributeValue( action, AttributeKey.ProcessingOptions );
            var canSendRedirect = url.IsNotNullOrWhiteSpace();

            var result = new InteractiveActionResult
            {
                IsSuccess = true,
                ProcessingType = InteractiveActionContinueMode.ContinueWhileUnattended,
                ActionData = new InteractiveActionDataBag
                {
                    Message = new InteractiveMessageBag
                    {
                        Type = InteractiveMessageType.Redirect,
                        Content = url
                    }
                }
            };

            if ( processOpt == "1" )
            {
                result.IsSuccess = canSendRedirect;
            }
            else if ( processOpt == "2" )
            {
                result.IsSuccess = false;
                result.ProcessingType = InteractiveActionContinueMode.Stop;
            }

            return result;
        }

        /// <inheritdoc/>
        InteractiveActionResult IInteractiveAction.UpdateAction( WorkflowAction action, Dictionary<string, string> componentData, RockContext rockContext, RockRequestContext requestContext )
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
