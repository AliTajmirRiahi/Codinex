const userInput = document.getElementById("userInput");
const sendBtn = document.getElementById("sendBtn");
const chatContainer = document.getElementById("chat-container");

userInput.addEventListener("input", function () {
    this.style.height = 'auto';
    this.style.height = (this.scrollHeight) + 'px';
}, false);



userInput.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

sendBtn.addEventListener("click", sendMessage);

function sendMessage() {
    const text = userInput.value.trim();
    if (!text) return;

    appendMessage(text, "user");

    userInput.value = "";
    userInput.style.height = "auto";

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            type: "USER_INPUT",
            payload: text
        });
    }
}

function appendMessage(text, sender) {
    const div = document.createElement("div");
    div.className = `message ${sender}`;
    div.innerText = text;

    chatContainer.appendChild(div);
    chatContainer.scrollTop = chatContainer.scrollHeight;
}

window.chrome.webview.addEventListener("message", event => {
    const msg = event.data;

    if (msg.type === "AI_RESPONSE") {
        appendMessage(msg.payload, "ai");
    }
});
