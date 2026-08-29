/**
 * ComposerController
 * Orchestrates logic for triggers (@, /, #), menu items, and context chips.
 */
import {
    getState,
    setDraftText,
    setActiveTrigger,
    setActiveMenu,
    setSelectedCommand,
    setSelectedAgent,
    setSelectedReferences,
    setCursorContext,
    resetComposer,
    subscribe,
} from '../state/appState.js';
export class ComposerController {
    constructor(composerView) {

        //subscribe(() => {
        //    console.log(getState());
        //})

        this.view = composerView;

        // Mock data - In production, these come from your services
        this.data = {
            contexts: [
                {
                    id: 'ref-active-document',
                    name: 'Active Document',
                    description: () => {
                        var state = getState();
                        return state.activeDocument ? `${state.activeDocument.description}` : 'No active document to attach';
                    },
                    action: () => {
                        var state = getState();

                        if (!state.activeDocument) return;

                        this.handleSpecialDocument('references', state.activeDocument, 'Active Document');
                    }
                },
                {
                    id: 'ref-solution',
                    name: 'Solution',
                    description: 'Attach the entire solution',
                    action: () => {
                        var state = getState();

                        var solutionItem = _.filter(state.composerReferences, { type: 'Solution' });

                        if (!solutionItem || solutionItem.length == 0) return;

                        this.handleSpecialDocument('references', solutionItem[0], 'Solution');
                    }
                },
                {
                    id: 'ref-files',
                    name: 'Files',
                    description: 'Browse and attach files from the solution',
                    action: () => {
                        this.view.insertTextAtCursor('file');
                    }
                },
                {
                    id: 'ref-classes',
                    name: 'Classes',
                    description: 'List classes from the project for selection',
                    action: () => {
                        this.view.insertTextAtCursor('class');
                    }
                },
                {
                    id: 'ref-interfaces',
                    name: 'Interfaces',
                    description: 'List interfaces from the project for selection',
                    action: () => {
                        this.view.insertTextAtCursor('interface');
                    }
                },
                {
                    id: 'ref-methods',
                    name: 'Methods',
                    description: 'List methods from the current file or selection',
                    action: () => {
                        this.view.insertTextAtCursor('method');
                    }
                },
                {
                    id: 'ref-fields',
                    name: 'Fields',
                    description: 'List fields from the current file or selection',
                    action: () => {
                        this.view.insertTextAtCursor('field');
                    }
                },
                {
                    id: 'ref-output-logs',
                    name: 'Output Window logs',
                    description: 'Include recent messages from the Output window',
                },
                {
                    id: 'ref-mcp-prompts',
                    name: 'MCP prompts',
                    description: 'Insert saved MCP prompts as context',
                },
                {
                    id: 'ref-mcp-resources',
                    name: 'MCP resources',
                    description: 'Attach MCP-generated resources and artifacts',
                },
                {
                    id: 'ref-upload-image',
                    name: 'Upload Image',
                    description: () => this.currentModelSupportsVision()
                        ? 'Upload an image to include as context'
                        : 'Select a vision-capable model to include image context',
                    supportsVisionRequired: true,
                    action: () => {
                        if (!this.currentModelSupportsVision()) return;

                        this.uploadImageContext();
                    }
                },
                {
                    id: 'ref-auto-attach',
                    name: 'Auto-attach active document',
                    description: 'Automatically attach the active document when composing',
                    isToggle: true,
                    toggled: true
                }
            ],
            commands: [
                {
                    id: 'cmd1',
                    name: '/document',
                    description: 'Generate comments for the selected symbol or code'
                },
                {
                    id: 'cmd2',
                    name: '/describe',
                    description: 'Describe what the selected code does'
                },
                {
                    id: 'cmd3',
                    name: '/repair',
                    description: 'Find and repair issues in the selected code'
                },
                {
                    id: 'cmd4',
                    name: '/setupGuidelines',
                    description: 'Create project-level AI coding guidelines'
                },
                {
                    id: 'cmd5',
                    name: '/assist',
                    description: 'Show available Codinex commands and usage tips'
                },
                {
                    id: 'cmd6',
                    name: '/makeItBetter',
                    description: 'Generate suggestions to improve the selected code'
                },
                {
                    id: 'cmd7',
                    name: '/storePrompt',
                    description: 'Save the current prompt for later reuse'
                },
                {
                    id: 'cmd8',
                    name: '/createTests',
                    description: 'Generate tests for the selected code'
                },
            ],
            agents: [
                {
                    id: 'agent-python',
                    name: '@python-expert',
                    icon: 'Status/hat-glasses',
                    description: 'Best for Python'
                },
                {
                    id: 'agent-web',
                    name: '@web-dev',
                    icon: 'Status/monitor',
                    description: 'Frontend specialist'
                },
                {
                    id: 'agent-debugger',
                    name: '@debugger',
                    icon: 'Status/bug',
                    description: 'Diagnose and fix bugs'
                },
                {
                    id: 'agent-modernize',
                    name: '@modernize',
                    icon: 'Actions/refresh-cw',
                    description: 'Modernize your applications'
                },
                {
                    id: 'agent-profiler',
                    name: '@profiler',
                    icon: 'Status/activity',
                    description: 'Optimize your code'
                },
                {
                    id: 'agent-test',
                    name: '@test',
                    icon: 'Status/test-tube-diagonal',
                    description: 'Generate unit tests'
                },
                {
                    id: 'agent-vs',
                    name: '@vs',
                    icon: 'Branding/Visual-Studio-Icon-Flat',
                    description: 'Ask questions about Visual Studio'
                }
            ],
            references: []
        };

        this.bindEvents();
    }

