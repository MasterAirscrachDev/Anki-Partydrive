using Newtonsoft.Json;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using System.Collections.Concurrent;
using AsyncAwaitBestPractices;

namespace OverdriveServer {
    public struct TrackingResult {
        public string CarId { get; init; }
        public int TrackIndex { get; init; }
        public CarTrust Trust { get; init; }
        public float HorizontalPosition { get; init; }
        public TimeSpan ProcessingTime { get; init; }
        public bool RequiresAction { get; init; }
        public TrackingAction RecommendedAction { get; init; }
    }
    
    public enum TrackingAction {
        None,
        UTurn,
        SlowDown,
        ClearMemory,
        UpdatePosition
    }
    
    class TrackManager {
        Segment[]? track;
        bool trackValidated = false, doInstantLocalisation = false;
        int carsAwaitingLineup = 0;
        Dictionary<string, TrackCarLocation> carLocations = new Dictionary<string, TrackCarLocation>();
        private readonly CarTrackingCoordinator _trackingCoordinator;
        public void SetTrack(Segment[] track, bool validated, bool instantLocalisation = false){ 
            this.track = track; trackValidated = validated; 
            carLocations.Clear(); //clear the car locations, we need to revalidate them
            //if we are not subscribed to Location events, subscribe now if doing instant localisation, otherwise unsubscribe
            if(instantLocalisation && !doInstantLocalisation){
                Program.messageManager.CarEventLocationCall += OnLocation;
            }else if(!instantLocalisation && doInstantLocalisation){
                Program.messageManager.CarEventLocationCall -= OnLocation;
            }
            doInstantLocalisation = instantLocalisation;
            //Program.Log($"Track set: validated={validated}, instantLocalisation={instantLocalisation}, segments={(track != null ? track.Length.ToString() : "null")}");
        }
        public TrackManager(){
            carLocations = new Dictionary<string, TrackCarLocation>();
            _trackingCoordinator = new CarTrackingCoordinator();
            Program.messageManager.CarEventSegmentCall += OnSegment; 
            Program.messageManager.CarEventDelocalised += OnCarDelocalised; 
            Program.messageManager.CarEventJumpCall += OnCarJumped;
        }
        public void AlertIfTrackIsValid(){ if(trackValidated){ Program.UtilLog($"{MSG_TR_SCAN_UPDATE}:{trackValidated}"); } }
        void OnCarJumped(string id){
            if (!trackValidated || track == null){ return; }
            if (!carLocations.TryGetValue(id, out TrackCarLocation? carTracker)) {
                carTracker = new TrackCarLocation(track.Length); // Create a new location for this car
                carLocations[id] = carTracker; // Add it to the dictionary
            }
            carTracker.OnJumped(); //update the car's track position
        }
        void OnSegment(string carID, Segment segment, float offset){
            //Program.Log($"Car {carID} on segment {segment.type} ({segment.internalID}), offset {offset}", true); //debugging
            if (!trackValidated){ return; }
            
            // Process tracking in isolation - doesn't block other cars
            ProcessSegmentIsolated(carID, segment, offset).SafeFireAndForget();
        }
        
        private async Task ProcessSegmentIsolated(string carID, Segment segment, float offset) {
            try {
                if (track == null) return;
                
                // Ensure car location exists
                if (!carLocations.TryGetValue(carID, out TrackCarLocation? carTracker)) {
                    carTracker = new TrackCarLocation(track.Length);
                    carLocations[carID] = carTracker;
                }
                
                // Process in complete isolation using coordinator
                var result = await _trackingCoordinator.ProcessCarSegmentAsync( carID, segment, offset, carTracker, track, doInstantLocalisation);
                
                // Handle result on main thread context
                await HandleTrackingResult(carID, result);
                
            } catch (Exception ex) {
                Program.Log($"Error in isolated tracking for car {carID}: {ex.Message}");
            }
        }
        
