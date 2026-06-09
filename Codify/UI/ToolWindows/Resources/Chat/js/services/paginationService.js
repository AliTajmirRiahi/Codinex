/**
 * Manages chunked loading of chat history to keep UI performant.
 */
export const paginationService = {
    pageSize: 5,
    currentPage: 0,

    getPaginated(allMessages) {
        const start = this.currentPage * this.pageSize;
        return allMessages.slice(start, start + this.pageSize);
    },

    nextPage() {
        this.currentPage++;
    }
};
