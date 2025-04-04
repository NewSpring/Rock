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

namespace Rock.ViewModels.Rest.Controls
{
    /// <summary>
    /// The settings returned from the GetGoogleMapSettings API action of
    /// the GeoPicker control.
    /// </summary>
    public class GeoPickerGoogleMapSettingsBag: GeoPickerSettingsBag
    {
        /// <summary>
        /// API key for using Google Maps
        /// </summary>
        public string GoogleApiKey { get; set; }

        /// <summary>
        /// Gets or sets the google map identifier.
        /// </summary>
        /// <value>
        /// The google map identifier.
        /// </value>
        public string GoogleMapId { get; set; }
    }
}
