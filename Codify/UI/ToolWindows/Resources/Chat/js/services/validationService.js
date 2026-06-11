/**
 * Validation logic for settings and inputs.
 */
export const validationService = {
    isValidUrl(url) {
        try {
            new URL(url);
            return true;
        } catch (_) {
            return false;
        }
    },
    isNotEmpty(value) {
        // Using !! or Boolean() to ensure a true/false result
        return !!(value && value.trim().length > 0);
    }
};
