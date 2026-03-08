using UnityEngine;
using Il2Cpp;
using MelonLoader;
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

    internal static void Init()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) { MelonLogger.Msg("[GlLineDrawer] shader NULL"); return; }
        _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        _cam = MVGameControllerBase.MainCameraManager?.MainCamera;
        MelonLogger.Msg($"[GlLineDrawer] cam={(_cam != null ? _cam.name : "NULL")}");
        MelonCoroutines.Start(DrawCoroutine());
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

    private static readonly List<((int, int, int) a, (int, int, int) b)> _cachedEdges = new();
    private static int _cachedEdgesVersion = -1;

    public static void Cubes(List<CubeData> cubes, Color color,
        Vector3? position = null, Vector3? scale = null, Quaternion? rotation = null)
    {
        if (cubes == null || cubes.Count == 0) return;

        var pos = position ?? Vector3.zero;
        var scl = scale ?? Vector3.one;
        var rot = rotation ?? Quaternion.identity;

        int ver = ClipboardManager.Version;
        if (ver != _cachedEdgesVersion)
        {
            _cachedEdgesVersion = ver;
            _cachedEdges.Clear();

            var allGrid = new (int, int, int)[cubes.Count][];
            var edgeCounts = new Dictionary<((int, int, int), (int, int, int)), int>();

            for (int ci = 0; ci < cubes.Count; ci++)
            {
                var cube = cubes[ci];
                var c = ClipboardManager.CornerBytesToCoords(cube.Corners);
                var gi = new (int, int, int)[8];
                for (int i = 0; i < 8; i++)
                    gi[i] = (cube.X * 4 + c[i].z, cube.Y * 4 + c[i].y, cube.Z * 4 + c[i].x);
                allGrid[ci] = gi;

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
                for (int i = 0; i < CubeEdges.Length; i += 2)
                {
                    var a = gi[CubeEdges[i]];
                    var b = gi[CubeEdges[i + 1]];
                    var key = GridLe(a, b) ? (a, b) : (b, a);
                    if (edgeCounts[key] == 1)
                        _cachedEdges.Add(key);
                }
            }
        }

        foreach (var (a, b) in _cachedEdges)
        {
            var wa = rot * new Vector3(a.Item1 / 4f * scl.x, a.Item2 / 4f * scl.y, a.Item3 / 4f * scl.z) + pos;
            var wb = rot * new Vector3(b.Item1 / 4f * scl.x, b.Item2 / 4f * scl.y, b.Item3 / 4f * scl.z) + pos;
            Line(wa, wb, color);
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
