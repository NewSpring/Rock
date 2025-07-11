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

import { Guid } from "@Obsidian/Types";
import axios, { AxiosError, AxiosProgressEvent, AxiosResponse, GenericAbortSignal } from "axios";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { HttpBodyData, HttpMethod, HttpFunctions, HttpResult, HttpUrlParams } from "@Obsidian/Types/Utility/http";
import { inject, provide, getCurrentInstance, ref, type Ref } from "vue";
import { ICancellationToken } from "./cancellation";


// #region HTTP Requests

/**
 * Make an API call. This is only place Axios (or AJAX library) should be referenced to allow tools like performance metrics to provide
 * better insights.
 * @param method
 * @param url
 * @param params
 * @param data
 */
async function doApiCallRaw(method: HttpMethod, url: string, params: HttpUrlParams, data: HttpBodyData, cancellationToken?: ICancellationToken): Promise<AxiosResponse<unknown>> {
    return await axios({
        method,
        url,
        params,
        data,
        signal: getSignal(cancellationToken)
    });
}

function getSignal(cancellationToken?: ICancellationToken): GenericAbortSignal | undefined {
    if (cancellationToken) {
        const controller = new AbortController();

        cancellationToken.onCancellationRequested(() => {
            if (controller && controller.signal && !controller.signal.aborted) {
                controller.abort();
            }
        });

        return controller.signal;
    }
}

/**
 * Make an API call.  This is a special use function that should not
 * normally be used. Instead call useHttp() to get the HTTP functions that
 * can be used.
 *
 * @param {string} method The HTTP method, such as GET
 * @param {string} url The endpoint to access, such as /api/campuses/
 * @param {object} params Query parameter object.  Will be converted to ?key1=value1&key2=value2 as part of the URL.
 * @param {any} data This will be the body of the request
 */
export async function doApiCall<T>(method: HttpMethod, url: string, params: HttpUrlParams = undefined, data: HttpBodyData = undefined, cancellationToken?: ICancellationToken): Promise<HttpResult<T>> {
    try {
        const result = await doApiCallRaw(method, url, params, data, cancellationToken);

        return {
            data: result.data as T,
            isError: false,
            isSuccess: true,
            statusCode: result.status,
            errorMessage: null
        } as HttpResult<T>;
    }
    catch (e) {
        if (axios.isAxiosError(e)) {
            if (e.response?.data?.Message || e?.response?.data?.message) {
                return {
                    data: null,
                    isError: true,
                    isSuccess: false,
                    statusCode: e.response.status,
                    errorMessage: e?.response?.data?.Message ?? e.response.data.message
                } as HttpResult<T>;
            }

            return {
                data: null,
                isError: true,
                isSuccess: false,
                statusCode: e.response?.status ?? 0,
                errorMessage: null
            } as HttpResult<T>;
        }
        else {
            return {
                data: null,
                isError: true,
                isSuccess: false,
                statusCode: 0,
                errorMessage: null
            } as HttpResult<T>;
        }
    }
}

/**
 * Make a GET HTTP request. This is a special use function that should not
 * normally be used. Instead call useHttp() to get the HTTP functions that
 * can be used.
 *
 * @param {string} url The endpoint to access, such as /api/campuses/
 * @param {object} params Query parameter object.  Will be converted to ?key1=value1&key2=value2 as part of the URL.
 */
export async function get<T>(url: string, params: HttpUrlParams = undefined): Promise<HttpResult<T>> {
    return await doApiCall<T>("GET", url, params, undefined);
}

/**
 * Make a POST HTTP request. This is a special use function that should not
 * normally be used. Instead call useHttp() to get the HTTP functions that
 * can be used.
 *
 * @param {string} url The endpoint to access, such as /api/campuses/
 * @param {object} params Query parameter object.  Will be converted to ?key1=value1&key2=value2 as part of the URL.
 * @param {any} data This will be the body of the request
 */
export async function post<T>(url: string, params: HttpUrlParams = undefined, data: HttpBodyData = undefined, cancellationToken?: ICancellationToken): Promise<HttpResult<T>> {
    return await doApiCall<T>("POST", url, params, data, cancellationToken);
}

const httpFunctionsSymbol = Symbol("http-functions");

/**
 * Provides the HTTP functions that child components will use. This is an
 * internal API and should not be used by third party components.
 *
 * @param functions The functions that will be made available to child components.
 */
