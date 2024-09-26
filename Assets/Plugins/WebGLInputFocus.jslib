mergeInto(LibraryManager.library, {
    CreateWebGLInput: function (elementIdPtr, xPos, yPos, width, height, contentType, characterLimit) {
        var elementId = UTF8ToString(elementIdPtr);

        // Remove existing input if it's already there
        var existingInput = document.getElementById(elementId);
        if (existingInput) {
            document.body.removeChild(existingInput);
        }

        // Create a new input element
        var input = document.createElement("input");

        // Configure input based on TMP_InputField content type
        switch (contentType) {
            case 0: input.type = "text"; break;
            case 1: input.type = "text"; input.autocapitalize = "on"; break;
            case 2: input.type = "number"; input.pattern = "\\d*"; break;
            case 3: input.type = "text"; input.inputMode = "decimal"; break;
            case 4: input.type = "text"; input.pattern = "[a-zA-Z0-9]*"; break;
            case 5: input.type = "text"; input.autocapitalize = "words"; break;
            case 6: input.type = "email"; break;
            case 7: input.type = "password"; break;
            case 8: input.type = "password"; input.pattern = "\\d*"; break;
            default: input.type = "text";
        }

        if (characterLimit > 0) {
            input.maxLength = characterLimit;
        }

        input.id = elementId;
        input.style.position = "absolute";
        input.style.top = yPos + "px";
        input.style.left = xPos + "px";
        input.style.width = width + "px";
        input.style.height = height + "px";
        input.style.fontSize = "16px";
        input.style.zIndex = "999999";
        input.style.backgroundColor = "transparent";
        input.style.border = "none";
        input.style.outline = "none";
        input.style.caretColor = "black";
        input.style.boxSizing = "border-box";
        input.style.padding = "0 5px";

        input.oninput = function() {
            SendMessage(elementId, "OnInputChanged", input.value);
        };

        input.onkeydown = function(event) {
            if (event.key === "Enter") {
                input.blur();
            }
        };

        input.onblur = function() {
            SendMessage(elementId, "OnInputFinished");
            document.body.removeChild(input);
        };

        input.onfocus = function() {
            setTimeout(function() {
                input.scrollIntoView({behavior: "smooth", block: "center", inline: "nearest"});
            }, 100);
        };

        document.body.appendChild(input);
        input.focus();

        console.log("WebGL input created: " + elementId);
    }
});