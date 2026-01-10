// Overdrive Server Control Panel - JavaScript

// Get color pair and name for car model
function getCarModelInfo(model) {
  const colorPairs = [
    { name: 'Unknown', primary: '#777777', secondary: '#818181' }, 
    { name: 'Kourai', primary: '#FFD700', secondary: '#B8860B' },
    { name: 'Boson', primary: '#C0C0C0', secondary: '#8B0000' },
    { name: 'Rho', primary: '#FF0000', secondary: '#250000' },
    { name: 'Katal', primary: '#0000FF', secondary: '#FFD700' },
    { name: 'Hadion', primary: '#FFA500', secondary: '#000000' },
    { name: 'Spektrix', primary: '#800080', secondary: '#00FFFF' },
    { name: 'Corax', primary: '#000000', secondary: '#FFA500' },
    { name: 'Groundshock', primary: '#0000FF', secondary: '#00FFFF' },
    { name: 'Skull', primary: '#000000', secondary: '#FF0000' },
    { name: 'Thermo', primary: '#FF0000', secondary: '#FFA500' },
    { name: 'Nuke', primary: '#08ac08', secondary: '#000000' },
    { name: 'Guardian', primary: '#FFFFFF', secondary: '#0000FF' },
    { name: 'IceWave', primary: '#FFFFFF', secondary: '#DDDDDD' },
    { name: 'Bigbang', primary: '#4B5320', secondary: '#CD7F32' },
    { name: 'Freewheel', primary: '#008000', secondary: '#D3D3D3' },
    { name: 'x52', primary: '#000000', secondary: '#a10808' },
    { name: 'x52Ice', primary: '#FFFFFF', secondary: '#7ac7e0' },
    { name: 'Mammoth', primary: '#516e6e', secondary: '#797979' },
    { name: 'Dynamo', primary: '#2f2f2f', secondary: '#b0b0b0' },
    { name: 'NukePhantom', primary: '#FFFFFF', secondary: '#000000' },
    { name: 'NeonPrime', primary: '#800080', secondary: '#FFA500' },
  ];
  return colorPairs[model % colorPairs.length];
}

// DOM Elements
const carList = document.getElementById('car-list');
const carData = document.getElementById('car-data');
const lineUpButton = document.getElementById('func-lineup');
const scanTrackButton = document.getElementById('func-scan-track');
const connectionStatus = document.getElementById('connection-status');
const systemLogs = document.getElementById('system-logs');

// State variables
let cars = [];
let availableCars = [];
let directCommanders = [];
let websocket = null;
let wsConnected = false;
let currentTrackData = null;
let selectedFirmwarePath = '';
let showTextInput = false;

// Button Event Listeners
lineUpButton.addEventListener('click', function() { sendWebSocketMessage('lineup', null); });
scanTrackButton.addEventListener('click', function() { sendWebSocketMessage('start_track_scan', 1); });
connectionStatus.addEventListener('click', toggleWebSocket);

// Model name mapping function
function getModelName(modelNumber) {
    const modelInfo = getCarModelInfo(modelNumber);
    return modelInfo.name;
}

// WebSocket Functions
function toggleWebSocket() {
    if (wsConnected) {
        disconnectWebSocket();
    } else {
        connectWebSocket();
    }
}

function connectWebSocket() {
    if (websocket && websocket.readyState === WebSocket.OPEN) {
        addLogEntry('Already connected to WebSocket', 'system');
        return;
    }

    connectionStatus.textContent = 'WebSocket: Connecting...';
    connectionStatus.className = 'connectionStatus connecting';

    websocket = new WebSocket('ws://localhost:7118/ws');

    websocket.onopen = function(event) {
        wsConnected = true;
        connectionStatus.textContent = 'WebSocket: Connected';
        connectionStatus.className = 'connectionStatus connected';
        addLogEntry('WebSocket connected successfully', 'system');
        sendWebSocketMessage('request_cars', null);
        sendWebSocketMessage('get_available_cars', null);
    };

    websocket.onmessage = function(event) {
        try {
            const data = JSON.parse(event.data);
            handleWebSocketMessage(data);
        } catch (error) {
            console.error('Error parsing WebSocket message:', error, event.data);
        }
    };

    websocket.onclose = function(event) {
        wsConnected = false;
        connectionStatus.textContent = 'WebSocket: Disconnected';
        connectionStatus.className = 'connectionStatus disconnected';
        addLogEntry('WebSocket connection closed', 'system');
    };

    websocket.onerror = function(error) {
        addLogEntry('WebSocket error: ' + error, 'system');
    };
}

function disconnectWebSocket() {
    if (websocket) {
        websocket.close();
    }
}

function sendWebSocketMessage(eventType, payload) {
    if (!websocket || websocket.readyState !== WebSocket.OPEN) {
        addLogEntry('WebSocket not connected. Cannot send message: ' + eventType, 'system');
        return;
    }

    const message = {
        EventType: eventType,
        Payload: payload
    };

    websocket.send(JSON.stringify(message));
}

