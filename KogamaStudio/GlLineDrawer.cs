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
    internal static int PreviewMode = 0;       // 0 = Outline, 1 = Surface Grid, 2 = Full Grid
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
        new[] { 0, 3, 2, 1 }, // Bottom
        new[] { 4, 5, 6, 7 }, // Top
        new[] { 0, 1, 6, 7 }, // Front
        new[] { 2, 3, 4, 5 }, // Back
        new[] { 0, 7, 4, 3 }, // Left
        new[] { 1, 2, 5, 6 }  // Right
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
                // Modes 0 & 1: Topology-based processing
                var faceCounts = new Dictionary<((int, int, int), (int, int, int), (int, int, int), (int, int, int)), int>();
                var allGrid = new (int, int, int)[cubes.Count][];

                // Pass 1: Count structural faces
                for (int ci = 0; ci < cubes.Count; ci++)
                {
                    var cube = cubes[ci];
                    var c = ClipboardManager.CornerBytesToCoords(cube.Corners);
                    var gi = new (int, int, int)[8];
                    for (int i = 0; i < 8; i++)
                        gi[i] = (cube.X * 4 + c[i].z, cube.Y * 4 + c[i].y, cube.Z * 4 + c[i].x);
                    allGrid[ci] = gi;

                    foreach (var fv in FaceVertices)
                    {
                        var fk = CreateFaceKey(gi[fv[0]], gi[fv[1]], gi[fv[2]], gi[fv[3]]);
                        // Skip fully collapsed faces (all vertices in the same spot or degenerate line)
                        if (fk.Item1 == fk.Item3) continue;

                        faceCounts.TryGetValue(fk, out var count);
                        faceCounts[fk] = count + 1;
                    }
                }

                // Pass 2: Extract external faces and process edges
                var externalEdgeNormals = new Dictionary<((int, int, int), (int, int, int)), List<(long, long, long)>>();
                var surfaceEdges = new HashSet<((int, int, int), (int, int, int))>();

                for (int ci = 0; ci < cubes.Count; ci++)
                {
                    var gi = allGrid[ci];
                    foreach (var fv in FaceVertices)
                    {
                        var p0 = gi[fv[0]];
                        var p1 = gi[fv[1]];
                        var p2 = gi[fv[2]];
                        var p3 = gi[fv[3]];

                        var fk = CreateFaceKey(p0, p1, p2, p3);
                        if (!faceCounts.ContainsKey(fk)) continue;

                        // If face appears exactly once, it is exposed to the outside
                        if (faceCounts[fk] == 1)
                        {
                            var edges = new[] {
                                CreateEdgeKey(p0, p1),
                                CreateEdgeKey(p1, p2),
                                CreateEdgeKey(p2, p3),
                                CreateEdgeKey(p3, p0)
                            };

                            if (PreviewMode == 1)
                            {
                                foreach (var ek in edges)
                                {
                                    if (ek.Item1 != ek.Item2) // Skip zero-length edge
                                        surfaceEdges.Add(ek);
                                }
                            }
                            else if (PreviewMode == 0)
                            {
                                var normal = CalcNormal(p0, p1, p2, p3);

                                foreach (var ek in edges)
                                {
                                    if (ek.Item1 == ek.Item2) continue; // Skip zero-length edge

                                    if (!externalEdgeNormals.TryGetValue(ek, out var nList))
                                    {
                                        nList = new List<(long, long, long)>();
                                        externalEdgeNormals[ek] = nList;
                                    }
                                    nList.Add(normal);
                                }
                            }
                        }
                    }
                }

                // Finalize Output for Modes 0 & 1
                if (PreviewMode == 1)
                {
                    _cachedEdges.AddRange(surfaceEdges);
                }
                else if (PreviewMode == 0)
                {
                    foreach (var kvp in externalEdgeNormals)
                    {
                        var ek = kvp.Key;
                        var normals = kvp.Value;

                        // Draw if it's a boundary edge OR if connecting faces are not coplanar (sharp crease)
                        if (normals.Count == 1 ||
                           (normals.Count == 2 && !AreParallel(normals[0], normals[1])) ||
                           normals.Count > 2)
                        {
                            _cachedEdges.Add(ek);
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

    private static ((int, int, int), (int, int, int), (int, int, int), (int, int, int)) CreateFaceKey((int, int, int) p0, (int, int, int) p1, (int, int, int) p2, (int, int, int) p3)
    {
        var arr = new[] { p0, p1, p2, p3 };
        System.Array.Sort(arr, GridCompare);
        return (arr[0], arr[1], arr[2], arr[3]);
    }

    private static ((int, int, int), (int, int, int)) CreateEdgeKey((int, int, int) a, (int, int, int) b)
    {
        return GridCompare(a, b) < 0 ? (a, b) : (b, a);
    }

    // Calculates normal using the cross product of the diagonals (p2 - p0) x (p3 - p1).
    // This correctly handles faces where 2 adjacent vertices have collapsed into a single point.
    private static (long x, long y, long z) CalcNormal((int x, int y, int z) p0, (int x, int y, int z) p1, (int x, int y, int z) p2, (int x, int y, int z) p3)
    {
        long dx1 = p2.x - p0.x;
        long dy1 = p2.y - p0.y;
        long dz1 = p2.z - p0.z;

        long dx2 = p3.x - p1.x;
        long dy2 = p3.y - p1.y;
        long dz2 = p3.z - p1.z;

        return (
            dy1 * dz2 - dz1 * dy2,
            dz1 * dx2 - dx1 * dz2,
            dx1 * dy2 - dy1 * dx2
        );
    }

    private static bool AreParallel((long x, long y, long z) n1, (long x, long y, long z) n2)
    {
        // If normal is dead zero (extreme topological collapse), treat as non-parallel to force crease rendering
        if ((n1.x == 0 && n1.y == 0 && n1.z == 0) || (n2.x == 0 && n2.y == 0 && n2.z == 0))
            return false;

        long cx = n1.y * n2.z - n1.z * n2.y;
        long cy = n1.z * n2.x - n1.x * n2.z;
        long cz = n1.x * n2.y - n1.y * n2.x;
        return cx == 0 && cy == 0 && cz == 0;
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
