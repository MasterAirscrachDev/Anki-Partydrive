using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OverdriveServer.Tracks;

namespace OverdriveServer
{
    class TrackManager
    {
        public int totalStarts = 1;
        TrackPiece[]? track;
        List<TrackCarLocation> carLocations;
        public void SetTrack(TrackPiece[] track){ this.track = track; }
        public TrackManager(){
            carLocations = new List<TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventTransitionCall += OnCarTransition;
            PositionTicker();
        }
        async Task PositionTicker(){
            await Task.Delay(15000); //odds are we havent connected to cars and scanned a track in 15 seconds
            while(true){
                if(track == null){ await Task.Delay(4900); continue; }
                await Task.Delay(100);
                foreach(TrackCarLocation location in carLocations){
                    
                }
            }
        }
        void OnCarPosUpdate(string id, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            
        }
        void OnCarTransition(string id, int trackPiece, int oldTrackPiece, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            
        }
    }
    public class Tracks{
        public class TrackPiece{
            public TrackPieceType type;
            public int up, down;
            public TrackPiece(TrackPieceType type, int up, int down){
                this.type = type;
                this.up = up;
                this.down = down;
            }
            public TrackPiece(TrackPieceType type){
                this.type = type;
                this.up = 0;
                this.down = 0;
            }
            public void SetUpDown(int up, int down){
                this.up = up;
                this.down = down;
            }
        }
        public enum TrackPieceType{
            Straight,
            CurveLeft,
            CurveRight,
            PowerupL,
            StartFinish,
            PreFinishLine,
            PowerupR,
            Unknown
        }
        public class TrackCarLocation{
            public int trackIndex;
            public float trackPosition;
            public float horizontalPosition;
            List<TrackPieceType> lastTracks = new List<TrackPieceType>();
            public void AddTrack(TrackPieceType type){
                lastTracks.Add(type);
                if(lastTracks.Count > 6){ lastTracks.RemoveAt(0); }
            }
            public void ClearTracks(){ lastTracks.Clear(); }
        }
    }
}
