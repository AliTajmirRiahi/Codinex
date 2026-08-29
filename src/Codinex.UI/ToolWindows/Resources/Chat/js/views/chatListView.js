/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 */

import { $, togglePanelHidden } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState } from '../state/appState.js';

export const chatListView = {

    initialize(onChatSelected, handleNewChat, handleDeleteChat, handleEditChat) {
        this.handleNewChat = handleNewChat;
        this.handleDeleteChat = handleDeleteChat;
        this.handleEditChat = handleEditChat;
        // Initialize
        this.modelDropDown = new DropDownView({
            containerId: 'chat-history-dropdown-menu-container',
            menuId: 'chat-history-dropdown',
            menuButtonId: 'chat-history-selector-btn',
            itemTemplate: (item, isActive) => {
                const option = document.createElement('div');
                option.className = `drop-option ${isActive ? 'active' : ''}`;
                option.dataset.value = item.id;

                const chatDate = this.formatChatDateTime(item);

                option.innerHTML = `
                    <div class="drop-row">
                        <div class="col-main">
                            <codinex-icon name="Status/message-circle-check" class="chat-icon"></codinex-icon>
                            <span class="chat-title">${this.escapeHtml(item.title)}</span>
                        </div>
                        <span class="col-date">${chatDate}</span>
                    </div>`;
                return option;
            },
            onItemSelect: (chat) => {
                onChatSelected(chat);
                this.setCurrentChatName();
                return true;
            }
        });

        const newChatBtn = $('#new-chat-btn');
        const deleteChatBtn = $('#delete-chat-btn');
        const editChatBtn = $('#edit-chat-btn');

        if (!newChatBtn || !deleteChatBtn || !editChatBtn) {
            throw new Error("ChatListView initialization failed: Missing required DOM elements.");
            return;
        }

        this.initializeChatModal();

        deleteChatBtn.addEventListener('click', () => {
            var appState = getState();

            if (appState.currentChat.isNewChat) return;

            this.handleDeleteChat(deleteChatBtn);
        });

        editChatBtn.addEventListener('click', () => {
            var appState = getState();

            if (!appState.currentChat || appState.currentChat.isNewChat) return;

            this.showChatModal(appState.currentChat);
        });

        newChatBtn.addEventListener('click', () => {
            var appState = getState();

            if (appState.currentChat.isNewChat) return;

            this.handleSendMessage(newChatBtn);
        });
    },

    initializeChatModal() {
        const modal = $('#chat-management-modal');
        const nameInput = $('#chat-modal-name');
        const closeBtn = $('#close-chat-modal');
        const cancelBtn = $('#cancel-chat-modal');
        const saveBtn = $('#save-chat-modal');

        if (!modal || !nameInput || !closeBtn || !cancelBtn || !saveBtn) {
            throw new Error("ChatListView initialization failed: Missing chat modal DOM elements.");
        }

        this._editingChatId = null;

        const closeModal = () => this.hideChatModal();

        closeBtn.addEventListener('click', closeModal);
        cancelBtn.addEventListener('click', closeModal);

        nameInput.addEventListener('input', () => {
            if (nameInput.value.length > 25) {
                nameInput.value = nameInput.value.substring(0, 25);
            }
        });

        saveBtn.addEventListener('click', () => {
            let name = nameInput.value.trim();

            if (!name) {
                nameInput.focus();
                return;
            }

            if (name.length > 25) {
                name = name.substring(0, 25);
            }

            const editingChatId = this._editingChatId;

            this.hideChatModal();

            if (editingChatId && this.handleEditChat) {
                this.handleEditChat({ id: editingChatId, title: name });
            }
        });
    },

    showChatModal(chat) {
        if (!chat || chat.isNewChat || chat.IsNewChat) return;

        const nameInput = $('#chat-modal-name');

        nameInput.value = (chat.title || chat.Title || '').substring(0, 25);
        this._editingChatId = chat.id || chat.Id || null;

        togglePanelHidden('#chat-management-modal', true);
        nameInput.focus();
    },

    hideChatModal() {
        this._editingChatId = null;
        togglePanelHidden('#chat-management-modal', false);
    },

    // updates current model name
    setCurrentChatName() {
        var appState = getState();
        $('#chat-history-name').textContent = appState.currentChat.title;
    },

    renderChatListMenu(items, selectedValue) {
        const sortedItems = this.sortChatsByDateDesc(items);

        this.modelDropDown.render(sortedItems, selectedValue);
        this.setCurrentChatName();
    },

    sortChatsByDateDesc(items) {
        if (!Array.isArray(items)) return [];

        return [...items].sort((a, b) => {
            const dateA = this.getChatDate(a);
            const dateB = this.getChatDate(b);

            return (dateB ? dateB.getTime() : 0) - (dateA ? dateA.getTime() : 0);
        });
    },

    getChatDate(chat) {
        const value = chat.createdAt || chat.CreatedAt;

        if (!value) return null;

        const date = new Date(value);

        return isNaN(date.getTime()) ? null : date;
    },

    formatChatDateTime(chat) {
        const date = this.getChatDate(chat);

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

    handleSendMessage(input) {
        this.handleNewChat();
    },
}

