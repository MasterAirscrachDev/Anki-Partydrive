'use strict';

const express   = require('express');
const http      = require('http');
const WebSocket = require('ws');
const QRCode    = require('qrcode');
const os        = require('os');
const path      = require('path');
const fs        = require('fs');

// ── Config ────────────────────────────────────────────────────────────────────
const PORT = parseInt(process.env.PORT || '3000', 10);

// ── Asset path resolution ─────────────────────────────────────────────────────
// After `npm run prebuild`, assets are copied into public/cars and public/abilityicons.
// In production (pkg), they are bundled with the public/ snapshot.
// In dev, falls back to the Unity Assets folder if the public copy doesn't exist yet.
function resolveAssetDir(publicSubDir, unityRelPath) {
    const publicPath = path.join(__dirname, 'public', publicSubDir);
    if (fs.existsSync(publicPath)) return publicPath;
    const devPath = path.join(__dirname, unityRelPath);
    if (fs.existsSync(devPath)) return devPath;
    return publicPath;
}

const carsDir         = resolveAssetDir('cars',         '../Assets/Textures/Cars');
const abilityIconsDir = resolveAssetDir('abilityicons', '../Assets/Textures/AbilityIcons');
const publicDir       = path.join(__dirname, 'public');

// ── Static file preloader ────────────────────────────────────────────────────
// pkg's readFileSync only works reliably when paths are resolvable at compile
// time. Loading everything into memory at startup avoids variable-path reads
// during request handling, which pkg cannot detect or bundle.
const MIME_TYPES = {
    '.html': 'text/html; charset=utf-8',
    '.css':  'text/css; charset=utf-8',
    '.js':   'application/javascript; charset=utf-8',
    '.png':  'image/png',
    '.svg':  'image/svg+xml',
    '.ico':  'image/x-icon',
    '.json': 'application/json',
    '.webp': 'image/webp',
};
const STATIC_FILES = {};
function addStaticFile(urlKey, filePath) {
    try {
        const data = fs.readFileSync(filePath);
        STATIC_FILES[urlKey] = { mime: MIME_TYPES[path.extname(filePath).toLowerCase()] || 'application/octet-stream', data };
    } catch (e) {
        console.warn(`[Static] Could not preload ${urlKey}: ${e.message}`);
    }
}
// Root web files — literal path segments allow pkg static analysis to detect them
addStaticFile('index.html', path.join(__dirname, 'public/index.html'));
addStaticFile('styles.css', path.join(__dirname, 'public/styles.css'));
addStaticFile('script.js',  path.join(__dirname, 'public/script.js'));
// Image files — data is embedded in the JS module at prebuild time;
// require() is statically detected by pkg so no pkg.assets binary globs needed.
const imageManifest = require('./public/image-manifest');
for (const { name, data } of (imageManifest.cars         || []))
    STATIC_FILES[`cars/${name}`]         = { mime: 'image/png', data };
for (const { name, data } of (imageManifest.abilityicons || []))
    STATIC_FILES[`abilityicons/${name}`] = { mime: 'image/png', data };

function pkgSafeStatic() {
    return (req, res, next) => {
        const rel   = (req.path === '/' ? 'index.html' : req.path).replace(/^\/+/, '');
        const entry = STATIC_FILES[rel];
        if (entry) return res.set('Content-Type', entry.mime).send(entry.data);
        next();
    };
}

// ── App Setup ─────────────────────────────────────────────────────────────────
const app    = express();
const server = http.createServer(app);
const wss    = new WebSocket.Server({ server });

// Map of connected controllers: id -> { ws, playerNumber, state }
const controllers = new Map();
let nextId = 1;

// Recently disconnected controllers (for page-reload reconnection)
// id -> { playerNumber, disconnectedAt }
const recentlyDisconnected = new Map();
const RECONNECT_WINDOW_MS = 30_000; // 30 seconds

// Single Unity game connection (null when not connected)
let gameWs = null;

// ── Utilities ─────────────────────────────────────────────────────────────────
function parseCookies(cookieHeader = '') {
    const out = {};
    for (const part of cookieHeader.split(';')) {
        const [k, ...v] = part.trim().split('=');
        if (k) out[k.trim()] = decodeURIComponent(v.join('='));
    }
    return out;
}

