// jest.config.js
module.exports = {
    testEnvironment: "jsdom",
    transform: {
        "^.+\\.js$": "babel-jest",
    },
    moduleFileExtensions: ["js"],
    testMatch: [
        "**/tests/**/*.test.js",
        "**/?(*.)+(spec|test).js"
    ],
    rootDir: "./"
};
