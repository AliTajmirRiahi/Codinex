/**
 * PaginationService
 * A reusable pagination controller for any list in the app.
 * Each instance maintains its own pagination state.
 */
export class PaginationService {

    /**
     * @param {Array} items The initial list of items to paginate.
     * @param {number} itemsPerPage Items per page (default: 5)
     */
    constructor(items = [], itemsPerPage = 5) {
        this.items = items;
        this.itemsPerPage = itemsPerPage;
        this.currentPage = 1;
    }

    /**
     * Sets a new data list and resets the current page.
     */
    setItems(items) {
        this.items = items || [];
        this.currentPage = 1;
    }

    /**
     * Returns how many total pages exist.
     */
    getTotalPages() {
        return Math.ceil(this.items.length / this.itemsPerPage) || 1;
    }

    /**
     * Returns items for the current page.
     */
    getPageItems() {
        const start = (this.currentPage - 1) * this.itemsPerPage;
        const end = start + this.itemsPerPage;
        return this.items.slice(start, end);
    }

    /**
     * Navigate to the next page.
     */
    nextPage() {
        if (this.currentPage < this.getTotalPages()) {
            this.currentPage++;
        }
    }

    /**
     * Navigate to the previous page.
     */
    prevPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
        }
    }

    /**
     * Goes to a specific page index (1-based).
     */
    goToPage(page) {
        const max = this.getTotalPages();
        this.currentPage = Math.min(Math.max(page, 1), max);
    }
}
