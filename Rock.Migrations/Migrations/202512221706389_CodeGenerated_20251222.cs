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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class CodeGenerated_20251222 : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            // Add/Update BlockType 
            //   Name: Defined Type Detail
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedTypeDetail.ascx
            //   EntityType: -
            RockMigrationHelper.UpdateBlockType( "Defined Type Detail", "Displays the details of the given defined type.", "~/Blocks/Core/DefinedTypeDetail.ascx", "Core", "D5EDF24A-0453-4C0D-B7F0-DFAE48F89ED2" );

            // Add/Update BlockType 
            //   Name: Defined Type List
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedTypeList.ascx
            //   EntityType: -
            RockMigrationHelper.UpdateBlockType( "Defined Type List", "Lists all the defined types and allows for managing them and their values.", "~/Blocks/Core/DefinedTypeList.ascx", "Core", "D2C026ED-32F9-4D8F-A904-7ABBA5364D64" );

            // Add/Update BlockType 
            //   Name: Defined Value List
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedValueList.ascx
            //   EntityType: -
            RockMigrationHelper.UpdateBlockType( "Defined Value List", "Block for viewing values for a defined type.", "~/Blocks/Core/DefinedValueList.ascx", "Core", "0E7C1A29-CD6C-4D2D-BC82-80985C9736FA" );

            // Attribute for BlockType
            //   BlockType: Persisted Data View List
            //   Category: Reporting
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "3C4FAFAE-41D1-4FF2-B6DC-FF99CD4DABBE", "9C204CD0-1233-41C5-818A-C5DA439445AA", "core.CustomActionsConfigs", "core.CustomActionsConfigs", "core.CustomActionsConfigs", @"", 0, @"", "D7B93E80-8C72-44A4-BDDC-956C47D2171B" );

            // Attribute for BlockType
            //   BlockType: Persisted Data View List
            //   Category: Reporting
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "3C4FAFAE-41D1-4FF2-B6DC-FF99CD4DABBE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", @"", 0, @"True", "72FBC523-D3A8-4DDB-8D09-6FCFB9EC615B" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Demographics
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Demographics", "ShowDemographics", "Show Demographics", @"When enabled the Demographics panel will be displayed.", 0, @"true", "622600CF-D020-4570-A051-983466C33C7B" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Demographics > Connection Status
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Demographics > Connection Status", "ShowConnectionStatus", "Show Demographics > Connection Status", @"When enabled the Connection Status chart will be displayed on the Demographics panel.", 1, @"true", "CF0A2008-91B2-4057-87E6-2F84F7D24342" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Demographics > Age
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Demographics > Age", "ShowAge", "Show Demographics > Age", @"When enabled the Age chart will be displayed on the Demographics panel.", 2, @"true", "82E3D217-9A85-4443-A004-6034E6B78CED" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Demographics > Marital Status
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Demographics > Marital Status", "ShowMaritalStatus", "Show Demographics > Marital Status", @"When enabled the Marital Status chart will be displayed on the Demographics panel.", 3, @"true", "9E05920A-0720-48F3-9E33-C2E8E1B1901E" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Demographics > Gender
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Demographics > Gender", "ShowGender", "Show Demographics > Gender", @"When enabled the Gender chart will be displayed on the Demographics panel.", 4, @"true", "EEF4B759-8E1C-4546-8F45-C1D3D08197AF" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Information Statistics
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Information Statistics", "ShowInformationStatistics", "Show Information Statistics", @"When enabled the Information Statistics panel will be displayed.", 7, @"true", "228F1561-F821-4662-9555-3B96F6C2F2BC" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Information Statistics > Information Completeness
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Information Statistics > Information Completeness", "ShowInformationCompleteness", "Show Information Statistics > Information Completeness", @"When enabled the Information Completeness chart will be displayed on the Information Statistics panel.", 8, @"true", "1215D4D0-5A92-43F8-8848-4249564DADD0" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Information Statistics > % of Active Individuals with Assessments
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Information Statistics > % of Active Individuals with Assessments", "ShowPercentageOfActiveIndividualsWithAssessments", "Show Information Statistics > % of Active Individuals with Assessments", @"When enabled the % of Active Individuals with Assessments chart will be displayed on the Information Statistics panel.", 9, @"true", "EA250D2E-E0A0-42C5-8F5B-6A4B7067A07D" );

            // Attribute for BlockType
            //   BlockType: Insights
            //   Category: Reporting
            //   Attribute: Show Information Statistics > Record Statuses
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B215F5FA-410C-4674-8C47-43DC40AF9F67", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Information Statistics > Record Statuses", "ShowRecordStatuses", "Show Information Statistics > Record Statuses", @"When enabled the Record Statuses chart will be displayed on the Information Statistics panel.", 10, @"true", "8EA9BB7E-45BB-4992-A2A0-4997E0218DA6" );

            // Attribute for BlockType
            //   BlockType: Page Search
            //   Category: CMS
            //   Attribute: Additional Content Template
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "A279A88E-D4E0-4867-A108-2AA743B3CFD0", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Additional Content Template", "AdditionalContentTemplate", "Additional Content Template", @"Lava template to use to display additional content on the page. This will be displayed in the sidebar below the section list.", 0, @"", "CBDC5F56-987D-4458-9688-79CE25DAC490" );

            // Attribute for BlockType
            //   BlockType: Defined Type Detail
            //   Category: Core
            //   Attribute: Defined Type
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "D5EDF24A-0453-4C0D-B7F0-DFAE48F89ED2", "BC48720C-3610-4BCF-AE66-D255A17F1CDF", "Defined Type", "DefinedType", "Defined Type", @"If a Defined Type is set, only details for it will be displayed (regardless of the querystring parameters).", 0, @"", "739E0765-6003-43CB-B162-358E1A5EE7DD" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: Detail Page
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "D2C026ED-32F9-4D8F-A904-7ABBA5364D64", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "Detail Page", @"", 0, @"", "CABDA513-0E45-4C8D-9F16-95001E6680EA" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: Categories
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "D2C026ED-32F9-4D8F-A904-7ABBA5364D64", "775899FB-AC17-4C2C-B809-CF3A1D2AA4E1", "Categories", "Categories", "Categories", @"If block should only display Defined Types from specific categories, select the categories here.", 1, @"", "24012177-D6E4-4440-BC01-9D6B98864DD6" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "D2C026ED-32F9-4D8F-A904-7ABBA5364D64", "9C204CD0-1233-41C5-818A-C5DA439445AA", "core.CustomActionsConfigs", "core.CustomActionsConfigs", "core.CustomActionsConfigs", @"", 0, @"", "B71A7BD8-746D-460B-8FED-D676CC690FB8" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "D2C026ED-32F9-4D8F-A904-7ABBA5364D64", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", @"", 0, @"True", "7862403C-F981-425C-9653-C37EEFD16EBC" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: Defined Type
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "0E7C1A29-CD6C-4D2D-BC82-80985C9736FA", "BC48720C-3610-4BCF-AE66-D255A17F1CDF", "Defined Type", "DefinedType", "Defined Type", @"If a Defined Type is set, only its Defined Values will be displayed (regardless of the querystring parameters).", 0, @"", "C8EBFF10-643F-4231-ADB9-0087EE4E47E5" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "0E7C1A29-CD6C-4D2D-BC82-80985C9736FA", "9C204CD0-1233-41C5-818A-C5DA439445AA", "core.CustomActionsConfigs", "core.CustomActionsConfigs", "core.CustomActionsConfigs", @"", 0, @"", "AD5DD7C3-872B-49E3-AD4D-331D1095C5F5" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "0E7C1A29-CD6C-4D2D-BC82-80985C9736FA", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", "core.EnableDefaultWorkflowLauncher", @"", 0, @"True", "7D98E7A7-E417-4415-BEB2-7EE801414C13" );
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Attribute for BlockType
            //   BlockType: Persisted Data View List
            //   Category: Reporting
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.DeleteAttribute( "72FBC523-D3A8-4DDB-8D09-6FCFB9EC615B" );

            // Attribute for BlockType
            //   BlockType: Persisted Data View List
            //   Category: Reporting
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.DeleteAttribute( "D7B93E80-8C72-44A4-BDDC-956C47D2171B" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.DeleteAttribute( "7D98E7A7-E417-4415-BEB2-7EE801414C13" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.DeleteAttribute( "AD5DD7C3-872B-49E3-AD4D-331D1095C5F5" );

            // Attribute for BlockType
            //   BlockType: Defined Value List
            //   Category: Core
            //   Attribute: Defined Type
            RockMigrationHelper.DeleteAttribute( "C8EBFF10-643F-4231-ADB9-0087EE4E47E5" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: core.EnableDefaultWorkflowLauncher
            RockMigrationHelper.DeleteAttribute( "7862403C-F981-425C-9653-C37EEFD16EBC" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: core.CustomActionsConfigs
            RockMigrationHelper.DeleteAttribute( "B71A7BD8-746D-460B-8FED-D676CC690FB8" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: Categories
            RockMigrationHelper.DeleteAttribute( "24012177-D6E4-4440-BC01-9D6B98864DD6" );

            // Attribute for BlockType
            //   BlockType: Defined Type List
            //   Category: Core
            //   Attribute: Detail Page
            RockMigrationHelper.DeleteAttribute( "CABDA513-0E45-4C8D-9F16-95001E6680EA" );

            // Attribute for BlockType
            //   BlockType: Defined Type Detail
            //   Category: Core
            //   Attribute: Defined Type
            RockMigrationHelper.DeleteAttribute( "739E0765-6003-43CB-B162-358E1A5EE7DD" );

            // Attribute for BlockType
            //   BlockType: Page Search
            //   Category: CMS
            //   Attribute: Additional Content Template
            RockMigrationHelper.DeleteAttribute( "CBDC5F56-987D-4458-9688-79CE25DAC490" );

            // Delete BlockType 
            //   Name: Defined Value List
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedValueList.ascx
            //   EntityType: -
            RockMigrationHelper.DeleteBlockType( "0E7C1A29-CD6C-4D2D-BC82-80985C9736FA" );

            // Delete BlockType 
            //   Name: Defined Type List
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedTypeList.ascx
            //   EntityType: -
            RockMigrationHelper.DeleteBlockType( "D2C026ED-32F9-4D8F-A904-7ABBA5364D64" );

            // Delete BlockType 
            //   Name: Defined Type Detail
            //   Category: Core
            //   Path: ~/Blocks/Core/DefinedTypeDetail.ascx
            //   EntityType: -
            RockMigrationHelper.DeleteBlockType( "D5EDF24A-0453-4C0D-B7F0-DFAE48F89ED2" );
        }
    }
}
