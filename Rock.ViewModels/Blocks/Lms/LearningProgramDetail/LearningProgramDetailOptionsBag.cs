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

namespace Rock.ViewModels.Blocks.Lms.LearningProgramDetail
{
    /// <summary>
    /// The options bag containing the necessary configuration data for the LearningProgram Detail block.
    /// </summary>
    public class LearningProgramDetailOptionsBag
    {
        /// <summary>
        /// Gets or sets the available grading systems.
        /// </summary>
        public List<ListItemBag> GradingSystems { get; set; }

        /// <summary>
        /// Gets or sets the available system communications.
        /// </summary>
        public List<ListItemBag> SystemCommunications { get; set; }

        /// <summary>
        /// Gets or sets the DisplayMode of the block, "Summary" or "Detail".
        /// </summary>
        public string DisplayMode { get; set; }
    }
}
