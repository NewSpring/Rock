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

namespace Rock.ViewModels.Blocks.Prayer.PrayerRequestDetail
{
    /// <summary>
    /// 
    /// </summary>
    public class PrayerRequestDetailOptionsBag
    {
        /// <summary>
        /// Pass the value of the RequireLastName block attribute to the front end
        /// </summary>
        public bool IsLastNameRequired { get; set; }

        /// <summary>
        /// Pass the value of the RequireCampus block attribute to the front end
        /// </summary>
        public bool IsCampusRequired { get; set; }

        /// <summary>
        /// Pass the EnableAIDisclaimer block attribute to the front end
        /// </summary>
        public bool IsAIDisclaimerEnabled { get; set; }

        /// <summary>
        /// Pass the AIDisclaimer block attribute to the front end
        /// </summary>
        public string AIDisclaimer { get; set; }
    }
}
