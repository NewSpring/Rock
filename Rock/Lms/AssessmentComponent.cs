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
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Newtonsoft.Json.Linq;

using Rock.Model;

namespace Rock.Lms
{
    /// <summary>
    /// The Assessment activity is an activity that requires a partcipant to complete multiple items.
    /// </summary>
    [Description( "A Learning Activity that requires a partcipant to complete multiple items." )]
    [Export( typeof( LearningActivityComponent ) )]
    [ExportMetadata( "ComponentName", "Assessment" )]

    [Rock.SystemGuid.EntityTypeGuid( "a585c101-02e8-4953-bf77-c783c7cfdfdc" )]
    public class AssessmentComponent : LearningActivityComponent
    {
        private const string SHORT_ANSWER_ITEM_TYPE_NAME = "Short Answer";
        private const string MULTIPLE_CHOICE_ITEM_TYPE_NAME = "Multiple Choice";

        /// <summary>
        /// Gets the Highlight color for the component.
        /// </summary>
        public override string HighlightColor => "#a9551d";

        /// <summary>
        /// Gets the icon CSS class for the component.
        /// </summary>
        public override string IconCssClass => "fa fa-list";

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string Name => "Assessment";

        /// <inheritdoc/>
        public override string ComponentUrl => @"/Obsidian/Controls/Internal/LearningActivity/assessmentLearningActivity.obs";

        /// <summary>
        /// Calculates the student grade based on the configured multiple choice responses and weights.
        /// Includes the calculations for the facilitator graded Short Answer items as well (if completed).
        /// </summary>
        /// <param name="configurationJson">The JSON string of the components configuration.</param>
        /// <param name="completionJson">The JSON string of the components completion.</param>
        /// <param name="pointsPossible">The total number of points possible for this activity.</param>
        /// <returns>The actual earned points for this activity.</returns>
        public override int CalculatePointsEarned( string configurationJson, string completionJson, int pointsPossible )
        {
            var multipleChoiceSectionPoints = GetMultipleChoiceSectionPoints( configurationJson, completionJson, pointsPossible );
            var shortAnswerSectionPoints = GetShortAnswerSectionPoints( configurationJson, completionJson );

            return multipleChoiceSectionPoints + shortAnswerSectionPoints;
        }

        /// <summary>
        /// Adds the correctAnswer from the configuration JSON back
        /// to the completion JSON for any multiple choice questions.
        /// </summary>
        /// <remarks>
        /// This method ensures the
        /// <see cref="LearningActivityCompletion.ActivityComponentCompletionJson"/>
        /// contains all the configuration data necessary to
        /// correctly display the assessment item in the event
        /// that the configuration data is later changed.
        /// </remarks>
        /// <param name="completionJson">The JSON string of the components completion.</param>
        /// <param name="configurationJson">The JSON string of the components configuration.</param>
        /// <returns></returns>
        public override string GetCompletionJsonToPersist( string completionJson, string configurationJson )
        {
            try
            {
                // Get the uniqueIds of the multiple choice assessment items.
                var uniqueIdPath = $"$.completedItems[?(@.typeName == '{MULTIPLE_CHOICE_ITEM_TYPE_NAME}')].uniqueId";
                var completionJObject = JObject.Parse( completionJson );
                var uniqueIds = completionJObject.SelectTokens( uniqueIdPath );

                // Get the correctAnsers from the configurationJson.
                var correctAnswerPathTemplate = $"$.items[?(@.uniqueId == '@uniqueId')].correctAnswer";
                var configurationJObject = JObject.Parse( configurationJson );

                foreach ( var uniqueId in uniqueIds )
                {
                    var correctAnswer = configurationJObject.SelectToken( $"$.items[?(@.uniqueId == '{uniqueId}')].correctAnswer" );

                    if ( correctAnswer == null )
                    {
                        continue;
                    }
                    
                    var completionItem = completionJObject.SelectToken( $"$.completedItems[?(@.uniqueId == '{uniqueId}')]" );

                    if (completionItem != null )
                    {
                        completionItem["correctAnswer"] = correctAnswer.ToString();
                    }
                }

                return completionJObject.ToJson();
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }

            return completionJson;
        }

        /// <summary>
        /// Requires grading if there are any short answer questions.
        /// </summary>
        /// <param name="completion">The completion record to evaluate.</param>
        /// <returns><c>true</c> if the completion's configuration has short answer items; otherwise <c>false</c>.</returns>
        public override bool RequiresGrading( LearningActivityCompletion completion )
        {
            if ( completion.GradedByPersonAliasId.HasValue )
            {
                return false;
            }

            var completionHasShortAnswer = HasShortAnswerItems( completion.ActivityComponentCompletionJson, "completedItems" );

            if ( completionHasShortAnswer.HasValue )
            {
                return completionHasShortAnswer.Value;
            }

            return HasShortAnswerItems( completion.LearningActivity?.ActivityComponentSettingsJson, "items" ) ?? false;
        }

