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

using Newtonsoft.Json;

namespace Rock.AI.OpenAI.OpenAIApiClient.Classes.Moderations
{
    /// <summary>
    /// The Response object for moderation results category scores
    /// </summary>
    internal class OpenAIModerationsResponseResultsCategoryScores
    {
        #region Properties

        /// <summary>
        /// Hate category result
        /// </summary>
        [JsonProperty( "hate" )]
        public double Hate { get; set; }

        /// <summary>
        /// Hate/threatening category result
        /// </summary>
        [JsonProperty( "hate/threatening" )]
        public double Threatening { get; set; }

        /// <summary>
        /// Self-harm category result
        /// </summary>
        [JsonProperty( "self-harm" )]
        public double SelfHarm { get; set; }

        /// <summary>
        /// Sexual category result
        /// </summary>
        [JsonProperty( "sexual" )]
        public double Sexual { get; set; }

        /// <summary>
        /// Sexual/minors category result
        /// </summary>
        [JsonProperty( "sexual/minors" )]
        public double SexualMinors { get; set; }

        /// <summary>
        /// Violence category result
        /// </summary>
        [JsonProperty( "violence" )]
        public double Violence { get; set; }

        /// <summary>
        /// Violence/graphic category result
        /// </summary>
        [JsonProperty( "violence/graphic" )]
        public double ViolenceGraphic { get; set; }

        #endregion
    }
}
