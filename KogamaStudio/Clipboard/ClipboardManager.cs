using MV.WorldObject;
using KogamaModFramework.Operations;
using KogamaStudio.Generating.Models;
using KogamaStudio.Tools.Properties;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio.Clipboard;

internal class ClipboardManager
{
    internal static List<CubeData> Clipboard;
    internal static List<CubeData> EditedClipboard;
    private static int _offsetX = 0;
    internal static int OffsetX { get => _offsetX; set { _offsetX = value; InvalidateCache(); } }
    private static int _offsetY = 0;
    internal static int OffsetY { get => _offsetY; set { _offsetY = value; InvalidateCache(); } }
    private static int _offsetZ = 0;
    internal static int OffsetZ { get => _offsetZ; set { _offsetZ = value; InvalidateCache(); } }
    private static int _rotationX = 0;
    internal static int RotationX { get => _rotationX; set { _rotationX = value; InvalidateCache(); } }
    private static int _rotationY = 0;
    internal static int RotationY { get => _rotationY; set { _rotationY = value; InvalidateCache(); } }
    private static int _rotationZ = 0;
    internal static int RotationZ { get => _rotationZ; set { _rotationZ = value; InvalidateCache(); } }
    private static bool _mirrorX = false;
    internal static bool MirrorX { get => _mirrorX; set { _mirrorX = value; InvalidateCache(); } }
    private static bool _mirrorY = false;
    internal static bool MirrorY { get => _mirrorY; set { _mirrorY = value; InvalidateCache(); } }
    private static bool _mirrorZ = false;
    internal static bool MirrorZ { get => _mirrorZ; set { _mirrorZ = value; InvalidateCache(); } }
    private static int _clipMinX = 0;
    internal static int ClipMinX { get => _clipMinX; set { _clipMinX = value; InvalidateCache(); } }
    private static int _clipMaxX = 0;
    internal static int ClipMaxX { get => _clipMaxX; set { _clipMaxX = value; InvalidateCache(); } }
    private static int _clipMinY = 0;
    internal static int ClipMinY { get => _clipMinY; set { _clipMinY = value; InvalidateCache(); } }
    private static int _clipMaxY = 0;
    internal static int ClipMaxY { get => _clipMaxY; set { _clipMaxY = value; InvalidateCache(); } }
    private static int _clipMinZ = 0;
    internal static int ClipMinZ { get => _clipMinZ; set { _clipMinZ = value; InvalidateCache(); } }
    private static int _clipMaxZ = 0;
    internal static int ClipMaxZ { get => _clipMaxZ; set { _clipMaxZ = value; InvalidateCache(); } }
    internal static bool Preview;
    internal static int Version = 0;

    internal static List<CubeData> BackupCubes;
    internal static int PreviewedObjectId = -1;
    internal static List<CubeData> CurrentCubeData;

    private static List<CubeData> _cachedTransformed;
    private static int _cachedTransformedVersion = -1;

    internal static void Clear()
    {
        Clipboard = null;
        InvalidateCache();
        SendCount();
    }

    internal static void CopyPropertiesModel()
    {
        Clipboard = KogamaModFramework.Operations.CubeOperations.GetAllCubes(ModelProperties.CubeModelBase);
        InvalidateCache();
        SendCount();
    }

    internal static void SendCount()
    {
        PipeClient.SendCommand($"clipboard_count|{Clipboard?.Count ?? 0}");
    }

    private static readonly byte[] KsCubesMagic = { (byte)'K', (byte)'S', (byte)'C', (byte)'B' };

