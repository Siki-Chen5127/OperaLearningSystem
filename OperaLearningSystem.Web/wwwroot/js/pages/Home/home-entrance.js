var isAuthenticated = window.OperaPageConfig.isAuthenticated;
var currentScene = 'main';
var roofModelUrls = window.OperaPageConfig.roofModelUrls;

var roofModelFallbackMap = {
    fast: ['balanced', 'ultra'],
    balanced: ['ultra'],
    ultra: []
};
var roofModelQuality = 'fast';
var roofGltfUrl = roofModelUrls.fast;
var roofGltfFallbackUrl = roofModelUrls.balanced;
var roofGltfFinalFallbackUrl = roofModelUrls.ultra;

var roofModelFallbackMap = {
    fast: ['balanced', 'ultra'],
    balanced: ['ultra'],
    ultra: []
};

var roofModelQuality = 'fast';
var roofGltfUrl = roofModelUrls.fast;
var roofGltfFallbackUrl = roofModelUrls.balanced;
var roofGltfFinalFallbackUrl = roofModelUrls.ultra;
var floor3StageRegionsUrl = window.OperaPageConfig.floor3StageRegionsUrl;
var floor3RegionsCache = [];
var roofModelLoaded = false;
var roofModelListenersBound = false;
var roofUnloadTimer = null;
var roofCurrentModelKey = 'fast';
var roofObjectUrl = '';
var roofLoadController = null;
var roofLoadToken = 0;
/** 单文件模型用 Cache API 持久缓存，避免同一会话反复下载（需 HTTPS 或 localhost） */
var ROOF_MODEL_CACHE_NAME = 'opera-roof-models-v1';
/** 仅对流畅使用磁盘缓存；标准仍为每次联网（体量大，可由需要时再开启） */
var roofModelDiskCacheKeys = { fast: true, balanced: false };
/** 按 gltf 引用的外部资源预拉取（与 scene.gltf 同目录解析出的 bin + textures） */
var roofAssetPromise = null;

function resolveRoofAssetUrl(relativePath) {
    return new URL(relativePath, new URL(roofGltfUrl, window.location.href)).href;
}

/** 与磁盘上一致：仅统计 gltf 中声明的外部文件（scene.bin、textures/ 下贴图等） */
function collectGltfExternalUris(gltf) {
    var map = {};
    function add(u) {
        if (u && typeof u === 'string') map[u] = true;
    }
    if (gltf.buffers) {
        for (var i = 0; i < gltf.buffers.length; i++) add(gltf.buffers[i].uri);
    }
    if (gltf.images) {
        for (var j = 0; j < gltf.images.length; j++) add(gltf.images[j].uri);
    }
    var out = [];
    for (var k in map) {
        if (Object.prototype.hasOwnProperty.call(map, k)) out.push(k);
    }
    return out;
}

function updateRoofProgressUI(pctVal, statusText) {
    var bar = document.getElementById('gltf-load-bar-fill');
    var pct = document.getElementById('gltf-load-pct');
    var track = document.getElementById('gltf-progress-track');
    var statusEl = document.getElementById('gltf-poster-status');
    var n = Math.min(100, Math.max(0, Math.round(pctVal)));
    if (bar) bar.style.width = n + '%';
    if (pct) pct.textContent = n + '%';
    if (track) track.setAttribute('aria-valuenow', String(n));
    if (statusEl && statusText !== undefined) statusEl.textContent = statusText;
}

function abortRoofModelLoading() {
    if (roofLoadController) {
        try { roofLoadController.abort(); } catch (e) { }
        roofLoadController = null;
    }
}

function clearRoofObjectUrl() {
    if (roofObjectUrl) {
        try { URL.revokeObjectURL(roofObjectUrl); } catch (e) { }
        roofObjectUrl = '';
    }
}

async function fetchBlobWithRealProgress(url, onProgress, signal) {
    var response = await fetch(url, { signal: signal, cache: 'default' });
    if (!response.ok) throw new Error('fetch failed');
    var total = parseInt(response.headers.get('content-length') || '0', 10);
    if (!response.body) {
        var fallbackBlob = await response.blob();
        onProgress(100);
        return fallbackBlob;
    }
    var reader = response.body.getReader();
    var chunks = [];
    var received = 0;
    while (true) {
        var read = await reader.read();
        if (read.done) break;
        if (!read.value) continue;
        chunks.push(read.value);
        received += read.value.length;
        if (total > 0) onProgress((received / total) * 100);
    }
    if (total <= 0) onProgress(100);
    return new Blob(chunks);
}

function fallbackKeyOf(key) {
    var arr = roofModelFallbackMap[key] || [];
    return arr.length ? arr[0] : '';
}

function roofAbsoluteModelUrl(url) {
    return new URL(url, window.location.href).href;
}

async function tryReadRoofModelFromDiskCache(absUrl) {
    if (!('caches' in window)) return null;
    try {
        var cache = await caches.open(ROOF_MODEL_CACHE_NAME);
        var res = await cache.match(absUrl);
        if (!res || !res.ok) return null;
        var b = await res.blob();
        return (b && b.size > 0) ? b : null;
    } catch (e) {
        return null;
    }
}

function storeRoofModelInDiskCache(absUrl, blob) {
    if (!('caches' in window) || !blob) return;
    caches.open(ROOF_MODEL_CACHE_NAME).then(function (cache) {
        return cache.put(new Request(absUrl), new Response(blob, {
            headers: { 'Content-Type': 'model/gltf-binary' }
        }));
    }).catch(function () { });
}

async function loadRoofModelByKey(viewer, key) {
    roofCurrentModelKey = key;
    roofModelLoaded = false;
    abortRoofModelLoading();
    clearRoofObjectUrl();
    var token = ++roofLoadToken;
    var url = roofModelUrls[key];
    if (!url) throw new Error('no model url');

    if (key === 'ultra') {
        updateRoofProgressUI(8, '正在加载高清模型资源…');
        viewer.setAttribute('src', url);
        return;
    }

    var absUrl = roofAbsoluteModelUrl(url);
    var useDiskCache = !!roofModelDiskCacheKeys[key];
    if (useDiskCache) {
        updateRoofProgressUI(2, '正在检查本地缓存…');
        var cached = await tryReadRoofModelFromDiskCache(absUrl);
        if (token !== roofLoadToken) return;
        if (cached) {
            clearRoofObjectUrl();
            roofObjectUrl = URL.createObjectURL(cached);
            updateRoofProgressUI(100, '已从缓存加载，正在解析模型…');
            viewer.setAttribute('src', roofObjectUrl);
            return;
        }
    }

    updateRoofProgressUI(0, '正在下载模型资源…');
    roofLoadController = new AbortController();
    var blob = await fetchBlobWithRealProgress(url, function (p) {
        if (token !== roofLoadToken) return;
        updateRoofProgressUI(p, '正在下载模型资源…');
    }, roofLoadController.signal);
    if (token !== roofLoadToken) return;
    roofLoadController = null;
    if (useDiskCache) storeRoofModelInDiskCache(absUrl, blob);
    clearRoofObjectUrl();
    roofObjectUrl = URL.createObjectURL(blob);
    updateRoofProgressUI(100, '下载完成，正在解析模型…');
    viewer.setAttribute('src', roofObjectUrl);
}

