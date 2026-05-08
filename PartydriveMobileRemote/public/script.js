'use strict';

// ── State ──────────────────────────────────────────────────────────────────────
const input = {
    throttle: 0,    // 0 .. 1
    steering: 0,    // -1 .. 1
    boost:    false,
    ability:  false,
};

let steerLeftHeld  = false;
let steerRightHeld = false;
let gyroEnabled    = false;
let throttleDragging = false;

let ws           = null;
let playerId     = null;
let inputDirty   = false;
let sendLoopId   = null;

// ── Element References ─────────────────────────────────────────────────────────
const connIndicator  = document.getElementById('conn-indicator');
const playerNumEl    = document.getElementById('player-num');
const playerPosEl    = document.getElementById('player-pos');
const energyArc      = document.getElementById('energy-arc');
const energyRingVal  = document.getElementById('energy-ring-val');
const ENERGY_CIRC    = 2 * Math.PI * 38; // stroke-dasharray matches r=38
const gyroBtn        = document.getElementById('gyro-btn');
const gyroOverlay    = document.getElementById('gyro-overlay');
const throttleTrack  = document.getElementById('throttle-track');
const throttleBar    = document.getElementById('throttle-fill-bar');
const throttleHandle = document.getElementById('throttle-handle');
const throttlePct    = document.getElementById('throttle-pct');
const btnAbility     = document.getElementById('btn-ability');
const btnBoost       = document.getElementById('btn-boost');
const btnSteerLeft   = document.getElementById('btn-steer-left');
const btnSteerRight  = document.getElementById('btn-steer-right');
const steerFill      = document.getElementById('steer-fill');
const abilityIcon    = document.getElementById('ability-icon');

// ── WebSocket ──────────────────────────────────────────────────────────────────
function connect() {
    setConnStatus('connecting');
    ws = new WebSocket(`ws://${location.host}`);

    ws.addEventListener('open', () => {
        setConnStatus('connected');
        startSendLoop();
    });

    ws.addEventListener('message', (e) => {
        try { handleServerMsg(JSON.parse(e.data)); } catch (_) { /* ignore malformed */ }
    });

    ws.addEventListener('close', () => {
        setConnStatus('disconnected');
        stopSendLoop();
        setTimeout(connect, 3000);
    });

    ws.addEventListener('error', () => ws.close());
}

function handleServerMsg(msg) {
    switch (msg.type) {
        case 'assigned':
            playerId = msg.id;
            playerNumEl.textContent = msg.playerNumber ?? '?';
            break;

        case 'game_state':
            if (Array.isArray(msg.players)) {
                const me = msg.players.find(p => p.controllerId === playerId);
                if (me) updateHUD(me);
            }
            break;

        case 'player_state':
            if (msg.controllerId === playerId) updateHUD(msg);
            break;

        case 'ability_update':
            if (msg.icon) abilityIcon.textContent = msg.icon;
            break;
    }
}

function updateHUD(data) {
    if (data.position != null) playerPosEl.textContent = toOrdinal(data.position);
    if (data.energy   != null) {
        const pct = Math.max(0, Math.min(100, data.energy));
        energyArc.style.strokeDashoffset = ENERGY_CIRC * (1 - pct / 100);
        energyRingVal.textContent = `${Math.round(pct)}%`;
    }
}

function toOrdinal(n) {
    const suffixes = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (suffixes[(v - 20) % 10] || suffixes[v] || suffixes[0]);
}

function setConnStatus(status) {
    const labels = { connected: '● ONLINE', disconnected: '● OFFLINE', connecting: '● CONNECTING' };
    connIndicator.className  = `conn-badge ${status}`;
    connIndicator.textContent = labels[status] || labels.disconnected;
}

// ── Send Loop (20 hz) ──────────────────────────────────────────────────────────
function startSendLoop() {
    if (sendLoopId) return;
    sendLoopId = setInterval(flushInput, 50);
}

function stopSendLoop() {
    clearInterval(sendLoopId);
    sendLoopId = null;
}

function markDirty() { inputDirty = true; }

function flushInput() {
    if (!inputDirty) return;
    if (!ws || ws.readyState !== WebSocket.OPEN) return;
    inputDirty = false;
    ws.send(JSON.stringify({ type: 'input', ...input }));
}

// ── Throttle Slider ────────────────────────────────────────────────────────────
function throttleFromY(clientY) {
    const rect = throttleTrack.getBoundingClientRect();
    return Math.max(0, Math.min(1, 1 - (clientY - rect.top) / rect.height));
}

function setThrottle(value) {
    input.throttle = value;
    const pct = Math.round(value * 100);
    throttleBar.style.height    = `${pct}%`;
    throttleHandle.style.bottom = `${pct}%`;
    throttlePct.textContent     = `${pct}%`;
    markDirty();
}

// Touch – events stay bound to the source element, so touchmove works even
// when the finger drifts outside the track bounds.
throttleTrack.addEventListener('touchstart', (e) => {
    e.preventDefault();
    throttleDragging = true;
    setThrottle(throttleFromY(e.touches[0].clientY));
}, { passive: false });

throttleTrack.addEventListener('touchmove', (e) => {
    e.preventDefault();
    if (throttleDragging) setThrottle(throttleFromY(e.touches[0].clientY));
}, { passive: false });

throttleTrack.addEventListener('touchend',   () => { throttleDragging = false; });
throttleTrack.addEventListener('touchcancel', () => { throttleDragging = false; });

