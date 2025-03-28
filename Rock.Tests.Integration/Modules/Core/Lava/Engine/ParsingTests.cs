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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Lava;
using Rock.Lava.Fluid;
using Rock.Tests.Shared;
using Rock.Tests.Shared.Lava;

namespace Rock.Tests.Integration.Modules.Core.Lava.Engine
{
    /// <summary>
    /// Test the compatibility of the Lava parser with the Liquid language syntax.
    /// </summary>
    [TestClass]
    public class LiquidLanguageCompatibilityTests : LavaIntegrationTestBase
    {
        [TestMethod]
        public void Whitespace_TagLeftAndRight_ProducesCorrectOutput()
        {
            var input = @"
{%- assign now = 'Now' | Date:'yyyy-MM-ddTHH:mm:sszzz' | AsDateTime -%}
";
            var expectedOutput = @"";

            TestHelper.AssertTemplateOutput( expectedOutput, input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        public void Variables_VariableNamesThatDifferOnlyByCase_AreReferencedAsDifferentVariables()
        {
            var input = @"
{%- assign text = 'lowercase' -%}
{%- assign TEXT = 'uppercase' -%}
Text (lower) = {{ text }}, Text (upper) = {{ TEXT }}
";
            var expectedOutput = @"Text (lower) = lowercase, Text (upper) = uppercase";

            TestHelper.AssertTemplateOutput( expectedOutput, input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        public void Variables_VariableNameBeginningWithNumber_IsValid()
        {
            TestHelper.AssertTemplateOutput( "first",
                "{% assign 1st = 'first' %}{{ 1st }}",
                new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        public void Whitespace_TrimInOutputTagWithVariable_RemovesWhitespace()
        {
            var input = @"{%- assign text = 'hello' -%}--> {{- text -}} <--";
            var expectedOutput = @"-->hello<--";

            TestHelper.AssertTemplateOutput( expectedOutput, input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        [Ignore("Not supported in Fluid. The empty output tag throws a parsing error.")]
        public void Whitespace_TrimInEmptyOutputTag_RemovesWhitespace()
        {
            var input = @"{{- -}}";
            var expectedOutput = @"";

            TestHelper.AssertTemplateOutput( expectedOutput, input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        public void Whitespace_TrimInOutputTagWithEmptyString_RemovesWhitespace()
        {
            var input = @"--> {{- '' -}} <--";
            var expectedOutput = @"--><--";

            TestHelper.AssertTemplateOutput( expectedOutput, input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        /// <summary>
        /// Verify the operation of the whitespace trim character (-) when used in a comment tag.
        /// </summary>
        /// <remarks>
        /// This represents valid Liquid syntax that failed to parse in Fluid v1.
        /// The behavior has been fixed in Fluid v2.
        /// </remarks>
        [TestMethod]
        public void Whitespace_TrimInCommentTag_RemovesWhitespace()
        {
            var input = @"
-->  {%- comment %}  Comment text.  {% endcomment -%}  <--
";

            TestHelper.AssertTemplateOutput( "--><--", input, new LavaTestRenderOptions { IgnoreWhiteSpace = false } );
        }

        [TestMethod]
        public void Keywords_ElseIfKeyword_IsParsedAsElsIf()
        {
            var input = @"
{% assign speed = 50 %}
{% if speed > 70 -%}
Fast
{% elseif speed > 30 -%}
Moderate
{% else -%}
Slow
{% endif -%}
";
            var expectedOutput = @"Moderate";

            TestHelper.AssertTemplateOutput( expectedOutput, input );
        }

        [DataTestMethod]
        [DataRow( "true | AsBoolean", true )]
        [DataRow( "'true'", true )]
        [DataRow( "''", true )]
        public void Operators_IfWithNoOperatorAndAnyDefinedValue_ReturnsTrue( string value, bool expectedResult )
        {
            var input = @"
{% assign value = $value %}
{% if value %}true{% else %}false{% endif %}
";
            input = input.Replace( "$value", value );
            TestHelper.AssertTemplateOutput( expectedResult.ToString().ToLower(), input );
        }

        [TestMethod]
        public void Operators_IfWithNoOperatorAndUndefinedVariable_ReturnsFalse()
        {
            var input = @"
{% if noVariable %}true{% else %}false{% endif %}
";

            TestHelper.AssertTemplateOutput( "false", input );
        }

        [TestMethod]
        public void Operators_IfWithNoOperatorAndNullVariable_ReturnsFalse()
        {
            var input = @"
{% if value %}true{% else %}false{% endif %}
";

            var options = new LavaTestRenderOptions();
            options.MergeFields = new Dictionary<string, object> { { "value", null } };
            TestHelper.AssertTemplateOutput( "false", input, options  );
        }

        [TestMethod]
        public void Tags_RawTagWithEmbeddedTag_ReturnsLiteralTagText()
        {
            var inputTemplate = @"
{% raw %}{% assign test = 'hello' %}{% endraw %}
";

            TestHelper.AssertTemplateOutput( "{% assign test = 'hello' %}", inputTemplate );
        }

        /// <summary>
        /// Verify that a comment tag correctly ignores any other tags that are contained within it.
        /// </summary>
        [TestMethod]
        public void Tags_CommentTagContainingInvalidRawTag_IsParsedCorrectly()
        {
            var inputTemplate = @"
Comment-->
{% comment %}Open-ended tag-->{% raw %}, Invalid tag-->{% invalid_tag %}{% endcomment %}
<--Comment
";

            TestHelper.AssertTemplateOutput( "Comment--><--Comment", inputTemplate );
        }

        [TestMethod]
        public void LavaToLiquidConverter_LavaTemplateWithElseIfKeyword_IsReplacedWithElsif()
        {
            var input = @"
{% assign speed = 50 %}
{% if speed > 70 -%}
Fast
{% elseif speed > 30 -%}
Moderate
{% else -%}
Slow
{% endif -%}
";
            var expectedOutput = @"
{% assign speed = 50 %}
{% if speed > 70 -%}
Fast
{% elsif speed > 30 -%}
Moderate
{% else -%}
Slow
{% endif -%}
";

            var converter = new LavaToLiquidTemplateConverter();

            var output = converter.ReplaceElseIfKeyword( input );

            Assert.That.AreEqual( expectedOutput, output );
        }

        [TestMethod]
        public void LavaToLiquidConverter_LavaShortcodeWithMultipleParameters_IsReplacedWithRenamedBlock()
        {
            var input = @"{[ shortcodetest fontname:'Arial' fontsize:'{{ fontsize }}' fontbold:'true' ]}{[ endshortcodetest ]}";
            var expectedOutput = @"{% shortcodetest_ fontname:'Arial' fontsize:'{{ fontsize }}' fontbold:'true' %}{% endshortcodetest_ %}";

            var converter = new LavaToLiquidTemplateConverter();

            var output = converter.ReplaceTemplateShortcodes( input );

            Assert.That.AreEqualIgnoreWhitespace( expectedOutput, output );
        }

        [TestMethod]
        public void LavaToLiquidConverter_LavaShortcodeWithNoParameters_IsReplacedWithRenamedBlock()
        {
            var input = @"{[ shortcodetest ]}{[ endshortcodetest ]}";
            var expectedOutput = @"{% shortcodetest_ %}{% endshortcodetest_ %}";

            var converter = new LavaToLiquidTemplateConverter();

            var output = converter.ReplaceTemplateShortcodes( input );

            Assert.That.AreEqualIgnoreWhitespace( expectedOutput, output );
        }
    }

    /// <summary>
    /// Test the compatibility of the Lava parser with the Liquid language syntax.
    /// </summary>
    [TestClass]
    public class ParameterParsingTests : LavaIntegrationTestBase
    {
        [TestMethod]
        public void BlockParameters_WithDelimiterInParameterLavaValue_EvaluatesValueCorrectly()
        {
            TestHelper.ExecuteForActiveEngines( ( engine ) =>
                {
                    var parameterString = "workflowtype:'51FE9641-FB8F-41BF-B09E-235900C3E53E' workflowname:'{{WorkflowName}}'";

                    var mergeFields = new LavaDataDictionary
                    {
                        { "WorkflowName", "Ted's Workflow" }
                    };

                    var context = engine.NewRenderContext( mergeFields );
                    var settings = LavaElementAttributes.NewFromMarkup( parameterString, context );

                    Assert.That.AreEqual( "Ted's Workflow", settings.GetString( "workflowname" ) );
                } );
        }

        [TestMethod]
        public void BlockParameters_NamesThatDifferOnlyByCase_AreReferencedAsTheSameParameter()
        {
            TestHelper.ExecuteForActiveEngines( ( engine ) =>
            {
                var parameterString = "param1:'1' PARAM2:'2'";

                var context = engine.NewRenderContext();
                var settings = LavaElementAttributes.NewFromMarkup( parameterString, context );

                Assert.That.AreEqual( "2", settings.GetString( "param2" ) );
            } );
        }

        [TestMethod]
        public void BlockParameters_WithLavaOutputTagContainingDelimiters_IsParsedCorrectly()
        {
            TestHelper.ExecuteForActiveEngines( ( engine ) =>
            {
                var parameterString = @"where:'ContentChannelId == 1 && Title == `{{ 'Blog Posts' }}`' iterator:'items' sort:'StartDateTime'"
                        .Replace( "`", @"""" );

                var context = engine.NewRenderContext();
                var settings = LavaElementAttributes.NewFromMarkup( parameterString, context );

                Assert.That.AreEqual( "ContentChannelId == 1 && Title == `Blog Posts`".Replace("`", @""""), settings.GetStringOrNull( "where" ) );
                Assert.That.AreEqual( "items", settings.GetStringOrNull( "iterator" ) );
                Assert.That.AreEqual( "StartDateTime", settings.GetStringOrNull( "sort" ) );
            } );
        }

        [TestMethod]
        public void BlockParameters_WithUndelimitedParameterValue_IsParsedCorrectly()
        {
            TestHelper.ExecuteForActiveEngines( ( engine ) =>
            {
                var parameterString = @"param1:1 param2:'2' param3:abc"
                        .Replace( "`", @"""" );

                var context = engine.NewRenderContext();
                var settings = LavaElementAttributes.NewFromMarkup( parameterString, context );

                Assert.That.AreEqual( "1", settings.GetStringOrNull( "param1" ) );
                Assert.That.AreEqual( "2", settings.GetStringOrNull( "param2" ) );
                Assert.That.AreEqual( "abc", settings.GetStringOrNull( "param3" ) );
            } );
        }

        [TestMethod]
        public void BlockParameters_WithEmptyParameterValue_IsParsedCorrectly()
        {
            TestHelper.ExecuteForActiveEngines( ( engine ) =>
            {
                var parameterString = @"param1:'' param2:";

                var context = engine.NewRenderContext();
                var settings = LavaElementAttributes.NewFromMarkup( parameterString, context );

                Assert.That.AreEqual( "", settings.GetStringOrNull( "param1" ) );
                Assert.That.AreEqual( "", settings.GetStringOrNull( "param2" ) );
            } );
        }
    }
}