    setRefrences() {
        var state = getState();

        this.data.references = state.composerReferences;
    }

    bindEvents() {
        // Handle menu selection from View
        document.addEventListener('composer:menu-select', (e) => {
            const { type, item, trigger } = e.detail;
            this.handleSelection(type, item, trigger);
        });

        // Handle chip removal
        document.addEventListener('composer:chip-remove', (e) => {
            this.removeChip(e.detail);
        });

        // Handle reference removal
        document.addEventListener('composer:ref-remove', (e) => {
            this.removeRef(e.detail);
        });

        document.addEventListener('paste', (e) => {
            this.handlePaste(e);
        });
    }

    /**
     * Main entry point called when input changes
     * @param {Object} context - { text, cursor, trigger }
     */
    handleInput(context) {
        setDraftText(this.view.getPlainText());

        if (this.view.getPlainText() == '') {
            setSelectedCommand(null);
            setSelectedAgent(null);
        }

        if (!context.trigger) {
            this.view.hideMenu();
            setActiveTrigger(null);
            setActiveMenu(null);
            return;
        }


        const state = getState();
        const { type, filter } = context.trigger;

        if (type != 'references' && (state.composer.selectedCommand != null || state.composer.selectedAgent != null)) return;

        setActiveTrigger(context.trigger);
        setActiveMenu(context.menuType);
        setCursorContext(context);

        const options = this.filterOptions(type, filter);

        if (options.length > 0) {
            this.view.showMenu(options, type, context.trigger);
        } else {
            this.view.hideMenu();
        }
    }
    /**
     * Main entry point called when input changes
     * @param {Object} context - { text, cursor, trigger }
     */
    handleContextInput(context) {
        if (!context.trigger) {
            this.view.hideMenu();
            setActiveTrigger(null);
            setActiveMenu(null);
            return;
        }


        const state = getState();
        const { type, filter } = context.trigger;

        setActiveTrigger(context.trigger);
        setActiveMenu(context.menuType);
        setCursorContext(context);

        const options = this.filterOptions(type, filter);

        if (options.length > 0) {
            this.view.showMenu(options, type, context.trigger);
        } else {
            this.view.hideMenu();
        }
    }

    handleContextClick(context) {
        var state = getState();

        if (!context.trigger || (state.composer.activeTrigger && state.composer.activeTrigger.symbol == '+')) {
            this.view.hideMenu();
            setActiveTrigger(null);
            setActiveMenu(null);
            return;
        }

        if (state.composer.activeTrigger)
            return;

        const { type, filter } = context.trigger;

        setActiveTrigger(context.trigger);
        setActiveMenu(context.menuType);
        setCursorContext(context);

        const options = this.filterOptions(type, filter);

        if (options.length > 0) {
            this.view.showMenu(options, type, context.trigger);
        } else {
            this.view.hideMenu();
        }
    }

