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

using Rock.ViewModels.Blocks.Lms.LearningGradingSystemScaleDetail;

namespace Rock.ViewModels.Blocks.Lms.LearningClassActivityCompletionDetail
{
    /// <summary>
    /// The additional configuration options for the Learning Class Activity Completion Detail block.
    /// </summary>
    public class LearningClassActivityCompletionDetailOptionsBag
    {
        /// <summary>
        /// Gets or sets the grading scales for the Learning Class.
        /// </summary>
        public List<LearningGradingSystemScaleBag> GradingScales { get; set; }
    }
}