// Mouse fallback (useful for desktop testing)
throttleTrack.addEventListener('mousedown', (e) => {
    throttleDragging = true;
    setThrottle(throttleFromY(e.clientY));
});
document.addEventListener('mousemove', (e) => {
    if (throttleDragging) setThrottle(throttleFromY(e.clientY));
});
document.addEventListener('mouseup', () => { throttleDragging = false; });

// ── Steering ───────────────────────────────────────────────────────────────────
function applySteering() {
    if (!gyroEnabled) {
        if      (steerLeftHeld  && !steerRightHeld) input.steering = -1;
        else if (steerRightHeld && !steerLeftHeld)  input.steering =  1;
        else                                         input.steering =  0;
    }
    updateSteerVisual();
    markDirty();
}

function updateSteerVisual() {
    const v      = input.steering; // -1 .. 1
    const halfW  = Math.abs(v) * 50;
    steerFill.style.left  = v >= 0 ? '50%' : `${50 + v * 50}%`;
    steerFill.style.width = `${halfW}%`;
}

// Bind a button so it behaves as a held control on both touch and mouse
function bindHold(el, onDown, onUp) {
    el.addEventListener('touchstart',  (e) => { e.preventDefault(); onDown(); }, { passive: false });
    el.addEventListener('touchend',    (e) => { e.preventDefault(); onUp();   }, { passive: false });
    el.addEventListener('touchcancel', (e) => { e.preventDefault(); onUp();   }, { passive: false });
    el.addEventListener('mousedown', onDown);
    el.addEventListener('mouseup',   onUp);
}

bindHold(btnSteerLeft,
    () => { steerLeftHeld = true;  btnSteerLeft.classList.add('pressed');    applySteering(); },
    () => { steerLeftHeld = false; btnSteerLeft.classList.remove('pressed'); applySteering(); }
);

bindHold(btnSteerRight,
    () => { steerRightHeld = true;  btnSteerRight.classList.add('pressed');    applySteering(); },
    () => { steerRightHeld = false; btnSteerRight.classList.remove('pressed'); applySteering(); }
);

// Global mouseup safety net (finger/cursor releases outside element)
document.addEventListener('mouseup', () => {
    if (steerLeftHeld)  { steerLeftHeld  = false; btnSteerLeft.classList.remove('pressed');  applySteering(); }
    if (steerRightHeld) { steerRightHeld = false; btnSteerRight.classList.remove('pressed'); applySteering(); }
    if (input.boost)    { input.boost    = false; btnBoost.classList.remove('pressed');   markDirty(); }
    if (input.ability)  { input.ability  = false; btnAbility.classList.remove('pressed'); markDirty(); }
});

// ── Action Buttons ─────────────────────────────────────────────────────────────
bindHold(btnBoost,
    () => { input.boost = true;  btnBoost.classList.add('pressed');    markDirty(); },
    () => { input.boost = false; btnBoost.classList.remove('pressed'); markDirty(); }
);

bindHold(btnAbility,
    () => { input.ability = true;  btnAbility.classList.add('pressed');    markDirty(); },
    () => { input.ability = false; btnAbility.classList.remove('pressed'); markDirty(); }
);

// ── Gyroscope ──────────────────────────────────────────────────────────────────
const GYRO_DEAD_ZONE = 5;   // degrees
const GYRO_MAX_TILT  = 30;  // degrees → full deflection

function onDeviceOrientation(e) {
    if (!gyroEnabled) return;
    const gamma = e.gamma ?? 0; // left/right tilt: -90 to +90
    let steering = 0;
    if (Math.abs(gamma) > GYRO_DEAD_ZONE) {
        steering = (gamma - Math.sign(gamma) * GYRO_DEAD_ZONE) / (GYRO_MAX_TILT - GYRO_DEAD_ZONE);
        steering = Math.max(-1, Math.min(1, steering));
    }
    input.steering = steering;
    updateSteerVisual();
    markDirty();
}

function enableGyro() {
    gyroEnabled = true;
    gyroBtn.textContent = 'GYRO: ON';
    gyroBtn.classList.add('active');
    btnSteerLeft.style.visibility  = 'hidden';
    btnSteerRight.style.visibility = 'hidden';
    window.addEventListener('deviceorientation', onDeviceOrientation);
}

function disableGyro() {
    gyroEnabled = false;
    gyroBtn.textContent = 'GYRO: OFF';
    gyroBtn.classList.remove('active');
    btnSteerLeft.style.visibility  = '';
    btnSteerRight.style.visibility = '';
    window.removeEventListener('deviceorientation', onDeviceOrientation);
    input.steering = 0;
    updateSteerVisual();
    markDirty();
}

gyroBtn.addEventListener('click', () => {
    if (gyroEnabled) { disableGyro(); return; }

    // iOS 13+ requires an explicit permission request
    if (typeof DeviceOrientationEvent !== 'undefined' &&
        typeof DeviceOrientationEvent.requestPermission === 'function') {
        gyroOverlay.classList.remove('hidden');
    } else {
        enableGyro();
    }
});

document.getElementById('gyro-permit-btn').addEventListener('click', () => {
    DeviceOrientationEvent.requestPermission()
        .then((permResult) => {
            gyroOverlay.classList.add('hidden');
            if (permResult === 'granted') enableGyro();
        })
        .catch(() => gyroOverlay.classList.add('hidden'));
});

document.getElementById('gyro-cancel-btn').addEventListener('click', () => {
    gyroOverlay.classList.add('hidden');
});

// ── Orientation lock (best-effort) ────────────────────────────────────────────
if (screen.orientation && typeof screen.orientation.lock === 'function') {
    screen.orientation.lock('landscape').catch(() => { /* not supported in all contexts */ });
}

// ── Kick off ──────────────────────────────────────────────────────────────────
connect();