        private async Task HandleTrackingResult(string carID, TrackingResult result) {
            // Update car location with result
            if (carLocations.TryGetValue(carID, out var carTracker)) {
                carTracker.trackIndex = result.TrackIndex;
                carTracker.trust = result.Trust;
                carTracker.horizontalPosition = result.HorizontalPosition;
                
                // Handle recommended actions
                if (result.RequiresAction) {  await ExecuteTrackingAction(carID, result.RecommendedAction); }
                
                // Check lineup logic
                if (carsAwaitingLineup > 0) { LineupLogic(carTracker, carID); }
                
                // Notify clients
                Program.socketMan.Notify(EVENT_CAR_TRACKING_UPDATE,  carTracker.GetCarLocationData(carID));
            }
        }
        
        private async Task ExecuteTrackingAction(string carID, TrackingAction action) {
            var car = Program.carSystem.GetCar(carID);
            if (car == null) return;
            
            switch (action) {
                case TrackingAction.UTurn:
                    Program.Log($"Car {carID} is lost, attempting U-Turn");
                    int speed = car.data.speedMMPS;
                    await car.SetCarSpeed(200, 500);
                    await car.UTurn();
                    
                    // Return to normal speed after delay
                    _ = Task.Delay(1500).ContinueWith(async _ => {
                        var currentCar = Program.carSystem.GetCar(carID);
                        if (currentCar != null) {
                            await currentCar.SetCarSpeed(speed);
                        }
                    });
                    break;
                    
                case TrackingAction.ClearMemory:
                    if (carLocations.TryGetValue(carID, out var tracker)) {
                        Program.Log($"Car {carID} state -> Unsure (clearing memory after error)", true);
                        tracker.ClearTracks(CarTrust.Unsure);
                    }
                    break;
            }
        }
        void OnLocation(string carID, int trackLocation, int trackID, float offset, int speed, bool reversed){
            return;
        }
        // Tracking logic moved to CarTrackingCoordinator for isolation
        void LineupLogic(TrackCarLocation carTracker, string carID){
            if(carTracker.trust == CarTrust.Trusted && track != null){ //if we are somewhat sure about the position
                //Program.Log($"Lineup: Car {carID} at index {carTracker.trackIndex}"); //debugging
                if(carTracker.trackIndex == 0){ //if we are at the first segment
                    Program.Log($"Lineup: Car {carID} at starting line");
                    var car = Program.carSystem.GetCar(carID);
                    if (car != null) {
                        car.SetCarSpeed(150, 1000).SafeFireAndForget(); //slow down to 150
                        car.TriggerStopOnTransition().SafeFireAndForget(); //stop the car on transition
                        Task.Delay(850).ContinueWith(t => { //in a bit mark the car as stopped
                            if(Program.carSystem.GetCar(carID) != null){
                                Program.Log($"Lineup finished for {carID}");
                                carsAwaitingLineup--;
                                Program.UtilLog($"{MSG_LINEUP}:{carID}:{carsAwaitingLineup}");
                            }
                        });
                    }
                }//if we are in the last 2 segments then slow down
                else if(carTracker.trackIndex >= track.Length - 1){ //if we are in the last 2 segments then slow down
                    var car = Program.carSystem.GetCar(carID);
                    if (car != null) {
                        car.SetCarSpeed(330, 500).SafeFireAndForget(); //slow down to 330
                    }
                }
            }
        }
        void OnCarDelocalised(string id){
            if(carLocations.ContainsKey(id)){ //we already have a location for this car
                carLocations[id].ClearTracks(CarTrust.Delocalized); //clear the track memory
                //Console.WriteLine($"Car {id} delocalised, clearing track memory");
            }
        }
        public void RequestLineup(){
            Car[] cars = Program.carSystem.GetCarsOnTrack();
            if(cars.Length == 0){ Program.Log("No cars to lineup"); return; }
            if(track == null || !trackValidated){ Program.Log("No track to lineup on"); return; }
            //set the lane offset for each car based on index
            if(cars.Length > 4) {  //max 4 cars (for now)
                Program.Log("Too many cars to lineup");
                Program.UtilLog($"{MSG_LINEUP}:0:0"); //fallback to allow app functionality
                return;
            }
            float[] lanes = { 67.5f, 22.5f, -22.5f, -67.5f }; // Second and first linable lanes (lane 8 and 3)
            for(int i = 0; i < cars.Length; i++){
                float laneOffset = lanes[1]; //default lane offset
                if(cars.Length == 2){ laneOffset = (i == 0) ? lanes[1] : lanes[2];
                }else if(cars.Length > 2){ laneOffset = lanes[i]; }
                
                cars[i].SetCarSpeed(550, 1000, true, false).SafeFireAndForget(); //set the speed to 550 for all cars
                cars[i].SetCarLane(laneOffset, 100).SafeFireAndForget(); //set the lane offset for each car
            }
            carsAwaitingLineup = cars.Length;
            Program.Log($"Lineup started with {cars.Length} cars");
        }
        public void CancelLineup(){
            Car[] cars = Program.carSystem.GetCarsOnTrack();
            if(cars.Length == 0){  return; }
            for(int i = 0; i < cars.Length; i++){
                cars[i].SetCarSpeed(0, 1000, true, false).SafeFireAndForget(); //set the speed to 0 for all cars
            }
            carsAwaitingLineup = 0;
            Program.Log($"Lineup cancelled");
        }
        public string TrackDataAsJson(){ return JsonConvert.SerializeObject(track); }
        
