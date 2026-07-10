(function () {
    const ar = document.documentElement.dir === 'rtl';

    function format(ms) {
        const diff = Math.max(0, ms);
        const d = Math.floor(diff / 86400000);
        const h = Math.floor((diff % 86400000) / 3600000);
        const m = Math.floor((diff % 3600000) / 60000);
        return ar ? `${d} يوم ${h} ساعة ${m} دقيقة` : `${d}d ${h}h ${m}m`;
    }

    function tick(el) {
        const start = new Date(el.dataset.hearingStart || el.dataset.hearingDate).getTime();
        const end = new Date(el.dataset.hearingEnd || el.dataset.hearingStart || el.dataset.hearingDate).getTime();
        const output = el.querySelector('strong');
        const label = el.querySelector('span');
        if (!output || Number.isNaN(start) || Number.isNaN(end)) return;

        const now = Date.now();
        el.classList.remove('phase-before-start', 'phase-in-session', 'phase-ended');

        if (now < start) {
            el.classList.add('phase-before-start');
            if (label) label.textContent = ar ? 'يبدأ بعد' : 'Starts in';
            output.textContent = format(start - now);
            return;
        }

        if (now <= end) {
            el.classList.add('phase-in-session');
            if (label) label.textContent = ar ? 'ينتهي بعد' : 'Ends in';
            output.textContent = format(end - now);
            return;
        }

        el.classList.add('phase-ended');
        const waiting = ar ? 'بانتظار موعد الجلسة القادمة' : 'Waiting for next hearing date';
        const status = el.closest('.case-card')?.querySelector('.status') || document.querySelector('.page-head .status.large');
        if (status) {
            status.textContent = waiting;
            status.className = 'status waiting-for-next-hearing-date';
        }
        el.style.display = 'none';
    }

    const items = document.querySelectorAll('.hearing-countdown[data-hearing-start], .hearing-countdown[data-hearing-date]');
    items.forEach(tick);
    if (items.length) setInterval(() => items.forEach(tick), 60000);
})();