function resolveRoofUrlsByQuality(q) {
    var key = (q === 'ultra' || q === 'balanced' || q === 'fast') ? q : 'fast';
    var falls = roofModelFallbackMap[key] || [];
    roofGltfUrl = roofModelUrls[key];
    roofGltfFallbackUrl = falls[0] ? roofModelUrls[falls[0]] : '';
    roofGltfFinalFallbackUrl = falls[1] ? roofModelUrls[falls[1]] : '';
    roofModelQuality = key;
}

function refreshModelQualityTip() {
    var tip = document.getElementById('model-quality-tip');
    if (!tip) return;
    if (roofModelQuality === 'ultra') {
        tip.textContent = '提示：极致模型最耗性能，可能导致卡顿或发热，建议仅在高性能设备使用。';
    } else if (roofModelQuality === 'balanced') {
        tip.textContent = '提示：标准画质较均衡；若设备较弱，建议切换到「流畅」。';
    } else {
        tip.textContent = '建议优先使用「流畅 / 标准」，避免设备卡顿。';
    }
}

function changeModelQuality(quality) {
    resolveRoofUrlsByQuality(quality);
    roofModelLoaded = false;
    roofAssetPromise = null;
    abortRoofModelLoading();
    clearRoofObjectUrl();
    try { localStorage.setItem('roof_model_quality', roofModelQuality); } catch (e) { }
    refreshModelQualityTip();
    var viewer = document.getElementById('gltf-model-viewer');
    if (viewer) {
        viewer.removeAttribute('src');
        updateRoofProgressUI(0, '已切换清晰度，重新加载模型…');
    }
}

function initModelQualitySelector() {
    var selected = 'fast';
    try {
        var saved = localStorage.getItem('roof_model_quality');
        if (saved === 'fast' || saved === 'balanced' || saved === 'ultra') selected = saved;
    } catch (e) { }
    resolveRoofUrlsByQuality(selected);
    var selector = document.getElementById('model-quality-select');
    if (selector) selector.value = roofModelQuality;
    refreshModelQualityTip();
}

function bindRoofModelViewerEvents() {
    if (roofModelListenersBound) return;
    roofModelListenersBound = true;
    var viewer = document.getElementById('gltf-model-viewer');
    var poster = document.getElementById('gltf-poster-overlay');
    var bar = document.getElementById('gltf-load-bar-fill');
    var pct = document.getElementById('gltf-load-pct');
    var statusEl = document.getElementById('gltf-poster-status');
    var track = document.getElementById('gltf-progress-track');
    if (!viewer) return;
    /** 不再用 model-viewer 的 progress：其 totalProgress 是内部多阶段加权，常在约 0.88 附近徘徊，与「文件是否下完」不一致 */
    viewer.addEventListener('load', function () {
        roofModelLoaded = true;
        if (poster) poster.classList.add('gltf-poster-hidden');
        if (bar) bar.style.width = '100%';
        if (pct) pct.textContent = '100%';
        if (track) track.setAttribute('aria-valuenow', '100');
        if (statusEl) statusEl.textContent = '';
    });
    viewer.addEventListener('error', function () {
        var nextKey = fallbackKeyOf(roofCurrentModelKey);
        if (nextKey) {
            if (statusEl) statusEl.textContent = '当前模型加载失败，正在切换备用模型…';
            loadRoofModelByKey(viewer, nextKey).catch(function () {
                if (statusEl) statusEl.textContent = '模型加载失败，请稍后重试';
            });
            return;
        }
        if (statusEl) statusEl.textContent = '模型加载失败，请稍后重试';
        if (pct) pct.textContent = '';
    });
}

/**
 * 先按 scene.gltf 清单把 bin + 贴图全部拉进 HTTP 缓存，再设置 src；
 * 进度条 = 已完成文件数 / 清单中外部文件数（与 models/top 下被引用的资源一致）。
 */
function preloadRoofModel() {
    bindRoofModelViewerEvents();
    var viewer = document.getElementById('gltf-model-viewer');
    if (viewer && viewer.getAttribute('src') === roofGltfUrl && roofModelLoaded) {
        return Promise.resolve();
    }
    // 大模型资源（含超大 bin 与贴图）不再做全量预拉取，避免页面占用暴涨。
    // 保留函数仅用于兼容旧调用点。
    return Promise.resolve();
}

document.addEventListener('DOMContentLoaded', () => {
    initModelQualitySelector();
    var ai = document.getElementById('aiWidget');
    if (ai) ai.style.display = 'none';
    hydrateTheaterCards();
    if (!isAuthenticated) return;
    var yslBack = false;
    try {
        yslBack = new URLSearchParams(window.location.search).get('yueshilouReturn') === '1';
    } catch (e) { }
    if (yslBack) {
        sessionStorage.setItem('door_opened', 'true');
        try { window.history.replaceState({}, '', window.location.pathname || '/'); } catch (e2) { }
        jumpToInnerSceneDirectly();
        var mainScene = document.getElementById('scene-main');
        var yslInScene = document.getElementById('scene-yueshilou-in');
        if (mainScene && yslInScene) {
            mainScene.classList.remove('scene-active');
            mainScene.style.display = 'none';
            yslInScene.style.display = 'block';
            yslInScene.classList.add('scene-active');
            currentScene = 'yueshilou-in';
        }
    } else if (sessionStorage.getItem('door_opened') === 'true') {
        jumpToInnerSceneDirectly();
    } else {
        initBrushCursor();
        initDrawDoor();
    }
});

// ================== 笔刷光标 ==================
var brushEl = document.getElementById('brush-cursor');
var brushActive = false;

function initBrushCursor() {
    brushActive = true;
    brushEl.style.display = 'block';
    document.body.style.cursor = 'none';
    document.addEventListener('mousemove', moveBrush);
}
function moveBrush(e) {
    if (!brushActive) return;
    brushEl.style.left = e.clientX + 'px';
    brushEl.style.top = e.clientY + 'px';
}
function hideBrushCursor() {
    brushActive = false;
    if (brushEl) brushEl.style.display = 'none';
    document.body.style.cursor = 'default';
}

