(function () {
    'use strict';

    function lockBody(on) {
        document.body.style.overflow = on ? 'hidden' : '';
    }

    window.yajiOpenModal = function (rootId) {
        var el = document.getElementById(rootId);
        if (!el) return;
        el.classList.add('is-open');
        el.setAttribute('aria-hidden', 'false');
        lockBody(true);
    };

    window.yajiCloseModal = function (rootId) {
        var el = document.getElementById(rootId);
        if (!el) return;
        el.classList.remove('is-open');
        el.setAttribute('aria-hidden', 'true');
        lockBody(false);
    };

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('.yaji-modal-root.is-open').forEach(function (m) {
            window.yajiCloseModal(m.id);
        });
    });
})();
