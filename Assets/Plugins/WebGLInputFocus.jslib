mergeInto(LibraryManager.library, {
    CreateWebGLInput: function (elementIdPtr, xPos, yPos, width, height, contentType, characterLimit) {
        var elementId = UTF8ToString(elementIdPtr);

        // Check if the input element already exists
        var existingInput = document.getElementById(elementId);
        if (existingInput) {
            existingInput.focus();
            return;
        }

        // Create a new input element dynamically
        var input = document.createElement("input");

        // Configure input based on TMP_InputField content type
        switch (contentType) {
            case 0: // Standard
                input.type = "text";
                break;
            case 1: // Autocorrected
                input.type = "text";
                input.autocapitalize = "on"; // Enable autocorrect
                break;
            case 2: // Integer Number
                input.type = "number";
                input.pattern = "\\d*"; // Limit to digits
                break;
            case 3: // Decimal Number
                input.type = "text";
                input.inputMode = "decimal"; // Decimal input
                break;
            case 4: // Alphanumeric
                input.type = "text";
                input.pattern = "[a-zA-Z0-9]*"; // Alphanumeric only
                break;
            case 5: // Name
                input.type = "text";
                input.autocapitalize = "words"; // Capitalize words
                break;
            case 6: // Email
                input.type = "email"; // Email input field
                break;
            case 7: // Password
                input.type = "password"; // Password input
                break;
            case 8: // Pin (numeric password)
                input.type = "password";
                input.pattern = "\\d*"; // Numeric-only password
                break;
            default:
                input.type = "text";
        }

        // Set max length if character limit is defined
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
        input.style.zIndex = 1000;
        input.style.userSelect = "text";

        // Ensure the input field remains focusable for clipboard interactions
        input.setAttribute('contenteditable', 'true');

        // Add an event listener to update the Unity input field whenever the user types
        input.addEventListener("input", function() {
            SendMessage(elementId, "OnInputChanged", input.value);
        });

        // Listen for the "Enter", "Go", or "Next" key press to signal the user is done typing
        input.addEventListener("keydown", function(event) {
            if (event.key === "Enter" || event.key === "Go" || event.key === "Next") {
                input.blur(); // Remove focus from the input field to close the keyboard
                if (input.parentNode) {  // Check if the input field is still in the DOM
                    document.body.removeChild(input); // Remove the input element from DOM
                }
                SendMessage(elementId, "OnInputFinished"); // Notify Unity that typing is done
            }
        });

        // Handle input field deselection (when the input loses focus)
        input.addEventListener("blur", function() {
            if (input.parentNode) {  // Safely check if input is still part of the DOM before removing
                document.body.removeChild(input); // Remove the input element from DOM
            }
            SendMessage(elementId, "OnInputFinished"); // Notify Unity that typing is done
        });

        document.body.appendChild(input);
        input.focus(); // Trigger focus to open the keyboard on mobile

        console.log("Input field created and focused");
    }
});