function getLocalIP() {
    for (const ifaces of Object.values(os.networkInterfaces())) {
        for (const iface of ifaces) {
            if (iface.family === 'IPv4' && !iface.internal) return iface.address;
        }
    }
    return 'localhost';
}

function log(tag, msg) {
    const ts = new Date().toISOString().slice(11, 19);
    console.log(`[${ts}][${tag}] ${msg}`);
}

function safeSend(ws, data) {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify(data));
    }
}

// ── WebSocket routing ─────────────────────────────────────────────────────────
wss.on('connection', (ws, req) => {
    if (req.url === '/game') {
        handleGameConnection(ws);
    } else {
        handleControllerConnection(ws, req);
    }
});

// ── Unity Game Connection ─────────────────────────────────────────────────────
function handleGameConnection(ws) {
    if (gameWs && gameWs.readyState === WebSocket.OPEN) {
        log('Game', 'Replacing existing game connection');
        gameWs.close();
    }
    gameWs = ws;
    log('Game', 'Unity connected');

    // Send the current state of all connected controllers immediately
    for (const [id, ctrl] of controllers) {
        safeSend(ws, {
            type:         'controller_connected',
            controllerId: id,
            playerNumber: ctrl.playerNumber,
        });
    }

    ws.on('message', (raw) => {
        let msg;
        try { msg = JSON.parse(raw.toString()); } catch { return; }
        handleGameMessage(msg);
    });

    ws.on('close', () => {
        if (gameWs === ws) gameWs = null;
        log('Game', 'Unity disconnected');
    });

    ws.on('error', (err) => log('Game', `Error: ${err.message}`));
}

function handleGameMessage(msg) {
    switch (msg.type) {
        case 'player_state': {
            // Forward to the matching controller browser
            const ctrl = controllers.get(msg.controllerId);
            if (ctrl) safeSend(ctrl.ws, msg);
            break;
        }
        // Future: handle qr_request, broadcast, etc.
    }
}

// ── Controller WebSocket (mobile browsers) ────────────────────────────────────
function handleControllerConnection(ws, req) {
    // Purge stale reconnect entries
    const now = Date.now();
    for (const [rid, entry] of recentlyDisconnected) {
        if (now - entry.disconnectedAt > RECONNECT_WINDOW_MS) recentlyDisconnected.delete(rid);
    }

    // Check if the browser is presenting a saved ID cookie
    const cookies      = parseCookies(req.headers.cookie);
    const savedId      = cookies['pdrive_id'];
    const savedEntry   = savedId ? recentlyDisconnected.get(savedId) : null;
    const isReconnect  = savedEntry && (now - savedEntry.disconnectedAt <= RECONNECT_WINDOW_MS);

    let id, playerNumber;
    if (isReconnect) {
        id           = savedId;
        playerNumber = savedEntry.playerNumber;
        recentlyDisconnected.delete(savedId);
        log('Controller', `${id} reconnected as Player ${playerNumber}`);
    } else {
        id           = `ctrl_${nextId++}`;
        playerNumber = nextId - 1;
        log('Controller', `${id} connected as Player ${playerNumber} (${controllers.size + 1} total)`);
    }

    controllers.set(id, {
        ws,
        playerNumber,
        state: { throttle: 0, steering: 0, boost: false, ability: false },
    });

    // Tell the browser its assigned ID (browser will persist as a cookie)
    safeSend(ws, { type: 'assigned', id, playerNumber });

    // Notify Unity
    safeSend(gameWs, { type: 'controller_connected', controllerId: id, playerNumber });

    ws.on('message', (raw) => {
        let msg;
        try { msg = JSON.parse(raw.toString()); } catch { return; }
        handleControllerMessage(id, msg);
    });

    ws.on('close', () => {
        controllers.delete(id);
        // Keep the ID in the reconnect window so a page reload can reclaim it
        recentlyDisconnected.set(id, { playerNumber, disconnectedAt: Date.now() });
        log('Controller', `${id} disconnected (${controllers.size} remaining)`);
        safeSend(gameWs, { type: 'controller_disconnected', controllerId: id });
    });

    ws.on('error', (err) => log('Controller', `${id} error: ${err.message}`));
}

