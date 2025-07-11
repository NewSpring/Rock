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
using System.Globalization;
using Rock.Utility.Enums;

namespace Rock
{
    /// <summary>
    /// DateTime and TimeStamp Extensions
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// The _gregorian calendar
        /// from http://stackoverflow.com/a/2136549/1755417
        /// </summary>
        private static GregorianCalendar _gregorianCalendar = new GregorianCalendar();

        #region DateTime Extensions

        /// <summary>
        /// Returns the age at the current date.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public static int Age( this DateTime? start )
        {
            if ( start.HasValue )
                return start.Value.Age();
            else
                return 0;
        }

        /// <summary>
        /// Returns the age at the current date.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public static int Age( this DateTime start )
        {
            var now = RockDateTime.Today;
            int age = now.Year - start.Year;

            if ( start > now.AddYears( -age ) )
            {
                age--;
            }

            return age;
        }

        /// <summary>
        /// The total number of months between the two dates.
        /// </summary>
        /// <param name="start">The start date.</param>
        /// <param name="end">The end date.</param>
        /// <returns></returns>
        public static int TotalMonths( this DateTime end, DateTime start )
        {
            return ( end.Year * 12 + end.Month ) - ( start.Year * 12 + start.Month );
        }

        /// <summary>
        /// The number of whole years between the start and end dates.
        /// </summary>
        /// <param name="start">The start date.</param>
        /// <param name="end">The end date.</param>
        /// <returns>A positive number of years if the start date is before the end date, otherwise a negative number.</returns>
        public static int TotalYears( this DateTime end, DateTime start )
        {
            var years = (int)Math.Truncate( ( end.Subtract( start ).TotalDays - LeapDaysBetweenYears( start.Year, end.Year ) ) / 365 );
            return years;
        }

        /// <summary>
        /// Calculates the number of leap days that have occurred occur between two years.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static int LeapDaysBetweenYears( int start, int end )
        {
            // A given year qualifies as a leap year if:
            // 1. it can be divided by 4, and...
            // 2. it can't be divided by 100, except if it can also be divided by 400.
            return Math.Abs( LeapYearsBefore( end ) - LeapYearsBefore( start + 1 ) );
        }

        private static int LeapYearsBefore( int year )
        {
            year--;
            return ( year / 4 ) - ( year / 100 ) + ( year / 400 );
        }

        /// <summary>
        /// Returns a friendly elapsed time string.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <param name="includeTime">if set to <c>true</c> [include time].</param>
        /// <returns></returns>
        public static string ToElapsedString( this DateTime? dateTime, bool condensed = false, bool includeTime = true )
        {
            return dateTime?.ToElapsedString( condensed, includeTime ) ?? string.Empty;
        }

        /// <summary>
        /// Returns a friendly elapsed time string.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <param name="includeTime">if set to <c>true</c> [include time].</param>
        /// <returns></returns>
        public static string ToElapsedString( this DateTime dateTime, bool condensed = false, bool includeTime = true )
        {
            return ToElapsedString( dateTime, RockDateTime.Now, condensed, includeTime );
        }