// ================== 画门擦除（灰度遮罩绘于 canvas，底层为暂停的开门视频） ==================
function initDrawDoor() {
    const canvas = document.getElementById('scratch-canvas');
    const video = document.getElementById('door-open-video');
    if (!canvas || !video) return;
    const ctx = canvas.getContext('2d', { willReadFrequently: true });
    let isDrawing = false, lastX = 0, lastY = 0;

    function drawSourceCover(source, w, h) {
        var iw = source.videoWidth || source.naturalWidth || source.width;
        var ih = source.videoHeight || source.naturalHeight || source.height;
        if (!iw || !ih) return;
        var r = Math.max(w / iw, h / ih);
        var nw = iw * r, nh = ih * r;
        ctx.drawImage(source, (w - nw) / 2, (h - nh) / 2, nw, nh);
    }

    function repaintGrayscaleMask() {
        // 1. 获取设备的像素比 (DPR)
        const dpr = window.devicePixelRatio || 1;
        const cssWidth = window.innerWidth;
        const cssHeight = window.innerHeight;

        // 2. 放大内部绘图尺寸
        canvas.width = cssWidth * dpr;
        canvas.height = cssHeight * dpr;

        // 3. 固定 CSS 显示尺寸
        canvas.style.width = cssWidth + 'px';
        canvas.style.height = cssHeight + 'px';

        // 4. 重置状态并缩放上下文
        ctx.setTransform(1, 0, 0, 1, 0, 0); // 重置可能存在的变换
        ctx.scale(dpr, dpr); // 整体缩放绘图环境

        // 5. 绘制源画面及滤镜
        ctx.globalCompositeOperation = 'source-over';
        ctx.filter = 'grayscale(100%) brightness(80%) sepia(30%)';
        drawSourceCover(video, cssWidth, cssHeight);
        ctx.filter = 'none';

        // 6. 重新设置擦除画笔参数，保证橡皮擦粗细在不同设备上观感一致
        ctx.lineWidth = 150;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
    }
    function onVideoReady() {
        try { video.pause(); video.currentTime = 0; } catch (e) { }
        repaintGrayscaleMask();
        window.addEventListener('resize', repaintGrayscaleMask);
        const d = document.getElementById('doorLayer');
        if (d) { d.style.transition = 'opacity 0.35s'; d.style.opacity = '1'; }
    }

    if (video.readyState >= 2) {
        onVideoReady();
    } else {
        video.addEventListener('loadeddata', onVideoReady, { once: true });
    }

    function getXY(e) {
        if (e.touches && e.touches.length) return [e.touches[0].clientX, e.touches[0].clientY];
        return [e.clientX, e.clientY];
    }
    function startDraw(e) {
        isDrawing = true;
        var xy = getXY(e);
        lastX = xy[0]; lastY = xy[1];
    }
    let chk = null;
    function drawStroke(e) {
        if (!isDrawing) return;
        var xy = getXY(e);
        ctx.globalCompositeOperation = 'destination-out';
        ctx.beginPath(); ctx.moveTo(lastX, lastY); ctx.lineTo(xy[0], xy[1]); ctx.stroke();
        lastX = xy[0]; lastY = xy[1];
        if (!chk) chk = setTimeout(checkClear, 400);
    }
    function endDraw() { isDrawing = false; }

    function onTouchStart(e) { e.preventDefault(); startDraw(e); }
    function onTouchMove(e) { e.preventDefault(); drawStroke(e); }

    canvas.addEventListener('mousedown', startDraw);
    canvas.addEventListener('mousemove', drawStroke);
    canvas.addEventListener('mouseup', endDraw);
    canvas.addEventListener('mouseout', endDraw);
    canvas.addEventListener('touchstart', onTouchStart, { passive: false });
    canvas.addEventListener('touchmove', onTouchMove, { passive: false });
    canvas.addEventListener('touchend', endDraw);

    function checkClear() {
        chk = null;
        const d = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
        let t = 0, s = 128;
        for (let i = 0; i < d.length; i += s * 4) if (d[i + 3] < 128) t++;
        if (t / (d.length / (s * 4)) > 0.08) {
            canvas.removeEventListener('mousemove', drawStroke);
            canvas.removeEventListener('touchmove', onTouchMove);
            var hintEl = document.getElementById('instruction-hint');
            if (hintEl) {
                hintEl.style.animation = 'none';
                hintEl.style.transition = 'opacity 0.45s ease';
                hintEl.style.opacity = '0';
                setTimeout(function () {
                    hintEl.style.display = 'none';
                    hintEl.style.visibility = 'hidden';
                }, 480);
            }
            canvas.style.transition = 'opacity 2s'; canvas.style.opacity = '0';
            setTimeout(function () { canvas.style.display = 'none'; showKnockPoint(); }, 2000);
        }
    }
}

// ================== 叩门转场 ==================
function showKnockPoint() {
    var k = document.getElementById('knock-point');
    if (k) { k.style.display = 'flex'; k.style.animation = "fadeIn 1.5s forwards"; }
    hideBrushCursor();
}
window.startKnock = function () {
    var k = document.getElementById('knock-point');
    if (k) k.style.display = 'none';
    var wrap = document.getElementById('doorLayer');
    var video = document.getElementById('door-open-video');
    if (wrap) {
        wrap.style.transition = 'transform 0.6s ease, filter 0.6s ease';
        wrap.style.transform = 'scale(1.03)';
        wrap.style.filter = 'brightness(1.12)';
    }
    function enterMainStage() {
        if (video) {
            video.onended = null;
        }
        if (wrap) {
            wrap.style.transition = 'opacity 1s ease';
            wrap.style.opacity = '0';
        }
        setTimeout(function () {
            if (wrap) {
                wrap.style.visibility = 'hidden';
                wrap.style.pointerEvents = 'none';
            }
            if (video) {
                try { video.pause(); } catch (e) { }
            }
            var s = document.getElementById('scene-main');
            if (s) {
                s.style.display = 'block';
                requestAnimationFrame(function () { s.classList.add('scene-active'); });
            }
            sessionStorage.setItem('door_opened', 'true');
            showGlobalUI();
            bindEntranceParallaxOnce();
        }, 1000);
    }
    if (video) {
        video.muted = false;
        try { video.currentTime = 0; } catch (e) { }
        var playPromise = video.play();
        if (playPromise && playPromise.catch) playPromise.catch(function () { enterMainStage(); });
        video.onended = function () { enterMainStage(); };
    } else {
        enterMainStage();
    }
};

function jumpToInnerSceneDirectly() {
    hideBrushCursor();
    var dv = document.getElementById('door-open-video');
    if (dv) { try { dv.pause(); } catch (e) { } }
    ['instruction-hint', 'scratch-canvas', 'doorLayer'].forEach(id => {
        var el = document.getElementById(id); if (el) el.style.setProperty('display', 'none', 'important');
    });
    var s = document.getElementById('scene-main');
    if (s) { s.style.display = 'block'; s.classList.add('scene-active'); }
    showGlobalUI();
    bindEntranceParallaxOnce();
}

function showGlobalUI() {
    var btn = document.getElementById('nav-summon-btn');
    if (btn) btn.style.display = 'flex';
    var ai = document.getElementById('aiWidget');
    if (ai) ai.style.setProperty('display', 'block', 'important');
    var bgm = document.getElementById('bgm-container');
    if (bgm) {
        bgm.style.removeProperty('display');
        bgm.style.removeProperty('opacity');
        bgm.style.display = 'block';
        bgm.style.opacity = '1';
    }
    // 取消首页即预载大模型，避免低配置设备出现卡顿或假死。
}

function maybeShowFloor1GuideOnce() {
    if (currentScene !== 'floor1') return;
    if (sessionStorage.getItem('entrance_floor1_guide_v1') === '1') return;
    sessionStorage.setItem('entrance_floor1_guide_v1', '1');
    var chatBoxEl = document.getElementById('aiChatBox');
    if (chatBoxEl && !chatBoxEl.classList.contains('active')) {
        chatBoxEl.classList.add('active');
        sessionStorage.setItem('ai_open', 'true');
    }
    if (typeof appendMsg === 'function') {
        appendMsg('bot', '阁下好眼力，这便是畅音阁最大的戏台——寿台。请先「进入内部」到台前细看；再展开戏折子，即可浏览剧种、剧目与名家。');
    }
}

