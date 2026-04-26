/**
 * 全屏 WebGL：对截图纹理做极坐标漩涡 + 吸入中心 + 渐黑
 * 梨园梦境西围房角色扮演对镜如梦屏幕发生扭曲的效果。
 * 基于 GLSL 的 WebGL 片元着色器，
 * 通过极坐标系下的距离衰减与角度偏移算法，
 * 实现了实时的黑洞吸入转场特效，并且做了 CPU 降级处理来保障不同设备的兼容性。
 */
(function (w) {
    function compileShader(gl, type, src) {
        var sh = gl.createShader(type);
        gl.shaderSource(sh, src);
        gl.compileShader(sh);
        if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS)) {
            gl.deleteShader(sh);
            return null;
        }
        return sh;
    }

    function linkProgram(gl, vsSrc, fsSrc) {
        var vs = compileShader(gl, gl.VERTEX_SHADER, vsSrc);
        var fs = compileShader(gl, gl.FRAGMENT_SHADER, fsSrc);
        if (!vs || !fs) return null;
        var p = gl.createProgram();
        gl.attachShader(p, vs);
        gl.attachShader(p, fs);
        gl.linkProgram(p);
        gl.deleteShader(vs);
        gl.deleteShader(fs);
        if (!gl.getProgramParameter(p, gl.LINK_STATUS)) return null;
        return p;
    }

    var VS = [
        'attribute vec2 a_pos;',
        'varying vec2 v_uv;',
        'void main(){',
        '  v_uv=vec2((a_pos.x+1.0)*0.5,1.0-(a_pos.y+1.0)*0.5);',
        '  gl_Position=vec4(a_pos,0.0,1.0);',
        '}'
    ].join('');

    var FS = [
        'precision mediump float;',
        'varying vec2 v_uv;',
        'uniform sampler2D u_tex;',
        'uniform vec2 u_res;',
        'uniform vec2 u_center;',
        'uniform float u_swirl;',
        'uniform float u_pull;',
        'uniform float u_darken;',
        'void main(){',
        '  vec2 px=v_uv*u_res;',
        '  vec2 d=px-u_center;',
        '  float r=length(d);',
        '  float ang=atan(d.y,d.x);',
        '  float maxR=length(u_res)*0.92;',
        '  float falloff=1.0-smoothstep(0.0,maxR,r);',
        '  falloff=falloff*falloff*falloff;',
        '  float twist=u_swirl*falloff;',
        '  float newA=ang+twist;',
        '  float pullAmt=u_pull*falloff;',
        '  float r2=r*(1.0-pullAmt*0.42);',
        '  vec2 srcPx=u_center+r2*vec2(cos(newA),sin(newA));',
        '  vec2 srcUv=srcPx/u_res;',
        '  srcUv.y=1.0-srcUv.y;',
        '  vec4 col;',
        '  if(srcUv.x<0.0||srcUv.x>1.0||srcUv.y<0.0||srcUv.y>1.0){',
        '    col=vec4(0.02,0.01,0.01,1.0);',
        '  }else{',
        '    col=texture2D(u_tex,srcUv);',
        '  }',
        '  col.rgb=mix(col.rgb,vec3(0.0),u_darken*0.94);',
        '  gl_FragColor=vec4(col.rgb,1.0);',
        '}'
    ].join('');

    /**
     * @param {HTMLCanvasElement} canvas 已与截图同尺寸的 canvas（css 全屏铺滿）
     * @param {HTMLCanvasElement|HTMLImageElement} imageCanvas html2canvas 输出或已 scale 到同尺寸的画布
     * @param {number} cx 漩涡中心 x（像素，与 texture 左上对齐）
     * @param {number} cy 漩涡中心 y
     * @param {number} durationMs
     * @param {function} onDone
     */
    /**
     * @returns {boolean} 是否已成功开始动画（失败时勿跳转，由外层走兜底）
     */
    w.runDreamVortex = function (canvas, imageCanvas, cx, cy, durationMs, onDone) {
        var gl = canvas.getContext('webgl', { premultipliedAlpha: false, alpha: true, antialias: false });
        if (!gl) {
            return false;
        }

        var W = canvas.width;
        var H = canvas.height;
        if (!W || !H) {
            return false;
        }
        gl.viewport(0, 0, W, H);

        var prog = linkProgram(gl, VS, FS);
        if (!prog) {
            return false;
        }

        var tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, 0);
        try {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, imageCanvas);
        } catch (e) {
            return false;
        }
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

        var buf = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, buf);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]), gl.STATIC_DRAW);

        var loc = gl.getAttribLocation(prog, 'a_pos');
        gl.enableVertexAttribArray(loc);
        gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);

        var u_tex = gl.getUniformLocation(prog, 'u_tex');
        var u_res = gl.getUniformLocation(prog, 'u_res');
        var u_center = gl.getUniformLocation(prog, 'u_center');
        var u_swirl = gl.getUniformLocation(prog, 'u_swirl');
        var u_pull = gl.getUniformLocation(prog, 'u_pull');
        var u_darken = gl.getUniformLocation(prog, 'u_darken');

        var t0 = performance.now();
        var dur = durationMs || 2600;

        function drawAt(now) {
            var t = Math.min(1, (now - t0) / dur);
            var e = t * t * (3 - 2 * t);

            gl.useProgram(prog);
            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, tex);
            gl.uniform1i(u_tex, 0);
            gl.uniform2f(u_res, W, H);
            gl.uniform2f(u_center, cx, cy);
            gl.uniform1f(u_swirl, e * 18.0 * Math.PI);
            gl.uniform1f(u_pull, e);
            gl.uniform1f(u_darken, Math.pow(e, 0.82));

            gl.disable(gl.BLEND);
            gl.drawArrays(gl.TRIANGLES, 0, 6);
            return t;
        }

        drawAt(performance.now());

        function frame(now) {
            var t = drawAt(now);
            if (t < 1) {
                requestAnimationFrame(frame);
            } else if (onDone) {
                setTimeout(onDone, 120);
            }
        }

        requestAnimationFrame(frame);
        return true;
    };

    /**
     * WebGL 不可用时：缩小采样 + 逐像素逆向映射漩涡（与 runDreamVortex 同一套极坐标公式），保证可见动画。
     */
    w.runDreamSwirl2d = function (displayCanvas, imageCanvas, cx, cy, durationMs, onDone) {
        var sw = imageCanvas.width;
        var sh = imageCanvas.height;
        if (!sw || !sh) return false;

        var maxSide = 260;
        var scale = Math.min(maxSide / sw, maxSide / sh, 1);
        var w = Math.max(32, Math.round(sw * scale));
        var h = Math.max(32, Math.round(sh * scale));

        var off = document.createElement('canvas');
        off.width = w;
        off.height = h;
        var octx = off.getContext('2d', { willReadFrequently: true });
        if (!octx) return false;
        octx.drawImage(imageCanvas, 0, 0, sw, sh, 0, 0, w, h);

        var imgData;
        try {
            imgData = octx.getImageData(0, 0, w, h);
        } catch (e) {
            return false;
        }

        var orig = new Uint8ClampedArray(imgData.data);
        var out = octx.createImageData(w, h);
        var cx2 = cx * scale;
        var cy2 = cy * scale;
        var maxR = Math.sqrt(w * w + h * h) * 0.92;

        displayCanvas.width = sw;
        displayCanvas.height = sh;
        var dctx = displayCanvas.getContext('2d');
        if (!dctx) return false;

        var t0 = performance.now();
        var dur = durationMs || 2600;

        function smoothstep01(edge0, edge1, x) {
            var t = Math.max(0, Math.min(1, (x - edge0) / (edge1 - edge0)));
            return t * t * (3 - 2 * t);
        }

        function drawFrame(now) {
            var t = Math.min(1, (now - t0) / dur);
            var e = t * t * (3 - 2 * t);
            var swirl = e * 18.0 * Math.PI;
            var pull = e;
            var dark = Math.pow(e, 0.82) * 0.94;

            var od = out.data;
            var j, i, idx, sx, sy, xi, yi, oidx;
            var dx, dy, r, ang, sp, falloff, twist, newA, r2;
            for (j = 0; j < h; j++) {
                for (i = 0; i < w; i++) {
                    dx = i - cx2;
                    dy = j - cy2;
                    r = Math.sqrt(dx * dx + dy * dy);
                    ang = Math.atan2(dy, dx);
                    sp = smoothstep01(0, maxR, r);
                    falloff = 1 - sp;
                    falloff = falloff * falloff * falloff;
                    twist = swirl * falloff;
                    newA = ang + twist;
                    r2 = r * (1 - pull * falloff * 0.42);
                    sx = cx2 + r2 * Math.cos(newA);
                    sy = cy2 + r2 * Math.sin(newA);
                    xi = Math.round(sx);
                    yi = Math.round(sy);
                    if (xi < 0) xi = 0;
                    else if (xi >= w) xi = w - 1;
                    if (yi < 0) yi = 0;
                    else if (yi >= h) yi = h - 1;
                    oidx = (yi * w + xi) * 4;
                    idx = (j * w + i) * 4;
                    od[idx] = (orig[oidx] * (1 - dark)) | 0;
                    od[idx + 1] = (orig[oidx + 1] * (1 - dark)) | 0;
                    od[idx + 2] = (orig[oidx + 2] * (1 - dark)) | 0;
                    od[idx + 3] = orig[oidx + 3];
                }
            }

            octx.putImageData(out, 0, 0);
            dctx.drawImage(off, 0, 0, w, h, 0, 0, sw, sh);

            if (t < 1) {
                requestAnimationFrame(drawFrame);
            } else if (onDone) {
                setTimeout(onDone, 120);
            }
        }

        drawFrame(performance.now());
        return true;
    };
})(window);
