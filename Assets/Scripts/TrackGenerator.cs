using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] bool generateTrack = false;
    [SerializeField] int levelOD = 1;
    [SerializeField] STransform[] points;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(generateTrack)
        {
            generateTrack = false;
            GenerateComplexPoints();
            GenerateTrackFromComplexPoints();
        }
    }
    void GenerateComplexPoints(){
        points = new STransform[(transform.childCount * levelOD) - levelOD + 1];
        for(int p = 0; p < points.Length; p++){
            points[p] = new STransform();
        }
        int index = 0;
        for(int i = 0; i < transform.childCount; i++)
        {
            points[index].Inherit(transform.GetChild(i));
            Debug.DrawRay(points[index].position + (Vector3.up * index), points[index].rotation * Vector3.up, Color.magenta, 100f);
            index++;
            //await Task.Delay(300);
            if(levelOD > 1 && i < transform.childCount - 1)
            {
                for(int j = 1; j < levelOD; j++)
                {
                    //Debug.Log($"Generating point {index} from {i} to {i + 1}");
                    Vector3 trueInterpolatedPosition = Vector3.Lerp(transform.GetChild(i).position, transform.GetChild(i + 1).position, (float)j / levelOD);
                    //slightly shift the position based on the rotation of the previous point
                    float distBetweenPoints = Vector3.Distance(transform.GetChild(i).position, transform.GetChild(i + 1).position);
                    Vector3 shiftedPos = transform.GetChild(i).position + (transform.GetChild(i).forward * (distBetweenPoints * 0.5f));
                    Debug.DrawRay(shiftedPos, transform.GetChild(i).rotation * Vector3.up, Color.yellow, 100f);
                    //get a float between 0.2 and 0.8 based on how close j is to levelOD / 2
                    float shiftAmount = Mathf.Lerp(0.5f, 0.6f, Mathf.Abs((float)j - (float)levelOD / 2f) / ((float)levelOD / 2f));
                    Vector3 shiftedPos2 = Vector3.Lerp(shiftedPos, trueInterpolatedPosition, shiftAmount);
                    points[index].position = shiftedPos2;
                    //points[index].position = Vector3.Lerp(transform.GetChild(i).position, transform.GetChild(i + 1).position, (float)j / levelOD);
                    points[index].rotation = Quaternion.Lerp(transform.GetChild(i).rotation, transform.GetChild(i + 1).rotation, (float)j / levelOD);
                    points[index].scale = Vector3.Lerp(transform.GetChild(i).localScale, transform.GetChild(i + 1).localScale, (float)j / levelOD);
                    Debug.DrawRay(points[index].position + (Vector3.up * index), points[index].rotation * Vector3.up, Color.black, 100f);
                    index++;
                    //await Task.Delay(300);
                }
            }
        }
        for(int i = 0; i < points.Length - 1; i++)
        {
            Debug.DrawLine(points[i].position, points[i + 1].position, Color.blue, 100f);
            Debug.DrawRay(points[i].position, points[i].rotation * Vector3.up, Color.green, 100f);
            //await Task.Delay(150);
        }
    }
    async Task GenerateComplexPointsSlow(){
        points = new STransform[(transform.childCount * levelOD) - levelOD + 1];
        for(int p = 0; p < points.Length; p++){
            points[p] = new STransform();
        }
        int index = 0;
        for(int i = 0; i < transform.childCount; i++)
        {
            points[index].Inherit(transform.GetChild(i));
            Debug.DrawRay(points[index].position + (Vector3.up * index), points[index].rotation * Vector3.up, Color.magenta, 100f);
            index++;
            await Task.Delay(300);
            if(levelOD > 1 && i < transform.childCount - 1)
            {
                for(int j = 1; j < levelOD; j++)
                {
                    Debug.Log($"Generating point {index} from {i} to {i + 1}");
                    points[index].position = Vector3.SlerpUnclamped(transform.GetChild(i).position, transform.GetChild(i + 1).position, (float)j / levelOD);
                    points[index].rotation = Quaternion.Lerp(transform.GetChild(i).rotation, transform.GetChild(i + 1).rotation, (float)j / levelOD);
                    points[index].scale = Vector3.Lerp(transform.GetChild(i).localScale, transform.GetChild(i + 1).localScale, (float)j / levelOD);
                    Debug.DrawRay(points[index].position + (Vector3.up * index), points[index].rotation * Vector3.up, Color.black, 100f);
                    index++;
                    await Task.Delay(300);
                }
            }
        }
        for(int i = 0; i < points.Length - 1; i++)
        {
            Debug.DrawLine(points[i].position, points[i + 1].position, Color.blue, 100f);
            Debug.DrawRay(points[i].position, points[i].rotation * Vector3.up, Color.green, 100f);
            await Task.Delay(150);
        }
    }
    void GenerateTrack(){
        for(int i = 0; i < transform.childCount; i++)
        {
            Vector3 lSide = transform.GetChild(i).position - transform.GetChild(i).right * transform.GetChild(i).localScale.x / 2f;
            Vector3 rSide = transform.GetChild(i).position + transform.GetChild(i).right * transform.GetChild(i).localScale.x / 2f;
            Debug.DrawLine(lSide, rSide, Color.red, 100f);
            if(transform.childCount > i + 1)
            {
                Vector3 lSide2 = transform.GetChild(i + 1).position - transform.GetChild(i + 1).right * transform.GetChild(i + 1).localScale.x / 2f;
                Vector3 rSide2 = transform.GetChild(i + 1).position + transform.GetChild(i + 1).right * transform.GetChild(i + 1).localScale.x / 2f;
                //Debug.DrawLine(lSide2, rSide2, Color.red, 100f);
                Debug.DrawLine(lSide, lSide2, Color.red, 100f);
                Debug.DrawLine(rSide, rSide2, Color.red, 100f);
                Debug.DrawLine(lSide, rSide2, Color.red, 100f);
            }
        }
        //generate as mesh
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[transform.childCount * 2];
        int[] triangles = new int[(transform.childCount - 1) * 6];
        Vector2[] uv = new Vector2[transform.childCount * 2];
        for(int i = 0; i < transform.childCount; i++)
        {
            Vector3 lSide = transform.GetChild(i).position - transform.GetChild(i).right * transform.GetChild(i).localScale.x / 2f;
            Vector3 rSide = transform.GetChild(i).position + transform.GetChild(i).right * transform.GetChild(i).localScale.x / 2f;
            vertices[i * 2] = lSide;
            vertices[i * 2 + 1] = rSide;
            uv[i * 2] = new Vector2(0f, i);
            uv[i * 2 + 1] = new Vector2(1f, i);
            if(transform.childCount > i + 1)
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
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    void GenerateTrackFromComplexPoints(){
        for(int i = 0; i < points.Length - 1; i++)
        {
            Vector3 Ppos = points[i].position; Quaternion Prot = points[i].rotation; float Px = points[i].scale.x;
            Vector3 lSide = Ppos - Prot * Vector3.right * Px / 2f;
            Vector3 rSide = Ppos + Prot * Vector3.right * Px / 2f;
            Debug.DrawLine(lSide, rSide, Color.red, 100f);
            if(points.Length > i + 1)
            {
                Vector3 Ppos2 = points[i + 1].position; Quaternion Prot2 = points[i + 1].rotation; float Px2 = points[i + 1].scale.x;
                Vector3 lSide2 = Ppos2 - Prot2 * Vector3.right * Px2 / 2f;
                Vector3 rSide2 = Ppos2 + Prot2 * Vector3.right * Px2 / 2f;
                //Debug.DrawLine(lSide2, rSide2, Color.red, 100f);
                Debug.DrawLine(lSide, lSide2, Color.red, 100f);
                Debug.DrawLine(rSide, rSide2, Color.red, 100f);
                Debug.DrawLine(lSide, rSide2, Color.red, 100f);
            }
        }
        //generate as mesh
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Length * 2];
        int[] triangles = new int[(points.Length - 1) * 6];
        Vector2[] uv = new Vector2[points.Length * 2];
        for(int i = 0; i < points.Length; i++)
        {
            Vector3 Ppos = points[i].position; Quaternion Prot = points[i].rotation; float Px = points[i].scale.x;
            Vector3 lSide = Ppos - Prot * Vector3.right * Px / 2f;
            Vector3 rSide = Ppos + Prot * Vector3.right * Px / 2f;
            vertices[i * 2] = rSide;
            vertices[i * 2 + 1] = lSide;
            uv[i * 2] = new Vector2(0f, i);
            uv[i * 2 + 1] = new Vector2(1f, i);
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
    [System.Serializable]
    class STransform{
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public STransform(Vector3 pos, Quaternion rot, Vector3 sca){
            position = pos;
            rotation = rot;
            scale = sca;
        }
        public STransform(){
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
        }
        public void Inherit(Transform inheritFrom){
            position = inheritFrom.position;
            rotation = inheritFrom.rotation;
            scale = inheritFrom.localScale;
        }
    }
}