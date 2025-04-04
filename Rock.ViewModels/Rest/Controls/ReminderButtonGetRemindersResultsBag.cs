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

using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Rest.Controls
{
    /// <summary>
    /// Representation of a Reminder retrieved from the GetReminders API action of
    /// the ReminderButton control.
    /// </summary>
    public class ReminderButtonGetRemindersResultsBag
    {
        /// <summary>
        /// A list of reminders to display
        /// </summary>
        public List<ReminderButtonGetRemindersReminderBag> Reminders { get; set; }

        /// <summary>
        /// List of available reminder types to choose from
        /// </summary>
        public List<ListItemBag> ReminderTypes { get; set; }

        /// <summary>
        /// Name of the entity the reminder is about
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Name of the type of entity that the entity is
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// URL to go to for viewing all of the person's reminders
        /// </summary>
        public string ViewUrl { get; set; }

        /// <summary>
        /// URL to go to to edit a reminder
        /// </summary>
        public string EditUrl { get; set; }
    }
}
