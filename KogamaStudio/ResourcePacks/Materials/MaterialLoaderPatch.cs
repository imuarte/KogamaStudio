using HarmonyLib;

using UnityEngine;

namespace KogamaStudio.ResourcePacks.Materials
{
    [HarmonyPatch(typeof(MaterialLoader), "SetMainTexture")]

    public class MaterialLoaderPatch
    {
        public static void Prefix(Texture texture)
        {
            //maybe i will do it in the future
        }
    }
}
