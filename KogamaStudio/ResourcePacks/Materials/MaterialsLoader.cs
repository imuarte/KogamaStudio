
using UnityEngine;

namespace KogamaStudio.ResourcePacks.Materials
{
    public class MaterialsLoader
    {
        public static void LoadTexture(string materialsDir)
        {
            // Get current texture array to know expected count and size
            var currentTex = DefaultMaterials.defaultMaterials;
            if (currentTex == null)
            {
                KogamaStudio.Log.LogError("[MaterialsLoader] Default materials not captured yet");
                return;
            }

            var currentArr = currentTex.Cast<Texture2DArray>();
            int sliceCount = currentArr.depth;
            int texWidth = currentArr.width;
            int texHeight = currentArr.height;

            KogamaStudio.Log.LogInfo($"[MaterialsLoader] Expected: {sliceCount} slices, {texWidth}x{texHeight}");

            // Load custom textures, falling back to default for missing ones
            var textures = new Texture2D[sliceCount];
            int loaded = 0;

            for (int i = 0; i < sliceCount; i++)
            {
                string path = Path.Combine(materialsDir, $"{i}.png");
                if (File.Exists(path))
                {
                    byte[] imageData = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(imageData);

                    // Resize if dimensions don't match
                    if (tex.width != texWidth || tex.height != texHeight)
                    {
                        var rt = RenderTexture.GetTemporary(texWidth, texHeight);
                        Graphics.Blit(tex, rt);
                        var resized = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        resized.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                        resized.Apply();
                        RenderTexture.active = prev;
                        RenderTexture.ReleaseTemporary(rt);
                        UnityEngine.Object.Destroy(tex);
                        tex = resized;
                    }

                    textures[i] = tex;
                    loaded++;
                }
                else
                {
                    textures[i] = null; // will copy from default
                }
            }

            if (loaded == 0)
            {
                KogamaStudio.Log.LogWarning("[MaterialsLoader] No textures found in pack");
                return;
            }

            var textureArray = new Texture2DArray(texWidth, texHeight, sliceCount, TextureFormat.RGBA32, false);

            for (int i = 0; i < sliceCount; i++)
            {
                if (textures[i] != null)
                {
                    Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
                    UnityEngine.Object.Destroy(textures[i]);
                }
                else
                {
                    // Copy slice from default texture array
                    Graphics.CopyTexture(currentArr, i, 0, textureArray, i, 0);
                }
            }

            MVGameControllerBase.MaterialLoader.SetMainTexture(textureArray, true);
            KogamaStudio.Log.LogInfo($"[MaterialsLoader] Loaded {loaded}/{sliceCount} custom textures");
        }
    }
}
