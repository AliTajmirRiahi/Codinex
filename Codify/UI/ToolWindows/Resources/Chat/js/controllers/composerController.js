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

        subscribe(() => {
            console.log(getState());
        })

        this.view = composerView;

        // Mock data - In production, these come from your services
        this.data = {
            commands: [
                {
                    id: 'cmd1',
                    name: '/document',
                    description: 'Generate documentation for the selected symbol or code'
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
                    description: 'Show available Codify commands and usage tips'
                },
                {
                    id: 'cmd6', 
                    name: '/improve',
                    description: 'Improve code quality, readability, and performance'
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
                }
            ],
            agents: [{ name: '@python-expert', description: 'Best for Python' }, { name: '@web-dev', description: 'Frontend specialist' }],
            references: [{ name: '#file1.js', description: 'Current file' }, { name: '#components.js', description: 'Dependency' }]
        };

        this.selectedItems = []; // Current chips in composer
        this.bindEvents();
    }

    bindEvents() {
        // Handle menu selection from View
        document.addEventListener('composer:menu-select', (e) => {
            const { type, item } = e.detail;
            this.handleSelection(type, item);
        });

        // Handle chip removal
        document.addEventListener('composer:chip-remove', (e) => {
            this.removeChip(e.detail);
        });
    }

    /**
     * Main entry point called when input changes
     * @param {Object} context - { text, cursor, trigger }
     */
    handleInput(context) {
        if (!context.trigger) {
            this.view.hideMenu();
            setActiveTrigger(null);
            setActiveMenu(null);
            return;
        }

        const state = getState();
        const { type, filter } = context.trigger;

        if ((type === 'commands' && state.composer.selectedCommand != null) || (type === 'agents' && state.composer.selectedAgent != null)) return;

        setActiveTrigger(context.trigger);
        setActiveMenu(context.menuType);
        setCursorContext(context);
        
        const options = this.filterOptions(type, filter);

        if (options.length > 0) {
            this.view.showMenu(options, type);
        } else {
            this.view.hideMenu();
        }
    }

    filterOptions(type, filter) {
        const list = this.data[type] || [];
        return list.filter(item =>
            item.name.toLowerCase().includes(filter.toLowerCase())
        );
    }

    handleSelection(type, item) {
        console.log(`Selecting ${type}:`, item);

        // 2. Insert chip into the view (replaces the typed trigger)
        // The insertChip method we implemented in the view handles selection and DOM insertion
        this.view.insertChip({
            id: item.id,
            text: item.name || item.text,
            type: type
        });


        if (type === 'commands') {
            setSelectedCommand(item);
        } else if (type === 'agents') {
            setSelectedAgent(item);
        } else if (type === 'reference') {
            const newRefs = [...this.selectedItems.filter(i => i.type === 'reference'), item];
            setSelectedReferences(newRefs);
        }

        // 3. Update the selected items list in the controller (for quick access)
        // Note: in the new model the DOM is the source of truth, but keeping this list
        // is useful to quickly send data to the AI
        this.selectedItems.push({ ...item, type });

        // 4. Hide menu and clear menu selection state
        this.view.hideMenu();

        // 5. Sync with AppState (we'll complete this in a later step)
        setActiveMenu(null);
        setActiveTrigger(null);
    }

    removeChip(item) {
        // 1. Remove from local tracking array using ID (safer than name)
        this.selectedItems = this.selectedItems.filter(i => i.id !== item.id);

        // 2. Sync the specific category with AppState
        if (item.type === 'agents') {
            setSelectedAgent(null);
        }
        else if (item.type === 'references') {
            const remainingRefs = this.selectedItems.filter(i => i.type === 'references');
            setSelectedReferences(remainingRefs);
        }
        else if (item.type === 'commands') {
            setSelectedCommand(null);
        }

        // Sync draft text if necessary (or re-parse)
        // The view will handle the DOM removal, but if your draftText depends on
        // these tokens, you might need to trigger a re-parse here.
    }
}
