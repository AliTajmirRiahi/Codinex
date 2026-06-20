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

                option.innerHTML = `
                    <div class="drop-info">
                        <codify-icon name="message-circle-check" class="chat-icon"></codify-icon>
                        <span>${item.title}</span>
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
        this.modelDropDown.render(items, selectedValue);
        this.setCurrentChatName();
    },

    handleSendMessage(input) {
        this.handleNewChat();
    },
}

