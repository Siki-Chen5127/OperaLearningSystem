/**
 * 雅集 / 戏台打卡 / 百宝阁 — 共用信息流（词云、排序、搜索、互动）
 * @param {object} opts
 * @param {number} opts.postKind - 0 雅集 1 打卡 2 百宝阁
 * @param {'full'|'checkin'} opts.reactionSet - full: 赞花草藏；checkin: 赞藏转
 * @param {boolean} opts.showMediaInFeed
 * @param {boolean} opts.authed
 * @param {string} [opts.emptyHtml] - 无帖时 HTML
 */
(function (window) {
    'use strict';

    function stripHtml(html) {
        if (!html) return '';
        var d = document.createElement('div');
        d.innerHTML = html;
        return (d.textContent || '').replace(/\s+/g, ' ').trim();
    }

    function esc(s) {
        if (!s) return '';
        var d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }

    function debounce(fn, ms) {
        var t;
        return function () {
            var a = arguments, ctx = this;
            clearTimeout(t);
            t = setTimeout(function () { fn.apply(ctx, a); }, ms);
        };
    }

    function copyText(text) {
        if (!text) return Promise.resolve(false);
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text).then(function () { return true; }).catch(function () { return false; });
        }
        try {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.focus();
            ta.select();
            var ok = document.execCommand('copy');
            document.body.removeChild(ta);
            return Promise.resolve(!!ok);
        } catch (e) {
            return Promise.resolve(false);
        }
    }

    window.initYajiFeed = function initYajiFeed(opts) {
        var postKind = opts.postKind;
        var reactionSet = opts.reactionSet || 'full';
        var showMedia = !!opts.showMediaInFeed;
        var authed = !!opts.authed;
        var emptyHtml = opts.emptyHtml || ('<div class="yaji-empty" data-aos="fade-in"><i class="far fa-scroll"></i>暂无内容<br/><small>调整筛选或率先发布。</small></div>');

        var state = { sort: 'smart', region: '', wcFilter: '', search: '', posts: [] };

        var feedHost = document.getElementById(opts.feedHostId || 'yajiFeedHost');
        var wcEl = document.getElementById(opts.wcHostId || 'yajiWordCloud');
        var regionInput = document.getElementById(opts.regionInputId || 'yajiRegion');
        var regionToggle = document.getElementById(opts.regionToggleId || 'yajiToggleRegion');
        var refreshBtn = document.getElementById(opts.refreshBtnId || 'yajiRefresh');
        var sortGroup = document.getElementById(opts.sortGroupId || 'yajiSortGroup');
        var searchInput = document.getElementById(opts.searchId || 'yajiSearch');

        function matchesFilters(p) {
            // 词云筛选由服务端 recommended?wc= 完成（含评论匹配）；此处只处理搜索框
            if (state.search) {
                var q = state.search.toLowerCase();
                var hay = ((p.title || '') + ' ' + stripHtml(p.content || '') + ' ' + (p.topicTags || '') + ' ' + (p.category || '')).toLowerCase();
                if (hay.indexOf(q) < 0) return false;
            }
            return true;
        }

        function mediaHtml(urls) {
            if (!showMedia || !urls || !urls.length) return '';
            var html = '<div class="yaji-feed-media">';
            var n = Math.min(urls.length, 4);
            for (var i = 0; i < n; i++) {
                var u = urls[i];
                var lower = (u || '').toLowerCase();
                if (lower.endsWith('.mp4') || lower.endsWith('.webm') || lower.endsWith('.mov')) {
                    html += '<video class="yaji-feed-media-item" controls src="' + esc(u) + '" playsinline></video>';
                } else {
                    html += '<img class="yaji-feed-media-item" src="' + esc(u) + '" alt="" />';
                }
            }
            return html + '</div>';
        }

        function reactBtn(pid, kind, icon, label, count, on) {
            return '<button type="button" class="yaji-react' + (on ? ' active' : '') + '" data-react="' + kind + '" data-post="' + pid + '">' +
                '<i class="fas ' + icon + '"></i> ' + label + ' <span class="num">' + (count || 0) + '</span></button>';
        }

        function rowHtml(p) {
            var excerpt = stripHtml(p.content || '');
            if (excerpt.length > 220) excerpt = excerpt.slice(0, 220) + '…';
            var tags = (p.topicTags || '').split(/[，,、]/).map(function (t) { return t.trim(); }).filter(Boolean);
            var tagHtml = tags.slice(0, 5).map(function (t) {
                return '<span class="yaji-chip">' + esc(t) + '</span>';
            }).join('');
            if (p.regionLabel) tagHtml += '<span class="yaji-chip yaji-chip--muted"><i class="fas fa-map-pin"></i> ' + esc(p.regionLabel) + '</span>';
            if (p.category) tagHtml += '<span class="yaji-chip yaji-chip--muted">' + esc(p.category) + '</span>';

            var s = p.stats || {};
            var m = p.mine || {};
            var actions = '';

            if (reactionSet === 'checkin') {
                actions += reactBtn(p.id, 0, 'fa-heart', '赞', s.like, m.like);
                actions += reactBtn(p.id, 3, 'fa-bookmark', '藏', s.bookmark, m.bookmark);
                actions += reactBtn(p.id, 4, 'fa-share', '转', s.share, m.share);
            } else {
                actions += reactBtn(p.id, 0, 'fa-heart', '赞', s.like, m.like);
                actions += reactBtn(p.id, 1, 'fa-spa', '花', s.flower, m.flower);
                actions += reactBtn(p.id, 2, 'fa-hands-clapping', '彩', s.cheer, m.cheer);
                actions += reactBtn(p.id, 3, 'fa-bookmark', '藏', s.bookmark, m.bookmark);
                actions += reactBtn(p.id, 4, 'fa-share', '转', s.share, m.share);
            }

            actions += '<button type="button" class="yaji-ghost-btn yaji-thread-toggle" data-post="' + p.id + '"><i class="far fa-comments"></i> 茶叙 ' + (s.comments || 0) + '</button>';
            actions += '<span class="yaji-stat-pill">#' + p.id + '</span>';

            var urls = p.mediaUrls || [];

            return '<article class="yaji-row" data-id="' + p.id + '" data-aos="fade-up">' +
                '<img class="yaji-avatar" src="' + esc(p.avatarUrl || '/images/default_avatar.png') + '" alt="" width="56" height="56" />' +
                '<div class="yaji-row-main">' +
                '<div class="yaji-row-title-row">' +
                '<a class="yaji-row-title" href="/Community/Details/' + p.id + '">' + esc(p.title || '无题') + '</a>' +
                '<span class="yaji-row-meta">' + esc(p.author || '戏友') + ' · ' + fmtDate(p.createdTime) + '</span></div>' +
                '<p class="yaji-row-excerpt">' + esc(excerpt || '（暂无摘要）') + '</p>' +
                mediaHtml(urls) +
                (tagHtml ? '<div class="yaji-tag-row">' + tagHtml + '</div>' : '') +
                '<div class="yaji-actions">' + actions + '</div></div>' +
                '<div class="yaji-thread" id="yaji-thread-' + p.id + '">' +
                '<div class="yaji-thread-comments" id="yaji-cmts-' + p.id + '"></div>' +
                (authed ? '<div class="yaji-quick-cmt"><input type="text" maxlength="500" placeholder="两句点评…" data-post="' + p.id + '" /><button type="button" data-post="' + p.id + '">发送</button></div>' :
                    '<p class="small text-muted mb-0"><a href="/Account/Login">登录</a> 后可在此速评</p>') +
                '</div></article>';
        }

        function fmtDate(iso) {
            if (!iso) return '';
            var d = new Date(iso);
            if (isNaN(d.getTime())) return '';
            return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
        }

        function renderFeed() {
            if (!feedHost) return;
            var list = state.posts.filter(matchesFilters);
            if (!list.length) {
                feedHost.innerHTML = emptyHtml;
                if (window.AOS) { if (typeof AOS.refreshHard === 'function') AOS.refreshHard(); else AOS.refresh(); }
                return;
            }
            feedHost.innerHTML = list.map(function (p) { return rowHtml(p); }).join('');
            bindRowEvents(feedHost);
            if (window.AOS) { if (typeof AOS.refreshHard === 'function') AOS.refreshHard(); else AOS.refresh(); }
        }

        function bindRowEvents(root) {
            root.querySelectorAll('.yaji-react').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    if (!authed) { window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname); return; }
                    var kind = parseInt(btn.getAttribute('data-react'), 10);
                    var pid = parseInt(btn.getAttribute('data-post'), 10);
                    toggleReact(pid, kind, btn).then(function (added) {
                        if (kind === 3 && added) {
                            // 让“收藏”成为可见功能，用户可立即前往收藏页
                            if (window.confirm('已收藏，前往个人中心「帖子收藏」查看吗？')) {
                                window.location.href = '/Account/UserCenter#tab-postbm';
                            }
                        } else if (kind === 4) {
                            var post = state.posts.find(function (x) { return x.id === pid; }) || {};
                            var shareUrl = window.location.origin + '/Community/Details/' + pid;
                            var shareTitle = post.title || '雅集';
                            if (navigator.share) {
                                navigator.share({ title: shareTitle, text: '来看看这篇帖子', url: shareUrl }).catch(function () { });
                            } else {
                                copyText(shareUrl).then(function (ok) {
                                    if (ok) alert('已复制帖子链接，可直接转发给好友。');
                                    else window.open(shareUrl, '_blank', 'noopener');
                                });
                            }
                        }
                    });
                });
            });
            root.querySelectorAll('.yaji-thread-toggle').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var id = parseInt(btn.getAttribute('data-post'), 10);
                    var panel = document.getElementById('yaji-thread-' + id);
                    if (!panel) return;
                    var open = panel.classList.toggle('open');
                    if (open) loadComments(id);
                });
            });
            root.querySelectorAll('.yaji-quick-cmt button').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var pid = parseInt(btn.getAttribute('data-post'), 10);
                    var row = btn.closest('.yaji-quick-cmt');
                    var inp = row ? row.querySelector('input') : null;
                    if (!inp || !inp.value.trim()) return;
                    postComment(pid, inp.value.trim(), inp, btn);
                });
            });
        }

        function toggleReact(postId, kind, btn) {
            return fetch('/api/community-feed/react/' + postId + '?kind=' + kind, { method: 'POST', credentials: 'same-origin' })
                .then(function (r) {
                    if (r.status === 401) { window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname); return null; }
                    return r.json();
                })
                .then(function (data) {
                    if (!data) return null;
                    var added = !!data.added;
                    btn.classList.toggle('active', added);
                    var num = btn.querySelector('.num');
                    var n = parseInt(num.textContent, 10) || 0;
                    num.textContent = added ? n + 1 : Math.max(0, n - 1);
                    return added;
                }).catch(function () { return null; });
        }

        function loadComments(postId) {
            var host = document.getElementById('yaji-cmts-' + postId);
            if (!host || host.dataset.loaded === '1') return;
            host.innerHTML = '<div class="small text-muted py-2">载入评论…</div>';
            fetch('/api/community-feed/comments/' + postId + '?take=25', { credentials: 'same-origin' })
                .then(function (r) { return r.json(); })
                .then(function (list) {
                    host.dataset.loaded = '1';
                    if (!list || !list.length) {
                        host.innerHTML = '<div class="small text-muted">尚无茶叙，来坐第一席。</div>';
                        return;
                    }
                    host.innerHTML = list.map(function (c) {
                        return '<div class="yaji-cmt"><span class="yaji-cmt-author">' + esc(c.author) + '</span>' + esc(c.content) +
                            '<div class="small text-muted mt-1">' + fmtDate(c.createdAt) + '</div></div>';
                    }).join('');
                }).catch(function () { host.textContent = '评论加载失败'; });
        }

        function postComment(postId, text, inp, submitBtn) {
            submitBtn.disabled = true;
            fetch('/api/community-feed/comments/' + postId, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ content: text })
            }).then(function (r) {
                if (r.status === 401) { window.location.href = '/Account/Login'; return null; }
                return r.json();
            }).then(function (data) {
                submitBtn.disabled = false;
                if (!data) return;
                inp.value = '';
                var host = document.getElementById('yaji-cmts-' + postId);
                if (host) {
                    host.dataset.loaded = '0';
                    loadComments(postId);
                }
                var p = state.posts.find(function (x) { return x.id === postId; });
                if (p && p.stats) {
                    p.stats.comments = (p.stats.comments || 0) + 1;
                    var art = document.querySelector('.yaji-row[data-id="' + postId + '"]');
                    if (art) {
                        var tbtn = art.querySelector('.yaji-thread-toggle');
                        if (tbtn) tbtn.innerHTML = '<i class="far fa-comments"></i> 茶叙 ' + p.stats.comments;
                    }
                }
            }).catch(function () { submitBtn.disabled = false; });
        }

        function loadFeed() {
            var q = '?kind=' + postKind + '&take=40&sort=' + encodeURIComponent(state.sort);
            if (state.region) q += '&region=' + encodeURIComponent(state.region);
            if (state.wcFilter) q += '&wc=' + encodeURIComponent(state.wcFilter);
            if (!feedHost) return;
            feedHost.innerHTML = '<div class="yaji-loading"><i class="fas fa-circle-notch fa-spin"></i> 载入中…</div>';
            fetch('/api/community-feed/recommended' + q, { credentials: 'same-origin' })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    state.posts = Array.isArray(data) ? data : [];
                    renderFeed();
                }).catch(function () {
                    feedHost.innerHTML = '<div class="yaji-empty">加载失败，请稍后重试。</div>';
                });
        }

        function loadWordCloud() {
            if (!wcEl) return;
            fetch('/api/community-feed/word-cloud?kind=' + postKind, { cache: 'no-store', credentials: 'same-origin' })
                .then(function (r) { return r.json(); })
                .then(function (words) {
                    if (!words || !words.length) {
                        wcEl.textContent = '暂无词云，发布内容或评论后将自动生成。';
                        wcEl.style.fontSize = '0.88rem';
                        wcEl.style.color = '#999';
                        return;
                    }
                    var vals = words.map(function (w) { return w.value; }).filter(function (v) { return typeof v === 'number'; });
                    var max = vals.length ? Math.max.apply(null, vals) : 1;
                    wcEl.innerHTML = words.slice(0, 36).map(function (w) {
                        var sz = 0.82 + (w.value / max) * 1.1;
                        return '<span class="yaji-wc-span" data-w="' + esc(w.text) + '" style="font-size:' + sz.toFixed(2) + 'rem">' + esc(w.text) + '</span>';
                    }).join('');
                    wcEl.querySelectorAll('.yaji-wc-span').forEach(function (span) {
                        span.addEventListener('click', function () {
                            var w = span.getAttribute('data-w');
                            if (state.wcFilter === w) {
                                state.wcFilter = '';
                                wcEl.querySelectorAll('.yaji-wc-span').forEach(function (s) { s.classList.remove('active'); });
                            } else {
                                state.wcFilter = w;
                                wcEl.querySelectorAll('.yaji-wc-span').forEach(function (s) { s.classList.toggle('active', s.getAttribute('data-w') === w); });
                            }
                            loadFeed();
                        });
                    });
                }).catch(function () { wcEl.textContent = ''; });
        }

        if (sortGroup) {
            sortGroup.querySelectorAll('.yaji-sort-btn').forEach(function (b) {
                b.addEventListener('click', function () {
                    sortGroup.querySelectorAll('.yaji-sort-btn').forEach(function (x) { x.classList.remove('active'); });
                    b.classList.add('active');
                    state.sort = b.getAttribute('data-sort');
                    loadFeed();
                });
            });
        }

        if (searchInput) {
            searchInput.addEventListener('input', debounce(function () {
                state.search = searchInput.value.trim();
                renderFeed();
            }, 220));
        }

        if (regionToggle && regionInput) {
            regionToggle.addEventListener('click', function () {
                var show = regionInput.style.display === 'none';
                regionInput.style.display = show ? 'block' : 'none';
                if (show) regionInput.focus();
                else { state.region = ''; regionInput.value = ''; loadFeed(); }
            });
            regionInput.addEventListener('input', debounce(function () {
                state.region = regionInput.value.trim();
                loadFeed();
            }, 400));
            regionInput.addEventListener('change', function () {
                state.region = regionInput.value.trim();
                loadFeed();
            });
        }

        if (refreshBtn) {
            refreshBtn.addEventListener('click', function () {
                loadWordCloud();
                loadFeed();
            });
        }

        window.yajiReloadFeed = function () {
            loadWordCloud();
            loadFeed();
        };

        loadWordCloud();
        loadFeed();
    };
})(window);
