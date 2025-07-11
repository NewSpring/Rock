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
namespace Rock.ViewModels.Blocks.Communication.Chat.ChatView
{
    /// <summary>
    /// A bag of settings for a chat person.
    /// </summary>
    public class ChatPersonSettingsBag
    {
        /// <summary>
        /// Gets or sets a value indicating whether the chat is open direct message allowed.
        /// </summary>
        public bool IsChatOpenDirectMessageAllowed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the chat profile is public.
        /// </summary>
        public bool IsChatProfilePublic { get; set; }
    }
}
