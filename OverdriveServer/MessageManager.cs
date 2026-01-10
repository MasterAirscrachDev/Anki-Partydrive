using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using static OverdriveServer.Tracks;
using OverdriveBLEProtocol;
using AsyncAwaitBestPractices;
using System.Threading.Channels;
namespace OverdriveServer {
    class MessageManager {
        readonly Channel<MessageTask> _messageQueue;
        readonly ChannelWriter<MessageTask> _messageWriter;
        readonly Task[] _workers;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly int _workerCount;
        
        struct MessageTask {
            public byte[] Content { get; init; }
            public Car Car { get; init; }
        }
        
        public MessageManager() {
            _workerCount = Math.Max(2, Environment.ProcessorCount / 2); // Use half of available cores, minimum 2
            _messageQueue = Channel.CreateUnbounded<MessageTask>();
            _messageWriter = _messageQueue.Writer;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Start worker tasks for parallel message processing
            _workers = new Task[_workerCount];
            for (int i = 0; i < _workerCount; i++) {
                _workers[i] = Task.Run(() => ProcessMessagesAsync(_cancellationTokenSource.Token));
            }
            Program.Log($"MessageManager: INIT @ {_workerCount}");
        }
        public void ParseMessage(byte[] content, Car car){
            // Enqueue message for parallel processing
            if (!_messageWriter.TryWrite(new MessageTask { Content = content, Car = car })) {
                // Queue is closed, process synchronously as fallback
                ProcessMessage(content, car);
            }
        }
        
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken) {
            try {
                await foreach (var messageTask in _messageQueue.Reader.ReadAllAsync(cancellationToken)) {
                    try {
                        if (messageTask.Car != null) {
                            ProcessMessage(messageTask.Content, messageTask.Car);
                        }
                    } catch (Exception ex) {
                        Program.Log($"Error processing message from car {messageTask.Car?.name ?? "unknown"}: {ex.Message}");
                    }
                }
            } catch (OperationCanceledException) {
                // Expected when shutting down
            } catch (Exception ex) {
                Program.Log($"Critical error in message processing worker: {ex.Message}");
            }
        }
        
