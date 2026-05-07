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
//

using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Reminders.ReminderEdit;
using Rock.ViewModels.Utility;

namespace Rock.Blocks.Reminders
{
    [DisplayName( "Reminder Edit" )]
    [Category( "Reminders" )]
    [Description( "Block for editing reminders." )]

    [Rock.SystemGuid.EntityTypeGuid( "1EAEBCD6-3A5A-4930-81DD-A86044149275" )]
    // was [Rock.SystemGuid.BlockTypeGuid( "EC258040-5707-402B-AFE0-4EA1E5AD5DCB" )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.REMINDER_EDIT )]
    public class ReminderEdit : RockBlockType
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string EntityTypeId = "EntityTypeId";
            public const string EntityId = "EntityId";
            public const string ReminderId = "ReminderId";
        }

        #endregion Keys

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new CustomBlockBox<ReminderEditBag, ReminderEditOptionsBag>();

            var reminderId = PageParameter( PageParameterKey.ReminderId ).AsInteger();
            var entityTypeId = PageParameter( PageParameterKey.EntityTypeId ).AsInteger();
            var entityId = PageParameter( PageParameterKey.EntityId ).AsInteger();

            if ( reminderId == 0 && ( entityTypeId == 0 || entityId == 0 ) )
            {
                box.ErrorMessage = "The required page parameters were not provided.";
                return box;
            }

            if ( reminderId != 0 )
            {
                LoadExistingReminder( box, reminderId );
            }
            else
            {
                LoadNewReminder( box, entityTypeId, entityId );
            }

            box.Options.ParentPageUrl = this.GetParentPageUrl();

            return box;
        }

        /// <summary>
        /// Populates the box with data for editing an existing reminder.
        /// </summary>
        /// <param name="box">The custom block box to populate.</param>
        /// <param name="reminderId">The reminder identifier.</param>
        private void LoadExistingReminder( CustomBlockBox<ReminderEditBag, ReminderEditOptionsBag> box, int reminderId )
        {
            var reminder = new ReminderService( RockContext ).Queryable()
                .Include( r => r.ReminderType )
                .Include( r => r.PersonAlias.Person )
                .FirstOrDefault( r => r.Id == reminderId );

            if ( reminder == null )
            {
                box.ErrorMessage = "The specified reminder was not found.";
                return;
            }

            var entityTypeId = reminder.ReminderType.EntityTypeId;

            // Get the entity description for the panel header.
            var entity = new EntityTypeService( RockContext ).GetEntity( entityTypeId, reminder.EntityId );
            var entityDescription = entity?.ToString() ?? string.Empty;

            // Load reminder types for the entity type.
            var reminderTypes = new ReminderTypeService( RockContext )
                .GetReminderTypesForEntityType( entityTypeId, RequestContext.CurrentPerson );

            box.Options.ReminderTypes = reminderTypes.ToListItemBagList();

            box.Bag = new ReminderEditBag
            {
                ReminderDate = reminder.ReminderDate.ToString( "s" ),
                IsComplete = reminder.IsComplete,
                Note = reminder.Note,
                ReminderType = reminder.ReminderType.ToListItemBag(),
                PersonAlias = new ListItemBag
                {
                    Value = reminder.PersonAlias.Guid.ToString(),
                    Text = reminder.PersonAlias.Person.FullName
                },
                RenewPeriodDays = reminder.RenewPeriodDays,
                RenewMaxCount = reminder.RenewMaxCount,
                IsExistingReminder = true,
                EntityDescription = entityDescription
            };
        }

        /// <summary>
        /// Populates the box with data for creating a new reminder.
        /// </summary>
        /// <param name="box">The custom block box to populate.</param>
        /// <param name="entityTypeId">The entity type identifier.</param>
        /// <param name="entityId">The entity identifier.</param>
        private void LoadNewReminder( CustomBlockBox<ReminderEditBag, ReminderEditOptionsBag> box, int entityTypeId, int entityId )
        {
            // Get the entity description for the panel header.
            var entity = new EntityTypeService( RockContext ).GetEntity( entityTypeId, entityId );
            var entityDescription = entity?.ToString() ?? string.Empty;

            // Load reminder types for the entity type.
            var reminderTypes = new ReminderTypeService( RockContext )
                .GetReminderTypesForEntityType( entityTypeId, RequestContext.CurrentPerson );

            box.Options.ReminderTypes = reminderTypes.ToListItemBagList();

            var currentPerson = RequestContext.CurrentPerson;
            var primaryAliasGuid = new PersonAliasService( RockContext ).GetPrimaryAliasGuid( currentPerson.Id );

            box.Bag = new ReminderEditBag
            {
                IsExistingReminder = false,
                EntityDescription = entityDescription,
                PersonAlias = new ListItemBag
                {
                    Value = primaryAliasGuid?.ToString(),
                    Text = currentPerson.FullName
                }
            };
        }

        #endregion Methods

        #region Block Actions

        /// <summary>
        /// Saves the reminder. Creates a new reminder or updates an existing one
        /// based on the provided data.
        /// </summary>
        /// <param name="bag">The save data from the client.</param>
        /// <returns>A block action result indicating success or failure.</returns>
        [BlockAction]
        public BlockActionResult Save( ReminderEditSaveBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest( "No data was provided." );
            }

            if ( bag.ReminderDate.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A reminder date is required." );
            }

            if ( bag.PersonAliasGuid == System.Guid.Empty )
            {
                return ActionBadRequest( "A person is required." );
            }

            if ( bag.ReminderTypeGuid == System.Guid.Empty )
            {
                return ActionBadRequest( "A reminder type is required." );
            }

            var reminderService = new ReminderService( RockContext );
            var reminderTypeService = new ReminderTypeService( RockContext );

            var reminderType = reminderTypeService.Get( bag.ReminderTypeGuid );
            if ( reminderType == null )
            {
                return ActionBadRequest( "The specified reminder type was not found." );
            }

            var personAlias = new PersonAliasService( RockContext ).Get( bag.PersonAliasGuid );
            if ( personAlias == null )
            {
                return ActionBadRequest( "The specified person was not found." );
            }

            var reminderDate = bag.ReminderDate.AsDateTime();
            if ( !reminderDate.HasValue )
            {
                return ActionBadRequest( "The reminder date is not valid." );
            }

            var reminderId = PageParameter( PageParameterKey.ReminderId ).AsInteger();

            if ( reminderId != 0 )
            {
                // Update existing reminder.
                var reminder = reminderService.Get( reminderId );
                if ( reminder == null )
                {
                    return ActionBadRequest( "The specified reminder was not found." );
                }

                reminder.ReminderTypeId = reminderType.Id;
                reminder.ReminderDate = reminderDate.Value;
                reminder.Note = bag.Note;
                reminder.RenewPeriodDays = bag.RenewPeriodDays;
                reminder.RenewMaxCount = bag.RenewMaxCount;
                reminder.PersonAliasId = personAlias.Id;

                /*
                    3/26/26 - MSE
                    Use the model's CompleteReminder / ResetCompletedReminder methods
                    to correctly handle renewal logic when toggling completion status.

                    Reason: Direct property assignment would bypass renewal date advancement.
                */
                if ( reminder.IsComplete && !bag.IsComplete )
                {
                    reminder.ResetCompletedReminder();
                }
                else if ( !reminder.IsComplete && bag.IsComplete )
                {
                    reminder.CompleteReminder();
                }

                RockContext.SaveChanges();
            }
            else
            {
                // Create new reminder.
                var entityTypeId = PageParameter( PageParameterKey.EntityTypeId ).AsInteger();
                var entityId = PageParameter( PageParameterKey.EntityId ).AsInteger();

                if ( entityTypeId == 0 || entityId == 0 )
                {
                    return ActionBadRequest( "Entity type and entity are required for new reminders." );
                }

                var reminder = new Reminder
                {
                    EntityId = entityId,
                    ReminderTypeId = reminderType.Id,
                    ReminderDate = reminderDate.Value,
                    Note = bag.Note,
                    IsComplete = false,
                    RenewPeriodDays = bag.RenewPeriodDays,
                    RenewMaxCount = bag.RenewMaxCount,
                    RenewCurrentCount = 0,
                    PersonAliasId = personAlias.Id
                };

                reminderService.Add( reminder );
                RockContext.SaveChanges();
            }

            return ActionOk();
        }

        #endregion Block Actions
    }
}
