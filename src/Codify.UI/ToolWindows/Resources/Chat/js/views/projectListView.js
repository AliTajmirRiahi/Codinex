/**
 * ProjectListView
 * Responsible for rendering conversation group project selector UI.
 */

import { $ } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState } from '../state/appState.js';

export const projectListView = {

    initialize(onProjectSelected, handleNewProject, handleDeleteProject) {
        this.handleNewProject = handleNewProject;
        this.handleDeleteProject = handleDeleteProject;

        this.projectDropDown = new DropDownView({
            containerId: 'project-dropdown-menu-container',
            menuId: 'project-dropdown',
            menuButtonId: 'project-selector-btn',
            itemTemplate: (item, isActive) => {
                const option = document.createElement('div');
                option.className = `drop-option ${isActive ? 'active' : ''}`;
                option.dataset.value = item.id;

                option.innerHTML = `
                    <div class="drop-info">
                        <codify-icon name="folder" class="chat-icon"></codify-icon>
                        <span>${item.name}</span>
                    </div>`;
                return option;
            },
            onItemSelect: (project) => {
                onProjectSelected(project);
                this.setCurrentProjectName(project);
                return true;
            }
        });

        const newProjectBtn = $('#new-project-btn');
        const deleteProjectBtn = $('#delete-project-btn');

        if (!newProjectBtn || !deleteProjectBtn) {
            throw new Error("ProjectListView initialization failed: Missing required DOM elements.");
            return;
        }

        deleteProjectBtn.addEventListener('click', () => {
            const appState = getState();

            if (!appState.currentGroup || appState.currentGroup.isDefault) return;

            this.handleDeleteProject(deleteProjectBtn);
        });

        newProjectBtn.addEventListener('click', () => {
            this.handleNewProject(newProjectBtn);
        });
    },

    setCurrentProjectName(project) {
        const appState = getState();
        const currentProject = project || appState.currentGroup;

        if (currentProject) {
            $('#project-name').innerHTML = currentProject.name;
        }
    },

    renderProjectListMenu(items, selectedValue) {
        this.projectDropDown.render(items, selectedValue);
        this.setCurrentProjectName();
    },
}
