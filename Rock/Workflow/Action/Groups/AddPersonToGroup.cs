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

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Workflow.Action
{
    /// <summary>
    /// Sets an attribute's value to the selected person 
    /// </summary>
    [ActionCategory( "Groups" )]
    [Description( "Adds person to a specific group." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Group Member Add" )]

    [WorkflowAttribute( "Person", "Workflow attribute that contains the person to add to the group.", true, "", "", 0, AttributeKey.Person, 
        new string[] { "Rock.Field.Types.PersonFieldType" })]
    [WorkflowAttribute( "Group Member", "An optional GroupMember attribute to store the group member that is added.", false, "", "", 1, AttributeKey.GroupMember, 
        new string[] { "Rock.Field.Types.GroupMemberFieldType" } )]

    [GroupAndRoleFieldAttribute( "Group and Role", "Group/Role to add the person to. Leave role blank to use the default role for that group.", "Group", true, "", "", 1, AttributeKey.GroupAndRole )]
    [EnumField( "Group Member Status", "The  status to set the user to in the group.", typeof( GroupMemberStatus ), true, "1", "", 2, AttributeKey.GroupMemberStatus )]
    [BooleanField( "Update Existing", "If the selected person already belongs to the selected group, should their current role and status be updated to reflect the configured values above.", true, "", 3, AttributeKey.UpdateExisting )]
    [BooleanField( "Ignore Group Member Requirements", "When enabled, group member requirements are bypassed, allowing the person to be added regardless of whether they meet the criteria.", false, "", 4, AttributeKey.IgnoreGroupMemberRequirements )]

    [Rock.SystemGuid.EntityTypeGuid( "DF0167A1-6928-4FBC-893B-5826A28AAC83")]
    public class AddPersonToGroup : ActionComponent
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string Person = "Person";
            public const string GroupMember = "GroupMember";
            public const string GroupAndRole = "GroupAndRole";
            public const string GroupMemberStatus = "GroupMemberStatus";
            public const string IsSecurityRole = "IsSecurityRole";
            public const string UpdateExisting = "UpdateExisting";
            public const string IgnoreGroupMemberRequirements = "IgnoreGroupMemberRequirements";
        }

        #endregion

        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Determine which group to add the person to
            Group group = null;
            int? groupRoleId = null;

            var groupAndRoleValues = ( GetAttributeValue( action, AttributeKey.GroupAndRole ) ?? string.Empty ).Split( '|' );
            if ( groupAndRoleValues.Count() > 1 )
            {
                var groupGuid = groupAndRoleValues[1].AsGuidOrNull();
                if ( groupGuid.HasValue )
                {
                    group = new GroupService( rockContext ).Get( groupGuid.Value );
                         
                    if ( groupAndRoleValues.Count() > 2 )
                    {
                        var groupTypeRoleGuid = groupAndRoleValues[2].AsGuidOrNull();
                        if ( groupTypeRoleGuid.HasValue )
                        {
                            var groupRole = new GroupTypeRoleService( rockContext ).Get( groupTypeRoleGuid.Value );
                            if ( groupRole != null )
                            {
                                groupRoleId = groupRole.Id;
                            }
                        }
                    }

                    if ( !groupRoleId.HasValue && group != null )
                    {
                        // use the group's grouptype's default group role if a group role wasn't specified
                        groupRoleId = group.GroupType.DefaultGroupRoleId;
                    }
                }
            }

            if ( group == null )
            {
                errorMessages.Add( "No group was provided" );
            }

            if ( group.IsArchived )
            {
                errorMessages.Add( "Group provided was archived." );
            }

            if ( !groupRoleId.HasValue )
            {
                errorMessages.Add( "No group role was provided and group doesn't have a default group role" );
            }

            // determine the person that will be added to the group
            Person person = null;

            // get the Attribute.Guid for this workflow's Person Attribute so that we can lookup the value
            var guidPersonAttribute = GetAttributeValue( action, AttributeKey.Person ).AsGuidOrNull();

            if ( guidPersonAttribute.HasValue )
            {
                var attributePerson = AttributeCache.Get( guidPersonAttribute.Value, rockContext );
                if ( attributePerson != null )
                {
                    string attributePersonValue = action.GetWorkflowAttributeValue( guidPersonAttribute.Value );
                    if ( !string.IsNullOrWhiteSpace( attributePersonValue ) )
                    {
                        if ( attributePerson.FieldType.Class == typeof( Rock.Field.Types.PersonFieldType ).FullName )
                        {
                            Guid personAliasGuid = attributePersonValue.AsGuid();
                            if ( !personAliasGuid.IsEmpty() )
                            {
                                person = new PersonAliasService( rockContext ).Queryable()
                                    .Where( a => a.Guid.Equals( personAliasGuid ) )
                                    .Select( a => a.Person )
                                    .FirstOrDefault();
                            }
                        }
                        else
                        {
                            errorMessages.Add( "The attribute used to provide the person was not of type 'Person'." );
                        }
                    }
                }
            }

            if ( person == null )
            {
                errorMessages.Add( string.Format( "Person could not be found for selected value ('{0}')!", guidPersonAttribute.ToString() ) );
            }

            // Add Person to Group
            if ( !errorMessages.Any() )
            {
                var status = this.GetAttributeValue( action, AttributeKey.GroupMemberStatus ).ConvertToEnum<GroupMemberStatus>( GroupMemberStatus.Active );
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMember = GetByGroupIdAndPersonIdAndPreferredGroupRoleId( groupMemberService, group.Id, person.Id, groupRoleId.Value );
                bool isNew = false;
                if ( groupMember == null )
                {
                    groupMember = new GroupMember();
                    groupMember.PersonId = person.Id;
                    groupMember.GroupId = group.Id;
                    groupMember.GroupRoleId = groupRoleId.Value;
                    groupMember.GroupMemberStatus = status;
                    isNew = true;
                }
                else
                {
                    groupMember.IsArchived = false;
                    if ( GetAttributeValue( action, AttributeKey.UpdateExisting ).AsBoolean() )
                    {
                        groupMember.GroupRoleId = groupRoleId.Value;
                        groupMember.GroupMemberStatus = status;
                    }

                    action.AddLogEntry( $"{person.FullName} was already a member of the selected group.", true );
                }

                // Set to skip group member requirements checking if the option is enabled.
                groupMember.IsSkipRequirementsCheckingDuringValidationCheck = GetAttributeValue( action, AttributeKey.IgnoreGroupMemberRequirements ).AsBoolean();

                if ( groupMember.IsValidGroupMember( rockContext ) )
                {
                    if (isNew)
                    {
                        groupMemberService.Add(groupMember);
                    }

                    rockContext.SaveChanges();
                }
                else
                {
                    // if the group member couldn't be added (for example, one of the group membership rules didn't pass), add the validation messages to the errormessages
                    errorMessages.AddRange( groupMember.ValidationResults.Select( a => a.ErrorMessage ) );
                }

                // If group member attribute was specified, re-query the request and set the attribute's value
                Guid? groupMemberAttributeGuid = GetAttributeValue( action, AttributeKey.GroupMember ).AsGuidOrNull();
                if ( groupMemberAttributeGuid.HasValue )
                {
                    groupMember = groupMemberService.Get( groupMember.Id );
                    if ( groupMember != null )
                    {
                        SetWorkflowAttributeValue( action, groupMemberAttributeGuid.Value, groupMember.Guid.ToString() );
                    }
                }
            }

            errorMessages.ForEach( m => action.AddLogEntry( m, true ) );

            return true;
        }

        /// <summary>
        /// Returns the first <see cref="Rock.Model.GroupMember"/> that matches the Id of the <see cref="Rock.Model.Group"/>,
        /// the Id of the <see cref="Rock.Model.Person"/>, and the Id of the <see cref="Rock.Model.GroupTypeRole"/>. If a 
        /// GroupMember cannot be found with a matching GroupTypeRole, the first GroupMember that matches the Group Id and 
        /// Person Id will be returned (with a different role id).
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="personId">The person identifier.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <param name="groupMemberService">The group member service.</param>
        /// <returns></returns>
        public GroupMember GetByGroupIdAndPersonIdAndPreferredGroupRoleId(GroupMemberService groupMemberService, int groupId, int personId, int groupRoleId )
        {
            var members = groupMemberService
                .Queryable( "Person,GroupRole", false, true )
                .Where( t => t.GroupId == groupId && t.PersonId == personId )
                .OrderBy( g => g.GroupRole.Order )
                .ToList();
            return members.Where( t => t.GroupRoleId == groupRoleId ).FirstOrDefault() ?? members.FirstOrDefault();
        }
    }
}