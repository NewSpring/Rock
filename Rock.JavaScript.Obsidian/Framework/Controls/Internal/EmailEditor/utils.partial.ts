/* eslint-disable @typescript-eslint/naming-convention */
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

import {
    inject,
    InjectionKey,
    Ref,
} from "vue";
import {
    AccordionManager,
    CssStyleDeclarationKebabKey,
    ComponentMigrationHelper,
    ComponentTypeName,
    ContentAreaElements,
    DomWatcher,
    EditorComponentTypeName,
    HorizontalAlignment,
    StyleSheetElements,
    ValueConverter,
    ComponentStructure,
    TableElements,
    ButtonWidthModel,
    ButtonWidthMode,
    LetterCase,
    BorderModel,
    ShorthandModel,
    BorderStyle,
    ButtonComponentAdapter,
    ButtonLocalProps,
    ButtonGlobalProps,
    ShorthandPropertyNames,
    DividerLocalProps,
    DividerGlobalProps,
    DividerComponentAdapter,
    DividerGlobalAdapter,
    RsvpLocalProps,
    RsvpComponentAdapter,
    ButtonGlobalAdapter,
    ImageComponentAdapter,
    ImageLocalProps,
    ImageSizeModel,
    ResizeMode,
    ImageSourceModel,
    BodyGlobalAdapter,
    BodyGlobalProps,
    TextAlignment,
    GlobalAdapterSnapshot,
} from "./types.partial";
import { isElement, isHTMLElement, isHTMLTableElement, replaceTagName } from "@Obsidian/Utility/dom";
import { newGuid, toGuidOrNull } from "@Obsidian/Utility/guid";
import { Enumerable } from "@Obsidian/Utility/linq";
import { splitCase, toTitleCase } from "@Obsidian/Utility/stringUtils";
import { isNullish } from "@Obsidian/Utility/util";
import { toNumberOrNull } from "@Obsidian/Utility/numberUtils";

// #region Constants

export const AccordionGroupInjectionKey: InjectionKey<AccordionManager> = Symbol("accordion-group");

export const EmptyDropzoneSvgPixelWidth = 103;
export const DefaultEmailBackgroundColor = "#e7e7e7";

export const RockStylesCssClass = "rock-styles" as const;

/**
 * The outermost wrapper for the entire email.
 * - **Purpose:** Defines the full email structure.
 * - **Usage:** Apply global styles (e.g., background color).
 * - **Best Practice:** There should be exactly **one** `.email-wrapper` per email.
 */
export const EmailWrapperCssClass = "email-wrapper" as const;

/**
 * Add a temporary (runtime) CSS class with this prefix to any element that should be stripped out when the HTML is retrieved via getProcessedHtml().
 */
export const RockRuntimeClassCssClassPrefix = "rock-runtime-class" as const;

/**
 * Add this CSS class to any temporary (runtime) element that should be stripped out when the HTML is retrieved via getProcessedHtml().
 */
export const RockRuntimeElementCssClass = "rock-runtime-element" as const;

/**
 * Add this CSS class to any element that should be editable inline when selected in the email designer.
 */
export const RockCssClassContentEditable = `rock-content-editable` as const;

/**
 * Add this CSS class to any temporary element that wraps other elements, and should be unwrapped when the HTML is retrieved via getProcessedHtml().
 *
 * The wrapped elements will be placed in the DOM in place of the wrapper.
 */
export const RockRuntimeWrapperElementCssClass = "rock-runtime-wrapper-element" as const;

export const SmallEmptyClass = `${RockRuntimeClassCssClassPrefix}-small` as const;

export function getComponentCssClass(componentTypeName: ComponentTypeName): string {
    return `component-${componentTypeName}`;
}

export function getMarginWrapperTableCssClass(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `margin-wrapper-for-${componentTypeName}${wrapperSuffix}`;
}

export function getMarginWrapperTableSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getMarginWrapperTableCssClass(componentTypeName, wrapperSuffix)}`;
}

export function getMarginWrapperCellSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getMarginWrapperTableCssClass(componentTypeName, wrapperSuffix)} > tbody > tr > td`;
}

export function getBorderWrapperTableCssClass(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `border-wrapper-for-${componentTypeName}${wrapperSuffix}`;
}

export function getBorderWrapperTableSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getBorderWrapperTableCssClass(componentTypeName, wrapperSuffix)}`;
}

export function getBorderWrapperCellSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getBorderWrapperTableCssClass(componentTypeName, wrapperSuffix)} > tbody > tr > td`;
}

export function getPaddingWrapperTableCssClass(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `padding-wrapper-for-${componentTypeName}${wrapperSuffix}`;
}

export function getPaddingWrapperTableSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getPaddingWrapperTableCssClass(componentTypeName, wrapperSuffix)}`;
}

export function getPaddingWrapperCellSelector(componentTypeName: ComponentTypeName, wrapperSuffix: string = ""): string {
    return `.${getPaddingWrapperTableCssClass(componentTypeName, wrapperSuffix)} > tbody > tr > td`;
}

type ContentWrapperSupportedComponentType = Extract<ComponentTypeName, "text" | "code">;

export function getContentWrapperCssClass(componentTypeName: ContentWrapperSupportedComponentType, wrapperSuffix: string = ""): string {
    return `content-wrapper-for-${componentTypeName}${wrapperSuffix}`;
}

export function getContentWrapperSelector(componentTypeName: ContentWrapperSupportedComponentType, wrapperSuffix: string = ""): string {
    return `.${getContentWrapperCssClass(componentTypeName, wrapperSuffix)}`;
}

/**
 * These classes that can be added to globally style elements.
 */
export const GlobalCssClasses = {
    borderHeading1: `border-heading-1`,
    borderHeading2: `border-heading-2`,
    borderHeading3: `border-heading-3`,
    borderParagraph: `border-paragraph`,

    fontGlobal: `font-global`,
    fontHeading1: `font-heading-1`,
    fontHeading2: `font-heading-2`,
    fontHeading3: `font-heading-3`,
    fontParagraph: `font-paragraph`,

    marginHeading1: `margin-heading-1`,
    marginHeading2: `margin-heading-2`,
    marginHeading3: `margin-heading-3`,
    marginParagraph: `margin-paragraph`,

    paddingHeading1: `padding-heading-1`,
    paddingHeading2: `padding-heading-2`,
    paddingHeading3: `padding-heading-3`,
    paddingParagraph: `padding-paragraph`
} as const;

export const GlobalStylesCssSelectors = {
    backgroundColor: `.${EmailWrapperCssClass}`,
} as const;

// #endregion Constants

// #region Converters

export const numberToStringConverter: ValueConverter<number | null | undefined, string | null> = {
    toTarget(source: number | null | undefined): string | null {
        return isNullish(source) ? null : `${source}`;
    },

    toSource(target: string | null): number | null | undefined {
        if (isNullish(target)) {
            return target;
        }
        else if (!target) {
            return null;
        }
        else if (target === "0") {
            return 0;
        }
        else {
            try {
                const result = parseInt(target);

                if (isNaN(result)) {
                    return undefined;
                }
                else {
                    return result;
                }
            }
            catch {
                return undefined;
            }
        }
    }
};

export const stringConverter: ValueConverter<string | null | undefined, string | null> = {
    toTarget(source: string | null | undefined): string | null {
        return source || null;
    },

    toSource(target: string | null): string | null | undefined {
        return target || null;
    }
};

export const pixelConverter: ValueConverter<number | null | undefined, string | null> = {
    toTarget(source: number | null | undefined): string | null {
        return isNullish(source) ? null : `${source}px`;
    },

    toSource(target: string | null): number | null | undefined {
        if (isNullish(target)) {
            return target;
        }
        else if (!target) {
            return null;
        }
        else if (target === "0") {
            return 0;
        }
        else if (target.endsWith("px")) {
            return parseInt(target);
        }
        else {
            try {
                const result = parseInt(target);

                if (isNaN(result)) {
                    return undefined;
                }
                else {
                    return result;
                }
            }
            catch {
                return undefined;
            }
        }
    }
};

export const percentageConverter: ValueConverter<number | null | undefined, string | null> = {
    toTarget(source: number | null | undefined): string | null {
        return isNullish(source) ? null : `${source}%`;
    },

    toSource(target: string | null): number | null | undefined {
        if (isNullish(target)) {
            return target;
        }
        else if (!target) {
            return null;
        }
        else if (!target || target === "0") {
            return 0;
        }
        else if (target.endsWith("%")) {
            return parseInt(target);
        }
        else {
            try {
                return parseInt(target);
            }
            catch {
                return undefined;
            }
        }
    }
};

export const borderStyleConverter: ValueConverter<BorderStyle | null | undefined, string | null> = {
    toTarget(source: BorderStyle | null | undefined): string | null {
        return stringConverter.toTarget(source);
    },

    toSource(target: string | null): BorderStyle | null | undefined {
        return stringConverter.toSource(target) as BorderStyle | null | undefined;
    }
};

export const horizontalAlignmentConverter: ValueConverter<HorizontalAlignment | "" | null | undefined, string | null> = {
    toTarget: function (source: HorizontalAlignment | "" | null | undefined): string | null {
        return stringConverter.toTarget(source);
    },
    toSource: function (target: string | null): HorizontalAlignment | "" | null | undefined {
        return stringConverter.toSource(target) as HorizontalAlignment | "" | null | undefined;
    }
};

// #endregion Converters

// #region Functions

/** Uses an accordion group if one is set up. */
export function useAccordionGroup(isExpanded: Ref<boolean>): void {
    const accordionKey = newGuid();
    const group = inject(AccordionGroupInjectionKey);
    group?.register(accordionKey, isExpanded);
}

export function getComponentTypeName(componentElement: Element): ComponentTypeName {
    const classList = [...componentElement.classList];

    const map: Record<string, ComponentTypeName> = {
        "component-button": "button",
        "component-code": "code",
        "component-divider": "divider",
        "component-image": "image",
        "component-message": "message",
        "component-rsvp": "rsvp",
        "component-section": "section",
        "component-text": "text",
        "component-title": "title",
        "component-video": "video",
        "component-row": "row"
    };

    for (const key in map) {
        if (classList.includes(key)) {
            return map[key];
        }
    }

    throw new Error("Unable to get component type for element");
}

export function getComponentIconHtml(componentTypeName: EditorComponentTypeName): string {
    function createIconElement(iconCssClass: string): string {
        return `<i class="${iconCssClass} ti-lg"></i>`;
    }

    switch (componentTypeName) {
        case "title":
            return createIconElement("ti ti-typography");
        case "video":
            return createIconElement("ti ti-player-play");
        case "button":
            return `
<div style="background-color: var(--color-interface-strong); width: 60px; border-radius: var(--border-radius-base);">
    <i class="ti ti-pointer ti-sm" style="color: var(--color-interface-softest);"></i>
</div>
`;
        case "text":
            return createIconElement("ti ti-align-left");
        case "divider":
            return `
<div class="d-flex flex-column align-items-center" style="gap: var(--spacing-tiny);">
    <div style="width: 42px; height: 10px; background-color: var(--color-interface-soft);"></div>
    <div style="width: 56px; height: 2px; background-color: var(--color-interface-strong);"></div>
    <div style="width: 42px; height: 10px; background-color: var(--color-interface-soft);"></div>
</div>`;
        case "message":
            return createIconElement("ti ti-user");
        case "image":
            return createIconElement("ti ti-photo");
        case "code":
            return createIconElement("ti ti-code");
        case "rsvp":
            return createIconElement("ti ti-square-check");
        case "section":
            return createIconElement("rk rk-one-column");
        case "one-column-section":
            return `
<div class="d-flex" style="width: 65px; height: 31px;">
    <div style="flex-basis: 100%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "right-sidebar-section":
            return `
<div class="d-flex" style="gap: var(--spacing-tiny); width: 65px; height: 31px;">
    <div style="flex-basis: 66.666666%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 33.333333%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "left-sidebar-section":
            return `
<div class="d-flex" style="gap: var(--spacing-tiny); width: 65px; height: 31px;">
    <div style="flex-basis: 33.333333%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 66.666666%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "two-column-section":
            return `
<div class="d-flex" style="gap: var(--spacing-tiny); width: 65px; height: 31px;">
    <div style="flex-basis: 50%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 50%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "three-column-section":
            return `
<div class="d-flex" style="gap: var(--spacing-tiny); width: 65px; height: 31px;">
    <div style="flex-basis: 33.333333%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 33.333333%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 33.333333%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "four-column-section":
            return `
<div class="d-flex" style="gap: var(--spacing-tiny); width: 65px; height: 31px;">
    <div style="flex-basis: 25%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 25%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 25%; background-color: var(--color-interface-soft)"></div>
    <div style="flex-basis: 25%; background-color: var(--color-interface-soft)"></div>
</div>`;
        case "row":
            return `
<svg viewBox="70 93 373 326" fill="none" xmlns="http://www.w3.org/2000/svg" style="width: 32px; display: block;">
    <path fill-rule="evenodd" clip-rule="evenodd" d="M104.929 93H407.643C426.926 93 442.571 108.645 442.571 127.929V384.071C442.571 403.355 426.926 419 407.643 419H104.929C85.6451 419 70 403.355 70 384.071V127.929C70 108.645 85.6451 93 104.929 93ZM407.641 128.656H105.655V383.344H407.641V128.656Z" style="fill: var(--color-interface-strong);"></path>
    <path d="M106.141 206H408.141V306H106.141V206Z" style="fill: var(--color-interface-soft)"></path>
</svg>
`;
        default:
            console.warn(`Unable to retrieve the icon for the unknown component type: '${componentTypeName}'. Returning the default icon.`);
            return createIconElement("ti ti-question-mark");
    }
}

export function getComponentTitle(componentTypeName: EditorComponentTypeName): string {
    switch (componentTypeName) {
        case "title":
            return "Title";
        case "video":
            return "Video";
        case "button":
            return "Button";
        case "text":
            return "Paragraph";
        case "divider":
            return "Divider";
        case "message":
            return "Message";
        case "image":
            return "Image";
        case "code":
            return "Lava";
        case "rsvp":
            return "RSVP";
        case "section":
        case "one-column-section":
            return "1";
        case "right-sidebar-section":
        case "left-sidebar-section":
        case "two-column-section":
            return "2";
        case "three-column-section":
            return "3";
        case "four-column-section":
            return "4";
        case "row":
            return "Row";
        default:
            console.warn(`Unable to retrieve the title for the unknown component type, '${componentTypeName}'. Returning the default icon.`);
            return toTitleCase(splitCase(componentTypeName).replace("-", " "));
    }
}

export function createComponentElementPlaceholder(document: Document): HTMLElement {
    const container = document.createElement("div");
    container.classList.add("component-placeholder-container");

    // Line (the horizontal divider)
    const line = document.createElement("div");
    line.classList.add("component-placeholder-line");

    // Pill (the text badge)
    const pill = document.createElement("div");
    pill.classList.add("component-placeholder-pill");
    pill.textContent = "Drag it here";

    // Assemble
    container.appendChild(line);
    container.appendChild(pill);
    container.appendChild(line.cloneNode());

    return container;
}

/**
 * Creates an `.email-content` table element with provided inner content.
 * Allows customization of the `<td>` cell styles (e.g., padding for sections).
 *
 * @param cellInnerHtml Inner HTML string to place inside the `<td>` cell.
 * @returns A table element with `.email-content` structure.
 */
export function createTable<T extends Element>(cellInnerHtml?: string | Enumerable<T> | T[] | undefined): TableElements {
    const table = document.createElement("table");
    table.setAttribute("border", "0");
    table.setAttribute("cellpadding", "0");
    table.setAttribute("cellspacing", "0");
    table.setAttribute("width", "100%");
    table.setAttribute("role", "presentation");

    const tbody = document.createElement("tbody");
    const tr = document.createElement("tr");
    const td = document.createElement("td");

    if (typeof cellInnerHtml === "string") {
        td.innerHTML = cellInnerHtml;
    }
    else if (cellInnerHtml !== undefined) {
        cellInnerHtml.forEach(el => {
            td.appendChild(el);
        });
    }

    tr.appendChild(td);
    tbody.appendChild(tr);
    table.appendChild(tbody);

    return {
        table,
        tbody,
        tr,
        td
    };
}

/**
 * Creates a standard component structure with margin, border, and padding wrappers.
 * @param componentCssClass - The CSS class for the component.
 * @param componentVersion - The version of the component.
 * @param componentInnerHtml - Optional inner HTML for the component.
 * @returns
 */
export function createComponent(
    componentTypeName: Exclude<EditorComponentTypeName,
        | "one-column-section"
        | "two-column-section"
        | "three-column-section"
        | "four-column-section"
        | "right-sidebar-section"
        | "left-sidebar-section"
    >,
    componentVersion: string,
    componentInnerHtml?: string | Enumerable<Element> | undefined
): ComponentStructure {
    const wrappers = createElementWrappers(componentInnerHtml);

    const marginWrapper = wrappers.marginWrapper;
    marginWrapper.table.classList.add(`margin-wrapper-for-${componentTypeName}`);
    marginWrapper.table.classList.add("component", `component-${componentTypeName}`);
    marginWrapper.table.dataset.state = "component";
    setComponentVersionNumber(marginWrapper.table, componentVersion);

    const borderWrapper = marginWrapper.borderWrapper;
    borderWrapper.table.classList.add(`border-wrapper-for-${componentTypeName}`);

    const paddingWrapper = borderWrapper.paddingWrapper;
    paddingWrapper.table.classList.add(`padding-wrapper-for-${componentTypeName}`);

    return {
        marginWrapper: {
            ...marginWrapper,
            borderWrapper: {
                ...borderWrapper,
                paddingWrapper
            }
        }
    };
}
export function createElementWrappers(
    componentInnerHtml?: string | Enumerable<Element> | undefined
): ComponentStructure {
    const paddingWrapper = createTable(componentInnerHtml);
    paddingWrapper.table.classList.add("padding-wrapper");

    const borderWrapper = createTable([paddingWrapper.table]);
    borderWrapper.table.classList.add("border-wrapper");
    // Important! To ensure border-radius is applied to the border,
    // the table must have border-collapse: separate.
    borderWrapper.table.style.setProperty("border-collapse", "separate", "important");
    // Set overflow hidden so corner radius is applied to the border.
    borderWrapper.td.style.overflow = "hidden";

    const marginWrapper = createTable([borderWrapper.table]);
    marginWrapper.table.classList.add("margin-wrapper");

    return {
        marginWrapper: {
            ...marginWrapper,
            borderWrapper: {
                ...borderWrapper,
                paddingWrapper
            }
        }
    };
}

/**
 * Finds the first descendant set of table elements matching the selector or null if some are not found.
 */
function findTable(element: Element | Document, tableSelector: string): TableElements | null {
    const { table, tbody, tr, td } = findTablePartial(element, tableSelector);

    if (table && tbody && tr && td) {
        return {
            table,
            tbody,
            tr,
            td
        };
    }
    else {
        return null;
    }
}

/**
 * Finds the first descendant set of table elements matching the selector.
 *
 * May return undefined for table elements that aren't found.
 */
function findTablePartial(element: Element | Document, tableSelector: string): Partial<TableElements> {
    let table = (isElement(element) && element.matches(tableSelector))
        ? element
        : (element.querySelector(tableSelector) ?? undefined);

    if (table && !isHTMLTableElement(table)) {
        table = undefined;
    }

    const tbody = (table?.querySelector(":scope > tbody") ?? undefined) as HTMLTableSectionElement | undefined;
    const tr = (tbody?.querySelector(":scope > tr") ?? undefined) as HTMLTableRowElement | undefined;
    const td = (tr?.querySelector(":scope > td") ?? undefined) as HTMLTableCellElement | undefined;

    return {
        table,
        tbody,
        tr,
        td
    };
}

/**
 * Finds and returns the component inner wrappers or null if one or more are missing.
 */
export function findComponentInnerWrappers(componentElement: Element): ComponentStructure | null {
    const componentInnerWrappers = findComponentInnerWrappersPartial(componentElement);

    if (componentInnerWrappers.marginWrapper?.borderWrapper?.paddingWrapper) {
        return componentInnerWrappers as ComponentStructure;
    }
    else {
        // One or more wrappers were missing
        // so return null.
        return null;
    }
}

/**
 * Recursively makes all properties in T (including nested ones) optional.
 */
