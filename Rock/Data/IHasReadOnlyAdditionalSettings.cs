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
using Rock.Attribute;

namespace Rock.Data
{
    /// <summary>
    /// Represents a model that supports reading categorized, additional settings.
    /// </summary>
    public interface IHasReadOnlyAdditionalSettings
    {
        /// <summary>
        /// Gets the additional settings JSON string.
        /// <para>
        /// DO NOT read from this property directly. Instead, use the <see cref="IHasReadOnlyAdditionalSettings"/>
        /// extension methods to ensure data is properly deserialized from this property.
        /// </para>
        /// </summary>
        /// <value>
        /// The additional settings JSON string.
        /// </value>
        string AdditionalSettingsJson { get; }
    }
}
