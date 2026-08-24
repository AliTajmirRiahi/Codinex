/*
 * path: Codinex\UI\ToolWindows\Resources\Chat\js\views\addProviderView.js
 */
import { $, togglePanelHidden } from '../utils/dom.js';
import { validationService } from '../services/validationService.js';

const FIELD_TARGETS = [
    '#add-provider-name',
    '#add-provider-protocol',
    '#add-provider-base-url',
    '#add-provider-model-endpoint-url',
    '#add-provider-api-key',
];

/**
 * Derives the relative ModelEndPoint (e.g. "/models") from the full model endpoint
 * URL the user typed, by stripping the Base URL prefix off of it.
 * e.g. baseUrl "https://api.openai.com/v1", modelEndpointUrl "https://api.openai.com/v1/models"
 * -> "/models". Returns null if the endpoint URL doesn't actually start with the base URL.
 */
function deriveModelEndPoint(baseUrl, modelEndpointUrl) {
    const base = (baseUrl || '').trim().replace(/\/+$/, '');
    const full = (modelEndpointUrl || '').trim().replace(/\/+$/, '');

    if (!base || !full) return null;
    if (full.toLowerCase() === base.toLowerCase()) return null;
    if (!full.toLowerCase().startsWith(base.toLowerCase())) return null;

    const relative = full.slice(base.length);
    if (!relative.startsWith('/')) return null;

    return relative;
}

/**
 * Manages the "Add Custom Provider" modal: form state, validation,
 * and loading/error presentation while the provider is being verified.
 */
export const addProviderView = {
    initEventHandlers(saveCallBack) {
        const needApiKeyCheckbox = $('#add-provider-need-api-key');
        if (needApiKeyCheckbox) {
            needApiKeyCheckbox.addEventListener('change', () => {
                togglePanelHidden('#add-provider-api-key-group', needApiKeyCheckbox.checked);
            });
        }

        $('#save-add-provider-btn').onclick = () => {
            const raw = this.getFormData();

            const rules = [
                { field: 'name', validator: validationService.isNotEmpty, message: 'Provider name is required.', mode: 'inline', target: '#add-provider-name' },
                { field: 'protocol', validator: validationService.isSelected, message: 'Please select a protocol.', mode: 'inline', target: '#add-provider-protocol' },
                { field: 'baseUrl', validator: validationService.isNotEmpty, message: 'Base URL is required.', mode: 'inline', target: '#add-provider-base-url' },
                {
                    field: 'modelEndpointUrl',
                    validator: (value) => !!deriveModelEndPoint(raw.baseUrl, value),
                    message: 'Model Endpoint must be the full URL and must start with the Base URL above.',
                    mode: 'inline',
                    target: '#add-provider-model-endpoint-url'
                },
            ];

            if (raw.needApiKey) {
                rules.push({ field: 'apiKey', validator: validationService.isNotEmpty, message: 'API key is required.', mode: 'inline', target: '#add-provider-api-key' });
            }

            const validation = validationService.validate(raw, { rules });

            if (!validation.valid) {
                validationService.showErrors(validation.errors);
                return;
            }

            const data = {
                name: raw.name,
                protocol: raw.protocol,
                baseUrl: raw.baseUrl,
                modelEndPoint: deriveModelEndPoint(raw.baseUrl, raw.modelEndpointUrl),
                needApiKey: raw.needApiKey,
                apiKey: raw.apiKey,
            };

            this.clearErrors();
            this.setLoading(true);

            if (saveCallBack) saveCallBack(data);
        };
    },

    getFormData() {
        return {
            name: $('#add-provider-name').value.trim(),
            protocol: $('#add-provider-protocol').value,
            baseUrl: $('#add-provider-base-url').value.trim(),
            modelEndpointUrl: $('#add-provider-model-endpoint-url').value.trim(),
            needApiKey: $('#add-provider-need-api-key').checked,
            apiKey: $('#add-provider-api-key').value.trim(),
        };
    },

    clearErrors() {
        FIELD_TARGETS.forEach(target => validationService.clearInlineError(target));
    },

    resetForm() {
        $('#add-provider-name').value = '';
        $('#add-provider-protocol').value = 'openai';
        $('#add-provider-base-url').value = '';
        $('#add-provider-model-endpoint-url').value = '';
        $('#add-provider-need-api-key').checked = true;
        $('#add-provider-api-key').value = '';
        togglePanelHidden('#add-provider-api-key-group', true);
        this.clearErrors();
    },

    setLoading(isLoading) {
        togglePanelHidden('#add-provider-loading-screen', !!isLoading);
    },

    showError(message) {
        this.setLoading(false);

        validationService.showError({
            message: message || 'Provider could not be added.',
            mode: 'inline',
            target: '#add-provider-model-endpoint-url'
        });
    },

    /**
     * Shows the Add Custom Provider modal.
     */
    show() {
        this.resetForm();
        togglePanelHidden('#add-provider-modal', true);
    },

    /**
     * Hides the Add Custom Provider modal. Only called after a successful save
     * (or an explicit cancel) — never automatically while a save is pending.
     */
    hide() {
        this.setLoading(false);
        togglePanelHidden('#add-provider-modal', false);
    },
};
