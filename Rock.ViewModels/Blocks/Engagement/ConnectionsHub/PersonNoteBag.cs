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

using System;
using System.Collections.Generic;

namespace Rock.ViewModels.Blocks.Engagement.ConnectionsHub
{
    /// <summary>
    /// 
    /// </summary>
    public class PersonNoteBag
    {
        public string IdKey { get; set; }

        public string CreatedByPhotoUrl { get; set; }

        public string NoteTypeName { get; set; }

        public string CreatedByName { get; set; }

        public DateTimeOffset? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this note is an alert note.
        /// </summary>
        /// <value><c>true</c> if this note is an alert note; otherwise, <c>false</c>.</value>
        public bool IsAlert { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this note should be pinned to top
        /// </summary>
        /// <value><c>true</c> if this note should be pinned to top otherwise, <c>false</c>.</value>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets the text of the note.
        /// </summary>
        /// <value>The text of the note.</value>
        public string Text { get; set; }
    }
}
