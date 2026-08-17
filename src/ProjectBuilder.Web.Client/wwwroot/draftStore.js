window.projectBuilderDrafts = {
    read: key => window.localStorage.getItem(key),
    write: (key, value) => window.localStorage.setItem(key, value),
    remove: key => window.localStorage.removeItem(key)
};

window.projectBuilderGuidance = {
    read: key => window.localStorage.getItem(key),
    write: (key, value) => window.localStorage.setItem(key, value),
    registrations: new Map(),
    register: (scopeId, token, receiver) => {
        const existing = window.projectBuilderGuidance.registrations.get(scopeId);
        if (existing) document.removeEventListener("keydown", existing.handler);
        const registration = { token, invoker: null, handler: null };
        registration.handler = event => {
            const key = event.key.toLowerCase();
            if ((event.ctrlKey || event.metaKey) && event.shiftKey && key === "g") {
                event.preventDefault();
                registration.invoker = document.activeElement;
                receiver.invokeMethodAsync("ToggleGuideFromKeyboard");
            } else if (event.key === "Escape") {
                receiver.invokeMethodAsync("CloseGuideFromKeyboard");
            }
        };
        document.addEventListener("keydown", registration.handler);
        window.projectBuilderGuidance.registrations.set(scopeId, registration);
    },
    rememberInvoker: (scopeId) => {
        const registration = window.projectBuilderGuidance.registrations.get(scopeId);
        if (registration) registration.invoker = document.activeElement;
    },
    restoreInvoker: scopeId => {
        const registration = window.projectBuilderGuidance.registrations.get(scopeId);
        if (registration?.invoker instanceof HTMLElement && document.contains(registration.invoker)) {
            registration.invoker.focus({ preventScroll: true });
            return true;
        }
        return false;
    },
    focusToggle: scopeId => document.querySelector(`[data-guide-toggle="${CSS.escape(scopeId)}"]`)?.focus({ preventScroll: true }),
    unregister: (scopeId, token) => {
        const registration = window.projectBuilderGuidance.registrations.get(scopeId);
        if (!registration || registration.token !== token) return;
        document.removeEventListener("keydown", registration.handler);
        window.projectBuilderGuidance.registrations.delete(scopeId);
    }
};

window.projectBuilderEditorKeys = {
    registrations: new Map(),
    register: (scopeId, token, receiver) => {
        const existing = window.projectBuilderEditorKeys.registrations.get(scopeId);
        if (existing) document.removeEventListener("keydown", existing.handler);
        const handler = event => {
            const scope = document.getElementById(scopeId);
            const outsideEditableScope = !scope?.contains(event.target) && event.target !== document.body && event.target !== document.documentElement;
            if (!scope || outsideEditableScope || (!event.ctrlKey && !event.metaKey)) return;
            const key = event.key.toLowerCase();
            let command = null;
            if (key === "s") command = "commit";
            else if (key === "z" && event.shiftKey) command = "redo";
            else if (key === "z") command = "undo";
            else if (key === "y") command = "redo";
            if (!command) return;
            event.preventDefault();
            receiver.invokeMethodAsync("Invoke", command);
        };
        document.addEventListener("keydown", handler);
        window.projectBuilderEditorKeys.registrations.set(scopeId, { token, handler });
    },
    unregister: (scopeId, token) => {
        const registration = window.projectBuilderEditorKeys.registrations.get(scopeId);
        if (!registration || registration.token !== token) return;
        document.removeEventListener("keydown", registration.handler);
        window.projectBuilderEditorKeys.registrations.delete(scopeId);
    }
};
