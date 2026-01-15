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

export const enum PreferenceKey {
    ConnectionmOpportunityFilterConnectionTypeIdKey = "ConnectionOpportunityFilter_ConnectionTypeIdKey_{0}",
    SelectedGroupByMode = "SelectedGroupByMode",
    FilterSortByConnectionTypIdKey = "FilterSortBy_ConnectionTypeIdKey_{0}",
    FilterIsOnlyMyActiveOpportunitiesShownConnectionTypeIdKey = "FilterIsOnlyActiveOpportunitiesShown_ConnectionTypeIdKey_{0}",
    FilterGridDataToShowConnectionTypIdKey = "FilterGridDataToShow_ConnectionTypeIdKey_{0}",
    FilterIsRequestSourceShownConnectionTypIdKey = "FilterIsRequestSourceShown_ConnectionTypeIdKey_{0}",
    FilterStateConnectionTypIdKey = "FilterState_ConnectionTypeIdKey_{0}",
    FilterStatusConnectionTypIdKey = "FilterStatus_ConnectionTypeIdKey_{0}",
    FilterDueConnectionTypIdKey = "FilterDue_ConnectionTypeIdKey_{0}",
}

export type ViewOptions = {
    sortBy?: string | null,
    isOnlyMyActiveOpportunitiesShown: boolean,
    gridDataToShow?: string[] | null,
    isRequestSourceShown: boolean,
    stateFilter?: string[] | null,
    statusFilter?: string[] | null,
    dueFilter?: string[] | null
};