        public async Task DisposeAsync() {
            try {
                await _trackingCoordinator.DisposeAsync();
                Program.Log("TrackManager disposed - tracking coordinator cleaned up");
            } catch (Exception ex) {
                Program.Log($"Error during TrackManager disposal: {ex.Message}");
            }
        }
    }
    
    // Car Tracking Coordinator for isolated per-car processing
    public class CarTrackingCoordinator {
        private readonly ConcurrentDictionary<string, IsolatedCarTracker> _carTrackers;
        private readonly TrackingMetrics _metrics;
        
        public CarTrackingCoordinator() {
            _carTrackers = new ConcurrentDictionary<string, IsolatedCarTracker>();
            _metrics = new TrackingMetrics();
        }
        
        public async Task<TrackingResult> ProcessCarSegmentAsync(
            string carId, Segment segment, float offset, TrackCarLocation carTracker, 
            Segment[]? track, bool doInstantLocalisation) {
            
            // Get or create isolated tracker for this car
            var tracker = _carTrackers.GetOrAdd(carId, id => new IsolatedCarTracker(id));
            
            // Process in complete isolation
            var result = await tracker.ProcessSegmentAsync(segment, offset, carTracker, track, doInstantLocalisation);
            
            // Record metrics
            _metrics.RecordTrackingOperation(carId, result.ProcessingTime);
            
            return result;
        }
        
        public void RemoveCarTracker(string carId) {
            if (_carTrackers.TryRemove(carId, out var tracker)) {
                tracker.Dispose();
            }
        }
        
        public async Task DisposeAsync() {
            var disposeTasks = _carTrackers.Values.Select(tracker => tracker.DisposeAsync()).ToArray();
            await Task.WhenAll(disposeTasks);
            _carTrackers.Clear();
        }
    }
    
    // Isolated tracker for individual car processing
    public class IsolatedCarTracker {
        private readonly string _carId;
        private readonly TaskScheduler _exclusiveScheduler;
        private readonly SemaphoreSlim _operationSemaphore;
        
        public IsolatedCarTracker(string carId) {
            _carId = carId;
            var schedulerPair = new ConcurrentExclusiveSchedulerPair();
            _exclusiveScheduler = schedulerPair.ExclusiveScheduler;
            _operationSemaphore = new SemaphoreSlim(1, 1);
        }
        
