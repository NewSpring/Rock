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
import { App, Component, createApp, defineComponent, Directive, h, markRaw, onMounted, provide, reactive, ref, VNode } from "vue";
import RockBlock from "./rockBlock.partial";
import { useStore } from "@Obsidian/PageState";
import "@Obsidian/ValidationRules";
import "@Obsidian/FieldTypes/index";
import { DebugTiming } from "@Obsidian/ViewModels/Utility/debugTiming";
import { ObsidianBlockConfigBag } from "@Obsidian/ViewModels/Cms/obsidianBlockConfigBag";
import { FormError, FormState, provideFormState } from "@Obsidian/Utility/form";
import { PageConfig } from "@Obsidian/Utility/page";
import { RockDateTime } from "@Obsidian/Utility/rockDateTime";
import { BasicSuspenseProvider, provideSuspense } from "@Obsidian/Utility/suspense";
import { alert } from "@Obsidian/Utility/dialogs";
import { HttpBodyData, HttpMethod, HttpResult, HttpUrlParams } from "@Obsidian/Types/Utility/http";
import { doApiCall, provideHttp } from "@Obsidian/Utility/http";
import { createInvokeBlockAction, provideBlockBrowserBus, provideBlockGuid, provideBlockTypeGuid } from "@Obsidian/Utility/block";
import { useBrowserBus } from "@Obsidian/Utility/browserBus";

type DebugTimingConfig = {
    elementId: string;
    debugTimingViewModels: DebugTiming[];
};

const store = useStore();

/**
 * This is a special use component that allows developers to include style
 * tags inside a string-literal component (i.e. not an SFC). It should only
 * be used temporarily until the styling team can move the styles into the
 * LESS and CSS files.
 */
const developerStyle = defineComponent({
    render(): VNode {
        return h("style", {}, this.$slots.default ? this.$slots.default() : undefined);
    }
});

/**
 * This directive (v-content) behaves much like v-html except it also allows
 * pre-defined HTML nodes to be passed in. This can be used to show and hide
 * nodes without losing any 3rd party event listeners or other data that would
 * otherwise be lost when using an HTML string.
 */
const contentDirective: Directive<Element, Node[] | Node | string | null | undefined> = {
    mounted(el, binding) {
        el.innerHTML = "";
        if (Array.isArray(binding.value)) {
            for (const v of binding.value) {
                el.append(v);
            }
        }
        else if (typeof binding.value === "string") {
            el.innerHTML = binding.value;
        }
        else if (binding.value) {
            el.append(binding.value);
        }
    },
    updated(el, binding) {
        el.innerHTML = "";
        if (Array.isArray(binding.value)) {
            for (const v of binding.value) {
                el.append(v);
            }
        }
        else if (typeof binding.value === "string") {
            el.innerHTML = binding.value;
        }
        else if (binding.value) {
            el.append(binding.value);
        }
    }
};


