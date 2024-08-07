import { BlockToolConstructorOptions } from "@editorjs/editorjs";
import EditorRaw, { RawData } from "@editorjs/raw";

export class Raw extends EditorRaw {
    constructor(config: BlockToolConstructorOptions<RawData, any>) {
        super(config);
    }

    render() {
        const container = super.render();
        const textarea = container.querySelector("textarea");

        if (textarea) {
            textarea.spellcheck = false;

            // Workaround for https://github.com/editor-js/raw/issues/16
            if (navigator.userAgent.toLowerCase().includes("firefox")) {
                const blockKeys = ["ArrowLeft", "ArrowUp", "Backspace"];

                textarea.addEventListener("keydown", function (event) {
                    if (blockKeys.includes(event.key)) {
                        event.stopPropagation();
                    }
                });
            }
        }

        return container;
    }

    static get toolbox() {
        const config = EditorRaw.toolbox;

        if (Array.isArray(config)) {
            for (const item of config) {
                if (item.title === "Raw HTML") {
                    item.title = "Raw HTML/Lava";
                }
            }
        }
        else {
            config.title = "Raw HTML/Lava";
        }

        return config;
    }
}