        /// <summary>
        /// Returns a friendly elapsed time string.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="currentDateTime">The time to use as the current date time for the calculation.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <param name="includeTime">if set to <c>true</c> [include time].</param>
        /// <returns></returns>
        internal static string ToElapsedString( DateTime dateTime, DateTime currentDateTime, bool condensed = false, bool includeTime = true )
        {
            var start = dateTime;
            var end = currentDateTime;
            var direction = " Ago";
            var duration = "";
            var timeSpan = end.Subtract( start );

            if ( timeSpan.TotalMilliseconds < 0 )
            {
                direction = " From Now";
                start = end;
                end = dateTime;
                timeSpan = timeSpan.Negate();
            }

            if ( timeSpan.TotalHours < 24 && includeTime )
            {
                // Less than one second
                if ( timeSpan.TotalSeconds < 2 )
                {
                    duration = string.Format( "1{0}", condensed ? "sec" : " Second" );
                }
                else if ( timeSpan.TotalSeconds < 60 )
                {
                    duration = string.Format( "{0:N0}{1}", Math.Truncate( timeSpan.TotalSeconds ), condensed ? "sec" : " Seconds" );
                }
                else if ( timeSpan.TotalMinutes < 2 )
                {
                    duration = string.Format( "1{0}", condensed ? "min" : " Minute" );
                }
                else if ( timeSpan.TotalMinutes < 60 )
                {
                    duration = string.Format( "{0:N0}{1}", Math.Truncate( timeSpan.TotalMinutes ), condensed ? "min" : " Minutes" );
                }
                else if ( timeSpan.TotalHours < 2 )
                {
                    duration = string.Format( "1{0}", condensed ? "hr" : " Hour" );
                }
                else if ( timeSpan.TotalHours < 24 )
                {
                    duration = string.Format( "{0:N0}{1}", Math.Truncate( timeSpan.TotalHours ), condensed ? "hr" : " Hours" );
                }
            }

            if ( duration == "" )
            {
                if ( timeSpan.TotalDays < 2 )
                {
                    duration = string.Format( "1{0}", condensed ? "day" : " Day" );
                }
                else if ( timeSpan.TotalDays < 31 )
                {
                    duration = string.Format( "{0:N0}{1}", Math.Truncate( timeSpan.TotalDays ), condensed ? "days" : " Days" );
                }
                else if ( end.TotalMonths( start ) <= 1 )
                {
                    duration = string.Format( "1{0}", condensed ? "mon" : " Month" );
                }
                else if ( end.TotalMonths( start ) <= 18 )
                {
                    duration = string.Format( "{0:N0}{1}", end.TotalMonths( start ), condensed ? "mon" : " Months" );
                }
                else if ( end.TotalYears( start ) <= 1 )
                {
                    duration = string.Format( "1{0}", condensed ? "yr" : " Year" );
                }
                else
                {
                    duration = string.Format( "{0:N0}{1}", end.TotalYears( start ), condensed ? "yrs" : " Years" );
                }
            }

            return duration + ( condensed ? "" : direction );
        }

        /// <summary>
        /// Returns a string in FB style relative format (x seconds ago, x minutes ago, about an hour ago, etc.).
        /// or if max days has already passed in FB datetime format (February 13 at 11:28am or November 5, 2011 at 1:57pm).
        /// </summary>
        /// <param name="dateTime">the datetime to convert to relative time.</param>
        /// <param name="maxDays">maximum number of days before formatting in FB date-time format (ex. November 5, 2011 at 1:57pm)</param>
        /// <returns></returns>
        public static string ToRelativeDateString( this DateTime? dateTime, int? maxDays = null )
        {
            return dateTime?.ToRelativeDateString( maxDays ) ?? string.Empty;
        }

        /// <summary>
        /// Returns the value for <see cref="DateTime.ToShortDateString"/> or empty string if the date is null
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string ToShortDateString( this DateTime? dateTime )
        {
            return dateTime?.ToShortDateString() ?? string.Empty;
        }

        /// <summary>
        /// Returns the dateTime in ISO-8601 ( https://en.wikipedia.org/wiki/ISO_8601 ) format. Use this when serializing a date/time as an AttributeValue, UserPreference, etc
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string ToISO8601DateString( this DateTime? dateTime )
        {
            return dateTime?.ToISO8601DateString() ?? string.Empty;
        }

        /// <summary>
        /// Returns the dateTime in ISO-8601 ( https://en.wikipedia.org/wiki/ISO_8601 ) format. Use this when serializing a date/time as an AttributeValue, UserPreference, etc
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string ToISO8601DateString( this DateTime dateTime )
        {
            return dateTime.ToString( "o" ) ?? string.Empty;
        }