export function provideHttp(functions: HttpFunctions): void {
    provide(httpFunctionsSymbol, functions);
}

/**
 * Gets the HTTP functions that can be used by the component. This is the
 * standard way to make HTTP requests.
 *
 * @returns An object that contains the functions which can be called.
 */
export function useHttp(): HttpFunctions {
    let http: HttpFunctions | undefined;

    // Check if we are inside a setup instance. This prevents warnings
    // from being displayed if being called outside a setup() function.
    if (getCurrentInstance()) {
        http = inject<HttpFunctions>(httpFunctionsSymbol);
    }

    return http || {
        doApiCall: doApiCall,
        get: get,
        post: post
    };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type ApiCallerOptions<ReturnType, Args extends any[] = []> = {
    url: string;
    params?: HttpUrlParams | ((...args: Args) => HttpUrlParams);
    data?: HttpBodyData | ((...args: Args) => HttpBodyData);
    onComplete?: ((data: ReturnType, ...args: Args) => void) | null | undefined;
    method?: "get" | "post" | "GET" | "POST" | undefined;
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type ApiCallerReturnType<ReturnType, Args extends any[] = []> = {
    run: (...args: Args) => Promise<ReturnType | undefined>;
    readonly isLoading: Ref<boolean>;
    readonly hasError: Ref<boolean>;
    readonly errorMessage: Ref<string | undefined>;
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function createApiCaller<ReturnType = unknown, Args extends any[] = []> (options: ApiCallerOptions<ReturnType, Args>): ApiCallerReturnType<ReturnType, Args> {
    const fetchFunction = useHttp()[(options.method || "post").toLowerCase()];
    const isLoading = ref(false);
    const hasError = ref(false);
    const errorMessage = ref<string>();

    return {
        isLoading,
        hasError,
        errorMessage,
        async run (...args) {
            isLoading.value = true;
            hasError.value = false;
            errorMessage.value = undefined;

            const params = typeof options.params === "function" ? options.params(...args) : options.params;
            const data = typeof options.data === "function" ? options.data(...args) : options.data;

            try {
                const result = (await fetchFunction<ReturnType>(options.url, params, data)) as HttpResult<ReturnType>;

                if (result.isSuccess) {
                    if (typeof options.onComplete === "function") {
                        options.onComplete(result.data as ReturnType, ...args);
                    }

                    return result.data as ReturnType;
                }
                else {
                    hasError.value = true;
                    errorMessage.value = result.errorMessage ?? undefined;
                }
            }
            catch (e: unknown) {
                hasError.value = true;

                if (e instanceof Error) {
                    errorMessage.value = e.message;
                }
                else if (typeof e === "string") {
                    errorMessage.value = e;
                }
                else {
                    errorMessage.value = "An unknown error occurred.";
                }
            }
            finally {
                isLoading.value = false;
            }
        }
    };

}

// #endregion

// #region File Upload

type FileUploadResponse = {
    /* eslint-disable @typescript-eslint/naming-convention */
    Guid: Guid;
    FileName: string;
    /* eslint-enable */
};

/**
 * Progress reporting callback used when uploading a file into Rock.
 */
export type UploadProgressCallback = (progress: number, total: number, percent: number) => void;

/**
 * Options used when uploading a file into Rock to change the default behavior.
 */
export type UploadOptions = {
    /**
     * The base URL to use when uploading the file, must accept the same parameters
     * and as the standard FileUploader.ashx handler.
     */
    baseUrl?: string;

    /** True if the file should be uploaded as temporary, only applies to binary files. */
    isTemporary?: boolean;

    /** A function to call to report the ongoing progress of the upload. */
    progress: UploadProgressCallback;

    /** The parent entity type identifier */
    parentEntityTypeId?: number;

    /** The parent entity identifier */
    parentEntityId?: number;
};

/**
 * Uploads a file in the form data into Rock. This is an internal function and
 * should not be exported.
 *
 * @param url The URL to use for the POST request.
 * @param data The form data to send in the request body.
 * @param progress The optional callback to use to report progress.
 *
 * @returns The response from the upload handler.
 */
async function uploadFile(url: string, data: FormData, progress: UploadProgressCallback | undefined): Promise<FileUploadResponse> {
    let result: AxiosResponse<FileUploadResponse | string> | undefined;
    try {
        result = await axios.post<FileUploadResponse | string>(url, data, {
            headers: {
                "Content-Type": "multipart/form-data"
            },
            onUploadProgress: (event: AxiosProgressEvent) => {
                if (progress && event.total !== undefined) {
                    progress(event.loaded, event.total, Math.floor(event.loaded * 100 / event.total));
                }
            }
        });
    }
    catch (e) {
        result = (e as AxiosError<FileUploadResponse | string>).response;
    }

    if (!result) {
        throw new Error("Upload failed.");
    }

    // Check for a "everything went perfectly fine" response.
    if (result.status === 200 && typeof result.data === "object") {
        return result.data;
    }

    if (result.status === 406) {
        throw new Error("File type is not allowed.");
    }

    if (typeof result.data === "string") {
        throw new Error(result.data);
    }

    throw new Error("Upload failed.");
}

/**
 * Uploads a file to the Rock file system, usually inside the ~/Content directory.
 *
 * @param file The file to be uploaded to the server.
 * @param encryptedRootFolder The encrypted root folder specified by the server,
 * this specifies the jail the upload operation is limited to.
 * @param folderPath The additional sub-folder path to use inside the root folder.
 * @param options The options to use when uploading the file.
 *
 * @returns A ListItemBag that contains the scrubbed filename that was uploaded.
 */
export async function uploadContentFile(file: File, encryptedRootFolder: string, folderPath: string, options?: UploadOptions): Promise<ListItemBag> {
    const url = `${options?.baseUrl ?? "/FileUploader.ashx"}?rootFolder=${encodeURIComponent(encryptedRootFolder)}`;
    const formData = new FormData();

    formData.append("file", file);

    if (folderPath) {
        formData.append("folderPath", folderPath);
    }

    const result = await uploadFile(url, formData, options?.progress);

    return {
        value: "",
        text: result.FileName
    };
}

/**
 * Uploads a file to an asset storage provider.
 *
 * @param file The file to be uploaded to the server.
 * @param folderPath The additional sub-folder path to use inside the root folder.
 * @param assetStorageId The ID of the asset storage provider that the file is being uploaded to
 * @param options The options to use when uploading the file.
 *
 * @returns A ListItemBag that contains the scrubbed filename that was uploaded.
 */
export async function uploadAssetProviderFile(file: File, folderPath: string, assetStorageId: string, options?: UploadOptions): Promise<ListItemBag> {
    const url = `${options?.baseUrl ?? "/FileUploader.ashx"}?rootFolder=`;
    const formData = new FormData();

    if (!assetStorageId) {
        throw "Asset Storage Id and Key are required.";
    }

    formData.append("file", file);
    formData.append("StorageId", assetStorageId);
    formData.append("Key", folderPath);
    formData.append("IsAssetStorageProviderAsset", "true");

    const result = await uploadFile(url, formData, options?.progress);

    return {
        value: "",
        text: result.FileName
    };
}

/**
 * Uploads a BinaryFile into Rock. The specific storage location is defined by
 * the file type.
 *
 * @param file The file to be uploaded into Rock.
 * @param binaryFileTypeGuid The unique identifier of the BinaryFileType to handle the upload.
 * @param options The options ot use when uploading the file.
 *
 * @returns A ListItemBag whose value contains the new file Guid and text specifies the filename.
 */
export async function uploadBinaryFile(file: File, binaryFileTypeGuid: Guid, options?: UploadOptions): Promise<ListItemBag> {
    let url = `${options?.baseUrl ?? "/FileUploader.ashx"}?isBinaryFile=True&fileTypeGuid=${binaryFileTypeGuid}`;

    // Assume file is temporary unless specified otherwise so that files
    // that don't end up getting used will get cleaned up.
    if (options?.isTemporary === false) {
        url += "&isTemporary=False";
    }
    else {
        url += "&isTemporary=True";
    }

    if (options?.parentEntityTypeId) {
        url += "&ParentEntityTypeId=" + options.parentEntityTypeId;
    }

    if (options?.parentEntityId) {
        url += "&ParentEntityId=" + options.parentEntityId;
    }

    const formData = new FormData();
    formData.append("file", file);

    const result = await uploadFile(url, formData, options?.progress);

    return {
        value: result.Guid,
        text: result.FileName
    };
}

// #endregion

export default {
    doApiCall,
    post,
    get
};
