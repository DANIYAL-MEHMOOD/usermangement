// MyApp theme engine (light | dark | glass) + sidebar collapse + reusable
// toast/confirm/fetch helpers shared by every CRUD screen.

(function () {
    'use strict';

    const VALID_THEMES = ['light', 'dark', 'glass'];

    function applyTheme(theme) {
        if (!VALID_THEMES.includes(theme)) return;
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme === 'glass' ? 'glass' : theme);
        // Let Bootstrap components (dropdowns, modals) follow the theme too.
        document.body.classList.remove('app-theme-light', 'app-theme-dark', 'app-theme-glass');
        document.body.classList.add('app-theme-' + theme);
    }

    function currentTheme() {
        const sessionTheme = document.documentElement.getAttribute('data-theme');
        const stored = localStorage.getItem('theme');
        return VALID_THEMES.includes(sessionTheme) ? sessionTheme
            : VALID_THEMES.includes(stored) ? stored : 'light';
    }

    function persistTheme(theme) {
        localStorage.setItem('theme', theme);
        // Persist to the server (user preferences) so it survives across
        // devices. The endpoint is CSRF-protected via the meta tag below.
        const csrf = document.querySelector('meta[name="csrf-token"]')?.content;
        fetch('/Settings/Theme', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrf || ''
            },
            body: JSON.stringify({ theme: theme, language: 'en' })
        }).catch(() => { /* offline / api down: local preference still applies */ });
    }

    document.addEventListener('DOMContentLoaded', () => {
        applyTheme(currentTheme());

        const collapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (collapsed) document.querySelector('.app-shell')?.classList.add('collapsed');

        document.getElementById('sidebarToggle')?.addEventListener('click', () => {
            const shell = document.querySelector('.app-shell');
            shell.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', shell.classList.contains('collapsed'));
        });

        document.querySelectorAll('.theme-option').forEach(el => {
            el.addEventListener('click', (e) => {
                e.preventDefault();
                const theme = el.getAttribute('data-theme');
                if (!VALID_THEMES.includes(theme)) return;
                applyTheme(theme);
                persistTheme(theme);
            });
        });
    });
})();

/** Shows a Bootstrap toast. kind: 'success' | 'danger' | 'warning' | 'info' */
function showToast(message, kind = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) { alert(message); return; }

    const el = document.createElement('div');
    el.className = `toast align-items-center text-bg-${kind} border-0`;
    el.setAttribute('role', 'alert');
    el.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>`;
    container.appendChild(el);
    const toast = new bootstrap.Toast(el, { delay: 4000 });
    toast.show();
    el.addEventListener('hidden.bs.toast', () => el.remove());
}

/** Promise-based confirm dialog using a Bootstrap modal (nicer than window.confirm). */
function confirmDialog(message, title = 'Please confirm') {
    return new Promise((resolve) => {
        let modalEl = document.getElementById('confirmDialogModal');
        if (!modalEl) {
            modalEl = document.createElement('div');
            modalEl.id = 'confirmDialogModal';
            modalEl.className = 'modal fade';
            modalEl.innerHTML = `
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="confirmDialogTitle"></h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body" id="confirmDialogBody"></div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-danger" id="confirmDialogOk">Confirm</button>
                        </div>
                    </div>
                </div>`;
            document.body.appendChild(modalEl);
        }

        modalEl.querySelector('#confirmDialogTitle').textContent = title;
        modalEl.querySelector('#confirmDialogBody').textContent = message;

        const bsModal = new bootstrap.Modal(modalEl);
        const okBtn = modalEl.querySelector('#confirmDialogOk');

        const cleanup = (result) => {
            okBtn.removeEventListener('click', onOk);
            bsModal.hide();
            resolve(result);
        };
        const onOk = () => cleanup(true);

        okBtn.addEventListener('click', onOk);
        modalEl.addEventListener('hidden.bs.modal', () => resolve(false), { once: true });

        bsModal.show();
    });
}

/** Unwraps the ApiResponse envelope and carries the anti-forgery header. */
async function callApi(url, options = {}) {
    const method = (options.method || 'GET').toUpperCase();
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };

    if (method !== 'GET') {
        const token = document.querySelector('meta[name="csrf-token"]')?.content;
        if (token) headers['X-CSRF-TOKEN'] = token;
    }

    const response = await fetch(url, { headers, ...options });
    let body = null;
    try { body = await response.json(); } catch { /* no body */ }
    return { ok: response.ok, status: response.status, body };
}