// ================== 场景切换引擎 ==================
function clearOverlay(delay) {
    var ov = document.getElementById('transition-overlay');
    setTimeout(function () { ov.className = 'transition-overlay'; }, delay || 50);
}
function postSceneHooks(targetId) {
    if (targetId === 'main' || targetId === 'floor3') bindEntranceParallaxOnce();
    if (targetId === 'floor1') {
        resetFloor1ToOuter();
        setTimeout(maybeShowFloor1GuideOnce, 700);
    }
    if (targetId === 'floor2') {
        resetFloor2ToOuter();
    }
}
function cleanInline(el) {
    el.style.transition = ''; el.style.transform = ''; el.style.opacity = ''; el.style.filter = '';
}

function goToScene(targetId, transition) {
    if (currentScene === 'floor1' && targetId !== 'floor1') {
        resetFloor1ToOuter();
    }
    if (currentScene === 'floor2' && targetId !== 'floor2') {
        resetFloor2ToOuter();
    }
    if (currentScene === 'floor3' && targetId !== 'floor3') {
        resetFloor3Ui();
    }
    var hint = document.getElementById('instruction-hint');
    if (hint) hint.style.setProperty('display', 'none', 'important');
    var fromEl = document.getElementById('scene-' + currentScene);
    var toEl = document.getElementById('scene-' + targetId);
    if (!fromEl || !toEl) return;
    var overlay = document.getElementById('transition-overlay');

    if (transition === 'zoomCenter') {
        fromEl.style.transition = 'transform 0.9s cubic-bezier(0.4,0,0.2,1), opacity 0.7s ease, filter 0.7s';
        fromEl.style.transform = 'scale(1.25)';
        fromEl.style.opacity = '0';
        fromEl.style.filter = 'blur(6px) brightness(1.4)';
        overlay.className = 'transition-overlay trans-flash';
        overlay.classList.add('active');
        setTimeout(function () {
            cleanInline(fromEl);
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            toEl.style.display = 'block';
            toEl.style.opacity = '0'; toEl.style.transform = 'scale(1.1)';
            requestAnimationFrame(function () {
                toEl.classList.add('scene-active');
                toEl.style.transition = 'transform 0.9s cubic-bezier(0.25,1,0.5,1), opacity 0.7s ease';
                toEl.style.transform = 'scale(1)'; toEl.style.opacity = '1';
            });
            currentScene = targetId;
            clearOverlay(500);
            postSceneHooks(targetId);
            setTimeout(function () { cleanInline(toEl); }, 1000);
        }, 750);

    } else if (transition === 'zoomOut') {
        overlay.className = 'transition-overlay trans-iris-close';
        overlay.classList.add('active');
        setTimeout(function () {
            cleanInline(fromEl);
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            toEl.style.display = 'block';
            requestAnimationFrame(function () { toEl.classList.add('scene-active'); });
            currentScene = targetId;
            overlay.className = 'transition-overlay trans-iris-open active';
            setTimeout(function () { clearOverlay(50); }, 700);
            postSceneHooks(targetId);
        }, 600);

    } else if (transition === 'fadeUp') {
        overlay.className = 'transition-overlay trans-wipe-up';
        overlay.classList.add('active');
        setTimeout(function () {
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            toEl.style.display = 'block'; toEl.classList.add('enter-from-below');
            requestAnimationFrame(function () {
                toEl.classList.add('scene-active');
                setTimeout(function () { toEl.classList.remove('enter-from-below'); }, 900);
            });
            currentScene = targetId;
            postSceneHooks(targetId);
            clearOverlay(500);
        }, 550);

    } else if (transition === 'fadeDown') {
        overlay.className = 'transition-overlay trans-wipe-down';
        overlay.classList.add('active');
        setTimeout(function () {
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            toEl.style.display = 'block'; toEl.classList.add('enter-from-above');
            requestAnimationFrame(function () {
                toEl.classList.add('scene-active');
                setTimeout(function () { toEl.classList.remove('enter-from-above'); }, 900);
            });
            currentScene = targetId;
            postSceneHooks(targetId);
            clearOverlay(500);
        }, 550);

    } else if (transition === 'turnBack') {
        var container = document.getElementById('entrance-container');
        container.style.transition = 'transform 0.7s cubic-bezier(0.4,0,0.2,1), filter 0.7s';
        container.style.transform = 'perspective(1200px) rotateY(90deg)';
        container.style.filter = 'brightness(0.3)';
        setTimeout(function () {
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            toEl.style.display = 'block';
            requestAnimationFrame(function () { toEl.classList.add('scene-active'); });
            currentScene = targetId;
            container.style.transform = 'perspective(1200px) rotateY(-90deg)';
            requestAnimationFrame(function () {
                container.style.transition = 'transform 0.8s cubic-bezier(0.25,1,0.5,1), filter 0.8s';
                container.style.transform = 'perspective(1200px) rotateY(0deg)';
                container.style.filter = 'brightness(1)';
            });
            setTimeout(function () { cleanInline(container); }, 900);
        }, 700);

    } else if (transition === 'slideLeft' || transition === 'slideRight') {
        var dir = transition === 'slideLeft' ? -1 : 1;
        fromEl.style.transition = 'transform 0.8s cubic-bezier(0.4,0,0.2,1), opacity 0.6s, filter 0.6s';
        fromEl.style.transform = 'translateX(' + (dir * -60) + '%) scale(0.85) rotateY(' + (dir * 15) + 'deg)';
        fromEl.style.opacity = '0'; fromEl.style.filter = 'blur(4px)';
        toEl.style.display = 'block';
        toEl.style.transform = 'translateX(' + (dir * 60) + '%) scale(0.85) rotateY(' + (dir * -15) + 'deg)';
        toEl.style.opacity = '0'; toEl.style.filter = 'blur(4px)';
        requestAnimationFrame(function () {
            toEl.style.transition = 'transform 0.8s cubic-bezier(0.25,1,0.5,1), opacity 0.6s 0.1s, filter 0.6s 0.1s';
            toEl.style.transform = 'translateX(0) scale(1) rotateY(0deg)';
            toEl.style.opacity = '1'; toEl.style.filter = 'blur(0px)';
            toEl.classList.add('scene-active');
        });
        setTimeout(function () {
            fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
            cleanInline(fromEl); cleanInline(toEl);
            currentScene = targetId;
            postSceneHooks(targetId);
        }, 900);

    } else {
        fromEl.style.display = 'none'; fromEl.classList.remove('scene-active');
        toEl.style.display = 'block';
        requestAnimationFrame(function () { toEl.classList.add('scene-active'); });
        currentScene = targetId;
    }
}

