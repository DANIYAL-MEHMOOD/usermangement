// Theme restore + sidebar collapse + reusable toast/confirm helpers shared by
// every CRUD screen (Users, Roles, Permissions, Menus).

document.addEventListener('DOMContentLoaded', () => {
    const stored = localStorage.getItem('theme');
    if (stored) document.documentElement.setAttribute('data-bs-theme', stored === 'auto'
        ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
        : stored);

    const collapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (collapsed) document.querySelector('.app-shell')?.classList.add('collapsed');

    document.getElementById('sidebarToggle')?.addEventListener('click', () => {
        const shell = document.querySelector('.app-shell');
        shell.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', shell.classList.contains('collapsed'));
    });
});

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

/** Small helper so every page's fetch() calls unwrap the ApiResponse envelope
 *  consistently and automatically carry the anti-forgery header Razor Pages
 *  expects on every non-GET handler (see Program.cs AddAntiforgery + the
 *  csrf-token meta tag in _Layout.cshtml). */
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
