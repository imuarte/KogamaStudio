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

    private static readonly int[] CubeEdges =
    {
        0,3, 3,2, 2,1, 1,0,
        7,4, 4,5, 5,6, 6,7,
        0,7, 3,4, 2,5, 1,6
    };

    // Each edge borders two faces. For each edge index (0..11), store which two faces it belongs to.
    // Faces: 0=bottom(0,1,2,3) 1=top(4,5,6,7) 2=front(0,1,6,7) 3=back(2,3,4,5) 4=left(0,3,4,7) 5=right(1,2,5,6)
    // Edge pairs from CubeEdges: 0:(0,3) 1:(3,2) 2:(2,1) 3:(1,0)  4:(7,4) 5:(4,5) 6:(5,6) 7:(6,7)  8:(0,7) 9:(3,4) 10:(2,5) 11:(1,6)
    private static readonly int[][] EdgeFaces =
    {
        new[]{0, 4}, // edge 0: 0-3  bottom + left
        new[]{0, 3}, // edge 1: 3-2  bottom + back
        new[]{0, 5}, // edge 2: 2-1  bottom + right
        new[]{0, 2}, // edge 3: 1-0  bottom + front
        new[]{1, 4}, // edge 4: 7-4  top + left
        new[]{1, 3}, // edge 5: 4-5  top + back
        new[]{1, 5}, // edge 6: 5-6  top + right
        new[]{1, 2}, // edge 7: 6-7  top + front
        new[]{2, 4}, // edge 8: 0-7  front + left
        new[]{3, 4}, // edge 9: 3-4  back + left
        new[]{3, 5}, // edge 10: 2-5  back + right
        new[]{2, 5}, // edge 11: 1-6  front + right
    };

    // Face definitions: normal direction offsets for neighbor check
    // Face 0 (bottom Y-): check (0,-1,0), Face 1 (top Y+): check (0,+1,0)
    // Face 2 (front Z-): check (0,0,-1), Face 3 (back Z+): check (0,0,+1)
    // Face 4 (left X-): check (-1,0,0), Face 5 (right X+): check (+1,0,0)
    private static readonly (int dx, int dy, int dz)[] FaceNeighborOffsets =
    {
        (0, -1, 0),  // face 0: bottom
        (0, +1, 0),  // face 1: top
        (0, 0, -1),  // face 2: front
        (0, 0, +1),  // face 3: back
        (-1, 0, 0),  // face 4: left
        (+1, 0, 0),  // face 5: right
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

            var allGrid = new (int, int, int)[cubes.Count][];
            var edgeCounts = new Dictionary<((int, int, int), (int, int, int)), int>();

            // For Surface Grid mode: track which cube positions exist
            HashSet<(int, int, int)> cubePositions = null;
            if (PreviewMode == 1)
                cubePositions = new HashSet<(int, int, int)>();

            for (int ci = 0; ci < cubes.Count; ci++)
            {
                var cube = cubes[ci];
                var c = ClipboardManager.CornerBytesToCoords(cube.Corners);
                var gi = new (int, int, int)[8];
                for (int i = 0; i < 8; i++)
                    gi[i] = (cube.X * 4 + c[i].z, cube.Y * 4 + c[i].y, cube.Z * 4 + c[i].x);
                allGrid[ci] = gi;

                if (cubePositions != null)
                    cubePositions.Add((cube.X, cube.Y, cube.Z));

                for (int i = 0; i < CubeEdges.Length; i += 2)
                {
                    var a = gi[CubeEdges[i]];
                    var b = gi[CubeEdges[i + 1]];
                    var key = GridLe(a, b) ? (a, b) : (b, a);
                    edgeCounts.TryGetValue(key, out var cnt);
                    edgeCounts[key] = cnt + 1;
                }
            }

            for (int ci = 0; ci < cubes.Count; ci++)
            {
                var gi = allGrid[ci];
                var cube = cubes[ci];

                for (int ei = 0; ei < CubeEdges.Length; ei += 2)
                {
                    var a = gi[CubeEdges[ei]];
                    var b = gi[CubeEdges[ei + 1]];
                    var key = GridLe(a, b) ? (a, b) : (b, a);

                    if (PreviewMode == 0)
                    {
                        // Outline: only edges belonging to exactly one cube
                        if (edgeCounts[key] == 1)
                            _cachedEdges.Add(key);
                    }
                    else if (PreviewMode == 1)
                    {
                        // Surface Grid: draw edge if at least one of its two adjacent faces is exposed
                        int edgeIdx = ei / 2;
                        int faceA = EdgeFaces[edgeIdx][0];
                        int faceB = EdgeFaces[edgeIdx][1];

                        var offA = FaceNeighborOffsets[faceA];
                        var offB = FaceNeighborOffsets[faceB];

                        bool faceAExposed = !cubePositions.Contains((cube.X + offA.dx, cube.Y + offA.dy, cube.Z + offA.dz));
                        bool faceBExposed = !cubePositions.Contains((cube.X + offB.dx, cube.Y + offB.dy, cube.Z + offB.dz));

                        if (faceAExposed || faceBExposed)
                            _cachedEdges.Add(key);
                    }
                    else
                    {
                        // Full Grid: all edges
                        _cachedEdges.Add(key);
                    }
                }
            }

            // Deduplicate for Surface Grid and Full Grid modes
            if (PreviewMode >= 1)
            {
                var unique = new HashSet<((int,int,int),(int,int,int))>(_cachedEdges);
                _cachedEdges.Clear();
                _cachedEdges.AddRange(unique);
            }
        }

        foreach (var (a, b) in _cachedEdges)
        {
            var wa = rot * new Vector3(a.Item1 / 4f * scl.x, a.Item2 / 4f * scl.y, a.Item3 / 4f * scl.z) + pos;
            var wb = rot * new Vector3(b.Item1 / 4f * scl.x, b.Item2 / 4f * scl.y, b.Item3 / 4f * scl.z) + pos;
            Line(wa, wb, lineColor);
        }
    }

    private static bool GridLe((int, int, int) a, (int, int, int) b)
    {
        if (a.Item1 != b.Item1) return a.Item1 < b.Item1;
        if (a.Item2 != b.Item2) return a.Item2 < b.Item2;
        return a.Item3 <= b.Item3;
    }

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
