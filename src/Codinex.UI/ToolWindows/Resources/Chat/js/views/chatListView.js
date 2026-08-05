/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 */

import { $ } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState } from '../state/appState.js';

export const chatListView = {

    initialize(onChatSelected, handleNewChat, handleDeleteChat) {
        this.handleNewChat = handleNewChat;
        this.handleDeleteChat = handleDeleteChat;
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
                            <codinex-icon name="message-circle-check" class="chat-icon"></codinex-icon>
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

        if (!newChatBtn || !deleteChatBtn) {
            throw new Error("ChatListView initialization failed: Missing required DOM elements.");
            return;
        }

        deleteChatBtn.addEventListener('click', () => {
            var appState = getState();

            if (appState.currentChat.isNewChat) return;

            this.handleDeleteChat(deleteChatBtn);
        });

        newChatBtn.addEventListener('click', () => {
            var appState = getState();

            if (appState.currentChat.isNewChat) return;

            this.handleSendMessage(newChatBtn);
        });
    },
    // updates current model name
    setCurrentChatName() {
        var appState = getState();
        $('#chat-history-name').innerHTML = appState.currentChat.title;
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
        const value = chat.updatedAt || chat.UpdatedAt || chat.createdAt || chat.CreatedAt;

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