        /// <summary>
        /// Returns the <paramref name="dateTime"/> in RFC3339 ( https://www.ietf.org/rfc/rfc3339.txt ) UTC format.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>The <paramref name="dateTime"/> in RFC3339 UTC format.</returns>
        public static string ToRfc3339UtcString( this DateTime dateTime )
        {
            // Turn the provided DateTime into a DateTimeOffset with the org's offset.
            var rockDateTimeOffset = dateTime.ToRockDateTimeOffset();

            // Then convert that offset-based time to UTC.
            var utcDateTimeOffset = rockDateTimeOffset.ToUniversalTime();

            // Format with 'Z' for RFC 3339 UTC representation.
            return utcDateTimeOffset.ToString( "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" );
        }

        /// <summary>
        /// Returns a string in relative format (x seconds ago, x minutes ago, about an hour ago, in x seconds,
        /// in x minutes, in about an hour, etc.) or if time difference is greater than max days in long format (February
        /// 13 at 11:28am or November 5, 2011 at 1:57pm).
        /// </summary>
        /// <param name="dateTime">the datetime to convert to relative time.</param>
        /// <param name="maxDays">maximum number of days before formatting in long format (ex. November 5, 2011 at 1:57pm) </param>
        /// <returns></returns>
        public static string ToRelativeDateString( this DateTime dateTime, int? maxDays = null )
        {
            try
            {
                DateTime now = RockDateTime.Now;

                string nowText = "just now";
                string format = "{0} ago";
                TimeSpan timeSpan = now - dateTime;
                if ( dateTime > now )
                {
                    nowText = "now";
                    format = "in {0}";
                    timeSpan = dateTime - now;
                }

                double seconds = timeSpan.TotalSeconds;
                double minutes = timeSpan.TotalMinutes;
                double hours = timeSpan.TotalHours;
                double days = timeSpan.TotalDays;
                double weeks = days / 7;
                double months = days / 30;
                double years = days / 365;

                // Just return in long format if max days has passed.
                if ( maxDays.HasValue && days > maxDays )
                {
                    if ( now.Year == dateTime.Year )
                    {
                        return dateTime.ToString( @"MMMM d a\t h:mm tt" );
                    }
                    else
                    {
                        return dateTime.ToString( @"MMMM d, yyyy a\t h:mm tt" );
                    }
                }

                if ( Math.Round( seconds ) < 5 )
                {
                    return nowText;
                }
                else if ( minutes < 1.0 )
                {
                    return string.Format( format, Math.Floor( seconds ) + " seconds" );
                }
                else if ( Math.Floor( minutes ) == 1 )
                {
                    return string.Format( format, "1 minute" );
                }
                else if ( hours < 1.0 )
                {
                    return string.Format( format, Math.Floor( minutes ) + " minutes" );
                }
                else if ( Math.Floor( hours ) == 1 )
                {
                    return string.Format( format, "about an hour" );
                }
                else if ( days < 1.0 )
                {
                    return string.Format( format, Math.Floor( hours ) + " hours" );
                }
                else if ( Math.Floor( days ) == 1 )
                {
                    return string.Format( format, "1 day" );
                }
                else if ( weeks < 1 )
                {
                    return string.Format( format, Math.Floor( days ) + " days" );
                }
                else if ( Math.Floor( weeks ) == 1 )
                {
                    return string.Format( format, "1 week" );
                }
                else if ( months < 3 )
                {
                    return string.Format( format, Math.Floor( weeks ) + " weeks" );
                }
                else if ( months <= 12 )
                {
                    return string.Format( format, Math.Floor( months ) + " months" );
                }
                else if ( Math.Floor( years ) <= 1 )
                {
                    return string.Format( format, "1 year" );
                }
                else
                {
                    return string.Format( format, Math.Floor( years ) + " years" );
                }
            }
            catch ( Exception )
            {
            }
            return "";
        }

        /// <summary>
        /// Converts the date to an Epoch of milliseconds since 1970/1/1.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static long ToJavascriptMilliseconds( this DateTime dateTime )
        {
            return (long)( dateTime.ToUniversalTime() - new DateTime( 1970, 1, 1 ) ).TotalMilliseconds;
        }

