using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;

namespace OverdriveServer
{
    class TrackScanner
    {
        List<TrackPiece> trackPieces;
        TrackPiece[] intialScan, confirmScan;
        int finishesPassed = 0, totalFinishes = 0;
        bool tracking = false, checkingScan = false, successfulScan = false, firstTick = true, finishedScan = false;
        int retries = 0, maxRetries = 3;
        int scanSpeed = 530;
        Car scanningCar;
        public async Task ScanTrack(Car car, int finishlines){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            Program.messageManager.CarEvent += OnCarEvent;
            await car.SetCarSpeed(480, 500);
            await car.SetCarTrackCenter(0);
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
                    for(int i = 0; i < intialScan.Length; i++){
                        TrackPieceType hopeful = TrackPieceType.Unknown;
                        TrackPieceType compare = TrackPieceType.Unknown;
                        if(i < confirmScan.Length){ compare = confirmScan[i].type; }
                        if(i < intialScan.Length){ hopeful = intialScan[i].type; }
                        Console.WriteLine($"Track {i} hopeful: {hopeful} compare: {compare}");
                        //Program.Log($"Track {i} hopeful: {hopeful} compare: {compare}");
                    }
                    retries++;
                    if(retries <= maxRetries){
                        Program.Log("Retrying track scan");
                    }
                    else{
                        Program.Log("Track scan failed");
                        return true;
                    }

                }
                else{
                    bool matched = true;
                    for(int i = 0; i < intialScan.Length; i++){
                        if(intialScan[i].type != confirmScan[i].type){
                            Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}");
                            matched = false; break;
                        }
                    }
                    if(matched){
                        Program.Log("Track scan successful");
                        successfulScan = true; return true;
                    }
                    else{
                        Program.Log("Track scan comparison failed");
                        retries++; return !(retries <= maxRetries);
                    }
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
            finishedScan = true;
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
            int height = 0;
            for(int i = 1; i < points.Count - 2; i++){
                //go through all points and check if there are any with the same position
                int pointsAtPos = 0;
                List<solvePoint> pointsAtPosition = new List<solvePoint>();
                for(int j = 0; j < points.Count; j++){
                    if(points[j].x == points[i].x && points[j].y == points[i].y && j != i){
                        pointsAtPos++;
                        pointsAtPosition.Add(points[j]);
                    }
                }
                if(pointsAtPos > 0){
                    //if there are point at this positon with a lower index

                    //TODO: turns can occupy the same space, so we need to check if the point is a turn

                    int lowerIndex = -1;
                    foreach(solvePoint point in pointsAtPosition){
                        if(point.solvingIndex < points[i].solvingIndex){
                            lowerIndex = point.solvingIndex;
                        }
                    }
                    if(lowerIndex != -1){
                        height = points[lowerIndex].piece.height + 2;
                        points[i].piece.height = height;
                        points[i + 1].piece.height = height;
                        Console.WriteLine($"Height at {points[i].x}, {points[i].y} for {points[i].piece.type}({i}) is {height}");
                    }
                }
            }

            for(int i = 0; i < points.Count; i++){ solvedPieces[i] = points[i].piece; }
            return solvedPieces;
        }
        void OnCarEvent(string content){
            string[] data = content.Split(':');
            if(data[0] == "41"){ //Track transition
                tracking = true;
                if(firstTick){ firstTick = false; return; }

            }
            else if(data[0] == "39"){ //Track location
                if(tracking){
                    tracking = false;
                    int trackID = int.Parse(data[3]);
                    if(finishesPassed <= 0){
                        if(trackID == 33){
                            finishesPassed++;
                            if(finishesPassed == 1){
                                trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0));
                            }
                        }
                    }
                    else{
                        if(trackID == 33){
                            finishesPassed++;
                            if(finishesPassed > totalFinishes){
                                bool stop = ScanLoopDone();
                                if(stop){
                                    scanningCar.SetCarSpeed(0, 500);
                                    Program.messageManager.CarEvent -= OnCarEvent;
                                    SendFinishedTrack();
                                    return;
                                }else{
                                    scanningCar.SetCarSpeed(scanSpeed, 500); //slow down a bit if we failed
                                }
                            }
                            trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0)); SendCurrentTrack();
                            //Console.WriteLine("Finish line detected");
                        }
                        else{
                            if(trackID == 36 || trackID == 39 || trackID == 40){
                                trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 0)); SendCurrentTrack();
                            }
                            else if(trackID == 17 || trackID == 18 || trackID == 20 || trackID == 23){
                                bool clockwise = bool.Parse(data[6]);
                                trackPieces.Add(clockwise ? new TrackPiece(TrackPieceType.CurveRight, 0) : new TrackPiece(TrackPieceType.CurveLeft, 0));
                                SendCurrentTrack();
                            }
                            else if(trackID == 57){
                                bool clockwise = bool.Parse(data[6]);
                                trackPieces.Add(clockwise ? new TrackPiece(TrackPieceType.PowerupR, 0) : new TrackPiece(TrackPieceType.PowerupL, 0));
                                SendCurrentTrack();
                            }
                            else if(trackID == 34){
                                trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 0));
                                //SendCurrentTrack();
                            }
                        }
                    }
                }
            }
            else if(data[0] == "45"){
                //scanningCar.SetCarLane(24);
            }
        }
        void SendCurrentTrack(){
            if(checkingScan){ return; }
            string content = $"-3:{scanningCar.id}";
            foreach(TrackPiece piece in trackPieces){
                content += $":{(int)piece.type}";
            }
            Program.UtilLog(content);
        }
        class TrackPiece{
            public TrackPieceType type;
            public int height;
            public TrackPiece(TrackPieceType type, int height){
                this.type = type;
                this.height = height;
            }
        }
        enum TrackPieceType{
            Straight,
            CurveLeft,
            CurveRight,
            PowerupL,
            StartFinish,
            PreFinishLine,
            PowerupR,
            Unknown
        }
    }
}
