const escapeArmed = new WeakSet();
document.addEventListener("keydown", event => {
    const field = event.target;
    if (!(field instanceof HTMLTextAreaElement) || field.id !== "draft" || field.readOnly || event.isComposing) return;
    if (event.key === "Escape") { escapeArmed.add(field); return; }
    if (event.key !== "Tab") {
        if (!["Shift", "Control", "Alt", "Meta"].includes(event.key)) escapeArmed.delete(field);
        return;
    }
    if (escapeArmed.has(field)) { escapeArmed.delete(field); return; }
    if (event.ctrlKey || event.altKey || event.metaKey) return;
    event.preventDefault();
    const text = field.value, start = field.selectionStart, end = field.selectionEnd;
    let from = start, to = end, replacement = "    ", nextStart, nextEnd;
    if (event.shiftKey || start !== end) {
        from = start === 0 ? 0 : text.lastIndexOf("\n", start - 1) + 1;
        to = end > start && text[end - 1] === "\n" ? end - 1 : end;
        const lineEnd = text.indexOf("\n", to);
        to = lineEnd < 0 ? text.length : lineEnd;
        const lines = text.slice(from, to).split("\n");
        const changes = lines.map(line => event.shiftKey ? -(line.match(/^(?:\t| {1,4})/)?.[0].length || 0) : 4);
        replacement = lines.map((line, i) => changes[i] < 0 ? line.slice(-changes[i]) : changes[i] > 0 ? "    " + line : line).join("\n");
        nextStart = Math.max(from, start + changes[0]);
        nextEnd = Math.max(nextStart, end + changes.reduce((a, b) => a + b, 0));
    } else nextStart = nextEnd = start + 4;
    if (text.slice(from, to) === replacement) return;
    field.setSelectionRange(from, to);
    // shortcut: insertText preserves native undo; setRangeText covers browsers without it.
    const inserted = document.execCommand("insertText", false, replacement);
    if (!inserted) field.setRangeText(replacement, from, to, "end");
    field.setSelectionRange(nextStart, nextEnd);
    field.dispatchEvent(new Event("input", { bubbles: true }));
});
document.addEventListener("focusout", event => escapeArmed.delete(event.target));
