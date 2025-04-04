//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// <copyright>
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
using System.Collections.Generic;
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// WorkflowActionFormAttribute Service class
    /// </summary>
    public partial class WorkflowActionFormAttributeService : Service<WorkflowActionFormAttribute>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowActionFormAttributeService"/> class
        /// </summary>
        /// <param name="context">The context.</param>
        public WorkflowActionFormAttributeService(RockContext context) : base(context)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( WorkflowActionFormAttribute item, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }
    }

    [HasQueryableAttributes( typeof( WorkflowActionFormAttribute.WorkflowActionFormAttributeQueryableAttributeValue ), nameof( WorkflowActionFormAttributeAttributeValues ) )]
    public partial class WorkflowActionFormAttribute
    {
        /// <summary>
        /// Gets the entity attribute values. This should only be used inside
        /// LINQ statements when building a where clause for the query. This
        /// property should only be used inside LINQ statements for filtering
        /// or selecting values. Do <b>not</b> use it for accessing the
        /// attributes after the entity has been loaded.
        /// </summary>
        public virtual ICollection<WorkflowActionFormAttributeQueryableAttributeValue> WorkflowActionFormAttributeAttributeValues { get; set; } 

        /// <inheritdoc/>
        public class WorkflowActionFormAttributeQueryableAttributeValue : QueryableAttributeValue
        {
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class WorkflowActionFormAttributeExtensionMethods
    {
        /// <summary>
        /// Clones this WorkflowActionFormAttribute object to a new WorkflowActionFormAttribute object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static WorkflowActionFormAttribute Clone( this WorkflowActionFormAttribute source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as WorkflowActionFormAttribute;
            }
            else
            {
                var target = new WorkflowActionFormAttribute();
                target.CopyPropertiesFrom( source );
                return target;
            }
        }

        /// <summary>
        /// Clones this WorkflowActionFormAttribute object to a new WorkflowActionFormAttribute object with default values for the properties in the Entity and Model base classes.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static WorkflowActionFormAttribute CloneWithoutIdentity( this WorkflowActionFormAttribute source )
        {
            var target = new WorkflowActionFormAttribute();
            target.CopyPropertiesFrom( source );

            target.Id = 0;
            target.Guid = Guid.NewGuid();
            target.ForeignKey = null;
            target.ForeignId = null;
            target.ForeignGuid = null;
            target.CreatedByPersonAliasId = null;
            target.CreatedDateTime = RockDateTime.Now;
            target.ModifiedByPersonAliasId = null;
            target.ModifiedDateTime = RockDateTime.Now;

            return target;
        }

        /// <summary>
        /// Copies the properties from another WorkflowActionFormAttribute object to this WorkflowActionFormAttribute object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public static void CopyPropertiesFrom( this WorkflowActionFormAttribute target, WorkflowActionFormAttribute source )
        {
            target.Id = source.Id;
            target.ActionFormSectionId = source.ActionFormSectionId;
            target.AttributeId = source.AttributeId;
            target.ColumnSize = source.ColumnSize;
            target.FieldVisibilityRulesJSON = source.FieldVisibilityRulesJSON;
            target.ForeignGuid = source.ForeignGuid;
            target.ForeignKey = source.ForeignKey;
            target.HideLabel = source.HideLabel;
            target.IsReadOnly = source.IsReadOnly;
            target.IsRequired = source.IsRequired;
            target.IsVisible = source.IsVisible;
            target.Order = source.Order;
            target.PostHtml = source.PostHtml;
            target.PreHtml = source.PreHtml;
            target.WorkflowActionFormId = source.WorkflowActionFormId;
            target.CreatedDateTime = source.CreatedDateTime;
            target.ModifiedDateTime = source.ModifiedDateTime;
            target.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            target.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            target.Guid = source.Guid;
            target.ForeignId = source.ForeignId;

        }
    }
}
