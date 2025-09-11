# Anki Partydrive - Project Structure

## Overview
Anki Partydrive is a Unity-based racing game that interfaces with physical Anki Overdrive cars through a WebSocket server. The project creates a virtual 3D representation of the real-world track and provides various game modes for racing with physical cars.

## Architecture Overview

```
Unity Client (Game) ←→ WebSocket ←→ OverdriveServer (C#) ←→ Bluetooth ←→ Anki Cars
```

The Unity game communicates with OverdriveServer which handles Bluetooth communication with the physical Anki Overdrive cars. The Unity client receives real-time car positions and can send control commands.

---

## Core Systems

### 1. **Central Management System (CMS)**
**File:** `CMS.cs`

The heart of the application that (should) orchestrate all major systems.

**Responsibilities:**
- Manages all car controllers (both AI and player)
- Handles color assignment for players
- Controls global game state (locks, game modes)
- Provides centralized TTS (Text-to-Speech) functionality
- Manages AI spawning and removal
- Acts as event dispatcher for car energy events

**Key Connections:**
- Connected to: `CarController`, `AIController`, `CarInterface`, `PlayerCardManager`
- Used by: All game modes, UI systems

### 2. **Car Interface System**
**File:** `CarInteraface.cs` (singleton via `CarInteraface.io`)

Manages all communication between Unity and the external server.

**Responsibilities:**
- WebSocket connection management to server (localhost:7118)
- Real-time car position updates
- Car control commands (speed, lane, lights)
- Track scanning and validation
- Car discovery and connection status
- Data parsing and event distribution

**Key Methods:**
- `ControlCar()` - Send movement commands to physical cars
- `ApiCallV2()` - Generic server communication
- `ProcessWebhookData()` - Handle incoming server messages
- `MapTrack()` - Initiate track scanning process

**Connects To:**
- `TrackGenerator` - Sends track data for 3D generation
- `CarEntityTracker` - Updates car positions in 3D space
- `CMS` - Reports car status and events
- `UIManager` - Updates connection status

---

## Control Systems

### 3. **Car Controller**
**File:** `Control/CarController.cs`

Base controller class for all cars (both player and AI controlled).

**Responsibilities:**
- Input processing (acceleration, steering, boost, items)
- Speed and energy management
- Lane positioning with dynamic track width calculation
- Speed modifiers system (boosts, penalties)
- Communication with physical cars via CarInterface
- UI integration for player cards

**Key Features:**
- **Energy System**: Boost consumes energy, coasting regenerates it
- **Dynamic Lane Limits**: Adapts to current track section width
- **Speed Modifiers**: Temporary effects (boosts, penalties) with ID system
- **Real-time Control**: 2Hz update rate to physical cars

**Input System:**
- `Iaccel` - Acceleration (0-1)
- `Isteer` - Steering (-1 to 1)
- `Iboost` - Boost button
- `Idrift` - Drift state
- `IitemA/B` - Item usage (future feature)

### 4. **AI Controller**
**File:** `Control/AIController.cs`

Extends CarController with autonomous driving capabilities.

**Responsibilities:**
- Pathfinding and collision avoidance
- Opponent tracking and racing behavior
- Dynamic difficulty adjustment
- Integration with car management system

### 5. **Player Controller**
**File:** `Control/PlayerController.cs`

Handles human player input from various sources (keyboard, gamepad).

---

## Track System

### 6. **Track Generator**
**File:** `Track/TrackGenerator.cs` (singleton via `TrackGenerator.track`)

Converts real-world track data into 3D Unity representation.

**Responsibilities:**
- Receives track segments from server
- Generates 3D track pieces with proper positioning
- Handles track validation states
- Provides spline data for car positioning
- Camera positioning and track overview

**Key Features:**
- **Procedural Generation**: Creates 3D track from segment data
- **Validation States**: Distinguishes between scanning and confirmed tracks
- **Spline System**: Provides smooth paths for car movement
- **Dynamic Width**: Different track pieces have varying widths

**Track Elements:**
- Straights, curves, intersections
- Special pieces: finish line, jumps, boost pads
- Modular system with prefab-based generation

### 7. **Track Splines**
**File:** `Track/TrackSpline.cs`