// ================== 视差（首页 + 三层福台；只绑定一次，避免重复监听拖慢帧率） ==================
var entranceParallaxBound = false;
function bindEntranceParallaxOnce() {
    if (entranceParallaxBound) return;
    var bg = document.getElementById('parallax-bg');
    if (!bg) return;
    entranceParallaxBound = true;
    document.addEventListener('mousemove', function (e) {
        if (currentScene === 'main') {
            var rx = (e.clientX / window.innerWidth - 0.5);
            var ry = (e.clientY / window.innerHeight - 0.5);
            bg.style.transform = 'translate(' + (-rx * 20) + 'px,' + (-ry * 15) + 'px) scale(1.05)';
        } else if (currentScene === 'floor3') {
            var frame = document.querySelector('#scene-floor3 .floor3-bg-frame');
            if (!frame) return;
            var rx3 = (e.clientX / window.innerWidth - 0.5);
            var ry3 = (e.clientY / window.innerHeight - 0.5);
            frame.style.transform = 'translate(' + (-rx3 * 20) + 'px,' + (-ry3 * 15) + 'px) scale(1.05)';
        }
    }, { passive: true });
}

// ================== 带过渡的页面跳转 ==================
function navigateWithTransition(url) {
    var ov = document.getElementById('transition-overlay');
    ov.className = 'transition-overlay trans-zoom-in';
    ov.classList.add('active');
    setTimeout(() => window.location.href = url, 700);
}

// ================== 一层 · 进入内部 / 戏折子弹窗 ==================
window.addEventListener('message', function (ev) {
    if (!ev.data || ev.data.type !== 'floor1BookMenu') return;
    floor1BookReturnToMenu();
});

function setFloor1NavMode(mode) {
    var navOuter = document.getElementById('floor1-nav-outer');
    var navInner = document.getElementById('floor1-nav-inner');
    if (!navOuter || !navInner) return;
    if (mode === 'inner') {
        navOuter.style.display = 'none';
        navInner.style.display = 'flex';
    } else {
        navOuter.style.display = 'flex';
        navInner.style.display = 'none';
    }
}

function enterFloor1Inner() {
    var overlay = document.getElementById('transition-overlay');
    var outer = document.getElementById('floor1-outer');
    var inner = document.getElementById('floor1-inner');
    if (!outer || !inner) return;
    outer.style.transition = 'transform 0.9s cubic-bezier(0.4,0,0.2,1), opacity 0.7s ease, filter 0.7s';
    outer.style.transform = 'scale(1.25)';
    outer.style.opacity = '0';
    outer.style.filter = 'blur(6px) brightness(1.4)';
    overlay.className = 'transition-overlay trans-flash';
    overlay.classList.add('active');
    setTimeout(function () {
        cleanInline(outer);
        outer.style.display = 'none';
        inner.style.display = 'block';
        inner.style.opacity = '0';
        inner.style.transform = 'scale(1.1)';
        requestAnimationFrame(function () {
            inner.style.transition = 'transform 0.9s cubic-bezier(0.25,1,0.5,1), opacity 0.7s ease';
            inner.style.transform = 'scale(1)';
            inner.style.opacity = '1';
        });
        setFloor1NavMode('inner');
        clearOverlay(500);
        setTimeout(function () { cleanInline(inner); }, 1000);
    }, 750);
}

function backToFloor1Outer() {
    var overlay = document.getElementById('transition-overlay');
    var outer = document.getElementById('floor1-outer');
    var inner = document.getElementById('floor1-inner');
    if (!outer || !inner) return;
    closeFloor1BookModal();
    inner.style.transition = 'transform 0.9s cubic-bezier(0.4,0,0.2,1), opacity 0.7s ease, filter 0.7s';
    inner.style.transform = 'scale(1.25)';
    inner.style.opacity = '0';
    inner.style.filter = 'blur(6px) brightness(1.4)';
    overlay.className = 'transition-overlay trans-flash';
    overlay.classList.add('active');
    setTimeout(function () {
        cleanInline(inner);
        inner.style.display = 'none';
        outer.style.display = 'block';
        outer.style.opacity = '0';
        outer.style.transform = 'scale(1.1)';
        requestAnimationFrame(function () {
            outer.style.transition = 'transform 0.9s cubic-bezier(0.25,1,0.5,1), opacity 0.7s ease';
            outer.style.transform = 'scale(1)';
            outer.style.opacity = '1';
        });
        setFloor1NavMode('outer');
        clearOverlay(500);
        setTimeout(function () { cleanInline(outer); }, 1000);
    }, 750);
}

function resetFloor1ToOuter() {
    var outer = document.getElementById('floor1-outer');
    var inner = document.getElementById('floor1-inner');
    if (outer) outer.style.display = '';
    if (inner) inner.style.display = 'none';
    setFloor1NavMode('outer');
    closeFloor1BookModal();
}

// ================== 二层 · 牌扁弹窗 / 进入内部（同寿台转场） ==================
function openFloor2PaibianModal() {
    var bd = document.getElementById('floor2-paibian-backdrop');
    var modal = document.getElementById('floor2-paibian-modal');
    if (!bd || !modal) return;
    bd.style.display = 'block';
    modal.style.display = 'flex';
    requestAnimationFrame(function () {
        bd.classList.add('open');
        modal.classList.add('open');
    });
}
function closeFloor2PaibianModal() {
    var bd = document.getElementById('floor2-paibian-backdrop');
    var modal = document.getElementById('floor2-paibian-modal');
    if (!bd || !modal) return;
    bd.classList.remove('open');
    modal.classList.remove('open');
    setTimeout(function () {
        bd.style.display = 'none';
        modal.style.display = 'none';
    }, 320);
}



function setFloor2NavMode(mode) {
    var navOuter = document.getElementById('floor2-nav-outer');
    var navInner = document.getElementById('floor2-nav-inner');
    if (!navOuter || !navInner) return;
    if (mode === 'inner') {
        navOuter.style.display = 'none';
        navInner.style.display = 'flex';
    } else {
        navOuter.style.display = 'flex';
        navInner.style.display = 'none';
    }
}

function enterFloor2Inner() {
    var overlay = document.getElementById('transition-overlay');
    var outer = document.getElementById('floor2-outer');
    var inner = document.getElementById('floor2-inner');
    if (!outer || !inner) return;
    outer.style.transition = 'transform 0.9s cubic-bezier(0.4,0,0.2,1), opacity 0.7s ease, filter 0.7s';
    outer.style.transform = 'scale(1.25)';
    outer.style.opacity = '0';
    outer.style.filter = 'blur(6px) brightness(1.4)';
    overlay.className = 'transition-overlay trans-flash';
    overlay.classList.add('active');
    setTimeout(function () {
        cleanInline(outer);
        outer.style.display = 'none';
        inner.style.display = 'block';
        inner.style.opacity = '0';
        inner.style.transform = 'scale(1.1)';
        requestAnimationFrame(function () {
            inner.style.transition = 'transform 0.9s cubic-bezier(0.25,1,0.5,1), opacity 0.7s ease';
            inner.style.transform = 'scale(1)';
            inner.style.opacity = '1';
        });
        setFloor2NavMode('inner');
        clearOverlay(500);
        setTimeout(function () { cleanInline(inner); }, 1000);
    }, 750);
}