type DeepPartial<T> = {
    [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

/**
 * Finds and returns the component inner wrappers that can be found.
 */
export function findComponentInnerWrappersPartial(componentElement: Element): DeepPartial<ComponentStructure> {
    // Margin should be the outermost wrapper,
    // then border + background,
    // then padding.
    const marginWrapper = findTable(componentElement, "table.margin-wrapper");

    if (!marginWrapper) {
        return {};
    }

    const borderWrapper = findTable(marginWrapper.table, "table.border-wrapper");

    if (!borderWrapper) {
        return {
            marginWrapper: { ...marginWrapper }
        };
    }

    const paddingWrapper = findTable(borderWrapper.table, "table.padding-wrapper");

    if (!paddingWrapper) {
        return {
            marginWrapper: {
                ...marginWrapper,
                borderWrapper: {
                    ...borderWrapper
                }
            }
        };
    }

    return {
        marginWrapper: {
            ...marginWrapper,
            borderWrapper: {
                ...borderWrapper,
                paddingWrapper
            }
        }
    };
}

/**
 * Ensures the document body is wrapped in an `.email-wrapper` table.
 * Moves existing body contents into the wrapper if not present and also adds `.email-row` and `.email-row-content` tables.
 *
 * @param document The HTML document to normalize.
 */
export function ensureBodyWrapsEmailWrapper(document: Document): HTMLTableElement {
    const existingWrapper = document.querySelector(`table.${EmailWrapperCssClass}`) as HTMLTableElement;

    if (existingWrapper) {
        console.debug("Email wrapper table already exists. Skipping.");
        return existingWrapper;
    }

    // Create `.email-wrapper` table structure
    const wrapperTable = document.createElement("table");
    wrapperTable.classList.add(EmailWrapperCssClass);
    wrapperTable.setAttribute("border", "0");
    wrapperTable.setAttribute("cellpadding", "0");
    wrapperTable.setAttribute("cellspacing", "0");
    wrapperTable.setAttribute("width", "100%");
    wrapperTable.setAttribute("role", "presentation");
    wrapperTable.style.minWidth = "100%";
    wrapperTable.style.height = "100%"; // Forces full-height behavior

    const wrapperTbody = document.createElement("tbody");
    const wrapperRow = document.createElement("tr");
    const wrapperCell = document.createElement("td");
    wrapperCell.setAttribute("align", "center");
    wrapperCell.setAttribute("valign", "top"); // Prevent content from being squashed
    wrapperCell.style.height = "100%"; // Ensures row stretches

    // Create `.email-row` (full width row)

    // Build `.email-wrapper` structure
    Enumerable
        .from([...document.body.children])
        .forEach((el) => {
            wrapperCell.appendChild(el);
        });
    wrapperRow.appendChild(wrapperCell);
    wrapperTbody.appendChild(wrapperRow);
    wrapperTable.appendChild(wrapperTbody);

    // Append the table back to the body
    document.body.appendChild(wrapperTable);

    return wrapperTable;
}

export function createComponentElement(document: Document, componentTypeName: EditorComponentTypeName): HTMLElement {

    // Notes:
    //  - Inline styles defined here will be at the component level instead of the global level.
    //  - Ensure inline styles can be edited, if desired. Otherwise, styles added here will not be
    //    editable by the individual using the editor.
    //  - Rock CSS variables should not be used here as these will be added to the HTML email.
    //  - Global style defaults are maintained in the emailIFrame.partial.obs file.
    switch (componentTypeName) {
        case "title": {
            const { createComponentElement } = getTitleComponentHelper();
            return createComponentElement();
        }

        case "video": {
            const { createComponentElement } = getVideoComponentHelper();
            return createComponentElement();
        }

        case "button": {
            const buttonAdapter = createButtonComponentAdapter();
            return buttonAdapter.createComponentElement(document);
        }

        case "text": {
            const { createComponentElement } = getTextComponentHelper();
            return createComponentElement();
        }

        case "divider": {
            const adapter = createDividerComponentAdapter();
            return adapter.createComponentElement(document);
        }

        case "message": {
            const componentElements = createComponent(
                "message",
                "v2-alpha",
                "Message"
            );
            return componentElements.marginWrapper.table;
        }

        case "image": {
            const adapter = createImageComponentAdapter();
            return adapter.createComponentElement(document);
        }

        case "code": {
            const { createComponentElement } = getCodeComponentHelper();
            return createComponentElement();
        }

        case "rsvp": {
            const adapter = createRsvpComponentAdapter();
            return adapter.createComponentElement(document);
        }

        // Section Components
        case "section":
        case "one-column-section":
        case "two-column-section":
        case "three-column-section":
        case "four-column-section":
        case "right-sidebar-section":
        case "left-sidebar-section": {
            const { createComponentElement } = getSectionComponentHelper();
            return createComponentElement(componentTypeName);
        }

        case "row": {
            const { createComponentElement } = getRowComponentHelper();
            return createComponentElement();
        }
        default:
            throw new Error(`Unknown typeName: ${componentTypeName}`);
    }
}

export function getComponentHostSelector(componentTypeName: EditorComponentTypeName): string {
    switch (componentTypeName) {
        case "row": return ".structure-dropzone";
        case "section": return ".dropzone, .structure-dropzone";
        case "one-column-section": return ".dropzone, .structure-dropzone";
        case "two-column-section": return ".dropzone, .structure-dropzone";
        case "three-column-section": return ".dropzone, .structure-dropzone";
        case "four-column-section": return ".dropzone, .structure-dropzone";
        case "left-sidebar-section": return ".dropzone, .structure-dropzone";
        case "right-sidebar-section": return ".dropzone, .structure-dropzone";
        case "button": return ".dropzone";
        case "code": return ".dropzone";
        case "divider": return ".dropzone";
        case "image": return ".dropzone";
        case "message": return ".dropzone";
        case "rsvp": return ".dropzone";
        case "text": return ".dropzone";
        case "title": return ".dropzone";
        case "video": return ".dropzone";
    }
}

export function get<T>(value: T): T {
    return value;
}

export function createCssRuleset(selector: string, declarations: Record<string, string>): string | null {
    if (Object.keys(declarations).length) {
        const validDeclarations = Object.entries(declarations).filter(([_k, v]) => !!v).map(([k, v]) => `${k}: ${v};`);
        if (validDeclarations.length) {
            return `${selector} {\n    ${validDeclarations.join("\n    ")}\n}`;
        }
    }

    return null;
}

/**
 * Finds descendant content area elements within a given HTML element.
 * This function identifies relevant table structures while ignoring specific classes.
 *
 * The outerTable must be a direct child element of the `element` argument.
 *
 * @param {HTMLElement} element - The root element to search within.
 * @returns {Partial<ContentAreaElements>} An object containing references to found table elements.
 *
 * @example
 * const container = document.getElementById("content");
 * const elements = findDescendantContentAreaElements(container);
 * console.debug(elements.outerTable); // Logs the first outer table found
 */
export function findDescendantContentAreaElements(element: HTMLElement): Partial<ContentAreaElements> {
    const searchResult: Partial<ContentAreaElements> = {};
    function filterTableMatches(element: Element): boolean {
        const outerTableCssClassesToIgnore = ["button-outerwrap", "header", "spacer"] as const;

        return !outerTableCssClassesToIgnore.some(ignoreCssClass => element.classList.contains(ignoreCssClass))  // Exclude tables with classes
            && !outerTableCssClassesToIgnore.some(ignoreCssClass => element.closest(`table.${ignoreCssClass}`));    // Exclude tables nested within a table with classes
    }

    // First look for new, and legacy outer table elements that have the .container CSS class.
    searchResult.outerTable = Enumerable
        .from(element.querySelectorAll(":scope table.container"))
        .where(filterTableMatches)
        .select(el => el as HTMLTableElement)
        .firstOrDefault();

    // Look for any table that is a direct descendant of the root element.
    if (!searchResult.outerTable) {
        searchResult.outerTable = Enumerable
            .from(element.querySelectorAll(":scope > table"))
            .where(filterTableMatches)
            .select(el => el as HTMLTableElement)
            .firstOrDefault();
    }

    if (searchResult.outerTable) {
        searchResult.outerTableBody = searchResult.outerTable.querySelector(":scope > tbody") as HTMLElement ?? undefined;
        searchResult.outerTableTr = searchResult.outerTable.querySelector(":scope > tbody > tr, :scope > tr") as HTMLElement ?? undefined;

        if (searchResult.outerTableTr) {
            searchResult.outerTableTd = searchResult.outerTableTr.querySelector("td, th") as HTMLElement ?? undefined;

            if (searchResult.outerTableTd) {
                const innerTableCssClassesToIgnore = ["header", "spacer"] as const;
                searchResult.innerTable = [...searchResult.outerTableTd.querySelectorAll("table")]
                    .filter(table =>
                        !innerTableCssClassesToIgnore.some(ignoreCssClass => table.classList.contains(ignoreCssClass)) && // Exclude tables with classes
                        !innerTableCssClassesToIgnore.some(ignoreCssClass => table.closest(`table.${ignoreCssClass}`))    // Exclude tables nested within a table with classes
                    )[0] as HTMLElement // Get the first result
                    ?? undefined;

                if (searchResult.innerTable) {
                    searchResult.innerTableBody = searchResult.innerTable.querySelector(":scope > tbody") as HTMLElement ?? undefined;
                    searchResult.innerTableTr = searchResult.innerTable.querySelector(":scope > tbody > tr, :scope > tr") as HTMLElement ?? undefined;

                    if (searchResult.innerTableTr) {
                        searchResult.innerTableTd = searchResult.innerTableTr.querySelector("td, th") as HTMLElement ?? undefined;
                    }
                }
            }
        }
    }

    return searchResult;
}

export function addContentAreaElementsIfMissing(
    element: HTMLElement,
    {
        outerTableCssClass,
        innerTableCssClass
    }: {
        outerTableCssClass?: string,
        innerTableCssClass?: string
    } = {}
): ContentAreaElements {
    const tableElements = findDescendantContentAreaElements(element);

    if (!tableElements.outerTable) {
        // Create all elements since the outermost table is missing.
        // The outer table's primary purposes are:
        // - structural container for child elements
        // - provides alignment, padding, background colors
        // - better supported by email clients than using "margin" or other CSS functionality
        tableElements.outerTable = element.ownerDocument.createElement("table");
        tableElements.outerTable.setAttribute("role", "presentation");
        tableElements.outerTable.setAttribute("width", "100%");
        tableElements.outerTable.setAttribute("cellpadding", "0");
        tableElements.outerTable.setAttribute("cellspacing", "0");
        tableElements.outerTable.setAttribute("role", "presentation");
        tableElements.outerTable.style.width = "100%";
        tableElements.outerTable.style.tableLayout = "fixed";
        tableElements.outerTable.style.borderSpacing = "0";
        element.append(tableElements.outerTable);
    }

    if (outerTableCssClass) {
        tableElements.outerTable.classList.add(outerTableCssClass);
    }

    // Store initial children before potentially adding outerTable
    const initialChildren = [...element.children].filter(child => child !== tableElements.outerTable && !child.classList.contains(RockStylesCssClass));

    if (!tableElements.outerTableBody) {
        tableElements.outerTableBody = element.ownerDocument.createElement("tbody");
        tableElements.outerTable.append(tableElements.outerTableBody);
    }

    if (!tableElements.outerTableTr) {
        tableElements.outerTableTr = element.ownerDocument.createElement("tr");
        tableElements.outerTableBody.append(tableElements.outerTableTr);
    }

    if (!tableElements.outerTableTd) {
        tableElements.outerTableTd = element.ownerDocument.createElement("td");
        tableElements.outerTableTr.append(tableElements.outerTableTd);
    }

    if (!tableElements.innerTable) {
        // Inner table.
        tableElements.innerTable = element.ownerDocument.createElement("table");
        tableElements.innerTable.setAttribute("role", "presentation");
        tableElements.innerTable.setAttribute("width", "100%");
        tableElements.innerTable.setAttribute("cellpadding", "0");
        tableElements.innerTable.setAttribute("cellspacing", "0");
        tableElements.innerTable.setAttribute("role", "presentation");
        tableElements.innerTable.style.width = "100%";
        tableElements.innerTable.style.borderSpacing = "0";
        tableElements.innerTable.style.tableLayout = "fixed";
        tableElements.outerTableTd.append(tableElements.innerTable);
    }

    if (innerTableCssClass) {
        tableElements.innerTable.classList.add(innerTableCssClass);
    }

    if (!tableElements.innerTableBody) {
        tableElements.innerTableBody = element.ownerDocument.createElement("tbody");
        tableElements.innerTable.append(tableElements.innerTableBody);
    }

    if (!tableElements.innerTableTr) {
        tableElements.innerTableTr = element.ownerDocument.createElement("tr");
        tableElements.innerTableBody.append(tableElements.innerTableTr);
    }

    if (!tableElements.innerTableTd) {
        tableElements.innerTableTd = element.ownerDocument.createElement("td");
        tableElements.innerTableTr.append(tableElements.innerTableTd);

        // Move all the element's children into the inner td element.
        tableElements.innerTableTd.append(...initialChildren);
    }

    return {
        outerTable: tableElements.outerTable,
        outerTableBody: tableElements.outerTableBody,
        outerTableTr: tableElements.outerTableTr,
        outerTableTd: tableElements.outerTableTd,
        innerTable: tableElements.innerTable,
        innerTableBody: tableElements.innerTableBody,
        innerTableTr: tableElements.innerTableTr,
        innerTableTd: tableElements.innerTableTd,
    };
}

/**
 * Determines if the specified shorthand property is set in the style source.
 *
 * @param source The style source to check.
 * @param shorthandProperty The shorthand property to check. (e.g. `margin`)
 * @param longhandProperties The longhand properties that make up the shorthand property. (e.g. `margin-top`, `margin-bottom`, `margin-right`, `margin-left`)
 */
export function isShorthandPropertySet(
    source: HTMLElement | Partial<Record<CssStyleDeclarationKebabKey, string>>,
    shorthandProperty: CssStyleDeclarationKebabKey,
    longhandProperties: CssStyleDeclarationKebabKey[]
): boolean {
    const isHTMLElementSource = isHTMLElement(source);

    const shorthandValue = isHTMLElementSource
        ? source.style.getPropertyValue(shorthandProperty)
        : (source[shorthandProperty] ?? "");

    // Check if shorthand is explicitly mentioned in cssText.
    const isShorthandValuePresent = isHTMLElementSource
        ? source.style.cssText.includes(shorthandProperty)
        : !isNullish(source[shorthandProperty]);

    // If shorthand is explicitly present in cssText (including "")...
    if (isShorthandValuePresent) {
        // ...verify that longhand properties match or are cleared.
        const areLonghandValuesCleared = longhandProperties.every(property => {
            return isHTMLElementSource
                ? source.style.getPropertyValue(property) === ""
                : !source[property];
        });

        if (shorthandValue === "" && areLonghandValuesCleared) {
            // Shorthand is explicitly set to "" and longhand properties are cleared.
            return true;
        }

        // Otherwise, is shorthand set to a valid value.
        return shorthandValue !== "";
    }
    else {
        return false;
    }
}

/**
 * Moves the specified inline styles from the source to the target element.
 *
 * @param source The source from which to move the inline styles.
 * @param targetElement The target element to which to move the inline styles.
 * @param shorthandProperty The shorthand property to move. (e.g. `margin`)
 * @param longhandProperties The longhand properties that make up the shorthand property. (e.g. `margin-top`, `margin-bottom`, `margin-right`, `margin-left`)
 */
export function moveShorthandInlineStyles(source: HTMLElement, targetElement: HTMLElement, shorthandProperty: CssStyleDeclarationKebabKey, longhandProperties: CssStyleDeclarationKebabKey[]): void {
    const isSourceShorthandPropertySet = isShorthandPropertySet(source, shorthandProperty, longhandProperties);

    // If the shorthand is explicitly set, transfer or remove it.
    if (isSourceShorthandPropertySet) {
        // Only copy the shorthand value if the target doesn't have its own value.
        const isTargetShorthandPropertySet = isShorthandPropertySet(targetElement, shorthandProperty, longhandProperties);

        if (!isTargetShorthandPropertySet) {
            const sourceShorthandPropertyValue = source.style.getPropertyValue(shorthandProperty);
            targetElement.style.setProperty(shorthandProperty, sourceShorthandPropertyValue);
        }

        // Remove the shorthand value from the source element.
        source.style.removeProperty(shorthandProperty);

        // No need to check longhand properties if the shorthand is set.
        return;
    }


    // Handle individual longhand properties.
    longhandProperties.forEach(longhandProperty => {
        if (!source.style.cssText.includes(longhandProperty)) {
            // Skip longhand properties that are not explicitly set in the source element.
            return;
        }

        // Move the inline styles to the target element if the property is not already set.
        // Using cssText.includes is safe in this case because longhand names are unique and cannot overlap with shorthand.
        const isTargetLonghandPropertySet = targetElement.style.cssText.includes(longhandProperty);

        if (!isTargetLonghandPropertySet) {
            const sourceLonghandPropertyValue = source.style.getPropertyValue(longhandProperty);
            const sourceLonghandPropertyPriority = source.style.getPropertyPriority(longhandProperty);

            targetElement.style.setProperty(longhandProperty, sourceLonghandPropertyValue, sourceLonghandPropertyPriority);
        }

        // Remove the inline style from the source element.
        source.style.removeProperty(longhandProperty);
    });
}

/**
 * Moves the specified inline styles from the source element to the target element.
 *
 * @param sourceElement The source element from which to move the inline styles.
 * @param targetElement The target element to which to move the inline styles.
 * @param standaloneProperties The inline styles to move. (e.g. `display`, `color`, `opacity`, `z-index`)
 */
export function moveStandaloneInlineStyles(
    sourceElement: HTMLElement,
    targetElement: HTMLElement,
    standaloneProperties: CssStyleDeclarationKebabKey[]
): void {
    standaloneProperties.forEach(property => {
        if (!sourceElement.style.cssText.includes(property)) {
            // Skip if the property is not explicitly set in the source element.
            return;
        }

        if (!targetElement.style.cssText.includes(property)) {
            const sourcePropertyValue = sourceElement.style.getPropertyValue(property);
            const sourcePropertyPriority = sourceElement.style.getPropertyPriority(property);

            // Move the property to the target element.
            targetElement.style.setProperty(property, sourcePropertyValue, sourcePropertyPriority);
        }

        // Remove the property from the source element.
        sourceElement.style.removeProperty(property);
    });
}

/**
 * Copies the specified shorthand inline styles from the source to the target element.
 *
 * @param source The source from which to copy the inline styles.
 * @param targetElement The target element to which to copy the inline styles.
 * @param shorthandProperty The shorthand property to copy. (e.g. `margin`)
 * @param longhandProperties The longhand properties that make up the shorthand property. (e.g. `margin-top`, `margin-bottom`, `margin-right`, `margin-left`)
 */
export function copyShorthandInlineStyles(
    source: HTMLElement | Partial<Record<CssStyleDeclarationKebabKey, string>>,
    targetElement: HTMLElement,
    shorthandProperty: CssStyleDeclarationKebabKey,
    longhandProperties: CssStyleDeclarationKebabKey[]
): void {
    const isHTMLElementSource = isHTMLElement(source);

    const isSourceShorthandPropertySet = isShorthandPropertySet(
        source,
        shorthandProperty,
        longhandProperties
    );

    // If the shorthand is explicitly set, copy it to the target.
    if (isSourceShorthandPropertySet) {
        const sourceShorthandPropertyValue = isHTMLElementSource
            ? source.style.getPropertyValue(shorthandProperty)
            : (source[shorthandProperty] ?? "");

        targetElement.style.setProperty(shorthandProperty, sourceShorthandPropertyValue);

        // No need to check longhand properties if the shorthand is set.
        return;
    }

    // Handle individual longhand properties.
    longhandProperties.forEach(longhandProperty => {
        const isSourceLonghandPropertySet = isHTMLElementSource
            ? source.style.cssText.includes(longhandProperty)
            : !isNullish(source[longhandProperty]);

        if (!isSourceLonghandPropertySet) {
            // Skip longhand properties that are not explicitly set in the source element.
            return;
        }

        const sourceLonghandPropertyValue = isHTMLElementSource
            ? source.style.getPropertyValue(longhandProperty)
            : source[longhandProperty] ?? "";
        const sourceLonghandPropertyPriority = isHTMLElementSource
            ? source.style.getPropertyPriority(longhandProperty)
            : "";

        targetElement.style.setProperty(
            longhandProperty,
            sourceLonghandPropertyValue,
            sourceLonghandPropertyPriority
        );
    });
}

/**
 * Finds the first CSS style sheet where the <style class> matches the `styleCssClass`
 * and the ruleset selector matches the `cssRulesetSelectors`.
 */
export function findElements(element: Element, styleCssClass: string, rulesetCssSelector: string): StyleSheetElements | undefined {
    const elementDocument = element.ownerDocument;
    const elementWindow = elementDocument.defaultView;

    if (elementWindow) {
        const eligibleStyleSheets: Omit<StyleSheetElements, "ruleset">[] = [];

        for (const styleSheet of elementDocument.styleSheets) {
            const styleSheetParentElement = styleSheet.ownerNode;

            if (styleSheetParentElement instanceof elementWindow.HTMLStyleElement
                && styleSheetParentElement.classList.contains(styleCssClass)
                && (element === styleSheetParentElement || element.contains(styleSheetParentElement))
            ) {
                const currentEligibleElements: Omit<StyleSheetElements, "ruleset"> = {
                    elementWindow,
                    elementDocument,
                    styleElement: styleSheetParentElement,
                    styleSheet
                };

                eligibleStyleSheets.push(currentEligibleElements);

                for (const ruleset of styleSheet.cssRules) {
                    if (ruleset instanceof elementWindow.CSSStyleRule) {
                        if (ruleset.selectorText === rulesetCssSelector) {
                            // An exact match was found: <style> element and CSS ruleset selector.
                            return {
                                ...currentEligibleElements,
                                ruleset
                            };
                        }
                    }
                }
            }
        }

        if (eligibleStyleSheets.length) {
            // A <style> was found but a CSS ruleset was not found.
            return eligibleStyleSheets[0];
        }
    }
}

export function createElements(element: Element, styleCssClass: string): StyleSheetElements | undefined {
    const elementDocument = element.ownerDocument;
    const elementWindow = elementDocument.defaultView;

    if (elementWindow) {
        // Add the stylesheet since it's missing.
        const styleElement = elementDocument.createElement("style") as HTMLStyleElement;
        styleElement.classList.add(styleCssClass);

        // Add it as the first element.
        element.insertBefore(styleElement, element.firstChild);

        return {
            elementDocument,
            elementWindow,
            styleElement,
            styleSheet: styleElement.sheet!
        };
    }
}

export function addRuleset(elements: StyleSheetElements, rulesetCssSelector: string): CSSStyleRule {
    if (elements.ruleset) {
        // Skip if the ruleset is already created.
        return elements.ruleset;
    }

    const { styleSheet } = elements;
    const rulesetIndex = styleSheet.insertRule(`${rulesetCssSelector} {}`, styleSheet.cssRules.length);
    const ruleset = styleSheet.cssRules[rulesetIndex] as CSSStyleRule;

    elements.ruleset = ruleset;

    return ruleset;
}

export function updateStyleElementTextContent(elements: StyleSheetElements): void {
    const { styleElement, styleSheet } = elements;

    // The previous ruleset change only affects the style in memory.
    // Update the HTML <style> element contents.
    styleElement.textContent = Enumerable.from(styleSheet.cssRules)
        .select(rule => rule.cssText)
        .aggregate((rules, rule, i) => i !== 0 ? `${rules}\n${rule}` : rule, "");
}

export function createDomWatcher(
    root: Document | Element,
    selector: string,
    { includeSelf, additionalFilter, additionalProjection }: { includeSelf?: string | boolean; additionalFilter?: (el: Element) => boolean; additionalProjection?: (el: Element) => Element; } = {}
): DomWatcher {
    const foundElements = new Set<Element>();
    let onFoundCallbacks: ((element: Element) => void)[] = [];
    let onRemovedCallbacks: ((element: Element) => void)[] = [];

    function updateMatches(): void {
        let matches = Enumerable.from(root.querySelectorAll(selector));
        if (additionalFilter) {
            matches = matches.where(additionalFilter);
        }

        if (additionalProjection) {
            matches = matches.select(additionalProjection);
        }

        const newMatches = new Set(matches);

        // Handle newly found elements
        newMatches.forEach((el) => {
            if (!foundElements.has(el)) {
                foundElements.add(el);
                onFoundCallbacks.forEach((cb) => cb(el));
            }
        });

        // Handle removed elements
        foundElements.forEach((el) => {
            if (!newMatches.has(el)) {
                foundElements.delete(el);
                onRemovedCallbacks.forEach((cb) => cb(el));
            }
        });
    }

    const observer = new MutationObserver((mutations) => {
        let shouldRevalidate = false;

        mutations.forEach((mutation) => {
            if (mutation.type === "childList") {
                shouldRevalidate = true; // Always revalidate for added/removed nodes
            }

            if (mutation.type === "attributes") {
                const attrName = mutation.attributeName || "";
                if (attrName === "class" || attrName.startsWith("data-")) {
                    shouldRevalidate = true; // Only revalidate for relevant attributes
                }
            }
        });

        if (shouldRevalidate) {
            updateMatches();
        }
    });

    observer.observe(root, {
        childList: true,
        subtree: true,
        attributes: true
    });

    if (includeSelf) {
        if (isElement(root) && root.matches(typeof includeSelf === "boolean" ? selector : includeSelf)) {
            foundElements.add(root);
        }
    }

    // Initial match check
    updateMatches();

    return {
        get foundElements() {
            return Enumerable.from(foundElements);
        },

        onElementFound(callback: (element: Element) => void): void {
            onFoundCallbacks.push(callback);
            foundElements.forEach(callback);
        },

        onElementRemoved(callback: (element: Element) => void): void {
            onRemovedCallbacks.push(callback);
        },

        dispose(): void {
            observer.disconnect();
            foundElements.clear();
            onFoundCallbacks = [];
            onRemovedCallbacks = [];
        }
    };
}

/**
 * Removes temporary wrapper elements from an element's or document's children.
 *
 * This will place the wrapped elements in place of their associated wrappers,
 * and the wrappers will be removed.
 */
export function removeTemporaryWrappers(element: Document | Element): void {
    element.querySelectorAll(`.${RockRuntimeWrapperElementCssClass}`)
        .forEach((wrapper) => {
            if (wrapper.parentNode) {
                // Move all child nodes of the wrapper to the wrapper's parent.
                while (wrapper.firstChild) {
                    wrapper.parentNode.insertBefore(wrapper.firstChild, wrapper);
                }
            }

            // Remove the now-empty wrapper.
            wrapper.remove();
        });
}

/**
 * Removes temporary elements from a document or element.
 */
export function removeTemporaryElements(element: Document | Element): void {
    element.querySelectorAll(`.${RockRuntimeElementCssClass}`)
        .forEach(el => el.remove());
}

/**
 * Removes temporary CSS classes from the children of a document or element children.
 */
export function removeTemporaryClasses(element: Document | Element): void {
    const selector = `[class*="${RockRuntimeClassCssClassPrefix}"]`;

    element.querySelectorAll(selector)
        .forEach(el => {
            const runtimeCssClasses = Enumerable
                .from(el.classList)
                .where(cssClass => cssClass.startsWith(RockRuntimeClassCssClassPrefix));

            el.classList.remove(...runtimeCssClasses);
        });
}

/**
 * Removes temporary attributes from a element's or document's children.
 */
export function removeTemporaryAttributes(element: Document | Element): void {
    const attributesToRemove: string[] = ["draggable", "contenteditable"];

    element.querySelectorAll(`[${attributesToRemove.join("], [")}]`)
        // Strip the Rock runtime CSS classes from the elements.
        .forEach(el => {
            attributesToRemove.forEach(attr => {
                el.removeAttribute(attr);
            });
        });
}

export function checkDropzoneSize(rect: DOMRectReadOnly, element: Element): void {
    if (rect.width < EmptyDropzoneSvgPixelWidth) {
        element.classList.add(SmallEmptyClass);
    }
    else {
        element.classList.remove(SmallEmptyClass);
    }
}

export function getComponentVersionNumber(componentElement: Element): string | null | undefined {
    if (isHTMLElement(componentElement)) {
        const version = componentElement.dataset.version ?? "";

        if (isComponentVersionNumber(version)) {
            return version;
        }
        else {
            return null;
        }
    }
}

export function setComponentVersionNumber(componentElement: Element, version: string): void {
    if (!isComponentVersionNumber(version)) {
        console.warn(`Invalid version number: ${version}`);
        return;
    }

    if (isHTMLElement(componentElement)) {
        componentElement.dataset.version = version;
    }
}

export function isComponentVersionNumber(str: string): boolean {
    const componentVersionRegex = /^v\d+(\.\d+)?(-[a-zA-Z]+(\.\d+)?)?$/;
    return componentVersionRegex.test(str);
}

/**
 * Compares two component version strings using a semantic like versioning system.
 *
 * Version format: `v<major>[.<minor>][.<patch>][-<phase>[.<phaseVersion>]]`
 * - `v2` (Major only, treated as `v2.0.0`)
 * - `v2.0` (Treated as `v2.0.0`)
 * - `v2.1.3` (Patch included)
 * - `v2.0-foo` (Pre release: phase "foo", no phase version)
 * - `v2.0-foo.1` (Pre release: phase "foo", version 1)
 *
 * Comparison rules:
 * 1) Major, then minor, then patch (higher means newer)
 * 2) If major minor patch match, a version with no phase is newer than a version with a phase (phase indicates pre-release and stable beats pre-release)
 * 3) If both have a phase, phase is compared alphabetically (case insensitive)
 * 4) If phases match, phaseVersion compares numerically. Missing phaseVersion is treated as 0
 */
export function compareComponentVersions(v1: string, v2: string): number {
    type Parsed = {
        major: number
        minor: number
        patch: number
        phase: string
        phaseVersion: number
    };

    function parseVersion(version: string): Parsed {
        // v<major>[.<minor>][.<patch>][-<phase>[.<phaseVersion>]]
        const match = version.match(
            /^v(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-([a-zA-Z]+)(?:\.(\d+))?)?$/
        );

        return {
            major: match ? parseInt(match[1], 10) : 0,
            minor: match && match[2] ? parseInt(match[2], 10) : 0,
            patch: match && match[3] ? parseInt(match[3], 10) : 0,
            phase: match && match[4] ? match[4] : "",
            phaseVersion: match && match[5] ? parseInt(match[5], 10) : 0,
        };
    }

    const a = parseVersion(v1);
    const b = parseVersion(v2);

    if (a.major !== b.major) {
        return a.major < b.major ? -1 : 1;
    }
    if (a.minor !== b.minor) {
        return a.minor < b.minor ? -1 : 1;
    }
    if (a.patch !== b.patch) {
        return a.patch < b.patch ? -1 : 1;
    }

    const aStable = a.phase === "";
    const bStable = b.phase === "";
    if (aStable && !bStable) {
        return 1;
    }
    if (!aStable && bStable) {
        return -1;
    }
    if (aStable && bStable) {
        return 0;
    }

    // Both have a phase: alphabetical sort, case insensitive for stability
    const phaseComparison = a.phase.toLowerCase().localeCompare(b.phase.toLowerCase());
    if (phaseComparison !== 0) {
        return phaseComparison < 0 ? -1 : 1;
    }

    if (a.phaseVersion !== b.phaseVersion) {
        return a.phaseVersion < b.phaseVersion ? -1 : 1;
    }

    return 0;
}


// #endregion Functions

// #region Components

export function getVideoComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): ComponentStructure | null;
    createComponentElement(): HTMLElement;
} {
    const latestVersion = "v17.3-alpha" as const;

    return {
        getElements(componentElement: Element): ComponentStructure | null {
            if (!componentElement.classList.contains("component-video")) {
                throw new Error(`Element is not a video component element: ${componentElement.outerHTML}`);
            }

            return findComponentInnerWrappers(componentElement);
        },

        createComponentElement(): HTMLElement {
            const componentElements = createComponent(
                "video",
                latestVersion,
                `<a href=""><img src="/Assets/Images/video-placeholder.jpg" data-imgcsswidth="full" style="width: 100%;"></a>`
            );
            // Image component needs a line-height of 0 to remove extra space under image.
            componentElements.marginWrapper.borderWrapper.paddingWrapper.td.style.lineHeight = "0";
            return componentElements.marginWrapper.table;
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-video")) {
                throw new Error(`Element is not a video component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            return !versionNumber || compareComponentVersions(versionNumber, latestVersion) < 0;
        },

        migrate(oldComponentElement: Element): Element {
            if (!oldComponentElement.classList.contains("component-video")) {
                throw new Error(`Element is not a video component element: ${oldComponentElement.outerHTML}`);
            }

            if (!this.isMigrationRequired(oldComponentElement)) {
                return oldComponentElement;
            }

            const migrations = [
                function v0ToV2Alpha(componentElement: Element): Element {
                    const anchor = componentElement.querySelector("a");
                    const img = anchor?.querySelector("img");

                    if (!anchor || !img) {
                        throw new Error("Invalid video component structure: missing <a> or <img>.");
                    }

                    const href = anchor.getAttribute("href") ?? "";
                    const src = img.getAttribute("src") ?? "";
                    const style = img.getAttribute("style") ?? "";
                    const dataImgCssWidth = img.getAttribute("data-imgcsswidth") ?? "";

                    // Prepare the new component
                    const newComponent = document.createElement("table");
                    newComponent.setAttribute("border", "0");
                    newComponent.setAttribute("cellpadding", "0");
                    newComponent.setAttribute("cellspacing", "0");
                    newComponent.setAttribute("width", "100%");
                    newComponent.setAttribute("role", "presentation");
                    newComponent.className = "margin-wrapper margin-wrapper-for-video component component-video";
                    newComponent.setAttribute("data-state", "component");

                    setComponentVersionNumber(newComponent, "v2-alpha");

                    // Preserve any known metadata (no parsing, just forwarding)
                    const filename = img.getAttribute("data-image-filename");
                    const guid = img.getAttribute("data-image-guid");
                    const sourceUrl = componentElement.getAttribute("data-image-source-video-url");

                    if (filename) newComponent.setAttribute("data-image-filename", filename);
                    if (guid) newComponent.setAttribute("data-image-guid", guid);
                    if (sourceUrl) newComponent.setAttribute("data-image-source-video-url", sourceUrl);

                    // Create the v2-alpha structure
                    newComponent.innerHTML =
                        `<tbody>
    <tr>
        <td>
            <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="border-wrapper border-wrapper-for-video" style="border-collapse: separate !important;">
                <tbody>
                    <tr>
                        <td style="overflow: hidden;">
                            <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper padding-wrapper-for-video">
                                <tbody>
                                    <tr>
                                        <td style="line-height: 0;">
                                            <a href="${href}">
                                                <img src="${src}" ${dataImgCssWidth ? `data-imgcsswidth="${dataImgCssWidth}"` : ""} style="${style}">
                                            </a>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </td>
                    </tr>
                </tbody>
            </table>
        </td>
    </tr>
</tbody>`;

                    return newComponent;
                },

                // eslint-disable-next-line @typescript-eslint/naming-convention
                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const versionNumber = getComponentVersionNumber(componentElement);
                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, "v17.3-alpha") >= 0) {
                        return componentElement; // Already migrated
                    }

                    // Bump version.
                    setComponentVersionNumber(componentElement, "v17.3-alpha");

                    return componentElement;
                }
            ];


            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };
}

type TitleComponentStructure = ComponentStructure & {
    readonly headingEl: HTMLHeadElement | null;
    readonly borderEl: HTMLElement;
    readonly marginEl: HTMLElement;
    readonly paddingEl: HTMLElement;
};

export type SupportedHeadingLevel = "h1" | "h2" | "h3";
export const SupportedHeadingLevels: SupportedHeadingLevel[] = ["h1", "h2", "h3"];

export function getTitleComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): TitleComponentStructure | null;
    createComponentElement(): HTMLElement;
    setCssClasses(componentElements: TitleComponentStructure): void;
} {
    const latestVersion = "v17.3-alpha" as const;


    const helper = {
        createComponentElement(): HTMLElement {
            const component = createComponent(
                "title",
                latestVersion,
                `<h1 class="${RockCssClassContentEditable}" style="margin: 0;">Title</h1>`
            );

            const componentElements = helper.getElements(component.marginWrapper.table);

            if (!componentElements) {
                throw new Error("Failed to create title component element. Structure is invalid.");
            }

            // Set CSS classes to enable global and component-specific styles.
            helper.setCssClasses(componentElements);

            return componentElements.marginWrapper.table;
        },

        getElements(componentElement: Element): TitleComponentStructure | null {
            if (!componentElement.classList.contains("component-title")) {
                throw new Error(`Element is not a title component element: ${componentElement.outerHTML}`);
            }

            const wrappers = findComponentInnerWrappers(componentElement);

            if (!wrappers) {
                return null;
            }

            return {
                ...wrappers,

                get headingEl(): HTMLHeadingElement | null {
                    return (wrappers.marginWrapper.borderWrapper.paddingWrapper.td.querySelector(SupportedHeadingLevels.join(", ")) ?? null) as HTMLHeadingElement | null;
                },

                get marginEl(): HTMLElement {
                    return wrappers.marginWrapper.td;
                },

                get borderEl(): HTMLElement {
                    return wrappers.marginWrapper.borderWrapper.td;
                },

                get paddingEl(): HTMLElement {
                    return wrappers.marginWrapper.borderWrapper.paddingWrapper.td;
                }
            };
        },

        setCssClasses(componentElements: TitleComponentStructure): void {
            // CSS classes are set based on the heading level.
            const headingLevel = (componentElements.headingEl?.tagName.toLowerCase() ?? "h1") as SupportedHeadingLevel;

            // Wrapper classes.

            const marginWrapperTable = componentElements.marginWrapper.table;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getMarginWrapperTableCssClass("title", `-${level}`))
                .forEach(cssClass => marginWrapperTable.classList.remove(cssClass));
            marginWrapperTable.classList.add(getMarginWrapperTableCssClass("title", `-${headingLevel}`));

            const borderWrapperTable = componentElements.marginWrapper.borderWrapper.table;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getBorderWrapperTableCssClass("title", `-${level}`))
                .forEach(cssClass => borderWrapperTable.classList.remove(cssClass));
            borderWrapperTable.classList.add(getBorderWrapperTableCssClass("title", `-${headingLevel}`));

            const paddingWrapperTable = componentElements.marginWrapper.borderWrapper.paddingWrapper.table;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getPaddingWrapperTableCssClass("title", `-${level}`))
                .forEach(cssClass => paddingWrapperTable.classList.remove(cssClass));
            paddingWrapperTable.classList.add(getPaddingWrapperTableCssClass("title", `-${headingLevel}`));

            // Style classes.

            function getMarginClass(level: SupportedHeadingLevel): string {
                switch (level) {
                    case "h1": return GlobalCssClasses.marginHeading1;
                    case "h2": return GlobalCssClasses.marginHeading2;
                    case "h3": return GlobalCssClasses.marginHeading3;
                    default: throw new Error(`Unsupported heading level: ${level}`);
                }
            }

            function getBorderClass(level: SupportedHeadingLevel): string {
                switch (level) {
                    case "h1": return GlobalCssClasses.borderHeading1;
                    case "h2": return GlobalCssClasses.borderHeading2;
                    case "h3": return GlobalCssClasses.borderHeading3;
                    default: throw new Error(`Unsupported heading level: ${level}`);
                }
            }

            function getPaddingClass(level: SupportedHeadingLevel): string {
                switch (level) {
                    case "h1": return GlobalCssClasses.paddingHeading1;
                    case "h2": return GlobalCssClasses.paddingHeading2;
                    case "h3": return GlobalCssClasses.paddingHeading3;
                    default: throw new Error(`Unsupported heading level: ${level}`);
                }
            }

            function getFontClass(level: SupportedHeadingLevel): string {
                switch (level) {
                    case "h1": return GlobalCssClasses.fontHeading1;
                    case "h2": return GlobalCssClasses.fontHeading2;
                    case "h3": return GlobalCssClasses.fontHeading3;
                    default: throw new Error(`Unsupported heading level: ${level}`);
                }
            }

            const marginWrapperTd = componentElements.marginWrapper.td;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getMarginClass(level))
                .forEach(cssClass => marginWrapperTd.classList.remove(cssClass));
            marginWrapperTd.classList.add(getMarginClass(headingLevel));

            const borderWrapperTd = componentElements.marginWrapper.borderWrapper.td;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getBorderClass(level))
                .forEach(cssClass => borderWrapperTd.classList.remove(cssClass));
            borderWrapperTd.classList.add(getBorderClass(headingLevel));

            const paddingWrapperTd = componentElements.marginWrapper.borderWrapper.paddingWrapper.td;
            Enumerable
                .from(SupportedHeadingLevels)
                .select(level => getPaddingClass(level))
                .forEach(cssClass => paddingWrapperTd.classList.remove(cssClass));
            paddingWrapperTd.classList.add(getPaddingClass(headingLevel));

            const headingEl = componentElements.headingEl;
            if (headingEl) {
                Enumerable
                    .from(SupportedHeadingLevels)
                    .select(level => getFontClass(level))
                    .forEach(cssClass => headingEl.classList.remove(cssClass));

                // Also add the global font class so the global font can affect the heading.
                headingEl.classList.add(GlobalCssClasses.fontGlobal, getFontClass(headingLevel));
            }
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-title")) {
                throw new Error(`Element is not a title component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            if (!versionNumber) {
                return true;
            }

            const comparison = compareComponentVersions(versionNumber, latestVersion);

            if (comparison < 0) {
                return true;
            }

            return false;
        },

        migrate(oldComponentElement: Element): Element {
            if (!oldComponentElement.classList.contains("component-title")) {
                throw new Error(`Element is not a title component element: ${oldComponentElement.outerHTML}`);
            }

            if (!helper.isMigrationRequired(oldComponentElement)) {
                return oldComponentElement;
            }

            // Migrations should be in chronological order
            // and should not use helper methods or variables that
            // can change.
            const migrations = [
                function v0ToV2Alpha(componentElement: Element): Element {
                    if (getComponentVersionNumber(componentElement)) {
                        return componentElement; // Already migrated
                    }

                    const heading = (componentElement.querySelector("h1, h2, h3, h4, h5, h6") ?? null) as HTMLHeadingElement | null;
                    if (!heading) {
                        throw new Error("Heading tag not found in title component.");
                    }

                    const headingTag = heading.tagName.toLowerCase();
                    const headingText = heading.textContent || "";
                    const style = heading.style;

                    // Extract and remove margin/padding
                    const simulatedMargin = style.margin || "";
                    const contentPadding = style.padding || "";

                    // Remove margin and padding from style to preserve the rest
                    const cleanStyle: string[] = [];
                    for (const prop of heading.style) {
                        if (prop !== "margin" && prop !== "padding") {
                            cleanStyle.push(`${prop}: ${style.getPropertyValue(prop)};`);
                        }
                    }
                    // Always override margin to 0 on heading
                    cleanStyle.push("margin: 0px;");

                    const version = "v2-alpha";

                    const wrapper = document.createElement("table");
                    wrapper.setAttribute("border", "0");
                    wrapper.setAttribute("cellpadding", "0");
                    wrapper.setAttribute("cellspacing", "0");
                    wrapper.setAttribute("width", "100%");
                    wrapper.setAttribute("role", "presentation");
                    wrapper.className = `margin-wrapper margin-wrapper-for-title component component-title margin-wrapper-for-title-${headingTag}`;
                    wrapper.setAttribute("data-state", "component");
                    setComponentVersionNumber(wrapper, version); // Ensure version tracking

                    const wrapperPaddingAttr = simulatedMargin ? ` style="padding: ${simulatedMargin};"` : "";
                    const innerTdPaddingAttr = contentPadding ? ` style="padding: ${contentPadding};"` : "";

                    wrapper.innerHTML = `
                        <tbody><tr><td${wrapperPaddingAttr}>
                            <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation"
                                class="border-wrapper border-wrapper-for-title border-wrapper-for-title-${headingTag}"
                                style="border-collapse: separate !important;">
                                <tbody><tr><td style="overflow: hidden;">
                                    <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation"
                                        class="padding-wrapper padding-wrapper-for-title padding-wrapper-for-title-${headingTag}">
                                        <tbody><tr><td${innerTdPaddingAttr}>
                                            <${headingTag} class="rock-content-editable" style="${cleanStyle.join(" ")}">${headingText}</${headingTag}>
                                        </td></tr></tbody>
                                    </table>
                                </td></tr></tbody>
                            </table>
                        </td></tr></tbody>
                    `;

                    return wrapper;
                },

                // eslint-disable-next-line @typescript-eslint/naming-convention
                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const versionNumber = getComponentVersionNumber(componentElement);
                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, "v17.3-alpha") >= 0) {
                        return componentElement; // Already migrated
                    }

                    const heading = (componentElement.querySelector("h1, h2, h3, h4, h5, h6") ?? null) as HTMLHeadingElement | null;
                    if (!heading) {
                        throw new Error("Heading tag not found in title component.");
                    }

                    const headingTag = heading.tagName.toLowerCase();
                    const cssClasses = headingTag === "h1"
                        ? {
                            margin: "margin-heading-1",
                            border: "border-heading-1",
                            padding: "padding-heading-1",
                            font: "font-heading-1"
                        }
                        : headingTag === "h2"
                            ? {
                                margin: "margin-heading-2",
                                border: "border-heading-2",
                                padding: "padding-heading-2",
                                font: "font-heading-2"
                            }
                            : {
                                margin: "margin-heading-3",
                                border: "border-heading-3",
                                padding: "padding-heading-3",
                                font: "font-heading-3"
                            };

                    // Add the CSS classes.
                    const marginWrapperTd = componentElement.querySelector(":scope > tbody > tr > td");
                    if (marginWrapperTd) {
                        marginWrapperTd.classList.add(cssClasses.margin);

                        const borderWrapperTd = marginWrapperTd.querySelector(".border-wrapper > tbody > tr > td");
                        if (borderWrapperTd) {
                            borderWrapperTd.classList.add(cssClasses.border);

                            const paddingWrapperTd = borderWrapperTd.querySelector(".padding-wrapper > tbody > tr > td");
                            if (paddingWrapperTd) {
                                paddingWrapperTd.classList.add(cssClasses.padding);

                                // Add the font class to the heading element.
                                heading.classList.add("font-global", cssClasses.font);
                            }
                        }
                    }

                    setComponentVersionNumber(componentElement, "v17.3-alpha"); // Ensure version tracking

                    return componentElement;
                }
            ];

            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };

    return helper;
}