function handleWebSocketMessage(data) {
    if (!data.eventType) {
        addLogEntry('Invalid message format: ' + JSON.stringify(data), 'system');
        return;
    }

    switch (data.eventType) {
        case 'system_log':
            addLogEntry(data.payload, 'system');
            break;
        case 'utility_log':
            handleUtilityMessage(data.payload);
            break;
        case 'car_data':
            handleCarDataUpdate(JSON.parse(data.payload));
            break;
        case 'car_tracking_update':
            handleCarTrackingUpdate(data.payload);
            break;
        case 'track_data':
            currentTrackData = JSON.parse(data.payload);
            renderTrackProgressBar(currentTrackData);
            renderTrackFromData(currentTrackData);
            break;
        case 'available_cars':
            handleAvailableCarsUpdate(JSON.parse(data.payload));
            break;
        default:
            break;
    }
}

// Car Functions
function handleCarDataUpdate(carDataArray) {
    updateCarsDisplay(carDataArray);
}

function handleCarTrackingUpdate(trackingData) {
    const car = cars.find(c => c.id === trackingData.carID);
    if (!car) return;
    
    // Update car tracking data
    if (trackingData.trackIndex !== undefined) {
        car.trackIndex = trackingData.trackIndex;
    }
    if (trackingData.offsetMM !== undefined) {
        car.offsetMM = trackingData.offsetMM;
    }
    if (trackingData.speedMMPS !== undefined) {
        car.speed = trackingData.speedMMPS;
    }
    
    // Update UI elements
    if (car.htmlelement) {
        const carBlock = car.htmlelement;
        
        const carLaneOffsetRead = carBlock.querySelector('#car-lane-offset-read');
        const carLaneOffsetSliderRead = carBlock.querySelector('#car-lane-offset-slider-read');
        if (trackingData.offsetMM !== undefined) {
            if (carLaneOffsetRead) {
                carLaneOffsetRead.innerHTML = 'Lane Offset: ' + trackingData.offsetMM.toFixed(1);
            }
            if (carLaneOffsetSliderRead) {
                carLaneOffsetSliderRead.value = trackingData.offsetMM;
            }
        }
        
        const carSpeedRead = carBlock.querySelector('#car-speed-read');
        if (trackingData.speedMMPS !== undefined && carSpeedRead) {
            carSpeedRead.innerHTML = 'Speed: ' + trackingData.speedMMPS;
        }
    }
    
    // Update car position on track bar
    updateCarProgressPositions();
}

function handleAvailableCarsUpdate(availableCarDataArray) {
    availableCars = availableCarDataArray;
    updateAvailableCarsDisplay();
}

function updateAvailableCarsDisplay() {
    const container = document.getElementById('available-cars-list');
    container.innerHTML = '';
    
    if (availableCars.length === 0) {
        container.innerHTML = '<p style="color: #999; font-style: italic;">No available cars found. Cars appear here automatically when discovered via Bluetooth.</p>';
        return;
    }

    availableCars.forEach(car => {
        const carElement = document.createElement('div');
        carElement.className = 'carBlock';
        carElement.style.minWidth = '200px';
        carElement.style.maxWidth = '250px';
        
        // Apply car model color theme
        const colors = getCarModelInfo(car.model);
        carElement.style.borderLeftColor = colors.primary;
        carElement.style.boxShadow = `0 5px 20px rgba(0, 0, 0, 0.5), 0 0 15px ${colors.primary}33`;
        
        const modelName = getModelName(car.model);
        const timeSinceLastSeen = getRelativeTime(new Date(car.lastSeen));
        
        carElement.innerHTML = `
            <div class="carBlockContent">
                <p style="margin: 2px 0;"><strong>${modelName}</strong></p>
                <p style="margin: 2px 0;">ID: ${car.id}</p>
                <p style="margin: 2px 0;">Last seen: ${timeSinceLastSeen}</p>
                <button onclick="connectToAvailableCar('${car.id}')" 
                    style="width: 100%; margin-top: 10px; background: linear-gradient(135deg, ${colors.primary}, ${colors.secondary}); box-shadow: 0 0 15px ${colors.primary}66;">
                    Connect to Car
                </button>
            </div>
        `;
        
        container.appendChild(carElement);
    });
}

function getRelativeTime(date) {
    const now = new Date();
    const diffMs = now - date;
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    
    if (diffSeconds < 60) {
        return diffSeconds <= 1 ? 'just now' : `${diffSeconds}s ago`;
    } else if (diffMinutes < 60) {
        return `${diffMinutes}m ago`;
    } else if (diffHours < 24) {
        return `${diffHours}h ago`;
    } else {
        return date.toLocaleString();
    }
}

function connectToAvailableCar(carId) {
    sendWebSocketMessage('connect_car', carId);
    addLogEntry(`Connection request sent for car ${carId}`, 'system');
}

function updateCarsDisplay(carDataArray) {
    const currentCarIds = carDataArray.map(car => car.id);
    
    cars = cars.filter(car => {
        if (!currentCarIds.includes(car.id)) {
            if (car.htmlelement && car.htmlelement.parentNode) {
                car.htmlelement.parentNode.removeChild(car.htmlelement);
            }
            directCommanders = directCommanders.filter(commander => commander.id !== car.id);
            return false;
        }
        return true;
    });

    for (let i = 0; i < carDataArray.length; i++) {
        const car = carDataArray[i];
        if (cars.find(c => c.id === car.id)) {
            updateExistingCarDisplay(car);
        } else {
            createNewCarDisplay(car);
        }
    }
}

