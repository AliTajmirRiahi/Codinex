/**
 * ProjectListView
 * Responsible for rendering conversation group project selector UI.
 */

import { $, togglePanelHidden } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState } from '../state/appState.js';

export const projectListView = {

    initialize(onProjectSelected, handleNewProject, handleDeleteProject, handleEditProject) {
        this.handleNewProject = handleNewProject;
        this.handleDeleteProject = handleDeleteProject;
        this.handleEditProject = handleEditProject;

        this.projectDropDown = new DropDownView({
            containerId: 'project-dropdown-menu-container',
            menuId: 'project-dropdown',
            menuButtonId: 'project-selector-btn',
            itemTemplate: (item, isActive) => {
                const option = document.createElement('div');
                option.className = `drop-option ${isActive ? 'active' : ''}`;
                option.dataset.value = item.id;

                const projectDate = this.isDefaultProject(item) ? '' : this.formatProjectDateTime(item);

                option.innerHTML = `
                    <div class="drop-row">
                        <div class="col-main">
                            <codinex-icon name="Status/folder" class="chat-icon"></codinex-icon>
                            <span class="chat-title">${this.escapeHtml(item.name)}</span>
                        </div>
                        <span class="col-date">${projectDate}</span>
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
        const editProjectBtn = $('#edit-project-btn');

        if (!newProjectBtn || !deleteProjectBtn || !editProjectBtn) {
            throw new Error("ProjectListView initialization failed: Missing required DOM elements.");
            return;
        }

        this.initializeProjectModal();
        this.initializeDeleteProjectModal();

        deleteProjectBtn.addEventListener('click', () => {
            const appState = getState();

            if (!appState.currentGroup || appState.currentGroup.isDefault) return;

            this.showDeleteProjectModal(appState.currentGroup);
        });

        editProjectBtn.addEventListener('click', () => {
            const appState = getState();
            if (!appState.currentGroup || appState.currentGroup.isDefault) return;
            this.showProjectModal(appState.currentGroup);
        });

        newProjectBtn.addEventListener('click', () => {
            this.showProjectModal();
        });
    },

    initializeProjectModal() {
        const modal = $('#project-management-modal');
        const nameInput = $('#project-modal-name');
        const descriptionInput = $('#project-modal-description');
        const closeBtn = $('#close-project-modal');
        const cancelBtn = $('#cancel-project-modal');
        const saveBtn = $('#save-project-modal');
        
        const modalTitle = modal ? modal.querySelector('.modal-header h2') : null;

        if (!modal || !nameInput || !descriptionInput || !closeBtn || !cancelBtn || !saveBtn || !modalTitle) {
            throw new Error("ProjectListView initialization failed: Missing project modal DOM elements.");
        }

        this._isEditMode = false;
        this._editingProjectId = null;

        const closeModal = () => this.hideProjectModal();

        closeBtn.addEventListener('click', closeModal);
        cancelBtn.addEventListener('click', closeModal);

        saveBtn.addEventListener('click', () => {
            const name = nameInput.value.trim();
            const description = descriptionInput.value.trim();

            if (!name) {
                nameInput.focus();
                return;
            }

            const isEditMode = this._isEditMode;
            const editingProjectId = this._editingProjectId;

            this.hideProjectModal();

            if (isEditMode && this.handleEditProject) {
                this.handleEditProject({ id: editingProjectId, name, description });
            } else {
                this.handleNewProject({ name, description });
            }
        });
    },

    initializeDeleteProjectModal() {
        const modal = $('#delete-project-modal');
        const closeBtn = $('#close-delete-project-modal');
        const cancelBtn = $('#cancel-delete-project-modal');
        const confirmBtn = $('#confirm-delete-project-modal');

        if (!modal || !closeBtn || !cancelBtn || !confirmBtn) {
            throw new Error("ProjectListView initialization failed: Missing delete project modal DOM elements.");
        }

        const closeModal = () => this.hideDeleteProjectModal();

        closeBtn.addEventListener('click', closeModal);
        cancelBtn.addEventListener('click', closeModal);

        confirmBtn.addEventListener('click', () => {
            this.hideDeleteProjectModal();
            this.handleDeleteProject();
        });
    },

    showProjectModal(project) {
        const nameInput = $('#project-modal-name');
        const descriptionInput = $('#project-modal-description');
        const modal = $('#project-management-modal');
        const modalTitle = modal ? modal.querySelector('.modal-header h2') : null;
        const saveBtn = $('#save-project-modal');

        if (project) {
            nameInput.value = project.name || '';
            descriptionInput.value = project.description || '';
            if (modalTitle) modalTitle.textContent = 'Edit Project';
            if (saveBtn) saveBtn.textContent = 'Save Changes';
            this._isEditMode = true;
            this._editingProjectId = project.id || project.Id || null;
        } else {
            nameInput.value = '';
            descriptionInput.value = '';
            if (modalTitle) modalTitle.textContent = 'New Project';
            if (saveBtn) saveBtn.textContent = 'Create Project';
            this._isEditMode = false;
            this._editingProjectId = null;
        }

        togglePanelHidden('#project-management-modal', true);
        nameInput.focus();
    },

    hideProjectModal() {
        this._isEditMode = false;
        this._editingProjectId = null;
        togglePanelHidden('#project-management-modal', false);
    },

    showDeleteProjectModal(project) {
        const message = $('#delete-project-message');

        if (message) {
            message.textContent = `Delete project "${project.name}" and all chats under it?`;
        }

        togglePanelHidden('#delete-project-modal', true);
    },

    hideDeleteProjectModal() {
        togglePanelHidden('#delete-project-modal', false);
    },

    setCurrentProjectName(project) {
        const appState = getState();
        const currentProject = project || appState.currentGroup;

        if (currentProject) {
            $('#project-name').innerHTML = currentProject.name;
        }
    },

    renderProjectListMenu(items, selectedValue) {
        const sortedItems = this.sortProjectsByDateDesc(items);

        this.projectDropDown.render(sortedItems, selectedValue);
        this.setCurrentProjectName();
    },

    sortProjectsByDateDesc(items) {
        if (!Array.isArray(items)) return [];

        const defaultProjects = items.filter((item) => this.isDefaultProject(item));
        const projects = items.filter((item) => !this.isDefaultProject(item));

        projects.sort((a, b) => {
            const dateA = this.getProjectDate(a);
            const dateB = this.getProjectDate(b);

            return (dateB ? dateB.getTime() : 0) - (dateA ? dateA.getTime() : 0);
        });

        return [...defaultProjects, ...projects];
    },

    isDefaultProject(project) {
        return !!(project && (project.isDefault || project.IsDefault));
    },

    getProjectDate(project) {
        const value =  project.createdAt || project.CreatedAt;

        if (!value) return null;

        const date = new Date(value);

        return isNaN(date.getTime()) ? null : date;
    },

    formatProjectDateTime(project) {
        const date = this.getProjectDate(project);

        if (!date) return '';

        return date.toLocaleString([], {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = value || '';

        return div.innerHTML;
    },
}