function handleControllerMessage(id, msg) {
    const ctrl = controllers.get(id);
    if (!ctrl) return;

    switch (msg.type) {
        case 'input': {
            const s = ctrl.state;
            if (typeof msg.throttle === 'number') s.throttle = Math.max(0, Math.min(1, msg.throttle));
            if (typeof msg.steering === 'number') s.steering = Math.max(-1, Math.min(1, msg.steering));
            if (typeof msg.boost    === 'boolean') s.boost    = msg.boost;
            if (typeof msg.ability  === 'boolean') s.ability  = msg.ability;

            // Forward input to Unity with the controller ID attached
            safeSend(gameWs, { type: 'input', controllerId: id, ...s });
            break;
        }

        case 'set_name':
            if (typeof msg.name === 'string') {
                ctrl.name = msg.name.slice(0, 20).trim();
            }
            break;

        case 'ping':
            safeSend(ctrl.ws, { type: 'pong', timestamp: msg.timestamp });
            break;

        case 'ui_input':
            // Forward D-pad / confirm / cancel actions to Unity
            if (typeof msg.action === 'string') {
                safeSend(gameWs, { type: 'ui_input', controllerId: id, action: msg.action });
            }
            break;
    }
}

// ── Routes ────────────────────────────────────────────────────────────────────
app.use(pkgSafeStatic());

// Explicit root fallback — safety net for any edge cases.
app.get('/', (req, res) => {
    const entry = STATIC_FILES['index.html'];
    if (entry) return res.set('Content-Type', entry.mime).send(entry.data);
    res.status(500).send('Server error: index.html not loaded');
});

// /qr.png — raw PNG for Unity texture fetch
app.get('/qr.png', async (req, res) => {
    const ip  = getLocalIP();
    const url = `http://${ip}:${PORT}`;
    const buf = await QRCode.toBuffer(url, { type: 'png', margin: 2, width: 300 });
    res.contentType('image/png').send(buf);
});

// /qr — debug page showing the controller QR code
app.get('/qr', async (req, res) => {
    const ip  = getLocalIP();
    const url = `http://${ip}:${PORT}`;
    const qrSvg = await QRCode.toString(url, { type: 'svg', margin: 2, width: 300 });
    res.send(`<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Partydrive Remote — QR</title>
<link href="https://fonts.googleapis.com/css2?family=Syne+Mono&display=swap" rel="stylesheet">
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{background:#0a0a0a;color:#fff;font-family:'Syne Mono',monospace;
       min-height:100vh;display:flex;flex-direction:column;
       align-items:center;justify-content:center;gap:24px;padding:24px}
  h1{font-size:18px;letter-spacing:3px;text-transform:uppercase;
     color:#ff6b00;text-shadow:0 0 12px rgba(255,107,0,.8)}
  .qr-wrap{background:#fff;padding:16px;border:4px solid #b026ff;
            box-shadow:0 0 30px rgba(176,38,255,.6)}
  .qr-wrap svg{display:block}
  .url{font-size:15px;color:#b0b0b0;letter-spacing:1px;
       border:1px solid #2a2a2a;padding:10px 20px;
       background:#151515;border-left:4px solid #ff6b00}
  .hint{font-size:11px;color:#555;letter-spacing:2px;text-transform:uppercase}
  .controllers{font-size:12px;color:#00ff88;letter-spacing:1px}
</style>
</head>
<body>
  <h1>Partydrive Mobile Remote</h1>
  <div class="qr-wrap">${qrSvg}</div>
  <div class="url">${url}</div>
  <div class="hint">Scan to open the controller on your phone</div>
  <div class="controllers">Controllers connected: ${controllers.size}</div>
</body>
</html>`);
});

// ── Start ─────────────────────────────────────────────────────────────────────
server.listen(PORT, () => {
    const ip = getLocalIP();
    log('Server', 'Partydrive Mobile Remote ready');
    log('Server', `Controller URL : http://${ip}:${PORT}`);
    log('Server', `QR debug page  : http://${ip}:${PORT}/qr`);});