function updateExistingCarDisplay(car) {
    const existingCar = cars.find(c => c.id === car.id);
    const carBlock = existingCar.htmlelement;
    const carLaneOffsetRead = carBlock.querySelector('#car-lane-offset-read');
    const carLaneOffsetSliderRead = carBlock.querySelector('#car-lane-offset-slider-read');
    const carSpeedRead = carBlock.querySelector('#car-speed-read');
    const carBattery = carBlock.querySelector('#car-battery');
    const carCharging = carBlock.querySelector('#car-charging');

    carLaneOffsetRead.innerHTML = 'Lane Offset: ' + car.offsetMM;
    carLaneOffsetSliderRead.value = car.offsetMM;
    carSpeedRead.innerHTML = 'Speed: ' + car.speedMMPS;
    carCharging.checked = car.charging;
    
    // Update onTrack from CarData
    const carOnTrack = carBlock.querySelector('#car-on-track');
    if (carOnTrack) {
        carOnTrack.checked = car.onTrack;
    }
    
    // Update stored car data
    existingCar.offsetMM = car.offsetMM;
    existingCar.trackIndex = car.trackIndex;
    existingCar.model = car.model;
    existingCar.hasPFeatures = car.hasPFeatures || false;
    
    if (car.batteryStatus === -1) {
        carBattery.innerHTML = 'Battery: Low';
    } else if (car.batteryStatus === 0) {
        carBattery.innerHTML = 'Battery: Normal';
    } else if (car.batteryStatus === 1) {
        carBattery.innerHTML = 'Battery: High';
    } else {
        carBattery.innerHTML = 'Battery: Unknown';
    }
    
    // Update car position on track bar
    updateCarProgressPositions();
}

function createNewCarDisplay(car) {
    const carBlock = document.getElementById('car-data').cloneNode(true);
    carBlock.style.display = 'block';
    
    // Apply car model color theme
    const colors = getCarModelInfo(car.model);
    carBlock.style.borderLeftColor = colors.primary;
    carBlock.style.boxShadow = `0 5px 20px rgba(0, 0, 0, 0.5), 0 0 15px ${colors.primary}33`;
    
    // Store colors on the element for later reference
    carBlock.dataset.primaryColor = colors.primary;
    carBlock.dataset.secondaryColor = colors.secondary;
    
    // ReadOnly Info
    const carName = carBlock.querySelector('#car-name');
    const carID = carBlock.querySelector('#car-id');
    const carLaneOffsetRead = carBlock.querySelector('#car-lane-offset-read');
    const carLaneOffsetSliderRead = carBlock.querySelector('#car-lane-offset-slider-read');
    const carBattery = carBlock.querySelector('#car-battery');
    const carSpeedRead = carBlock.querySelector('#car-speed-read');
    const carCharging = carBlock.querySelector('#car-charging');
    
    carCharging.checked = car.charging;
    carName.innerHTML = 'Name: ' + car.name;
    carID.innerHTML = 'ID: ' + car.id + ' | Model: ' + getModelName(car.model);
    carLaneOffsetRead.innerHTML = 'Lane Offset: ' + car.offsetMM;
    carLaneOffsetSliderRead.value = car.offsetMM;
    carSpeedRead.innerHTML = 'Speed: ' + car.speedMMPS;
    
    if (car.batteryStatus === -1) {
        carBattery.innerHTML = 'Battery: Low';
    } else if (car.batteryStatus === 0) {
        carBattery.innerHTML = 'Battery: Normal';
    } else if (car.batteryStatus === 1) {
        carBattery.innerHTML = 'Battery: High';
    } else {
        carBattery.innerHTML = 'Battery: Unknown';
    }

    // Control Info
    setupCarControls(carBlock, car, colors);
    setupDirectDrive(carBlock, car, colors);
    
    // Apply theme colors to all controls
    applyCarThemeToControls(carBlock, colors);
    
    // Disconnect button
    const disconnectButton = carBlock.querySelector('#disconnect-car-btn');
    disconnectButton.addEventListener('click', function() {
        sendWebSocketMessage('disconnect_car', car.id);
    });

    carList.appendChild(carBlock);

    const carDataObj = {
        id: car.id,
        name: car.name,
        trackPosition: car.trackPosition,
        trackID: car.trackID,
        trackIndex: car.trackIndex || 0,
        speed: car.speed,
        battery: car.battery,
        charging: car.charging,
        offsetMM: car.offsetMM || 0,
        model: car.model || 0,
        hasPFeatures: car.hasPFeatures || false,
        htmlelement: carBlock
    };
    cars.push(carDataObj);
    
    // Update car position on track bar
    updateCarProgressPositions();
}