        private void ProcessMessage(byte[] content, Car car){
            byte id = content[1];
            if(id == (int)MSG.RECV_PING){//23 ping response
                Program.Log($"[23] Ping response: {Program.BytesToString(content)}");
            } else if(id == (int)MSG.RECV_CAR_VERSION){ //25 version response
                //int8 size, int8 id, int16 version
                //read the version from the content
                short version = BitConverter.ToInt16(content, 2);
                //Program.Log($"[25] Version response: {version} for {car.name}");
                car.SetCarSoftwareVersion(version); 
            } else if(id == (int)MSG.RECV_BATTERY_VOLTAGE){ //27 battery response
                //int battery = content[2];
                //Program.Log($"[27] Battery response: {battery} / {maxBattery}");
                //Program.UtilLog($"27:{car.id}:{battery}");
            } else if(id == (int)MSG.RECV_TR_POSITION_UPDATE){ //39 where is car
                OnPosition(Parser.RECV_PositionUpdate(content), car); //moved to dedicated function bc its complex
            } else if(id == (int)MSG.RECV_TR_TRANSITION_UPDATE){ //41 car moved between track pieces
                OnTransition(Parser.RECV_TransitionUpdate(content), car); //moved to dedicated function bc its complex
            } else if(id == (int)MSG.RECV_TR_INTERSECTION_POSITION_UPDATE){ //42 track intersection
                Structures.IntersectionPositionUpdate intersection = Parser.RECV_IntersectionPositionUpdate(content);
                bool isReversed = false;
                //calculate reversed from data
                //1 - 2 is a line, 3 - 4 is a line
                if (intersection.isExiting) {
                    if(intersection.intersectionCode == 1 || intersection.intersectionCode == 3)
                    { isReversed = true; }
                    // location is void, segment is 10 (intersection) bits is 4, reversed is from intersection code
                    car.UpdateTrackingValues(-1, 10, intersection.offset, 4, isReversed); //update tracking values with intersection data
                }
                //Program.Log($"[42] Intersection position update: code:{(int)intersection.intersectionCode}, exiting:{intersection.isExiting} for {car.name}");
            } else if(id == (int)MSG.RECV_TR_CAR_DELOCALIZED){ //43 Off track
                OnDelocalized(car); //moved to dedicated function for reuse
            } else if(id == (int)MSG.RECV_OFFSET_FROM_ROAD_CENTER_UPDATE){ //45 Track center updated
                float offset = BitConverter.ToSingle(content, 2); //get the offset from the content
                car.data.offsetMM = offset; //set the offset
            } else if(id == (int)MSG.RECV_SPEED_UPDATE){ //54 car speed changed
                Structures.SpeedUpdate speed = Parser.RECV_SpeedUpdate(content);
                speed.desiredSpeedMMPS = (short)car.GetUnbalancedSpeed(speed.desiredSpeedMMPS); //apply speed balancing
                Program.UtilLog($"{MSG_CAR_SPEED_UPDATE}:{car.id}:{speed.desiredSpeedMMPS}:{speed.actualSpeedMMPS}");
                //Program.Log($"Car {car.name} speed update: Desired={speed.desiredSpeedMMPS}mm/s, Actual={speed.actualSpeedMMPS}mm/s", true);
                car.data.speedMMPS = speed.desiredSpeedMMPS; //set the speed
            } else if(id == (int)MSG.RECV_STATUS_UPDATE){ //63 car status changed
                Structures.CarStatus state = Parser.RECV_CarStatusUpdate(content);
                Program.UtilLog($"{MSG_CAR_STATUS_UPDATE}:{car.id}");
                car.GoIfNotGoing(state.onTrack); //if the car is supposed to be going, go (used when reconnecting after a power blip)
                if(!car.data.charging && car.data.onTrack && state.onCharger){ //if the car is not charging and is on the charger, delocalize it
                    OnDelocalized(car); //delocalize the car   
                }else{
                    car.data.onTrack = state.onCharger ? false : state.onTrack; //if the car is on the charger, it is not on track
                }
                car.data.charging = state.onCharger;
                car.data.batteryStatus = state.hasChargedBattery ? 1 : (state.hasLowBattery ? -1 : 0); //0 = normal, 1 = charged, -1 = low battery
            } else if(id == (int)MSG.RECV_LANE_CHANGE_UPDATE){ //65 car lane change status update
                //Program.UtilLog($"65:{car.id}");
            } else if(id == (int)MSG.RECV_TR_DELOC_AUTO_RECOVERY_ENTERED){ //67 car auto recovery
                //Program.UtilLog($"67:{car.id}");
            } else if(id == (int)MSG.RECV_TR_DELOC_AUTO_RECOVERY_SUCCESS){ //68 car auto recovery success
                car.data.onTrack = true; //set the car to on track
            }else if(id == (int)MSG.RECV_TR_JUMP_PIECE_BOOST){ //75 car jumped
                Program.UtilLog($"{MSG_CAR_JUMPED}:{car.id}");
                CarEventJumpCall?.Invoke(car.id);
            } else if(id == (int)MSG.RECV_TR_COLLISION_DETECTED){ //77 Collision Detected
                //Program.UtilLog($"77:{car.id}");
            } else if(id == (int)MSG.RECV_JUMP_PIECE_RESULT){ //78 Jump Piece Result
                Structures.JumpPieceResult result = Parser.RECV_JumpPieceResult(content); //get the result from the content
                Program.UtilLog($"{MSG_CAR_LANDED}:{car.id}:{!result.jumpFailed}"); //log the result
            } else if(id == (int)MSG.RECV_SUPERCODE){ //79 Supercode
                //Program.UtilLog($"79:{car.id}");
            } else if(id == (int)MSG.RECV_TR_SPECIAL){ //83 FnF specialBlock
                Program.UtilLog($"{MSG_CAR_POWERUP}:{car.id}");
            } else if(id == (int)MSG.RECV_DEV_CYCLE_OVERTIME){ //134 Car message cycle overtime
                //Program.UtilLog($"134:{car.id}");
            } else if(id == (int)MSG.RECV_DEV_UPDATE_MODEL_RESPONSE){ //148 Car model update response
                var response = Parser.RECV_UpdateModelResponse(content);
                string statusText = response.status == 0 ? "SUCCESS" : $"ERROR({response.status})";
                Program.Log($"[148] Car {car.name} model update response: {statusText} - Model ID: {response.modelID}");
                car.RequestCarDisconnect().SafeFireAndForget(); //disconnect the car after a model update (needs to reconnect)
            }
            else{
                Program.Log($"???({id})[{Program.IntToByteString(id)}]:{Program.BytesToString(content)}");
            }
        }
        void OnDelocalized(Car car){
            Program.UtilLog($"{MSG_CAR_DELOCALIZED}:{car.id}");
            car.data.onTrack = false; //set the car to off track
            CarEventDelocalised?.Invoke(car.id);
        }
        void OnPosition(Structures.PositionUpdate p, Car car){
            try{
                bool reversed = (p.parsingFlags & (int)ParseflagsMask.REVERSE_PARSING) != 0; //check if the car is reversed
                int bits = p.parsingFlags & (int)ParseflagsMask.NUM_BITS; //get the parsing flags
                p.speedMMPS = (short)car.GetUnbalancedSpeed(p.speedMMPS); //apply speed balancing
                if(Program.sendExtraTracking){
                    Program.socketMan.Notify(EVENT_CAR_LOCATION, 
                        new LocationData {
                            carID = car.id,
                            locationID = p.locationID,
                            trackID = p.segmentID,
                            offsetMM = p.offset,
                            speedMMPS = p.speedMMPS,
                            reversed = reversed
                        }
                    );
                }
                
                car.data.speedMMPS = p.speedMMPS; //set the speed

                car.UpdateTrackingValues(p.locationID, p.segmentID, p.offset, bits, reversed); //check the lane

                CarEventLocationCall?.Invoke(car.id, p.locationID, p.segmentID, p.offset, p.speedMMPS, reversed);
            }catch (Exception e){
                Console.WriteLine($"Position error: {e}"); //catch any errors
            }
        }
        void OnTransition(Structures.TransitionUpdate u, Car car){
            // There is a shorter segment for the starting line track. (may fail on inside turns) 35, 34
            bool crossedStartingLine = (u.leftWheelDistanceCM < 36) && (u.leftWheelDistanceCM > 32) && (u.rightWheelDistanceCM < 36) && (u.rightWheelDistanceCM > 32);
            if(Program.sendExtraTracking){
                Program.socketMan.Notify(EVENT_CAR_TRANSITION, 
                    new TransitionData{
                        carID = car.id,
                        trackPiece = u.currentSegmentIdx,
                        oldTrackPiece = u.previousSegmentIdx,
                        offsetMM = u.offset,
                        uphillCounter = u.uphillCounter,
                        downhillCounter = u.downhillCounter,
                        leftWheelDistance = u.leftWheelDistanceCM,
                        rightWheelDistance = u.rightWheelDistanceCM,
                        crossedStartingLine = crossedStartingLine
                    }
                );
            }
            
            CarEventTransitionCall?.Invoke(car.id, u.currentSegmentIdx, u.previousSegmentIdx, u.offset, u.uphillCounter, u.downhillCounter, u.leftWheelDistanceCM, u.rightWheelDistanceCM, crossedStartingLine); //call the event
            car.data.offsetMM = u.offset;
            
            SolveSegment(car, u.leftWheelDistanceCM, u.rightWheelDistanceCM, crossedStartingLine, u.offset, u.uphillCounter, u.downhillCounter); //solve the segment
            car.lastSegmentID = 0; // reset last position ID to avoid incorrect assumptions
        }
        void SolveSegment(Car car, int left, int right, bool finish, float offset, int up, int down){
            Segment segment = new Segment(SegmentType.Unknown, 0, false); //create a new track piece
            int ID = car.lastSegmentID; //get the last track piece ID
            bool reversed = car.lastReversed; //get the last track piece 
            if(ID != 0){ 
                SegmentType type = SegmentTypeFromID(ID);
                if(type != SegmentType.Unknown){ segment = new Segment(type, ID, reversed); } //if we have a valid ID, set the segment to that
            }else{
                SegmentType type = (Abs(left - right) < 4) ? SegmentType.Straight : SegmentType.Turn;
                if(type == SegmentType.Straight && finish){ type = SegmentType.PreFinishLine; } //if we are straight and at the finish line, set it to prefinish line
                reversed = (type == SegmentType.Straight) ? false : (left > right);
                segment = new Segment(type, 0, reversed); //fallback to straight or turn
            }
            segment.SetUpDown(up, down); //set the up and down values
            CarEventSegmentCall?.Invoke(car.id, segment, offset); //call the event
            Program.socketMan.Notify(EVENT_CAR_SEGMENT,  
                new SegmentData{
                    type = segment.type,
                    internalID = segment.internalID,
                    reversed = segment.flipped,
                    up = segment.up,
                    down = segment.down,
                    validated = segment.validated
                }
            );
        }
        int Abs(int i) { return i < 0 ? -i : i; }

        //subscribable event
        public delegate void CarEventLocation(string carID, int trackLocation, int trackID, float offset, int speed, bool reversed);
        public event CarEventLocation? CarEventLocationCall;
        public delegate void CarEventTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine);
        public event CarEventTransition? CarEventTransitionCall;
        public delegate void CarEventSegment(string carID, Segment trackPiece, float offset);
        public event CarEventSegment? CarEventSegmentCall;
        public delegate void CarEventFell(string carID);
        public event CarEventFell? CarEventDelocalised;
        public delegate void CarEventJump(string carID);
        public event CarEventJump? CarEventJumpCall;
        
        public async Task DisposeAsync() {
            try {
                // Signal completion and wait for workers to finish
                _messageWriter.Complete();
                _cancellationTokenSource.Cancel();
                
                // Wait for all workers to complete with timeout
                await Task.WhenAll(_workers).WaitAsync(TimeSpan.FromSeconds(5));
                
                Program.Log($"MessageManager disposed - processed remaining messages");
            } catch (TimeoutException) {
                Program.Log("MessageManager disposal timed out - some messages may not have been processed");
            } catch (Exception ex) {
                Program.Log($"Error during MessageManager disposal: {ex.Message}");
            } finally {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}