type TextComponentStructure = ComponentStructure & {
    readonly contentWrapper: HTMLHeadElement | null;
    readonly borderEl: HTMLElement;
    readonly marginEl: HTMLElement;
    readonly paddingEl: HTMLElement;
};

export function getTextComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): TextComponentStructure | null;
    createComponentElement(): HTMLElement;
} {
    const latestVersion = "v17.3-alpha" as const;

    const helper = {
        createComponentElement(): HTMLElement {
            const componentStructure = createComponent(
                "text",
                latestVersion,
                // Wrap component in a content-wrapper so the wrapper can be styled.
                // It's important that no whitespace is left around the editable area.
                `<div class="content-wrapper content-wrapper-for-text ${RockCssClassContentEditable}"><p style="margin: 0;">Let's see what you have to say!</p></div>`
            );

            const componentElements = helper.getElements(componentStructure.marginWrapper.table);

            if (!componentElements) {
                throw new Error("Failed to create text component element. Structure is invalid.");
            }

            // Set CSS classes to enable global and component-specific styles.
            componentElements.marginEl.classList.add(GlobalCssClasses.marginParagraph);
            componentElements.borderEl.classList.add(GlobalCssClasses.borderParagraph);
            componentElements.paddingEl.classList.add(GlobalCssClasses.paddingParagraph);
            componentElements.contentWrapper?.classList.add(GlobalCssClasses.fontGlobal, GlobalCssClasses.fontParagraph);

            return componentElements.marginWrapper.table;
        },

        getElements(componentElement: Element): TextComponentStructure | null {
            if (!componentElement.classList.contains("component-text")) {
                throw new Error(`Element is not a text component element: ${componentElement.outerHTML}`);
            }

            const wrappers = findComponentInnerWrappers(componentElement);

            if (wrappers) {
                return {
                    ...wrappers,

                    get contentWrapper(): HTMLElement | null {
                        return wrappers.marginWrapper.borderWrapper.paddingWrapper.td.querySelector(getContentWrapperSelector("text")) as HTMLElement | null;
                    },

                    get marginEl(): HTMLElement {
                        return wrappers.marginWrapper.td;
                    },

                    get borderEl(): HTMLElement {
                        return wrappers.marginWrapper.borderWrapper.td;
                    },

                    get paddingEl(): HTMLElement {
                        return wrappers.marginWrapper.borderWrapper.paddingWrapper.td;
                    }
                };
            }
            else {
                return null;
            }
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-text")) {
                throw new Error(`Element is not a text component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            return !versionNumber || compareComponentVersions(versionNumber, latestVersion) < 0;
        },

        migrate(oldComponentElement: Element): Element {
            if (!oldComponentElement.classList.contains("component-text")) {
                throw new Error(`Element is not a text component element: ${oldComponentElement.outerHTML}`);
            }

            if (!helper.isMigrationRequired(oldComponentElement)) {
                return oldComponentElement;
            }

            // Migrations should be in chronological order
            // and should not use helper methods or variables that
            // can change.
            const migrations = [
                function v0ToV2Alpha(componentElement: Element): Element {
                    if (getComponentVersionNumber(componentElement)) {
                        return componentElement; // Already migrated
                    }

                    // Create the new root table element
                    const newRoot = document.createElement("table");
                    newRoot.setAttribute("border", "0");
                    newRoot.setAttribute("cellpadding", "0");
                    newRoot.setAttribute("cellspacing", "0");
                    newRoot.setAttribute("width", "100%");
                    newRoot.setAttribute("role", "presentation");
                    newRoot.classList.add("margin-wrapper", "component", "component-text");
                    newRoot.setAttribute("data-state", "component");
                    setComponentVersionNumber(newRoot, "v2-alpha"); // Ensure version tracking

                    // Check for `.js-component-text-wrapper`
                    const oldWrapper = (componentElement.querySelector(".js-component-text-wrapper") ?? null) as HTMLElement | null;

                    // Start building the inner structure
                    let innerStructure = `
                        <tbody><tr><td>
                            <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="border-wrapper" style="border-collapse: separate !important;">
                                <tbody><tr><td`;

                    let padding = "";
                    if (oldWrapper) {
                        // Extract styles from `.js-component-text-wrapper`
                        const backgroundColor = (componentElement as HTMLElement).style.backgroundColor || "";
                        const borderWidth = oldWrapper.style.borderWidth || "";
                        const borderColor = oldWrapper.style.borderColor || "";
                        const borderStyle = oldWrapper.style.borderStyle || "";
                        padding = oldWrapper.style.padding || "";

                        // Apply inline styles for full "v0" migration
                        let inlineStyle = "";
                        if (backgroundColor) inlineStyle += `background-color: ${backgroundColor}; `;
                        if (borderWidth) inlineStyle += `border-width: ${borderWidth}; `;
                        if (borderColor) inlineStyle += `border-color: ${borderColor}; `;
                        if (borderStyle) inlineStyle += `border-style: ${borderStyle}; `;

                        innerStructure += inlineStyle ? ` style="${inlineStyle.trim()}"` : "";
                    }

                    innerStructure += `>
                                    <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper">
                                        <tbody><tr><td`;

                    if (oldWrapper) {
                        // Apply padding style if `.js-component-text-wrapper` existed
                        innerStructure += padding ? ` style="padding: ${padding};"` : "";
                    }

                    innerStructure += `>
                                            <div class="content-wrapper content-wrapper-for-text rock-content-editable"></div>
                                        </td></tr></tbody>
                                    </table>
                                </td></tr></tbody>
                            </table>
                        </td></tr></tbody>
                    `;

                    // Set the new structure
                    newRoot.innerHTML = innerStructure;

                    // Move existing content into `.rock-content-editable`
                    const newContentContainer = newRoot.querySelector(".rock-content-editable") as HTMLElement;
                    if (oldWrapper) {
                        // Move children of `.js-component-text-wrapper`
                        oldWrapper.childNodes.forEach(node => newContentContainer.appendChild(node.cloneNode(true)));
                    }
                    else {
                        // Move children of the original `.component-text` (simple case)
                        componentElement.childNodes.forEach(node => newContentContainer.appendChild(node.cloneNode(true)));
                    }

                    return newRoot;
                },

                // eslint-disable-next-line @typescript-eslint/naming-convention
                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const versionNumber = getComponentVersionNumber(componentElement);
                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, "v17.3-alpha") >= 0) {
                        return componentElement; // Already migrated
                    }

                    const contentWrapper = componentElement.querySelector(".content-wrapper-for-text");

                    if (!contentWrapper) {
                        throw new Error("Content wrapper not found in text component.");
                    }

                    // Add the CSS classes.
                    const marginWrapperTd = componentElement.querySelector(":scope > tbody > tr > td");
                    if (marginWrapperTd) {
                        marginWrapperTd.classList.add("margin-paragraph");

                        const borderWrapperTd = marginWrapperTd.querySelector(".border-wrapper > tbody > tr > td");
                        if (borderWrapperTd) {
                            borderWrapperTd.classList.add("border-paragraph");

                            const paddingWrapperTd = borderWrapperTd.querySelector(".padding-wrapper > tbody > tr > td");
                            if (paddingWrapperTd) {
                                paddingWrapperTd.classList.add("padding-paragraph");

                                // Add the font class to the content wrapper element.
                                contentWrapper.classList.add("font-global", "font-paragraph");
                            }
                        }
                    }

                    setComponentVersionNumber(componentElement, "v17.3-alpha"); // Ensure version tracking

                    return componentElement;
                }
            ];

            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };

    return helper;
}

export function getCodeComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): ComponentStructure & { readonly contentWrapper: HTMLElement | null; } | null
    createComponentElement(): HTMLElement;
} {
    const latestVersion = "v17.3-alpha" as const;

    return {
        createComponentElement(): HTMLElement {
            const componentElements = createComponent(
                "code",
                latestVersion,
                // Wrap component in a content-wrapper so the wrapper can be styled.
                // It's important that no whitespace is left around the editable area.
                `<div class="content-wrapper content-wrapper-for-code ${RockCssClassContentEditable}">Add your code here...</div>`
            );
            return componentElements.marginWrapper.table;
        },

        getElements(componentElement: Element): ComponentStructure & { readonly contentWrapper: HTMLElement | null; } | null {
            if (!componentElement.classList.contains("component-code")) {
                throw new Error(`Element is not a code component element: ${componentElement.outerHTML}`);
            }

            const wrappers = findComponentInnerWrappers(componentElement);

            if (wrappers) {
                return {
                    ...wrappers,

                    get contentWrapper(): HTMLElement | null {
                        const searchFromElement = wrappers.marginWrapper.borderWrapper.paddingWrapper.td;
                        return (searchFromElement.querySelector(getContentWrapperSelector("code")) ?? null) as HTMLElement | null;
                    }
                };
            }

            return wrappers;
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-code")) {
                throw new Error(`Element is not a code component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            return !versionNumber || compareComponentVersions(versionNumber, latestVersion) < 0;
        },

        migrate(oldComponentElement: Element): Element {
            if (!oldComponentElement.classList.contains("component-code")) {
                throw new Error(`Element is not a code component element: ${oldComponentElement.outerHTML}`);
            }

            if (!this.isMigrationRequired(oldComponentElement)) {
                return oldComponentElement;
            }

            const migrations = [
                function v0ToV2Alpha(componentElement: Element): Element {
                    if (!componentElement.classList.contains("component-code")) {
                        throw new Error("Element is not a code component.");
                    }

                    const newComponentElement = document.createElement("table");
                    newComponentElement.setAttribute("border", "0");
                    newComponentElement.setAttribute("cellpadding", "0");
                    newComponentElement.setAttribute("cellspacing", "0");
                    newComponentElement.setAttribute("width", "100%");
                    newComponentElement.setAttribute("role", "presentation");
                    newComponentElement.className = "margin-wrapper margin-wrapper-for-code component component-code";
                    newComponentElement.setAttribute("data-state", "component");

                    // Ensure version tracking.
                    setComponentVersionNumber(newComponentElement, "v2-alpha");

                    const margin = (componentElement as HTMLElement).style.margin || "";

                    newComponentElement.innerHTML = `
                    <tbody><tr><td${margin ? ` style="padding: ${margin};"` : ""}>
                        <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="border-wrapper border-wrapper-for-code" style="border-collapse: separate !important;">
                            <tbody><tr><td style="overflow: hidden;">
                                <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper padding-wrapper-for-code">
                                    <tbody><tr><td>
                                        <div class="content-wrapper content-wrapper-for-code rock-content-editable"></div>
                                    </td></tr></tbody>
                                </table>
                            </td></tr></tbody>
                        </table>
                    </td></tr></tbody>
                `;

                    const contentContainer = newComponentElement.querySelector(".rock-content-editable") as HTMLElement;
                    contentContainer.innerHTML = componentElement.innerHTML;

                    return newComponentElement;
                },

                // eslint-disable-next-line @typescript-eslint/naming-convention
                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const versionNumber = getComponentVersionNumber(componentElement);
                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, "v17.3-alpha") >= 0) {
                        return componentElement; // Already migrated
                    }

                    // Bump version.
                    setComponentVersionNumber(componentElement, "v17.3-alpha");

                    return componentElement;
                }
            ];

            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement as Element);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };
}

