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

import { Guid } from "@Obsidian/Types";

/**
 * The options that can be passed to the SetFollowing API action of
 * the Following control.
 */
export type FollowingSetFollowingOptionsBag = {
    /**
     * Gets or sets the entity key used with Rock.ViewModels.Rest.Controls.FollowingSetFollowingOptionsBag.EntityTypeGuid
     * to locate the entity.
     */
    entityKey?: string | null;

    /** Gets or sets the entity type unique identifier. */
    entityTypeGuid: Guid;

    /** Gets or sets a value indicating whether the entity should be followed. */
    isFollowing: boolean;

    /**
     * Gets or sets the purpose key to use when checking if the entity is
     * currently being followed.
     */
    purposeKey?: string | null;
};