function backToFloor2Outer() {
    var overlay = document.getElementById('transition-overlay');
    var outer = document.getElementById('floor2-outer');
    var inner = document.getElementById('floor2-inner');
    if (!outer || !inner) return;
    inner.style.transition = 'transform 0.9s cubic-bezier(0.4,0,0.2,1), opacity 0.7s ease, filter 0.7s';
    inner.style.transform = 'scale(1.25)';
    inner.style.opacity = '0';
    inner.style.filter = 'blur(6px) brightness(1.4)';
    overlay.className = 'transition-overlay trans-flash';
    overlay.classList.add('active');
    setTimeout(function () {
        cleanInline(inner);
        inner.style.display = 'none';
        outer.style.display = 'block';
        outer.style.opacity = '0';
        outer.style.transform = 'scale(1.1)';
        requestAnimationFrame(function () {
            outer.style.transition = 'transform 0.9s cubic-bezier(0.25,1,0.5,1), opacity 0.7s ease';
            outer.style.transform = 'scale(1)';
            outer.style.opacity = '1';
        });
        setFloor2NavMode('outer');
        clearOverlay(500);
        setTimeout(function () { cleanInline(outer); }, 1000);
    }, 750);
}

function resetFloor2ToOuter() {
    var outer = document.getElementById('floor2-outer');
    var inner = document.getElementById('floor2-inner');
    if (outer) {
        outer.style.display = '';
        cleanInline(outer);
    }
    if (inner) {
        inner.style.display = 'none';
        cleanInline(inner);
    }
    setFloor2NavMode('outer');
    closeFloor2PaibianModal();
}

function floor1BookReturnToMenu() {
    var iframe = document.getElementById('floor1-book-iframe');
    var wrap = document.getElementById('floor1-book-embed-wrap');
    var menu = document.getElementById('floor1-book-menu');
    var modal = document.getElementById('floor1-book-modal');
    if (modal) modal.classList.remove('floor1-book-modal--embed');
    if (iframe) iframe.src = 'about:blank';
    if (wrap) wrap.style.display = 'none';
    if (menu) menu.style.display = '';
}

function openFloor1BookEmbed(url) {
    var iframe = document.getElementById('floor1-book-iframe');
    var wrap = document.getElementById('floor1-book-embed-wrap');
    var menu = document.getElementById('floor1-book-menu');
    var modal = document.getElementById('floor1-book-modal');
    if (!iframe || !wrap || !menu || !modal) return;
    modal.classList.add('floor1-book-modal--embed');
    menu.style.display = 'none';
    wrap.style.display = 'flex';
    iframe.src = url;
}

function closeFloor1BookModal() {
    var modal = document.getElementById('floor1-book-modal');
    var dim = document.getElementById('floor1-book-dim');
    var img = document.getElementById('floor1-xizhezi-img');
    if (!modal) {
        floor1BookReturnToMenu();
        return;
    }
    if (modal.style.display === 'none' && !modal.classList.contains('open')) {
        floor1BookReturnToMenu();
        if (dim) {
            dim.classList.remove('open');
            dim.style.display = 'none';
        }
        if (img) {
            img.classList.remove('floor1-xizhezi-hidden', 'floor1-xizhezi-unfolding', 'floor1-xizhezi-busy');
            img.style.visibility = '';
            img.style.opacity = '';
        }
        return;
    }
    modal.classList.remove('open');
    requestAnimationFrame(function () {
        modal.classList.add('is-closing');
    });
    if (dim) dim.classList.remove('open');
    setTimeout(function () {
        modal.classList.remove('is-closing', 'floor1-book-modal--embed');
        modal.style.display = 'none';
        floor1BookReturnToMenu();
        if (dim) dim.style.display = 'none';
        if (img) {
            img.classList.remove('floor1-xizhezi-hidden', 'floor1-xizhezi-unfolding');
            img.style.visibility = '';
            img.style.opacity = '';
            img.classList.remove('floor1-xizhezi-busy');
            img.classList.add('floor1-xizhezi-appear');
            setTimeout(function () {
                img.classList.remove('floor1-xizhezi-appear');
            }, 520);
        }
    }, 420);
}

function openFloor1BookFromScript() {
    var img = document.getElementById('floor1-xizhezi-img');
    var dim = document.getElementById('floor1-book-dim');
    var modal = document.getElementById('floor1-book-modal');
    if (!img || img.classList.contains('floor1-xizhezi-busy')) return;
    img.classList.add('floor1-xizhezi-busy');
    img.classList.remove('floor1-xizhezi-hidden');
    img.style.visibility = '';
    img.style.opacity = '';
    floor1BookReturnToMenu();
    if (dim) {
        dim.style.display = 'block';
        requestAnimationFrame(function () { dim.classList.add('open'); });
    }
    img.classList.add('floor1-xizhezi-unfolding');
    var done = false;
    function afterUnfold() {
        if (done) return;
        done = true;
        img.classList.remove('floor1-xizhezi-unfolding', 'floor1-xizhezi-busy');
        img.classList.add('floor1-xizhezi-hidden');
        if (modal) {
            modal.classList.remove('is-closing', 'floor1-book-modal--embed');
            modal.style.display = 'flex';
            requestAnimationFrame(function () { modal.classList.add('open'); });
        }
    }
    setTimeout(afterUnfold, 900);
}

