'use strict';

const hasTrustedTypes = typeof trustedTypes !== "undefined";

let escapeHtmlPolicy;
if (hasTrustedTypes) {
    escapeHtmlPolicy = trustedTypes.createPolicy("escapePolicy", {
        createHTML: (string) => string.replace(/</g, "&lt;"),
    });
}