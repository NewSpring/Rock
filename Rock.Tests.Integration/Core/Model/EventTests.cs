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
using System.Linq;
using System.Threading;

using Ical.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Data;
using Rock.Model;
using Rock.Tests.Integration.TestData;
using Rock.Tests.Shared;
using Rock.Tests.Shared.TestFramework;
using Rock.Web.Cache;

using TimeZoneConverter;

namespace Rock.Tests.Integration.Core.Model
{
    /// <summary>
    /// Tests related to Calendar Events.
    /// </summary>
    /// <remarks>
    /// These tests require a database populated with standard Rock sample data. 
    /// </remarks>
    [TestClass]
    [TestCategory( "Core.Events.CalendarFeed" )]
    public class EventTests : DatabaseTestsBase
    {
        [ClassInitialize]
        public static void Initialize( TestContext context )
        {
            EventsDataManager.Instance.UpdateSampleDataEventDates();
            EventsDataManager.Instance.AddDataForRockSolidFinancesClass();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.SetRockDateTimeToLocalTimezone();
        }

        #region EventItemService Tests

        /// <summary>
        /// Retrieving a list of active events with the default settings should only return events having occurrences after the current date.
        /// </summary>
        [TestMethod]
        public void EventItemService_GetActiveEventsDefault_ReturnsOnlyEventsHavingOccurrencesAfterCurrentDate()
        {
            var rockContext = new RockContext();
            var eventItemService = new EventItemService( rockContext );
            var events = eventItemService.GetActiveItems();

            // This event has a single occurrence scheduled in the past.
            // It should not be returned in the list of active items.
            var warriorEvent = events.FirstOrDefault( x => x.Name == "Customs & Classics Car Show" );

            Assert.That.IsNull( warriorEvent, "Unexpected event found in result set." );

            // This event is endlessly recurring, and the scheduled start date is prior to the current date.
            // Only instances after the current date should be returned in the list of active items.
            var staffEvent = events.FirstOrDefault( x => x.Name == "Staff Meeting" );

            Assert.That.IsNotNull( staffEvent, "Expected event not found in result set." ); 
        }

        /// <summary>
        /// Retrieving a list of active events should only return an event scheduled for a single date if it occurs after the specified date.
        /// </summary>
        [TestMethod]
        public void EventItemService_GetActiveEventsAtSpecificDate_ReturnsEventScheduledForSingleDateOnOrAfterSpecifiedDate()
        {
            var effectiveDate = EventsDataManager.Instance.GetDefaultEffectiveDate();

            // Get an instance of Event "Warrior Youth Event", which has a single occurrence scheduled for the current month.
            var rockContext = new RockContext();
            var eventItemService = new EventItemService( rockContext );

            var warriorEvent = eventItemService.Queryable().FirstOrDefault( x => x.Name == "Warrior Youth Event" );

            Assert.That.IsNotNull( warriorEvent, "Target event not found." );

            ForceUpdateScheduleEffectiveDates( rockContext, warriorEvent );

            // Verify that the filter returns a single event occurrence for the current month.
            var validEvents = eventItemService.Queryable()
                .Where( x => x.Name == "Warrior Youth Event" )
                .HasOccurrencesOnOrAfterDate( effectiveDate )
                .ToList();

            Assert.That.IsTrue( validEvents.Count() == 1, "Expected Event not found." );

            // ... but no event occurrences for the following month.
            var invalidEvents = eventItemService.Queryable()
                .Where( x => x.Name == "Warrior Youth Event" )
                .HasOccurrencesOnOrAfterDate( effectiveDate.AddMonths(1) )
                .ToList();

            Assert.That.IsTrue( invalidEvents.Count() == 0, "Unexpected Event found." );
        }