export function createDividerGlobalAdapter(): DividerGlobalAdapter {
    const globalVersions = ["v0", "v17.3-alpha", "v18.2"] as const;
    type DividerGlobalVersion = (typeof globalVersions)[number];

    const attributeValues = {
        META_NAME_GLOBAL_DIVIDER_VERSION: "x-rock-global-divider-version"
    } as const;

    const attributeNames = {
        DATA_COMPONENT_HORIZONTAL_ALIGNMENT: "data-component-horizontal-alignment",
        DATA_COMPONENT_WIDTH: "data-component-width"
    } as const;

    const defaultGlobalProps: DividerGlobalProps = {
        style: "solid",
        thicknessPx: 1,
        color: "#8b8ba7",
        widthPercent: 100,
        horizontalAlignment: "center",
        marginPx: {
            top: 12,
            bottom: 12,
            left: 0,
            right: 0
        }
    };

    function getGlobalVersion(emailDocument: Document): DividerGlobalVersion {
        // v18.2 and later use the meta tag to track global button schema version.
        const globalVersion = getMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_DIVIDER_VERSION)?.getAttribute("content");

        if (globalVersion && globalVersions.includes(globalVersion as DividerGlobalVersion)) {
            return globalVersion as DividerGlobalVersion;
        }

        // Try to infer from structure.
        const hasAnyGlobalStylesFromV2_1_alpha =
            findRockStyleRules(emailDocument, ".padding-wrapper-for-divider>tbody>tr>td").any()
            || findRockStyleRules(emailDocument, ".margin-wrapper-for-divider>tbody>tr>td").any();

        if (hasAnyGlobalStylesFromV2_1_alpha) {
            // v17.3-alpha and later used rock-styles for global divider styles.
            return "v17.3-alpha";
        }

        // No version info, assume v0 (legacy).
        return "v0";
    }

    // Don't modify a specific version once released.
    // Instead, add new versions using semver.
    // The major and minor versions should align with the current Rock version.
    // If necessary, you can add patch versions for fixes within the same release.
    // Avoid defining pre-release versions (like "v17.3-alpha") unless absolutely necessary.
    const adapters: Record<DividerGlobalVersion, GlobalAdapterSnapshot<DividerGlobalProps>> = {
        "v0": {
            version: "v0",

            readGlobalProps(_emailDocument: Document): DividerGlobalProps {
                // No global props for v0.
                return {
                    style: null,
                    thicknessPx: null,
                    color: null,
                    widthPercent: null,
                    horizontalAlignment: null,
                    marginPx: null
                };
            },

            writeGlobalProps(_emailDocument: Document, _globalProps: DividerGlobalProps): void {
                // No global props for v0.
            }
        },

        "v17.3-alpha": {
            version: "v17.3-alpha",

            readGlobalProps(emailDocument: Document): DividerGlobalProps {
                const paddingWrapperTdStyles = findRockStyleRules(emailDocument, ".padding-wrapper-for-divider>tbody>tr>td")
                    .select(r => r.style)
                    .toArray();
                const marginWrapperTdStyles = findRockStyleRules(emailDocument, ".margin-wrapper-for-divider>tbody>tr>td")
                    .select(r => r.style)
                    .toArray();
                const firstDividerWithoutHorizontalAlignment = emailDocument.querySelector(`.component-divider:not([data-component-horizontal-alignment="true"]) > tbody > tr > td`);
                const firstDividerWithoutWidth = emailDocument.querySelector(`.component-divider:not([data-component-width="true"]) .border-wrapper-for-divider`);

                return {
                    style: toBorderStyleOrNull(getStylePropertyValueOrNull(paddingWrapperTdStyles, "border-top-style")),
                    thicknessPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(paddingWrapperTdStyles, "border-top-width")),
                    color: getStylePropertyValueOrNull(paddingWrapperTdStyles, "border-top-color"),
                    marginPx: getStylePaddingPx(marginWrapperTdStyles),
                    horizontalAlignment: toHorizontalAlignmentOrNull(firstDividerWithoutHorizontalAlignment?.getAttribute("align")),
                    widthPercent: toPercentageNumericValueOrNull(firstDividerWithoutWidth?.getAttribute("width"))
                };
            },

            writeGlobalProps(emailDocument: Document, globalProps: DividerGlobalProps): void {
                const paddingWrapperTdRule = findRockStyleRules(emailDocument, ".padding-wrapper-for-divider>tbody>tr>td").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".padding-wrapper-for-divider>tbody>tr>td");
                const marginWrapperTdRule = findRockStyleRules(emailDocument, ".margin-wrapper-for-divider>tbody>tr>td").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".margin-wrapper-for-divider>tbody>tr>td");
                const rules = [
                    marginWrapperTdRule,
                    paddingWrapperTdRule
                ];
                const dividersWithoutWidth = Enumerable.from(emailDocument.querySelectorAll(`.component-divider:not([data-component-width="true"]) .border-wrapper-for-divider`)).ofType<HTMLElement>(isHTMLElement).toArray();
                const dividersWithoutHorizontalAlignment = Enumerable.from(emailDocument.querySelectorAll(`.component-divider:not([data-component-horizontal-alignment="true"]) > tbody > tr > td`)).ofType<HTMLElement>(isHTMLElement).toArray();

                setStyleBorderStyle(
                    paddingWrapperTdRule.style,
                    globalProps.style
                        ? {
                            top: globalProps.style,
                            bottom: "none",
                            left: "none",
                            right: "none"
                        }
                        : null);

                setStyleBorderWidthPx(
                    paddingWrapperTdRule.style,
                    !isNullish(globalProps.thicknessPx)
                        ? {
                            top: globalProps.thicknessPx,
                            bottom: 0,
                            left: 0,
                            right: 0
                        }
                        : null);

                setStyleBorderColor(paddingWrapperTdRule.style,
                    globalProps.color
                        ? {
                            top: globalProps.color,
                            bottom: "transparent",
                            left: "transparent",
                            right: "transparent"
                        }
                        : null);

                // divider width %
                const dividerWidthPercentage = toPercentageStringValueOrNull(globalProps.widthPercent);
                dividersWithoutWidth.forEach(divider => {
                    setAttributePropertyValue(divider, "width", dividerWidthPercentage);
                    setStylePropertyValue(divider.style, "width", dividerWidthPercentage);
                });

                // horizontal alignment
                dividersWithoutHorizontalAlignment.forEach(dividerTd => {
                    setAttributePropertyValue(dividerTd, "align", globalProps.horizontalAlignment);
                });

                // margin
                setStylePaddingPx(marginWrapperTdRule.style, globalProps.marginPx);

                // All the above work only updates the in-memory document; now synchronize the sheet to the DOM.
                synchronizeRulesToDom(rules);
            }
        },

        "v18.2": {
            version: "v18.2",

            readGlobalProps(emailDocument: Document): DividerGlobalProps {
                const marginWrapperTdStyles = findRockStyleRules(emailDocument, ".margin-wrapper-for-divider > tbody > tr > td")
                    .select(rule => rule.style)
                    .toArray();
                const borderWrapperTableStyles = findRockStyleRules(emailDocument, ".border-wrapper-for-divider")
                    .select(rule => rule.style)
                    .toArray();

                const horizontalAlignmentMargin = getStyleShorthandValueOrNull(
                    borderWrapperTableStyles,
                    {
                        top: "margin-top",
                        bottom: "margin-bottom",
                        left: "margin-left",
                        right: "margin-right"
                    },
                    v => v);
                const right = horizontalAlignmentMargin?.right;
                const left = horizontalAlignmentMargin?.left;
                const horizontalAlignment: HorizontalAlignment | null =
                    left === "auto" && right === "auto" ? "center"
                    : left === "auto" ? "right"
                    : right === "auto" ? "left"
                    : null;

                return {
                    style: toBorderStyleOrNull(getStylePropertyValueOrNull(borderWrapperTableStyles, "border-top-style")),
                    thicknessPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(borderWrapperTableStyles, "border-top-width")),
                    color: getStylePropertyValueOrNull(borderWrapperTableStyles, "border-top-color"),
                    marginPx: getStylePaddingPx(marginWrapperTdStyles),
                    horizontalAlignment: horizontalAlignment,
                    widthPercent: toPercentageNumericValueOrNull(getStylePropertyValueOrNull(borderWrapperTableStyles, "width")),
                };
            },

            writeGlobalProps(emailDocument: Document, globalProps: DividerGlobalProps): void {
                addOrUpdateMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_DIVIDER_VERSION, "v18.2");

                const marginWrapperTdSelector = ".margin-wrapper-for-divider > tbody > tr > td";
                const marginWrapperTdRule = findRockStyleRules(emailDocument, marginWrapperTdSelector)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, marginWrapperTdSelector);
                const borderWrapperTableSelector = ".border-wrapper-for-divider";
                const borderWrapperTableRule = findRockStyleRules(emailDocument, borderWrapperTableSelector)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, borderWrapperTableSelector);
                const rules = [
                    marginWrapperTdRule,
                    borderWrapperTableRule
                ];

                // style
                setStyleBorderStyle(
                    borderWrapperTableRule.style,
                    globalProps.style
                        ? {
                            top: globalProps.style,
                            // The following must be null instead of "none" so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // thicknessPx
                setStyleBorderWidthPx(
                    borderWrapperTableRule.style,
                    !isNullish(globalProps.thicknessPx)
                        ? {
                            top: globalProps.thicknessPx,
                            // The following must be null instead of 0 so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // color
                setStyleBorderColor(
                    borderWrapperTableRule.style,
                    globalProps.color
                        ? {
                            top: globalProps.color,
                            // The following must be null instead of "transparent" so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // marginPx
                setStylePaddingPx(marginWrapperTdRule?.style, globalProps.marginPx);

                // horizontalAlignment
                        // Only set attribute values on components that don't have the data attribute.
                emailDocument.querySelectorAll(`.component-divider:not([${attributeNames.DATA_COMPONENT_HORIZONTAL_ALIGNMENT}]) ${marginWrapperTdSelector}`)
                    .forEach(marginWrapperTd => {
                        setAttributePropertyValue(marginWrapperTd, "align", globalProps.horizontalAlignment);
                    });
                emailDocument.querySelectorAll(`.component-divider:not([${attributeNames.DATA_COMPONENT_HORIZONTAL_ALIGNMENT}]) ${borderWrapperTableSelector}`)
                    .forEach(borderWrapperTable => {
                        setAttributePropertyValue(borderWrapperTable, "align", globalProps.horizontalAlignment);
                    });
                // Add margin to achieve horizontal alignment in Outlook.
                const horizontalAlignmentMargin: ShorthandModel<string | null> | null =
                    globalProps.horizontalAlignment === "center"
                        ? {
                            top: "0",
                            bottom: "0",
                            left: "auto",
                            right: "auto"
                        }
                        : globalProps.horizontalAlignment === "left"
                            ? {
                                top: "0",
                                bottom: "0",
                                left: "0",
                                right: "auto"
                            }
                            : globalProps.horizontalAlignment === "right"
                                ? {
                                    top: "0",
                                    bottom: "0",
                                    left: "auto",
                                    right: "0"
                                }
                                : null;
                setStyleShorthandValue(
                    borderWrapperTableRule?.style,
                    horizontalAlignmentMargin,
                    {
                        top: "margin-top",
                        bottom: "margin-bottom",
                        left: "margin-left",
                        right: "margin-right"
                    },
                    v => v);

                // widthPercent
                setStylePropertyValue(borderWrapperTableRule?.style, "width", toPercentageStringValueOrNull(globalProps.widthPercent));
                emailDocument.querySelectorAll(`.component-divider:not([${attributeNames.DATA_COMPONENT_WIDTH}]) ${borderWrapperTableSelector}`)
                    .forEach(borderWrapperTable => {
                        setAttributePropertyValue(borderWrapperTable, "width", toPercentageStringValueOrNull(globalProps.widthPercent));
                    });

                // All the above work only updates the in-memory document; now synchronize the sheet to the DOM.
                synchronizeRulesToDom(rules);
            },
        }
    };

    const latestAdapter = adapters[getLatestVersion(globalVersions)];

    return {
        ...latestAdapter,

        areGlobalDefaultsNeeded(emailDocument: Document): boolean {
            return compareComponentVersions(getGlobalVersion(emailDocument), "v0") === 0;
        },

        getDefaultGlobalProps(): DividerGlobalProps {
            return defaultGlobalProps;
        },

        migrateGlobalProps(emailDocument: Document): DividerGlobalProps {
            const adapter = adapters[getGlobalVersion(emailDocument)];
            const thisAdapter = latestAdapter;
            const globalProps = adapter.readGlobalProps(emailDocument);

            if (compareComponentVersions(adapter.version, thisAdapter.version) >= 0) {
                // Already on this or a newer version; no migration needed.
                return globalProps;
            }

            // Clear the global props using the old adapter (to clean up any old styles),
            // and restore them using the new adapter.
            adapter.writeGlobalProps(emailDocument, {
                color: null,
                horizontalAlignment: null,
                marginPx: null,
                style: null,
                thicknessPx: null,
                widthPercent: null
            });
            thisAdapter.writeGlobalProps(emailDocument, globalProps);
            return thisAdapter.readGlobalProps(emailDocument);
        },
    };
}

function getComponentVersion<T extends string>(componentElement: HTMLElement, componentVersions: readonly T[]): T {
    const version = componentElement.getAttribute("data-version");

    if (version && componentVersions.includes(version as T)) {
        return version as T;
    }

    return getEarliestVersion(componentVersions);
}

export function createDividerComponentAdapter(): DividerComponentAdapter {
    const componentVersions = ["v0", "v17.3-alpha", "v18.2"] as const;
    type DividerComponentVersion = (typeof componentVersions)[number];
    const latestVersion: DividerComponentVersion = getLatestVersion(componentVersions);

    const dividerDatasetKeys = {
        COMPONENT_HORIZONTAL_ALIGNMENT: "data-component-horizontal-alignment",
        COMPONENT_WIDTH: "data-component-width"
    } as const;

    // Define the adapters for each version and we'll only return the latest.
    // These are used for both component migration and global props migration.
    const adapters: Record<DividerComponentVersion, DividerComponentAdapter> = {
        "v0": {
            version: "v0",

            createComponentElement(emailDocument: Document): HTMLElement {
                const componentElement = createHtmlElement(emailDocument, `
<div class="component component-divider" data-state="component">
    <div></div>
</div>`);

                // Avoid setting default local props and use global props instead.
                const defaultLocalProps: DividerLocalProps = {
                    style: null,
                    thicknessPx: null,
                    color: null,
                    marginPx: null,
                    isDividedWithLine: false,
                    horizontalAlignment: null, // not supported in v0
                    widthPercent: null // not supported in v0
                };

                adapters["v0"].writeLocalProps(componentElement, defaultLocalProps);

                return componentElement;
            },

            migrateComponent(_emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                // No migration needed for v0.
                return componentElement;
            },

            readLocalProps(componentElement: HTMLElement): DividerLocalProps {
                const dividerElement = componentElement.querySelector(":scope > div,:scope > hr") as HTMLElement | null;

                const localProps: DividerLocalProps = {
                    style: null, // No border style in v0.
                    thicknessPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(dividerElement?.style, "height")),
                    color: getStylePropertyValueOrNull(dividerElement?.style, "background-color"),
                    marginPx: getStyleMarginPx(dividerElement?.style),
                    isDividedWithLine: dividerElement?.tagName.toLowerCase() === "hr",
                    horizontalAlignment: null, // not supported in v0
                    widthPercent: null // not supported in v0
                };

                return localProps;
            },

            writeLocalProps(componentElement: HTMLElement, localProps: DividerLocalProps): void {
                const dividerElement = componentElement.querySelector(":scope > div,:scope > hr") as HTMLElement | null;

                setStylePropertyValue(dividerElement?.style, "height", toPixelStringValueOrNull(localProps.thicknessPx));
                setStylePropertyValue(dividerElement?.style, "background-color", localProps.color);
                setStyleMarginPx(dividerElement?.style, localProps.marginPx);
                if (dividerElement) {
                    if (localProps.isDividedWithLine) {
                        if (dividerElement?.tagName.toLowerCase() !== "hr") {
                            replaceTagName(dividerElement, "hr");
                        }
                    }
                    else {
                        if (dividerElement?.tagName.toLowerCase() !== "div") {
                            replaceTagName(dividerElement, "div");
                        }
                    }
                }
            }
        },
        "v17.3-alpha": {
            version: "v17.3-alpha",

            createComponentElement(emailDocument: Document): HTMLElement {
                const componentElement = createHtmlElement(emailDocument, `
<table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="margin-wrapper margin-wrapper-for-divider component component-divider" data-state="component" data-version="v17.3-alpha">
    <tbody>
        <tr>
            <td>
                <table border="0" cellpadding="0" cellspacing="0" role="presentation" class="border-wrapper border-wrapper-for-divider">
                    <tbody>
                        <tr>
                            <td>
                                <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper padding-wrapper-for-divider">
                                    <tbody>
                                        <tr>
                                            <td></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>
`);

                // Avoid setting default local props and use global props instead.
                const defaultLocalProps: DividerLocalProps = {
                    style: null,
                    thicknessPx: null,
                    color: null,
                    marginPx: null,
                    isDividedWithLine: false,
                    horizontalAlignment: null,
                    widthPercent: null
                };
                adapters["v17.3-alpha"].writeLocalProps(componentElement, defaultLocalProps);

                return componentElement;
            },

            readLocalProps(componentElement: HTMLElement): DividerLocalProps {
                const marginWrapperTd = componentElement.querySelector(":scope > tbody > tr > td") as HTMLElement | null;
                const paddingWrapperTd = componentElement.querySelector(".padding-wrapper-for-divider > tbody > tr > td") as HTMLElement | null;
                const borderWrapperTable = componentElement.querySelector(".border-wrapper-for-divider") as HTMLElement | null;

                return {
                    style: toBorderStyleOrNull(getStylePropertyValueOrNull(paddingWrapperTd?.style, "border-top-style")),
                    thicknessPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(paddingWrapperTd?.style, "border-top-width")),
                    color: getStylePropertyValueOrNull(paddingWrapperTd?.style, "border-top-color"),
                    isDividedWithLine: false, // No longer used in v17.3-alpha
                    marginPx: getStylePaddingPx(marginWrapperTd?.style), // Margin is stored as padding on the margin wrapper td.
                    horizontalAlignment: toHorizontalAlignmentOrNull(marginWrapperTd?.getAttribute("align")),
                    widthPercent: toPercentageNumericValueOrNull(borderWrapperTable?.getAttribute("width"))
                };
            },

            writeLocalProps(componentElement: HTMLElement, localProps: DividerLocalProps): void {
                const marginWrapperTd = componentElement.querySelector(":scope > tbody > tr > td") as HTMLElement | null;
                const paddingWrapperTd = componentElement.querySelector(".padding-wrapper-for-divider > tbody > tr > td") as HTMLElement | null;
                const borderWrapperTable = componentElement.querySelector(".border-wrapper-for-divider") as HTMLElement | null;

                setStyleBorderStyle(
                    paddingWrapperTd?.style,
                    localProps.style
                        ? {
                            top: localProps.style,
                            bottom: "none",
                            left: "none",
                            right: "none"
                        }
                        : null);
                setStyleBorderWidthPx(
                    paddingWrapperTd?.style,
                    !isNullish(localProps.thicknessPx)
                        ? {
                            top: localProps.thicknessPx,
                            bottom: 0,
                            left: 0,
                            right: 0
                        }
                        : null);
                setStyleBorderColor(
                    paddingWrapperTd?.style,
                    localProps.color
                        ? {
                            top: localProps.color,
                            bottom: "transparent",
                            left: "transparent",
                            right: "transparent"
                        }
                        : null
                );
                setStylePaddingPx(marginWrapperTd?.style, localProps.marginPx);

                // horizontal alignment
                setAttributePropertyValue(marginWrapperTd, "align", localProps.horizontalAlignment);
                setAttributePropertyValue(componentElement, dividerDatasetKeys.COMPONENT_HORIZONTAL_ALIGNMENT, !isNullish(localProps.horizontalAlignment) ? "true" : null);

                // width
                setStylePropertyValue(borderWrapperTable?.style, "width", toPercentageStringValueOrNull(localProps.widthPercent));
                setAttributePropertyValue(borderWrapperTable, "width", toPercentageStringValueOrNull(localProps.widthPercent));
                setAttributePropertyValue(componentElement, dividerDatasetKeys.COMPONENT_WIDTH, !isNullish(localProps.widthPercent) ? "true" : null);
            },

            migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                const version = getComponentVersion(componentElement, componentVersions);
                const thisAdapter = adapters["v17.3-alpha"];
                const adapter = adapters[version];

                if (thisAdapter === adapter || compareComponentVersions(adapter.version, thisAdapter.version) >= 0) {
                    // Already on this or a later version; no migration needed.
                    return componentElement;
                }

                // Get the props of the current version,
                // create a new component element at the latest version,
                // and write the old props to the new element.
                const localProps = adapter.readLocalProps(componentElement);
                const newComponentElement = thisAdapter.createComponentElement(emailDocument);
                thisAdapter.writeLocalProps(newComponentElement, localProps);
                componentElement.replaceWith(newComponentElement);

                return newComponentElement;
            }
        },
        "v18.2": {
            version: "v18.2",

            createComponentElement(emailDocument: Document): HTMLElement {
                const componentElement = createHtmlElement(emailDocument, `
<div class="component component-divider"
     data-state="component"
     data-version="v18.2">
    <table class="margin-wrapper margin-wrapper-for-divider"
           border="0"
           cellpadding="0"
           cellspacing="0"
           role="presentation"
           style="vertical-align: top;"
           width="100%">
        <tbody>
            <tr>
                <td style="font-size: 0px;
                           word-break: break-word;">
                    <table class="border-wrapper border-wrapper-for-divider"
                           border="0"
                           cellpadding="0"
                           cellspacing="0"
                           role="presentation"
                           style="font-size: 1px;">
                        <tbody>
                            <tr>
                                <td style="height: 0; line-height: 0;"> &nbsp;
</td>
                            </tr>
                        </tbody>
                    </table>
                </td>
            </tr>
        </tbody>
    </table>
</div>
`);

                // Avoid setting default local props and use global props instead.
                const defaultLocalProps: DividerLocalProps = {
                    style: null,
                    thicknessPx: null,
                    color: null,
                    marginPx: null,
                    isDividedWithLine: false,
                    horizontalAlignment: null,
                    widthPercent: null
                };
                adapters["v18.2"].writeLocalProps(componentElement, defaultLocalProps);

                return componentElement;
            },

            readLocalProps(componentElement: HTMLElement): DividerLocalProps {
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-divider > tbody > tr > td") as HTMLElement | null | undefined;
                const borderWrapperTable = marginWrapperTd?.querySelector(".border-wrapper-for-divider") as HTMLElement | null | undefined;

                return {
                    isDividedWithLine: false, // No longer used since v17.3-alpha
                    style: toBorderStyleOrNull(getStylePropertyValueOrNull(borderWrapperTable?.style, "border-top-style")),
                    thicknessPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(borderWrapperTable?.style, "border-top-width")),
                    color: getStylePropertyValueOrNull(borderWrapperTable?.style, "border-top-color"),
                    marginPx: getStylePaddingPx(marginWrapperTd?.style), // Margin is stored as padding on the margin wrapper td.
                    horizontalAlignment: toHorizontalAlignmentOrNull(marginWrapperTd?.getAttribute("align")),
                    widthPercent: toPercentageNumericValueOrNull(borderWrapperTable?.getAttribute("width") || "100%")
                };
            },

            writeLocalProps(componentElement: HTMLElement, localProps: DividerLocalProps): void {
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-divider > tbody > tr > td") as HTMLElement | null | undefined;
                const borderWrapperTable = marginWrapperTd?.querySelector(".border-wrapper-for-divider") as HTMLElement | null | undefined;

                // style
                setStyleBorderStyle(
                    borderWrapperTable?.style,
                    localProps.style
                        ? {
                            top: localProps.style,
                            // The following must be null instead of "none" so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // thicknessPx
                setStyleBorderWidthPx(
                    borderWrapperTable?.style,
                    !isNullish(localProps.thicknessPx)
                        ? {
                            top: localProps.thicknessPx,
                            // The following must be null instead of 0 so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // color
                setStyleBorderColor(
                    borderWrapperTable?.style,
                    localProps.color
                        ? {
                            top: localProps.color,
                            // The following must be null instead of "transparent" so Outlook works.
                            bottom: null,
                            left: null,
                            right: null
                        }
                        : null);

                // marginPx
                setStylePaddingPx(marginWrapperTd?.style, localProps.marginPx);

                // horizontalAlignment
                setAttributePropertyValue(marginWrapperTd, "align", localProps.horizontalAlignment);
                setAttributePropertyValue(borderWrapperTable, "align", localProps.horizontalAlignment);
                // Add margin to achieve horizontal alignment in Outlook.
                const horizontalAlignmentMargin: ShorthandModel<string | null> | null =
                    localProps.horizontalAlignment === "center"
                        ? {
                            top: "0",
                            bottom: "0",
                            left: "auto",
                            right: "auto"
                        }
                        : localProps.horizontalAlignment === "left"
                            ? {
                                top: "0",
                                bottom: "0",
                                left: "0",
                                right: "auto"
                            }
                            : localProps.horizontalAlignment === "right"
                                ? {
                                    top: "0",
                                    bottom: "0",
                                    left: "auto",
                                    right: "0"
                                }
                                : null;
                setStyleShorthandValue(
                    borderWrapperTable?.style,
                    horizontalAlignmentMargin,
                    {
                        top: "margin-top",
                        bottom: "margin-bottom",
                        left: "margin-left",
                        right: "margin-right"
                    },
                    v => v);
                setAttributePropertyValue(componentElement, dividerDatasetKeys.COMPONENT_HORIZONTAL_ALIGNMENT, !isNullish(localProps.horizontalAlignment) ? "true" : null);

                // widthPercent
                setStylePropertyValue(borderWrapperTable?.style, "width", toPercentageStringValueOrNull(localProps.widthPercent));
                setAttributePropertyValue(borderWrapperTable, "width", toPercentageStringValueOrNull(localProps.widthPercent));
                setAttributePropertyValue(componentElement, dividerDatasetKeys.COMPONENT_WIDTH, !isNullish(localProps.widthPercent) ? "true" : null);
            },

            migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                const version = getComponentVersion(componentElement, componentVersions);
                const thisAdapter = adapters["v18.2"];
                const adapter = adapters[version];

                if (thisAdapter === adapter || compareComponentVersions(adapter.version, thisAdapter.version) >= 0) {
                    // Already on this or a later version; no migration needed.
                    return componentElement;
                }

                // Get the props of the current version,
                // create a new component element at the latest version,
                // and write the old props to the new element.
                const localProps = adapter.readLocalProps(componentElement);
                const newComponentElement = thisAdapter.createComponentElement(emailDocument);
                thisAdapter.writeLocalProps(newComponentElement, localProps);
                componentElement.replaceWith(newComponentElement);

                return newComponentElement;
            }
        }
    };

    return adapters[latestVersion];
}

function getLatestVersion<T extends string>(versions: readonly T[] | T[]): T {
    return Enumerable.from(versions).aggregate((v1, v2) => compareComponentVersions(v1, v2) > 0 ? v1 : v2, "v0" as T);
}

function getEarliestVersion<T extends string>(versions: readonly T[] | T[]): T {
    return Enumerable.from(versions).aggregate((v1, v2) => compareComponentVersions(v1, v2) < 0 ? v1 : v2, "v9999" as T);
}

export function createRsvpComponentAdapter(): RsvpComponentAdapter {
    const componentVersions = ["v0", "v2-alpha", "v17.3-alpha"] as const;

    type RsvpComponentVersion = typeof componentVersions[number];

    const LATEST_VERSION = getLatestVersion(componentVersions);

    // These are for reading local component properties for each version.
    const localPropReaders: Record<RsvpComponentVersion, (componentElement: HTMLElement) => RsvpLocalProps> = {
        "v0": (componentElement: HTMLElement): RsvpLocalProps => {
            const rsvpInnerwrap = componentElement.querySelector(".rsvp-innerwrap") as HTMLElement | null;
            const acceptButtonShell = componentElement.querySelector(".accept-button-shell") as HTMLElement | null;
            const rsvpAcceptLink = componentElement.querySelector(".rsvp-accept-link") as HTMLElement | null;
            const rsvpAcceptContent = componentElement.querySelector(".rsvp-accept-content") as HTMLElement | null;
            const declineButtonShell = componentElement.querySelector(".decline-button-shell") as HTMLElement | null;
            const rsvpDeclineLink = componentElement.querySelector(".rsvp-decline-link") as HTMLElement | null;
            const rsvpGroupIdEl = componentElement.querySelector(".rsvp-group-id") as HTMLInputElement | null;
            const rsvpOccurrenceValueEl = componentElement.querySelector(".rsvp-occurrence-value") as HTMLInputElement | null;

            return {
                blockPaddingPx: null, // not supported in v0
                blockHorizontalAlignment: toHorizontalAlignmentOrNull(rsvpInnerwrap?.getAttribute("align")),
                fontFamily: getStylePropertyValueOrNull(rsvpAcceptLink?.style, "font-family"),
                fontSizePx: getStyleFontSizePx(rsvpAcceptLink?.style),
                isBold: getStyleIsBold(rsvpAcceptLink?.style),
                isUnderlined: getStyleIsUnderlined(rsvpAcceptLink?.style),
                isItalicized: null, // not supported in v0
                letterCase: null, // not supported in v0
                lineHeight: null, // not supported in v0
                buttonPaddingPx: getStylePaddingPx(rsvpAcceptContent?.style),
                buttonBorderRadiusPx: null, // not supported in v0
                acceptText: rsvpAcceptLink?.textContent || "Accept",
                acceptWidth: null, // not supported in v0
                acceptBackgroundColor: getStylePropertyValueOrNull(acceptButtonShell?.style, "background-color"),
                acceptTextColor: getStylePropertyValueOrNull(rsvpAcceptLink?.style, "color"),
                isDeclineHidden: getStylePropertyValueOrNull(declineButtonShell?.style, "display") === "none",
                declineText: rsvpDeclineLink?.textContent || "Decline",
                declineWidth: null, // not supported in v0
                declineBackgroundColor: getStylePropertyValueOrNull(declineButtonShell?.style, "background-color"),
                declineTextColor: getStylePropertyValueOrNull(rsvpDeclineLink?.style, "color"),
                rsvpGroupGuid: rsvpGroupIdEl?.value || null,
                rsvpOccurrenceValue: rsvpOccurrenceValueEl?.value || null
            };
        },
        "v2-alpha": (componentElement: HTMLElement): RsvpLocalProps => {
            const rsvpInnerwrap = componentElement.querySelector(".rsvp-innerwrap") as HTMLElement | null;
            const rsvpAcceptLink = componentElement.querySelector(".rsvp-accept-link") as HTMLElement | null;
            const rsvpDeclineLink = componentElement.querySelector(".rsvp-decline-link") as HTMLElement | null;
            const rsvpGroupIdEl = componentElement.querySelector(".rsvp-group-id") as HTMLInputElement | null;
            const rsvpOccurrenceValueEl = componentElement.querySelector(".rsvp-occurrence-value") as HTMLInputElement | null;
            const acceptButtonShell = componentElement.querySelector(".accept-button-shell") as HTMLElement | null;
            const declineButtonShell = componentElement.querySelector(".decline-button-shell") as HTMLElement | null;

            const acceptButtonShellAttrWidth = acceptButtonShell?.getAttribute("width") || "";
            const acceptWidthFixedPx = toPixelNumericValueOrNull(acceptButtonShell?.style.width || rsvpAcceptLink?.style.width);
            const acceptWidthIsFull = acceptButtonShellAttrWidth === "100%" || acceptButtonShell?.style.width === "100%";
            const acceptWidthIsFixed = !isNullish(acceptWidthFixedPx);

            const declineButtonShellAttrWidth = declineButtonShell?.getAttribute("width") || "";
            const declineWidthFixedPx = toPixelNumericValueOrNull(declineButtonShell?.style.width || rsvpDeclineLink?.style.width);
            const declineWidthIsFull = declineButtonShellAttrWidth === "100%" || declineButtonShell?.style.width === "100%";
            const declineWidthIsFixed = !isNullish(declineWidthFixedPx);

            return {
                blockPaddingPx: getStylePaddingPx(componentElement.style),
                blockHorizontalAlignment: toHorizontalAlignmentOrNull(rsvpInnerwrap?.getAttribute("align")),
                fontFamily: getStylePropertyValueOrNull(rsvpAcceptLink?.style, "font-family"),
                fontSizePx: getStyleFontSizePx(rsvpAcceptLink?.style),
                isBold: getStyleIsBold(rsvpAcceptLink?.style),
                isUnderlined: getStyleIsUnderlined(rsvpAcceptLink?.style),
                isItalicized: getStyleIsItalicized(rsvpAcceptLink?.style),
                letterCase: getStyleLetterCase(rsvpAcceptLink?.style),
                lineHeight: getStyleLineHeight(rsvpAcceptLink?.style),
                buttonPaddingPx: getStylePaddingPx(rsvpAcceptLink?.style),
                buttonBorderRadiusPx: getStyleBorderRadiusPx(acceptButtonShell?.style),
                acceptText: rsvpAcceptLink?.textContent || "Accept",
                acceptWidth: acceptWidthIsFull
                    ? {
                        mode: "full",
                        fixedWidthPx: null
                    }
                    : acceptWidthIsFixed
                        ? {
                            mode: "fixed",
                            fixedWidthPx: acceptWidthFixedPx!
                        }
                        : null,
                acceptBackgroundColor: getStylePropertyValueOrNull(acceptButtonShell?.style, "background-color"),
                acceptTextColor: getStylePropertyValueOrNull(rsvpAcceptLink?.style, "color"),
                isDeclineHidden: getStylePropertyValueOrNull(declineButtonShell?.style, "display") === "none",
                declineText: rsvpDeclineLink?.textContent || "Decline",
                declineWidth: declineWidthIsFull
                    ? {
                        mode: "full",
                        fixedWidthPx: null
                    }
                    : declineWidthIsFixed
                        ? {
                            mode: "fixed",
                            fixedWidthPx: declineWidthFixedPx!
                        }
                        : null,
                declineBackgroundColor: getStylePropertyValueOrNull(declineButtonShell?.style, "background-color"),
                declineTextColor: getStylePropertyValueOrNull(rsvpDeclineLink?.style, "color"),
                rsvpGroupGuid: rsvpGroupIdEl?.value || null,
                rsvpOccurrenceValue: rsvpOccurrenceValueEl?.value || null
            };
        },
        "v17.3-alpha": (componentElement: HTMLElement): RsvpLocalProps => {
            // v17.3-alpha uses the same structure as v2-alpha; it was only a version bump.
            // FYI, Don't do version bumps any more to keep things simpler.
            return localPropReaders["v2-alpha"](componentElement);
        }
    };

    function createEmptyComponentElement(emailDocument: Document): HTMLElement {
        const div = emailDocument.createElement("div");
        div.classList.add("component", "component-rsvp");
        setComponentVersionNumber(div, "v17.3-alpha");
        div.dataset.state = "component";
        div.innerHTML =
            `<table class="rsvp-outerwrap" border="0" cellpadding="0" cellspacing="0" role="presentation" width="100%" style="min-width: 100%;">
                <tbody>
                    <tr>
                        <td class="rsvp-innerwrap" valign="top" style="padding: 0;">
                            <table border="0" cellpadding="0" cellspacing="0" role="presentation">
                                <tbody>
                                    <tr>
                                        <td>
                                            <table class="accept-button-shell" border="0" cellpadding="0" cellspacing="0" role="presentation" style="border-collapse: separate; display: inline-table;">
                                                <tbody>
                                                    <tr>
                                                        <td class="rsvp-accept-content" align="center" valign="middle">
                                                            <a class="rsvp-accept-link ${RockCssClassContentEditable}" rel="noopener noreferrer" style="display: inline-block; letter-spacing: normal; text-align: center;"></a>
                                                        </td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </td>
                                        <td>
                                            <table class="decline-button-shell" border="0" cellpadding="0" cellspacing="0" role="presentation" style="border-collapse: separate;">
                                                <tbody>
                                                    <tr>
                                                        <td class="rsvp-decline-content" align="center" valign="middle">
                                                            <a class="rsvp-decline-link ${RockCssClassContentEditable}" rel="noopener noreferrer" style="display: inline-block; letter-spacing: normal; text-align: center;"></a>
                                                        </td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </td>
                    </tr>
                </tbody>
            </table>
            <input type="hidden" class="rsvp-group-id">
            <input type="hidden" class="rsvp-occurrence-value">`;
        return div;
    }

    function getComponentVersion(componentElement: HTMLElement): RsvpComponentVersion {
        const versionNumber = getComponentVersionNumber(componentElement);

        if (!versionNumber || !componentVersions.includes(versionNumber as RsvpComponentVersion)) {
            return "v0";
        }

        return versionNumber as RsvpComponentVersion;
    }

    const adapter: RsvpComponentAdapter = {
        version: LATEST_VERSION,

        createComponentElement(emailDocument: Document): HTMLElement {
            const componentElement = createEmptyComponentElement(emailDocument);

            // Use global props instead of local props when possible.
            const localPropDefaults: RsvpLocalProps = {
                blockPaddingPx: null,
                blockHorizontalAlignment: "center",
                fontFamily: FontFamilies.Arial,
                fontSizePx: 16,
                isBold: true,
                isUnderlined: false,
                isItalicized: null,
                letterCase: null,
                lineHeight: null,
                buttonPaddingPx: createShorthandModel(15),
                buttonBorderRadiusPx: createShorthandModel(3),
                acceptText: "Accept",
                acceptWidth: null,
                acceptBackgroundColor: "#16C98D",
                acceptTextColor: "#FFFFFF",
                isDeclineHidden: false,
                declineText: "Decline",
                declineWidth: null,
                declineBackgroundColor: "#D4442E",
                declineTextColor: "#FFFFFF",
                rsvpGroupGuid: null,
                rsvpOccurrenceValue: null
            };

            adapter.writeLocalProps(componentElement, localPropDefaults);

            // Global props will be applied after the component is added to the email DOM.
            return componentElement;
        },

        // Local Props
        readLocalProps(componentElement: HTMLElement): RsvpLocalProps {
            return localPropReaders[getComponentVersion(componentElement)](componentElement);
        },
        writeLocalProps(componentElement: HTMLElement, localProps: RsvpLocalProps): void {
            const rsvpInnerwrap = componentElement.querySelector(".rsvp-innerwrap") as HTMLElement | null;
            const rsvpAcceptLink = componentElement.querySelector("a.rsvp-accept-link") as HTMLAnchorElement | null;
            const rsvpDeclineLink = componentElement.querySelector("a.rsvp-decline-link") as HTMLAnchorElement | null;
            const acceptButtonShell = componentElement.querySelector(".accept-button-shell") as HTMLElement | null;
            const declineButtonShell = componentElement.querySelector(".decline-button-shell") as HTMLElement | null;
            const declineButtonShellParent = declineButtonShell?.parentElement;
            const rsvpGroupIdEl = componentElement.querySelector(".rsvp-group-id") as HTMLInputElement | null;
            const rsvpOccurrenceValueEl = componentElement.querySelector(".rsvp-occurrence-value") as HTMLInputElement | null;

            setStylePaddingPx(componentElement.style, localProps.blockPaddingPx);
            setAttributePropertyValue(rsvpInnerwrap, "align", localProps.blockHorizontalAlignment ?? "center"); // this might need to set the text-align inline style as well.
            setStylePropertyValue(rsvpAcceptLink?.style, "font-family", localProps.fontFamily);
            setStylePropertyValue(rsvpDeclineLink?.style, "font-family", localProps.fontFamily);
            setStyleFontSizePx(rsvpAcceptLink?.style, localProps.fontSizePx);
            setStyleFontSizePx(rsvpDeclineLink?.style, localProps.fontSizePx);
            setStyleIsBold(rsvpAcceptLink?.style, localProps.isBold);
            setStyleIsBold(rsvpDeclineLink?.style, localProps.isBold);
            setStyleIsUnderlined(rsvpAcceptLink?.style, localProps.isUnderlined);
            setStyleIsUnderlined(rsvpDeclineLink?.style, localProps.isUnderlined);
            setStyleIsItalicized(rsvpAcceptLink?.style, localProps.isItalicized);
            setStyleIsItalicized(rsvpDeclineLink?.style, localProps.isItalicized);
            setStyleLetterCase(rsvpAcceptLink?.style, localProps.letterCase);
            setStyleLetterCase(rsvpDeclineLink?.style, localProps.letterCase);
            setStyleLineHeight(rsvpAcceptLink?.style, localProps.lineHeight);
            setStyleLineHeight(rsvpDeclineLink?.style, localProps.lineHeight);
            setStylePaddingPx(rsvpAcceptLink?.style, localProps.buttonPaddingPx);
            setStylePaddingPx(rsvpDeclineLink?.style, localProps.buttonPaddingPx);

            // border radius
            setStyleBorderRadiusPx(acceptButtonShell?.style, localProps.buttonBorderRadiusPx);
            setStyleBorderRadiusPx(rsvpAcceptLink?.style, localProps.buttonBorderRadiusPx);
            setStyleBorderRadiusPx(declineButtonShell?.style, localProps.buttonBorderRadiusPx);
            setStyleBorderRadiusPx(rsvpDeclineLink?.style, localProps.buttonBorderRadiusPx);

            if (rsvpAcceptLink) {
                rsvpAcceptLink.textContent = localProps.acceptText;
                rsvpAcceptLink.title = localProps.acceptText;
            }

            // accept width
            if (localProps.acceptWidth?.mode === "full") {
                setAttributePropertyValue(acceptButtonShell, "width", "100%");
                setStylePropertyValue(acceptButtonShell?.style, "width", "100%");
            }
            else if (localProps.acceptWidth?.mode === "fixed") {
                setAttributePropertyValue(acceptButtonShell, "width", localProps.acceptWidth.fixedWidthPx); // no "px" in the attribute
                setStylePropertyValue(acceptButtonShell?.style, "width", toPixelStringValueOrNull(localProps.acceptWidth.fixedWidthPx));
            }
            else {
                // default and "fitToText"
                setAttributePropertyValue(acceptButtonShell, "width", null);
                setStylePropertyValue(acceptButtonShell?.style, "width", null);
            }

            setStylePropertyValue(acceptButtonShell?.style, "background-color", localProps.acceptBackgroundColor);
            setStylePropertyValue(rsvpAcceptLink?.style, "color", localProps.acceptTextColor);

            // decline is hidden
            setStylePropertyValue(declineButtonShell?.style, "display", localProps.isDeclineHidden ? "none" : "inline-table");
            setStylePaddingPx(declineButtonShellParent?.style, !localProps.isDeclineHidden ? { left: 10, top: null, right: null, bottom: null } : null);

            if (rsvpDeclineLink) {
                rsvpDeclineLink.textContent = localProps.declineText;
                rsvpDeclineLink.title = localProps.declineText;
            }

            // decline width
            if (localProps.declineWidth?.mode === "full") {
                setAttributePropertyValue(declineButtonShell, "width", "100%");
                setStylePropertyValue(declineButtonShell?.style, "width", "100%");
            }
            else if (localProps.declineWidth?.mode === "fixed") {
                setAttributePropertyValue(declineButtonShell, "width", localProps.declineWidth.fixedWidthPx); // no "px" in the attribute
                setStylePropertyValue(declineButtonShell?.style, "width", toPixelStringValueOrNull(localProps.declineWidth.fixedWidthPx));
            }
            else {
                // default and "fitToText"
                setAttributePropertyValue(declineButtonShell, "width", null);
                setStylePropertyValue(declineButtonShell?.style, "width", null);
            }

            setStylePropertyValue(declineButtonShell?.style, "background-color", localProps.declineBackgroundColor);
            setStylePropertyValue(rsvpDeclineLink?.style, "color", localProps.declineTextColor);

            // group occurrence
            if (rsvpGroupIdEl) {
                rsvpGroupIdEl.value = localProps.rsvpGroupGuid || "";
            }
            if (rsvpOccurrenceValueEl) {
                rsvpOccurrenceValueEl.value = localProps.rsvpOccurrenceValue || "";
            }

            // hrefs
            const commonHrefProps: Record<string, string> = {
                p: `{{ Person | PersonActionIdentifier:'RSVP' }}`,
                AcceptButtonText: localProps.acceptText,
                AcceptButtonColor: localProps.acceptBackgroundColor ?? "",
                AcceptButtonFontColor: localProps.acceptTextColor ?? "",
                DeclineButtonText: localProps.declineText,
                DeclineButtonColor: localProps.declineBackgroundColor ?? "",
                DeclineButtonFontColor: localProps.declineTextColor ?? "",
                AttendanceOccurrenceId: Enumerable.from((localProps.rsvpOccurrenceValue ?? "").split("|")).firstOrDefault("")
            };
            if (rsvpAcceptLink) {
                const queryString = new URLSearchParams({
                    ...commonHrefProps,
                    isAccept: "1"
                });
                rsvpAcceptLink.href = `{{ 'Global' | Attribute:'PublicApplicationRoot' }}RSVP?${queryString}`;
            }
            if (rsvpDeclineLink) {
                const queryString = new URLSearchParams({
                    ...commonHrefProps,
                    isAccept: "0"
                });
                rsvpDeclineLink.href = `{{ 'Global' | Attribute:'PublicApplicationRoot' }}RSVP?${queryString}`;
            }
        },

        migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
            const componentVersion = getComponentVersion(componentElement);
            if (componentVersion === LATEST_VERSION) {
                // No migration needed; already at latest version.
                return componentElement;
            }

            // Copy the local props to the new component element.
            const localProps = localPropReaders[componentVersion](componentElement);
            const newComponentElement = createEmptyComponentElement(emailDocument);
            adapter.writeLocalProps(newComponentElement, localProps);

            // Replace the old component element in the document.
            componentElement.replaceWith(newComponentElement);

            return newComponentElement;
        }
    };

    return adapter;
}

