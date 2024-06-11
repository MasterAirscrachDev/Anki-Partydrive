using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting.Dependencies.Sqlite;
using System.Linq;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{


    public void GenerateTrackFromComplexPoints(Vector3[] points,int[] turnIndexes, float width){
        Quaternion firstPointRotation = Quaternion.LookRotation(points[1] - points[0]);
        for(int i = 0; i < points.Length - 1; i++){
            Vector3 Ppos = points[i];
            Quaternion Prot = (i < points.Length - 1) ? Quaternion.LookRotation(points[i + 1] - points[i]) : firstPointRotation;
            Vector3 lSide = Ppos - Prot * Vector3.right * width / 2f;
            Vector3 rSide = Ppos + Prot * Vector3.right * width / 2f;
            Debug.DrawLine(lSide, rSide, Color.red, 3f);
            if(points.Length > i + 1)
            {
                Vector3 Ppos2 = points[i + 1]; 
                Quaternion Prot2 = (i < points.Length - 2) ? Quaternion.LookRotation(points[i + 2] - points[i + 1]) : firstPointRotation;
                Vector3 lSide2 = Ppos2 - Prot2 * Vector3.right * width / 2f;
                Vector3 rSide2 = Ppos2 + Prot2 * Vector3.right * width / 2f;
                //Debug.DrawLine(lSide2, rSide2, Color.red, 100f);
                Debug.DrawLine(lSide, lSide2, Color.red, 3f);
                Debug.DrawLine(rSide, rSide2, Color.red, 3f);
                Debug.DrawLine(lSide, rSide2, Color.red, 3f);
            }
        }
        //generate as mesh
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Length * 2];
        int[] triangles = new int[(points.Length - 1) * 6];
        Vector2[] uv = new Vector2[points.Length * 2];
        int turnTicks = 0;
        float uvY = 0;
        for(int i = 0; i < points.Length; i++)
        {
            Vector3 Ppos = points[i]; 
            Quaternion Prot = (i < points.Length - 1) ? Quaternion.LookRotation(points[i + 1] - points[i]) : firstPointRotation;
            Vector3 lSide = Ppos - Prot * Vector3.right * width / 2f;
            Vector3 rSide = Ppos + Prot * Vector3.right * width / 2f;
            vertices[i * 2] = rSide;
            vertices[i * 2 + 1] = lSide;
            if(turnIndexes.Contains(i)){ turnTicks = 10; }else if(turnTicks > 0){ turnTicks--; }
            // Adjust UV mapping based on whether we are on a corner
            // Use accumulatedDistance for UV mapping, adjust calculation for corners
            // Adjust UV mapping for turns
            if(i > 0 && turnTicks > 0) {
                // For turns, adjust uvY based on the specific needs of the turn's curvature
                // This might involve a smaller increment or a different calculation
                uvY += 0.1f; // Example increment, adjust based on the desired texture stretch/compression
            }
            else if(i > 0) { uvY += 1.0f; } // Increment uvY for each quad to apply the texture once per quad

            // Apply the calculated UV Y coordinate to both vertices of the quad
            uv[i * 2] = new Vector2(0f, uvY);
            uv[i * 2 + 1] = new Vector2(1f, uvY);
            
            if(points.Length > i + 1)
            {
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = i * 2 + 1;
                triangles[i * 6 + 2] = i * 2 + 2;
                triangles[i * 6 + 3] = i * 2 + 2;
                triangles[i * 6 + 4] = i * 2 + 1;
                triangles[i * 6 + 5] = i * 2 + 3;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

    }
}