        private void ForceUpdateScheduleEffectiveDates( RockContext rockContext, EventItem eventItem )
        {
            // Ensure that the Event Schedule has been updated to record the EffectiveEndDate.
            // This is only necessary if the RockCleanup job has not been run in the current database
            // since upgrading from v1.12.4.
            foreach ( var occurrence in eventItem.EventItemOccurrences )
            {
                var isUpdated = occurrence.Schedule.EnsureEffectiveStartEndDates();
                if ( isUpdated )
                {
                    rockContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Retrieving a list of active events by calendar should only return events from the specified calendar.
        /// </summary>
        [TestMethod]
        public void EventItemService_GetActiveEventsByCalendar_ReturnsOnlyEventsInSpecifiedCalendar()
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            var publicCalendar = calendarService.Queryable()
                .FirstOrDefault( x => x.Name == "Public" );

            var eventItemService = new EventItemService( rockContext );
            var publicEvents = eventItemService.GetActiveItemsByCalendarId( publicCalendar.Id )
                .ToList();

            // The Event "Warrior Youth Event" exists in the Public calendar.
            // It should be returned in the list of active items.
            var staffEvent = publicEvents.FirstOrDefault( x => x.Name == "Warrior Youth Event" );

            Assert.That.IsNotNull( staffEvent, "Expected event not found in result set." );

            // The Event "Staff Meeting" only exists in the Internal calendar.
            // It should not be returned in the list of active items.
            var warriorEvent = publicEvents.FirstOrDefault( x => x.Name == "Staff Meeting" );

            Assert.That.IsNull( warriorEvent, "Unexpected event found in result set." );
        }

        #endregion

        #region EventItemOccurrence.NextDateTime Tests

        private const string TestDataForeignKey = "test_data";
        private const string EventFinancesClassGuid = "6EFC00B0-F5D3-4352-BC3B-F09852FB5788";
        private const string ScheduleSat1630Guid = "7883CAC8-6E30-482B-95A7-2F0DEE859BE1";
        private const string ScheduleSun1200Guid = "1F6C15DA-982F-43B1-BDE9-D4E70CFBCB45";
        private const string FinancesClassOccurrenceTestGuid = "73D5B5F2-CDD7-4DC6-B62F-D251CB8832CA";
        private static string MainCampusGuidString = "76882AE3-1CE8-42A6-A2B6-8C0B29CF8CF8";

        /// <summary>
        /// Adding an EventItemOccurrence correctly sets the NextDateTime property.
        /// </summary>
        [TestMethod]
        public void EventItemOccurrence_NewWithFutureActiveSchedule_HasCorrectNextDate()
        {
            var rockContext = new RockContext();
            var financeEvent1 = EventItemOccurrenceAddOrUpdateInstance( rockContext, ScheduleSat1630Guid.AsGuid(), deleteExistingInstance: true );

            rockContext.SaveChanges();

            // Verify that the NextDateTime is correctly set.
            var scheduleService = new ScheduleService( rockContext );
            var scheduleSat1630 = scheduleService.Get( ScheduleSat1630Guid.AsGuid() );
            var nextDateTime = scheduleSat1630.GetNextStartDateTime( RockDateTime.Now );

            Assert.AreEqual( financeEvent1.NextStartDateTime, nextDateTime );
        }

        /// <summary>
        /// Modifying the schedule for an EventItemOccurrence correctly adjusts the NextDateTime property.
        /// </summary>
        [TestMethod]
        public void EventItemOccurrence_ModifiedWithActiveSchedule_HasUpdatedNextDate()
        {
            var rockContext = new RockContext();
            var financeEvent1 = EventItemOccurrenceAddOrUpdateInstance( rockContext, ScheduleSat1630Guid.AsGuid(), deleteExistingInstance: true );

            rockContext.SaveChanges();

            // Get existing schedules.
            var scheduleService = new ScheduleService( rockContext );
            var scheduleSat1630 = scheduleService.Get( ScheduleSat1630Guid.AsGuid() );
            var scheduleSat1800 = scheduleService.Get( ScheduleSun1200Guid.AsGuid() );

            // Verify that the NextDateTime is correctly set.
            var nextDateTime1 = scheduleSat1630.GetNextStartDateTime( RockDateTime.Now );
            Assert.AreEqual( financeEvent1.NextStartDateTime, nextDateTime1 );

            // Now modify the event occurrence to use Schedule 2.
            financeEvent1.ScheduleId = scheduleSat1800.Id;

            rockContext.SaveChanges();

            // Verify that the NextDateTime is correctly set.
            var nextDateTime2 = scheduleSat1800.GetNextStartDateTime( RockDateTime.Now );
            Assert.AreEqual( financeEvent1.NextStartDateTime, nextDateTime2 );
        }

        /// <summary>
        /// Setting an inactive schedule for an EventItemOccurrence correctly nullifies the NextDateTime property.
        /// </summary>
        [TestMethod]
        public void EventItemOccurrence_ModifiedWithInactiveSchedule_HasNullNextDate()
        {
            var rockContext = new RockContext();

            // Get existing schedules.
            var scheduleService = new ScheduleService( rockContext );
            var scheduleSat1630 = scheduleService.Get( ScheduleSat1630Guid.AsGuid() );

            // Set the schedule to inactive.
            scheduleSat1630.IsActive = false;
            rockContext.SaveChanges();

            // Add an event occurrence with the inactive schedule
            var financeEvent1 = EventItemOccurrenceAddOrUpdateInstance( rockContext, ScheduleSat1630Guid.AsGuid(), deleteExistingInstance: true );
            rockContext.SaveChanges();

            // Verify that the NextDateTime is correctly set.
            Assert.IsNull( financeEvent1.NextStartDateTime );

            // Set the schedule to active.
            scheduleSat1630.IsActive = true;
            rockContext.SaveChanges();
        }

        /// <summary>
        /// Saving changes to an EventItemOccurrence attached to an inactive Event correctly nullifies the NextDateTime property.
        /// </summary>
        [TestMethod]
        public void EventItemOccurrence_UpdatedWithInactiveEvent_HasNullNextDate()
        {
            var rockContext = new RockContext();

            // Get Event "Rock Solid Finances".
            var eventItemService = new EventItemService( rockContext );
            var financeEvent = eventItemService.Get( EventFinancesClassGuid.AsGuid() );

            // Get existing schedules.
            var scheduleService = new ScheduleService( rockContext );
            var scheduleSat1630 = scheduleService.Get( ScheduleSat1630Guid.AsGuid() );

            // Set the event to inactive.
            financeEvent.IsActive = false;
            rockContext.SaveChanges();

            // Add an event occurrence for the inactive event.
            var financeEvent1 = EventItemOccurrenceAddOrUpdateInstance( rockContext, ScheduleSat1630Guid.AsGuid(), deleteExistingInstance: true );
            rockContext.SaveChanges();

            // Verify that the NextDateTime is correctly set.
            Assert.IsNull( financeEvent1.NextStartDateTime );

            // Set the event to active.
            financeEvent.IsActive = true;
            rockContext.SaveChanges();
        }

        private EventItemOccurrence EventItemOccurrenceAddOrUpdateInstance( RockContext rockContext, Guid scheduleGuid, bool deleteExistingInstance )
        {
            // Get existing schedules.
            var scheduleService = new ScheduleService( rockContext );
            var schedule = scheduleService.Get( scheduleGuid );

            // Get Event "Rock Solid Finances".
            var eventItemService = new EventItemService( rockContext );
            var eventItemOccurrenceService = new EventItemOccurrenceService( rockContext );

            var financeEvent = eventItemService.Get( EventFinancesClassGuid.AsGuid() );

            // Add a new occurrence of this event.
            var financeEvent1 = eventItemOccurrenceService.Get( FinancesClassOccurrenceTestGuid.AsGuid() );
            if ( financeEvent1 != null && deleteExistingInstance )
            {
                eventItemOccurrenceService.Delete( financeEvent1 );
                rockContext.SaveChanges();
            }
            financeEvent1 = new EventItemOccurrence();

            var mainCampusId = CampusCache.GetId( MainCampusGuidString.AsGuid() );

            financeEvent1.Location = "Meeting Room 1";
            financeEvent1.ForeignKey = TestDataForeignKey;
            financeEvent1.ScheduleId = schedule.Id;
            financeEvent1.Guid = FinancesClassOccurrenceTestGuid.AsGuid();
            financeEvent1.CampusId = mainCampusId;

            financeEvent.EventItemOccurrences.Add( financeEvent1 );

            return financeEvent1;
        }

        #endregion

        #region GetEventCalendarFeed Tests

        private const string testEventOccurrence11Guid = "C4D3D174-66BB-40D2-8BF5-5B8827C46538";

        /// <summary>
        /// Retrieving events for a calendar feed from a Rock server having a Rock time that differs from the system local time
        /// should return events in Rock time.
        /// </summary>
        /// <remarks>
        /// This situation arises when the Rock server is hosted in a datacenter in a different timezone from the preferred server timezone.
        /// [Fixes Issue #5029 (https://github.com/SparkDevNetwork/Rock/issues/5029)]
        /// </remarks>
        [TestMethod]
        public void EventCalendarFeed_WithRockTimezoneDifferentFromSystemTimezone_ReturnsEventsWithRockTimezone()
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            // Set RockDateTime to the local timezone, assuming that this corresponds to the timezone of the event data in the current database.
            TestConfigurationHelper.SetRockDateTimeToLocalTimezone();

            // Verify that the events returned in the feed are scheduled in local server time.
            var args = GetCalendarEventFeedArgumentsForTest( "Internal", TestDataHelper.SecondaryCampusName );
            var calendarString1 = calendarService.CreateICalendar( args );

            // Deserialize the calendar output.
            var events1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events;
            var event1 = events1.FirstOrDefault( x => x.Summary == "Rock Solid Finances Class" );

            Assert.AreEqual( TimeZoneInfo.Local.BaseUtcOffset.Hours,
                event1.DtStart.AsDateTimeOffset.Offset.Hours,
                "Unexpected Time Offset. The offset should match the system local time." );

            // Verify that the events returned in the feed are scheduled in Rock time,
            // which is now different to the local server time.
            TestConfigurationHelper.SetRockDateTimeToAlternateTimezone();
            var tzAlternate = TestConfigurationHelper.GetTestTimeZoneAlternate();

            var calendarString2 = calendarService.CreateICalendar( args );

            var events2 = CalendarCollection.Load( calendarString2 )?.FirstOrDefault()?.Events;
            var event2 = events2.FirstOrDefault( x => x.Summary == "Rock Solid Finances Class" );

            Assert.AreEqual( tzAlternate.BaseUtcOffset.Hours,
                event2.DtStart.AsDateTimeOffset.Offset.Hours,
                "Unexpected Time Offset. The offset should match the Rock time." );
        }

        [TestMethod]
        public void EventCalendarFeed_FilteredByCampus_ReturnsEventsForSpecifiedCampusOnly()
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            // Make sure our test campus exists.
            TestDataHelper.GetOrAddCampusSteppingStone( rockContext );

            var args = GetCalendarEventFeedArgumentsForTest( "Public", campusName: TestGuids.Crm.CampusSteppingStone );
            var calendarString1 = calendarService.CreateICalendar( args );

            // Deserialize the calendar output and verify the results.
            var events1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events;

            Assert.IsTrue( events1.Count > 0,
                "Expected result not found. Filter returned no Events." );
            Assert.IsTrue( events1.Any( x => x.Location == "Meeting Room 2" ),
                "Expected result not found. Event with Campus/Location not found." );
        }