/**
* This should be called once per block on the page. The config contains configuration provided by the block's server side logic
* counterpart.  This adds the block to the page and allows it to begin initializing.
* @param config
* @param blockComponent
*/
export async function initializeBlock(config: ObsidianBlockConfigBag): Promise<App> {
    const blockPath = `${config.blockFileUrl}.js`;
    let blockComponent: Component | null = null;
    let errorMessage = "";

    if (!config || !config.blockFileUrl || !config.blockGuid || !config.rootElementId) {
        console.error("Invalid block configuration:", config);
        throw "Could not initialize Obsidian block because the configuration is invalid.";
    }

    const rootElement = document.getElementById(config.rootElementId);
    const wrapperElement = rootElement?.querySelector<HTMLElement>(".obsidian-block-wrapper");

    if (!rootElement || !wrapperElement) {
        throw "Could not initialize Obsidian block because the root element was not found.";
    }

    try {
        const blockComponentModule = await import(blockPath);
        blockComponent = blockComponentModule ?
            (blockComponentModule.default || blockComponentModule) :
            null;
    }
    catch (e) {
        // Log the error, but continue setting up the app so the UI will show the user an error
        console.error(e);
        errorMessage = `${e}`;
    }

    const startTimeMs = RockDateTime.now().toMilliseconds();
    const name = `Root${config.blockFileUrl.replace(/\//g, ".")}`;
    const staticContent = ref<Node[]>([]);

    while (wrapperElement.firstChild !== null) {
        const node = wrapperElement.firstChild;
        node.remove();
        staticContent.value.push(node);
    }

    const app = createApp({
        name,
        components: {
            RockBlock
        },
        setup() {
            let isLoaded = false;

            // Create a suspense provider so we can monitor any asynchronous load
            // operations that should delay the display of the page.
            const suspense = new BasicSuspenseProvider(undefined);
            provideSuspense(suspense);

            /** Called to note on the body element that this block is loading. */
            const startLoading = (): void => {
                let pendingCount = parseInt(document.body.getAttribute("data-obsidian-pending-blocks") ?? "0");
                pendingCount++;
                document.body.setAttribute("data-obsidian-pending-blocks", pendingCount.toString());
            };

            /** Called to note when this block has finished loading. */
            const finishedLoading = (): void => {
                if (isLoaded) {
                    return;
                }

                isLoaded = true;

                if (rootElement.classList.contains("obsidian-block-has-placeholder")) {
                    wrapperElement.style.padding = "1px 0px";
                    const realHeight = wrapperElement.getBoundingClientRect().height - 2;
                    wrapperElement.style.padding = "";

                    rootElement.style.height = `${realHeight}px`;
                    setTimeout(() => {
                        rootElement.querySelector(".obsidian-block-placeholder")?.remove();
                        rootElement.style.height = "";
                        rootElement.classList.remove("obsidian-block-has-placeholder");
                    }, 200);
                }

                rootElement.classList.remove("obsidian-block-loading");

                // Get the number of pending blocks. If this is the last one
                // then signal the page that all blocks are loaded and ready.
                let pendingCount = parseInt(document.body.getAttribute("data-obsidian-pending-blocks") ?? "0");
                if (pendingCount > 0) {
                    pendingCount--;
                    document.body.setAttribute("data-obsidian-pending-blocks", pendingCount.toString());
                    if (pendingCount === 0) {
                        document.body.classList.remove("obsidian-loading");
                    }
                }
            };

            // Start loading and wait for up to 5 seconds for the block to finish.
            startLoading();
            setTimeout(finishedLoading, 5000);

            // Called when all our child components have initialized.
            onMounted(() => {
                if (!suspense.hasPendingOperations()) {
                    finishedLoading();
                }
                else {
                    suspense.addFinishedHandler(() => {
                        finishedLoading();
                    });
                }
            });

            return {
                config: config,
                blockComponent: blockComponent ? markRaw(blockComponent) : null,
                startTimeMs,
                staticContent,
                errorMessage
            };
        },

        // Note: We are using a custom alert so there is not a dependency on
        // the Controls package.
        template: `
<div v-if="errorMessage" class="alert alert-danger">
    <strong>Error Initializing Block</strong>
    <br />
    {{errorMessage}}
</div>
<RockBlock v-else :config="config" :blockComponent="blockComponent" :startTimeMs="startTimeMs" :staticContent="staticContent" />`
    });

    app.directive("content", contentDirective);
    app.component("v-style", developerStyle);
    app.mount(wrapperElement);

    return app;
}

/**
* This is an internal method which would be changed in future. This was created for the Short Link Modal and would be made more generic in future.
* @param url
*/
export function showShortLink(url: string): void {
    const rootElement = document.createElement("div");
    const modalPopup = document.createElement("div");
    const modalPopupContentPanel = document.createElement("div");
    const iframe = document.createElement("iframe");

    rootElement.className = "modal-scrollable";
    rootElement.id = "shortlink-modal-popup";
    rootElement.style.zIndex = "1060";
    rootElement.appendChild(modalPopup);

    modalPopup.id = "shortlink-modal-popup";
    modalPopup.className = "modal container modal-content rock-modal rock-modal-frame modal-overflow in";
    modalPopup.style.opacity = "1";
    modalPopup.style.display = "block";
    modalPopup.style.marginTop = "0px";
    modalPopup.style.position = "absolute";
    modalPopup.style.top = "30px";
    modalPopup.appendChild(modalPopupContentPanel);

    modalPopupContentPanel.className = "iframe";
    modalPopupContentPanel.id = "shortlink-modal-popup_contentPanel";
    modalPopupContentPanel.appendChild(iframe);

    document.body.appendChild(rootElement);

    const modalBackDroping = document.createElement("div");
    modalBackDroping.id = "shortlink-modal-popup_backDrop";
    modalBackDroping.className = "modal-backdrop in";
    modalBackDroping.style.zIndex = "1050";
    document.body.appendChild(modalBackDroping);


    iframe.id = "shortlink-modal-popup_iframe";
    iframe.src = url;
    iframe.style.display = "block";
    iframe.style.width = "100%";
    iframe.style.borderRadius = "6px";
    iframe.scrolling = "no";
    iframe.style.overflowY = "clip";
    const iframeResizer = new ResizeObserver((event) => {
        const iframeBody = event[0].target;
        iframe.style.height = iframeBody.scrollHeight.toString() + "px";
    });
    iframe.onload = () => {
        if (!iframe?.contentWindow?.document?.documentElement) {
            return;
        }
        iframe.style.height = iframe.contentWindow.document.body.scrollHeight.toString() + "px";
        iframeResizer.observe(iframe.contentWindow.document.body);
    };
}

