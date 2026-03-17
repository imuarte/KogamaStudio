
using UnityEngine;
using System.Reflection;
using System.ComponentModel;

namespace KogamaStudio.ResourcePacks.Materials
{
    public class MaterialsLoader
    {
        public static void LoadTexture(List<string> paths)
        {

            List<Texture2D> textures = new List<Texture2D>();

            foreach (string path in paths)
            {
                if (!File.Exists(path)) continue;
                byte[] imageData = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texture.LoadImage(imageData);
                textures.Add(texture);
            }

            Texture2DArray textureArray = new Texture2DArray(textures[0].width, textures[0].height, textures.Count, TextureFormat.ARGB32, false);

            for (int i = 0; i < textures.Count; i++)
            {
                Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
                UnityEngine.Object.Destroy(textures[i]);
            }

            MVGameControllerBase.MaterialLoader.SetMainTexture( textureArray, true );
        }
    }
}   