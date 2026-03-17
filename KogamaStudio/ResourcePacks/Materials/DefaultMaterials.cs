using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

using UnityEngine;

namespace KogamaStudio.ResourcePacks.Materials
{
    [HarmonyPatch(typeof(MaterialLoader), "SetMainTexture")]
    public class DefaultMaterials
    {
        public static Texture defaultMaterials;
        public static void Prefix(Texture texture, bool isGameModeInitialized)
        {
            if (texture == null) return;

            if (!isGameModeInitialized) return;

            if (defaultMaterials != null) return;

            defaultMaterials = texture;
        }
    }
}