/**
 * This is an internal method that will be removed in the future. It serves the
 * ObsidianDataComponentWrapper WebForms control to initialize an Obsidian
 * component inside a WebForms component.
 *
 * @param url The URL of the Obsidian component to load.
 * @param rootElementId The identifier of the DOM node to mount the component on.
 * @param componentDataId The identifier of the DOM node that contains the component data.
 * @param componentPropertiesId The identifier of the DOM node that contains the additional component properties.
 */
export async function initializeDataComponentWrapper(url: string, rootElementId: string, componentDataId: string, componentPropertiesId: string | undefined): Promise<void> {
    const componentUrl = `${url}.js`;
    let component: Component | null = null;
    let errorMessage = "";

    const rootElement = document.getElementById(rootElementId);

    if (!rootElement) {
        throw new Error("Could not initialize Obsidian component because the root element was not found.");
    }

    try {
        const componentModule = await import(componentUrl);
        component = componentModule ?
            (componentModule.default || componentModule) :
            null;
    }
    catch (e) {
        // Log the error, but continue setting up the app so the UI will show the user an error
        console.error(e);
        errorMessage = `${e}`;
    }

    const name = `Root${componentUrl.replace(/\//g, ".")}`;

    // Initialize a fake form state to track errors and proxy them to the
    // WebForms system.
    const formErrors: Record<string, FormError> = {};
    const formState = reactive<FormState>({
        submitCount: 0,
        setError(id, name, error) {
            if (error) {
                formErrors[id] = { name, text: error };
            }
            else if (formErrors[id]) {
                delete formErrors[id];
            }
        }
    });

    // Register the validator function for this component. This will be called
    // whenever form validation is triggered by submitting a form.
    window[`validator_${rootElementId}`] = function (validationControl: HTMLElement & { errormessage?: string }, controlState: Record<string, unknown>): void {
        formState.submitCount++;

        // If we don't have any errors, then the form is valid.
        if (Object.keys(formErrors).length === 0) {
            controlState.IsValid = true;
            return;
        }

        // Translate the form errors into error strings.
        const errors: string[] = [];
        for (const key of Object.keys(formErrors)) {
            errors.push(`${formErrors[key].name} ${formErrors[key].text}`);
        }

        // Put the error strings into the WebForms control. It injects the error
        // text as raw HTML, so we hijack things a bit to make the bullet list
        // look right.
        if (errors.length === 1) {
            validationControl.errormessage = errors[0];
        }
        else {
            const firstError = errors.shift();
            validationControl.errormessage = `${firstError}</li><li>${errors.join("</li><li>")}</li>`;
        }

        controlState.IsValid = false;
    };

    const app = createApp({
        name,
        setup() {
            let componentData: Record<string, string> = {};
            let componentProperties: Record<string, unknown> = {};

            provideFormState(formState);

            try {
                const componentDataElement = document.getElementById(componentDataId) as HTMLInputElement;

                componentData = JSON.parse(decodeURIComponent(componentDataElement.value)) ?? {};
            }
            catch (e) {
                if (!errorMessage) {
                    errorMessage = `${e}`;
                }
            }

            if (componentPropertiesId) {
                try {
                    const componentPropertiesElement = document.getElementById(componentPropertiesId) as HTMLInputElement;

                    componentProperties = JSON.parse(decodeURIComponent(componentPropertiesElement.value)) ?? {};
                }
                catch (e) {
                    if (!errorMessage) {
                        errorMessage = `${e}`;
                    }
                }
            }

            function onUpdateComponentData(data: Record<string, string>): void {
                const componentDataElement = document.getElementById(componentDataId) as HTMLInputElement;

                if (componentDataElement) {
                    componentDataElement.value = encodeURIComponent(JSON.stringify(data));
                }
            }

            return {
                component: component ? markRaw(component) : null,
                componentData,
                componentProperties,
                onUpdateComponentData,
                errorMessage
            };
        },

        // Note: We are using a custom alert so there is not a dependency on
        // the Controls package.
        template: `
<div v-if="errorMessage" class="alert alert-danger">
    <strong>Error Initializing Component</strong>
    <br />
    {{errorMessage}}
</div>
<component v-else :is="component" :modelValue="componentData" @update:modelValue="onUpdateComponentData" v-bind="componentProperties" />`
    });

    app.mount(rootElement);

    // This monitors for a WebForms postback that has removed the old DOM
    // tree. The app will then be unmounted to free memory and remove even
    // listeners that may have been installed.
    const observer = new MutationObserver(mutations => {
        let removed = false;

        for (const mutation of mutations) {
            for (const node of mutation.removedNodes) {
                if (node == rootElement || node.contains(rootElement)) {
                    removed = true;
                }
            }
        }

        if (removed) {
            app.unmount();
            observer.disconnect();
        }
    });

    observer.observe(document.body, { subtree: true, childList: true });
}

