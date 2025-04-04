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
namespace Rock.Migrations
{

    /// <summary>
    ///
    /// </summary>
    public partial class RemindersUpdates : Rock.Migrations.RockMigration
    {
        private const string REMINDER_LIST_BLOCK_INSTANCE = "8CD5AA01-88F3-4D7B-B25A-92280792451E";
        private const string REMINDER_EDIT_BLOCK_INSTANCE = "8987B121-AA92-4562-B3CD-196CE0CC3B15";
        private const string REMINDER_LINKS_BLOCK_INSTANCE = "A5F41693-2A52-4C78-AC3C-69A504D896D3";
        private const string REMINDER_TYPES_BLOCK_INSTANCE = "64BED7FE-2F5B-4270-B045-9AE069E98DDD";
        private const string REMINDERS_BUTTON_BLOCK_INSTANCE = "C2F5EF35-7F40-426A-8DD0-60A131B19BA5";
        private const string REMINDERS_BUTTON_HTML_CONTENT = "45623F45-2FF6-4DB4-B22F-8CC3E6496445";
        private const string REMINDERS_ROUTE_1 = "8990DCBB-2AD7-4FD6-A62C-EB4888338997";
        private const string REMINDERS_ROUTE_2 = "92F0EBC1-F5A2-4604-A5B8-B04AFCC40CC0";
        private const string REMINDERS_ROUTE_3 = "720A959A-093A-4CBE-BCA5-B26337DFC5A5";
        private const string REMINDERS_ROUTE_4 = "ADDFC440-A803-4F97-8E11-20C62519B1D4";
        private const string REMINDERS_ROUTE_5 = "7D31D260-32CC-41E7-8A32-9DE5E0FD44D6";
        private const string REMINDER_TYPES_ROUTE = "D00C20AB-90E2-4FFE-8221-89466DCD4EE5";
        private const string CRON_EXPRESSION = "0 0 4 1/1 * ? *"; // 4am daily.

        private const string SHIFT_BLOCK_ORDER_QUERY = @"
-- Update order of blocks in Site Header so that ReminderLinks block is first.
IF NOT EXISTS( SELECT [Id] FROM [Block] WHERE [Guid] = 'A5F41693-2A52-4C78-AC3C-69A504D896D3' )
BEGIN
	UPDATE	[Block]
	SET		[Order] = [Order] + 1
	WHERE	[SiteId] = (SELECT [Id] FROM [Site] WHERE [Guid] = 'C2D29296-6A87-47A9-A753-EE4E9159C4C4')
		AND	[Zone] = 'Header';
END

-- Update order of blocks in Sidebar1 section of Dashboard page so that Reminders button will be first.
IF NOT EXISTS( SELECT [Id] FROM [Block] WHERE [Guid] = 'C2F5EF35-7F40-426A-8DD0-60A131B19BA5' )
BEGIN
	UPDATE	[Block]
	SET		[Order] = [Order] + 1
	WHERE	[PageId] = (SELECT [Id] FROM [Page] WHERE [Guid] = 'AE1818D8-581C-4599-97B9-509EA450376A')
		AND	[Zone] = 'Sidebar1'
END";

        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            AddReminderCommunicationTemplate();
            AddBlockTypes();
            AddReminderLinksBlockAndDashboardButton();
            AddReminderListPage();
            AddReminderEditPage();
            AddReminderTypesPage();
            AddRemindersJob();
        }

