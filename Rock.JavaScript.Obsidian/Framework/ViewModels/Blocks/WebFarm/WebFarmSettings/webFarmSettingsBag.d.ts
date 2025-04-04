//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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

import { WebFarmNodeBag } from "@Obsidian/ViewModels/Blocks/WebFarm/WebFarmNodeDetail/webFarmNodeBag";
import { PublicAttributeBag } from "@Obsidian/ViewModels/Utility/publicAttributeBag";

/** Contains details on the web farm settings. */
export type WebFarmSettingsBag = {
    /** Gets or sets the attributes. */
    attributes?: Record<string, PublicAttributeBag> | null;

    /** Gets or sets the attribute values. */
    attributeValues?: Record<string, string> | null;

    /** Gets or sets a value indicating whether the web farm has a valid key. */
    hasValidKey: boolean;

    /** Gets or sets the identifier key of this entity. */
    idKey?: string | null;

    /** Gets or sets a value indicating whether this instance is active. */
    isEnabled: boolean;

    /** Gets or sets a value indicating whether this instance is in memory transport. */
    isInMemoryTransport: boolean;

    /** Gets or sets a value indicating whether the web farm is running. */
    isRunning: boolean;

    /** Gets or sets the lower polling limit. */
    lowerPollingLimit?: number | null;

    /** Gets or sets the maximum polling wait seconds. */
    maxPollingWaitSeconds?: number | null;

    /** Gets or sets the minimum polling difference. */
    minimumPollingDifference?: number | null;

    /** Gets or sets the nodes. */
    nodes?: WebFarmNodeBag[] | null;

    /** Gets or sets the upper polling limit. */
    upperPollingLimit?: number | null;

    /** Gets or sets the web farm key. */
    webFarmKey?: string | null;
};