// ================== 通用工具 ==================
function escapeHtml(s) { var d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

// ================== 三层 · 福台 牌匾 / 卷轴百叶窗 / 戏台详情 ==================
function openFloor3PaibianModal() {
    var bd = document.getElementById('floor3-paibian-backdrop');
    var modal = document.getElementById('floor3-paibian-modal');
    if (!bd || !modal) return;
    bd.style.display = 'block';
    modal.style.display = 'flex';
    requestAnimationFrame(function () {
        bd.classList.add('open');
        modal.classList.add('open');
    });
}
function closeFloor3PaibianModal() {
    var bd = document.getElementById('floor3-paibian-backdrop');
    var modal = document.getElementById('floor3-paibian-modal');
    if (!bd || !modal) return;
    bd.classList.remove('open');
    modal.classList.remove('open');
    setTimeout(function () {
        bd.style.display = 'none';
        modal.style.display = 'none';
    }, 320);
}
function openYueshilouPaibianModal() {
    var bd = document.getElementById('yueshilou-paibian-backdrop');
    var modal = document.getElementById('yueshilou-paibian-modal');
    if (!bd || !modal) return;
    bd.style.display = 'block';
    modal.style.display = 'flex';
    requestAnimationFrame(function () {
        bd.classList.add('open');
        modal.classList.add('open');
    });
}
function closeYueshilouPaibianModal() {
    var bd = document.getElementById('yueshilou-paibian-backdrop');
    var modal = document.getElementById('yueshilou-paibian-modal');
    if (!bd || !modal) return;
    bd.classList.remove('open');
    modal.classList.remove('open');
    setTimeout(function () {
        bd.style.display = 'none';
        modal.style.display = 'none';
    }, 320);
}
function resetFloor3Ui() {
    closeFloor3PaibianModal();
    closeFloor3ScrollsModal(true);
    closeFloor3StageDetail(false);
    var f3f = document.querySelector('#scene-floor3 .floor3-bg-frame');
    if (f3f) f3f.style.transform = '';
}
function floor3BuildScrollStackHtml() {
    return '<div class="floor3-scroll-stack" aria-hidden="true">'
        + '<span class="floor3-scroll-stack__roll floor3-scroll-stack__roll--back"></span>'
        + '<span class="floor3-scroll-stack__roll floor3-scroll-stack__roll--mid"></span>'
        + '<span class="floor3-scroll-stack__roll floor3-scroll-stack__roll--front"></span>'
        + '</div>';
}
function floor3SetRegionPanel(idx) {
    var panel = document.getElementById('floor3-region-panel');
    if (!panel) return;
    panel.classList.remove('floor3-region-panel--open');
    var reg = floor3RegionsCache[idx];
    if (!reg) {
        panel.innerHTML = '';
        return;
    }
    panel.innerHTML = '';
    var head = document.createElement('div');
    head.className = 'floor3-region-panel__head';
    head.textContent = reg.name || '未命名分区';
    panel.appendChild(head);
    if (!reg.stages || !reg.stages.length) {
        var empty = document.createElement('p');
        empty.className = 'floor3-region-panel__empty';
        empty.textContent = '该区域暂无戏台条目，可在后台「戏台知识」中添加。';
        panel.appendChild(empty);
    } else {
        var grid = document.createElement('div');
        grid.className = 'floor3-pick-grid';
        grid.setAttribute('role', 'list');
        reg.stages.forEach(function (st, pickIdx) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'floor3-stage-pick';
            btn.setAttribute('role', 'listitem');
            btn.style.setProperty('--floor3-pick-i', String(pickIdx));
            var nameEl = document.createElement('span');
            nameEl.className = 'floor3-stage-pick__name';
            nameEl.textContent = st.name || '未命名';
            var closed = document.createElement('span');
            closed.className = 'floor3-stage-pick__closed';
            closed.setAttribute('aria-hidden', 'true');
            var capL = document.createElement('span');
            capL.className = 'floor3-stage-pick__knob floor3-stage-pick__knob--left';
            var roll = document.createElement('span');
            roll.className = 'floor3-stage-pick__roll';
            var capR = document.createElement('span');
            capR.className = 'floor3-stage-pick__knob floor3-stage-pick__knob--right';
            closed.appendChild(capL);
            closed.appendChild(roll);
            closed.appendChild(capR);
            btn.appendChild(nameEl);
            btn.appendChild(closed);
            btn.addEventListener('click', function () {
                openFloor3StageDetail({
                    name: st.name || '',
                    imageUrl: st.imageUrl || '/images/default.png',
                    introduction: st.introduction || ''
                });
            });
            grid.appendChild(btn);
        });
        panel.appendChild(grid);
    }
    void panel.offsetWidth;
    panel.classList.add('floor3-region-panel--open');
}
function floor3SetActiveTab(idx) {
    var row = document.getElementById('floor3-blinds-row');
    if (!row) return;
    row.querySelectorAll('.floor3-blind-tab').forEach(function (b, i) {
        var on = i === idx;
        b.classList.toggle('floor3-blind-tab--active', on);
        b.setAttribute('aria-selected', on ? 'true' : 'false');
    });
    floor3SetRegionPanel(idx);
}
function openFloor3ScrollsModal() {
    closeFloor3StageDetail(false);
    var bd = document.getElementById('floor3-scrolls-backdrop');
    var modal = document.getElementById('floor3-scrolls-modal');
    var loadEl = document.getElementById('floor3-scrolls-loading');
    var emptyEl = document.getElementById('floor3-scrolls-empty');
    var row = document.getElementById('floor3-blinds-row');
    var panel = document.getElementById('floor3-region-panel');
    if (!bd || !modal || !row) return;
    floor3RegionsCache = [];
    if (panel) {
        panel.innerHTML = '';
        panel.classList.remove('floor3-region-panel--open');
    }
    bd.style.display = 'block';
    modal.style.display = 'flex';
    if (loadEl) loadEl.style.display = 'block';
    if (emptyEl) emptyEl.style.display = 'none';
    row.innerHTML = '';
    requestAnimationFrame(function () {
        bd.classList.add('open');
        modal.classList.add('open');
    });
    fetch(floor3StageRegionsUrl).then(function (r) { return r.ok ? r.json() : []; }).then(function (regions) {
        if (loadEl) loadEl.style.display = 'none';
        if (!regions || !regions.length) {
            if (emptyEl) emptyEl.style.display = 'block';
            return;
        }
        floor3RegionsCache = regions;
        regions.forEach(function (reg, idx) {
            var tab = document.createElement('button');
            tab.type = 'button';
            tab.className = 'floor3-blind-tab';
            tab.setAttribute('role', 'tab');
            tab.setAttribute('aria-selected', 'false');
            tab.setAttribute('data-idx', String(idx));
            tab.innerHTML = '<div class="floor3-blind-face" aria-hidden="true">' + floor3BuildScrollStackHtml() + '</div><span class="floor3-blind-tab__label">' + escapeHtml(reg.name || '') + '</span>';
            tab.addEventListener('click', function () {
                floor3SetActiveTab(idx);
            });
            row.appendChild(tab);
        });
        floor3SetActiveTab(0);
    }).catch(function () {
        if (loadEl) loadEl.style.display = 'none';
        if (emptyEl) {
            emptyEl.style.display = 'block';
            emptyEl.textContent = '加载失败，请稍后重试。';
        }
    });
}
function closeFloor3ScrollsModal(instant) {
    closeFloor3StageDetail(false);
    var bd = document.getElementById('floor3-scrolls-backdrop');
    var modal = document.getElementById('floor3-scrolls-modal');
    var row = document.getElementById('floor3-blinds-row');
    var panel = document.getElementById('floor3-region-panel');
    if (!bd || !modal) return;
    var delay = instant ? 0 : 200;
    setTimeout(function () {
        bd.classList.remove('open');
        modal.classList.remove('open');
        setTimeout(function () {
            bd.style.display = 'none';
            modal.style.display = 'none';
            if (row) row.innerHTML = '';
            if (panel) {
                panel.innerHTML = '';
                panel.classList.remove('floor3-region-panel--open');
            }
            floor3RegionsCache = [];
        }, 320);
    }, delay);
}
function openFloor3StageDetail(data) {
    var bd = document.getElementById('floor3-stage-detail-backdrop');
    var box = document.getElementById('floor3-stage-detail');
    var inner = document.getElementById('floor3-stage-detail-inner');
    var img = document.getElementById('floor3-stage-detail-img');
    var nm = document.getElementById('floor3-stage-detail-name');
    var intro = document.getElementById('floor3-stage-detail-intro');
    if (!bd || !box || !inner || !img || !nm || !intro) return;
    box.classList.remove('floor3-unroll--open', 'floor3-unroll--closing');
    void box.offsetWidth;
    img.src = data.imageUrl || '/images/default.png';
    img.alt = data.name || '';
    nm.textContent = data.name || '';
    intro.textContent = data.introduction || '';
    bd.style.display = 'block';
    box.style.display = 'flex';
    requestAnimationFrame(function () {
        bd.classList.add('open');
        requestAnimationFrame(function () {
            box.classList.add('floor3-unroll--open');
        });
    });
}
function closeFloor3StageDetail(animateRoll) {
    var bd = document.getElementById('floor3-stage-detail-backdrop');
    var box = document.getElementById('floor3-stage-detail');
    if (!bd || !box) return;
    if (animateRoll) {
        box.classList.remove('floor3-unroll--open');
        box.classList.add('floor3-unroll--closing');
        setTimeout(function () {
            bd.classList.remove('open');
            box.classList.remove('floor3-unroll--closing');
            box.style.display = 'none';
            bd.style.display = 'none';
        }, 700);
    } else {
        box.classList.remove('floor3-unroll--open', 'floor3-unroll--closing');
        bd.classList.remove('open');
        box.style.display = 'none';
        bd.style.display = 'none';
    }
}


