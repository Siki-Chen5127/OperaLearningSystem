// ============ 传习私塾 ============
document.addEventListener("DOMContentLoaded", function () {
    var suppressBookClickUntil = 0;

    // 从页面获取 browseMode，在 HTML 中会把这个值设在 #bookshelf 的 data 属性里
    var bookshelf = document.getElementById('bookshelf');
    var browseMode = bookshelf ? bookshelf.getAttribute('data-browse-mode') : 'search';

    var axis = document.getElementById('carouselAxis');
    var viewport = document.getElementById('carouselViewport');

    // --- 3D 旋转书架逻辑 ---
    if (browseMode === 'spotlight' && axis && viewport) {
        var cells = axis.querySelectorAll('.carousel-cell');
        var n = cells.length;
        var radius = n <= 1 ? 0 : Math.max(200, Math.min(360, (58 * n) / (2 * Math.PI) * 1.22));
        var angle = 0;
        var vel = 0;
        var dragging = false;
        var lastX = 0;
        var totalDrag = 0;
        var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var autoSpeed = reducedMotion ? 0 : 0.11;

        function normDeg(d) {
            d = d % 360;
            if (d > 180) d -= 360;
            if (d < -180) d += 360;
            return d;
        }

        function layoutRing() {
            if (n <= 1) {
                cells.forEach(function (cell, i) {
                    if (i === 0) {
                        cell.style.opacity = '1';
                        cell.style.pointerEvents = '';
                        cell.style.transform = 'rotateY(0deg) translateZ(' + (radius + 36) + 'px) rotateY(0deg)';
                    } else {
                        cell.style.opacity = '0';
                        cell.style.pointerEvents = 'none';
                        cell.style.transform = 'scale(0)';
                    }
                    cell.classList.remove('carousel-cell--rear');
                });
                return;
            }
            applyRingTransforms();
        }

        function applyRingTransforms() {
            if (n <= 1) return;
            cells.forEach(function (cell, i) {
                cell.style.opacity = '1';
                var slotDeg = (360 / n) * i;
                var yaw = slotDeg + angle;
                cell.style.transform =
                    'rotateY(' + yaw + 'deg) translateZ(' + radius + 'px) rotateY(' + -yaw + 'deg)';
            });
        }

        function updateRearFaces() {
            if (n <= 1) return;
            cells.forEach(function (cell, i) {
                var rel = normDeg((360 / n) * i + angle);
                var rear = Math.abs(rel) > 82;
                cell.classList.toggle('carousel-cell--rear', rear);
                cell.style.pointerEvents = rear ? 'none' : '';
                var z = 20 - Math.abs(rel) / 90 * 8;
                cell.style.zIndex = String(Math.round(z));
            });
        }

        function tick() {
            if (!dragging) {
                if (Math.abs(vel) > 0.02) {
                    angle += vel;
                    vel *= 0.93;
                } else {
                    vel = 0;
                    angle += autoSpeed;
                }
            }
            axis.style.transform = 'translateZ(-56px) rotateX(6deg)';
            applyRingTransforms();
            updateRearFaces();
            requestAnimationFrame(tick);
        }

        function onDown(x) {
            dragging = true;
            lastX = x;
            totalDrag = 0;
            vel = 0;
            viewport.classList.add('carousel-dragging');
        }
        function onMove(x) {
            if (!dragging) return;
            var dx = x - lastX;
            lastX = x;
            totalDrag += Math.abs(dx);
            angle += dx * 0.38;
            vel = dx * 0.38;
        }
        function onUp() {
            if (!dragging) return;
            dragging = false;
            viewport.classList.remove('carousel-dragging');
            window.removeEventListener('pointermove', onWindowPointerMove);
            window.removeEventListener('pointerup', onWindowPointerUp);
            window.removeEventListener('pointercancel', onWindowPointerUp);
            if (totalDrag > 12) {
                suppressBookClickUntil = Date.now() + 400;
            }
        }
        function onWindowPointerMove(e) { onMove(e.clientX); }
        function onWindowPointerUp() { onUp(); }

        layoutRing();
        axis.querySelectorAll('.scroll-book').forEach(function (book, idx) {
            book.style.animationDelay = idx * 0.045 + 's';
        });
        tick();

        viewport.addEventListener('pointerdown', function (e) {
            if (e.button !== undefined && e.button !== 0) return;
            onDown(e.clientX);
            window.addEventListener('pointermove', onWindowPointerMove);
            window.addEventListener('pointerup', onWindowPointerUp);
            window.addEventListener('pointercancel', onWindowPointerUp);
        });

        viewport.addEventListener('wheel', function (e) {
            angle += e.deltaY * 0.09;
            vel = e.deltaY * 0.06;
            suppressBookClickUntil = Date.now() + 200;
        }, { passive: true });
    }

    // --- 展卷交互逻辑 ---
    var overlay = document.getElementById('scrollOverlay');
    var closeBtn = document.getElementById('scrollClose');
    var detailsBtn = document.getElementById('spDetailsBtn');

    if (bookshelf) {
        bookshelf.addEventListener('click', function (e) {
            var book = e.target.closest('.scroll-book');
            if (!book || !bookshelf.contains(book)) return;
            if (browseMode === 'spotlight' && book.closest('.carousel-cell--rear')) return;
            if (Date.now() < suppressBookClickUntil) return;

            var courseId = book.getAttribute('data-course-id');
            if (!courseId) return;
            var studyUrl = book.getAttribute('data-study-url');
            var tipT = book.querySelector('.tooltip-title');
            var spine = book.querySelector('.spine-text');
            var name = (tipT && tipT.textContent) || (spine && spine.textContent) || '';

            // 飞行动画
            var rect = book.getBoundingClientRect();
            var ghost = book.cloneNode(true);
            ghost.classList.add('scroll-book-flying');
            ghost.style.cssText = 'position:fixed;left:' + rect.left + 'px;top:' + rect.top + 'px;width:' + rect.width + 'px;height:' + rect.height + 'px;z-index:99999;pointer-events:none;';
            document.body.appendChild(ghost);

            var cx = window.innerWidth / 2 - rect.left - rect.width / 2;
            var cy = window.innerHeight / 2 - rect.top - rect.height / 2;
            requestAnimationFrame(function () {
                ghost.style.transition = 'transform 0.7s cubic-bezier(0.22,1,0.36,1), opacity 0.7s';
                ghost.style.transform = 'translate(' + cx + 'px,' + cy + 'px) scale(0.15) rotate(-8deg)';
                ghost.style.opacity = '0.3';
            });

            setTimeout(function () { ghost.remove(); }, 700);

            // 展卷
            setTimeout(function () {
                document.getElementById('spTitle').textContent = name;
                document.getElementById('spCat').textContent = '';
                document.getElementById('spDesc').textContent = '研墨展卷中...';
                document.getElementById('spStudyBtn').href = studyUrl;
                if (detailsBtn) detailsBtn.href = '/Course/Details/' + courseId;
                overlay.classList.add('open');

                fetch('/Course/DetailsJson/' + courseId)
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        document.getElementById('spTitle').textContent = d.name;
                        document.getElementById('spCat').textContent = d.category ? ('分类：' + d.category) : '';
                        document.getElementById('spDesc').textContent = d.description || '暂无介绍';
                        document.getElementById('spStudyBtn').href = d.studyUrl;
                        if (detailsBtn && d.detailsUrl) detailsBtn.href = d.detailsUrl;
                    })
                    .catch(function () {
                        document.getElementById('spDesc').textContent = '未寻得残卷信息。';
                    });
            }, 500);
        });
    }

    function closeOverlay() {
        if (overlay) overlay.classList.remove('open');
    }
    if (closeBtn) closeBtn.addEventListener('click', closeOverlay);
    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeOverlay();
        });
    }

    // --- 背景浮动粒子 ---
    var canvas = document.getElementById('bgParticles');
    if (canvas) {
        for (var i = 0; i < 15; i++) {
            var p = document.createElement('div');
            p.className = 'bg-particle';
            p.style.left = Math.random() * 100 + '%';
            p.style.animationDelay = Math.random() * 8 + 's';
            p.style.animationDuration = (8 + Math.random() * 12) + 's';
            p.style.opacity = (0.2 + Math.random() * 0.4).toString();
            p.style.width = p.style.height = (4 + Math.random() * 6) + 'px';
            canvas.appendChild(p);
        }
    }
});