    filterOptions(type, filter) {
        const list = this.data[type] || [];
        const trigger = getState().composer.activeTrigger;

        return list.filter(item => {
            if (item.supportsVisionRequired && !this.currentModelSupportsVision()) return false;

            const nameMatch = item.name.toLowerCase().includes(filter.toLowerCase());

            // If user typed #type:filter (e.g., #folder:), strict check the ReferenceKind
            if (trigger && trigger.typeFilter) {
                const kindMatch = item.type && item.type.toLowerCase() === trigger.typeFilter.toLowerCase();
                return kindMatch && nameMatch;
            }

            // Default behavior for #filter
            return nameMatch || (item.type && item.type.toLowerCase().includes(filter.toLowerCase()));
        });
    }

    currentModelSupportsVision() {
        const state = getState();
        const support = state.currentModel?.supportsVision ?? state.currentModel?.SupportsVision;

        return support === 'Supported' || support === 0;
    }

    handleSelection(type, item, trigger) {

        // Hide menu and clear menu selection state
        this.view.hideMenu();

        // Sync with AppState (we'll complete this in a later step)
        setActiveMenu(null);
        setActiveTrigger(null);


        // Define strategies for different item types to clean up conditional logic
        const selectionStrategies = {
            contexts: (item) => {
                if (!item.action) return;
                item.action();
                return { shouldInsertChip: false, updateRefs: false };
            },
            commands: (item) => {
                setSelectedCommand(item);
                return { shouldInsertChip: true };
            },
            agents: (item) => {
                setSelectedAgent(item);
                return { shouldInsertChip: true };
            },
            references: (item) => {
                const state = getState();
                const itemId = item.id || item.Id;
                const alreadySelected = itemId && state.composer.selectedReferences.some(i => (i.id || i.Id) === itemId);

                if (alreadySelected) {
                    return { shouldInsertChip: false, updateRefs: false };
                }

                const newRefs = [...state.composer.selectedReferences, item];
                setSelectedReferences(newRefs);
                return { shouldInsertChip: true, updateRefs: true };
            }
        };

        // Execute the strategy based on type
        const strategy = selectionStrategies[type];
        const result = strategy ? strategy(item) : { shouldInsertChip: false };

        if (result.shouldInsertChip) {
            // Insert chip into the view
            this.view.insertChip({
                id: item.id || item.Id,
                text: item.name || item.Name || item.text,
                type: type,
                icon: item.icon || item.Icon,
                trigger: trigger
            });
        }

        if (result.updateRefs) {
            // Update reference chips specifically
            const state = getState();
            this.view.updateReferenceChips(state.composer.selectedReferences);
        }

    }

    handleSpecialDocument(type, item, name) {
        const state = getState();

        const itemId = item.id || item.Id;
        const newRefs = [
            item,
            ...state.composer.selectedReferences.filter(i =>
                i.name != name && (!itemId || (i.id || i.Id) !== itemId))
        ];

        setSelectedReferences(newRefs);

        this.view.updateReferenceChips(newRefs);

        // Hide menu and clear menu selection state
        this.view.hideMenu();

        // Sync with AppState (we'll complete this in a later step)
        setActiveMenu(null);
        setActiveTrigger(null);
    }

    removeActiveDocumentReference() {
        const state = getState();
        const activeDocument = state.activeDocument;

        const remainingRefs = state.composer.selectedReferences.filter(item => {
            const itemName = item.name || item.Name;
            const itemValue = item.value || item.Value;
            const activeDocumentValue = activeDocument?.value || activeDocument?.Value;

            return itemName !== 'Active Document' &&
                (!activeDocumentValue || itemValue !== activeDocumentValue);
        });

        if (remainingRefs.length === state.composer.selectedReferences.length) return;

        setSelectedReferences(remainingRefs);
        this.view.updateReferenceChips(remainingRefs);
    }