        public Task<TrackingResult> ProcessSegmentAsync(
            Segment segment, float offset, TrackCarLocation carTracker, 
            Segment[]? track, bool doInstantLocalisation) {
            
            return Task.Factory.StartNew(async () => {
                await _operationSemaphore.WaitAsync();
                try { return ProcessSegmentInternal(segment, offset, carTracker, track, doInstantLocalisation); } 
                finally { _operationSemaphore.Release(); }
            }, CancellationToken.None, TaskCreationOptions.None, _exclusiveScheduler).Unwrap();
        }
        
        private TrackingResult ProcessSegmentInternal(
            Segment segment, float offset, TrackCarLocation carTracker, 
            Segment[]? track, bool doInstantLocalisation) {
            
            var startTime = DateTime.UtcNow;
            var action = TrackingAction.None;
            var requiresAction = false;
            // Update horizontal position
            carTracker.horizontalPosition = offset;
            
            if (carTracker.skipSegments > 0) {
                carTracker.skipSegments--;
            } else {
                if (doInstantLocalisation) {
                    ProcessInstantLocalization(segment, carTracker, track);
                } else {
                    var trackingResult = ProcessTrackingLogic(segment, offset, carTracker, track);
                    action = trackingResult.Action;
                    requiresAction = trackingResult.RequiresAction;
                }
            }
            
            var processingTime = DateTime.UtcNow - startTime;
            
            return new TrackingResult {
                CarId = _carId,
                TrackIndex = carTracker.trackIndex,
                Trust = carTracker.trust,
                HorizontalPosition = carTracker.horizontalPosition,
                ProcessingTime = processingTime,
                RequiresAction = requiresAction,
                RecommendedAction = action
            };
        }
        
        private void ProcessInstantLocalization(Segment segment, TrackCarLocation carTracker, Segment[]? track) {
            if (track == null) return;
            if (segment.internalID == 0) { //we dont know, just move forward
                carTracker.trackIndex = (carTracker.trackIndex + 1) % track.Length;
            }
            else
            {
                int expectedIndex = carTracker.trackIndex;
                for (int i = 0; i < track.Length; i++) {
                    if (track[i].internalID == segment.internalID) {
                        if (segment.flipped != track[i].flipped) {
                            Program.Log($"Car {_carId} segment flip mismatch at index {i} - U-turn may be needed");
                            //request a U-turn
                            
                            break;
                        }
                        
                        int newIndex = (i + 1) % track.Length;
                        
                        // Check if we jumped more than 1 segment
                        int forwardDistance = (newIndex - expectedIndex + track.Length) % track.Length;
                        int backwardDistance = (expectedIndex - newIndex + track.Length) % track.Length;
                        int minDistance = Math.Min(forwardDistance, backwardDistance);
                        
                        if (minDistance > 1 && carTracker.lastMatchedIndex != -1) {
                            Program.Log($"Car {_carId} localization error: expected index ~{expectedIndex}, got {newIndex} (distance: {minDistance} segments)");
                        }
                        
                        carTracker.trackIndex = newIndex;
                        carTracker.trust = CarTrust.Trusted;
                        carTracker.lastMatchedIndex = i;
                        break;
                    }
                }
            }
        }
        