        /// <summary>
        /// Converts the date to a string containing month and day values ( culture-specific ).
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string ToMonthDayString( this DateTime dateTime )
        {
            var dtf = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat;
            string mdp = dtf.ShortDatePattern;
            mdp = mdp.Replace( dtf.DateSeparator + "yyyy", "" ).Replace( "yyyy" + dtf.DateSeparator, "" );
            return dateTime.ToString( mdp );
        }

        /// <summary>
        /// Returns the date of the start of the month for the specified date/time.
        /// For example 3/23/2021 11:15am will return 3/1/2021 00:00:00.
        /// </summary>
        /// <param name="dt">The DateTime.</param>
        /// <returns></returns>
        public static DateTime StartOfMonth( this DateTime dt )
        {
            return new DateTime( dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind );
        }

        /// <summary>
        /// Returns the Date of the last day of the month.
        /// For example 3/23/2021 11:15am will return 3/31/2021 00:00:00.
        /// </summary>
        /// <param name="dt">The DateTime</param>
        /// <returns></returns>
        public static DateTime EndOfMonth( this DateTime dt )
        {
            return new DateTime( dt.Year, dt.Month, 1 ).AddMonths( 1 ).Subtract( new TimeSpan( 1, 0, 0, 0, 0 ) );
        }

        /// <summary>
        /// Returns the date of the start of the week for the specified date/time
        /// For example, if Monday is considered the start of the week: "2015-05-13" would return "2015-05-11"
        /// from http://stackoverflow.com/a/38064/1755417
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <param name="startOfWeek">The start of week.</param>
        /// <returns></returns>
        public static DateTime StartOfWeek( this DateTime dt, DayOfWeek startOfWeek )
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if ( diff < 0 )
            {
                diff += 7;
            }