        [TestMethod]
        public void EventCalendarFeed_FilteredByAudience_ReturnsEventsForSpecifiedAudienceOnly()
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            var dtAudienceType = DefinedTypeCache.Get( SystemGuid.DefinedType.CONTENT_CHANNEL_AUDIENCE_TYPE.AsGuid(), rockContext );
            var audienceTypeId = dtAudienceType.DefinedValues
                .Where( x => x.Value == "Men" )
                .Select( x => x.Id )
                .FirstOrDefault();

            var args = GetCalendarEventFeedArgumentsForTest( "Public", "Main Campus" );
            args.AudienceIds = new List<int> { audienceTypeId };

            var calendarString1 = calendarService.CreateICalendar( args );

            // Deserialize the calendar output and verify the results.
            var events1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events;
            Assert.IsTrue( events1.Count > 0,
                "Expected result not found. Filter returned no Events." );
            Assert.IsNull( events1.FirstOrDefault( x => !x.Categories.Contains( "Men" ) ),
                "Event with unexpected Audience found." );
        }

        [TestMethod]
        public void EventCalendarFeed_FilteredByEvent_ReturnsSpecifiedEventsOnly()
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            var args = GetCalendarEventFeedArgumentsForTest( calendarName: "Public",
                eventIdentifier: TestGuids.Events.EventIdentifierRockSolidFinancesClass );

