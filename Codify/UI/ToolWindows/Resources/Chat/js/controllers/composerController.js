/**
 * ComposerController
 * Orchestrates logic for triggers (@, /, #), menu items, and context chips.
 */

export class ComposerController {
    constructor(composerView) {
        this.view = composerView;

        // Mock data - In production, these come from your services
        this.data = {
            commands: [
                {
                    name: '/document',
                    description: 'Generate documentation for the selected symbol or code'
                },
                {
                    name: '/describe',
                    description: 'Describe what the selected code does'
                },
                {
                    name: '/repair',
                    description: 'Find and repair issues in the selected code'
                },
                {
                    name: '/setupGuidelines',
                    description: 'Create project-level AI coding guidelines'
                },
                {
                    name: '/assist',
                    description: 'Show available Codify commands and usage tips'
                },
                {
                    name: '/improve',
                    description: 'Improve code quality, readability, and performance'
                },
                {
                    name: '/storePrompt',
                    description: 'Save the current prompt for later reuse'
                },
                {
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
            return;
        }

        const { type, filter } = context.trigger;
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
        // 1. Add to chips
        this.selectedItems.push({ ...item, type });
        this.view.renderChips(this.selectedItems);

        // 2. Clear menu
        this.view.hideMenu();

        // 3. Optional: Clear the trigger text from input
        console.log(`Selected ${type}: ${item.name}`);
    }

    removeChip(item) {
        this.selectedItems = this.selectedItems.filter(i => i.name !== item.name);
        this.view.renderChips(this.selectedItems);
    }
}
