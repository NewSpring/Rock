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
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Data;
using Rock.Model;
using Rock.Tests.Integration.Modules.Core.Lava;
using Rock.Tests.Integration.TestData;

namespace Rock.Tests.Integration.BugFixes
{
    /// <summary>
    /// Tests that verify specific bug fixes for a Rock version.
    /// </summary>
    /// <remarks>
    /// These tests are developed to verify bugs and fixes that are difficult or time-consuming to reproduce.
    /// They are only relevant to the Rock version in which the bug is fixed, and should be removed in subsequent versions.
    /// </remarks>
    /// 
    [TestClass]
    [RockObsolete("1.14")]
    public class BugFixVerificationTests_v14 : LavaIntegrationTestBase
    {
        [TestMethod]
        public void Issue5324_CommunicationListTimeout_CreateTestData()
        {
            /* Creates several very large Communication Lists needed to test this issue.
             * Requires an existing sample database with 50,000+ person records.
             */

            // Get Person identifiers for the list members.
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var personIdList = personService.Queryable().Take( 50000 ).Select( p => p.Id.ToString() ).ToList();

            if ( personIdList.Count < 50000 )
            {
                Assert.Inconclusive( "There are insufficient Person records in the current database to create the Communication List." );
            }

            // Create List 1
            CreateTestCommunicationList( "Test Communication List 1",
                new Guid( "8AD585F0-6CA3-4C8D-9294-9217B06CD4AA" ),
                personIdList );
            CreateTestCommunicationList( "Test Communication List 2",
                new Guid( "88B4DA2B-D0D4-4D91-A320-A6F6D0FBBBA8" ),
                personIdList );
            CreateTestCommunicationList( "Test Communication List 3",
                new Guid( "93EF1A39-79F2-4F22-A102-FB6DF5DFD429" ),
                personIdList );
        }

        private void CreateTestCommunicationList( string name, Guid guid, List<string> personIdList )
        {
            var listArgs = new TestDataHelper.Communications.CreateCommunicationListArgs
            {
                ExistingItemStrategy = CreateExistingItemStrategySpecifier.Replace,
                Name = name,
                ForeignKey = "IntegrationTest",
                Guid = guid
            };

            var listGroup = TestDataHelper.Communications.CreateCommunicationList( listArgs );

            var addPeopleArgs = new TestDataHelper.Communications.CommunicationListAddPeopleArgs
            {
                CommunicationListGroupIdentifier = listGroup.Id.ToString(),
                ForeignKey = "IntegrationTest",
                PersonIdentifiers = personIdList
            };

            var addCount = TestDataHelper.Communications.AddPeopleToCommunicationList( addPeopleArgs );

            System.Diagnostics.Debug.WriteLine( $"Added {addCount} people to communication list \"{listGroup.Name}\"." );
        }
    }
}
