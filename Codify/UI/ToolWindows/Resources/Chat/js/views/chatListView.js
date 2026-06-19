/**
 * ChatView
 * Responsible only for rendering UI elements.
 * No business logic or AI communication should exist here.
 */

import { $ } from '../utils/dom.js';
import { DropDownView } from '../views/dropDownView.js';
import { getState } from '../state/appState.js';

export const chatListView = {

    initialize(onChatSelected) {
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

}