Individual track piece components that provide positioning data.

**Responsibilities:**
- Smooth car interpolation along track sections
- Width calculation for lane limits
- Connection points between track pieces
- Visual effects and animations

---

## Car Visualization System

### 8. **Car Entity Tracker**
**File:** `CarVisualisation/CarEntityTracker.cs`

Manages 3D representations of all cars on the track.

**Responsibilities:**
- Creates and destroys car entities
- Real-time position updates from server data
- Coordinate conversion (track → world space)
- Lap detection and finish line events
- AI opponent location sharing

**Key Features:**
- **Trust System**: Handles position confidence levels
- **Delocalization**: Manages cars losing track positioning
- **Lap Tracking**: Detects finish line crossings
- **AI Integration**: Provides opponent data to AI controllers

### 9. **Car Entity Position**
**File:** `CarVisualisation/CarEntityPosition.cs`

Individual car representation in 3D space.

**Responsibilities:**
- Smooth interpolation between position updates
- Speed and offset tracking
- Model management and animations
- Trust level visualization

---

## Game Mode System

### 10. **Time Trial Mode**
**File:** `GameModes/TimeTrialMode.cs`

3-minute time trial racing mode.

**Features:**
- Countdown system with TTS
- Individual lap timing
- Automatic lineup at start
- Results display and replay options
- Camera switching to track view

### 11. **Laps Mode** 
**File:** `GameModes/LapsMode.cs`

Traditional lap-based racing.

**Features:**
- Configurable lap counts
- Position tracking
- Race completion detection
- Lap time recording

---

## User Interface System

### 12. **UI Manager**
**File:** `UI/UIManager.cs`

Central UI coordination and state management.

**Responsibilities:**
- UI layer management (menu, game, scanning states)
- Camera switching (menu ↔ track view)
- Server connection status display
- Car count display and loading states
- Track scanning UI coordination

**UI Layers:**
0. Main Menu
1. Game Selection  
2. Car Management
3. Track Scanning
4. Car Balancing
5. Time Trial Mode
6. Laps Mode

### 13. **Cars Management UI**
**File:** `UI/CarsManagement.cs`

Car selection and configuration interface.

**Responsibilities:**
- Car list rendering and updates
- Player assignment to cars
- AI toggle controls
- Game mode launching
- Real-time car status display

### 14. **Player Card System**
**File:** `UI/PlayerCardSystem.cs`

Individual player status display during races.

**Features:**
- Real-time energy display
- Position indicators
- Lap times and counts
- Player color coding
- Car connection status

---

## Utility Systems

### 15. **Car Balancer**
**File:** `Utils/CarBalancer.cs`

Automated car performance calibration system.

**Purpose:**
- Standardizes car performance across different physical units
- Handles speed variations between individual cars
- Automated testing and calibration procedures

### 16. **File System**
**File:** `Utils/FileSuper.cs`

Handles data persistence and configuration management.

---

## Data Structures

### Core Data Types (from OverdriveServer):
- **`UCarData`**: Unity car representation (ID, name, speed, energy, charging state)
- **`CarData`**: Server car data structure  
- **`Segment`**: Track piece definition (type, position, validation)
- **`CarLocationData`**: Real-time position data (track index, offset, speed, trust)
- **`TrackCoordinate`**: 1.5D track position system

### Communication Protocol:
- **WebSocket JSON Messages**: Bidirectional communication
- **Event Types**: Car movement, track scanning, car discovery
- **Trust Levels**: Position confidence (Trusted, Untrusted, Delocalized)

---

## Key Design Patterns

1. **Singleton Pattern**: `CarInteraface.io`, `TrackGenerator.track`, `CMS`
2. **Observer Pattern**: Event system for car states, finish line crossings
3. **Component System**: Unity's component-based car controllers
4. **State Machine**: UI layer management, game mode states
5. **Factory Pattern**: Car entity creation and management

---

## Game Flow

### Typical Session Flow:
1. **Startup**: Connect to server, discover cars
2. **Track Scanning**: Build and validate track layout
3. **Game Mode Selection**: Choose racing format
4. **Car Assignment**: Players select cars, configure settings
5. **Race Execution**: Real-time racing with physical cars
6. **Results**: Display times, replay options

--- 