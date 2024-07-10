using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class HeightTesting : MonoBehaviour
{
    [SerializeField] bool test;
    [SerializeField] Segment[] SourceSegments;
    // Update is called once per frame
    void Update()
    {
        if(test){
            test = false;
            CalculateHeights();
        }
    }
    void CalculateHeights(){
        Segment[] calculatedSegments = new Segment[SourceSegments.Length];
        CalculationSegment[] calcSegments = new CalculationSegment[SourceSegments.Length];
        // Define direction changes for left and right curves
        int[] leftTurn = {3, 0, 1, 2}; // Maps current direction to new direction for left turn
        int[] rightTurn = {1, 2, 3, 0}; // Maps current direction to new direction for right turn
        // Define movement deltas for each direction: 0 = up, 1 = right, 2 = down, 3 = left
        (int dx, int dy)[] movement = { (0, 1), (1, 0), (0, -1), (-1, 0) };
        int dir = 0; //north = 0, east = 1, south = 2, west = 3
        int x = 0, y = 0;
        for (int i = 0; i < SourceSegments.Length; i++){
            TrackPieceType piece = SourceSegments[i].type;
            calcSegments[i] = new CalculationSegment(){
                type = piece,
                elevation = SourceSegments[i].elevation,
                index = i,
                X = x,
                Y = y,
                up = SourceSegments[i].up,
                down = SourceSegments[i].down,
                flipped = SourceSegments[i].flipped
            };
            //Debug.Log($"Segment {i}: {piece}({x}, {y})");
            int startDir = dir;
            if(piece == TrackPieceType.Turn){ dir = SourceSegments[i].flipped ? leftTurn[dir] : rightTurn[dir]; }
            // For straight pieces or any piece that is not Unknown or PreFinishLine, update coordinates directly
            if(piece != TrackPieceType.Unknown){
                x += movement[dir].dx;
                y += movement[dir].dy;
            }
            
        }
        for(int i = 0; i < SourceSegments.Length; i++){
            calculatedSegments[i] = SourceSegments[i];
            if(i == 0){ calculatedSegments[i].elevation = 0; }
            else{
                CalculationSegment[] segmentsAtXY = GetSegmentsFromXY(calcSegments[i], calcSegments);
                if(segmentsAtXY.Length == 1){
                    int lastElevation = calculatedSegments[i - 1].elevation;
                    if(segmentsAtXY[0].up > 200 && segmentsAtXY[0].down == 0){
                        calculatedSegments[i].elevation = lastElevation + 2;
                    }
                    else if(segmentsAtXY[0].down > 200 && segmentsAtXY[0].up == 0){
                        calculatedSegments[i].elevation = lastElevation - 2;
                    }
                    else{
                        calculatedSegments[i].elevation = lastElevation;
                    }

                }
                else if(segmentsAtXY.Length == 2){
                    int indexOfThis = segmentsAtXY[0].index == i ? 0 : 1;
                    int indexOfOther = indexOfThis == 0 ? 1 : 0;
                    //check if we went 0ver 240 up and over 70 down
                    if(segmentsAtXY[indexOfThis].up > 200 && segmentsAtXY[indexOfThis].down > 70 && (segmentsAtXY[indexOfThis].type != TrackPieceType.Turn)){
                        calculatedSegments[i].elevation = -1;
                    }
                    else if(segmentsAtXY[indexOfThis].up > 200 && segmentsAtXY[indexOfThis].down == 0){
                        calculatedSegments[i].elevation = calculatedSegments[i - 1].elevation + 2;
                    }
                    else if(segmentsAtXY[indexOfThis].down > 200 && segmentsAtXY[indexOfThis].up == 0){
                        calculatedSegments[i].elevation = calculatedSegments[i - 1].elevation - 2;
                    }
                    else{
                        if(segmentsAtXY[indexOfThis].up < 30 && segmentsAtXY[indexOfThis].down < 30){
                            if(calculatedSegments[i - 1].elevation != -1){
                                calculatedSegments[i].elevation = calculatedSegments[i - 2].elevation;
                            }
                            else{
                                calculatedSegments[i].elevation = calculatedSegments[i - 1].elevation;
                            }
                        }
                        else{
                            int thisDown = segmentsAtXY[indexOfThis].down;
                            int nextDown = calcSegments[i + 1].down;
                            if(thisDown < 100 && nextDown < 100 && (thisDown + nextDown) > 100){    
                                calculatedSegments[i].elevation = calculatedSegments[i - 1].elevation - 2;
                            }
                        }
                    }
                    if(calculatedSegments[i].elevation < 0){
                        if(calculatedSegments[i].type == TrackPieceType.Turn){
                            calculatedSegments[i].elevation = 0;
                        }
                        else if(calculatedSegments[i].elevation != -1){
                            calculatedSegments[i].elevation = 0;
                        }
                    }
                }
                else{
                    if(segmentsAtXY.Length == 0){continue;}
                    Debug.LogError($"ERR: {segmentsAtXY.Length} segments at the same XY {calcSegments[i].X}, {calcSegments[i].Y}");
                    for(int j = 0; j < segmentsAtXY.Length; j++){
                        Debug.LogError($"Segment {j}: {segmentsAtXY[j].type}({segmentsAtXY[j].index})");
                    }
                }
                calcSegments[i].heightSet = true;
            }
        }

        FindObjectOfType<TrackGenerator>().Generate(calculatedSegments);
    }
    CalculationSegment[] GetSegmentsFromXY(CalculationSegment calculationSegment, CalculationSegment[] calcSegments){
        List<CalculationSegment> segmentsAtXY = new List<CalculationSegment>();
        for(int j = 0; j < calcSegments.Length; j++){
            if(calculationSegment.atXY(calcSegments[j])){ segmentsAtXY.Add(calcSegments[j]); }
        }
        return segmentsAtXY.ToArray();
    }
    class CalculationSegment{
        public TrackPieceType type;
        public int elevation, up, down;
        public bool flipped;
        public int index, X, Y;
        public bool heightSet = false;
        public bool atXY(CalculationSegment other){
            if(type == TrackPieceType.Unknown || other.type == TrackPieceType.Unknown){ return false; }
            return X == other.X && Y == other.Y;
        }
    }
}
