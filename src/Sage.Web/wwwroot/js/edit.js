import Tags from "../lib/bootstrap-tags/tags.js";

function toTitleCase(str) {
    return str.toLowerCase().split(' ').map(function (word) {
        return (word.charAt(0).toUpperCase() + word.slice(1));
    }).join(' ');
}

Tags.init();

document.getElementById("titleCaseBtn").addEventListener("click", (ev) => {
    const inputEl = document.getElementById(ev.currentTarget.dataset.inputId);
    inputEl.value = toTitleCase(inputEl.value.trim());
});

document.getElementById("justifyBtn").addEventListener("click", (ev) => {
    const inputEl = document.getElementById(ev.currentTarget.dataset.inputId);
    inputEl.value = inputEl.value.trim().replaceAll(/(?:\r\n|\r|\n)/g, " ");
});