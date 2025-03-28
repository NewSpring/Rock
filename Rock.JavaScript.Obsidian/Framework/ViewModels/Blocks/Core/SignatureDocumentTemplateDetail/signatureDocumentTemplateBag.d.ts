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

import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { PublicAttributeBag } from "@Obsidian/ViewModels/Utility/publicAttributeBag";

export type SignatureDocumentTemplateBag = {
    /** Gets or sets the attributes. */
    attributes?: Record<string, PublicAttributeBag> | null;

    /** Gets or sets the attribute values. */
    attributeValues?: Record<string, string> | null;

    /** Gets or sets the type of the Rock.Model.BinaryFile. */
    binaryFileType?: ListItemBag | null;

    /** Gets or sets a value indicating whether this instance can administrate. */
    canAdministrate: boolean;

    /** The System Communication that will be used when sending the signature document completion email. */
    completionSystemCommunication?: ListItemBag | null;

    /** Gets or sets a user defined description or summary about the SignatureDocumentTemplate. */
    description?: string | null;

    /** The term used to simply describe the document (wavier, release form, etc.). */
    documentTerm?: string | null;

    /** Gets or sets the identifier key of this entity. */
    idKey?: string | null;

    /** Gets or sets a flag indicating if this item is active or not. */
    isActive: boolean;

    /**
     * Gets or sets a value indicating if the signature document made using this template
     * may be kept valid for future use.
     */
    isValidInFuture: boolean;

    /** The Lava template that will be used to build the signature document. */
    lavaTemplate?: string | null;

    /** Gets or sets the friendly Name of the SignatureDocumentTemplate. This property is required. */
    name?: string | null;

    /** Gets or sets the PDF URL. */
    pdfUrl?: string | null;

    /** Gets or sets the type of the entity. */
    providerEntityType?: ListItemBag | null;

    /** Gets or sets the provider template key. */
    providerTemplateKey?: string | null;

    /** Gets or sets the signature input types. */
    signatureInputTypes?: ListItemBag[] | null;

    /** Gets or sets the type of the signature. */
    signatureType?: string | null;

    /**
     * Gets or sets a number of days the signature document made form this template be deemed valid.
     * This property is honored only if the IsValidInFuture property is set.
     */
    validityDurationInDays?: string | null;
};
