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

using Rock.Enums.Lms;

namespace Rock.ViewModels.Blocks.Lms.LearningCourseDetail
{
    /// <summary>
    /// The additional configuration options for the Learning Course Detail block.
    /// </summary>
    public class LearningCourseDetailOptionsBag
    {
        /// <summary>
        /// Gets or sets the configuration mode of the course's learning program.
        /// </summary>
        public ConfigurationMode ConfigurationMode { get; set; }
    }
}