// Apply car theme colors to all controls in a car block
function applyCarThemeToControls(carBlock, colors) {
    // Apply to all buttons except disconnect
    const buttons = carBlock.querySelectorAll('button:not(#disconnect-car-btn)');
    buttons.forEach(button => {
        button.style.background = `linear-gradient(135deg, ${colors.primary}, ${colors.secondary})`;
        button.style.boxShadow = `0 0 20px ${colors.primary}66`;
    });
    
    // Apply to all range sliders
    const sliders = carBlock.querySelectorAll('input[type="range"]');
    sliders.forEach(slider => {
        slider.style.background = `linear-gradient(90deg, ${colors.secondary} 0%, ${colors.primary} 100%)`;
    });
    
    // Apply to section backgrounds
    const directDriveSection = carBlock.querySelector('.direct-drive-section');
    if (directDriveSection) {
        directDriveSection.style.background = `rgba(${hexToRgb(colors.primary)}, 0.1)`;
        directDriveSection.style.borderColor = colors.primary;
    }
    
    const defaultDriveSection = carBlock.querySelector('.default-drive-section');
    if (defaultDriveSection) {
        defaultDriveSection.style.borderColor = colors.primary;
    }
}

// Helper function to convert hex to RGB
function hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? 
        `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : 
        '255, 107, 0';
}

function setupCarControls(carBlock, car, colors) {
    const carSpeed = carBlock.querySelector('#car-speed');
    const carSpeedSlider = carBlock.querySelector('#car-speed-slider');
    const carLaneOffset = carBlock.querySelector('#car-lane-offset');
    const carLaneOffsetSlider = carBlock.querySelector('#car-lane-offset-slider');
    const carControlButton = carBlock.querySelector('#func-car-control');

    carControlButton.addEventListener('click', function() {
        const speed = carBlock.querySelector('#car-speed-slider').value;
        const laneOffset = carBlock.querySelector('#car-lane-offset-slider').value;
        const payload = `${car.id}:${speed}:${laneOffset}`;
        sendWebSocketMessage('car_move_update', payload);
    });

    carSpeedSlider.addEventListener('input', function() {
        carSpeed.textContent = 'Speed: ' + carSpeedSlider.value;
    });

    carLaneOffsetSlider.addEventListener('input', function() {
        carLaneOffset.textContent = 'Lane Offset: ' + carLaneOffsetSlider.value;
    });
}

function setupDirectDrive(carBlock, car, colors) {
    const enableDirectButton = carBlock.querySelector('#func-enable-direct');
    const directCountdown = carBlock.querySelector('#direct-countdown');
    const joystickControl = carBlock.querySelector('#joystick-control');
    const joystick = carBlock.querySelector('#joystick');
    const joystickKnob = carBlock.querySelector('#joystick-knob');
    const motorValues = carBlock.querySelector('#motor-values');
    
    // Apply theme colors to joystick
    if (joystick) {
        joystick.style.borderColor = colors.primary;
        joystick.style.boxShadow = `inset 0 0 30px ${colors.primary}33, 0 0 20px ${colors.primary}66`;
    }
    if (joystickKnob) {
        joystickKnob.style.background = `radial-gradient(circle, ${colors.primary} 0%, ${colors.secondary} 100%)`;
        joystickKnob.style.borderColor = colors.primary;
        joystickKnob.style.boxShadow = `0 0 20px ${colors.primary}cc`;
    }
    if (motorValues) {
        motorValues.style.color = colors.primary;
    }

    enableDirectButton.addEventListener('click', function() {
        sendWebSocketMessage('enable_direct', car.id);
        
        const defaultDriveSection = carBlock.querySelector('.default-drive-section');
        if (defaultDriveSection) {
            defaultDriveSection.style.display = 'none';
        }
        
        enableDirectButton.style.display = 'none';
        directCountdown.style.display = 'block';
        
        let countdown = 9;
        directCountdown.textContent = `Enabling in ${countdown}...`;
        
        const countdownInterval = setInterval(() => {
            countdown--;
            if (countdown > 0) {
                directCountdown.textContent = `Enabling in ${countdown}...`;
            } else {
                clearInterval(countdownInterval);
                directCountdown.style.display = 'none';
                joystickControl.style.display = 'block';
                setupJoystick(car.id, joystick, joystickKnob, motorValues);
            }
        }, 1000);
    });
}

function updateCarRealTimeData(carId, data) {
    const car = cars.find(c => c.id === carId);
    if (car && car.htmlelement) {
        const carBlock = car.htmlelement;
        
        if (data.speedMMPS !== undefined) {
            const carSpeedRead = carBlock.querySelector('#car-speed-read');
            carSpeedRead.innerHTML = 'Speed: ' + data.speedMMPS;
        }
        
        if (data.offsetMM !== undefined) {
            const carLaneOffsetRead = carBlock.querySelector('#car-lane-offset-read');
            const carLaneOffsetSliderRead = carBlock.querySelector('#car-lane-offset-slider-read');
            carLaneOffsetRead.innerHTML = 'Lane Offset: ' + data.offsetMM;
            carLaneOffsetSliderRead.value = data.offsetMM;
            car.offsetMM = data.offsetMM;
        }
        
        if (data.trackIndex !== undefined) {
            car.trackIndex = data.trackIndex;
        }
        
        if (data.onTrack !== undefined) {
            const carOnTrack = carBlock.querySelector('#car-on-track');
            carOnTrack.checked = data.onTrack;
        }
        
        // Update car position on track bar
        updateCarProgressPositions();
    }
}

// Joystick Setup
function setupJoystick(carId, joystickContainer, knob, motorDisplay) {
    let isDragging = false;
    const radius = joystickContainer.offsetWidth / 2;
    const knobRadius = knob.offsetWidth / 2;
    let useGamepad = true;
    let currentGamepadIndex = 0;

    let commander = directCommanders.find(c => c.id === carId);
    if (!commander) {
        commander = { id: carId, leftCurrent: 0, rightCurrent: 0, leftLastSent: 300, rightLastSent: 300 };
        directCommanders.push(commander);
    }

    function updateJoystickWithMouse(clientX, clientY) {
        const containerRect = joystickContainer.getBoundingClientRect();
        const centerX = containerRect.left + containerRect.width / 2;
        const centerY = containerRect.top + containerRect.height / 2;

        let deltaX = clientX - centerX;
        let deltaY = clientY - centerY;

        const distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY);
        const maxDistance = radius - knobRadius;

        if (distance > maxDistance) {
            deltaX = (deltaX / distance) * maxDistance;
            deltaY = (deltaY / distance) * maxDistance;
        }

        const normalizedX = deltaX / maxDistance;
        const normalizedY = -(deltaY / maxDistance);

        let turnCurve = Math.sign(normalizedX) * Math.pow(Math.abs(normalizedX), 1.5);
        let leftMotor = normalizedY + turnCurve;
        let rightMotor = normalizedY - turnCurve;

        leftMotor = Math.max(-1, Math.min(1, leftMotor));
        rightMotor = Math.max(-1, Math.min(1, rightMotor));
        const sensitivity = 500;

        commander.leftCurrent = Math.round(leftMotor * sensitivity);
        commander.rightCurrent = Math.round(rightMotor * sensitivity);

        updateJoystick(normalizedX, normalizedY);
    }

    function updateJoystick(xAxis, yAxis) {
        let deltaX = xAxis * (radius - knobRadius);
        let deltaY = yAxis * (radius - knobRadius);

        knob.style.transform = `translate(${deltaX - knobRadius}px, ${(-deltaY) - knobRadius}px)`;
        motorDisplay.textContent = `Left: ${commander.leftCurrent}, Right: ${commander.rightCurrent}`;
    }

    function resetJoystick() {
        knob.style.transform = 'translate(-50%, -50%)';
        motorDisplay.textContent = 'Left: 0, Right: 0';
        commander.leftCurrent = 0;
        commander.rightCurrent = 0;
    }

    function updateJoystickWithGamepad() {
        const gamepad = navigator.getGamepads()[currentGamepadIndex];
        if (gamepad) {
            const axisX = gamepad.axes[0];
            const axisY = gamepad.axes[1];
            const trigger = Math.max(gamepad.buttons[6].value, gamepad.buttons[7].value);
            const bumper = Math.max(gamepad.buttons[4].value, gamepad.buttons[5].value);
            const fastButton = gamepad.buttons[3].value;

            const deadzone = 0.2;
            const adjustedX = Math.abs(axisX) > deadzone ? (axisX - Math.sign(axisX) * deadzone) / (1 - deadzone) : 0;
            const adjustedY = Math.abs(axisY) > deadzone ? (axisY - Math.sign(axisY) * deadzone) / (1 - deadzone) : 0;

            let speedSensitivity = 800;
            if (fastButton > 0.5) {
                speedSensitivity = 2300;
            }
            const steeringSensitivity = 100;
            let leftMotor = trigger * speedSensitivity;
            let rightMotor = trigger * speedSensitivity;
            let steeringOffset = adjustedX * steeringSensitivity;
            leftMotor += steeringOffset;
            rightMotor -= steeringOffset;
            if (bumper > 0.5) {
                leftMotor = -300;
                rightMotor = -300;
            }
            commander.leftCurrent = Math.round(leftMotor);
            commander.rightCurrent = Math.round(rightMotor);

            updateJoystick(adjustedX, -adjustedY);
        }
    }

    // Gamepad selector UI
    const cycleGamepadButton = document.createElement('div');
    cycleGamepadButton.style.display = 'flex';
    cycleGamepadButton.style.gap = '5px';
    cycleGamepadButton.style.marginTop = '10px';

    for (let i = 0; i < 4; i++) {
        const square = document.createElement('div');
        square.style.width = '10px';
        square.style.height = '10px';
        square.style.border = '1px solid #444';
        square.style.backgroundColor = i === currentGamepadIndex ? '#ffa600' : '#333';
        square.dataset.index = i;
        cycleGamepadButton.appendChild(square);
    }

    cycleGamepadButton.addEventListener('click', () => {
        const gamepads = navigator.getGamepads();
        const availableGamepads = Array.from(gamepads).filter(g => g !== null);

        if (availableGamepads.length > 0) {
            currentGamepadIndex = (currentGamepadIndex + 1) % availableGamepads.length;
            Array.from(cycleGamepadButton.children).forEach((square, index) => {
                square.style.backgroundColor = index === currentGamepadIndex ? '#ffa600' : '#333';
            });
        }
    });

    joystickContainer.parentElement.appendChild(cycleGamepadButton);

    // Mouse/Touch controls
    knob.addEventListener('mousedown', (e) => {
        if (!useGamepad) {
            isDragging = true;
            e.preventDefault();
        }
    });

    document.addEventListener('mousemove', (e) => {
        if (isDragging && !useGamepad) {
            updateJoystickWithMouse(e.clientX, e.clientY);
        }
    });

    document.addEventListener('mouseup', () => {
        if (isDragging && !useGamepad) {
            isDragging = false;
            resetJoystick();
        }
    });

    knob.addEventListener('touchstart', (e) => {
        if (!useGamepad) {
            isDragging = true;
            e.preventDefault();
        }
    });

    document.addEventListener('touchmove', (e) => {
        if (isDragging && !useGamepad) {
            const touch = e.touches[0];
            updateJoystickWithMouse(touch.clientX, touch.clientY);
            e.preventDefault();
        }
    });

    document.addEventListener('touchend', () => {
        if (isDragging && !useGamepad) {
            isDragging = false;
            resetJoystick();
        }
    });

    // Toggle between gamepad and mouse
    const toggleControlButton = document.createElement('button');
    toggleControlButton.textContent = 'Switch to Mouse';
    toggleControlButton.addEventListener('click', () => {
        useGamepad = !useGamepad;
        toggleControlButton.textContent = useGamepad ? 'Switch to Mouse' : 'Switch to Gamepad';
        cycleGamepadButton.style.display = useGamepad ? 'flex' : 'none';
    });
    joystickContainer.parentElement.appendChild(toggleControlButton);

    setInterval(() => {
        if (useGamepad) {
            updateJoystickWithGamepad();
        }
    }, 100);
}

// Direct drive command sender
setInterval(() => {
    directCommanders.forEach(commander => {
        if (commander.leftCurrent !== commander.leftLastSent || commander.rightCurrent !== commander.rightLastSent) {
            const payload = `${commander.id}:${commander.leftCurrent}:${commander.rightCurrent}`;
            sendWebSocketMessage('car_direct_drive', payload);
            commander.leftLastSent = commander.leftCurrent;
            commander.rightLastSent = commander.rightCurrent;
        }
    });
}, 250);

// Logging
function addLogEntry(message, type) {
    const logEntry = document.createElement('div');
    logEntry.className = `log-entry log-${type}`;
    logEntry.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
    
    systemLogs.appendChild(logEntry);
    systemLogs.scrollTop = systemLogs.scrollHeight;
    
    while (systemLogs.children.length > 100) {
        systemLogs.removeChild(systemLogs.firstChild);
    }
}

function handleUtilityMessage(utilityData) {
    const parts = utilityData.split(':');
    const utilityType = parts[0];
    
    if (utilityType === 'cfp') { // Car Firmware Progress
        const carId = parts[1];
        const currentBytes = parseInt(parts[2]);
        const totalBytes = parseInt(parts[3]);
        const percentage = (currentBytes / totalBytes) * 100;
        updateFirmwareProgress(percentage);
        //addLogEntry(`Firmware progress for ${carId}: ${Math.round(percentage)}%`, 'utility');
    } else if(utilityType === 'skup') { //tracking scan update
        const state = parts[1];
        if(state == 'true') { //scan success
            // Auto-refresh track visualization 3 seconds after track scan completes
            setTimeout(() => {
                refreshTrackVisualization();
            }, 3000);
        }
    }
}

// Firmware Control
const firmwareWarningBtn = document.getElementById('firmware-warning-btn');
const firmwareControls = document.getElementById('firmware-controls');
const firmwareCarDropdown = document.getElementById('firmware-car-dropdown');
const firmwarePathInput = document.getElementById('firmware-path-input');
const firmwareStableBtn = document.getElementById('firmware-stable-btn');
const firmwareLatestBtn = document.getElementById('firmware-latest-btn');
const firmwareToggleTextBtn = document.getElementById('firmware-toggle-text');
const firmwareProgressContainer = document.getElementById('firmware-progress-container');
const firmwareProgressBar = document.getElementById('firmware-progress-bar');
const firmwareProgressText = document.getElementById('firmware-progress-text');

firmwareWarningBtn.addEventListener('click', function() {
    const confirmed = confirm(
        "⚠️ WARNING: Firmware flashing is an ADVANCED feature!\n\n" +
        "Flashing incorrect firmware can permanently brick your car\n" +
        "Do you want to continue?"
    );
    
    if (confirmed) {
        firmwareControls.style.display = 'block';
        firmwareWarningBtn.style.display = 'none';
        updateFirmwareCarDropdown();
    }
});

firmwareStableBtn.addEventListener('click', function() {
    selectedFirmwarePath = 'overdrive_2.6.ota';
    updateFirmwareButtons('legacy');
    firmwarePathInput.style.display = 'none';
});

firmwareLatestBtn.addEventListener('click', function() {
    selectedFirmwarePath = 'party.ota';
    updateFirmwareButtons('latest');
    firmwarePathInput.style.display = 'none';
});

firmwareToggleTextBtn.addEventListener('click', function() {
    showTextInput = !showTextInput;
    if (showTextInput) {
        firmwarePathInput.style.display = 'block';
        firmwarePathInput.focus();
        updateFirmwareButtons('text');
    } else {
        firmwarePathInput.style.display = 'none';
        updateFirmwareButtons('none');
    }
});

function updateFirmwareButtons(active) {
    firmwareStableBtn.classList.remove('active');
    firmwareLatestBtn.classList.remove('active');
    firmwareToggleTextBtn.classList.remove('active');
    
    if (active === 'legacy') firmwareStableBtn.classList.add('active');
    else if (active === 'latest') firmwareLatestBtn.classList.add('active');
    else if (active === 'text') firmwareToggleTextBtn.classList.add('active');
}

document.getElementById('firmware-flash-btn').addEventListener('click', function() {
    const selectedCarId = firmwareCarDropdown.value;
    const firmwarePath = showTextInput ? firmwarePathInput.value.trim() : selectedFirmwarePath;

    if (!selectedCarId) {
        alert('Please select a car first.');
        return;
    }

    if (!firmwarePath) {
        alert('Please select a firmware option or enter a custom path.');
        return;
    }

    const finalConfirm = confirm(
        `Are you absolutely sure you want to flash firmware to car ${selectedCarId}?\n\n` +
        `Firmware: ${firmwarePath}\n\n` +
        "This action cannot be undone!"
    );

    if (finalConfirm) {
        const payload = `${selectedCarId}:${firmwarePath}`;
        sendWebSocketMessage('car_flash', payload);
        addLogEntry(`Firmware flash initiated for car ${selectedCarId} with ${firmwarePath}`, 'system');
        
        firmwareProgressContainer.style.display = 'block';
        updateFirmwareProgress(0);
    }
});

function updateFirmwareProgress(percentage) {
    firmwareProgressBar.style.width = percentage + '%';
    firmwareProgressText.textContent = Math.round(percentage) + '%';
    
    if (percentage >= 100) {
        setTimeout(() => {
            firmwareProgressContainer.style.display = 'none';
            firmwareControls.style.display = 'none';
            firmwareWarningBtn.style.display = 'block';
            firmwarePathInput.value = '';
            firmwareCarDropdown.value = '';
            selectedFirmwarePath = '';
            updateFirmwareButtons('none');
        }, 2000);
    }
}

function updateFirmwareCarDropdown() {
    firmwareCarDropdown.innerHTML = '<option value="">Select a car...</option>';
    
    cars.forEach(car => {
        const option = document.createElement('option');
        option.value = car.id;
        option.textContent = `${car.name} (${car.id})`;
        firmwareCarDropdown.appendChild(option);
    });
}

// Model Update Control
const modelWarningBtn = document.getElementById('model-warning-btn');
const modelControls = document.getElementById('model-controls');
const modelCarDropdown = document.getElementById('model-car-dropdown');
const modelNumberInput = document.getElementById('model-number-input');
const modelUpdateBtn = document.getElementById('model-update-btn');
const modelCancelBtn = document.getElementById('model-cancel-btn');

modelWarningBtn.addEventListener('click', function() {
    modelControls.style.display = 'block';
    modelWarningBtn.style.display = 'none';
    updateModelCarDropdown();
});

modelCancelBtn.addEventListener('click', function() {
    modelControls.style.display = 'none';
    modelWarningBtn.style.display = 'block';
    modelNumberInput.value = '';
    modelCarDropdown.value = '';
});

modelCarDropdown.addEventListener('change', function() {
    const selectedCarId = modelCarDropdown.value;
    if (!selectedCarId) return;
    
    const car = cars.find(c => c.id === selectedCarId);
    if (car && !car.hasPFeatures) {
        alert('This car does not support Model Update.\n\nPlease install the Latest firmware first to enable this feature.');
        modelCarDropdown.value = '';
    }
});

modelUpdateBtn.addEventListener('click', function() {
    const selectedCarId = modelCarDropdown.value;
    const modelNumber = parseInt(modelNumberInput.value);

    if (!selectedCarId) {
        alert('Please select a car first.');
        return;
    }
    
    const car = cars.find(c => c.id === selectedCarId);
    if (car && !car.hasPFeatures) {
        alert('This car does not support Model Update.\n\nPlease install the Latest firmware first to enable this feature.');
        return;
    }

    if (isNaN(modelNumber) || modelNumber < 0 || modelNumber > 256) {
        alert('Please enter a valid model number (0-256).');
        return;
    }

    const finalConfirm = confirm(
        `Are you absolutely sure you want to update the model for car ${selectedCarId}?\n` +
        `New Model Number: ${modelNumber}\n`
    );

    if (finalConfirm) {
        const payload = `${selectedCarId}:${modelNumber}`;
        sendWebSocketMessage('car_update_model', payload);
        
        alert('Model update initiated!\n\nPlease press the RESET button on the underside of the car for the change to take effect.');
        
        modelControls.style.display = 'none';
        modelWarningBtn.style.display = 'block';
        modelNumberInput.value = '';
        modelCarDropdown.value = '';
    }
});

function updateModelCarDropdown() {
    modelCarDropdown.innerHTML = '<option value="">Select a car...</option>';
    
    cars.forEach(car => {
        const option = document.createElement('option');
        option.value = car.id;
        option.textContent = `${car.name} (${car.id})`;
        modelCarDropdown.appendChild(option);
    });
}

// Track Visualization
const refreshTrackBtn = document.getElementById('refresh-track-btn');
const trackImage = document.getElementById('track-image');
const trackLoading = document.getElementById('track-loading');
const trackStatus = document.getElementById('track-status');
const trackProgressLine = document.getElementById('trackProgressLine');

refreshTrackBtn.addEventListener('click', function() {
    refreshTrackVisualization();
});

function refreshTrackVisualization() {
    if (!wsConnected) {
        trackLoading.style.display = 'block';
        trackImage.style.display = 'none';
        trackStatus.textContent = 'Error: WebSocket not connected';
        trackStatus.style.color = '#ff6666';
        return;
    }
    
    trackLoading.textContent = 'Requesting track data...';
    trackLoading.style.display = 'block';
    trackImage.style.display = 'none';
    trackStatus.textContent = '';
    
    sendWebSocketMessage('get_track', null);
}

function renderTrackProgressBar(trackData) {
    if (!trackData || trackData.length === 0) {
        trackProgressLine.innerHTML = '<div style="display: flex; align-items: center; justify-content: center; width: 100%; color: var(--text-secondary); font-size: 12px;">No track data available</div>';
        return;
    }
    
    // Clear existing segments
    trackProgressLine.innerHTML = '';
    
    // Create segment elements
    trackData.forEach((segment, index) => {
        const segmentEl = document.createElement('div');
        segmentEl.className = 'track-segment';
        segmentEl.dataset.index = index;
        
        const indexLabel = document.createElement('div');
        indexLabel.className = 'track-segment-index';
        
        // Get segment type names
        const typeNames = ['Unknown', 'Straight', 'Turn', 'PreFinish', 'FinishLine', 
                         'FnFSpecial', 'CrissCross', 'JumpRamp', 'JumpLanding',
                         'Oval', 'Bottleneck', 'Crossroads', 'F1', 'DoubleCross'];
        
        const typeName = typeNames[segment.type] || 'Unknown';
        
        // Add direction for turns (reversed because left and right are flipped)
        if (segment.type === 2) { // Turn
            const isLeft = segment.reversed || false;
            indexLabel.textContent = `${index} Turn${isLeft ? 'R' : 'L'}`;
        } else {
            indexLabel.textContent = `${index} ${typeName}`;
        }
        
        segmentEl.appendChild(indexLabel);
        trackProgressLine.appendChild(segmentEl);
    });
}

function updateCarProgressPositions() {
    if (!currentTrackData || currentTrackData.length === 0) return;
    
    const totalSegments = currentTrackData.length;
    
    cars.forEach(car => {
        if (car.trackIndex === undefined) return;
        
        // Find or create car dot
        let carDot = trackProgressLine.querySelector(`[data-car-id="${car.id}"]`);
        if (!carDot) {
            carDot = document.createElement('div');
            carDot.className = 'car-dot';
            carDot.dataset.carId = car.id;
            carDot.title = car.name || car.id;
            
            // Apply car model colors
            const colors = getCarModelInfo(car.model || 0);
            carDot.style.background = `linear-gradient(135deg, ${colors.primary}, ${colors.secondary})`;
            carDot.style.color = colors.primary;
            
            trackProgressLine.appendChild(carDot);
        }
        
        // Calculate horizontal position
        const position = ((car.trackIndex || 0) / totalSegments) * 100;
        carDot.style.left = `${position}%`;
        
        // Calculate vertical offset position
        const offsetMM = car.offsetMM || 0;
        const offsetPercent = (offsetMM / 67.5) * 35;
        const topPosition = 50 - offsetPercent;
        carDot.style.top = `${topPosition}%`;
    });
    
    // Remove dots for disconnected cars
    const carDots = trackProgressLine.querySelectorAll('.car-dot');
    carDots.forEach(dot => {
        const carId = dot.dataset.carId;
        if (!cars.find(c => c.id === carId)) {
            dot.remove();
        }
    });
}

async function renderTrackFromData(trackData) {
    try {
        if (!trackData || trackData.length === 0) {
            throw new Error('No track data available');
        }

        trackLoading.textContent = 'Loading track visualization...';

        // Load the statically saved track image (saved by ServerLink.TrackRender)
        // Add timestamp to prevent caching
        const imageUrl = `/track-render.png?t=${Date.now()}`;
        
        trackImage.src = imageUrl;
        trackImage.onload = function() {
            trackImage.style.display = 'block';
            trackLoading.style.display = 'none';
            trackStatus.textContent = `Track rendered successfully (${trackData.length} segments)`;
            trackStatus.style.color = '#999';
        };
        
        trackImage.onerror = function() {
            trackLoading.style.display = 'none';
            trackStatus.textContent = `Track data loaded (${trackData.length} segments). Visualization not yet available - scan track to generate.`;
            trackStatus.style.color = '#ffaa00';
        };

    } catch (error) {
        trackLoading.style.display = 'none';
        trackStatus.textContent = `Error: ${error.message}`;
        trackStatus.style.color = '#ff6666';
        console.error('Track rendering error:', error);
    }
}

// Initialize on page load
connectWebSocket();

setTimeout(() => {
    if (wsConnected) {
        sendWebSocketMessage('scan', null);
    }
}, 1000);

// Periodic updates
setInterval(() => {
    if (wsConnected) {
        sendWebSocketMessage('request_cars', null);
        sendWebSocketMessage('get_available_cars', null);
    }
}, 5000);