// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

function renderAuthState() {
    const loginButton = document.getElementById('login-button');
    const avatarButton = document.getElementById('user-avatar-button');
    const userMenuWrapper = document.getElementById('user-menu-wrapper');
    const dropdownUserName = document.getElementById('dropdown-user-name');
    if (!loginButton || !avatarButton || !userMenuWrapper) return;

    let user = null;
    const stored = localStorage.getItem('user_information');
    if (stored) {
        try {
            user = JSON.parse(stored);
        } catch {
            user = null;
        }
    }

    if (!user && getCookie('user_session')) {
        user = { full_name: 'User' };
    }

    if (user) {
        const fullName = user.fullName || user.full_name || 'User';
        const initial = fullName.trim().charAt(0).toUpperCase() || 'U';
        avatarButton.textContent = initial;
        if (dropdownUserName) dropdownUserName.textContent = fullName;
        loginButton.classList.add('hidden');
        userMenuWrapper.classList.remove('hidden');
        avatarButton.classList.add('flex');
        avatarButton.title = fullName;
    } else {
        loginButton.classList.remove('hidden');
        userMenuWrapper.classList.add('hidden');
        avatarButton.classList.remove('flex');
    }
}

function closeUserDropdown() {
    const dropdown = document.getElementById('user-dropdown');
    if (dropdown) dropdown.classList.add('hidden');
}

function toggleUserDropdown() {
    const dropdown = document.getElementById('user-dropdown');
    if (!dropdown) return;
    dropdown.classList.toggle('hidden');
}

function markAuthInputError(input) {
    if (!input) return;
    input.classList.add('border-red-500', 'ring-2', 'ring-red-200');
}

window.showLoginModal = function () {
    const modal = document.getElementById('login-modal');
    if (modal) modal.classList.replace('hidden', 'flex');
};

window.hideLoginModal = function () {
    const modal = document.getElementById('login-modal');
    if (modal) modal.classList.replace('flex', 'hidden');
};

window.showRegisterModal = function () {
    const modal = document.getElementById('register-modal');
    if (modal) modal.classList.replace('hidden', 'flex');
};

window.hideRegisterModal = function () {
    const modal = document.getElementById('register-modal');
    if (modal) modal.classList.replace('flex', 'hidden');
};

window.showToast = function (message, type) {
    const toast = document.getElementById('toast');
    const toastMessage = document.getElementById('toast-message');
    if (!toast || !toastMessage) return;

    const styles = {
        success: 'bg-emerald-600',
        error: 'bg-red-600',
        info: 'bg-blue-700'
    };

    toast.className = `fixed top-6 right-6 z-[1000] max-w-sm px-5 py-4 rounded-2xl shadow-2xl text-sm font-medium text-white transition-all duration-300 ${styles[type] || styles.info}`;
    toastMessage.textContent = message;
    toast.classList.remove('hidden', 'opacity-0', 'translate-y-[-12px]');
    toast.classList.add('opacity-100', 'translate-y-0');

    clearTimeout(window.__toastTimer);
    window.__toastTimer = setTimeout(() => {
        toast.classList.add('opacity-0', 'translate-y-[-12px]');
        toast.classList.remove('opacity-100', 'translate-y-0');
        setTimeout(() => toast.classList.add('hidden'), 200);
    }, 2500);
};

window.fakeLogin = async function () {
    const roleInput = document.getElementById('login-role');
    const emailInput = document.getElementById('login-email');
    const passwordInput = document.getElementById('login-password');
    const role = roleInput ? roleInput.value : '';
    const email = emailInput ? emailInput.value : '';
    const password = passwordInput ? passwordInput.value : '';

    if (!role.trim() || !email.trim() || !password.trim()) {
        window.showToast('Vui lòng nhập đầy đủ thông tin đăng nhập', 'error');
        return;
    }

    try {
        const response = await fetch('/api/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email,
                password,
                role
            })
        });

        const data = await response.json();
        if (response.ok && data.success) {
            localStorage.setItem('user_information', JSON.stringify(data.user));
            window.showToast(data.message || 'Login successful', 'success');
            window.hideLoginModal();
            renderAuthState();
            window.location.reload();
            return;
        }

        window.showToast(data.message || 'Login failed', 'error');
    } catch {
        window.showToast('Login failed', 'error');
    }
};

window.submitRegister = function () {
    window.showToast('Chức năng đăng ký đang được nối tiếp.', 'info');
};

window.logoutUser = function() {
    localStorage.removeItem('user_information');
    document.cookie = 'user_session=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    closeUserDropdown();
    renderAuthState();
};

document.addEventListener('DOMContentLoaded', () => {
    const mobileBtn = document.getElementById('mobile-menu-btn');
    const mobileMenu = document.getElementById('mobile-menu');
    if (mobileBtn && mobileMenu) {
        mobileBtn.addEventListener('click', () => {
            mobileMenu.classList.toggle('hidden');
            const icon = mobileBtn.querySelector('i');
            if (icon) {
                icon.classList.toggle('fa-bars');
                icon.classList.toggle('fa-xmark');
            }
        });
    }

    renderAuthState();
});
