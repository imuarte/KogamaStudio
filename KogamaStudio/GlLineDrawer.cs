using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KogamaStudio.Clipboard;
using KogamaModFramework.Operations;

namespace KogamaStudio;

internal static class GlLineDrawer
{
    private static Material _mat;
    private static UnityEngine.Camera _cam;
    private static readonly List<(Vector3 from, Vector3 to, Color color)> _lines = new();

    // Preview settings (set from pipe command)
    internal static Color PreviewColor = Color.yellow;
    internal static int   PreviewMode = 0;       // 0 = Outline, 1 = Surface Grid, 2 = Full Grid
    internal static float PreviewOpacity = 1.0f;

    internal static void Init()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) { KogamaStudio.Log.LogInfo("[GlLineDrawer] shader NULL"); return; }
        _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        _cam = MVGameControllerBase.MainCameraManager?.MainCamera;
        KogamaStudio.Log.LogInfo($"[GlLineDrawer] cam={(_cam != null ? _cam.name : "NULL")}");
        KogamaStudioBehaviour.StartCo(DrawCoroutine());
    }

    internal static void BeginFrame() => _lines.Clear();

    public static void Line(Vector3 from, Vector3 to, Color color) =>
        _lines.Add((from, to, color));

    // Topology: 6 faces of a cube defined by vertex indices (0-7).
    private static readonly int[][] FaceVertices =
    {
        new[] { 0, 1, 2, 3 }, // Bottom
        new[] { 4, 5, 6, 7 }, // Top
        new[] { 0, 1, 6, 7 }, // Front
        new[] { 2, 3, 4, 5 }, // Back
        new[] { 0, 3, 4, 7 }, // Left
        new[] { 1, 6, 5, 2 }  // Right
    };

    private static readonly List<((int, int, int) a, (int, int, int) b)> _cachedEdges = new();
    private static int _cachedVersion = -1;
    private static int _cachedMode = -1;

    public static void Cubes(List<CubeData> cubes,
        Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        if (cubes == null || cubes.Count == 0) return;

        var pos = position ?? Vector3.zero;
        var scl = scale ?? Vector3.one;
        var rot = rotation ?? Quaternion.identity;

        var lineColor = new Color(PreviewColor.r, PreviewColor.g, PreviewColor.b, PreviewOpacity);

        int ver = ClipboardManager.Version;
        if (ver != _cachedVersion || PreviewMode != _cachedMode)
        {
            _cachedVersion = ver;
            _cachedMode = PreviewMode;
            _cachedEdges.Clear();

            if (PreviewMode == 2)
            {
                // Full Grid: Draw all unique edges blindly
                var allEdges = new HashSet<((int, int, int), (int, int, int))>();
                for (int ci = 0; ci < cubes.Count; ci++)
                {
                    var cube = cubes[ci];
                    var c = ClipboardManager.CornerBytesToCoords(cube.Corners);
                    var gi = new (int, int, int)[8];
                    for (int i = 0; i < 8; i++)
                        gi[i] = (cube.X * 4 + c[i].z, cube.Y * 4 + c[i].y, cube.Z * 4 + c[i].x);

                    foreach (var fv in FaceVertices)
                    {
                        var ek1 = CreateEdgeKey(gi[fv[0]], gi[fv[1]]);
                        var ek2 = CreateEdgeKey(gi[fv[1]], gi[fv[2]]);
                        var ek3 = CreateEdgeKey(gi[fv[2]], gi[fv[3]]);
                        var ek4 = CreateEdgeKey(gi[fv[3]], gi[fv[0]]);

                        if (ek1.Item1 != ek1.Item2) allEdges.Add(ek1);
                        if (ek2.Item1 != ek2.Item2) allEdges.Add(ek2);
                        if (ek3.Item1 != ek3.Item2) allEdges.Add(ek3);
                        if (ek4.Item1 != ek4.Item2) allEdges.Add(ek4);
                    }
                }
                _cachedEdges.AddRange(allEdges);
            }
            else
            {
                // Modes 0 & 1: CSG-like Architecture with Global Vertex Registry

                // Faza 1: Build Global Vertex Registry and Face Registry
                var vSetAll = new HashSet<(int, int, int)>();
                var faceCounts = new Dictionary<((int,int,int),(int,int,int),(int,int,int),(int,int,int)), (List<(int ci, int fi)>, (long,long,long))>();
                var allGrid = new (int, int, int)[cubes.Count][];

                for (int ci = 0; ci < cubes.Count; ci++)
                {
                    var cube = cubes[ci];
                    var c = ClipboardManager.CornerBytesToCoords(cube.Corners);
                    var gi = new (int, int, int)[8];
                    
                    for (int i = 0; i < 8; i++)
                    {
                        gi[i] = (cube.X * 4 + c[i].z, cube.Y * 4 + c[i].y, cube.Z * 4 + c[i].x);
                        vSetAll.Add(gi[i]); // Register every physical point in the model
                    }
                    
                    allGrid[ci] = gi;

                    for (int fi = 0; fi < FaceVertices.Length; fi++)
                    {
                        var fv = FaceVertices[fi];
                        var p0 = gi[fv[0]]; var p1 = gi[fv[1]]; 
                        var p2 = gi[fv[2]]; var p3 = gi[fv[3]];

                        var normal = CalcNormal(p0, p1, p2, p3);
                        if (normal.x == 0 && normal.y == 0 && normal.z == 0) continue; // Skip collapsed faces

                        var fk = CreateFaceKey(p0, p1, p2, p3);
                        if (!faceCounts.TryGetValue(fk, out var data))
                        {
                            data = (new List<(int ci, int fi)>(), normal);
                            faceCounts[fk] = data;
                        }
                        data.Item1.Add((ci, fi));
                    }
                }

                // Faza 2 & 3: Fragment Exposed Hull Geometry (Unconditional Edge Splitting)
                var atomicEdges = new Dictionary<((int,int,int), (int,int,int)), List<(long,long,long)>>();
                var exposedFaceList = new List<((int,int,int)[], (long,long,long))>();

                foreach (var kvp in faceCounts)
                {
                    var exposed = kvp.Value.Item1;
                    if (exposed.Count != 1) continue; // Cull perfectly matching coplanar hidden faces.

                    var ci = exposed[0].ci;
                    var fi = exposed[0].fi;
                    var gi = allGrid[ci];
                    var fv = FaceVertices[fi];
                    var normal = kvp.Value.Item2;

                    var pts = new[] { gi[fv[0]], gi[fv[1]], gi[fv[2]], gi[fv[3]] };
                    exposedFaceList.Add((pts, normal));
                }

                foreach (var face in exposedFaceList)
                {
                    var pts = face.Item1;
                    var normal = face.Item2;

                    for (int i = 0; i < 4; i++)
                    {
                        var A = pts[i];
                        var B = pts[(i + 1) % 4];
                        if (A == B) continue;

                        int minX = System.Math.Min(A.Item1, B.Item1);
                        int maxX = System.Math.Max(A.Item1, B.Item1);
                        int minY = System.Math.Min(A.Item2, B.Item2);
                        int maxY = System.Math.Max(A.Item2, B.Item2);
                        int minZ = System.Math.Min(A.Item3, B.Item3);
                        int maxZ = System.Math.Max(A.Item3, B.Item3);

                        var splits = new List<((int,int,int) v, long distSq)>();

                        // Test against ALL registered vertices to guarantee no missed T-junctions
                        foreach (var v in vSetAll)
                        {
                            if (v.Item1 < minX || v.Item1 > maxX ||
                                v.Item2 < minY || v.Item2 > maxY ||
                                v.Item3 < minZ || v.Item3 > maxZ) continue;

                            if (v == A || v == B) continue;

                            if (IsPointOnSegment(A, B, v))
                            {
                                long dx = v.Item1 - A.Item1;
                                long dy = v.Item2 - A.Item2;
                                long dz = v.Item3 - A.Item3;
                                splits.Add((v, dx * dx + dy * dy + dz * dz));
                            }
                        }

                        splits.Sort((a, b) => a.distSq.CompareTo(b.distSq));

                        var current = A;
                        foreach (var split in splits)
                        {
                            var v = split.v;
                            var key = CreateEdgeKey(current, v);
                            if (!atomicEdges.TryGetValue(key, out var list))
                            {
                                list = new List<(long, long, long)>();
                                atomicEdges[key] = list;
                            }
                            list.Add(normal);
                            current = v;
                        }
                        
                        var lastKey = CreateEdgeKey(current, B);
                        if (!atomicEdges.TryGetValue(lastKey, out var lastList))
                        {
                            lastList = new List<(long, long, long)>();
                            atomicEdges[lastKey] = lastList;
                        }
                        lastList.Add(normal);
                    }
                }

                // Faza 4: Output Resolve with Internal Cancellation
                foreach (var kvp in atomicEdges)
                {
                    var ek = kvp.Key;
                    var normals = kvp.Value;

                    // Resolve geometry cancellations: opposite normals annihilate each other
                    var resolved = new List<(long, long, long)>();
                    foreach (var n in normals)
                    {
                        var opp = (-n.Item1, -n.Item2, -n.Item3);
                        bool foundOpposite = false;
                        
                        for (int i = 0; i < resolved.Count; i++)
                        {
                            if (resolved[i] == opp)
                            {
                                resolved.RemoveAt(i);
                                foundOpposite = true;
                                break;
                            }
                        }
                        
                        if (!foundOpposite)
                            resolved.Add(n);
                    }

                    if (resolved.Count == 0) continue; // Completely internal hidden edge

                    if (PreviewMode == 1)
                    {
                        _cachedEdges.Add(ek); // Exposed to surface grid
                    }
                    else if (PreviewMode == 0)
                    {
                        if (resolved.Count == 1)
                        {
                            _cachedEdges.Add(ek); // Boundary line
                        }
                        else 
                        {
                            // If normals are different, it's a crease. If they are all exactly the same, it's a flat surface (hide).
                            bool allSame = true;
                            var firstN = resolved[0];
                            for (int i = 1; i < resolved.Count; i++)
                            {
                                if (resolved[i] != firstN)
                                {
                                    allSame = false;
                                    break;
                                }
                            }

                            if (!allSame)
                            {
                                _cachedEdges.Add(ek);
                            }
                        }
                    }
                }
            }
        }

        // Apply transformations and render
        foreach (var (a, b) in _cachedEdges)
        {
            var wa = rot * new Vector3(a.Item1 / 4f * scl.x, a.Item2 / 4f * scl.y, a.Item3 / 4f * scl.z) + pos;
            var wb = rot * new Vector3(b.Item1 / 4f * scl.x, b.Item2 / 4f * scl.y, b.Item3 / 4f * scl.z) + pos;
            Line(wa, wb, lineColor);
        }
    }

    // --- Math & Topology Helpers ---

    private static int GridCompare((int, int, int) a, (int, int, int) b)
    {
        int c1 = a.Item1.CompareTo(b.Item1);
        if (c1 != 0) return c1;
        int c2 = a.Item2.CompareTo(b.Item2);
        if (c2 != 0) return c2;
        return a.Item3.CompareTo(b.Item3);
    }

    private static ((int,int,int), (int,int,int)) CreateEdgeKey((int,int,int) a, (int,int,int) b)
    {
        return GridCompare(a, b) < 0 ? (a, b) : (b, a);
    }

    private static ((int,int,int), (int,int,int), (int,int,int), (int,int,int)) CreateFaceKey((int,int,int) p0, (int,int,int) p1, (int,int,int) p2, (int,int,int) p3)
    {
        var arr = new[] { p0, p1, p2, p3 };
        System.Array.Sort(arr, GridCompare);
        return (arr[0], arr[1], arr[2], arr[3]);
    }

    private static bool IsPointOnSegment((int x, int y, int z) a, (int x, int y, int z) b, (int x, int y, int z) v)
    {
        long abx = b.x - a.x, aby = b.y - a.y, abz = b.z - a.z;
        long avx = v.x - a.x, avy = v.y - a.y, avz = v.z - a.z;

        // Cross product must be strictly 0 for exact collinearity
        long cx = aby * avz - abz * avy;
        long cy = abz * avx - abx * avz;
        long cz = abx * avy - aby * avx;
        if (cx != 0 || cy != 0 || cz != 0) return false;

        // Dot product bounds check
        long dotAV_AB = avx * abx + avy * aby + avz * abz;
        if (dotAV_AB <= 0) return false;

        long dotAB_AB = abx * abx + aby * aby + abz * abz;
        if (dotAV_AB >= dotAB_AB) return false;

        return true;
    }

    private static (long x, long y, long z) CalcNormal((int x, int y, int z) p0, (int x, int y, int z) p1, (int x, int y, int z) p2, (int x, int y, int z) p3)
    {
        long dx1 = p2.x - p0.x;
        long dy1 = p2.y - p0.y;
        long dz1 = p2.z - p0.z;

        long dx2 = p3.x - p1.x;
        long dy2 = p3.y - p1.y;
        long dz2 = p3.z - p1.z;

        long nx = dy1 * dz2 - dz1 * dy2;
        long ny = dz1 * dx2 - dx1 * dz2;
        long nz = dx1 * dy2 - dy1 * dx2;

        return NormalizeNormal(nx, ny, nz);
    }

    private static long Gcd(long a, long b)
    {
        a = System.Math.Abs(a); b = System.Math.Abs(b);
        while (b != 0) { long t = b; b = a % b; a = t; }
        return a;
    }

    private static (long, long, long) NormalizeNormal(long nx, long ny, long nz)
    {
        long g = Gcd(nx, Gcd(ny, nz));
        if (g == 0) return (0, 0, 0);
        return (nx / g, ny / g, nz / g);
    }

    // --- Render Loop ---

    private static IEnumerator DrawCoroutine()
    {
        var eof = new WaitForEndOfFrame();
        while (true)
        {
            yield return eof;
            if (_lines.Count == 0 || _mat == null || _cam == null) continue;
            _mat.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(_cam.projectionMatrix);
            GL.modelview = _cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            foreach (var (from, to, color) in _lines)
            {
                GL.Color(color);
                GL.Vertex(from);
                GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
    }
}
