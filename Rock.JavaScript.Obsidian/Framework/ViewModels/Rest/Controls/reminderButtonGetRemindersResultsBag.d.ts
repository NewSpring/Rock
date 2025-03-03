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
//

import { ReminderButtonGetRemindersReminderBag } from "@Obsidian/ViewModels/Rest/Controls/reminderButtonGetRemindersReminderBag";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";

/**
 * Representation of a Reminder retrieved from the GetReminders API action of
 * the ReminderButton control.
 */
export type ReminderButtonGetRemindersResultsBag = {
    /** URL to go to to edit a reminder */
    editUrl?: string | null;

    /** Name of the entity the reminder is about */
    entityName?: string | null;

    /** Name of the type of entity that the entity is */
    entityTypeName?: string | null;

    /** A list of reminders to display */
    reminders?: ReminderButtonGetRemindersReminderBag[] | null;

    /** List of available reminder types to choose from */
    reminderTypes?: ListItemBag[] | null;

    /** URL to go to for viewing all of the person's reminders */
    viewUrl?: string | null;
};