    handleFileContext(type, item, name) {
        const state = getState();

        const newRefs = [item, ...state.composer.selectedReferences.filter(i => i.name != name)];

        setSelectedReferences(newRefs);

        this.view.updateReferenceChips(newRefs);

        // Hide menu and clear menu selection state
        this.view.hideMenu();

        // Sync with AppState (we'll complete this in a later step)
        setActiveMenu(null);
        setActiveTrigger(null);
    }

    async handlePaste(e) {
        if (!this.currentModelSupportsVision()) return;

        const clipboardData = e.clipboardData || window.clipboardData;
        if (!clipboardData) return;

        const files = [];

        if (clipboardData.items && clipboardData.items.length) {
            for (const item of clipboardData.items) {
                if (item.kind !== 'file' || !item.type || !item.type.startsWith('image/')) continue;

                const file = item.getAsFile();
                if (file) files.push(file);
            }
        }

        if (!files.length && clipboardData.files && clipboardData.files.length) {
            for (const file of clipboardData.files) {
                if (file && file.type && file.type.startsWith('image/')) files.push(file);
            }
        }

        if (!files.length) return;

        e.preventDefault();

        for (const file of files) {
            await this.addImageReference(file);
        }
    }

    uploadImageContext() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.multiple = true;
        input.style.display = 'none';

        input.addEventListener('change', async () => {
            const files = Array.from(input.files || []);

            for (const file of files) {
                await this.addImageReference(file);
            }

            input.remove();
        });

        document.body.appendChild(input);
        input.click();
    }

    async addImageReference(file) {
        if (!file || !file.type || !file.type.startsWith('image/')) return;

        const dataUrl = await this.readFileAsDataUrl(file);
        const separatorIndex = dataUrl.indexOf(',');
        const base64Content = separatorIndex >= 0 ? dataUrl.substring(separatorIndex + 1) : dataUrl;

        const imageRef = {
            id: `image-${Date.now()}-${Math.random().toString(36).slice(2)}`,
            name: file.name,
            description: 'Uploaded image context',
            type: 'Image',
            icon: 'fileTypes/file_type_image',
            color: '--vscode-charts-blue',
            value: file.name,
            metadata: {
                filePath: file.name,
                signature: file.type,
                body: dataUrl,
                content: base64Content
            }
        };

        const state = getState();
        const newRefs = [...state.composer.selectedReferences, imageRef];

        setSelectedReferences(newRefs);
        this.view.updateReferenceChips(newRefs);

        this.view.hideMenu();
        setActiveMenu(null);
        setActiveTrigger(null);
    }

    readFileAsDataUrl(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = () => resolve(reader.result);
            reader.onerror = () => reject(reader.error);

            reader.readAsDataURL(file);
        });
    }

    removeChip(item) {

        // Sync the specific category with AppState
        if (item.type === 'agents') {
            setSelectedAgent(null);
        }
        else if (item.type === 'references') {
            const state = getState();
            const itemId = item.id || item.Id;

            const remainingRefs = state.composer.selectedReferences.filter(i => (i.id || i.Id) !== itemId);

            setSelectedReferences(remainingRefs);
            this.view.updateReferenceChips(remainingRefs);
        }
        else if (item.type === 'commands') {
            setSelectedCommand(null);
        }
    }

    removeRef(item) {
        const state = getState();
        const itemId = item.id || item.Id;
        // Sync the specific category with AppState
        const remainingRefs = state.composer.selectedReferences.filter(i => (i.id || i.Id) !== itemId);
        setSelectedReferences(remainingRefs);

        // Sync draft text if necessary (or re-parse)
        // The view will handle the DOM removal, but if your draftText depends on
        // these tokens, you might need to trigger a re-parse here.

        this.view.removeRefNode(itemId);
    }

    resetComposer() {
        setDraftText("");
        setActiveTrigger(null);
        setActiveMenu(null);
        setSelectedCommand(null);
        setSelectedAgent(null);
        setSelectedReferences([]);
        setCursorContext(null);

        this.view.updateReferenceChips([]);
    }
}