/**
 * Loads and shows a custom block action. This is a special purpose function
 * designed to be used only by the WebForms PageZoneBlocksEditor.ascx.cs control.
 * It will be removed once WebForms blocks are no longer supported.
 *
 * @param actionFileUrl The component file URL for the action handler.
 * @param pageGuid The unique identifier of the page.
 * @param blockGuid The unique identifier of the block.
 * @param blockTypeGuid The unique identifier of the block type.
 */
export async function showCustomBlockAction(actionFileUrl: string, pageGuid: string, blockGuid: string, blockTypeGuid: string): Promise<void> {
    let actionComponent: Component | null = null;

    try {
        const actionComponentModule = await import(actionFileUrl);
        actionComponent = actionComponentModule ?
            (actionComponentModule.default || actionComponentModule) :
            null;
    }
    catch (e) {
        // Log the error, but continue setting up the app so the UI will show the user an error
        console.error(e);
        alert("There was an error trying to show these settings.");
        return;
    }

    const name = `Action${actionFileUrl.replace(/\//g, ".")}`;

    const app = createApp({
        name,
        components: {
        },
        setup() {
            // Create a suspense provider so we can monitor any asynchronous load
            // operations that should delay the display of the page.
            const suspense = new BasicSuspenseProvider(undefined);
            provideSuspense(suspense);

            const httpCall = async <T>(method: HttpMethod, url: string, params: HttpUrlParams = undefined, data: HttpBodyData = undefined): Promise<HttpResult<T>> => {
                return await doApiCall<T>(method, url, params, data);
            };

            const get = async <T>(url: string, params: HttpUrlParams = undefined): Promise<HttpResult<T>> => {
                return await httpCall<T>("GET", url, params);
            };

            const post = async <T>(url: string, params: HttpUrlParams = undefined, data: HttpBodyData = undefined): Promise<HttpResult<T>> => {
                return await httpCall<T>("POST", url, params, data);
            };

            const invokeBlockAction = createInvokeBlockAction(post, pageGuid, blockGuid, store.state.pageParameters, store.state.sessionGuid, store.state.interactionGuid);

            provideHttp({
                doApiCall,
                get,
                post
            });
            provide("blockActionUrl", (actionName: string): string => {
                return `/api/v2/BlockActions/${pageGuid}/${blockGuid}/${actionName}`;
            });
            provide("invokeBlockAction", invokeBlockAction);
            provideBlockGuid(blockGuid);
            provideBlockTypeGuid(blockTypeGuid);
            provideBlockBrowserBus(useBrowserBus({ block: blockGuid, blockType: blockTypeGuid }));

            return {
                actionComponent,
                onCustomActionClose
            };
        },

        // Note: We are using a custom alert so there is not a dependency on
        // the Controls package.
        template: `<component :is="actionComponent" @close="onCustomActionClose" />`
    });

    function onCustomActionClose(): void {
        app.unmount();
        rootElement.remove();
    }

    const rootElement = document.createElement("div");
    document.body.append(rootElement);

    app.component("v-style", developerStyle);
    app.mount(rootElement);
}

/**
 * This should be called once per page with data from the server that pertains to the entire page. This includes things like
 * page parameters and context entities.
 *
 * @param {object} pageConfig
 */
export async function initializePage(pageConfig: PageConfig): Promise<void> {
    await store.initialize(pageConfig);
}

/**
 * Shows the Obsidian debug timings
 * @param debugTimingConfig
 */
export async function initializePageTimings(config: DebugTimingConfig): Promise<void> {
    const rootElement = document.getElementById(config.elementId);

    if (!rootElement) {
        console.error("Could not show Obsidian debug timings because the HTML element did not resolve.");
        return;
    }

    const pageDebugTimings = (await import("@Obsidian/Controls/Internal/pageDebugTimings.obs")).default;

    const app = createApp({
        name: "PageDebugTimingsRoot",
        components: {
            PageDebugTimings: pageDebugTimings
        },
        data() {
            return {
                viewModels: config.debugTimingViewModels
            };
        },
        template: `<PageDebugTimings :serverViewModels="viewModels" />`
    });
    app.mount(rootElement);
}

/**
* This is an internal type which would be changed in future. This was created for the Short Link Modal and would be made more generic in future.
*/
export type ShortLinkEmitter = {
    closeModal: string
};