            return dt.AddDays( -1 * diff ).Date;
        }

        /// <summary>
        /// Returns the date of the last day of the week for the specified date/time
        /// For example, if Monday is considered the start of the week: "2015-05-13" would return "2015-05-17"
        /// from http://stackoverflow.com/a/38064/1755417
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <param name="startOfWeek">The start of week.</param>
        /// <returns></returns>
        public static DateTime EndOfWeek( this DateTime dt, DayOfWeek startOfWeek )
        {
            return dt.StartOfWeek( startOfWeek ).AddDays( 6 );
        }

        /// <summary>
        /// Gets the Date of which Sunday is associated with the specified Date/Time, based on <see cref="RockDateTime.FirstDayOfWeek" />
        /// </summary>
        /// <param name="dt">The DateTime.</param>
        /// <returns></returns>
        public static DateTime SundayDate( this DateTime dt )
        {
            return RockDateTime.GetSundayDate( dt );
        }

        /// <summary>
        /// Gets the week of month.
        /// from http://stackoverflow.com/a/2136549/1755417 but with an option to specify the FirstDayOfWeek
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="firstDayOfWeek">The first day of week. For example" RockDateTime.FirstDayOfWeek</param>
        /// <returns></returns>
        public static int GetWeekOfMonth( this DateTime dateTime, DayOfWeek firstDayOfWeek )
        {
            DateTime first = new DateTime( dateTime.Year, dateTime.Month, 1 );

            // note: CalendarWeekRule doesn't matter since we are subtracting
            return dateTime.GetWeekOfYear( CalendarWeekRule.FirstDay, firstDayOfWeek ) - first.GetWeekOfYear( CalendarWeekRule.FirstDay, firstDayOfWeek ) + 1;
        }

        /// <summary>
        /// Gets the week of year.
        /// from http://stackoverflow.com/a/2136549/1755417, but with an option to specify the FirstDayOfWeek (for example RockDateTime.FirstDayOfWeek)
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="calendarWeekRule">The calendar week rule.</param>
        /// <param name="firstDayOfWeek">The first day of week.</param>
        /// <returns></returns>
        public static int GetWeekOfYear( this DateTime dateTime, CalendarWeekRule calendarWeekRule, DayOfWeek firstDayOfWeek )
        {
            return _gregorianCalendar.GetWeekOfYear( dateTime, calendarWeekRule, firstDayOfWeek );
        }

        /// <summary>
        /// Converts a datetime to the short date/time format.
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <returns></returns>
        public static string ToShortDateTimeString( this DateTime dt )
        {
            return dt.ToShortDateString() + " " + dt.ToShortTimeString();
        }

        /// <summary>
        /// To the RFC822 date time.
        /// From https://madskristensen.net/blog/convert-a-date-to-the-rfc822-standard-for-use-in-rss-feeds/
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string ToRfc822DateTime( this DateTime dateTime )
        {
            int offset = TimeZoneInfo.Local.GetUtcOffset( DateTime.Now ).Hours;
            string timeZone = "+" + offset.ToString().PadLeft( 2, '0' );

            if ( offset < 0 )
            {
                int i = offset * -1;
                timeZone = "-" + i.ToString().PadLeft( 2, '0' );
            }

            return dateTime.ToString( "ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight( 5, '0' ) );
        }

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static string GetFormattedAge( this DateTime dateTime )
        {
            DateTime today = RockDateTime.Today;
            int age = today.Year - dateTime.Year;
            if ( dateTime > today.AddYears( -age ) )
            {
                // their birthdate is after today's date, so they aren't a year older yet
                age--;
            }

            if ( age > 0 )
            {
                return age + ( age == 1 ? " yr" : " yrs" );
            }
            else if ( age < -1 )
            {
                return string.Empty;
            }

            int months = today.Month - dateTime.Month;
            if ( dateTime.Year < today.Year )
            {
                months = months + 12;
            }
            if ( dateTime.Day > today.Day )
            {
                months--;
            }
            if ( months > 0 )
            {
                return months + ( months == 1 ? " mo" : " mos" );
            }

            int days = today.Day - dateTime.Day;
            if ( days < 0 )
            {
                // Add the number of days in the birth month
                var birthMonth = new DateTime( dateTime.Year, dateTime.Month, 1 );
                days = days + birthMonth.AddMonths( 1 ).AddDays( -1 ).Day;
            }
            return days + ( days == 1 ? " day" : " days" );
        }

        /// <summary>
        ///  Determines whether the DateTime is in the future.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>true if the value is in the future, false if not.</returns>
        public static bool IsFuture( this DateTime dateTime )
        {
            return dateTime > RockDateTime.Now;
        }

        /// <summary>
        ///  Determines whether the DateTime is in the past.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>true if the value is in the past, false if not.</returns>
        public static bool IsPast( this DateTime dateTime )
        {
            return dateTime < RockDateTime.Now;
        }

        /// <summary>
        ///  Determines whether the DateTime is today.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>true if the value is in the past, false if not.</returns>
        public static bool IsToday( this DateTime dateTime )
        {
            return dateTime.Date == RockDateTime.Today;
        }

        /// <summary>
        /// Gets the date key. For example: 3/24/2021 11:15 am, will return "20210324".
        /// This handy for the various DateKey columns used in some of Rock tables
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static int AsDateKey( this DateTime dateTime )
        {
            return dateTime.ToString( "yyyyMMdd" ).AsInteger();
        }

        /// <summary>
        ///  Gets the Start of the Day for the specified DateTime.
        ///  For example: 3/24/2021 11:15 am, will return 3/24/2021 00:00:00.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static DateTime StartOfDay( this DateTime dateTime )
        {
            return new DateTime( dateTime.Year, dateTime.Month, dateTime.Day );
        }

        /// <summary>
        ///  Gets the End of the Day (last millisecond) for the specified DateTime.
        ///  For example: 3/24/2021 11:15 am, will return 3/24/2021 23:59:59:999.
        ///  Gets the DateTime of the last day of the year with the time set to "23:59:59:999". The last moment of the last day of the year.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static DateTime EndOfDay( this DateTime dateTime )
        {
            return new DateTime( dateTime.Year, dateTime.Month, dateTime.Day ).AddDays( 1 ).Subtract( new TimeSpan( 0, 0, 0, 0, 1 ) );
        }

        /// <summary>
        ///  Gets the End of the Year (last millisecond) for the specified DateTime.
        ///  For example: 3/24/2021 11:15 am, will return 12/31/2021 23:59:59:999.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static DateTime EndOfYear( this DateTime dateTime )
        {
            return new DateTime( dateTime.Year, 1, 1 ).AddYears( 1 ).Subtract( new TimeSpan( 1, 0, 0, 0, 0 ) );
        }

        /// <summary>
        /// Returns the date of the start of the year for the specified date/time.
        /// For example 3/23/2021 11:15am will return 1/1/2021 00:00:00.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        public static DateTime StartOfYear( this DateTime dateTime )
        {
            return new DateTime( dateTime.Year, 1, 1 );
        }

        /// <summary>
        /// Gets the next weekday.
        /// https://stackoverflow.com/questions/6346119/datetime-get-next-tuesday
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <param name="day">The day.</param>
        /// <returns></returns>
        public static DateTime GetNextWeekday( this DateTime dateTime, DayOfWeek day )
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ( ( int ) day - ( int ) dateTime.DayOfWeek + 7 ) % 7;
            return dateTime.AddDays( daysToAdd );
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> that is in <see cref="RockDateTime.OrgTimeZoneInfo"/>
        /// into a <see cref="DateTimeOffset"/> that is also in the organization time zone.
        /// <param name="dateTime">The Rock date time.</param>
        /// <returns>The <see cref="DateTimeOffset"/> instance that specifies the same point in time.</returns>
        /// </summary>
        public static DateTimeOffset ToRockDateTimeOffset( this DateTime dateTime )
        {
            // We can only apply a time zone offset to an unspecified type.
            var unspecifiedDateTime = DateTime.SpecifyKind( dateTime, DateTimeKind.Unspecified );

            return new DateTimeOffset( unspecifiedDateTime, RockDateTime.OrgTimeZoneInfo.GetUtcOffset( unspecifiedDateTime ) );
        }

        /// <summary>
        /// Takes a DateTimeOffset and converts it into the organization timezone.
        /// </summary>
        /// <param name="dateTimeOffset">The date time offset.</param>
        /// <remarks>This probably isn't the method you're looking for.
        /// In 99.9% of cases you should be able to use the .DateTime property,
        /// but this is used when a remote client has to set a specific spot in time.</remarks>
        /// <returns>DateTime.</returns>
        public static DateTime ToOrganizationDateTime( this DateTimeOffset dateTimeOffset )
        {
            return dateTimeOffset.ToOffset( RockDateTime.OrgTimeZoneInfo.GetUtcOffset( dateTimeOffset ) ).DateTime;
        }

        /// <summary>
        /// Converts to the flag (bit per day) friendly enum.
        /// </summary>
        /// <param name="dayOfWeek">The day of week.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Rock is not familiar with this day of the week: {dateTime.DayOfWeek}</exception>
        public static DayOfWeekFlag AsFlag( this DayOfWeek dayOfWeek )
        {
            switch ( dayOfWeek )
            {
                case DayOfWeek.Sunday:
                    return DayOfWeekFlag.Sunday;
                case DayOfWeek.Monday:
                    return DayOfWeekFlag.Monday;
                case DayOfWeek.Tuesday:
                    return DayOfWeekFlag.Tuesday;
                case DayOfWeek.Wednesday:
                    return DayOfWeekFlag.Wednesday;
                case DayOfWeek.Thursday:
                    return DayOfWeekFlag.Thursday;
                case DayOfWeek.Friday:
                    return DayOfWeekFlag.Friday;
                case DayOfWeek.Saturday:
                    return DayOfWeekFlag.Saturday;
                default:
                    throw new Exception( $"Rock is not familiar with this day of the week: {dayOfWeek}" );
            }
        }

        /// <summary>
        /// Converts the flags to "day of week" enum list.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <returns></returns>
        public static List<DayOfWeek> AsDayOfWeekList( this DayOfWeekFlag flags )
        {
            var enums = new List<DayOfWeek>();

            if ( ( flags & DayOfWeekFlag.Sunday ) == DayOfWeekFlag.Sunday )
            {
                enums.Add( DayOfWeek.Sunday );
            }

            if ( ( flags & DayOfWeekFlag.Monday ) == DayOfWeekFlag.Monday )
            {
                enums.Add( DayOfWeek.Monday );
            }

            if ( ( flags & DayOfWeekFlag.Tuesday ) == DayOfWeekFlag.Tuesday )
            {
                enums.Add( DayOfWeek.Tuesday );
            }

            if ( ( flags & DayOfWeekFlag.Wednesday ) == DayOfWeekFlag.Wednesday )
            {
                enums.Add( DayOfWeek.Wednesday );
            }

            if ( ( flags & DayOfWeekFlag.Thursday ) == DayOfWeekFlag.Thursday )
            {
                enums.Add( DayOfWeek.Thursday );
            }

            if ( ( flags & DayOfWeekFlag.Friday ) == DayOfWeekFlag.Friday )
            {
                enums.Add( DayOfWeek.Friday );
            }

            if ( ( flags & DayOfWeekFlag.Saturday ) == DayOfWeekFlag.Saturday )
            {
                enums.Add( DayOfWeek.Saturday );
            }

            return enums;
        }

        #endregion DateTime Extensions

        #region TimeSpan Extensions

        /// <summary>
        /// Returns a TimeSpan as h:mm AM/PM (culture invariant)
        /// Examples: 1:45 PM, 12:01 AM
        /// </summary>
        /// <param name="timespan">The timespan.</param>
        /// <returns></returns>
        public static string ToTimeString( this TimeSpan timespan )
        {
            // since the comments on this say HH:MM AM/PM, make sure to return the time in that format
            return RockDateTime.Today.Add( timespan ).ToString( "h:mm tt", System.Globalization.CultureInfo.InvariantCulture );
        }

        #endregion TimeSpan Extensions

        #region Time/Date Rounding 

        /// <summary>
        /// Rounds the specified rounding interval.
        /// from https://stackoverflow.com/a/4108889/1755417
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="roundingInterval">The rounding interval.</param>
        /// <param name="roundingType">Type of the rounding.</param>
        /// <returns></returns>
        public static TimeSpan Round( this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType )
        {
            return new TimeSpan(
                Convert.ToInt64( Math.Round(
                    time.Ticks / ( decimal ) roundingInterval.Ticks,
                    roundingType
                ) ) * roundingInterval.Ticks
            );
        }

        /// <summary>
        /// Rounds the specified rounding interval.
        /// from https://stackoverflow.com/a/4108889/1755417
        /// </summary>
        /// <example>
        /// new TimeSpan(0, 2, 26).Round( TimeSpan.FromSeconds(5)); // rounds to 00:02:25
        /// new TimeSpan(3, 34, 0).Round( TimeSpan.FromMinutes(30); // round to 03:30
        /// </example>
        /// <param name="time">The time.</param>
        /// <param name="roundingInterval">The rounding interval.</param>
        /// <returns></returns>
        public static TimeSpan Round( this TimeSpan time, TimeSpan roundingInterval )
        {
            return Round( time, roundingInterval, MidpointRounding.ToEven );
        }

        /// <summary>
        /// Rounds the specified rounding interval.
        /// from https://stackoverflow.com/a/4108889/1755417
        /// </summary>
        /// <example>
        /// new DateTime(2010, 11, 4, 10, 28, 27).Round( TimeSpan.FromMinutes(1) ); // rounds to 2010.11.04 10:28:00
        /// new DateTime(2010, 11, 4, 13, 28, 27).Round( TimeSpan.FromDays(1) ); // rounds to 2010.11.05 00:00
        /// </example>
        /// <param name="datetime">The datetime.</param>
        /// <param name="roundingInterval">The rounding interval.</param>
        /// <returns></returns>
        public static DateTime Round( this DateTime datetime, TimeSpan roundingInterval )
        {
            return new DateTime( ( datetime - DateTime.MinValue ).Round( roundingInterval ).Ticks );
        }

        #endregion Time/Date Rounding 
    }
}