        private void AddReminderCommunicationTemplate()
        {
            string emailSubject = "Reminder: {{ ReminderType.Name }} for {{ EntityName }} on {{ ReminderDate|Date: 'ddd,MMMM d,yyyy' }}";
            string emailBody = @"{{ 'Global' | Attribute:'EmailHeader' }}

<h1>System Reminder</h1>

<p>Hi {{  Person.NickName  }}!</p>

<p>This is a ""{{ ReminderType.Name }}"" reminder for {{ EntityName }} on {{ Reminder.ReminderDate | Date:'dddd, MMMM d, yyyy' }}.</p>

{% if ReminderType.ShouldShowNote %}
  <p>Reminder Note:<br />
    {{ Reminder.Note }}
  </p>
{% endif %}

<p>Thanks!</p>

<p>{{ 'Global' | Attribute:'OrganizationName' }}</p>

{{ 'Global' | Attribute:'EmailFooter' }}";

            string smsMessage = "This is a reminder from {{ 'Global' | Attribute:'OrganizationName' }} that you have a \"{{ ReminderType.Name }}\" reminder for {{ EntityName }} on {{ Reminder.ReminderDate | Date:'dddd, MMMM d, yyyy' }}.";
            string pushTitle = "{{ ReminderType.Name }} Reminder";
            string pushMessage = "You have a \"{{ ReminderType.Name }}\" reminder for {{ EntityName }} on {{ Reminder.ReminderDate | Date:'dddd, MMMM d, yyyy' }}.";

            RockMigrationHelper.UpdateSystemCommunication(
                "Reminders",
                "Reminder Notification",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                emailSubject,
                emailBody,
                SystemGuid.SystemCommunication.REMINDER_NOTIFICATION,
                true,
                smsMessage,
                null,
                pushTitle,
                pushMessage );
        }

        #region Blocks, Pages, and Routes

        private void AddBlockTypes()
        {
            RockMigrationHelper.UpdateBlockType(
                "Reminder Links",
                "This block is used to show reminder links.",
                "~/Blocks/Reminders/ReminderLinks.ascx",
                "Core",
                SystemGuid.BlockType.REMINDER_LINKS );

            RockMigrationHelper.UpdateBlockType(
                "Reminder List",
                "Block to show a list of reminders.",
                "~/Blocks/Reminders/ReminderList.ascx",
                "Core",
                SystemGuid.BlockType.REMINDER_LIST );

            RockMigrationHelper.UpdateBlockType(
                "Reminder Edit",
                "Block for editing reminders.",
                "~/Blocks/Reminders/ReminderEdit.ascx",
                "Core",
                SystemGuid.BlockType.REMINDER_EDIT );

            RockMigrationHelper.UpdateBlockType(
                "Reminder Types",
                "Block for editing reminder types.",
                "~/Blocks/Reminders/ReminderTypes.ascx",
                "Core",
                SystemGuid.BlockType.REMINDER_TYPES );
        }

