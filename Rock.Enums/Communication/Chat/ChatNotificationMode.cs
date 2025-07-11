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
namespace Rock.Enums.Communication.Chat
{
    /// <summary>
    /// Determines how notifications are sent for a chat channel.
    /// </summary>
    public enum ChatNotificationMode
    {
        /// <summary>
        /// Sends a notification for every new message within the chat channel.
        /// </summary>
        AllMessages = 0,

        /// <summary>
        /// Only sends a notification when an individual is mentioned or someone replies in a thread they're a part of.
        /// </summary>
        MentionsAndReplies = 1,

        /// <summary>
        /// Disables all notifications for the chat channel.
        /// </summary>
        Silent = 2
    }
}
