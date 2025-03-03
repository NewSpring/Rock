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

import { WebKioskBag } from "@Obsidian/ViewModels/Blocks/CheckIn/CheckInKiosk/webKioskBag";
import { CheckInItemBag } from "@Obsidian/ViewModels/CheckIn/checkInItemBag";
import { ConfigurationTemplateBag } from "@Obsidian/ViewModels/CheckIn/configurationTemplateBag";

/**
 * Details about the configuration of a kiosk that will be used for check-in.
 * This is intended to contain everything required for the kiosk to start.
 */
export type KioskConfigurationBag = {
    /** Gets or sets the enabled areas. */
    areas?: CheckInItemBag[] | null;

    /** Gets or sets the kiosk details. */
    kiosk?: WebKioskBag | null;

    /** Gets or sets the check-in template. */
    template?: ConfigurationTemplateBag | null;
};
