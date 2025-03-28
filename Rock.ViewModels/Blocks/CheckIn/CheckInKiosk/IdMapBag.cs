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

namespace Rock.ViewModels.Blocks.CheckIn.CheckInKiosk
{
    /// <summary>
    /// Identifies a mapping between identifiers for an object.
    /// </summary>
    public class IdMapBag
    {
        /// <summary>
        /// The encrypted identifier that represents the same item as the
        /// unique identifier.
        /// </summary>
        public string IdKey { get; set; }

        /// <summary>
        /// The unique identifier the represents the same item as the
        /// encrypted identifier.
        /// </summary>
        public Guid Guid { get; set; }
    }
}
