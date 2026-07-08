(() => {
    const root = document.documentElement;
    const themeButtons = document.querySelectorAll('[data-theme-choice]');
    const themeToggles = document.querySelectorAll('[data-theme-toggle]');
    const sidebar = document.getElementById('clientSidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const backdrop = document.getElementById('sidebarBackdrop');
    const desktopWidth = 992;
    const sidebarKey = 'jdsp-sidebar-collapsed';

    function applyTheme(value) {
        const selectedTheme = value === 'dark' ? 'dark' : 'light';
        root.setAttribute('data-bs-theme', selectedTheme);
        localStorage.setItem('jdsp-theme', selectedTheme);

        themeButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.themeChoice === selectedTheme);
        });

        themeToggles.forEach(button => {
            button.textContent = selectedTheme === 'dark' ? '☀' : '☾';
            button.setAttribute('aria-label', selectedTheme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode');
            button.setAttribute('title', selectedTheme === 'dark' ? 'Light mode' : 'Dark mode');
        });
    }

    themeButtons.forEach(button => {
        button.addEventListener('click', () => applyTheme(button.dataset.themeChoice));
    });

    themeToggles.forEach(button => {
        button.addEventListener('click', () => {
            applyTheme(root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark');
        });
    });

    applyTheme(localStorage.getItem('jdsp-theme') || 'light');

    function isDesktop() {
        return window.innerWidth >= desktopWidth;
    }

    function closeMobileSidebar() {
        sidebar?.classList.remove('open');
        backdrop?.classList.remove('open');
    }

    function setDesktopCollapsed(isCollapsed) {
        document.body.classList.toggle('sidebar-collapsed', isCollapsed);
        localStorage.setItem(sidebarKey, isCollapsed ? '1' : '0');
    }

    if (sidebar && isDesktop() && localStorage.getItem(sidebarKey) === '1') {
        document.body.classList.add('sidebar-collapsed');
    }

    sidebarToggle?.addEventListener('click', () => {
        if (isDesktop()) {
            closeMobileSidebar();
            setDesktopCollapsed(!document.body.classList.contains('sidebar-collapsed'));
            return;
        }

        sidebar?.classList.toggle('open');
        backdrop?.classList.toggle('open');
    });

    backdrop?.addEventListener('click', closeMobileSidebar);

    window.addEventListener('resize', () => {
        if (isDesktop()) {
            closeMobileSidebar();
            if (sidebar && localStorage.getItem(sidebarKey) === '1') {
                document.body.classList.add('sidebar-collapsed');
            }
        }
    });
})();