        /// <summary>
        /// Removes the isCorrect flag from any multiple choice assessment items.
        /// </summary>
        /// <param name="configurationJson"></param>
        /// <returns>The json string stripped of any information that might identify correct answers.</returns>
        public override string StudentScrubbedConfiguration( string configurationJson )
        {
            try
            {
                var correctAnswerPath = $"$.items[?(@.typeName == '{MULTIPLE_CHOICE_ITEM_TYPE_NAME}')].correctAnswer";
                var jObject = JObject.Parse( configurationJson );
                var correctAnswers = jObject.SelectTokens( correctAnswerPath );

                foreach ( var correctAnswer in correctAnswers )
                {
                    correctAnswer.Parent.Remove();
                }

                return jObject.ToJson();
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }

            // If there was an error don't return anything (to prevent leaking answers).
            return string.Empty;
        }

        /// <summary>
        /// Parses the configuration and completion Jsons and calculates the points earned for the multiple choice section of the assessment.
        /// </summary>
        /// <param name="configurationJson">The JSON string of the components configuration.</param>
        /// <param name="completionJson">The JSON string of the components completion.</param>
        /// <param name="pointsPossible">The total number of points possible for this activity.</param>
        /// <returns>The actual earned points for the multiple choice section of the assessment.</returns>
        private int GetMultipleChoiceSectionPoints( string configurationJson, string completionJson, int pointsPossible )
        {
            try
            {
                var correctMultipleChoiceItems = 0;
                const string multipleChoiceItemTypeName = "Multiple Choice";
                var itemsPath = $"$.items[?(@.typeName == '{multipleChoiceItemTypeName}')]";

                var config = JObject.Parse( configurationJson );
                var completion = JObject.Parse( completionJson );

                var configuredItems = config.SelectTokens( itemsPath );
                var multipleChoiceWeight = config.SelectToken( "multipleChoiceWeight" )?.ToObject<decimal>() ?? 0;

                foreach ( var question in configuredItems )
                {
                    var questionId = question.SelectToken( "uniqueId" )?.ToObject<string>() ?? string.Empty;

                    if ( questionId == string.Empty )
                    {
                        continue;
                    }

                    var correctAnswer = question["correctAnswer"]?.ToStringSafe();
                    var response = completion.SelectToken( $"$.completedItems[?(@.uniqueId  == '{questionId}')].response" )?.ToObject<string>() ?? string.Empty;

                    if ( correctAnswer.Equals( response, StringComparison.OrdinalIgnoreCase ) )
                    {
                        correctMultipleChoiceItems++;
                    }
                }

                decimal multipleChoiceItemCount = configuredItems.Count();
                var sectionWeight = multipleChoiceWeight / 100;
                var availablePoints = pointsPossible * sectionWeight;
                var percentCorrect = correctMultipleChoiceItems / multipleChoiceItemCount;
                var pointsEarned = availablePoints * percentCorrect;
                return (int)pointsEarned;
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }

            return 0;
        }

        /// <summary>
        /// Parses the configuration and completion Jsons and calculates the points earned for the short answer section of the assessment.
        /// </summary>
        /// <param name="configurationJson">The JSON string of the components configuration.</param>
        /// <param name="completionJson">The JSON string of the components completion.</param>
        /// <returns>The actual earned points for the short answer section of the assessment.</returns>
        private int GetShortAnswerSectionPoints( string configurationJson, string completionJson )
        {
            var pointsEarned = 0;
            try
            {
                var itemsPath = $"$.items[?(@.typeName == '{SHORT_ANSWER_ITEM_TYPE_NAME}')]";

                var config = JObject.Parse( configurationJson );
                var completion = JObject.Parse( completionJson );

                var configuredItems = config.SelectTokens( itemsPath ); ;
                
                foreach ( var question in configuredItems )
                {
                    var questionId = question.SelectToken( "uniqueId" )?.ToObject<string>() ?? string.Empty;
                    
                    if ( questionId == string.Empty )
                    {
                        continue;
                    }

                    var faciltatorScore = completion.SelectToken( $"$.completedItems[?(@.uniqueId  == '{questionId}')].pointsEarned" )?.ToObject<int?>();

                    if (faciltatorScore.HasValue )
                    {
                        pointsEarned += faciltatorScore.Value;
                    }
                }

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }

            return pointsEarned;
        }

        /// <summary>
        /// Checks the configuration JSON for any items of the "Short Answer" type.
        /// </summary>
        /// <param name="configurationJson">The raw string of configuration JSON data to evaluate.</param>
        /// <param name="itemsPropertyNames">The name of the property containing the items.</param>
        /// <returns><c>true</c>If able to positively determine the configuration contains "Short Answer" items; otherwise <c>false</c>.</returns>
        private bool? HasShortAnswerItems( string configurationJson, string itemsPropertyNames )
        {
            try
            {
                var config = JObject.Parse( configurationJson );
                var itemsPath = $"$.{itemsPropertyNames}[?(@.typeName == '{SHORT_ANSWER_ITEM_TYPE_NAME}')]";
                var shortAnswerItems = config.SelectTokens( itemsPath );
                return shortAnswerItems.Any();

            }
            catch
            {
                return null;
            }
        }

    }
}