        (TrackingAction Action, bool RequiresAction) ProcessTrackingLogic(
            Segment segment, float offset, TrackCarLocation carTracker, Segment[]? track) {
            
            if (track == null) return (TrackingAction.None, false);
            
            try {
                int memoryLength = carTracker.GetTrackMemoryLength();
                if (memoryLength > 1) {
                    List<int> matchedIndexes = carTracker.GetBestIndexes(track);
                    
/// ================SCENARIO 1 ==== Single match found - position is certain ======================================================
                    if (matchedIndexes.Count == 1) { // 
                        int trustedMatch = matchedIndexes[0];
                        

                        if(trustedMatch == carTracker.trackIndex) {
                            //Program.Log($"Car {_carId} state -> Trusted (single match at index {trustedMatch}, current: {carTracker.trackIndex})", true);
                            //we are where we expect to be
                            carTracker.trust = CarTrust.Trusted;
                            carTracker.wasManyOptionsLastSegment = false;
                            carTracker.prospectiveIsReverse = false;
                            carTracker.prospectiveIndex = -2; //clear prospective index
                        }else if(trustedMatch == carTracker.prospectiveIndex) { //we have matched with an unexpected jump
                            Program.Log($"Car {_carId} state -> Trusted (jump to index {trustedMatch} from {carTracker.trackIndex})", true);
                            //we are where we expected to be
                            carTracker.trackIndex = trustedMatch;
                            carTracker.trust = CarTrust.Trusted;
                            carTracker.wasManyOptionsLastSegment = false;
                            carTracker.prospectiveIsReverse = false;
                            carTracker.prospectiveIndex = -2; //clear prospective index
                        }
                        else
                        {
                            //Program.Log($"Car {_carId} state -> Trusted (considering jump to index {trustedMatch} from {carTracker.trackIndex})", true);
                            carTracker.prospectiveIndex = trustedMatch;
                        }
                        
                    }
/// ================SCENARIO 2 ==== Multiple matches found - position is ambiguous but car is going the right way =================
                    else if (matchedIndexes.Count > 1) {
                        bool includesCurrent = matchedIndexes.Any(x => x == carTracker.trackIndex);
                        if (includesCurrent) {
                            carTracker.trust = CarTrust.Trusted;
                        } else if (carTracker.wasManyOptionsLastSegment) {
                            Program.Log($"Car {_carId} state -> Unsure (multiple matches, no current position match)", true);
                            carTracker.trust = CarTrust.Unsure;
                        } else {
                            carTracker.wasManyOptionsLastSegment = true;
                        }
                    }
/// ================SCENARIO 3 ==== No forward matches - check for backwards movement ==============================================
                    else {
                        bool goingBackwards = carTracker.CheckReverseDirection(track);
/// ================SCENARIO 3A ==== Car is going backwards - trigger U-Turn =================================================
                        if (goingBackwards) {
                            if (carTracker.prospectiveIsReverse)
                            {
                                // Car is going backwards - trigger U-Turn and clear memory
                                Program.Log($"Car {_carId} detected going backwards - initiating U-Turn", true);
                                carTracker.ClearTracks(CarTrust.Delocalized);
                                return (TrackingAction.UTurn, true);
                            }
                            else
                            { /*Program.Log($"Car {_carId} possibly going backwards", true);*/ carTracker.prospectiveIsReverse = true; }
                            
                        }
/// ================SCENARIO 3B ==== No matches forward or backward - clear memory ================================================
                        else {
                            // No matches forward or backward - just clear memory and continue
                            carTracker.ClearTracks(carTracker.trust == CarTrust.Trusted ? CarTrust.Trusted : CarTrust.Unsure);
                        }
                    }
                }
                
                carTracker.OnSegment(segment, offset);
                return (TrackingAction.None, false);
                
            } catch (Exception ex) {
                Program.Log($"Error in isolated tracking logic for car {_carId}: {ex.Message}");
                return (TrackingAction.ClearMemory, true);
            }
        }
        
        public void Dispose() {
            _operationSemaphore?.Dispose();
        }
        
        public async Task DisposeAsync() {
            await Task.Run(() => Dispose());
        }
    }
    
    // Simple metrics tracking
    public class TrackingMetrics {
        private readonly ConcurrentQueue<TimeSpan> _processingTimes = new();
        private long _totalOperations = 0;
        
        public void RecordTrackingOperation(string carId, TimeSpan processingTime) {
            Interlocked.Increment(ref _totalOperations);
            _processingTimes.Enqueue(processingTime);
            
            // Keep only recent times for memory efficiency
            while (_processingTimes.Count > 1000) {
                _processingTimes.TryDequeue(out _);
            }
        }
        
        public TimeSpan GetAverageProcessingTime() {
            var times = _processingTimes.ToArray();
            if (times.Length == 0) return TimeSpan.Zero;
            
            var totalTicks = times.Sum(t => t.Ticks);
            return new TimeSpan(totalTicks / times.Length);
        }
        
        public long GetTotalOperations() => _totalOperations;
    }
}