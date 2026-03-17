
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio.Generating.Models
{
    internal class ModelLoader
    {
        private static readonly string ModelsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KogamaStudio",
                "Generate",
                "Models"
            );

        internal static List<ModelCubeData> LoadModel(string path)
        {
            try
            {
                string filePath = Path.Combine(ModelsPath, path);

                if (!File.Exists(filePath))
                {
                    KogamaStudio.Log.LogError($"[ModelLoader] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                JObject jObject = JObject.Parse(json);

                var cubes = new List<ModelCubeData>();
                JArray cubesArray = jObject["cubes"] as JArray;
                
                if (cubesArray == null )
                {
                    return null;
                }

                foreach (var cubeObj in cubesArray)
                {
                    int x = cubeObj["x"].Value<int>();
                    int y = cubeObj["y"].Value<int>();
                    int z = cubeObj["z"].Value<int>();

                    byte[] materials = null;
                    byte[] corners = null;

                    if (cubeObj["materials"] != null)
                    {
                        materials = cubeObj["materials"].ToObject<byte[]>();
                    }

                    if (cubeObj["corners"] != null)
                    {
                        corners = cubeObj["corners"].ToObject<byte[]>();
                    }

                    cubes.Add(new ModelCubeData(x, y, z, materials, corners));
                }

                return cubes;
            }
            catch (Exception ex )
            {
                KogamaStudio.Log.LogError($"[ModelLoader] Error loading model: {ex.Message}");
                return null;
            }
        }
    }
}
