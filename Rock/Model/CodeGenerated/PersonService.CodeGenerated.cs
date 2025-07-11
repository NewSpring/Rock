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
    /// Person Service class
    /// </summary>
    public partial class PersonService : Service<Person>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonService"/> class
        /// </summary>
        /// <param name="context">The context.</param>
        public PersonService(RockContext context) : base(context)
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
        public bool CanDelete( Person item, out string errorMessage )
        {
            errorMessage = string.Empty;

            // ignoring PeerNetwork,SourcePersonId

            // ignoring PeerNetwork,TargetPersonId

            if ( new Service<PersonAlias>( Context ).Queryable().Any( a => a.PersonId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", Person.FriendlyTypeName, PersonAlias.FriendlyTypeName );
                return false;
            }
            return true;
        }
    }

    [HasQueryableAttributes( typeof( Person.PersonQueryableAttributeValue ), nameof( PersonAttributeValues ) )]
    public partial class Person
    {
        /// <summary>
        /// Gets the entity attribute values. This should only be used inside
        /// LINQ statements when building a where clause for the query. This
        /// property should only be used inside LINQ statements for filtering
        /// or selecting values. Do <b>not</b> use it for accessing the
        /// attributes after the entity has been loaded.
        /// </summary>
        public virtual ICollection<PersonQueryableAttributeValue> PersonAttributeValues { get; set; } 

        /// <inheritdoc/>
        public class PersonQueryableAttributeValue : QueryableAttributeValue
        {
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class PersonExtensionMethods
    {
        /// <summary>
        /// Clones this Person object to a new Person object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static Person Clone( this Person source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as Person;
            }
            else
            {
                var target = new Person();
                target.CopyPropertiesFrom( source );
                return target;
            }
        }

        /// <summary>
        /// Clones this Person object to a new Person object with default values for the properties in the Entity and Model base classes.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static Person CloneWithoutIdentity( this Person source )
        {
            var target = new Person();
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
        /// Copies the properties from another Person object to this Person object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public static void CopyPropertiesFrom( this Person target, Person source )
        {
            target.Id = source.Id;
            target.AccountProtectionProfile = source.AccountProtectionProfile;
            target.AgeClassification = source.AgeClassification;
            target.AnniversaryDate = source.AnniversaryDate;
            target.BirthDateKey = source.BirthDateKey;
            target.BirthDay = source.BirthDay;
            target.BirthMonth = source.BirthMonth;
            target.BirthYear = source.BirthYear;
            target.CommunicationPreference = source.CommunicationPreference;
            target.ConnectionStatusValueId = source.ConnectionStatusValueId;
            target.ContributionFinancialAccountId = source.ContributionFinancialAccountId;
            target.DeceasedDate = source.DeceasedDate;
            target.Email = source.Email;
            target.EmailNote = source.EmailNote;
            target.EmailPreference = source.EmailPreference;
            target.EthnicityValueId = source.EthnicityValueId;
            target.FirstName = source.FirstName;
            target.FirstNamePronunciationOverride = source.FirstNamePronunciationOverride;
            target.ForeignGuid = source.ForeignGuid;
            target.ForeignKey = source.ForeignKey;
            target.Gender = source.Gender;
            target.GivingGroupId = source.GivingGroupId;
            target.GivingLeaderId = source.GivingLeaderId;
            target.GraduationYear = source.GraduationYear;
            target.InactiveReasonNote = source.InactiveReasonNote;
            target.IsChatOpenDirectMessageAllowed = source.IsChatOpenDirectMessageAllowed;
            target.IsChatProfilePublic = source.IsChatProfilePublic;
            target.IsDeceased = source.IsDeceased;
            target.IsEmailActive = source.IsEmailActive;
            target.IsLockedAsChild = source.IsLockedAsChild;
            target.IsSystem = source.IsSystem;
            target.LastName = source.LastName;
            target.LastNamePronunciationOverride = source.LastNamePronunciationOverride;
            target.MaritalStatusValueId = source.MaritalStatusValueId;
            target.MiddleName = source.MiddleName;
            target.NickName = source.NickName;
            target.NickNamePronunciationOverride = source.NickNamePronunciationOverride;
            target.PhotoId = source.PhotoId;
            target.PreferredLanguageValueId = source.PreferredLanguageValueId;
            target.PrimaryAliasGuid = source.PrimaryAliasGuid;
            target.PrimaryAliasId = source.PrimaryAliasId;
            target.PrimaryCampusId = source.PrimaryCampusId;
            target.PrimaryFamilyId = source.PrimaryFamilyId;
            target.PronunciationNote = source.PronunciationNote;
            target.RaceValueId = source.RaceValueId;
            target.RecordSourceValueId = source.RecordSourceValueId;
            target.RecordStatusLastModifiedDateTime = source.RecordStatusLastModifiedDateTime;
            target.RecordStatusReasonValueId = source.RecordStatusReasonValueId;
            target.RecordStatusValueId = source.RecordStatusValueId;
            target.RecordTypeValueId = source.RecordTypeValueId;
            target.ReminderCount = source.ReminderCount;
            target.ReviewReasonNote = source.ReviewReasonNote;
            target.ReviewReasonValueId = source.ReviewReasonValueId;
            target.SuffixValueId = source.SuffixValueId;
            target.SystemNote = source.SystemNote;
            target.TitleValueId = source.TitleValueId;
            target.TopSignalColor = source.TopSignalColor;
            target.TopSignalIconCssClass = source.TopSignalIconCssClass;
            target.TopSignalId = source.TopSignalId;
            target.ViewedCount = source.ViewedCount;
            target.CreatedDateTime = source.CreatedDateTime;
            target.ModifiedDateTime = source.ModifiedDateTime;
            target.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            target.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            target.Guid = source.Guid;
            target.ForeignId = source.ForeignId;

        }
    }
}
