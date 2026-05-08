'use strict';

const express   = require('express');
const http      = require('http');
const WebSocket = require('ws');
const QRCode    = require('qrcode');
const os        = require('os');
const path      = require('path');

// ── Config ────────────────────────────────────────────────────────────────────
const PORT = parseInt(process.env.PORT || '3000', 10);

// ── App Setup ─────────────────────────────────────────────────────────────────
const app    = express();
const server = http.createServer(app);
const wss    = new WebSocket.Server({ server });

// Map of connected controllers: id -> { ws, name, state }
const controllers = new Map();
let nextId = 1;

// ── Utilities ─────────────────────────────────────────────────────────────────
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



// ── Controller WebSocket Server ───────────────────────────────────────────────
wss.on('connection', (ws) => {
    const id           = `ctrl_${nextId++}`;
    const playerNumber = nextId - 1;

    controllers.set(id, {
        ws,
        name:  `Player ${playerNumber}`,
        state: { throttle: 0, steering: 0, boost: false, ability: false },
    });

    log('Controller', `${id} connected (${controllers.size} total)`);

    // Tell the browser its assigned ID
    safeSend(ws, { type: 'assigned', id, playerNumber });

    ws.on('message', (raw) => {
        let msg;
        try { msg = JSON.parse(raw.toString()); } catch { return; }
        handleControllerMessage(id, msg);
    });

    ws.on('close', () => {
        controllers.delete(id);
        log('Controller', `${id} disconnected (${controllers.size} remaining)`);
    });

    ws.on('error', (err) => log('Controller', `${id} error: ${err.message}`));
});

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
    }
}

// ── Routes ────────────────────────────────────────────────────────────────────
app.use(express.static(path.join(__dirname, 'public')));

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
    log('Server', `QR debug page  : http://${ip}:${PORT}/qr`);
});