type RowComponentStructure = ComponentStructure & {
    readonly dropzone: HTMLElement | null;
};

export function getRowComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): RowComponentStructure | null;
    createComponentElement(): HTMLElement;
} {
    const latestVersion = "v17.3-alpha" as const;

    const helper = {
        createComponentElement(): HTMLElement {
            const componentElements = createComponent(
                "row",
                latestVersion,
                `<div class="dropzone"></div>`
            );
            return componentElements.marginWrapper.table;
        },

        getElements(componentElement: Element): RowComponentStructure | null {
            if (!componentElement.classList.contains("component-row")) {
                throw new Error(`Element is not a row component element: ${componentElement.outerHTML}`);
            }

            const wrappers = findComponentInnerWrappers(componentElement);

            if (!wrappers) {
                return null;
            }

            return {
                ...wrappers,

                get dropzone(): HTMLElement | null {
                    return (wrappers.marginWrapper.borderWrapper.paddingWrapper.td.querySelector(".dropzone") ?? null) as HTMLElement | null;
                },
            };
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-row")) {
                throw new Error(`Element is not a row component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            if (!versionNumber) {
                return true;
            }

            const comparison = compareComponentVersions(versionNumber, latestVersion);

            if (comparison < 0) {
                return true;
            }

            // Are there any child components that need migration?
            if (Enumerable
                .from(componentElement.querySelectorAll(".component"))
                .any(c => {
                    try {
                        const helper = getComponentHelper(getComponentTypeName(c));
                        return helper?.isMigrationRequired(c) ?? false;
                    }
                    catch (e) {
                        // Log error and continue
                        console.error(`Error checking migration for row component child: ${e}`);
                        return false;
                    }
                })
            ) {
                return true;
            }

            return false;
        },

        migrate(oldComponentElement: Element): Element {
            if (!oldComponentElement.classList.contains("component-row")) {
                throw new Error(`Element is not a row component element: ${oldComponentElement.outerHTML}`);
            }

            if (!this.isMigrationRequired(oldComponentElement)) {
                // The component is already at the latest version.
                return oldComponentElement;
            }

            // These are in order from oldest to newest; new migrations should be added at the end.
            const migrations = [
                // Always start by migrating child components.
                function migrateChildComponents(componentElement: Element): Element {
                    const childComponents = componentElement.querySelectorAll(".component");

                    childComponents.forEach(childComponent => {
                        const helper = getComponentHelper(getComponentTypeName(childComponent));
                        if (helper?.isMigrationRequired(childComponent)) {
                            const migratedChild = helper.migrate(childComponent);
                            if (migratedChild !== childComponent) {
                                childComponent.replaceWith(migratedChild);
                            }
                        }
                    });

                    return componentElement;
                },

                // Enable placeholder `<div class="component component-row"></div>`
                // to create a row component.
                function placeholderToLatest(componentElement: Element): Element {
                    const outerHTML = componentElement.outerHTML;
                    const innerHTML = componentElement.innerHTML;

                    const outerHTMLWithoutChildren = outerHTML.replace(innerHTML, "");

                    if (outerHTMLWithoutChildren === `<div class="component component-row"></div>`) {
                        const newComponent = helper.createComponentElement();

                        if (innerHTML.trim()) {
                            helper.getElements(newComponent)
                                ?.dropzone
                                ?.append(...componentElement.childNodes);
                        }

                        return newComponent;
                    }
                    else {
                        return componentElement;
                    }
                },

                // eslint-disable-next-line @typescript-eslint/naming-convention
                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const migrationVersionNumber = "v17.3-alpha";
                    const versionNumber = getComponentVersionNumber(componentElement);

                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, migrationVersionNumber) >= 0) {
                        return componentElement; // Already migrated
                    }

                    // Bump version.
                    setComponentVersionNumber(componentElement, migrationVersionNumber);

                    return componentElement;
                }
            ];

            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };

    return helper;
}

type SectionComponentTypeName = Extract<EditorComponentTypeName,
    "section"
    | "one-column-section"
    | "two-column-section"
    | "three-column-section"
    | "four-column-section"
    | "right-sidebar-section"
    | "left-sidebar-section">;

export function getSectionComponentHelper(): ComponentMigrationHelper & {
    getElements(componentElement: Element): ComponentStructure & { readonly rowWrapper: HTMLTableElement | null; } | null;
    createComponentElement(componentTypeName: SectionComponentTypeName): HTMLElement;
} {
    const latestVersion = "v17.3-alpha" as const;

    return {
        createComponentElement(componentTypeName: SectionComponentTypeName): HTMLElement {
            function getSectionColumns(componentTypeName: EditorComponentTypeName): string {
                const wrappers = createElementWrappers(`<div class="dropzone"></div>`);
                const wrapperHtml = wrappers.marginWrapper.table.outerHTML;
                switch (componentTypeName) {
                    case "right-sidebar-section":
                        return `<td class="section-column columns small-12 start large-8" valign="top" width="66.666666%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 last large-4" valign="top" width="33.333333%">${wrapperHtml}</td>`;
                    case "left-sidebar-section":
                        return `<td class="section-column columns small-12 start large-4" valign="top" width="33.333333%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 last large-8" valign="top" width="66.666666%">${wrapperHtml}</td>`;
                    case "two-column-section":
                        return `<td class="section-column columns small-12 start large-6" valign="top" width="50%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 last large-6" valign="top" width="50%">${wrapperHtml}</td>`;
                    case "three-column-section":
                        return `<td class="section-column columns small-12 start large-4" valign="top" width="33.333333%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 large-4" valign="top" width="33.333333%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 last large-4" valign="top" width="33.333333%">${wrapperHtml}</td>`;
                    case "four-column-section":
                        return `<td class="section-column columns small-12 start large-3" valign="top" width="25%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 large-3" valign="top" width="25%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 large-3" valign="top" width="25%">${wrapperHtml}</td>
                                <td class="section-column columns small-12 last large-3" valign="top" width="25%">${wrapperHtml}</td>`;
                    case "one-column-section":
                    default:
                        return `<td class="section-column columns small-12 start last large-12" valign="top" width="100%">${wrapperHtml}</td>`;
                }
            }

            const componentElements = createComponent(
                "section",
                latestVersion,
                `<table class="row section-row" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width: 100%;">
                    <tbody>
                        <tr>
                            ${getSectionColumns(componentTypeName)}
                        </tr>
                    </tbody>
                </table>`
            );
            return componentElements.marginWrapper.table;
        },

        getElements(componentElement: Element): ComponentStructure & { readonly rowWrapper: HTMLTableElement | null; } | null {
            if (!componentElement.classList.contains("component-section")) {
                throw new Error(`Element is not a section component element: ${componentElement.outerHTML}`);
            }

            const wrappers = findComponentInnerWrappers(componentElement);

            if (!wrappers) {
                return null;
            }

            return {
                ...wrappers,

                get rowWrapper(): HTMLTableElement | null {
                    return wrappers.marginWrapper.borderWrapper.paddingWrapper.td.querySelector("table.section-row") ?? null;
                }
            };
        },

        isMigrationRequired(componentElement: Element): boolean {
            if (!componentElement.classList.contains("component-section")) {
                throw new Error(`Element is not a section component element: ${componentElement.outerHTML}`);
            }

            const versionNumber = getComponentVersionNumber(componentElement);

            if (!versionNumber) {
                return true;
            }

            const comparison = compareComponentVersions(versionNumber, latestVersion);

            if (comparison < 0) {
                return true;
            }

            // Are there any child components that need migration?
            if (Enumerable
                .from(componentElement.querySelectorAll(".component"))
                .any(c => {
                    try {
                        const helper = getComponentHelper(getComponentTypeName(c));
                        return helper?.isMigrationRequired(c) ?? false;
                    }
                    catch (e) {
                        // Log error and continue
                        console.error(`Error checking migration for row component child: ${e}`);
                        return false;
                    }
                })
            ) {
                return true;
            }

            return false;
        },

        migrate(oldComponentElement: Element): Element {
            const componentVersion = getComponentVersionNumber(oldComponentElement);

            // Check if the component is already at the latest version.
            if (componentVersion && compareComponentVersions(componentVersion, latestVersion) === 0) {
                return oldComponentElement;
            }

            // These are in order from oldest to newest; new migrations should be added at the end.
            const migrations = [
                // Always start by migrating child components.
                function migrateChildComponents(componentElement: Element): Element {
                    const childComponents = componentElement.querySelectorAll(".component");

                    childComponents.forEach(childComponent => {
                        const helper = getComponentHelper(getComponentTypeName(childComponent));
                        if (helper?.isMigrationRequired(childComponent)) {
                            const migratedChild = helper.migrate(childComponent);
                            if (migratedChild !== childComponent) {
                                childComponent.replaceWith(migratedChild);
                            }
                        }
                    });

                    return componentElement;
                },

                function v0ToV2Alpha(componentElement: Element): Element {
                    if (getComponentVersionNumber(componentElement)) {
                        // The old component element didn't have a version number
                        // so if there is any version number at all, it's already migrated.
                        return componentElement;
                    }

                    // Create the new root table element.
                    const newRoot = document.createElement("table");
                    newRoot.setAttribute("border", "0");
                    newRoot.setAttribute("cellpadding", "0");
                    newRoot.setAttribute("cellspacing", "0");
                    newRoot.setAttribute("width", "100%");
                    newRoot.setAttribute("role", "presentation");
                    newRoot.classList.add("margin-wrapper", "component", "component-section");
                    newRoot.setAttribute("data-state", "component");

                    // Ensure version tracking.
                    setComponentVersionNumber(newRoot, "v2-alpha");

                    // Extract inline styles from the old component.
                    const componentElementStyle = (componentElement as HTMLElement).style;
                    const backgroundColor = componentElementStyle.backgroundColor || "";
                    const padding = componentElementStyle.padding || "";
                    const borderRadius = componentElementStyle.borderRadius || "";
                    const borderStyle = componentElementStyle.borderStyle || "";
                    const borderWidth = componentElementStyle.borderWidth || "";
                    const borderColor = componentElementStyle.borderColor || "";

                    // Create the inner structure.
                    newRoot.innerHTML = `
                        <tbody>
                            <tr><td>
                                <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="border-wrapper" style="border-collapse: separate !important;">
                                    <tbody><tr><td style="${backgroundColor ? `background-color: ${backgroundColor};` : ""}
                                                    ${borderRadius ? `border-radius: ${borderRadius};` : ""}
                                                    ${borderStyle ? `border-style: ${borderStyle};` : ""}
                                                    ${borderWidth ? `border-width: ${borderWidth};` : ""}
                                                    ${borderColor ? `border-color: ${borderColor};` : ""}">
                                        <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper">
                                            <tbody><tr><td ${padding ? `style="padding: ${padding};"` : ""}>
                                                <table class="row section-row" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width: 100%;">
                                                    <tbody><tr></tr></tbody>
                                                </table>
                                            </td></tr></tbody>
                                        </table>
                                    </td></tr></tbody>
                                </table>
                            </td></tr>
                        </tbody>
                    `;

                    // Find old columns (dropzones and spacers)
                    const oldColumns = Array.from(componentElement.querySelectorAll(".dropzone, .spacer"));
                    const nonSpacerColumns = oldColumns.filter(col => !col.classList.contains("spacer")).length;
                    const newColumnsRow = newRoot.querySelector(".section-row tbody tr") as HTMLElement;

                    // Calculate "large-[n]" values ensuring the sum is 12
                    const calculatedLarge = Math.floor(12 / nonSpacerColumns);
                    const remainder = 12 - (calculatedLarge * nonSpacerColumns);
                    const largeValues = new Array(nonSpacerColumns).fill(calculatedLarge);
                    for (let i = 0; i < remainder; i++) {
                        largeValues[i]++;
                    }

                    // Process each column
                    oldColumns.forEach((oldColumn, index) => {
                        const oldColumnElement = oldColumn as HTMLElement;

                        if (oldColumnElement.classList.contains("spacer")) {
                            // Copy spacers exactly
                            const spacer = document.createElement("td");
                            spacer.className = "spacer";
                            spacer.setAttribute("width", oldColumnElement.getAttribute("width") || "8px");
                            spacer.setAttribute("style", oldColumnElement.getAttribute("style") || "width: 8px; min-width: 8px; font-size: 0px; line-height: 0; padding: 0px;");
                            spacer.innerHTML = "&nbsp;";

                            // Append the spacer to the new row
                            newColumnsRow.appendChild(spacer);
                        }
                        else {
                            const width = oldColumnElement.getAttribute("width") || `${(100 / nonSpacerColumns).toFixed(3)}%`;
                            const existingLargeClass = Array.from(oldColumnElement.classList).find(cls => cls.startsWith("large-"));
                            const largeClass = existingLargeClass || `large-${largeValues[index]}`;

                            // Extract styles
                            const dropzoneBackgroundColor = oldColumnElement.style.backgroundColor || "";
                            const dropzoneBorderRadius = oldColumnElement.style.borderRadius || "";
                            const dropzoneBorderStyle = oldColumnElement.style.borderStyle || "";
                            const dropzoneBorderWidth = oldColumnElement.style.borderWidth || "";
                            const dropzoneBorderColor = oldColumnElement.style.borderColor || "";
                            const dropzonePadding = oldColumnElement.style.padding || "";
                            const textAlign = oldColumnElement.style.textAlign;
                            const alignAttribute = oldColumnElement.style.textAlign ? `align="${oldColumnElement.style.textAlign}"` : "";

                            // Create new column
                            const newColumn = document.createElement("td");
                            newColumn.className = `dropzone columns small-12 section-column ${largeClass}`;
                            newColumn.setAttribute("valign", "top");
                            newColumn.setAttribute("width", width);

                            // Construct column content
                            newColumn.innerHTML = `
    <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="margin-wrapper" align="center">
        <tbody><tr><td>
            <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="border-wrapper" style="border-collapse: separate !important;">
                <tbody><tr><td style="${dropzoneBackgroundColor ? `background-color: ${dropzoneBackgroundColor};` : ""}
                                ${dropzoneBorderRadius ? `border-radius: ${dropzoneBorderRadius};` : ""}
                                ${dropzoneBorderStyle ? `border-style: ${dropzoneBorderStyle};` : ""}
                                ${dropzoneBorderWidth ? `border-width: ${dropzoneBorderWidth};` : ""}
                                ${dropzoneBorderColor ? `border-color: ${dropzoneBorderColor};` : ""}">
                    <table border="0" cellpadding="0" cellspacing="0" width="100%" role="presentation" class="padding-wrapper">
                        <tbody><tr><td style="${dropzonePadding ? `padding: ${dropzonePadding};` : ""} ${textAlign ? `text-align: ${textAlign};` : ""}" ${alignAttribute}>
                            <div class="dropzone"></div>
                        </td></tr></tbody>
                    </table>
                </td></tr></tbody>
            </table>
        </td></tr></tbody>
    </table>
`;

                            // Create a new dropzone and move existing content into it
                            const newDropzone = newColumn.querySelector(".dropzone") as HTMLElement;
                            oldColumn.childNodes.forEach(node => newDropzone.appendChild(node.cloneNode(true)));

                            // Append the new column to the row
                            newColumnsRow.appendChild(newColumn);
                        }
                    });

                    return newRoot;
                },

                function v2AlphaToV17_3Alpha(componentElement: Element): Element {
                    const versionNumber = getComponentVersionNumber(componentElement);
                    if (!versionNumber) {
                        // This shouldn't occur unless the v0 migration was skipped or modified incorrectly.
                        throw new Error("Component version number is missing.");
                    }

                    if (compareComponentVersions(versionNumber, "v17.3-alpha") >= 0) {
                        return componentElement; // Already migrated
                    }

                    // Bump version.
                    setComponentVersionNumber(componentElement, "v17.3-alpha");

                    return componentElement;
                }
            ];

            // Run migrations.
            return migrations.reduce((component, migrate) => migrate(component), oldComponentElement);
        },

        get latestVersion(): string {
            return latestVersion;
        }
    };
}

function getMetaTag(emailDocument: Document, name: string): HTMLMetaElement | null {
    return emailDocument.head.querySelector(`meta[name="${name}"]`) as HTMLMetaElement | null;
}

function addOrUpdateMetaTag(emailDocument: Document, name: string, content: string): void {
    let metaTag = getMetaTag(emailDocument, name);

    if (!metaTag) {
        metaTag = emailDocument.createElement("meta");
        metaTag.setAttribute("name", name);
        emailDocument.head.appendChild(metaTag);
    }

    metaTag.setAttribute("content", content);
}

export function createBodyGlobalAdapter(): BodyGlobalAdapter {
    const globalVersions = ["v0", "v17.3-alpha", "v18.2"] as const;
    type BodyGlobalVersion = (typeof globalVersions)[number];
    const latestVersion = getLatestVersion<BodyGlobalVersion>(globalVersions);

    const attributeValues = {
        META_NAME_GLOBAL_BODY_VERSION: "x-rock-global-body-version"
    } as const;

    const defaultGlobalProps: BodyGlobalProps = {
        widthPx: 600,
        backgroundColor: "#ffffff",
        bodyAlignment: "center",
        border: null,
        marginPx: null,
        paddingPx: createShorthandModel(24)
    };

    function getGlobalVersion(emailDocument: Document): BodyGlobalVersion {
        // v18.2 and later use the meta tag to track global button schema version.
        const globalVersion = getMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_BODY_VERSION)?.getAttribute("content");

        if (globalVersion && globalVersions.includes(globalVersion as BodyGlobalVersion)) {
            // v18.2 and up use the meta tag for version tracking.
            return globalVersion as BodyGlobalVersion;
        }

        const hasAnyGlobalStylesFromV17_3_alpha =
            findRockStyleRules(emailDocument, ".border-wrapper-for-row").any(r => !!r.style.maxWidth);

        if (hasAnyGlobalStylesFromV17_3_alpha) {
            return "v17.3-alpha";
        }

        // No version info, assume v0 (legacy).
        return "v0";
    }

    // Don't modify a specific version once released.
    // Instead, add new versions using semver.
    // The major and minor versions should align with the current Rock version.
    // If necessary, you can add patch versions for fixes within the same release.
    // Avoid defining pre-release versions (like "v17.3-alpha") unless absolutely necessary.
    const adapters: Record<BodyGlobalVersion, GlobalAdapterSnapshot<BodyGlobalProps>> = {
        /*
            - Mirrors the WebForms version where there were no global properties.
         */
        "v0": {
            version: "v0",

            readGlobalProps(_emailDocument: Document): BodyGlobalProps {
                // v0 has no global props.
                return {
                    widthPx: null,
                    backgroundColor: null,
                    bodyAlignment: null,
                    border: null,
                    marginPx: null,
                    paddingPx: null
                };
            },

            writeGlobalProps(_emailDocument: Document, _globalProps: BodyGlobalProps): void {
                // v0 has no global props.
            }
        },

        /*
            - Obsidian version where global properties were introduced.
         */
        "v17.3-alpha": {
            version: "v17.3-alpha",

            readGlobalProps(emailDocument: Document): BodyGlobalProps {
                const borderWrapperForRowStyles = findRockStyleRules(emailDocument, ".border-wrapper-for-row")
                    .select(rule => rule.style)
                    .toArray();
                const backgroundColorStyles = findRockStyleRules(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-row>tbody>tr>td`)
                    .select(rule => rule.style)
                    .toArray();
                const borderCellStyles = findRockStyleRules(emailDocument, `.border-wrapper-for-row > tbody > tr > td`)
                    .select(rule => rule.style)
                    .toArray();
                const marginWrapperTdForRowStyles = findRockStyleRules(emailDocument, `.margin-wrapper-for-row > tbody > tr > td`)
                    .select(rule => rule.style)
                    .toArray();
                const paddingWrapperTdForRowStyles = findRockStyleRules(emailDocument, `.padding-wrapper-for-row > tbody > tr > td`)
                    .select(rule => rule.style)
                    .toArray();

                return {
                    widthPx: toPixelNumericValueOrNull(getStylePropertyValueOrNull(borderWrapperForRowStyles, "max-width")),
                    backgroundColor: getStylePropertyValueOrNull(backgroundColorStyles, "background-color"),
                    bodyAlignment: toHorizontalAlignmentOrNull(emailDocument.querySelector(`.margin-wrapper-for-row > tbody > tr > td`)?.getAttribute("align")),
                    border: getStyleBorder(borderCellStyles),
                    marginPx: getStylePaddingPx(marginWrapperTdForRowStyles),
                    paddingPx: getStylePaddingPx(paddingWrapperTdForRowStyles)
                };
            },

            writeGlobalProps(emailDocument: Document, globalProps: BodyGlobalProps): void {
                const borderWrapperForRowRule = findRockStyleRules(emailDocument, `.border-wrapper-for-row`)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.border-wrapper-for-row`);
                const borderWrapperForRowTdRule = findRockStyleRules(emailDocument, `.border-wrapper-for-row > tbody > tr > td`)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.border-wrapper-for-row > tbody > tr > td`);
                const paddingWrapperForRowTdWithoutBackgroundColorRule = findRockStyleRules(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-row>tbody>tr>td`)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-row>tbody>tr>td`);
                const marginWrapperForRowTdRule = findRockStyleRules(emailDocument, `.margin-wrapper-for-row > tbody > tr > td`)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.margin-wrapper-for-row > tbody > tr > td`);
                const paddingWrapperForRowTdRule = findRockStyleRules(emailDocument, `.padding-wrapper-for-row > tbody > tr > td`)?.lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.padding-wrapper-for-row > tbody > tr > td`);

                const updatedRules: CSSRule[] = [
                    borderWrapperForRowRule,
                    borderWrapperForRowTdRule,
                    paddingWrapperForRowTdWithoutBackgroundColorRule,
                    marginWrapperForRowTdRule,
                    paddingWrapperForRowTdRule
                ];

                setStyleBorder(borderWrapperForRowTdRule.style, globalProps.border);
                setStylePaddingPx(marginWrapperForRowTdRule.style, globalProps.marginPx);
                setStylePaddingPx(paddingWrapperForRowTdRule.style, globalProps.paddingPx);

                // horizontalAlignment
                emailDocument.querySelectorAll(marginWrapperForRowTdRule.selectorText).forEach(element => {
                    setAttributePropertyValue(element, "align", globalProps.bodyAlignment ?? defaultGlobalProps.bodyAlignment);
                });

                // bodyWidthPx
                const bodyWidthPxString = toPixelStringValueOrNull(globalProps.widthPx);
                setStylePropertyValue(borderWrapperForRowRule.style, "max-width", bodyWidthPxString);
                setStylePropertyValue(borderWrapperForRowRule.style, "width", !isNullish(globalProps.widthPx) ? "100%" : null);
                emailDocument.querySelectorAll(`.border-wrapper-for-row:not([data-component-body-width="true"])`) // For some reason this is on the border-wrapper instead of the top-level component.
                    .forEach(element => {
                        setAttributePropertyValue(element, "width", globalProps.widthPx?.toString());
                    });
                setStylePropertyValue(borderWrapperForRowTdRule.style, "max-width", bodyWidthPxString);

                // Whenever the body width is changed,
                // also update the responsive @media rules.
                const fallbackBodyWidthPxString = toPixelStringValueOrNull(defaultGlobalProps.widthPx) || "600px";
                findRockMediaStyleSheets(emailDocument).forEach(sheet => {
                    sheet.ownerNode?.remove();
                });
                const sheet = createRockMediaStyleSheet(emailDocument);
                const i = sheet.insertRule(`@media screen and (max-width: ${bodyWidthPxString || fallbackBodyWidthPxString}) {
    .email-wrapper {
        min-height: 100vh;
    }

    .small-12 {
        display: inline-block !important;
        width: 100% !important;
    }

    .small-11 {
        display: inline-block !important;
        width: 91.66667% !important;
    }

    .small-10 {
        display: inline-block !important;
        width: 83.33333% !important;
    }

    .small-9 {
        display: inline-block !important;
        width: 75% !important;
    }

    .small-8 {
        display: inline-block !important;
        width: 66.66667% !important;
    }

    .small-7 {
        display: inline-block !important;
        width: 58.33333% !important;
    }

    .small-6 {
        display: inline-block !important;
        width: 50% !important;
    }

    .small-5 {
        display: inline-block !important;
        width: 41.66667% !important;
    }

    .small-4 {
        display: inline-block !important;
        width: 33.33333% !important;
    }

    .small-3 {
        display: inline-block !important;
        width: 25% !important;
    }

    .small-2 {
        display: inline-block !important;
        width: 16.66667% !important;
    }

    .small-1 {
        display: inline-block !important;
        width: 8.33333% !important;
    }

    .spacer {
        display: none !important;
        width: 0 !important;
    }
}`, sheet.cssRules.length);

                // If the media rule was added successfully,
                // include it in the updated rules array so the DOM can be updated accordingly
                // in the following step.
                const mediaRule = sheet.cssRules.item(i);
                if (mediaRule) {
                    updatedRules.push(mediaRule);
                }

                // backgroundColor
                setStylePropertyValue(paddingWrapperForRowTdWithoutBackgroundColorRule.style, "background-color", globalProps.backgroundColor);
                let backgroundColor = globalProps.backgroundColor;

                // bgcolor doesn't allow hex values with alpha values.
                // If transparent, use the "transparent" named color instead.
                if (backgroundColor?.startsWith("#")
                    && (
                        (backgroundColor.length === 5 && backgroundColor.endsWith("0"))     // #RGBA
                        || (backgroundColor.length === 9 && backgroundColor.endsWith("00")) // #RRGGBBAA
                    )
                ) {
                    backgroundColor = "transparent";
                }
                emailDocument.querySelectorAll(`.component:not([data-component-background-color="true"]) .padding-wrapper-for-row`).forEach(element => {
                    setAttributePropertyValue(element, "bgcolor", backgroundColor);
                });

                // All the above work only updates the in-memory document;
                // now synchronize the sheet(s) to the DOM.
                synchronizeRulesToDom(updatedRules);
            }
        },

        /*
            - Added meta tag to track global body version.
         */
        "v18.2": {
            version: "v18.2",

            readGlobalProps(emailDocument: Document): BodyGlobalProps {
                return adapters["v17.3-alpha"].readGlobalProps(emailDocument);
            },

            writeGlobalProps(emailDocument: Document, globalProps: BodyGlobalProps): void {
                addOrUpdateMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_BODY_VERSION, "v18.2");

                adapters["v17.3-alpha"].writeGlobalProps(emailDocument, globalProps);
            }
        }
    };

    const latestAdapter = adapters[latestVersion];

    return {
        ...latestAdapter,

        areGlobalDefaultsNeeded(emailDocument: Document): boolean {
            return compareComponentVersions(getGlobalVersion(emailDocument), "v0") === 0;
        },

        getDefaultGlobalProps(): BodyGlobalProps {
            return defaultGlobalProps;
        },

        migrateGlobalProps(emailDocument: Document): void {
            const adapter = adapters[getGlobalVersion(emailDocument)];

            if (adapter === latestAdapter || compareComponentVersions(adapter.version, latestAdapter.version) >= 0) {
                // Already on this or later version.
                return;
            }

            // Clear old props first
            const globalProps = adapter.readGlobalProps(emailDocument);
            adapter.writeGlobalProps(emailDocument, {
                widthPx: null,
                backgroundColor: null,
                bodyAlignment: null,
                border: null,
                marginPx: null,
                paddingPx: null
            });

            latestAdapter.writeGlobalProps(emailDocument, globalProps);
        }
    };
}