        private void AddReminderListPage()
        {
            // Clear any existing routes linked to the page.
            Sql( $@"
DECLARE @PageId INT = (SELECT [Id] FROM [Page] WHERE [Guid] = '{SystemGuid.Page.REMINDER_LIST}');
DELETE FROM [PageRoute] WHERE [PageId] = @PageId;" );

            RockMigrationHelper.AddPage(
                true,
                SystemGuid.Page.MY_DASHBOARD,
                SystemGuid.Layout.FULL_WIDTH_INTERNAL_SITE,
                "View Reminders",
                string.Empty,
                SystemGuid.Page.REMINDER_LIST,
                "fa fa-bell" );
#pragma warning disable CS0618 // Type or member is obsolete
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_LIST, "reminders", REMINDERS_ROUTE_1 );
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_LIST, "reminders/{EntityTypeId}", REMINDERS_ROUTE_2 );
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_LIST, "reminders/{EntityTypeId}/{EntityId}", REMINDERS_ROUTE_3 );
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_LIST, "reminders/{EntityTypeId}/{ReminderTypeId}", REMINDERS_ROUTE_4 );
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_LIST, "reminders/{EntityTypeId}/{ReminderTypeId}/{EntityId}", REMINDERS_ROUTE_5 );
#pragma warning restore CS0618 // Type or member is obsolete

            RockMigrationHelper.AddBlock(
                true,
                SystemGuid.Page.REMINDER_LIST,
                string.Empty,
                SystemGuid.BlockType.REMINDER_LIST,
                "Reminder List",
                "Main",
                string.Empty,
                string.Empty,
                0,
                REMINDER_LIST_BLOCK_INSTANCE );
        }

        private void AddReminderEditPage()
        {
            RockMigrationHelper.AddPage(
                true,
                SystemGuid.Page.REMINDER_LIST,
                SystemGuid.Layout.FULL_WIDTH_INTERNAL_SITE,
                "Edit Reminder",
                string.Empty,
                SystemGuid.Page.REMINDER_EDIT,
                "fa fa-bell" );

            RockMigrationHelper.AddBlock(
                true,
                SystemGuid.Page.REMINDER_EDIT,
                string.Empty,
                SystemGuid.BlockType.REMINDER_EDIT,
                "Edit Reminder",
                "Main",
                string.Empty,
                string.Empty,
                0,
                REMINDER_EDIT_BLOCK_INSTANCE );
        }

        private void AddReminderTypesPage()
        {
            RockMigrationHelper.AddPage(
                true,
                SystemGuid.Page.GENERAL_SETTINGS,
                SystemGuid.Layout.FULL_WIDTH_INTERNAL_SITE,
                "Reminder Types",
                string.Empty,
                SystemGuid.Page.REMINDER_TYPES,
                "fa fa-bell" );
#pragma warning disable CS0618 // Type or member is obsolete
            RockMigrationHelper.AddPageRoute( SystemGuid.Page.REMINDER_TYPES, "admin/general/reminder-types", REMINDER_TYPES_ROUTE );
#pragma warning restore CS0618 // Type or member is obsoleteS

            RockMigrationHelper.AddBlock(
                true,
                SystemGuid.Page.REMINDER_TYPES,
                string.Empty,
                SystemGuid.BlockType.REMINDER_TYPES,
                "Reminder Types",
                "Main",
                string.Empty,
                string.Empty,
                0,
                REMINDER_TYPES_BLOCK_INSTANCE );
        }

        private void AddReminderLinksBlockAndDashboardButton()
        {
            // Shifts any blocks in the internal site header zone (e.g., the Personal Links block) by one
            // so that the reminders link will be first.
            Sql( SHIFT_BLOCK_ORDER_QUERY );

            // Add Block Reminder Links to  Site: Rock RMS        
            RockMigrationHelper.AddBlock(
                true,
                null,
                null,
                SystemGuid.Site.SITE_ROCK_INTERNAL.AsGuid(),
                SystemGuid.BlockType.REMINDER_LINKS.AsGuid(),
                "Reminder Links",
                "Header",
                string.Empty,
                string.Empty,
                0,
                REMINDER_LINKS_BLOCK_INSTANCE );

            RockMigrationHelper.AddBlock(
                true,
                SystemGuid.Page.MY_DASHBOARD,
                string.Empty,
                SystemGuid.BlockType.HTML_CONTENT,
                "Reminders Button",
                "Sidebar1",
                string.Empty,
                string.Empty,
                0,
                REMINDERS_BUTTON_BLOCK_INSTANCE );

            RockMigrationHelper.UpdateHtmlContentBlock( REMINDERS_BUTTON_BLOCK_INSTANCE, @"<a class=""btn btn-default btn-block margin-b-md"" href=""/reminders""><i class=""fa fa-bell""></i> Reminders</a>", REMINDERS_BUTTON_HTML_CONTENT );
        }

        private void AddRemindersJob()
        {
            var jobClass = "Rock.Jobs.ProcessReminders";

            Sql( $@"
            IF NOT EXISTS( SELECT [Id] FROM [ServiceJob] WHERE [Class] = '{jobClass}' AND [Guid] = '{SystemGuid.ServiceJob.PROCESS_REMINDERS}' )
            BEGIN
                INSERT INTO [ServiceJob] (
                    [IsSystem],
                    [IsActive],
                    [Name],
                    [Description],
                    [Class],
                    [CronExpression],
                    [NotificationStatus],
                    [Guid] )
                VALUES (
                    0,
                    1,
                    'Process Reminders',
                    'A job which processes reminders, including creating appropriate notifications and updating the reminder count value for people with active reminders.',
                    '{jobClass}',
                    '{CRON_EXPRESSION}',
                    1,
                    '{SystemGuid.ServiceJob.PROCESS_REMINDERS}' );
            END
            ELSE
            BEGIN
	            UPDATE	[ServiceJob]
	            SET
		              [IsSystem] = 1
		            , [IsActive] = 1
		            , [Name] = 'Process Reminders'
		            , [Description] = 'A job which processes reminders, including creating appropriate notifications and updating the reminder count value for people with active reminders.'
		            , [Class] = '{jobClass}'
		            , [CronExpression] = '{CRON_EXPRESSION}'
		            , [NotificationStatus] = 1
	            WHERE
		              [Guid] = '{SystemGuid.ServiceJob.PROCESS_REMINDERS}';
            END" );
        }

        #endregion Blocks, Pages, and Routes

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }
    }
}
