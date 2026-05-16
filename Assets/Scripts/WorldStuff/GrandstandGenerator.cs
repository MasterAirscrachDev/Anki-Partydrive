using System.Collections.Generic;
using UnityEngine;

public class GrandstandGenerator : MonoBehaviour
{
    [SerializeField] Transform trackParent;
    [SerializeField] Mesh grandstandMeshSlice;

    void Start()
    {
        //GenerateGrandstand();
    }

    public void GenerateGrandstand()
    {
        List<Vector3> outline = ComputeTrackOutline();
        if (outline == null || outline.Count == 0) return;

        List<Vector3> smoothed = ChamferOutline(outline, 0.25f);
        List<Vector3> clean    = RemoveCollinear(smoothed);

        if (grandstandMeshSlice != null)
        {
            Mesh mesh = BuildGrandstandMesh(clean);
            if (mesh == null) return;
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            if (GetComponent<MeshRenderer>() == null)
                gameObject.AddComponent<MeshRenderer>();
        }
    }

    Mesh BuildGrandstandMesh(List<Vector3> outline)
    {
        Vector3[] sliceVerts = grandstandMeshSlice.vertices;
        int N = outline.Count;

        if (sliceVerts.Length < 2) { Debug.LogWarning("grandstandMeshSlice needs at least 2 vertices."); return null; }
        if (N < 2) return null;

        // Sort slice vertices into a nearest-neighbour chain starting from the
        // lowest point so the profile strip is in a consistent traversal order
        // regardless of how Unity stored the verts in the mesh asset.
        sliceVerts = SortProfileChain(sliceVerts);
        int S = sliceVerts.Length;

        // Re-centre the profile so its bottom vertex is at the local origin.
        // This ensures rotation happens around the base of the grandstand slice,
        // not the mesh asset's arbitrary pivot.
        Vector3 profileBase = sliceVerts[0];
        for (int j = 0; j < S; j++) sliceVerts[j] -= profileBase;

        // Pre-compute world-space ring positions for all outline stations.
        Vector3[][] rings = new Vector3[N][];
        for (int i = 0; i < N; i++)
        {
            Vector3 cur     = outline[i];
            Vector3 prev    = outline[(i - 1 + N) % N];
            Vector3 next    = outline[(i + 1) % N];
            Vector3 tangent = ((cur - prev).normalized + (next - cur).normalized).normalized;
            if (tangent.sqrMagnitude < 0.001f) tangent = (next - cur).normalized;
            Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
            rings[i] = new Vector3[S];
            for (int j = 0; j < S; j++)
                rings[i][j] = cur + rot * sliceVerts[j];
        }

        // Arc-length U coordinates along the outline (0 at start, 1 after full loop).
        float[] uCoords = new float[N + 1];
        for (int i = 0; i < N; i++)
            uCoords[i + 1] = uCoords[i] + Vector3.Distance(outline[i], outline[(i + 1) % N]);
        float totalLen = uCoords[N];
        for (int i = 0; i <= N; i++) uCoords[i] /= totalLen;

        // Arc-length V coordinates along the profile (0 at bottom, 1 at top).
        float[] vCoords = new float[S];
        for (int j = 1; j < S; j++)
            vCoords[j] = vCoords[j - 1] + Vector3.Distance(sliceVerts[j], sliceVerts[j - 1]);
        float profileLen = vCoords[S - 1];
        if (profileLen > 0f)
            for (int j = 0; j < S; j++) vCoords[j] /= profileLen;

        // Each quad gets 4 unique vertices so RecalculateNormals produces flat (per-face) shading.
        int quadCount = N * (S - 1);
        var verts = new Vector3[quadCount * 4];
        var uvs   = new Vector2[quadCount * 4];
        var tris  = new int[quadCount * 6];

        int vi = 0, ti = 0;
        for (int i = 0; i < N; i++)
        {
            int ni = (i + 1) % N;
            float uA = uCoords[i];
            float uB = uCoords[i + 1];
            for (int j = 0; j < S - 1; j++)
            {
                verts[vi]   = rings[i][j];     uvs[vi]   = new Vector2(uA, vCoords[j]);
                verts[vi+1] = rings[i][j+1];   uvs[vi+1] = new Vector2(uA, vCoords[j+1]);
                verts[vi+2] = rings[ni][j];    uvs[vi+2] = new Vector2(uB, vCoords[j]);
                verts[vi+3] = rings[ni][j+1];  uvs[vi+3] = new Vector2(uB, vCoords[j+1]);

                tris[ti]   = vi;   tris[ti+1] = vi+2; tris[ti+2] = vi+3;
                tris[ti+3] = vi;   tris[ti+4] = vi+3; tris[ti+5] = vi+1;

                vi += 4; ti += 6;
            }
        }

        var mesh = new Mesh { name = "Grandstand" };
        if (verts.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Remove points that sit on a straight line between their neighbours.
    static List<Vector3> RemoveCollinear(List<Vector3> pts, float dotThreshold = 0.9999f)
    {
        int n = pts.Count;
        var result = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
        {
            Vector3 prev = pts[(i - 1 + n) % n];
            Vector3 cur  = pts[i];
            Vector3 next = pts[(i + 1) % n];

            Vector3 toPrev = (prev - cur).normalized;
            Vector3 toNext = (next - cur).normalized;

            // If the two directions are nearly opposite the point is collinear — skip it.
            if (Vector3.Dot(toPrev, toNext) < -dotThreshold) continue;
            result.Add(cur);
        }
        return result;
    }

    // For every corner, replace it with two points each lerped 'amount' (0–0.5) toward
    // the adjacent points, giving a chamfered/bevelled outline.
    static List<Vector3> ChamferOutline(List<Vector3> pts, float amount)
    {
        int n = pts.Count;
        var result = new List<Vector3>(n * 2);
        for (int i = 0; i < n; i++)
        {
            Vector3 prev = pts[(i - 1 + n) % n];
            Vector3 cur  = pts[i];
            Vector3 next = pts[(i + 1) % n];
            result.Add(Vector3.Lerp(cur, prev, amount));
            result.Add(Vector3.Lerp(cur, next, amount));
        }
        return result;
    }

    // Returns the outer CCW outline of all 1x1 tile objects in trackParent (XZ plane).
    List<Vector3> ComputeTrackOutline()
    {
        if (trackParent == null) return null;

        // Snap each child to a grid cell
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        float avgY = 0f;
        int count = 0;
        foreach (Transform child in trackParent)
        {
            Vector3 pos = child.position;
            cells.Add(new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z)));
            avgY += pos.y;
            count++;
        }
        if (count == 0) return null;
        avgY /= count;

        // Build directed boundary edges for the CCW outer contour.
        // Corners are represented in integer coords scaled by 2 so (cx*2±1, cz*2±1)
        // gives the four corners of cell (cx, cz) without any floating-point ambiguity.
        Dictionary<Vector2Int, Vector2Int> nextVert = new Dictionary<Vector2Int, Vector2Int>();
        foreach (var cell in cells)
        {
            int x = cell.x * 2, z = cell.y * 2;
            // Right face exposed → edge goes +Z (CCW outer normal = +X)
            if (!cells.Contains(new Vector2Int(cell.x + 1, cell.y)))
                nextVert[new Vector2Int(x + 1, z - 1)] = new Vector2Int(x + 1, z + 1);
            // Top face exposed → edge goes -X (CCW outer normal = +Z)
            if (!cells.Contains(new Vector2Int(cell.x, cell.y + 1)))
                nextVert[new Vector2Int(x + 1, z + 1)] = new Vector2Int(x - 1, z + 1);
            // Left face exposed → edge goes -Z (CCW outer normal = -X)
            if (!cells.Contains(new Vector2Int(cell.x - 1, cell.y)))
                nextVert[new Vector2Int(x - 1, z + 1)] = new Vector2Int(x - 1, z - 1);
            // Bottom face exposed → edge goes +X (CCW outer normal = -Z)
            if (!cells.Contains(new Vector2Int(cell.x, cell.y - 1)))
                nextVert[new Vector2Int(x - 1, z - 1)] = new Vector2Int(x + 1, z - 1);
        }

        if (nextVert.Count == 0) return null;

        // Trace all closed loops and keep the one with the largest positive (CCW) signed area
        // — that is the outer boundary; inner holes come out CW (negative area).
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> outerPolygon = null;
        float maxArea = float.NegativeInfinity;

        foreach (var startVert in nextVert.Keys)
        {
            if (visited.Contains(startVert)) continue;

            List<Vector2Int> polygon = new List<Vector2Int>();
            Vector2Int cur = startVert;
            while (!visited.Contains(cur) && nextVert.ContainsKey(cur))
            {
                visited.Add(cur);
                polygon.Add(cur);
                cur = nextVert[cur];
            }

            if (polygon.Count < 3) continue;

            float area = SignedArea(polygon);
            if (area > maxArea)
            {
                maxArea = area;
                outerPolygon = polygon;
            }
        }

        if (outerPolygon == null) return null;

        var result = new List<Vector3>(outerPolygon.Count);
        foreach (var p in outerPolygon)
            result.Add(new Vector3(p.x * 0.5f, avgY, p.y * 0.5f));
        return result;
    }

    // Sort profile vertices bottom-to-top by Y, then by X for ties.
    static Vector3[] SortProfileChain(Vector3[] verts)
    {
        var sorted = new Vector3[verts.Length];
        System.Array.Copy(verts, sorted, verts.Length);
        System.Array.Sort(sorted, (a, b) =>
        {
            int c = a.y.CompareTo(b.y);
            return c != 0 ? c : a.x.CompareTo(b.x);
        });
        return sorted;
    }

    // Shoelace formula — positive = CCW in XZ viewed from +Y.
    static float SignedArea(List<Vector2Int> polygon)
    {
        float area = 0f;
        int n = polygon.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2Int a = polygon[i];
            Vector2Int b = polygon[(i + 1) % n];
            area += (float)(a.x * b.y - b.x * a.y);
        }
        return area * 0.5f;
    }

    [ContextMenu("Generate Grandstand")]
    void GenerateGrandstandContextMenu()
    {
        GenerateGrandstand();
    }
}
