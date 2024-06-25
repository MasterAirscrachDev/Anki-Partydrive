using static OverdriveServer.Tracks;
namespace OverdriveServer
{
    class TrackScanner{
        List<TrackPiece> trackPieces;
        TrackPiece[] intialScan, confirmScan;
        int finishesPassed = 0, totalFinishes = 0;
        bool tracking = false, checkingScan = false, successfulScan = false, finishedScan = false;
        int retries = 0, maxRetries = 3;
        int scanSpeed = 530;
        Car scanningCar;
        public async Task<bool> CancelScan(Car cancelThis){
            if(scanningCar == cancelThis){
                SetEventsSub(false);
                await scanningCar.SetCarSpeed(0, 500);
                finishedScan = true;
                return true;
            }
            return false;
        }
        void SetEventsSub(bool sub){
            if(sub){ 
                Program.messageManager.CarEventLocationCall += OnTrackPosition; 
                Program.messageManager.CarEventTransitionCall += OnTrackTransition;
            }else{ 
                Program.messageManager.CarEventLocationCall -= OnTrackPosition;
                Program.messageManager.CarEventTransitionCall -= OnTrackTransition;
            }
        }
        public async Task ScanTrack(Car car, int finishlines){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            SetEventsSub(true);
            await car.SetCarSpeed(480, 500);
            //await car.SetCarTrackCenter(0);
            await car.SetCarLane(24);
            while (!finishedScan){
                await Task.Delay(500);
            }
        }
        bool ScanLoopDone(){
            if(!checkingScan){
                checkingScan = true;
                intialScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                scanningCar.SetCarSpeed(scanSpeed, 500);
                scanSpeed -= 10;
            }else{
                scanSpeed -= 10;
                confirmScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                //compare the two track scans
                if(intialScan.Length != confirmScan.Length){
                    Program.Log($"\n\nlengths do not match {intialScan.Length} != {confirmScan.Length}");
                    //log the descrepency and its index
                    for(int i = 0; i < intialScan.Length; i++){
                        if(i >= confirmScan.Length){ Program.Log($"Index {i} does not exist in confirmScan"); }
                        else if(intialScan[i].type != confirmScan[i].type){ Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}"); }
                    }
                    retries++;
                    if(retries <= maxRetries){ Program.Log("Retrying track scan"); }
                    else{ Program.Log("Track scan failed"); return true; }
                }
                else{
                    bool matched = true;
                    for(int i = 0; i < intialScan.Length; i++){
                        if(intialScan[i].type != confirmScan[i].type){
                            Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}");
                            matched = false; break;
                        }
                    }
                    if(matched){ Program.Log("Track scan successful"); successfulScan = true; return true; }
                    Program.Log("Track scan comparison failed"); retries++; return !(retries <= maxRetries);
                }
            }
            return false;
        }
        async Task SendFinishedTrack(){
            while(scanningCar.data.speed > 0){
                await Task.Delay(500);
                await scanningCar.SetCarSpeed(0, 500);
            }
            string content = $"-4:{scanningCar.id}:{successfulScan}";
            TrackPiece[] solvedPieces = HeightSolver();
            foreach(TrackPiece piece in solvedPieces){
                content += $":{(int)piece.type}:{piece.height}";
            }
            Program.UtilLog(content);
            if(successfulScan){
                Program.trackManager.SetTrack(solvedPieces);
            }
            finishedScan = true;
        }
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            tracking = true;
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(tracking){
                tracking = false;
                if(trackID == 33){
                    finishesPassed++;
                    if(finishesPassed <= 1){
                        if(finishesPassed == 1){ trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0)); SendCurrentTrack();}
                    }else{
                        if(finishesPassed > totalFinishes){
                            bool stop = ScanLoopDone();
                            if(stop){
                                scanningCar.SetCarSpeed(0, 500);
                                SetEventsSub(false);
                                SendFinishedTrack(); return;
                            }else{ scanningCar.SetCarSpeed(scanSpeed, 500); trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0));  } //slow down a bit if we failed
                        }
                    }
                }else if(finishesPassed >= 1){
                    TrackPieceType pt = PeiceFromID(trackID, clockwise);
                    trackPieces.Add(new TrackPiece(pt, 0));
                    if(pt != TrackPieceType.Unknown || pt != TrackPieceType.PreFinishLine){
                        SendCurrentTrack();
                    }
                }
            }
        }
        void SendCurrentTrack(){
            if(checkingScan){ return; }
            string content = $"-3:{scanningCar.id}";
            foreach(TrackPiece piece in trackPieces){ content += $":{(int)piece.type}"; }
            Program.UtilLog(content);
        }
        public static TrackPieceType PeiceFromID(int id, bool clockwise = false){
            if(id == 17 || id == 18 || id == 20 || id == 23){ return clockwise ? TrackPieceType.CurveRight : TrackPieceType.CurveLeft; }
            else if(id == 36 || id == 39 || id == 40){ return TrackPieceType.Straight; }
            else if(id == 57){ return clockwise ? TrackPieceType.PowerupR : TrackPieceType.PowerupL; }
            else if(id == 34){ return TrackPieceType.PreFinishLine; }
            else if(id == 33){ return TrackPieceType.StartFinish; }
            else{ return TrackPieceType.Unknown; }
        }

        TrackPiece[] HeightSolver(){
            TrackPiece[] solvedPieces = new TrackPiece[intialScan.Length];
            List<solvePoint> points = new List<solvePoint>();
            // Define direction changes for left and right curves
            int[] leftTurn = {3, 0, 1, 2}; // Maps current direction to new direction for left turn
            int[] rightTurn = {1, 2, 3, 0}; // Maps current direction to new direction for right turn
            // Define movement deltas for each direction: 0 = up, 1 = right, 2 = down, 3 = left
            (int dx, int dy)[] movement = { (0, 1), (1, 0), (0, -1), (-1, 0) };
            int dir = 0; //north = 0, east = 1, south = 2, west = 3
            int x = 0, y = 0;
            for (int i = 0; i < intialScan.Length; i++){
                TrackPiece piece = intialScan[i];
                int startDir = dir;
                if(piece.type == TrackPieceType.CurveLeft){ dir = leftTurn[dir];  } // Update direction for left curve
                else if(piece.type == TrackPieceType.CurveRight){ dir = rightTurn[dir];  } // Update direction for right curve
                // For straight pieces or any piece that is not Unknown or PreFinishLine, update coordinates directly
                if(piece.type != TrackPieceType.Unknown && piece.type != TrackPieceType.PreFinishLine){
                    x += movement[dir].dx;
                    y += movement[dir].dy;
                }
                points.Add(new solvePoint(piece, x, y, points.Count));
                if(piece.type == TrackPieceType.CurveLeft || piece.type == TrackPieceType.CurveRight){
                    points[points.Count - 1].SetCurvePoints(startDir, dir);
                }
            }
            Console.WriteLine($"Solving for heights, source: {intialScan.Length} points, {points.Count} solve points");
            // Solve for heights
            for(int i = 1; i < points.Count - 2; i++){
                //go through all points and check if there are any with the same position
                int pointsAtPos = 0;
                List<solvePoint> pointsAtPosition = new List<solvePoint>();
                for(int j = 0; j < points.Count; j++){
                    if(j != i && points[j].x == points[i].x && points[j].y == points[i].y){
                        pointsAtPos++; pointsAtPosition.Add(points[j]);
                    }
                }
                if(pointsAtPos > 0){
                    //if there are point at this positon with a lower index
                    int lowerIndex = pointsAtPosition.Where(point => point.solvingIndex < points[i].solvingIndex)
                        .Select(point => (int?)point.solvingIndex).Min() ?? -1;
                    if(lowerIndex != -1){
                        //if this is a turn and the other point is a turn
                        if(points[i].piece.type == TrackPieceType.CurveLeft || points[i].piece.type == TrackPieceType.CurveRight){
                            if(points[lowerIndex].piece.type == TrackPieceType.CurveLeft || points[lowerIndex].piece.type == TrackPieceType.CurveRight){
                                //if there are no matches in the tStart and tEnd values
                                int[] dirs = {points[i].tStart, points[i].tEnd, points[lowerIndex].tStart, points[lowerIndex].tEnd};
                                //if dir has no matching elements, then the two points are not connected
                                if(dirs.Distinct().Count() == dirs.Length){ continue; }
                            }
                        }
                        int height = points[lowerIndex].piece.height + 2;
                        points[i].piece.height = height;
                        points[i + 1].piece.height = height;
                        Console.WriteLine($"Height at {points[i].x}, {points[i].y} for {points[i].piece.type}({i}) is {height}");
                    }
                }
            }
            for(int i = 0; i < points.Count; i++){ solvedPieces[i] = points[i].piece; }
            return solvedPieces;
        }
        class solvePoint{
            public TrackPiece piece;
            public int x, y, tStart, tEnd;
            public int solvingIndex;
            public solvePoint(TrackPiece piece, int x, int y, int solvingIndex){
                this.piece = piece;
                this.x = x;
                this.y = y;
                this.solvingIndex = solvingIndex;
            }
            public void SetCurvePoints(int tStart, int tEnd){
                this.tStart = tStart;
                this.tEnd = tEnd;
            }
        }
    }
}
