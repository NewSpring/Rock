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

namespace Rock.SystemGuid
{
    /// <summary>
    /// System Group Types
    /// </summary>
    public static class GroupType
    {
        /// <summary>
        /// Security Role group type guid
        /// </summary>
        public const string GROUPTYPE_SECURITY_ROLE = "AECE949F-704C-483E-A4FB-93D5E4720C4C";

        /// <summary>
        /// Family group type guid
        /// </summary>
        public const string GROUPTYPE_FAMILY = "790E3215-3B10-442B-AF69-616C0DCB998E";

        /// <summary>
        /// Event Attendees group type guid
        /// </summary>
        public const string GROUPTYPE_EVENTATTENDEES = "3311132B-268D-44E9-811A-A56A0835E50A";

        /// <summary>
        /// Know relationship group type guid
        /// </summary>
        public const string GROUPTYPE_KNOWN_RELATIONSHIPS = "E0C5A0E2-B7B3-4EF4-820D-BBF7F9A374EF";

        /// <summary>
        /// Learning Management System (LMS) Class group type guid
        /// </summary>
        public const string GROUPTYPE_LMS_CLASS = "4BBC41E2-0A37-4289-B7A7-756B9FE8F775";

        /// <summary>
        /// Peer network group type guid
        /// </summary>
        [RockObsolete( "17.0" )]
        [Obsolete( "Peer Networks of this Group Type have been replaced with a formal Peer Network feature." )]
        public const string GROUPTYPE_PEER_NETWORK = "8C0E5852-F08F-4327-9AA5-87800A6AB53E";

        /// <summary>
        /// Serving Team group type guid
        /// </summary>
        public const string GROUPTYPE_SERVING_TEAM = "2C42B2D4-1C5F-4AD5-A9AD-08631B872AC4";

        /// <summary>
        /// Small Group Section group type guid
        /// </summary>
        public const string GROUPTYPE_SMALL_GROUP_SECTION = "FAB75EC6-0402-456A-BE34-252097DE4F20";

        /// <summary>
        /// Small Group group type guid
        /// </summary>
        public const string GROUPTYPE_SMALL_GROUP = "50FCFB30-F51A-49DF-86F4-2B176EA1820B";

        /// <summary>
        /// Application Group group type guid
        /// </summary>
        public const string GROUPTYPE_APPLICATION_GROUP = "3981CF6D-7D15-4B57-AACE-C0E25D28BD49";

        /// <summary>
        /// Organization Unit group type guid
        /// </summary>
        public const string GROUPTYPE_ORGANIZATION_UNIT = "AAB2E9F4-E828-4FEE-8467-73DC9DAB784C";

        /// <summary>
        /// Organization Unit group type guid
        /// </summary>
        public const string GROUPTYPE_WEEKLY_SERVICE_CHECKIN_AREA = "FEDD389A-616F-4A53-906C-63D8255631C5";

        /// <summary>
        /// Fundraising Opportunity group type guid
        /// </summary>
        public const string GROUPTYPE_FUNDRAISINGOPPORTUNITY = "4BE7FC44-332D-40A8-978E-47B7035D7A0C";

        /// <summary>
        /// General group type guid
        /// </summary>
        public const string GROUPTYPE_GENERAL = "8400497B-C52F-40AE-A529-3FCCB9587101";

        /// <summary>
        /// Communication List group type guid
        /// </summary>
        public const string GROUPTYPE_COMMUNICATIONLIST = "D1D95777-FFA3-CBB3-4A6D-658706DAED33";

        /// <summary>
        /// The Service Attendance group type guid
        /// </summary>
        public const string GROUPTYPE_SERVICE_ATTENDANCE = "77713830-AE5E-4B1A-94FA-E145DFF85035";

        /// <summary>
        /// The Services group type guid
        /// </summary>
        public const string GROUPTYPE_SERVICES = "235BAE2B-5760-4763-AADF-3938F34BA100";

        /// <summary>
        /// Campus Team group type guid
        /// </summary>
        public const string GROUPTYPE_CAMPUS_TEAM = "BADD7A6C-1FB3-4E11-A721-6D1377C6958C";

        /// <summary>
        /// Sign-up Group group type guid
        /// </summary>
        public const string GROUPTYPE_SIGNUP_GROUP = "499B1367-06B3-4538-9D56-56D53F55DCB1";

        /// <summary>
        /// Chat Direct Message group type guid
        /// </summary>
        public const string GROUPTYPE_CHAT_DIRECT_MESSAGE = "DFDC14A3-D1DC-4342-A012-5CE9E8994B5E";

        /// <summary>
        /// Chat Shared Channel group type guid
        /// </summary>
        public const string GROUPTYPE_CHAT_SHARED_CHANNEL = "268D4802-24C7-4891-890E-71313BEE17B1";

        /// <summary>
        /// Hidden Application Group group type guid
        /// </summary>
        public const string GROUPTYPE_HIDDEN_APPLICATION_GROUP = "2C6F2847-B404-4595-AB35-CE42F2303868";
    }
}