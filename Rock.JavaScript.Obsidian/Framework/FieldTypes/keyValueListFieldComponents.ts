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
import { computed, defineComponent, ref, watch } from "vue";
import KeyValueList from "@Obsidian/Controls/keyValueList.obs";
import CheckBox from "@Obsidian/Controls/checkBox.obs";
import DropDownList from "@Obsidian/Controls/dropDownList.obs";
import RockFormField from "@Obsidian/Controls/rockFormField.obs";
import TextBox from "@Obsidian/Controls/textBox.obs";
import { asBoolean, asBooleanOrNull, asTrueFalseOrNull } from "@Obsidian/Utility/booleanUtils";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { ClientValue, ConfigurationPropertyKey, ConfigurationValueKey, ValueItem } from "./keyValueListField.partial";
import { getFieldConfigurationProps, getFieldEditorProps } from "./utils";

function parseModelValue(modelValue: string | undefined): ClientValue[] {
    try {
        return JSON.parse(modelValue ?? "[]") as ClientValue[];
    }
    catch {
        return [];
    }
}

export const EditComponent = defineComponent({
    name: "KeyValueListField.Edit",

    components: {
        KeyValueList,
        RockFormField,
        DropDownList,
        TextBox
    },

    props: getFieldEditorProps(),

    setup(props, { emit }) {
        const internalValues = ref(parseModelValue(props.modelValue));

        const valueOptions = computed((): ValueItem[] => {
            try {
                return JSON.parse(props.configurationValues[ConfigurationValueKey.Values] ?? "[]") as ValueItem[];
            }
            catch {
                return [];
            }
        });

        /** The options to choose from in the drop down list */
        const options = computed((): ListItemBag[] => {
            const providedOptions: ListItemBag[] = valueOptions.value.map(v => {
                return {
                    text: v.text,
                    value: v.value
                };
            });

            return providedOptions;
        });

        const keyPlaceholder = computed((): string => {
            return props.configurationValues[ConfigurationValueKey.KeyPrompt]?.trimEnd() || "Key";
        });

        const valuePlaceholder = computed((): string => {
            return props.configurationValues[ConfigurationValueKey.ValuePrompt]?.trimEnd() || "Value";
        });

        const displayValueFirst = computed((): boolean => {
            return asBoolean(props.configurationValues[ConfigurationValueKey.DisplayValueFirst] ?? "");
        });

        const allowHtml = computed((): boolean => {
            return asBoolean(props.configurationValues[ConfigurationValueKey.AllowHtml] ?? "");
        });

        watch(() => props.modelValue, () => {
            internalValues.value = parseModelValue(props.modelValue);
        });

        watch(() => internalValues.value, () => {
            emit("update:modelValue", JSON.stringify(internalValues.value));
        }, { deep: true });

        return {
            internalValues,
            displayValueFirst,
            options,
            keyPlaceholder,
            valuePlaceholder,
            allowHtml
        };
    },

    template: `
<KeyValueList v-model="internalValues"
    :keyPlaceholder="keyPlaceholder"
    :valuePlaceholder="valuePlaceholder"
    :displayValueFirst="displayValueFirst"
    :valueOptions="options"
    :allowHtml="allowHtml" />
`
});

