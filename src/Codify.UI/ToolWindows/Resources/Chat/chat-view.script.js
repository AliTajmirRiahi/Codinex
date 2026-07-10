const userInput = document.getElementById("userInput");
const sendBtn = document.getElementById("sendBtn");
const chatContainer = document.getElementById("chat-container");
const settingBtn = document.getElementById("settingBtn");

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

    appendMessageToChat(text, "user");

    userInput.value = "";
    userInput.style.height = "auto";

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
            type: "USER_INPUT",
            payload: text
        });
    }
}

function appendMessageToChat(text, sender) {
    const div = document.createElement("div");
    div.className = `message ${sender}`;
    div.innerText = text;

    chatContainer.appendChild(div);
    chatContainer.scrollTop = chatContainer.scrollHeight;
}

settingBtn.addEventListener("click", openSettingsModal);

function openSettingsModal() {
    document.getElementById("settings-modal").classList.remove("hidden");
}

function closeSettingsModal() {
    document.getElementById("settings-modal").classList.add("hidden");
}

document.getElementById("close-settings")
    .addEventListener("click", closeSettingsModal);

document.getElementById("model-api-key").addEventListener("input", toggleSaveButton);


// --- Global Variables ---
let allProvidersData = []; // To store all provider data fetched from C#

// --- UI Functions ---

/**
 * Populates the provider dropdown (#provider-select)
 * @param {Array} providers - Full list of AI providers
 */
function populateProviderSelector(providers) {
    allProvidersData = providers;

    const providerSelect = document.getElementById('provider-select');
    if (!providerSelect) {
        console.error("Provider selector element not found!");
        return;
    }

    providerSelect.innerHTML = ''; // Clear existing options

    // Add a default/placeholder option
    const defaultOption = document.createElement('option');
    defaultOption.value = "";
    defaultOption.textContent = "Select a provider...";
    defaultOption.disabled = true;
    defaultOption.selected = true;
    providerSelect.appendChild(defaultOption);

    providers.forEach(provider => {
        const option = document.createElement('option');
        option.value = provider.id; // Use provider's ID as value
        option.textContent = provider.name;
        providerSelect.appendChild(option);
    });

    // Add event listener to the provider selector
    providerSelect.addEventListener('change', handleProviderChange);

    // If there's enabled provider, select it automatically
    checkFirstRunAndInitialize();
}

// Global State for Pagination
let currentProviderModels = [];
let currentPage = 1;
const itemsPerPage = 5;
let selectedProvider = null;
let selectedModels = new Map(); // Stores selected models across pages

// Update handleProviderChange to initialize pagination
function handleProviderChange() {
    const providerId = document.getElementById('provider-select').value;
    selectedProvider = allProvidersData.find(p => p.id === providerId);

    if (selectedProvider && selectedProvider.models) {
        currentProviderModels = selectedProvider.models;
        currentPage = 1;
        renderModelPage();
        document.getElementById('model-pagination').classList.remove('hidden');
    } else {
        currentProviderModels = [];
        document.getElementById('models-checkbox-list').innerHTML = 'Please select a provider...';
        document.getElementById('model-pagination').classList.add('hidden');
    }
}

// Function to render exactly 5 models based on current page

function renderModelPage() {
    const listContainer = document.getElementById('models-checkbox-list');
    listContainer.innerHTML = '';

    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    const pageItems = currentProviderModels.slice(start, end);

    pageItems.forEach(model => {
        const item = document.createElement('div');
        item.className = 'model-item';

        const isChecked = selectedModels.has(model.id);

        item.innerHTML = `
            <input type="checkbox" id="model-${model.id}" value="${model.id}" ${isChecked ? 'checked' : ''}>
            <label for="model-${model.id}">
                ${model.name} <span style="opacity:0.6; font-size:0.9em;">(Limit: ${model.tokenLimit ?? 'N/A'})</span>
            </label>
        `;

        item.addEventListener('click', (e) => {
            const clickedCheckbox = item.querySelector('input[type="checkbox"]');
            if (!clickedCheckbox) return;

            const model = currentProviderModels.find(p => p.id === clickedCheckbox.value);
            if (!model) return;

            if (selectedModels.has(model.id)) {
                selectedModels.delete(model.id);
                clickedCheckbox.checked = false;
            } else {
                selectedModels.set(model.id, model);
                clickedCheckbox.checked = true;
            }

            toggleSaveButton();
        });

        // Also handle direct checkbox changes
        const checkbox = item.querySelector('input[type="checkbox"]');
        checkbox.addEventListener('change', () => {
            const model = currentProviderModels.find(p => p.id === checkbox.value);
            if (!model) return;

            if (checkbox.checked) {
                selectedModels.set(model.id, model);
            } else {
                selectedModels.delete(model.id);
            }

            toggleSaveButton();
        });

        listContainer.appendChild(item);
    });

    updatePaginationUI();
}
function toggleSaveButton() {
    const apiKeyInput = document.getElementById('model-api-key');
    if (selectedModels.size > 0 && apiKeyInput.value)
        document.getElementById('save-settings-btn').classList.remove('disable');
    else
        document.getElementById('save-settings-btn').classList.add('disable');
}

function updatePaginationUI() {
    const totalPages = Math.ceil(currentProviderModels.length / itemsPerPage);
    document.getElementById('page-info').textContent = `Page ${currentPage} of ${totalPages || 1}`;
    document.getElementById('prev-page').disabled = currentPage === 1;
    document.getElementById('next-page').disabled = currentPage === totalPages || totalPages === 0;
}

// Event Listeners for Pager Buttons
document.getElementById('prev-page').onclick = () => {
    if (currentPage > 1) {
        currentPage--;
        renderModelPage();
    }
};

document.getElementById('next-page').onclick = () => {
    const totalPages = Math.ceil(currentProviderModels.length / itemsPerPage);
    if (currentPage < totalPages) {
        currentPage++;
        renderModelPage();
    }
};


function checkFirstRunAndInitialize() {
    selectedProvider = allProvidersData.find(p => p.isEnabled);

    if (selectedProvider != null) return;

    openSettingsModal();
}

// --- Event Listener for "Add Custom Model" button ---
// You'll need to implement the logic to show the custom model form here
document.getElementById('btn-show-add-custom')?.addEventListener('click', () => {
    console.log("Show add custom model form...");
    // Implement logic to display the custom model form (likely part of the settings modal)
    // openSettingsModal('add-custom'); // Example: pass a state to the modal
});