// ================== 3D 气泡 ==================
function openModelBubble(titleHtml) {
    var backdrop = document.getElementById('model-viewer-backdrop');
    var modal = document.getElementById('model-viewer-modal');
    var viewer = document.getElementById('gltf-model-viewer');
    var th = document.getElementById('model-bubble-title');
    var poster = document.getElementById('gltf-poster-overlay');
    if (roofUnloadTimer) {
        clearTimeout(roofUnloadTimer);
        roofUnloadTimer = null;
    }
    bindRoofModelViewerEvents();
    if (th) th.innerHTML = titleHtml;
    if (roofModelLoaded && roofCurrentModelKey === roofModelQuality && viewer.getAttribute('src')) {
        if (poster) poster.classList.add('gltf-poster-hidden');
    } else {
        if (poster) poster.classList.remove('gltf-poster-hidden');
        loadRoofModelByKey(viewer, roofModelQuality).catch(function () {
            updateRoofProgressUI(0, '模型加载失败，请稍后重试');
        });
    }
    if (backdrop) {
        backdrop.style.display = 'block';
        requestAnimationFrame(function () { backdrop.classList.add('open'); });
    }
    modal.style.display = 'flex';
    modal.classList.add('open');
}
function open3DViewer() {
    openModelBubble('<i class="fas fa-cube"></i> 畅音阁屋顶构造');
}
function close3DViewer() {
    var backdrop = document.getElementById('model-viewer-backdrop');
    var modal = document.getElementById('model-viewer-modal');
    var viewer = document.getElementById('gltf-model-viewer');
    if (backdrop) backdrop.classList.remove('open');
    modal.classList.remove('open');
    roofUnloadTimer = setTimeout(function () {
        if (backdrop) backdrop.style.display = 'none';
        modal.style.display = 'none';
        roofUnloadTimer = null;
        abortRoofModelLoading();
        clearRoofObjectUrl();
        if (viewer) {
            viewer.removeAttribute('src');
            if (typeof viewer.pause === 'function') viewer.pause();
        }
        roofModelLoaded = false;
        roofAssetPromise = null;
        var poster = document.getElementById('gltf-poster-overlay');
        if (poster) poster.classList.remove('gltf-poster-hidden');
        updateRoofProgressUI(0, '3D 模型加载中…');
    }, 400);
}

async function hydrateTheaterCards() {
    var host = document.getElementById('theater-cards-dynamic');
    if (!host) return;
    try {
        var items = await fetch('/data/theaters.json').then(function (r) { return r.ok ? r.json() : []; });
        if (!items || !items.length) return;
        host.innerHTML = items.map(function (x) {
            return '<div class="theater-card">'
                + '<img src="' + escapeHtml(x.image || '/images/default.png') + '" class="theater-card-img" alt="" onerror="this.src=\'/images/default.png\'" />'
                + '<h3>' + escapeHtml(x.name || '') + '</h3>'
                + '<p>' + escapeHtml(x.description || '') + '</p>'
                + '</div>';
        }).join('');
    } catch (e) {
        // keep server-rendered fallback cards
    }
}

// ================== 高清建筑图片逻辑 ==================
const galleryPhotos = [
    "/images/entrance/changyinge/1.jpg",
    "/images/entrance/changyinge/2.jpg",
    "/images/entrance/changyinge/3.jpg",
    "/images/entrance/changyinge/4.jpg",
    "/images/entrance/changyinge/5.jpg",
    "/images/entrance/changyinge/6.jpg",
    "/images/entrance/changyinge/7.jpg",
    "/images/entrance/changyinge/8.jpg",
    "/images/entrance/changyinge/9.jpg",
    "/images/entrance/changyinge/10.jpg",
    "/images/entrance/changyinge/11.jpg",
    "/images/entrance/changyinge/12.jpg",
];

// 2. 记录当前显示的图片索引
let currentPhotoIndex = 0;

// 打开画廊
window.openHighResPhotos = function (startIndex = 0) {
    var bd = document.getElementById('image-viewer-backdrop');
    var modal = document.getElementById('image-viewer-modal');
    var img = document.getElementById('gallery-main-img');

    if (!bd || !modal || !img) return;

    // --- 核心：在弹窗显示前，先塞入默认图片！---
    currentPhotoIndex = startIndex;
    updateGalleryContent(img);

    // 显示容器
    bd.style.display = 'block';
    modal.style.display = 'flex';

    // 触发动画类 (使用 setTimeout 确保 display 先生效)
    setTimeout(function () {
        bd.classList.add('open');
        modal.classList.add('open');
    }, 10);
};

// 关闭画廊
window.closeHDImageViewer = function () {
    var bd = document.getElementById('image-viewer-backdrop');
    var modal = document.getElementById('image-viewer-modal');
    var img = document.getElementById('gallery-main-img');

    if (!bd || !modal) return;

    // 先移除动画类，触发淡出
    bd.classList.remove('open');
    modal.classList.remove('open');

    // 动画结束后隐藏 DOM 并清理 img
    setTimeout(function () {
        bd.style.display = 'none';
        modal.style.display = 'none';
        if (img) {
            img.src = ""; // 清空图片，防止下次打开看到旧图残影
            img.classList.remove('loaded');
        }
    }, 300); // 对应 CSS 中的 0.3s transition
};

// 切换图片 (方向: -1 上一张, 1 下一张)
window.changeGalleryImage = function (direction) {
    currentPhotoIndex += direction;

    // 循环播放逻辑：如果到了最后一张，再点下一张回到第一张
    if (currentPhotoIndex >= galleryPhotos.length) {
        currentPhotoIndex = 0;
    } else if (currentPhotoIndex < 0) {
        currentPhotoIndex = galleryPhotos.length - 1;
    }

    var img = document.getElementById('gallery-main-img');
    updateGalleryContent(img);
};

// 统一更新图片和页码状态的辅助函数
function updateGalleryContent(imgElement) {
    if (!imgElement) return;

    var counter = document.getElementById('gallery-counter');

    // 1. 切换图之前，先隐藏旧图（触发淡出）
    imgElement.classList.remove('loaded');

    // 2. 塞入新图片的 src
    imgElement.src = galleryPhotos[currentPhotoIndex];

    // 3. 监听加载完成，再显示新图（触发淡入）
    imgElement.onload = function () {
        imgElement.classList.add('loaded');
    };

    // 4. 更新页码文本 (例如 "1 / 3")
    if (counter) {
        counter.innerText = (currentPhotoIndex + 1) + " / " + galleryPhotos.length;
    }
}