export const ConfigurationComponent = defineComponent({
    name: "KeyValueListField.Configuration",

    components: {
        CheckBox,
        DropDownList,
        TextBox
    },

    props: getFieldConfigurationProps(),

    emits: [
        "update:modelValue",
        "updateConfiguration",
        "updateConfigurationValue"
    ],

    setup(props, { emit }) {
        // Define the properties that will hold the current selections.
        const customValues = ref("");
        const internalCustomValues = ref("");
        const keyPrompt = ref("");
        const labelPrompt = ref("");
        const definedType = ref("");
        const allowHtml = ref(false);
        const displayValueFirst = ref(false);

        const definedTypeOptions = computed((): ListItemBag[] => {
            try {
                return JSON.parse(props.configurationProperties[ConfigurationPropertyKey.DefinedTypes] ?? "[]") as ListItemBag[];
            }
            catch {
                return [];
            }
        });

        const onBlur = (): void => {
            internalCustomValues.value = customValues.value;
        };

        /**
         * Update the modelValue property if any value of the dictionary has
         * actually changed. This helps prevent unwanted postbacks if the value
         * didn't really change - which can happen if multiple values get updated
         * at the same time.
         *
         * @returns true if a new modelValue was emitted to the parent component.
         */
        const maybeUpdateModelValue = (): boolean => {
            const newValue: Record<string, string> = {};

            // Construct the new value that will be emitted if it is different
            // than the current value.
            newValue[ConfigurationValueKey.KeyPrompt] = keyPrompt.value ?? "";
            newValue[ConfigurationValueKey.ValuePrompt] = labelPrompt.value ?? "";
            newValue[ConfigurationValueKey.DefinedType] = definedType.value ?? "";
            newValue[ConfigurationValueKey.CustomValues] = internalCustomValues.value ?? "";
            newValue[ConfigurationValueKey.AllowHtml] = asTrueFalseOrNull(allowHtml.value) ?? "False";
            newValue[ConfigurationValueKey.DisplayValueFirst] = asTrueFalseOrNull(displayValueFirst.value) ?? "False";

            // Compare the new value and the old value.
            const anyValueChanged = newValue[ConfigurationValueKey.KeyPrompt] !== (props.modelValue[ConfigurationValueKey.KeyPrompt] ?? "")
                || newValue[ConfigurationValueKey.ValuePrompt] !== (props.modelValue[ConfigurationValueKey.ValuePrompt] ?? "")
                || newValue[ConfigurationValueKey.DefinedType] !== (props.modelValue[ConfigurationValueKey.DefinedType] ?? "")
                || newValue[ConfigurationValueKey.CustomValues] !== (props.modelValue[ConfigurationValueKey.CustomValues] ?? "")
                || newValue[ConfigurationValueKey.AllowHtml] !== (props.modelValue[ConfigurationValueKey.AllowHtml] ?? "False")
                || newValue[ConfigurationValueKey.DisplayValueFirst] !== (props.modelValue[ConfigurationValueKey.DisplayValueFirst] ?? "False");

            // If any value changed then emit the new model value.
            if (anyValueChanged) {
                emit("update:modelValue", newValue);
                return true;
            }
            else {
                return false;
            }
        };

        /**
         * Emits the updateConfigurationValue if the value has actually changed.
         *
         * @param key The key that was possibly modified.
         * @param value The new value.
         */
        const maybeUpdateConfiguration = (key: string, value: string): void => {
            if (maybeUpdateModelValue()) {
                emit("updateConfigurationValue", key, value);
            }
        };

        // Watch for changes coming in from the parent component and update our
        // data to match the new information.
        watch(() => [props.modelValue, props.configurationProperties], () => {
            keyPrompt.value = props.modelValue[ConfigurationValueKey.KeyPrompt] ?? "";
            labelPrompt.value = props.modelValue[ConfigurationValueKey.ValuePrompt] ?? "";
            definedType.value = props.modelValue[ConfigurationValueKey.DefinedType] ?? "";
            customValues.value = props.modelValue[ConfigurationValueKey.CustomValues] ?? "";
            internalCustomValues.value = customValues.value;
            allowHtml.value = asBooleanOrNull(props.modelValue[ConfigurationValueKey.AllowHtml]) ?? false;
            displayValueFirst.value = asBooleanOrNull(props.modelValue[ConfigurationValueKey.DisplayValueFirst]) ?? false;
        }, {
            immediate: true
        });

        // Watch for changes in properties that require new configuration
        // properties to be retrieved from the server.
        watch([definedType, internalCustomValues, keyPrompt, labelPrompt, allowHtml, displayValueFirst], () => {
            if (maybeUpdateModelValue()) {
                emit("updateConfiguration");
            }
        });

        // Watch for changes in properties that only require a local UI update.
        watch(keyPrompt, () => maybeUpdateConfiguration(ConfigurationValueKey.KeyPrompt, keyPrompt.value ?? ""));
        watch(labelPrompt, () => maybeUpdateConfiguration(ConfigurationValueKey.ValuePrompt, labelPrompt.value ?? ""));
        watch(definedType, () => maybeUpdateConfiguration(ConfigurationValueKey.DefinedType, definedType.value ?? ""));
        watch(allowHtml, () => maybeUpdateConfiguration(ConfigurationValueKey.AllowHtml, asTrueFalseOrNull(allowHtml.value) ?? "False"));
        watch(displayValueFirst, () => maybeUpdateConfiguration(ConfigurationValueKey.DisplayValueFirst, asTrueFalseOrNull(displayValueFirst.value) ?? "False"));

        return {
            allowHtml,
            definedType,
            definedTypeOptions,
            displayValueFirst,
            keyPrompt,
            labelPrompt,
            onBlur,
            customValues
        };
    },

    template: `
<div>
    <TextBox v-model="keyPrompt"
        label="Key Prompt"
        help="The text to display as a prompt in the key textbox." />

    <TextBox v-model="labelPrompt"
        label="Label Prompt"
        help="The text to display as a prompt in the label textbox." />

    <DropDownList v-model="definedType"
        label="Defined Type"
        help="Optional Defined Type to select values from, otherwise values will be free-form text fields."
        :items="definedTypeOptions" />

    <TextBox v-model="customValues"
        label="Custom Values"
        help="Optional list of options to use for the values.  Format is either 'value1,value2,value3,...', or 'value1^text1,value2^text2,value3^text3,...'."
        textMode="multiline"
        @blur="onBlur" />

    <CheckBox v-model="allowHtml"
        label="Allow HTML"
        help="Allow HTML content in values." />

    <CheckBox v-model="displayValueFirst"
        label="Display Value First"
        help="Reverses the display order of the key and the value." />
</div>
`
});