export function createImageComponentAdapter(): ImageComponentAdapter {
    const componentVersions = ["v0", "v18-alpha", "v18.2"] as const;
    type ImageComponentVersion = (typeof componentVersions)[number];
    const latestVersion = getLatestVersion<ImageComponentVersion>(componentVersions);
    const placeholderImageSrc = "/Assets/Images/image-placeholder.jpg";

    const bodyGlobalAdapter = createBodyGlobalAdapter();

    const attributeNames = {
        // Asset keys (don't change the letter case of these values)
        dataImageWidth: "data-image-width",
        dataImageId: "data-image-id",
        dataImageHeight: "data-image-height",
        dataImageGuid: "data-image-guid",
        dataImageFileName: "data-image-filename",
        dataImgCssWidth: "data-imgcsswidth",
        dataImageResizeMode: "data-image-resizemode",
        dataImageHighRes: "data-image-high-res",
        dataImageSize: "data-image-size",

        // Asset keys (don't change the letter case of these values)
        dataImageAssetStorageProviderId: "data-image-assetstorageproviderid",
        dataImageKey: "data-image-key",
        dataImageIconpath: "data-image-iconpath",
        dataImageName: "data-image-name",
        dataImageUrl: "data-image-url"
    } as const;

    const adapters: Record<ImageComponentVersion, ImageComponentAdapter> = {
        /*
            Changes:
            - Initial version that mirrors the functionality of the WebForms version.
         */
        "v0": {
            version: "v0",

            createComponentElement(): HTMLElement {
                throw new Error("Cannot create image component with version 'v0'.");
            },

            migrateComponent(_emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                // This is the base version; no migration needed.
                return componentElement;
            },

            readLocalProps(componentElement: HTMLElement): ImageLocalProps {
                const image = componentElement.querySelector("img");
                const anchor = componentElement.querySelector("a");

                const imageWidth = toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageWidth));
                const imageHeight = toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageHeight));
                const imageResizeMode = componentElement.getAttribute(attributeNames.dataImageResizeMode) ?? "";

                const imageSize: ImageSizeModel =
                    image?.getAttribute(attributeNames.dataImgCssWidth) === "full"
                        ? { type: "responsive" }
                        : isNullish(imageWidth) && isNullish(imageHeight) ? { type: "original" }
                            : {
                                type: "fixed",
                                fixedWidthPx: imageWidth,
                                fixedHeightPx: imageHeight,
                                resizeMode: toResizeModeOrNull(imageResizeMode) ?? "crop"
                            };

                // Determine if image is an asset or file
                const assetStorageProviderId = toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageAssetStorageProviderId));
                const assetKey = componentElement.getAttribute(attributeNames.dataImageKey);
                const assetIconpath = componentElement.getAttribute(attributeNames.dataImageIconpath);
                const assetName = componentElement.getAttribute(attributeNames.dataImageName);
                const assetUri = componentElement.getAttribute(attributeNames.dataImageUrl);
                const isAsset = !isNullish(assetStorageProviderId)
                    && !isNullish(assetKey)
                    && !isNullish(assetIconpath)
                    && !isNullish(assetName)
                    && !isNullish(assetUri);
                const fileGuid = toGuidOrNull(componentElement.getAttribute(attributeNames.dataImageGuid));
                const fileName = componentElement.getAttribute(attributeNames.dataImageFileName);

                const imageSource: ImageSourceModel =
                    isAsset
                        ? {
                            type: "asset",
                            asset: {
                                assetStorageProviderId: assetStorageProviderId,
                                key: assetKey,
                                iconPath: assetIconpath,
                                name: assetName,
                                uri: assetUri,
                                description: "",
                                fileSize: 0,
                                formattedFileSize: "",
                                lastModifiedDateTime: "",
                                type: 0
                            }
                        }
                        : {
                            type: "file",
                            file: fileGuid
                                ? {
                                    value: fileGuid,
                                    text: fileName
                                }
                                : null,
                            isHighResolution: false // not supported in v0
                        };

                return {
                    altText: image?.getAttribute("alt") ?? "",
                    border: null, // not supported in v0
                    borderRadiusPx: null, // not supported in v0
                    horizontalAlignment: toHorizontalAlignmentOrNull(toTextAlignmentOrNull(getStylePropertyValueOrNull(componentElement.style, "text-align"))),
                    href: anchor?.getAttribute("href") || null,
                    imageSize,
                    imageSource,
                    marginPx: getStylePaddingPx(componentElement.style) // margin stored as padding
                };
            },

            writeLocalProps(_componentElement: HTMLElement, _localProps: ImageLocalProps): void {
                throw new Error("Cannot write local props to image component with version 'v0'.");
            }
        },

        // TODO Implement intermediate 17.3-alpha adapter that sets the scale attribute; update v18-alpha to handle the scale appropriately, if needed.

        /*
            Changes:
            - New image component structure for the Obsidian email editor.
         */
        "v18-alpha": {
            version: "v18-alpha",

            createComponentElement(emailDocument: Document): HTMLElement {
                // Important!
                // To ensure border-radius is applied to the border,
                //   the .border-wrapper-for-image <table> must have `border-collapse: separate`
                //   with `overflow: hidden` on its <td>.
                // To remove extra space under the image,
                //   the .padding-wrapper-for-image <table> needs `line-height: 0`
                const componentElement = createHtmlElement(emailDocument, `
<table class="component component-image margin-wrapper margin-wrapper-for-image"
       data-state="component"
       data-version="v18-alpha"
       border="0"
       cellpadding="0"
       cellspacing="0"
       width="100%"
       role="presentation">
    <tbody>
        <tr>
            <td>
                <table class="border-wrapper border-wrapper-for-image"
                       border="0"
                       cellpadding="0"
                       cellspacing="0"
                       role="presentation"
                       width="100%"
                       style="border-collapse: separate !important;">
                    <tbody>
                        <tr>
                            <td style="overflow: hidden;">
                                <table class="padding-wrapper padding-wrapper-for-image"
                                       border="0"
                                       cellpadding="0"
                                       cellspacing="0"
                                       role="presentation"
                                       width="100%">
                                    <tbody>
                                        <tr>
                                            <td style="line-height: 0;">
                                                <img alt="" src="/Assets/Images/image-placeholder.jpg" data-imgcsswidth="full" style="width: 100%; box-sizing: border-box;">
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>
`);

                // Apply default local props before returning the element.
                // If there are any global properties to set, prefer to set shared properties there rather than here,
                // otherwise each component instance will have to be updated manually in the UI when global defaults change.
                const adapter = adapters["v18.2"];
                const defaultLocalProps: ImageLocalProps = {
                    imageSource: {
                        type: "file",
                        file: null,
                        isHighResolution: false
                    },
                    altText: "",
                    href: null,
                    imageSize: { type: "responsive" },
                    horizontalAlignment: null,
                    borderRadiusPx: null,
                    border: null,
                    marginPx: null
                };
                adapter.writeLocalProps(componentElement, defaultLocalProps);

                return componentElement;
            },

            migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                const adapter = adapters[getComponentVersion<ImageComponentVersion>(componentElement, componentVersions)];
                const thisAdapter = adapters["v18-alpha"];

                if (adapter === thisAdapter || compareComponentVersions(adapter.version, thisAdapter.version) >= 0) {
                    // Already on this or later version.
                    return componentElement;
                }

                const localProps = adapter.readLocalProps(componentElement);
                const newComponentElement = thisAdapter.createComponentElement(emailDocument);
                thisAdapter.writeLocalProps(newComponentElement, localProps);
                componentElement.replaceWith(newComponentElement);

                return newComponentElement;
            },

            readLocalProps(componentElement: HTMLElement): ImageLocalProps {
                const imageElement = componentElement.querySelector("img") as HTMLImageElement | null;
                const anchorElement = componentElement.querySelector("a") as HTMLAnchorElement | null;
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;

                const imageSizeType = componentElement.getAttribute(attributeNames.dataImageResizeMode)
                    ? "fixed"
                    : getStylePropertyValueOrNull(imageElement?.style, "width") === "100%"
                        ? "responsive"
                        : (imageElement?.src && !imageElement.src.includes("width=") && !imageElement.src.includes("height="))
                            ? "original"
                            : "responsive"; // fallback to responsive if we can't determine size

                const imageSize: ImageSizeModel =
                    imageSizeType === "original" ? { type: "original" }
                        : imageSizeType === "fixed"
                            ? {
                                type: "fixed",
                                fixedWidthPx: toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageWidth)),
                                fixedHeightPx: toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageHeight)),
                                resizeMode: toResizeModeOrNull(componentElement.getAttribute(attributeNames.dataImageResizeMode)) ?? "crop"
                            }
                            : { type: "responsive" };

                // Determine if image is an asset or file
                const assetStorageProviderId = toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageAssetStorageProviderId));
                const assetKey = componentElement.getAttribute(attributeNames.dataImageKey);
                const assetIconpath = componentElement.getAttribute(attributeNames.dataImageIconpath);
                const assetName = componentElement.getAttribute(attributeNames.dataImageName);
                const assetUri = componentElement.getAttribute(attributeNames.dataImageUrl);
                const isAsset = !isNullish(assetStorageProviderId)
                    && !isNullish(assetKey)
                    && !isNullish(assetIconpath)
                    && !isNullish(assetName)
                    && !isNullish(assetUri);
                const fileGuid = toGuidOrNull(componentElement.getAttribute(attributeNames.dataImageGuid));
                const fileName = componentElement.getAttribute(attributeNames.dataImageFileName);

                const imageSource: ImageSourceModel =
                    isAsset
                        ? {
                            type: "asset",
                            asset: {
                                assetStorageProviderId: assetStorageProviderId,
                                key: assetKey,
                                iconPath: assetIconpath,
                                name: assetName,
                                uri: assetUri,
                                description: "",
                                fileSize: 0,
                                formattedFileSize: "",
                                lastModifiedDateTime: "",
                                type: 0
                            }
                        }
                        : {
                            type: "file",
                            file: fileGuid
                                ? {
                                    value: fileGuid,
                                    text: fileName
                                }
                                : null,
                            isHighResolution: componentElement.getAttribute(attributeNames.dataImageHighRes) === "true"
                        };

                return {
                    imageSource,
                    altText: imageElement?.getAttribute("alt") ?? "",
                    href: anchorElement?.getAttribute("href") || null,
                    imageSize,
                    horizontalAlignment: toHorizontalAlignmentOrNull(getStylePropertyValueOrNull(componentElement.style, "text-align")),
                    borderRadiusPx: getStyleBorderRadiusPx(imageElement?.style),
                    border: getStyleBorder(imageElement?.style),
                    marginPx: getStylePaddingPx(marginWrapperTd?.style)
                };
            },

            writeLocalProps(componentElement: HTMLElement, localProps: ImageLocalProps): void {
                const emailDocument = componentElement.ownerDocument;
                const imageElement = componentElement.querySelector("img") as HTMLImageElement | null;
                let anchorElement = componentElement.querySelector("a") as HTMLAnchorElement | null;
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;
                const borderWrapperTd = componentElement.querySelector(".border-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;
                const paddingWrapperTd = componentElement.querySelector(".padding-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;

                // imageSource
                if (localProps.imageSource.type === "asset") {
                    // Set asset attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageAssetStorageProviderId, localProps.imageSource.asset?.assetStorageProviderId);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageKey, localProps.imageSource.asset?.key);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageIconpath, localProps.imageSource.asset?.iconPath);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageName, localProps.imageSource.asset?.name);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageUrl, localProps.imageSource.asset?.uri);

                    // Clear file attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageGuid, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageFileName, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageHighRes, null);

                    // Set shared attributes.
                    // Ensure imgCssWidth marker when a source exists.
                    setAttributePropertyValue(componentElement, attributeNames.dataImgCssWidth, localProps.imageSource.asset ? "image" : null);

                    // Show the asset image or the placeholder.
                    if (localProps.imageSource.asset?.uri) {
                        setAttributePropertyValue(imageElement, "src", localProps.imageSource.asset.uri);
                    }
                    else {
                        setAttributePropertyValue(imageElement, "src", placeholderImageSrc);
                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "height", null);
                        setAttributePropertyValue(imageElement, "width", null);
                    }
                }
                else if (localProps.imageSource.type === "file") {
                    // Clear asset attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageAssetStorageProviderId, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageKey, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageIconpath, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageName, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageUrl, null);

                    // Set file attributes.
                    const fileGuid = toGuidOrNull(localProps.imageSource.file?.value);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageGuid, fileGuid);
                    const fileName = fileGuid ? localProps.imageSource.file?.text : null;
                    setAttributePropertyValue(componentElement, attributeNames.dataImageFileName, fileName);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageHighRes, localProps.imageSource.isHighResolution ? "true" : null);

                    // Set shared attributes.
                    // Ensure imgCssWidth marker when a source exists.
                    setAttributePropertyValue(componentElement, attributeNames.dataImgCssWidth, fileGuid ? "image" : null);

                    // Show the file image or the placeholder.
                    if (fileGuid) {
                        // Build the file image URL with parameters.
                        const params = new URLSearchParams();
                        params.set("isBinaryFile", "T");
                        params.set("guid", fileGuid);
                        params.set("fileName", fileName ?? "");

                        const resolutionFactor = localProps.imageSource.isHighResolution ? 2 : 1;

                        if (localProps.imageSize.type === "fixed") {
                            if (localProps.imageSize.fixedWidthPx) {
                                params.set("width", `${localProps.imageSize.fixedWidthPx * resolutionFactor}`);
                            }

                            if (localProps.imageSize.fixedHeightPx) {
                                params.set("height", `${localProps.imageSize.fixedHeightPx * resolutionFactor}`);
                            }

                            params.set("mode", localProps.imageSize.resizeMode);
                            params.set("scale", "both");
                        }
                        else if (localProps.imageSize.type === "responsive") {
                            const rowSpecificBodyWidth = toNumberOrNull(componentElement.closest("[data-component-body-width='true']")?.getAttribute("width"));
                            const bodyWidth = rowSpecificBodyWidth ?? bodyGlobalAdapter.readGlobalProps(emailDocument).widthPx ?? 0;

                            if (bodyWidth) {
                                params.set("width", `${bodyWidth * resolutionFactor}`);
                            }
                        }

                        setAttributePropertyValue(imageElement, "src", `/GetImage.ashx?${params.toString()}`);
                    }
                    else {
                        setAttributePropertyValue(imageElement, "src", placeholderImageSrc);
                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "height", null);
                        setAttributePropertyValue(imageElement, "width", null);
                    }
                }

                // altText
                setAttributePropertyValue(imageElement, "alt", localProps.altText);

                // href - adds/removes anchor element as needed
                if (localProps.href) {
                    if (!anchorElement) {
                        anchorElement = emailDocument.createElement("a");

                        if (imageElement) {
                            imageElement.replaceWith(anchorElement);
                            anchorElement.appendChild(imageElement);
                        }
                    }

                    anchorElement.setAttribute("href", localProps.href);
                }
                else if (anchorElement) {
                    if (imageElement) {
                        anchorElement.replaceWith(imageElement);
                    }
                    else {
                        anchorElement.remove();
                    }

                    anchorElement = null;
                }

                // imageSize
                switch (localProps.imageSize.type) {
                    case "responsive":
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "responsive");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, null);

                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "width", null);
                        setStylePropertyValue(imageElement?.style, "height", null);
                        setAttributePropertyValue(imageElement, "height", null);
                        setStylePropertyValue(imageElement?.style, "object-fit", null);
                        break;
                    case "original":
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "original");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, null);

                        setStylePropertyValue(imageElement?.style, "width", null);
                        setAttributePropertyValue(imageElement, "width", null);
                        setStylePropertyValue(imageElement?.style, "height", null);
                        setAttributePropertyValue(imageElement, "height", null);
                        setStylePropertyValue(imageElement?.style, "object-fit", null);
                        break;
                    case "fixed":
                        // Handle metadata first
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "fixed");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, localProps.imageSize.resizeMode);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, localProps.imageSize.fixedWidthPx?.toString());
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, localProps.imageSize.fixedHeightPx?.toString());

                        setStylePropertyValue(imageElement?.style, "width", toPixelStringValueOrNull(localProps.imageSize.fixedWidthPx));
                        setAttributePropertyValue(imageElement, "width", localProps.imageSize.fixedWidthPx?.toString());
                        setStylePropertyValue(imageElement?.style, "height", toPixelStringValueOrNull(localProps.imageSize.fixedHeightPx));
                        setAttributePropertyValue(imageElement, "height", localProps.imageSize.fixedHeightPx?.toString());

                        if (localProps.imageSource.type === "asset") {
                            const resizeMode = localProps.imageSize.resizeMode;
                            const objectFit =
                                resizeMode === "pad" ? "contain"
                                    : resizeMode === "stretch" ? "fill"
                                        : "cover";
                            setStylePropertyValue(imageElement?.style, "object-fit", objectFit);
                        }
                        else {
                            setStylePropertyValue(imageElement?.style, "object-fit", null);
                        }

                        break;
                }

                // horizontalAlignment
                setStylePropertyValue(componentElement.style, "text-align", toTextAlignmentOrNull(localProps.horizontalAlignment));
                setAttributePropertyValue(paddingWrapperTd, "align", localProps.horizontalAlignment);

                // borderRadiusPx
                setStyleBorderRadiusPx(imageElement?.style, localProps.borderRadiusPx);
                setStyleBorderRadiusPx(borderWrapperTd?.style, localProps.borderRadiusPx);

                // border
                setStyleBorder(imageElement?.style, localProps.border);

                // marginPx
                setStylePaddingPx(marginWrapperTd?.style, localProps.marginPx); // Set padding on the margin wrapper td
            }
        },

        /*
            Changes:
            - Fixed an issue where responsive images would expand the width of columns beyond the body width.
        */
        "v18.2": {
            version: "v18.2",

            createComponentElement(emailDocument: Document): HTMLElement {
                // Important!
                // To ensure border-radius is applied to the border,
                //   the .border-wrapper-for-image <table> must have `border-collapse: separate`
                //   with `overflow: hidden` on its <td>.
                // To remove extra space under the image,
                //   the .padding-wrapper-for-image <table> needs `line-height: 0`
                const componentElement = createHtmlElement(emailDocument, `
<table class="component component-image margin-wrapper margin-wrapper-for-image"
       data-state="component"
       data-version="v18.2"
       border="0"
       cellpadding="0"
       cellspacing="0"
       role="presentation"
       width="100%">
    <tbody>
        <tr>
            <td width="100%">
                <table class="border-wrapper border-wrapper-for-image"
                       border="0"
                       cellpadding="0"
                       cellspacing="0"
                       role="presentation"
                       width="100%"
                       style="border-collapse: separate !important;">
                    <tbody>
                        <tr>
                            <td width="100%"
                                style="overflow: hidden;">
                                <table class="padding-wrapper padding-wrapper-for-image"
                                       border="0"
                                       cellpadding="0"
                                       cellspacing="0"
                                       role="presentation"
                                       width="100%">
                                    <tbody>
                                        <tr>
                                            <td width="100%"
                                                style="line-height: 0;">
                                                <img style="box-sizing: border-box;" />
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>
`);

                // Apply default local props before returning the element.
                // If there are any global properties to set, prefer to set shared properties there rather than here,
                // otherwise each component instance will have to be updated manually in the UI when global defaults change.
                const adapter = adapters["v18.2"];
                const defaultLocalProps: ImageLocalProps = {
                    imageSource: {
                        type: "file",
                        file: null,
                        isHighResolution: false
                    },
                    altText: "",
                    href: null,
                    imageSize: { type: "responsive" },
                    horizontalAlignment: null,
                    borderRadiusPx: null,
                    border: null,
                    marginPx: null
                };
                adapter.writeLocalProps(componentElement, defaultLocalProps);

                return componentElement;
            },

            migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
                const adapter = adapters[getComponentVersion<ImageComponentVersion>(componentElement, componentVersions)];
                const thisAdapter = adapters["v18.2"];

                if (adapter === thisAdapter || compareComponentVersions(adapter.version, thisAdapter.version) >= 0) {
                    // Already on this or later version.
                    return componentElement;
                }

                const localProps = adapter.readLocalProps(componentElement);
                const newComponentElement = thisAdapter.createComponentElement(emailDocument);
                thisAdapter.writeLocalProps(newComponentElement, localProps);
                componentElement.replaceWith(newComponentElement);

                return newComponentElement;
            },

            readLocalProps(componentElement: HTMLElement): ImageLocalProps {
                const imageElement = componentElement.querySelector("img") as HTMLImageElement | null;
                const anchorElement = componentElement.querySelector("a") as HTMLAnchorElement | null;
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;

                const imageSizeType = componentElement.getAttribute(attributeNames.dataImageResizeMode)
                    ? "fixed"
                    : getStylePropertyValueOrNull(imageElement?.style, "width") === "100%"
                        ? "responsive"
                        : (imageElement?.src && !imageElement.src.includes("width=") && !imageElement.src.includes("height="))
                            ? "original"
                            : "responsive"; // fallback to responsive if we can't determine size

                const imageSize: ImageSizeModel =
                    imageSizeType === "original" ? { type: "original" }
                        : imageSizeType === "fixed"
                            ? {
                                type: "fixed",
                                fixedWidthPx: toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageWidth)),
                                fixedHeightPx: toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageHeight)),
                                resizeMode: toResizeModeOrNull(componentElement.getAttribute(attributeNames.dataImageResizeMode)) ?? "crop"
                            }
                            : { type: "responsive" };

                // Determine if image is an asset or file
                const assetStorageProviderId = toNumberOrNull(componentElement.getAttribute(attributeNames.dataImageAssetStorageProviderId));
                const assetKey = componentElement.getAttribute(attributeNames.dataImageKey);
                const assetIconpath = componentElement.getAttribute(attributeNames.dataImageIconpath);
                const assetName = componentElement.getAttribute(attributeNames.dataImageName);
                const assetUri = componentElement.getAttribute(attributeNames.dataImageUrl);
                const isAsset = !isNullish(assetStorageProviderId)
                    && !isNullish(assetKey)
                    && !isNullish(assetIconpath)
                    && !isNullish(assetName)
                    && !isNullish(assetUri);
                const fileGuid = toGuidOrNull(componentElement.getAttribute(attributeNames.dataImageGuid));
                const fileName = componentElement.getAttribute(attributeNames.dataImageFileName);

                const imageSource: ImageSourceModel =
                    isAsset
                        ? {
                            type: "asset",
                            asset: {
                                assetStorageProviderId: assetStorageProviderId,
                                key: assetKey,
                                iconPath: assetIconpath,
                                name: assetName,
                                uri: assetUri,
                                description: "",
                                fileSize: 0,
                                formattedFileSize: "",
                                lastModifiedDateTime: "",
                                type: 0
                            }
                        }
                        : {
                            type: "file",
                            file: fileGuid
                                ? {
                                    value: fileGuid,
                                    text: fileName
                                }
                                : null,
                            isHighResolution: componentElement.getAttribute(attributeNames.dataImageHighRes) === "true"
                        };

                return {
                    imageSource,
                    altText: imageElement?.getAttribute("alt") ?? "",
                    href: anchorElement?.getAttribute("href") || null,
                    imageSize,
                    horizontalAlignment: toHorizontalAlignmentOrNull(getStylePropertyValueOrNull(componentElement.style, "text-align")),
                    borderRadiusPx: getStyleBorderRadiusPx(imageElement?.style),
                    border: getStyleBorder(imageElement?.style),
                    marginPx: getStylePaddingPx(marginWrapperTd?.style)
                };
            },

            writeLocalProps(componentElement: HTMLElement, localProps: ImageLocalProps): void {
                const emailDocument = componentElement.ownerDocument;
                const imageElement = componentElement.querySelector("img") as HTMLImageElement | null;
                let anchorElement = componentElement.querySelector("a") as HTMLAnchorElement | null;
                const marginWrapperTd = componentElement.querySelector(".margin-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;
                const borderWrapperTd = componentElement.querySelector(".border-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;
                const paddingWrapperTd = componentElement.querySelector(".padding-wrapper-for-image > tbody > tr > td") as HTMLTableCellElement | null;

                // imageSource
                if (localProps.imageSource.type === "asset") {
                    // Set asset attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageAssetStorageProviderId, localProps.imageSource.asset?.assetStorageProviderId);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageKey, localProps.imageSource.asset?.key);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageIconpath, localProps.imageSource.asset?.iconPath);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageName, localProps.imageSource.asset?.name);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageUrl, localProps.imageSource.asset?.uri);

                    // Clear file attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageGuid, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageFileName, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageHighRes, null);

                    // Set shared attributes.
                    // Ensure imgCssWidth marker when a source exists.
                    setAttributePropertyValue(componentElement, attributeNames.dataImgCssWidth, localProps.imageSource.asset ? "image" : null);

                    // Show the asset image or the placeholder.
                    if (localProps.imageSource.asset?.uri) {
                        setAttributePropertyValue(imageElement, "src", localProps.imageSource.asset.uri);
                    }
                    else {
                        setAttributePropertyValue(imageElement, "src", placeholderImageSrc);
                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "height", null);
                        setAttributePropertyValue(imageElement, "width", null);
                    }
                }
                else if (localProps.imageSource.type === "file") {
                    // Clear asset attributes.
                    setAttributePropertyValue(componentElement, attributeNames.dataImageAssetStorageProviderId, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageKey, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageIconpath, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageName, null);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageUrl, null);

                    // Set file attributes.
                    const fileGuid = toGuidOrNull(localProps.imageSource.file?.value);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageGuid, fileGuid);
                    const fileName = fileGuid ? localProps.imageSource.file?.text : null;
                    setAttributePropertyValue(componentElement, attributeNames.dataImageFileName, fileName);
                    setAttributePropertyValue(componentElement, attributeNames.dataImageHighRes, localProps.imageSource.isHighResolution ? "true" : null);

                    // Set shared attributes.
                    // Ensure imgCssWidth marker when a source exists.
                    setAttributePropertyValue(componentElement, attributeNames.dataImgCssWidth, fileGuid ? "image" : null);

                    // Show the file image or the placeholder.
                    if (fileGuid) {
                        // Build the file image URL with parameters.
                        const params = new URLSearchParams();
                        params.set("isBinaryFile", "T");
                        params.set("guid", fileGuid);
                        params.set("fileName", fileName ?? "");

                        const resolutionFactor = localProps.imageSource.isHighResolution ? 2 : 1;

                        if (localProps.imageSize.type === "fixed") {
                            if (localProps.imageSize.fixedWidthPx) {
                                params.set("width", `${localProps.imageSize.fixedWidthPx * resolutionFactor}`);
                            }

                            if (localProps.imageSize.fixedHeightPx) {
                                params.set("height", `${localProps.imageSize.fixedHeightPx * resolutionFactor}`);
                            }

                            params.set("mode", localProps.imageSize.resizeMode);
                            params.set("scale", "both");
                        }
                        else if (localProps.imageSize.type === "responsive") {
                            const rowSpecificBodyWidth = toNumberOrNull(componentElement.closest("[data-component-body-width='true']")?.getAttribute("width"));
                            const bodyWidth = rowSpecificBodyWidth ?? bodyGlobalAdapter.readGlobalProps(emailDocument).widthPx ?? 0;

                            if (bodyWidth) {
                                params.set("width", `${bodyWidth * resolutionFactor}`);
                            }
                        }

                        setAttributePropertyValue(imageElement, "src", `/GetImage.ashx?${params.toString()}`);
                    }
                    else {
                        setAttributePropertyValue(imageElement, "src", placeholderImageSrc);
                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "height", null);
                        setAttributePropertyValue(imageElement, "width", null);
                    }
                }

                // altText
                setAttributePropertyValue(imageElement, "alt", localProps.altText);

                // href - adds/removes anchor element as needed
                if (localProps.href) {
                    if (!anchorElement) {
                        anchorElement = emailDocument.createElement("a");

                        if (imageElement) {
                            imageElement.replaceWith(anchorElement);
                            anchorElement.appendChild(imageElement);
                        }
                    }

                    anchorElement.setAttribute("href", localProps.href);
                }
                else if (anchorElement) {
                    if (imageElement) {
                        anchorElement.replaceWith(imageElement);
                    }
                    else {
                        anchorElement.remove();
                    }

                    anchorElement = null;
                }

                // imageSize
                switch (localProps.imageSize.type) {
                    case "responsive":
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "responsive");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, null);

                        setStylePropertyValue(imageElement?.style, "width", "100%");
                        setAttributePropertyValue(imageElement, "width", "100%");
                        setStylePropertyValue(imageElement?.style, "height", null);
                        setAttributePropertyValue(imageElement, "height", null);
                        setStylePropertyValue(imageElement?.style, "object-fit", null);
                        break;
                    case "original":
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "original");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, null);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, null);

                        setStylePropertyValue(imageElement?.style, "width", null);
                        setAttributePropertyValue(imageElement, "width", null);
                        setStylePropertyValue(imageElement?.style, "height", null);
                        setAttributePropertyValue(imageElement, "height", null);
                        setStylePropertyValue(imageElement?.style, "object-fit", null);
                        break;
                    case "fixed":
                        // Handle metadata first
                        setAttributePropertyValue(componentElement, attributeNames.dataImageSize, "fixed");
                        setAttributePropertyValue(componentElement, attributeNames.dataImageResizeMode, localProps.imageSize.resizeMode);
                        setAttributePropertyValue(componentElement, attributeNames.dataImageWidth, localProps.imageSize.fixedWidthPx?.toString());
                        setAttributePropertyValue(componentElement, attributeNames.dataImageHeight, localProps.imageSize.fixedHeightPx?.toString());

                        setStylePropertyValue(imageElement?.style, "width", toPixelStringValueOrNull(localProps.imageSize.fixedWidthPx));
                        setAttributePropertyValue(imageElement, "width", localProps.imageSize.fixedWidthPx?.toString());
                        setStylePropertyValue(imageElement?.style, "height", toPixelStringValueOrNull(localProps.imageSize.fixedHeightPx));
                        setAttributePropertyValue(imageElement, "height", localProps.imageSize.fixedHeightPx?.toString());

                        if (localProps.imageSource.type === "asset") {
                            const resizeMode = localProps.imageSize.resizeMode;
                            const objectFit =
                                resizeMode === "pad" ? "contain"
                                    : resizeMode === "stretch" ? "fill"
                                        : "cover";
                            setStylePropertyValue(imageElement?.style, "object-fit", objectFit);
                        }
                        else {
                            setStylePropertyValue(imageElement?.style, "object-fit", null);
                        }

                        break;
                }

                // horizontalAlignment
                setStylePropertyValue(componentElement.style, "text-align", toTextAlignmentOrNull(localProps.horizontalAlignment));
                setAttributePropertyValue(paddingWrapperTd, "align", localProps.horizontalAlignment);

                // borderRadiusPx
                setStyleBorderRadiusPx(imageElement?.style, localProps.borderRadiusPx);
                setStyleBorderRadiusPx(borderWrapperTd?.style, localProps.borderRadiusPx);

                // border
                setStyleBorder(imageElement?.style, localProps.border);

                // marginPx
                setStylePaddingPx(marginWrapperTd?.style, localProps.marginPx); // Set padding on the margin wrapper td
            }
        }
    };

    return adapters[latestVersion];
}

// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
export function getComponentHelper(componentTypeName: ComponentTypeName) {
    switch (componentTypeName) {
        case "title":
            return getTitleComponentHelper();
        case "text":
            return getTextComponentHelper();
        case "video":
            return getVideoComponentHelper();
        case "row":
            return getRowComponentHelper();
        case "code":
            return getCodeComponentHelper();
        case "section":
            return getSectionComponentHelper();
        case "button":
        case "rsvp":
        case "divider":
        case "image":
            // These components have their own adapters created elsewhere.
            return null;
        default:
            console.error(`Unknown component type: ${componentTypeName}`);
            return null;
    }
}

// #endregion Components

export const FontFamilies = {
    "Arial": "Arial, Helvetica, sans-serif",
    "Courier New": '"Courier New", Courier, monospace',
    "Georgia": "Georgia, serif",
    "Tahoma": "Tahoma, Geneva, sans-serif",
    "Times New Roman": '"Times New Roman", Times, serif',
    "Trebuchet MS": '"Trebuchet MS", Helvetica, sans-serif',
    "Verdana": "Verdana, Geneva, sans-serif",
    "Lucida": '"Lucida Sans Unicode", "Lucida Grande", sans-serif',
    "Helvetica Neue": '"Helvetica Neue", Helvetica, Arial, sans-serif'
} as const;

export const LineHeights = {
    tight: { value: "1.2", title: "Tight" },
    normal: { value: "1.5", title: "Normal" },
    loose: { value: "1.8", title: "Loose" },
    veryLoose: { value: "2", title: "Very Loose" }
} as const;

function createHtmlElement(document: Document, html: string): HTMLElement {
    const template = document.createElement("template");
    template.innerHTML = html.trim();
    const element = template.content.firstElementChild;

    if (!isHTMLElement(element)) {
        throw new Error(`Failed to create element from HTML string:\n${html}`);
    }

    return element;
}

/**
 * Value provider used by ButtonWidthProperty controls for both local
 * and global scopes. It abstracts get and set of the logical width model.
 */
export type ButtonWidthValueProvider = {
    /**
     * Reads the current button width model from either local or global scope.
     */
    get: () => ButtonWidthModel;

    /**
     * Writes a new button width model to either local or global scope.
     */
    set: (value: ButtonWidthModel) => void;
};

function createShorthandModel<T>(value: T): ShorthandModel<T> {
    return {
        top: value,
        right: value,
        bottom: value,
        left: value
    };
}

/**
 * Factory returning a self contained ButtonComponentAdapter.
 */
export function createButtonComponentAdapter(): ButtonComponentAdapter {
    // These are the supported versions for local props
    // and are used to look up the appropriate reader/writer functions
    // so that components can handle different versions correctly.
    // Must be in the format "v{major}.{minor}-{tag}" for proper comparison.
    const componentVersions = ["v0", "v2.1-alpha", "v17.3-alpha", "v18.2"] as const;

    type ComponentVersion = typeof componentVersions[number];

    const LATEST_VERSION: ComponentVersion = getLatestVersion(componentVersions);

    const datasetKeys = {
        VERSION: "data-version",
        COMPONENT_BUTTON_WIDTH: "data-component-button-width",
        COMPONENT_BACKGROUND_COLOR: "data-component-background-color",
    } as const;

    // These are for reading local component properties from different versions.
    const localPropReaders: Record<ComponentVersion, (componentElement: HTMLElement) => ButtonLocalProps> = {
        "v0": (componentElement: HTMLElement): ButtonLocalProps => {
            const buttonShell = componentElement.querySelector(".button-shell") as HTMLElement | null;
            const buttonContent = componentElement.querySelector(".button-content") as HTMLElement | null;
            const buttonLink = componentElement.querySelector("a.button-link") as HTMLElement | null;
            const innerwrap = componentElement.querySelector(".button-innerwrap") as HTMLElement | null;

            const attrWidth = buttonShell?.getAttribute("width") || "";
            const fixedWidthPx = toPixelNumericValueOrNull(buttonShell?.style.width || buttonLink?.style.width);
            const isFullWidth = attrWidth === "100%" || buttonShell?.style.width === "100%";
            const isFixedWidth = !isNullish(fixedWidthPx);

            return {
                backgroundColor: buttonLink?.style.backgroundColor || null,
                // Old buttons always had a 1px solid border the same color as the button background
                border: buttonLink?.style.backgroundColor
                    ? {
                        style: createShorthandModel<BorderStyle>("solid"),
                        color: createShorthandModel(buttonLink.style.backgroundColor),
                        widthPx: createShorthandModel(1)
                    }
                    : null,
                borderRadiusPx: getStyleBorderRadiusPx(buttonContent?.style),
                fontFamily: buttonLink?.style.fontFamily || null,
                fontSizePx: getStyleFontSizePx(buttonLink?.style),
                horizontalAlignment: toHorizontalAlignmentOrNull(innerwrap?.getAttribute("align")) ?? "center",
                href: buttonLink?.getAttribute("href") ?? "",
                isBold: null, // no bold in this version
                isItalicized: null, // no italic in this version
                isUnderlined: null, // no underline in this version
                letterCase: null, // no letter case in this version
                lineHeight: null, // no line height in this version
                marginPx: createShorthandModel(0),
                paddingPx: getStylePaddingPx(buttonLink?.style),
                text: buttonLink?.textContent?.trim() ?? "",
                textColor: buttonLink?.style.color || null,
                width: isFullWidth
                    ? { mode: "full", fixedWidthPx: null }
                    : isFixedWidth
                        ? { mode: "fixed", fixedWidthPx: fixedWidthPx }
                        : null // default to null so that later logic can apply global default
            };
        },
        "v2.1-alpha": (componentElement: HTMLElement): ButtonLocalProps => {
            const buttonLink = componentElement.querySelector(".button-link") as HTMLElement | null;
            const buttonContent = componentElement.querySelector(".button-content") as HTMLElement | null;
            const buttonShell = componentElement.querySelector(".button-shell") as HTMLElement | null;
            const buttonInnerwrap = componentElement.querySelector(".button-innerwrap") as HTMLElement | null;
            const paddingWrapperTd = componentElement.querySelector(".padding-wrapper-for-button > tbody > tr > td") as HTMLElement | null;

            const attrWidth = buttonShell?.getAttribute("width") || "";
            const fixedWidthPx = toPixelNumericValueOrNull(buttonShell?.style.width || buttonLink?.style.width);
            const isFullWidth = attrWidth === "100%" || buttonShell?.style.width === "100%";
            const isFixedWidth = !isNullish(fixedWidthPx);

            return {
                text: buttonLink?.textContent?.trim() ?? "",
                href: buttonLink?.getAttribute("href") ?? "",
                fontFamily: buttonLink?.style.fontFamily || null,
                fontSizePx: getStyleFontSizePx(buttonLink?.style),
                isBold: getStyleIsBold(buttonLink?.style),
                isUnderlined: getStyleIsUnderlined(buttonLink?.style),
                isItalicized: getStyleIsItalicized(buttonLink?.style),
                letterCase: getStyleLetterCase(buttonLink?.style),
                lineHeight: toNumberOrNull(buttonLink?.style.lineHeight),
                textColor: buttonLink?.style.color || null,
                horizontalAlignment: toHorizontalAlignmentOrNull(buttonInnerwrap?.getAttribute("align")),
                backgroundColor: paddingWrapperTd?.style.backgroundColor || null,
                borderRadiusPx: getStyleBorderRadiusPx(buttonContent?.style),
                width: isFullWidth
                    ? { mode: "full", fixedWidthPx: null }
                    : isFixedWidth
                        ? { mode: "fixed", fixedWidthPx: fixedWidthPx }
                        : null,
                marginPx: getStylePaddingPx(buttonInnerwrap?.style),
                paddingPx: getStylePaddingPx(buttonLink?.style),
                border: getStyleBorder(buttonLink?.style)
            };
        },
        // Nothing changed between v2.1-alpha and v17.3-alpha except for a version bump.
        "v17.3-alpha": (componentElement: HTMLElement) => localPropReaders["v2.1-alpha"](componentElement),
        "v18.2": (componentElement: HTMLElement): ButtonLocalProps => {
            const buttonInnerwrap = componentElement.querySelector(".button-innerwrap") as HTMLElement | null;
            const buttonLink = componentElement.querySelector(".button-link") as HTMLElement | null;

            let buttonWidthModel: ButtonWidthModel | null = null;

            const widthMode = (componentElement.getAttribute(datasetKeys.COMPONENT_BUTTON_WIDTH) || null) as (ButtonWidthMode | null);
            if (widthMode) {
                const fixedWidthPx = toPixelNumericValueOrNull(buttonLink?.style.width);
                buttonWidthModel = {
                    mode: widthMode,
                    fixedWidthPx
                };
            }

            return {
                text: buttonLink?.textContent ?? "",
                href: buttonLink?.getAttribute("href") || "",
                fontFamily: buttonLink?.style.fontFamily || null,
                fontSizePx: getStyleFontSizePx(buttonLink?.style),
                isBold: getStyleIsBold(buttonLink?.style),
                isUnderlined: getStyleIsUnderlined(buttonLink?.style),
                isItalicized: getStyleIsItalicized(buttonLink?.style),
                letterCase: getStyleLetterCase(buttonLink?.style),
                lineHeight: toNumberOrNull(buttonLink?.style.lineHeight),
                textColor: buttonLink?.style.color || null,
                horizontalAlignment: toHorizontalAlignmentOrNull(buttonInnerwrap?.getAttribute("align")),
                backgroundColor: buttonLink?.style.backgroundColor || null,
                borderRadiusPx: getStyleBorderRadiusPx(buttonLink?.style),
                width: buttonWidthModel,
                marginPx: getStylePaddingPx(buttonInnerwrap?.style),
                paddingPx: getStylePaddingPx(buttonLink?.style),
                border: getStyleBorder(buttonLink?.style)
            };
        }
    };

    function createEmptyComponentElement(emailDocument: Document): HTMLElement {
        // Only put static/constant structure here; all styles and content should be set via writeLocalProps/writeGlobalProps.
        return createHtmlElement(emailDocument, `
<div class="component component-button" data-state="component" ${datasetKeys.VERSION}="${LATEST_VERSION}">
    <table class="button-outerwrap" border="0" cellpadding="0" cellspacing="0" width="100%" style="min-width: 100%;">
        <tbody>
            <tr>
                <td class="button-innerwrap" valign="top">
                    <table class="button-shell" border="0" cellpadding="0" cellspacing="0">
                        <tbody>
                            <tr>
                                <td class="button-content" align="center" valign="middle">
                                    <a class="button-link ${RockCssClassContentEditable}" target="_blank" rel="noopener noreferrer" style="display: block; letter-spacing: normal; text-align: center;"></a>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </td>
            </tr>
        </tbody>
    </table>
</div>`);
    }

    function getComponentVersion(componentElement: HTMLElement): ComponentVersion {
        const versionNumber = getComponentVersionNumber(componentElement);

        if (!versionNumber || !componentVersions.includes(versionNumber as ComponentVersion)) {
            throw new Error(`Unsupported Button component version "${versionNumber}".`);
        }

        return versionNumber as ComponentVersion;
    }

    const adapter: ButtonComponentAdapter = {
        version: LATEST_VERSION,

        migrateComponent(emailDocument: Document, componentElement: HTMLElement): HTMLElement {
            const version = getComponentVersion(componentElement);

            if (compareComponentVersions(version, LATEST_VERSION) >= 0) {
                // Already latest version
                return componentElement;
            }

            // Copy the local props from the old element to the new one.
            const localProps = localPropReaders[version](componentElement);
            const newComponentElement = createEmptyComponentElement(emailDocument);
            adapter.writeLocalProps(newComponentElement, localProps);

            // Replace the old element with the new one.
            componentElement.replaceWith(newComponentElement);

            return newComponentElement;
        },

        readLocalProps(componentElement: HTMLElement): ButtonLocalProps {
            return localPropReaders[LATEST_VERSION](componentElement);
        },

        writeLocalProps(componentElement: HTMLElement, localProps: ButtonLocalProps): void {
            // This always assumes the componentElement is already migrated to latest version.
            // We don't keep track of version-specific writers; only the latest writer.

            const {
                text,
                href,
                fontFamily,
                fontSizePx,
                isBold,
                isUnderlined,
                isItalicized,
                letterCase,
                lineHeight,
                textColor,
                horizontalAlignment,
                backgroundColor,
                borderRadiusPx,
                width,
                marginPx,
                paddingPx,
                border
            } = localProps;

            const buttonInnerwrap = componentElement.querySelector(".button-innerwrap") as HTMLElement | null;
            const buttonShell = componentElement.querySelector(".button-shell") as HTMLElement | null;
            const buttonContent = componentElement.querySelector(".button-content") as HTMLElement | null;
            const buttonLink = componentElement.querySelector(".button-link") as HTMLElement | null;

            // text
            if (buttonLink) {
                buttonLink.textContent = text;
                buttonLink.title = text;
            }

            setAttributePropertyValue(buttonLink, "href", href);
            setStylePropertyValue(buttonLink?.style, "font-family", fontFamily);
            setStyleFontSizePx(buttonLink?.style, fontSizePx);
            setStyleIsBold(buttonLink?.style, isBold);
            setStyleIsUnderlined(buttonLink?.style, isUnderlined);
            setStyleIsItalicized(buttonLink?.style, isItalicized);
            setStyleLetterCase(buttonLink?.style, letterCase);
            setStyleLineHeight(buttonLink?.style, lineHeight);
            setStylePropertyValue(buttonLink?.style, "color", textColor);
            setStylePaddingPx(buttonLink?.style, paddingPx);
            setStyleBorder(buttonLink?.style, border);

            // background color
            setAttributePropertyValue(componentElement, datasetKeys.COMPONENT_BACKGROUND_COLOR, backgroundColor ? "true" : null);
            setStylePropertyValue(buttonLink?.style, "background-color", backgroundColor);
            setStylePropertyValue(buttonContent?.style, "background-color", backgroundColor);

            // border radius
            setStyleBorderRadiusPx(buttonLink?.style, borderRadiusPx);
            setStyleBorderRadiusPx(buttonContent?.style, borderRadiusPx);

            // width
            setAttributePropertyValue(componentElement, datasetKeys.COMPONENT_BUTTON_WIDTH, width?.mode); // v18.2 stores the actual mode instead of "true"
            if (width?.mode === "full") {
                setAttributePropertyValue(buttonShell, "width", "100%");
                setStylePropertyValue(buttonShell?.style, "width", "100%");
            }
            else if (width?.mode === "fixed") {
                setAttributePropertyValue(buttonShell, "width", width.fixedWidthPx); // no "px" in the attribute
                setStylePropertyValue(buttonShell?.style, "width", toPixelStringValueOrNull(width.fixedWidthPx));
            }
            else {
                // default and "fitToText"
                setAttributePropertyValue(buttonShell, "width", null);
                setStylePropertyValue(buttonShell?.style, "width", null);
            }

            // horizontal alignment
            if (buttonInnerwrap && horizontalAlignment) {
                buttonInnerwrap.setAttribute("align", horizontalAlignment);
            }
            else {
                // Don't delete the horizontal alignment; let global styles handle it.
                //innerwrap.removeAttribute("align");
            }

            // margin (padding on innerwrap)
            setStylePaddingPx(buttonInnerwrap?.style, marginPx);
        },

        createComponentElement(emailDocument: Document): HTMLElement {
            const componentElement = createEmptyComponentElement(emailDocument);

            // Use global props instead of local props when possible.
            const localPropDefaults: ButtonLocalProps = {
                text: "Click Me",
                href: "https://",
                backgroundColor: null,
                fontFamily: null,
                fontSizePx: null,
                isBold: null,
                isUnderlined: null,
                isItalicized: null,
                letterCase: null,
                lineHeight: null,
                textColor: null,
                horizontalAlignment: "center",
                borderRadiusPx: null,
                width: null,
                border: null,
                marginPx: null,
                paddingPx: null
            };

            adapter.writeLocalProps(componentElement, localPropDefaults);

            // Global props will be applied after the component is added to the email DOM.
            return componentElement;
        }
    };

    return adapter;
}

/**
 * Factory returning a self contained ButtonGlobalAdapter.
 */
export function createButtonGlobalAdapter(): ButtonGlobalAdapter {
    // These are the supported versions for global props
    // and are used to look up the appropriate reader/writer functions
    // so that components can handle different versions correctly.
    // Must be in the format "v{major}.{minor}-{tag}" for proper comparison.
    const globalVersions = ["v0", "v2.1-alpha", "v18.2"] as const;
    type GlobalVersion = typeof globalVersions[number];
    const latestVersion: GlobalVersion = getLatestVersion(globalVersions);

    const attributeNames = {
        DATA_VERSION: "data-version",
        DATA_COMPONENT_BUTTON_WIDTH: "data-component-button-width",
        DATA_COMPONENT_BACKGROUND_COLOR: "data-component-background-color",
    } as const;

    const attributeValues = {
        META_NAME_GLOBAL_BUTTON_VERSION: "x-rock-global-button-version",
    } as const;

    const defaultGlobalProps: ButtonGlobalProps = {
        backgroundColor: "#2196f3",
        fontFamily: FontFamilies.Arial,
        fontSizePx: null,
        isBold: true,
        isUnderlined: false,
        isItalicized: null,
        letterCase: null,
        lineHeight: toNumberOrNull(LineHeights.tight.value),
        textColor: "#FFFFFF",
        border: {
            style: createShorthandModel<BorderStyle>("solid"),
            widthPx: createShorthandModel<number>(1),
            color: createShorthandModel<string>("#2196f3")
        },
        borderRadiusPx: createShorthandModel<number>(4),
        width: {
            mode: "fitToText",
            fixedWidthPx: null
        },
        marginPx: null,
        paddingPx: createShorthandModel<number>(15)
    };

    function getGlobalVersion(emailDocument: Document): GlobalVersion {
        // v18.2 and later use the meta tag to track global button schema version.
        const globalVersion = getMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_BUTTON_VERSION)?.getAttribute("content");

        if (globalVersion && globalVersions.includes(globalVersion as GlobalVersion)) {
            return globalVersion as GlobalVersion;
        }

        // Try to infer from structure.
        const buttonLinkStyles = findRockStyleRules(emailDocument, ".component-button .button-link")
            .select(rule => rule.style)
            .toArray();

        if (getStylePropertyValueOrNull(buttonLinkStyles, "text-align") === "center"
            || getStylePropertyValueOrNull(buttonLinkStyles, "letter-spacing") === "normal") {
            // v2.1-alpha and later used rock-styles for button styles.
            return "v2.1-alpha";
        }

        // No version info, assume v0 (legacy).
        return "v0";
    }

    // Don't modify a specific version once released.
    // Instead, add new versions using semver.
    // The major and minor versions should align with the current Rock version.
    // If necessary, you can add patch versions for fixes within the same release.
    // Avoid defining pre-release versions (like "v17.3-alpha") unless absolutely necessary.
    const adapters: Record<GlobalVersion, GlobalAdapterSnapshot<ButtonGlobalProps>> = {
        "v0": {
            version: "v0",

            readGlobalProps(_emailDocument: Document): ButtonGlobalProps {
                // No global props in v0 (legacy)
                return {
                    backgroundColor: null,
                    fontFamily: null,
                    fontSizePx: null,
                    isBold: null,
                    isUnderlined: null,
                    isItalicized: null,
                    letterCase: null,
                    lineHeight: null,
                    textColor: null,
                    border: null,
                    borderRadiusPx: null,
                    width: null,
                    marginPx: null,
                    paddingPx: null
                };
            },

            writeGlobalProps(_emailDocument: Document, _globalProps: ButtonGlobalProps): void {
                // No global props in v0 (legacy)
            }
        },

        "v2.1-alpha": {
            version: "v2.1-alpha",

            readGlobalProps(emailDocument: Document): ButtonGlobalProps {
                const buttonLinkStyles = findRockStyleRules(emailDocument, ".component-button .button-link")
                    .select(rule => rule.style)
                    .toArray();
                const marginWrapperTdStyles = findRockStyleRules(emailDocument, ".margin-wrapper-for-button>tbody>tr>td")
                    .select(rule => rule.style)
                    .toArray();
                const paddingWrapperTdStyles = findRockStyleRules(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-button>tbody>tr>td`)
                    .select(rule => rule.style)
                    .toArray();
                const borderWrapperTdStyles = findRockStyleRules(emailDocument, ".border-wrapper-for-button>tbody>tr>td")
                    .select(rule => rule.style)
                    .toArray();
                const buttonShellWidthStyles = findRockStyleRules(emailDocument, `.component-button:not([data-component-button-width="true"]) .button-shell`)
                    .select(rule => rule.style)
                    .toArray();

                const buttonShellWidth = getStylePropertyValueOrNull(buttonShellWidthStyles, "width");
                const buttonShellWidthPx = toPixelNumericValueOrNull(buttonShellWidth);
                const isFullWidth = buttonShellWidth === "100%";
                const isFixedWidth = !isNullish(buttonShellWidthPx);

                return {
                    backgroundColor: getStylePropertyValueOrNull(paddingWrapperTdStyles, "background-color"),
                    fontFamily: getStylePropertyValueOrNull(buttonLinkStyles, "font-family"),
                    fontSizePx: getStyleFontSizePx(buttonLinkStyles),
                    isBold: getStyleIsBold(buttonLinkStyles),
                    isUnderlined: getStyleIsUnderlined(buttonLinkStyles),
                    isItalicized: getStyleIsItalicized(buttonLinkStyles),
                    letterCase: getStyleLetterCase(buttonLinkStyles),
                    lineHeight: getStyleLineHeight(buttonLinkStyles),
                    textColor: getStylePropertyValueOrNull(buttonLinkStyles, "color"),
                    border: getStyleBorder(buttonLinkStyles),
                    borderRadiusPx: getStyleBorderRadiusPx(borderWrapperTdStyles),
                    width: isFullWidth
                        ? { mode: "full", fixedWidthPx: null }
                        : isFixedWidth
                            ? { mode: "fixed", fixedWidthPx: buttonShellWidthPx }
                            : { mode: "fitToText", fixedWidthPx: null },
                    marginPx: getStylePaddingPx(marginWrapperTdStyles),
                    paddingPx: getStylePaddingPx(buttonLinkStyles)
                };
            },

            writeGlobalProps(emailDocument: Document, globalProps: ButtonGlobalProps): void {
                const buttonLinkRule = findRockStyleRules(emailDocument, ".component-button .button-link").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".component-button .button-link");
                const backgroundColorRule = findRockStyleRules(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-button>tbody>tr>td`).lastOrDefault()
                    ?? createRockStyleRule(emailDocument, `.component:not([data-component-background-color="true"]) .padding-wrapper-for-button>tbody>tr>td`);
                const borderWrapperTdRule = findRockStyleRules(emailDocument, ".border-wrapper-for-button>tbody>tr>td").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".border-wrapper-for-button>tbody>tr>td");
                const marginWrapperTdRule = findRockStyleRules(emailDocument, ".margin-wrapper-for-button>tbody>tr>td").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".margin-wrapper-for-button>tbody>tr>td");
                const buttonWidthButtonShellRuleSelector = `.component-button:not([data-component-button-width="true"]) .button-shell`;
                const buttonWidthButtonShellRule = findRockStyleRules(emailDocument, buttonWidthButtonShellRuleSelector).lastOrDefault()
                    ?? createRockStyleRule(emailDocument, buttonWidthButtonShellRuleSelector);
                const rules = [
                    buttonLinkRule,
                    backgroundColorRule,
                    borderWrapperTdRule,
                    marginWrapperTdRule,
                    buttonWidthButtonShellRule
                ];

                setStylePropertyValue(buttonLinkRule.style, "font-family", globalProps.fontFamily);
                setStyleFontSizePx(buttonLinkRule.style, globalProps.fontSizePx);
                setStyleIsBold(buttonLinkRule.style, globalProps.isBold);
                setStyleIsUnderlined(buttonLinkRule.style, globalProps.isUnderlined);
                setStyleIsItalicized(buttonLinkRule.style, globalProps.isItalicized);
                setStyleLetterCase(buttonLinkRule.style, globalProps.letterCase);
                setStyleLineHeight(buttonLinkRule.style, globalProps.lineHeight);
                setStylePropertyValue(buttonLinkRule.style, "color", globalProps.textColor);
                setStyleBorder(borderWrapperTdRule.style, globalProps.border);
                setStyleBorderRadiusPx(borderWrapperTdRule.style, globalProps.borderRadiusPx);
                setStylePaddingPx(marginWrapperTdRule.style, globalProps.marginPx); // margin is padding on the margin wrapper td
                setStylePaddingPx(buttonLinkRule.style, globalProps.paddingPx);

                // background color
                setStylePropertyValue(buttonLinkRule.style, "background-color", globalProps.backgroundColor);
                setStylePropertyValue(backgroundColorRule.style, "background-color", globalProps.backgroundColor);

                // width
                if (globalProps.width?.mode === "full") {
                    Enumerable
                        .from(emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector))
                        .forEach(el => setAttributePropertyValue(el, "width", "100%"));

                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", "100%");
                }
                else if (globalProps.width?.mode === "fixed") {
                    Enumerable
                        .from(emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector))
                        .forEach(el => setAttributePropertyValue(el, "width", globalProps.width?.fixedWidthPx)); // No "px" in the attribute.

                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", toPixelStringValueOrNull(globalProps.width.fixedWidthPx));
                }
                else {
                    // default and "fitToText"
                    Enumerable
                        .from(emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector))
                        .forEach(el => setAttributePropertyValue(el, "width", null));

                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", null);
                }

                // All the above work only updates the in-memory document; now synchronize the sheet to the DOM.
                synchronizeRulesToDom(rules);
            }
        },

        "v18.2": {
            version: "v18.2",

            readGlobalProps(emailDocument: Document): ButtonGlobalProps {
                const buttonLinkStyles = findRockStyleRules(emailDocument, ".component-button .button-link")
                    .select(rule => rule.style)
                    .toArray(); // Materialize to array the elements are only queried once.
                const buttonWidthButtonShellRuleSelector = `.component-button:not([data-component-button-width]) .button-shell`;
                const buttonWidthButtonShellStyles = findRockStyleRules(emailDocument, buttonWidthButtonShellRuleSelector)
                    .select(rule => rule.style)
                    .toArray();
                const buttonInnerwrapStyles = findRockStyleRules(emailDocument, ".component-button .button-innerwrap")
                    .select(rule => rule.style)
                    .toArray();

                let widthMode: ButtonWidthMode | null = emailDocument.body.getAttribute(attributeNames.DATA_COMPONENT_BUTTON_WIDTH) as (ButtonWidthMode | null) || null;
                if (<string>widthMode === "true") {
                    // Fix issues where the attribute was set to "true" instead of a valid mode.
                    widthMode = "fitToText";
                }
                const fixedWidthPx = toPixelNumericValueOrNull(getStylePropertyValueOrNull(buttonWidthButtonShellStyles, "width"));

                return {
                    fontFamily: getStylePropertyValueOrNull(buttonLinkStyles, "font-family"),
                    fontSizePx: getStyleFontSizePx(buttonLinkStyles),
                    isBold: getStyleIsBold(buttonLinkStyles),
                    isUnderlined: getStyleIsUnderlined(buttonLinkStyles),
                    isItalicized: getStyleIsItalicized(buttonLinkStyles),
                    letterCase: getStyleLetterCase(buttonLinkStyles),
                    lineHeight: getStyleLineHeight(buttonLinkStyles),
                    textColor: getStylePropertyValueOrNull(buttonLinkStyles, "color"),
                    backgroundColor: getStylePropertyValueOrNull(buttonLinkStyles, "background-color"),
                    borderRadiusPx: getStyleBorderRadiusPx(buttonLinkStyles),
                    width: widthMode === "fixed"
                        ? {
                            mode: "fixed",
                            fixedWidthPx
                        }
                        : widthMode
                            ? {
                                mode: widthMode,
                                fixedWidthPx: null
                            }
                            : null,
                    marginPx: getStylePaddingPx(buttonInnerwrapStyles), // margin is padding on the innerwrap
                    paddingPx: getStylePaddingPx(buttonLinkStyles),
                    border: getStyleBorder(buttonLinkStyles)
                };
            },

            writeGlobalProps(emailDocument: Document, globalProps: ButtonGlobalProps): void {
                // Write the version to the document body.
                addOrUpdateMetaTag(emailDocument, attributeValues.META_NAME_GLOBAL_BUTTON_VERSION, "v18.2");

                const buttonLinkRule = findRockStyleRules(emailDocument, ".component-button .button-link").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".component-button .button-link");
                const buttonContentRule = findRockStyleRules(emailDocument, ".component-button .button-content").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".component-button .button-content");
                const buttonWidthButtonShellRuleSelector = `.component-button:not([data-component-button-width]) .button-shell`;
                const buttonWidthButtonShellRule = findRockStyleRules(emailDocument, buttonWidthButtonShellRuleSelector).lastOrDefault()
                    ?? createRockStyleRule(emailDocument, buttonWidthButtonShellRuleSelector);
                const buttonInnerwrapRule = findRockStyleRules(emailDocument, ".component-button .button-innerwrap").lastOrDefault()
                    ?? createRockStyleRule(emailDocument, ".component-button .button-innerwrap");
                const rules = [
                    buttonLinkRule,
                    buttonContentRule,
                    buttonWidthButtonShellRule,
                    buttonInnerwrapRule
                ];

                const {
                    fontFamily,
                    fontSizePx,
                    isBold,
                    isUnderlined,
                    isItalicized,
                    letterCase,
                    lineHeight,
                    textColor,
                    backgroundColor,
                    borderRadiusPx,
                    width,
                    marginPx,
                    paddingPx,
                    border
                } = globalProps;

                // Delete old global rules from v2.1-alpha that are now inline in the buttonLinkRule.
                setStylePropertyValue(buttonLinkRule.style, "text-align", null);
                setStylePropertyValue(buttonLinkRule.style, "letter-spacing", null);

                setStylePropertyValue(buttonLinkRule.style, "font-family", fontFamily);
                setStyleFontSizePx(buttonLinkRule.style, fontSizePx);
                setStyleIsBold(buttonLinkRule.style, isBold);
                setStyleIsUnderlined(buttonLinkRule.style, isUnderlined);
                setStyleIsItalicized(buttonLinkRule.style, isItalicized);
                setStyleLetterCase(buttonLinkRule.style, letterCase);
                setStyleLineHeight(buttonLinkRule.style, lineHeight);
                setStylePropertyValue(buttonLinkRule.style, "color", textColor);
                setStylePaddingPx(buttonLinkRule.style, paddingPx);
                setStylePaddingPx(buttonInnerwrapRule.style, marginPx); // margin (padding on innerwrap)
                setStyleBorder(buttonLinkRule.style, border);

                // background color
                // Clean up any old DATA_COMPONENT_BACKGROUND_COLOR attributes first.
                emailDocument.querySelectorAll(`.component-button[${attributeNames.DATA_COMPONENT_BACKGROUND_COLOR}]`)
                    .forEach(el => {
                        setAttributePropertyValue(el, attributeNames.DATA_COMPONENT_BACKGROUND_COLOR, null);
                    });
                setStylePropertyValue(buttonLinkRule.style, "background-color", backgroundColor);
                setStylePropertyValue(buttonContentRule.style, "background-color", backgroundColor);

                // border radius
                setStyleBorderRadiusPx(buttonLinkRule.style, borderRadiusPx);
                setStyleBorderRadiusPx(buttonContentRule.style, borderRadiusPx);

                // width
                // Set data attribute to indicate whether global button width is used.
                setAttributePropertyValue(emailDocument.body, attributeNames.DATA_COMPONENT_BUTTON_WIDTH, width?.mode);
                if (width?.mode === "full") {
                    emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector).forEach(buttonShell => {
                        setAttributePropertyValue(buttonShell, "width", "100%");
                    });
                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", "100%");
                }
                else if (width?.mode === "fixed") {
                    emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector).forEach(buttonShell => {
                        setAttributePropertyValue(buttonShell, "width", width.fixedWidthPx); // No "px" in the attribute.
                    });
                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", toPixelStringValueOrNull(width.fixedWidthPx));
                }
                else {
                    // default and "fitToText"
                    emailDocument.querySelectorAll(buttonWidthButtonShellRuleSelector).forEach(buttonShell => {
                        setAttributePropertyValue(buttonShell, "width", null);
                    });
                    setStylePropertyValue(buttonWidthButtonShellRule.style, "width", null);
                }

                // All the above work only updates the in-memory document; now synchronize the sheet to the DOM.
                synchronizeRulesToDom(rules);
            }
        }
    };

    const latestAdapter = adapters[latestVersion];

    return {
        ...latestAdapter,

        areGlobalDefaultsNeeded(emailDocument: Document): boolean {
            return compareComponentVersions(getGlobalVersion(emailDocument), "v0") === 0;
        },

        migrateGlobalProps(emailDocument: Document): void {
            const adapter = adapters[getGlobalVersion(emailDocument)];

            if (adapter === latestAdapter || compareComponentVersions(adapter.version, latestAdapter.version) >= 0) {
                // Already latest version; no migration needed.
                return;
            }

            // To migrate global props:
            // 1. Read global props using the old version reader.
            // 2. Write null props using the old version writer to clear old styles.
            // 3. Write the previously read props using the latest version writer.

            const globalProps = adapter.readGlobalProps(emailDocument);
            adapter.writeGlobalProps(emailDocument, {
                backgroundColor: null,
                fontFamily: null,
                fontSizePx: null,
                isBold: null,
                isUnderlined: null,
                isItalicized: null,
                letterCase: null,
                lineHeight: null,
                textColor: null,
                border: null,
                borderRadiusPx: null,
                width: null,
                marginPx: null,
                paddingPx: null
            });
            latestAdapter.writeGlobalProps(emailDocument, globalProps);
        },

        getDefaultGlobalProps(): ButtonGlobalProps {
            return defaultGlobalProps;
        }
    };
}