            var calendarString1 = calendarService.CreateICalendar( args );

            // Deserialize the calendar output and verify the results.
            var events1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events;
            Assert.IsTrue( events1.Any( e => e.Summary == "Rock Solid Finances Class" ),
                "Expected result not found. Filter returned no Events." );
            Assert.IsFalse( events1.Any( e => e.Summary != "Rock Solid Finances Class" ),
                "Expected result not found. Filter returned unexpected Events." );
        }

        /// <summary>
        /// The calendar feed must comply with specific rules to be imported correctly by Google Calendar and other calendar applications.
        /// This test verifies that the calendar feed format conforms to the known requirements.
        /// </summary>
        [TestMethod]
        public void EventCalendarFeed_GoogleCalendarAsImportTarget_HasCorrectFormat()
        {
            const string eventName = "Rock Solid Finances Class";
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            // Verify that the events returned in the feed are scheduled in local server time, within the period in which DST applies.
            var startDate = RockDateTime.New( 2020, 1, 1 ).Value;
            var endDate = RockDateTime.New( 2020, 12, 31 ).Value;

            // Get calendar events for DST Timezone.
            TestConfigurationHelper.SetRockDateTimeToDaylightSavingTimezone();
            var tzDst = TestConfigurationHelper.GetTestTimeZoneDaylightSaving();

            var args = GetCalendarEventFeedArgumentsForTest( calendarName: null, campusName: null, startDate, endDate, eventName );
            var calendarStringDst = calendarService.CreateICalendar( args );

            // Verify that the calendar feed contains the IANA timezone identifier rather the Windows equivalent.
            var winTimeZoneId = tzDst.Id;
            var ianaTimeZoneId = TZConvert.WindowsToIana( winTimeZoneId );

            Assert.That.Contains( calendarStringDst, ianaTimeZoneId );
            Assert.That.DoesNotContain( calendarStringDst, winTimeZoneId );
        }

        private static GetCalendarEventFeedArgs GetCalendarEventFeedArgumentsForTest( string calendarName = null, string campusName = null, DateTime? startDate = null, DateTime? endDate = null, string eventIdentifier = null )
        {
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            var args = new GetCalendarEventFeedArgs();

            calendarName = calendarName ?? "Internal";

            var calendar = calendarService.Queryable()
                .GetByIdentifierOrThrow( calendarName );

            args.CalendarId = calendar.Id;

            args.StartDate = startDate ?? RockDateTime.New( 2010, 1, 1 ).Value;
            args.EndDate = endDate ?? args.StartDate.AddYears( 10 );

            if ( !string.IsNullOrWhiteSpace( campusName ) )
            {
                var campusService = new CampusService( rockContext );
                var campus = campusService.Queryable()
                    .GetByIdentifierOrThrow( campusName );

                args.CampusIds = new List<int> { campus.Id };
            }

            if ( !string.IsNullOrWhiteSpace( eventIdentifier ) )
            {
                var eventItemService = new EventItemService( rockContext );
                var eventItemId = eventItemService.Queryable()
                    .GetByIdentifierOrThrow( eventIdentifier )?.Id ?? 0;

                args.EventItemIds = new List<int> { eventItemId };
            }

            return args;

        }

        /// <summary>
        /// Verifies that a calendar feed with specific dates is created in a format that is compatible with major calendaring applications.
        /// </summary>
        /// <remarks>
        /// </remarks>
        [TestMethod]
        public void EventCalendarFeed_EventWithFixedDurationAndSpecificDates_ReturnsICalendarWithVerifiedFormat()
        {
            var testScheduleGuid = "05CB6931-C095-4817-8AD0-9B7DFCB9165E";
            var testEventGuid = "FA6D9A9D-94B2-4722-A29C-1D4445BE1A27";
            var testCalendarGuid = "A46D56AB-B398-4494-B3B9-87DBE51A9109";

            var duration = new TimeSpan( 3, 0, 0 );
            var startTime = new TimeSpan( 10, 30, 0 );

            var dateNow = RockDateTime.Now.Date;
            var day1Date = new DateTime( dateNow.Year, dateNow.Month, 1 ).Add( startTime );
            var day2Date = day1Date.AddDays( 3 );
            var day3Date = day1Date.AddDays( 6 );

            var specificDates = new List<DateTime> { day1Date, day2Date, day3Date };

            var scheduleDays = EventCalendarFeed_CreateScheduleForSpecificDates( testScheduleGuid,
                "Test-Specific Days/Fixed Duration",
                specificDates,
                startTime,
                duration );

            var testCalendar = GetOrAddEventCalendar( testCalendarGuid, "Test Calendar" );

            var eventItem = EventCalendarFeed_CreateEventForSpecificDates( testEventGuid,
                $"Specific Days/Fixed Duration ({day1Date.Day},{day2Date.Day},{day3Date.Day})",
                testCalendar.Id,
                scheduleDays.Id );

            // Set RockDateTime to the local timezone, assuming that this corresponds to the timezone of the event data in the current database.
            TestConfigurationHelper.SetRockDateTimeToLocalTimezone();

            // Get the calendar feed for the Internal calendar that includes the Test Event.
            var args = GetCalendarEventFeedArgumentsForTest( "Test Calendar", eventIdentifier: eventItem.Guid.ToString() );
            args.StartDate = day1Date;
            args.EndDate = day1Date.AddMonths( 3 );

            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );
            var calendarString = calendarService.CreateICalendar( args );

            var events = CalendarCollection.Load( calendarString )?.FirstOrDefault()?.Events;

            Assert.AreEqual( specificDates.Count, events.Count, "Unexpected event count." );

            foreach ( var specificDate in specificDates )
            {
                var calendarEvent = events.FirstOrDefault( x => x.Start.Value == specificDate );
                Assert.IsNotNull( calendarEvent, $"Event with date/time not found: {specificDate:G}" );
                Assert.AreEqual( duration, calendarEvent.Duration, "Event has unexpected duration." );
            }

            LogHelper.Log( $"** ICalendar Document Output:\n{calendarString}" );
        }

        private EventCalendar GetOrAddEventCalendar( string calendarGuid, string calendarName )
        {
            // Add a test calendar
            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );

            var calendar = calendarService.GetByIdentifier( calendarGuid );
            if ( calendar == null )
            {
                calendar = new EventCalendar();
                calendar.Name = calendarName;
                calendar.Guid = calendarGuid.AsGuid();
                calendarService.Add( calendar );

                rockContext.SaveChanges();
            }

            return calendar;
        }

        /// <summary>
        /// Verifies that a calendar feed with specific dates is created in a format that is compatible with major calendaring applications.
        /// </summary>
        /// <remarks>
        /// </remarks>
        [TestMethod]
        public void EventCalendarFeed_EventWithAllDayDurationAndSpecificDates_ReturnsICalendarWithVerifiedFormat()
        {
            var testScheduleGuid = "55B01442-AED1-4B8B-A566-2BC3ABB8E494";
            var testEventGuid = "D32BAA71-7D90-4E2F-BAB7-01CA34AF1BE3";
            var testCalendarGuid = "C86B3EBE-564E-48B9-AAD4-0753E155A180";

            var dateNow = RockDateTime.Now.Date;
            var day1Date = new DateTime( dateNow.Year, dateNow.Month, 1 );
            var day2Date = day1Date.AddDays( 3 );
            var day3Date = day1Date.AddDays( 6 );

            var specificDates = new List<DateTime> { day1Date, day2Date, day3Date };

            var scheduleDays = EventCalendarFeed_CreateScheduleForSpecificDates( testScheduleGuid,
                "Test Schedule for Specific Days",
                specificDates );

            var testCalendar = GetOrAddEventCalendar( testCalendarGuid, "Feed Test" );

            var eventItem = EventCalendarFeed_CreateEventForSpecificDates( testEventGuid,
                $"Specific Days ({day1Date.Day},{day2Date.Day},{day3Date.Day})",
                testCalendar.Id,
                scheduleDays.Id );

            // Set RockDateTime to the local timezone, assuming that this corresponds to the timezone of the event data in the current database.
            TestConfigurationHelper.SetRockDateTimeToLocalTimezone();

            // Get the calendar feed for the Internal calendar that includes the Test Event.
            var args = GetCalendarEventFeedArgumentsForTest( "Feed Test", eventIdentifier: eventItem.Guid.ToString() );
            args.StartDate = day1Date;
            args.EndDate = day1Date.AddMonths( 3 );

            var rockContext = new RockContext();
            var calendarService = new EventCalendarService( rockContext );
            var calendarString = calendarService.CreateICalendar( args );

            var events = CalendarCollection.Load( calendarString )?.FirstOrDefault()?.Events;

            Assert.AreEqual( specificDates.Count, events.Count, "Unexpected event count." );

            foreach ( var specificDate in specificDates )
            {
                var calendarEvent = events.FirstOrDefault( x => x.Start.Value == specificDate );
                Assert.IsNotNull( calendarEvent, $"Event with date/time not found: {specificDate:G}" );
                Assert.IsTrue( calendarEvent.IsAllDay, "Event should be 'all day' but is not." );
            }

            LogHelper.Log( $"** ICalendar Document Output:\n{calendarString}" );
        }

        private Schedule EventCalendarFeed_CreateScheduleForSpecificDates( string scheduleGuid, string name, List<DateTime> specificDates, TimeSpan? eventStartTime = null, TimeSpan? eventDuration = null )
        {
            var rockContext = new RockContext();
            var scheduleService = new ScheduleService( rockContext );

            var scheduleDays = scheduleService.Get( scheduleGuid );
            if ( scheduleDays == null )
            {
                scheduleDays = new Schedule();
                scheduleService.Add( scheduleDays );
            }

            scheduleDays.Name = name;
            scheduleDays.Guid = scheduleGuid.AsGuid();

            var iCalSchedule = ScheduleTestHelper.GetScheduleWithSpecificDates( specificDates, eventStartTime, eventDuration );

            scheduleDays.iCalendarContent = iCalSchedule.iCalendarContent;

            rockContext.SaveChanges();

            return scheduleDays;
        }

        private EventItem EventCalendarFeed_CreateEventForSpecificDates( string eventGuid, string eventName, int calendarId, int scheduleId )
        {
            var rockContext = new RockContext();

            // Create a test Rock Event associated with the schedule.
            var eventItemService = new EventItemService( rockContext );

            var testEvent1 = eventItemService.Get( eventGuid );
            if ( testEvent1 != null )
            {
                eventItemService.Delete( testEvent1 );
                rockContext.SaveChanges();
            }

            testEvent1 = new EventItem();
            testEvent1.Guid = eventGuid.AsGuid();
            testEvent1.Name = eventName;
            testEvent1.IsApproved = true;
            eventItemService.Add( testEvent1 );

            rockContext.SaveChanges();

            var testEvent1CalendarInternal = new EventCalendarItem { EventCalendarId = calendarId };

            testEvent1.EventCalendarItems.Add( testEvent1CalendarInternal );

            var testOccurrence11 = new EventItemOccurrence();

            testOccurrence11.ScheduleId = scheduleId;
            testOccurrence11.NextStartDateTime = null;

            testEvent1.EventItemOccurrences.Add( testOccurrence11 );

            rockContext.SaveChanges();

            return testEvent1;
        }

        /// <summary>
        /// If an EventItem is modified, the associated iCalendar.SEQUENCE property should be greater than the previous instance.
        /// The EventItem represents the template from which occurrences of the event are created.
        /// </summary>
        [TestMethod]
        public void EventCalendarFeed_UpdatedEventItem_HasIncrementedSequenceNo()
        {
            var testScheduleUid = "509884F8-B7A2-4C4B-8F12-F14E98362DFB";
            var testEventGuid = "8EF41051-29B0-45A3-8C44-85E11622BD81";
            var startDate = RockDateTime.New( 2020, 3, 1 ).Value;
            var eventName = $"Test Event ({testEventGuid})";

            AddOrUpdateScheduleForConsecutiveSpecifiedDays( testScheduleUid, startDate, 2 );

            var rockContext = new RockContext();

            // Create a test Rock Event associated with the schedule.
            var eventItemArgs = new EventsDataManager.CreateEventItemActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testEventGuid.AsGuid(),
                Properties = new EventsDataManager.EventItemInfo
                {
                    EventName = eventName,
                    CalendarIdentifiers = new List<string> { "Public", "Internal" },
                    IsApproved = true
                }
            };

            var eventItem = EventsDataManager.Instance.AddEventItem( eventItemArgs );

            // Add an Event Occurrence.
            var occurrenceArgs = new EventsDataManager.CreateEventItemOccurrenceActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testEventOccurrence11Guid.AsGuid(),
                Properties = new EventsDataManager.EventItemOccurrenceInfo
                {
                    EventIdentifier = testEventGuid.ToString(),
                    ScheduleIdentifier = testScheduleUid
                }
            };

            var eventOccurrence = EventsDataManager.Instance.AddEventItemOccurrence( occurrenceArgs );

            var calendarService = new EventCalendarService( rockContext );

            var args = GetCalendarEventFeedArgumentsForTest( calendarName: "Public",
                eventIdentifier: testEventGuid,
                startDate: startDate );

            // Get the ICalendar.
            var calendarString1 = calendarService.CreateICalendar( args );

            var calendarEvent1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == eventName );

            Assert.IsNotNull( calendarEvent1, "Expected Event not found." );

            // Delay at least 1s to ensure that the sequence number should be incremented.
            Thread.Sleep( 1000 );

            // Modify the EventItem.
            var updateArgs = new EventsDataManager.UpdateEventItemActionArgs
            {
                Properties = new EventsDataManager.EventItemInfo { EventName = $"{eventName} [Updated]" },
                UpdateTargetIdentifier = testEventGuid
            };
            EventsDataManager.Instance.UpdateEventItem( updateArgs );

            // Get the ICalendar.
            var calendarString2 = calendarService.CreateICalendar( args );

            var calendarEvent2 = CalendarCollection.Load( calendarString2 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == $"{eventName} [Updated]" );

            Assert.IsNotNull( calendarEvent2, "Expected Event not found." );
            Assert.IsTrue( calendarEvent2.Sequence > calendarEvent1.Sequence, $"Event2 Sequence number is not greater than Event 1. [Event1={calendarEvent1.Sequence}, Event2={calendarEvent2.Sequence}]" );
        }

        /// <summary>
        /// If an EventItem is modified, the associated iCalendar.SEQUENCE property should be greater than the previous instance.
        /// The EventItem represents the template from which occurrences of the event are created.
        /// </summary>
        [TestMethod]
        public void EventCalendarFeed_UpdatedEventOccurrence_HasIncrementedSequenceNo()
        {
            var testScheduleUid1 = "0017BFAF-0E5D-46F5-ACB6-0543439BB1BE";
            var testEventGuid = "2329682C-5F92-4FB5-8D62-88B235DB6325";
            var eventName = $"Test Event ({testEventGuid})";

            var startDate1 = RockDateTime.New( 2020, 3, 1 ).Value;

            AddOrUpdateScheduleForConsecutiveSpecifiedDays( testScheduleUid1, startDate1, 2 );

            var rockContext = new RockContext();

            // Create a test Rock Event associated with the schedule.
            var eventItemArgs = new EventsDataManager.CreateEventItemActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testEventGuid.AsGuid(),
                Properties = new EventsDataManager.EventItemInfo
                {
                    EventName = eventName,
                    CalendarIdentifiers = new List<string> { "Public", "Internal" },
                    IsApproved = true
                }
            };

            var eventItem = EventsDataManager.Instance.AddEventItem( eventItemArgs );

            rockContext.SaveChanges();

            // Add an Event Occurrence.
            var occurrenceArgs = new EventsDataManager.CreateEventItemOccurrenceActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testEventOccurrence11Guid.AsGuid(),
                Properties = new EventsDataManager.EventItemOccurrenceInfo
                {
                    EventIdentifier = testEventGuid.ToString(),
                    ScheduleIdentifier = testScheduleUid1
                }
            };

            var eventOccurrence = EventsDataManager.Instance.AddEventItemOccurrence( occurrenceArgs );

            var calendarService = new EventCalendarService( rockContext );

            var args = GetCalendarEventFeedArgumentsForTest( calendarName: "Public",
                eventIdentifier: testEventGuid,
                startDate: startDate1 );

            // Get the ICalendar.
            var calendarString1 = calendarService.CreateICalendar( args );

            LogHelper.Log( calendarString1 );

            var calendarEvent1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == eventName );

            Assert.IsNotNull( calendarEvent1, "Expected Event not found." );

            // Delay at least 1s to ensure that the sequence number should be incremented.
            Thread.Sleep( 1000 );

            // Modify the EventOccurrence: change the Location Description.
            var updateArgs = new EventsDataManager.UpdateEventItemOccurrenceActionArgs
            {
                UpdateTargetIdentifier = testEventOccurrence11Guid,
                Properties = new EventsDataManager.EventItemOccurrenceInfo
                {
                    MeetingLocationDescription = "Location 2"
                }
            };

            eventOccurrence = EventsDataManager.Instance.UpdateEventItemOccurrence( updateArgs );

            // Get the ICalendar.
            var calendarString2 = calendarService.CreateICalendar( args );

            LogHelper.Log( calendarString2 );

            var calendarEvent2 = CalendarCollection.Load( calendarString2 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == eventName );

            Assert.IsNotNull( calendarEvent2, "Expected Event not found." );
            Assert.IsTrue( calendarEvent2.Sequence > calendarEvent1.Sequence, $"Event2 Sequence number is not greater than Event 1. [Event1={calendarEvent1.Sequence}, Event2={calendarEvent2.Sequence}]" );
        }

        /// <summary>
        /// If the Schedule associated with an Event Occurrence is modified, the associated iCalendar.SEQUENCE property should be greater than the previous instance.
        /// </summary>
        [TestMethod]
        public void EventCalendarFeed_UpdatedEventOccurrenceSchedule_HasIncrementedSequenceNo()
        {
            var testScheduleUid1 = "C1821E26-8986-4AF5-B998-7972A2F73B87";
            var testEventGuid = "447F9D24-29CD-4F56-A2CC-3EB028F49E81";
            var testOccurrenceGuid = "61B14EC0-40EE-4B2C-ABEA-1A0458E006A2";
            var eventName = $"Test Event ({testEventGuid})";

            var startDate1 = RockDateTime.New( 2020, 3, 1 ).Value;
            var startDate2 = RockDateTime.New( 2020, 4, 1 ).Value;

            AddOrUpdateScheduleForConsecutiveSpecifiedDays( testScheduleUid1, startDate1, 2 );

            var rockContext = new RockContext();

            // Create a test Rock Event associated with the schedule.
            var eventItemArgs = new EventsDataManager.CreateEventItemActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testEventGuid.AsGuid(),
                Properties = new EventsDataManager.EventItemInfo
                {
                    EventName = eventName,
                    CalendarIdentifiers = new List<string> { "Public", "Internal" },
                    IsApproved = true
                }
            };

            var eventItem = EventsDataManager.Instance.AddEventItem( eventItemArgs );

            // Add an Event Occurrence.
            var occurrenceArgs = new EventsDataManager.CreateEventItemOccurrenceActionArgs
            {
                ExistingItemStrategy = TestData.CreateExistingItemStrategySpecifier.Replace,
                Guid = testOccurrenceGuid.AsGuid(),
                Properties = new EventsDataManager.EventItemOccurrenceInfo
                {
                    EventIdentifier = testEventGuid.ToString(),
                    ScheduleIdentifier = testScheduleUid1
                }
            };

            var eventOccurrence = EventsDataManager.Instance.AddEventItemOccurrence( occurrenceArgs );

            var calendarService = new EventCalendarService( rockContext );

            var args = GetCalendarEventFeedArgumentsForTest( calendarName: "Public",
                eventIdentifier: testEventGuid,
                startDate: startDate1 );

            // Get the ICalendar.
            var calendarString1 = calendarService.CreateICalendar( args );

            LogHelper.Log( calendarString1 );

            var calendarEvent1 = CalendarCollection.Load( calendarString1 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == eventName );

            Assert.IsNotNull( calendarEvent1, "Expected Event not found." );

            // Delay at least 1s to ensure that the sequence number should be incremented.
            Thread.Sleep( 1000 );

            // Modify the Schedule associated with the EventOccurrence.
            AddOrUpdateScheduleForConsecutiveSpecifiedDays( testScheduleUid1, startDate2, 2 );

            // Get the ICalendar.
            var calendarString2 = calendarService.CreateICalendar( args );

            LogHelper.Log( calendarString2 );

            var calendarEvent2 = CalendarCollection.Load( calendarString2 )?.FirstOrDefault()?.Events
                .FirstOrDefault( e => e.Summary == eventName );

            Assert.IsNotNull( calendarEvent2, "Expected Event not found." );
            Assert.IsTrue( calendarEvent2.Sequence > calendarEvent1.Sequence, $"Event2 Sequence number is not greater than Event 1. [Event1={calendarEvent1.Sequence}, Event2={calendarEvent2.Sequence}]" );
        }

        private Schedule AddOrUpdateScheduleForConsecutiveSpecifiedDays( string testScheduleGuid, DateTime firstDate, int repeatCount )
        {
            var rockContext = new RockContext();
            var scheduleService = new ScheduleService( rockContext );
            var scheduleName = $"Test Schedule {testScheduleGuid.AsGuid()}";

            // Create a new Rock Schedule for an all-day event on specific days:
            // tomorrow and the following 2 days.
            var specificDates = new List<DateTime>();

            for ( int i = 0; i <= repeatCount; i++ )
            {
                var dayDate = firstDate.AddDays( i );
                specificDates.Add( dayDate );
            }

            var scheduleDays = scheduleService.Get( testScheduleGuid );
            if ( scheduleDays == null )
            {
                scheduleDays = new Schedule();
                scheduleService.Add( scheduleDays );
            }

            scheduleDays.Name = scheduleName;
            scheduleDays.Guid = testScheduleGuid.AsGuid();

            var iCalSchedule = ScheduleTestHelper.GetScheduleWithSpecificDates( specificDates );
            scheduleDays.iCalendarContent = iCalSchedule.iCalendarContent;

            rockContext.SaveChanges();

            return scheduleDays;
        }

        #endregion
    }
}