    internal static void SaveToFile(string path)
    {
        if (Clipboard == null || Clipboard.Count == 0) { KogamaStudio.Log.LogWarning("[Clipboard] Nothing to save"); return; }
        if (!Path.IsPathRooted(path))
        {
            var dir = Path.Combine(PathHelper.GetPath(), "Generate", "Cubes");
            Directory.CreateDirectory(dir);
            path = Path.Combine(dir, path + ".kscubes");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Open(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        bw.Write(KsCubesMagic);
        bw.Write((byte)1); // version
        bw.Write(Clipboard.Count);
        foreach (var c in Clipboard)
        {
            bw.Write(c.X);
            bw.Write(c.Y);
            bw.Write(c.Z);
            bw.Write(c.Materials);
            bw.Write(c.Corners);
        }
        KogamaStudio.Log.LogInfo($"[Clipboard] Saved {Clipboard.Count} cubes to {path}");
    }

    internal static void LoadFromFile(string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(PathHelper.GetPath(), "Generate", "Cubes", path + ".kscubes");
        if (!File.Exists(path)) { KogamaStudio.Log.LogWarning($"[Clipboard] File not found: {path}"); return; }
        using var fs = File.OpenRead(path);
        bool isJson = fs.ReadByte() == '{';
        fs.Seek(0, SeekOrigin.Begin);
        if (isJson)
        {
            KogamaStudio.Log.LogWarning("[Clipboard] JSON .kscubes format is deprecated and will be removed in v1.0.0. Resave the file to convert it to binary.");
            using var reader = new StreamReader(fs);
            var jObj = Newtonsoft.Json.Linq.JObject.Parse(reader.ReadToEnd());
            var cubesArray = jObj["cubes"] as Newtonsoft.Json.Linq.JArray;
            if (cubesArray == null) { KogamaStudio.Log.LogWarning("[Clipboard] Invalid file"); return; }
            Clipboard = cubesArray.Select(c => new CubeData(
                c.Value<int>("x"), c.Value<int>("y"), c.Value<int>("z"),
                c["materials"]?.ToObject<byte[]>(), c["corners"]?.ToObject<byte[]>()
            )).ToList();
        }
        else
        {
            using var br = new BinaryReader(fs);
            var magic = br.ReadBytes(4);
            if (magic[0] != 'K' || magic[1] != 'S' || magic[2] != 'C' || magic[3] != 'B')
            {
                KogamaStudio.Log.LogError("[Clipboard] Unknown file format");
                return;
            }
            br.ReadByte(); // version
            int count = br.ReadInt32();
            var cubes = new List<CubeData>(count);
            for (int i = 0; i < count; i++)
            {
                int x = br.ReadInt32();
                int y = br.ReadInt32();
                int z = br.ReadInt32();
                byte[] materials = br.ReadBytes(6);
                byte[] corners = br.ReadBytes(8);
                cubes.Add(new CubeData(x, y, z, materials, corners));
            }
            Clipboard = cubes;
        }
        InvalidateCache();
        SendCount();
        KogamaStudio.Log.LogInfo($"[Clipboard] Loaded {Clipboard.Count} cubes from {path}");
    }

    internal static List<CubeData> GetTransformedCubes()
    {
        if (Clipboard == null) return null;
        if (_cachedTransformedVersion == Version && _cachedTransformed != null)
            return _cachedTransformed;

        var edited = new List<CubeData>(Clipboard);
        if (ClipMinX != 0 || ClipMaxX != 0) edited = Clip(edited, 'x', ClipMinX, ClipMaxX);
        if (ClipMinY != 0 || ClipMaxY != 0) edited = Clip(edited, 'y', ClipMinY, ClipMaxY);
        if (ClipMinZ != 0 || ClipMaxZ != 0) edited = Clip(edited, 'z', ClipMinZ, ClipMaxZ);
        if (RotationX != 0) edited = Rotate(edited, 'x', RotationX);
        if (RotationY != 0) edited = Rotate(edited, 'y', RotationY);
        if (RotationZ != 0) edited = Rotate(edited, 'z', RotationZ);
        if (MirrorX) edited = Mirror(edited, 'x');
        if (MirrorY) edited = Mirror(edited, 'y');
        if (MirrorZ) edited = Mirror(edited, 'z');
        if (OffsetX != 0 || OffsetY != 0 || OffsetZ != 0) edited = Offset(edited, OffsetX, OffsetY, OffsetZ);

        _cachedTransformed = edited;
        _cachedTransformedVersion = Version;
        return _cachedTransformed;
    }

    private static void InvalidateCache() => Version++;

    internal static void ShowPreview()
    {
        Preview = Clipboard != null;
        Version++;
    }

    internal static int LastPasteTotalCubes = 0;
    private static object _pasteCoroutine = null;

    internal static void CancelPaste()
    {
        KogamaModFramework.Operations.CubeOperations.CancelGeneration = true;
        _pasteCoroutine = null;
        PipeClient.SendCommand("clipboard_paste_done");
    }

    internal static void PasteEditedModel()
    {
        var edited = new List<CubeData>(Clipboard);

        if (ClipMinX != 0 || ClipMaxX != 0)
            edited = Clip(edited, 'x', ClipMinX, ClipMaxX);
        if (ClipMinY != 0 || ClipMaxY != 0)
            edited = Clip(edited, 'y', ClipMinY, ClipMaxY);
        if (ClipMinZ != 0 || ClipMaxZ != 0)
            edited = Clip(edited, 'z', ClipMinZ, ClipMaxZ);

        if (RotationX != 0)
            edited = Rotate(edited, 'x', RotationX);
        if (RotationY != 0)
            edited = Rotate(edited, 'y', RotationY);
        if (RotationZ != 0)
            edited = Rotate(edited, 'z', RotationZ);

        if (MirrorX)
            edited = Mirror(edited, 'x');
        if (MirrorY)
            edited = Mirror(edited, 'y');
        if (MirrorZ)
            edited = Mirror(edited, 'z');

        if (OffsetX != 0 || OffsetY != 0 || OffsetZ != 0)
            edited = Offset(edited, OffsetX, OffsetY, OffsetZ);

        if (!KogamaModFramework.Operations.CubeOperations.IsBuilding)
        {
            LastPasteTotalCubes = edited.Count;
            KogamaStudioBehaviour.StartCo(KogamaModFramework.Operations.CubeOperations.Add(ModelProperties.CubeModelBase, edited));
        }
    }

    internal static List<CubeData> Offset(List<CubeData> cubes, int offsetX, int offsetY, int offsetZ)
    {
        return cubes.Select(c => new CubeData(c.X + offsetX, c.Y + offsetY, c.Z + offsetZ, c.Materials, c.Corners)).ToList();
    }

    internal static List<CubeData> Rotate(List<CubeData> cubes, char axis, int rotations)
    {
        var result = new List<CubeData>();
        int rot = (rotations / 90) % 4;
        if (rot == 0) return cubes;
        int[] perm = axis switch
        {
            'x' => new[] { 7, 6, 1, 0, 3, 2, 5, 4 },
            'y' => new[] { 1, 2, 3, 0, 7, 4, 5, 6 },
            'z' => new[] { 1, 6, 5, 2, 3, 4, 7, 0 },
            _ => new[] { 0, 1, 2, 3, 4, 5, 6, 7 }
        };
        foreach (var cube in cubes)
        {
            int newX = cube.X, newY = cube.Y, newZ = cube.Z;
            var corners = (byte[])cube.Corners.Clone();
            var mats = (byte[])cube.Materials.Clone();
            for (int r = 0; r < rot; r++)
            {
                if (axis == 'x')
                    (newY, newZ) = (-newZ, newY);
                else if (axis == 'y')
                    (newX, newZ) = (newZ, -newX);
                else if (axis == 'z')
                    (newX, newY) = (-newY, newX);

                var transformed = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    byte val = corners[i];
                    int x = val % 5, y = (val / 5) % 5, z = (val / 25) % 5;
                    if (axis == 'x')
                        (x, y) = (y, 4 - x);
                    else if (axis == 'y')
                        (x, z) = (4 - z, x);
                    else if (axis == 'z')
                        (y, z) = (z, 4 - y);
                    transformed[i] = (byte)(x + y * 5 + z * 25);
                }
                var permuted = new byte[8];
                for (int i = 0; i < 8; i++)
                    permuted[i] = transformed[perm[i]];
                corners = permuted;

                if (axis == 'x')
                    (mats[1], mats[2], mats[4], mats[5]) =
                        (mats[5], mats[1], mats[2], mats[4]);
                else if (axis == 'y')
                    (mats[0], mats[2], mats[3], mats[5]) =
                        (mats[2], mats[3], mats[5], mats[0]);
                else if (axis == 'z')
                    (mats[0], mats[1], mats[3], mats[4]) =
                        (mats[4], mats[0], mats[1], mats[3]);
            }
            result.Add(new CubeData(newX, newY, newZ, mats, corners));
        }
        return result;
    }

    internal static List<CubeData> Mirror(List<CubeData> cubes, char axis)
    {
        var result = new List<CubeData>();

        int minX = cubes.Min(c => c.X), maxX = cubes.Max(c => c.X);
        int minY = cubes.Min(c => c.Y), maxY = cubes.Max(c => c.Y);
        int minZ = cubes.Min(c => c.Z), maxZ = cubes.Max(c => c.Z);

        double centerX = (minX + maxX) / 2.0;
        double centerY = (minY + maxY) / 2.0;
        double centerZ = (minZ + maxZ) / 2.0;

        foreach (var cube in cubes)
        {
            var mirrored = new CubeData(cube.X, cube.Y, cube.Z, cube.Materials, cube.Corners);

            if (axis == 'x')
                mirrored.X = (int)(2 * centerX - cube.X);
            else if (axis == 'y')
                mirrored.Y = (int)(2 * centerY - cube.Y);
            else if (axis == 'z')
                mirrored.Z = (int)(2 * centerZ - cube.Z);

            var corners = CornerBytesToCoords(mirrored.Corners);

            int[][] swaps = axis == 'x' ? new[] { new[] { 0, 3 }, new[] { 1, 2 }, new[] { 4, 7 }, new[] { 5, 6 } } :
                            axis == 'y' ? new[] { new[] { 0, 7 }, new[] { 1, 6 }, new[] { 2, 5 }, new[] { 3, 4 } } :
                                          new[] { new[] { 0, 1 }, new[] { 3, 2 }, new[] { 4, 5 }, new[] { 7, 6 } };

            foreach (var pair in swaps)
                (corners[pair[0]], corners[pair[1]]) = (corners[pair[1]], corners[pair[0]]);

            int axisIdx = axis == 'x' ? 0 : axis == 'y' ? 1 : 2;
            for (int i = 0; i < 8; i++)
            {
                var c = corners[i];
                if (axisIdx == 0) corners[i] = (4 - c.x, c.y, c.z);
                else if (axisIdx == 1) corners[i] = (c.x, 4 - c.y, c.z);
                else corners[i] = (c.x, c.y, 4 - c.z);
            }

            if (axis == 'x' || axis == 'z')
            {
                for (int i = 0; i < 8; i++)
                {
                    var c = corners[i];
                    corners[i] = (4 - c.x, c.y, 4 - c.z);
                }
                (corners[0], corners[2]) = (corners[2], corners[0]);
                (corners[1], corners[3]) = (corners[3], corners[1]);
                (corners[4], corners[6]) = (corners[6], corners[4]);
                (corners[5], corners[7]) = (corners[7], corners[5]);
            }

            mirrored.Corners = CoordsToCornerBytes(corners);

            var mats = (byte[])mirrored.Materials.Clone();
            if (axis == 'x')
                (mats[2], mats[5]) = (mats[5], mats[2]);
            else if (axis == 'y')
                (mats[0], mats[1]) = (mats[1], mats[0]);
            else
                (mats[3], mats[4]) = (mats[4], mats[3]);
            mirrored.Materials = mats;

            result.Add(mirrored);
        }

        return result;
    }

    internal static List<CubeData> Clip(List<CubeData> cubes, char axis, int min, int max)
    {
        int minX = cubes.Min(c => c.X);
        int maxX = cubes.Max(c => c.X);
        int minY = cubes.Min(c => c.Y);
        int maxY = cubes.Max(c => c.Y);
        int minZ = cubes.Min(c => c.Z);
        int maxZ = cubes.Max(c => c.Z);

        if (axis == 'x')
        {
            minX = min;
            maxX = max;
        }
        else if (axis == 'y')
        {
            minY = min;
            maxY = max;
        }
        else if (axis == 'z')
        {
            minZ = min;
            maxZ = max;
        }

        var result = new List<CubeData>();
        foreach (var cube in cubes)
        {
            if (cube.X >= minX && cube.X <= maxX &&
                cube.Y >= minY && cube.Y <= maxY &&
                cube.Z >= minZ && cube.Z <= maxZ)
            {
                result.Add(cube);
            }
        }

        return result;
    }

    public static (int x, int y, int z)[] CornerBytesToCoords(byte[] corners)
    {
        var coords = new (int, int, int)[8];
        for (int i = 0; i < 8; i++)
        {
            int v = corners[i];
            coords[i] = (v % 5, (v / 5) % 5, (v / 25) % 5);
        }
        return coords;
    }

    static byte[] CoordsToCornerBytes((int x, int y, int z)[] coords)
    {
        var bytes = new byte[8];
        for (int i = 0; i < 8; i++)
            bytes[i] = (byte)(coords[i].x + coords[i].y * 5 + coords[i].z * 25);
        return bytes;
    }
}