/**
 * Converts a pixel value string (e.g., "10px") to a number (e.g., 10).
 *
 * @param pixels Pixel value string (e.g., "10px").
 * @returns Numeric pixel value (e.g., 10), or null if invalid.
 */
function toPixelNumericValueOrNull(pixels: string | null | undefined): number | null {
    pixels = pixels?.trim();

    if (pixels?.endsWith("px")) {
        const parsed = Number(pixels.slice(0, -2));

        if (Number.isFinite(parsed)) {
            return parsed;
        }
    }

    // Not in the right format.
    return null;
}

/**
 * Converts a pixel value (e.g., 10) to a string (e.g., "10px").
 *
 * @param pixels Pixel value number (e.g., 10).
 * @returns Pixel value string (e.g., "10px"), or null if invalid.
 */
function toPixelStringValueOrNull(pixels: number | null | undefined): string | null {
    if (isNullish(pixels)) {
        return null;
    }

    return `${pixels}px`;
}

/**
 * Converts a percentage string (e.g., "50%") to a numeric value (e.g., 50).
 *
 * @param percentage Percentage string (e.g., "50%").
 * @returns Numeric percentage value (e.g., 50), or null if invalid.
 */
function toPercentageNumericValueOrNull(percentage: string | null | undefined): number | null {
    percentage = percentage?.trim();

    if (percentage?.endsWith("%")) {
        const parsed = Number(percentage.slice(0, -1));
        if (Number.isFinite(parsed)) {
            return parsed;
        }
    }

    return null;
}

/**
 * Converts a numeric percentage value (e.g., 50) to a percentage string (e.g., "50%").
 *
 * @param percentage Numeric percentage value (e.g., 50).
 * @returns Percentage string (e.g., "50%"), or null if invalid.
 */
function toPercentageStringValueOrNull(percentage: number | null | undefined): string | null {
    if (isNullish(percentage)) {
        return null;
    }

    return `${percentage}%`;
}

function toResizeModeOrNull(resizeMode: string | null | undefined): ResizeMode | null {
    if (!resizeMode) {
        return null;
    }

    resizeMode = resizeMode.trim().toLowerCase();
    switch (resizeMode) {
        case "crop":
            return "crop";
        case "pad":
            return "pad";
        case "stretch":
            return "stretch";
        default:
            return null;
    }
}

/**
 * Converts a CSS border style (e.g., "solid", "dashed", etc.) string to a BorderStyle or null if invalid.
 *
 * @param cssBorderStyle CSS border style string.
 * @returns BorderStyle or null if invalid.
 */
function toBorderStyleOrNull(cssBorderStyle: string | null | undefined): BorderStyle | null {
    cssBorderStyle = cssBorderStyle?.trim().toLowerCase();
    switch (cssBorderStyle) {
        case "solid":
        case "dashed":
        case "dotted":
        case "none":
            return cssBorderStyle;
        default:
            return null;
    }
}

/**
 * Converts a string to HorizontalAlignment or null if invalid.
 *
 * @param align Alignment string (e.g., "left", "center", "right").
 * @returns HorizontalAlignment or null if invalid.
 */
function toHorizontalAlignmentOrNull(align: string | null | undefined): HorizontalAlignment | null {
    const alignValue = align?.trim().toLowerCase() || "";

    return alignValue === "left" || alignValue === "center" || alignValue === "right"
        ? (alignValue as HorizontalAlignment)
        : null;
}

function toTextAlignmentOrNull(horizontalAlignment: HorizontalAlignment | string | null | undefined): TextAlignment | null {
    switch (horizontalAlignment?.trim().toLowerCase()) {
        case "left":
            return "left";
        case "center":
            return "center";
        case "right":
            return "right";
        case "justify":
            return "justify";
        default:
            return null;
    }
}

function isEnumerable<T>(obj: unknown): obj is Enumerable<T> {
    return !!obj
        && typeof obj === "object"
        && typeof (obj as Enumerable<T>).ofType === "function"
        && typeof (obj as Enumerable<T>).aggregate === "function";
}

/**
 * Gets the last specified value of a CSS property from the provided style(s), mimicking cascading behavior (last style wins).
 *
 * @param style
 * @param propertyName
 * @returns
 */
function getStylePropertyValueOrNull(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined, propertyName: CssStyleDeclarationKebabKey): string | null {
    const lastValue = Array.isArray(style)
        ? Enumerable.from(style).select(s => s.getPropertyValue(propertyName)).lastOrDefault(v => !!v)
        : isEnumerable<CSSStyleDeclaration>(style)
            ? style.select(s => s.getPropertyValue(propertyName)).lastOrDefault(v => !!v)
            : style?.getPropertyValue(propertyName);

    return lastValue || null;
}

/**
 * Sets the value of an attribute on the provided element.
 *
 * @param element The HTML element on which to set the attribute.
 * @param attributeName The name of the attribute to set.
 * @param value The value to set for the attribute, or null/undefined to remove it.
 */
function setAttributePropertyValue(element: Element | null | undefined, attributeName: string, value: string | number | null | undefined): void {
    if (!element) {
        // No element to modify. This is for convenience so callers don't have to check for null.
        return;
    }

    if (typeof value === "number") {
        value = value.toString();
    }

    if (value) {
        element.setAttribute(attributeName, value);
    }
    else {
        element.removeAttribute(attributeName);
    }
}

/**
 * Sets the value of a CSS property on the provided style.
 *
 * @param style CSSStyleDeclaration object to modify.
 * @param propertyName CSS property name in kebab-case.
 * @param value Value to set, or null/undefined to remove the property.
 */
function setStylePropertyValue(style: CSSStyleDeclaration | null | undefined, propertyName: CssStyleDeclarationKebabKey, value: string | null | undefined): void {
    if (!style) {
        // No style to modify. This is for convenience so callers don't have to check for null.
        return;
    }

    if (value) {
        style.setProperty(propertyName, value);
    }
    else {
        style.removeProperty(propertyName);
    }
}


function getStyleShorthandValueOrNull<T>(
    style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined,
    { top, right, bottom, left }: ShorthandPropertyNames,
    toValue: (value: string | null) => T
): ShorthandModel<T> | null {
    const topValueString = getStylePropertyValueOrNull(style, top);
    const rightValueString = getStylePropertyValueOrNull(style, right);
    const bottomValueString = getStylePropertyValueOrNull(style, bottom);
    const leftValueString = getStylePropertyValueOrNull(style, left);

    if (!topValueString && !rightValueString && !bottomValueString && !leftValueString) {
        return null;
    }

    const topValue = toValue(topValueString);
    const rightValue = toValue(rightValueString);
    const bottomValue = toValue(bottomValueString);
    const leftValue = toValue(leftValueString);

    if (isNullish(topValue) && isNullish(rightValue) && isNullish(bottomValue) && isNullish(leftValue)) {
        return null;
    }

    return {
        top: topValue,
        right: rightValue,
        bottom: bottomValue,
        left: leftValue
    };
}

function setStyleShorthandValue<T>(style: CSSStyleDeclaration | null | undefined, value: ShorthandModel<T | null> | null | undefined,
    { top, bottom, right, left }: ShorthandPropertyNames,
    toString: (v: T) => string
): void {
    if (!style) {
        // No style to modify. This is for convenience so callers don't have to check for null.
        return;
    }

    setStylePropertyValue(style, top, !isNullish(value?.top) ? toString(value!.top) : null);
    setStylePropertyValue(style, right, !isNullish(value?.right) ? toString(value!.right) : null);
    setStylePropertyValue(style, bottom, !isNullish(value?.bottom) ? toString(value!.bottom) : null);
    setStylePropertyValue(style, left, !isNullish(value?.left) ? toString(value!.left) : null);
}

function getStyleFontSizePx(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): number | null {
    const lastFontSize = getStylePropertyValueOrNull(style, "font-size");
    return toPixelNumericValueOrNull(lastFontSize);
}

function setStyleFontSizePx(style: CSSStyleDeclaration | null | undefined, fontSizePx: number | null | undefined): void {
    setStylePropertyValue(style, "font-size", toPixelStringValueOrNull(fontSizePx));
}

/**
 * Determines if the style indicates bold text.
 *
 * Interprets the last specified "font-weight" property from the provided style(s), mimicking cascading behavior (last style wins).
 *
 * @param style CSS style(s) to check.
 * @returns `true` if `"bold"`, `false` if `"normal"`, or `null` if unspecified.
 */
function getStyleIsBold(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): boolean | null {
    const lastFontWeight = getStylePropertyValueOrNull(style, "font-weight");

    if (!lastFontWeight) {
        return null;
    }

    const fontWeight = lastFontWeight.toLocaleLowerCase();
    const fontWeightAsNumber = toNumberOrNull(fontWeight);

    // Use tri-state to indicate bold, not bold, or unspecified (for global inheritance)
    if (fontWeight === "bold"
        || fontWeight === "bolder"
        || (!isNullish(fontWeightAsNumber) && fontWeightAsNumber >= 700)) {
        return true;
    }
    else if (fontWeight === "normal"
        || fontWeight === "light"
        || fontWeight === "lighter"
        || (!isNullish(fontWeightAsNumber) && fontWeightAsNumber < 700)) {
        return false;
    }
    else {
        return null;
    }
}

/**
 * Sets the "font-weight" property on the style based on the isBold value.
 *
 * @param style CSS style to modify.
 * @param isBold `true` to set `"bold"`, `false` to set `"normal"`, or `null`/`undefined` to remove the property.
 */
function setStyleIsBold(style: CSSStyleDeclaration | null | undefined, isBold: boolean | null | undefined): void {
    setStylePropertyValue(style, "font-weight", !isNullish(isBold) ? (isBold ? "bold" : "normal") : null);
}

/**
 * Determines if the style indicates italicized text.
 *
 * Interprets the last specified "font-style" property from the provided style(s), mimicking cascading behavior (last style wins).
 *
 * @param style CSS style(s) to check.
 * @returns `true` if `"italic"`, `false` if `"normal"`, or `null` if unspecified.
 */
function getStyleIsItalicized(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): boolean | null {
    const lastFontStyle = getStylePropertyValueOrNull(style, "font-style");

    if (!lastFontStyle) {
        return null;
    }

    const fontStyle = lastFontStyle.toLocaleLowerCase();

    // Use tri-state to indicate italicized, not italicized, or unspecified (for global inheritance)
    if (fontStyle === "italic") {
        return true;
    }
    else if (fontStyle === "normal") {
        return false;
    }
    else {
        return null;
    }
}

/**
 * Sets the "font-style" property on the style based on the isItalicized value.
 *
 * @param style CSS style to modify.
 * @param isItalicized `true` to set `"italic"`, `false` to set `"normal"`, or `null`/`undefined` to remove the property.
 */
function setStyleIsItalicized(style: CSSStyleDeclaration | null | undefined, isItalicized: boolean | null | undefined): void {
    setStylePropertyValue(style, "font-style", !isNullish(isItalicized) ? (isItalicized ? "italic" : "normal") : null);
}

/**
 * Determines if the style indicates underlined text.
 *
 * Interprets the last specified "text-decoration" property from the provided style(s), mimicking cascading behavior (last style wins).
 *
 * @param style CSS style(s) to check.
 * @returns `true` if underlined, `false` if not underlined, or `null` if unspecified.
 */
function getStyleIsUnderlined(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): boolean | null {
    const lastTextDecoration = getStylePropertyValueOrNull(style, "text-decoration");

    if (!lastTextDecoration) {
        return null;
    }

    const textDecoration = lastTextDecoration.toLocaleLowerCase();

    // Use tri-state to indicate underlined, not underlined, or unspecified (for global inheritance)
    if (textDecoration === "underline") {
        return true;
    }
    else if (textDecoration === "none") {
        return false;
    }
    else {
        return null;
    }
}

/**
 * Sets the "text-decoration" property on the style based on the isUnderlined value.
 *
 * @param style CSS style to modify.
 * @param isUnderlined `true` to set `"underline"`, `false` to set `"none"`, or `null`/`undefined` to remove the property.
 */
function setStyleIsUnderlined(style: CSSStyleDeclaration | null | undefined, isUnderlined: boolean | null | undefined): void {
    setStylePropertyValue(style, "text-decoration", !isNullish(isUnderlined) ? (isUnderlined ? "underline" : "none") : null);
}

/**
 * Gets the line height from the style.
 *
 * @param style CSS style(s) to check.
 * @returns Line height as a number, or null if unspecified.
 */
function getStyleLineHeight(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): number | null {
    const lastLineHeight = getStylePropertyValueOrNull(style, "line-height");
    return toNumberOrNull(lastLineHeight);
}

/**
 * Sets the "line-height" property on the style.
 *
 * @param style CSS style to modify.
 * @param lineHeight Line height to set, or `null`/`undefined` to remove the property.
 */
function setStyleLineHeight(style: CSSStyleDeclaration | null | undefined, lineHeight: number | null | undefined): void {
    setStylePropertyValue(style, "line-height", !isNullish(lineHeight) ? lineHeight.toString() : null);
}

/**
 * Gets the letter case from the style.
 *
 * Interprets the last specified "text-transform" property from the provided style(s) to mimic cascading behavior (last style wins).
 *
 * @param style CSS style(s) to check.
 * @returns LetterCase or null if unspecified.
 */
function getStyleLetterCase(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): LetterCase | null {
    const lastTextTransform = getStylePropertyValueOrNull(style, "text-transform");

    if (!lastTextTransform) {
        return null;
    }

    const textTransform = lastTextTransform.toLocaleLowerCase();

    if (textTransform === "none"
        || textTransform === "uppercase"
        || textTransform === "lowercase"
        || textTransform === "capitalize") {
        return textTransform as LetterCase;
    }
    else {
        return null;
    }
}

/**
 * Sets the "text-transform" property on the style based on the letterCase value.
 *
 * @param style CSS style to modify.
 * @param letterCase LetterCase to set, or `null`/`undefined` to remove the property.
 */
function setStyleLetterCase(style: CSSStyleDeclaration | null | undefined, letterCase: LetterCase | null | undefined): void {
    setStylePropertyValue(style, "text-transform", !isNullish(letterCase) ? letterCase : null);
}

function getStylePaddingPx(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<number | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "padding-top",
            right: "padding-right",
            bottom: "padding-bottom",
            left: "padding-left"
        },
        toPixelNumericValueOrNull
    );
}

function setStylePaddingPx(style: CSSStyleDeclaration | null | undefined, paddingPx: ShorthandModel<number | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        paddingPx,
        {
            top: "padding-top",
            right: "padding-right",
            bottom: "padding-bottom",
            left: "padding-left"
        },
        v => `${v}px`
    );
}

function getStyleMarginPx(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<number | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "margin-top",
            right: "margin-right",
            bottom: "margin-bottom",
            left: "margin-left"
        },
        toPixelNumericValueOrNull
    );
}

function setStyleMarginPx(style: CSSStyleDeclaration | null | undefined, marginPx: ShorthandModel<number | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        marginPx,
        {
            top: "margin-top",
            right: "margin-right",
            bottom: "margin-bottom",
            left: "margin-left"
        },
        v => `${v}px`
    );
}

function getStyleBorderRadiusPx(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<number | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "border-top-left-radius",
            bottom: "border-top-right-radius",
            right: "border-bottom-right-radius",
            left: "border-bottom-left-radius"
        },
        toPixelNumericValueOrNull
    );
}

function setStyleBorderRadiusPx(style: CSSStyleDeclaration | null | undefined, borderRadiusPx: ShorthandModel<number | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        borderRadiusPx,
        {
            top: "border-top-left-radius",
            bottom: "border-top-right-radius",
            right: "border-bottom-right-radius",
            left: "border-bottom-left-radius"
        },
        v => toPixelStringValueOrNull(v)!
    );
}

function getStyleBorderStyle(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<BorderStyle | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "border-top-style",
            right: "border-right-style",
            bottom: "border-bottom-style",
            left: "border-left-style"
        },
        toBorderStyleOrNull
    );
}

function setStyleBorderStyle(style: CSSStyleDeclaration | null | undefined, value: ShorthandModel<BorderStyle | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        value,
        {
            top: "border-top-style",
            right: "border-right-style",
            bottom: "border-bottom-style",
            left: "border-left-style"
        },
        v => v
    );
}

function getStyleBorderColor(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<string | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "border-top-color",
            right: "border-right-color",
            bottom: "border-bottom-color",
            left: "border-left-color"
        },
        v => v || null
    );
}

function setStyleBorderColor(style: CSSStyleDeclaration | null | undefined, value: ShorthandModel<string | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        value,
        {
            top: "border-top-color",
            right: "border-right-color",
            bottom: "border-bottom-color",
            left: "border-left-color"
        },
        v => v
    );
}

function getStyleBorderWidthPx(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): ShorthandModel<number | null> | null {
    return getStyleShorthandValueOrNull(
        style,
        {
            top: "border-top-width",
            right: "border-right-width",
            bottom: "border-bottom-width",
            left: "border-left-width"
        },
        toPixelNumericValueOrNull
    );
}

function setStyleBorderWidthPx(style: CSSStyleDeclaration | null | undefined, value: ShorthandModel<number | null> | null | undefined): void {
    setStyleShorthandValue(
        style,
        value,
        {
            top: "border-top-width",
            right: "border-right-width",
            bottom: "border-bottom-width",
            left: "border-left-width"
        },
        v => toPixelStringValueOrNull(v)!
    );
}

function getStyleBorder(style: Enumerable<CSSStyleDeclaration> | CSSStyleDeclaration[] | CSSStyleDeclaration | null | undefined): BorderModel | null {
    const borderColor = getStyleBorderColor(style);
    const borderStyle = getStyleBorderStyle(style);
    const borderWidth = getStyleBorderWidthPx(style);

    return !borderColor && !borderStyle && !borderWidth
        ? null
        : {
            color: borderColor,
            style: borderStyle,
            widthPx: borderWidth
        };
}

function setStyleBorder(style: CSSStyleDeclaration | null | undefined, border: BorderModel | null | undefined): void {
    setStyleBorderColor(style, border?.color);
    setStyleBorderStyle(style, border?.style);
    setStyleBorderWidthPx(style, border?.widthPx);
}

/**
 * Finds rock style CSS rules matching the specified ruleset selector within the document.
 *
 * This looks for `<style class="rock-styles">` elements within the `<body>` of the document
 *
 * @param doc The document in which to find the rock style rules.
 * @param rulesetSelector The CSS selector for the ruleset.
 * @returns An enumerable of matching CSSStyleRule objects.
 */
function findRockStyleRules(doc: Document, rulesetSelector: string): Enumerable<CSSStyleRule> {
    const emailDocWindow = doc.defaultView;

    if (!emailDocWindow) {
        return Enumerable.empty<CSSStyleRule>();
    }

    const normalizedRulesetSelectors = new Set(normalizeSelectorList(rulesetSelector));

    // 1. Find the <style class="rock-styles"> elements within <body>.
    return Enumerable
        .from(doc.body.querySelectorAll(`style.rock-styles`))
        .ofType((el): el is HTMLStyleElement => el instanceof emailDocWindow.HTMLStyleElement)
        .selectMany(styleEl => {
            if (!styleEl || !styleEl.sheet) {
                return Enumerable.empty<CSSStyleRule>();
            }

            const sheet = styleEl.sheet as CSSStyleSheet;

            // 2. Iterate through the matching CSS rules
            return Enumerable
                .from(sheet.cssRules)
                .ofType<CSSStyleRule>((rule): rule is CSSStyleRule => rule instanceof emailDocWindow.CSSStyleRule)
                .where(rule => normalizeSelectorList(rule.selectorText).some(selector => normalizedRulesetSelectors.has(selector)));
        });
}

/**
 * Finds rock style CSS rules matching the specified ruleset selector within the document.
 *
 * This looks for `<style class="rock-styles">` elements within the `<body>` of the document
 *
 * @param emailDocument The document in which to find the rock style rules.
 * @returns An enumerable of matching CSSStyleRule objects.
 */
function findRockMediaStyleSheets(emailDocument: Document): Enumerable<CSSStyleSheet> {
    const docWindow = emailDocument.defaultView;

    if (!docWindow) {
        return Enumerable.empty<CSSStyleSheet>();
    }

    return Enumerable
        .from(emailDocument.head.querySelectorAll(`style.rock-media-styles`))
        .ofType((el): el is HTMLStyleElement => el instanceof docWindow.HTMLStyleElement)
        .select(el => el.sheet)
        .ofType((sheet): sheet is CSSStyleSheet => sheet instanceof docWindow.CSSStyleSheet);
}

function createRockMediaStyleSheet(emailDocument: Document): CSSStyleSheet {
    const emailDocWindow = emailDocument.defaultView;
    if (!emailDocWindow) {
        throw new Error("Document has no defaultView.");
    }

    // Create the <style class="rock-media-styles"> element within <body>.
    const styleEl = emailDocument.createElement("style");
    styleEl.className = "rock-media-styles";
    emailDocument.head.append(styleEl);

    // This should not be `null` now that the style element is in the DOM.
    return styleEl.sheet as CSSStyleSheet;
}

/**
 * Creates a new rock style CSS rule for the specified ruleset selector within the document.
 *
 * This creates (or reuses) a `<style class="rock-styles">` element within the `<body>` of the document
 *
 * @param emailDocument The document in which to create or find the rock style rule.
 * @param rulesetSelector The CSS selector for the ruleset.
 * @returns The created or found CSSStyleRule.
 */
function createRockStyleRule(emailDocument: Document, rulesetSelector: string): CSSStyleRule {
    const emailDocWindow = emailDocument.defaultView;
    if (!emailDocWindow) {
        throw new Error("Document has no defaultView.");
    }

    // Find the last or create the <style class="rock-styles"> elements within <body>.
    let styleEl = Enumerable
        .from(emailDocument.body.querySelectorAll(`style.rock-styles`))
        .ofType((el): el is HTMLStyleElement => el instanceof emailDocWindow.HTMLStyleElement)
        .lastOrDefault();
    if (!styleEl) {
        styleEl = emailDocument.createElement("style");
        styleEl.className = "rock-styles";
        emailDocument.body.insertBefore(styleEl, emailDocument.body.firstChild);
    }

    const sheet = styleEl.sheet as CSSStyleSheet; // This should not be `null` now that the style element is in the DOM.

    // Create a new CSS rule for the specified selector.
    const ruleIndex = sheet.cssRules.length;
    sheet.insertRule(`${rulesetSelector} { }`, ruleIndex);
    return sheet.cssRules.item(ruleIndex) as CSSStyleRule;
}

function normalizeSingleSelector(selector: string): string {
    return selector
        .trim()
        // Remove spaces around combinators like ">", "+", "~"
        .replace(/\s*([>+~])\s*/g, "$1")
        // Collapse all remaining whitespace to a single space
        .replace(/\s+/g, " ");
}

function normalizeSelectorList(selectorText: string): string[] {
    return selectorText
        .split(",")
        .map(s => normalizeSingleSelector(s));
}

function serializeSheet(sheet: CSSStyleSheet): string {
    let css = "";
    for (const rule of sheet.cssRules) {
        css += rule.cssText + "\n";
    }
    return css;
}

function synchronizeSheetToDom(sheet: CSSStyleSheet | null | undefined): void {
    if (!sheet) {
        return;
    }

    const ownerNode = sheet.ownerNode;

    if (ownerNode?.ownerDocument?.defaultView) {
        if (ownerNode instanceof ownerNode.ownerDocument.defaultView.HTMLStyleElement) {
            ownerNode.textContent = serializeSheet(sheet);
        }
    }
}

function synchronizeRulesToDom(rules: Iterable<CSSRule>): void {
    if (!rules) {
        return;
    }

    const sheets = Enumerable
        .from(rules)
        .select(rule => rule.parentStyleSheet)
        .distinct()
        .toArray();

    for (const sheet of sheets) {
        synchronizeSheetToDom(sheet);
    }
}
