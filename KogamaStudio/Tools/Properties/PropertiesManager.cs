using Il2Cpp;
using KogamaModFramework.Operations;
using KogamaStudio.Clipboard;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KogamaStudio.Tools.Properties;

internal class PropertiesManager
{
    internal static int SelectedWOId = -1;
    internal static void SendProperties(int WOId)
    {
        var wo = MVGameControllerBase.WOCM.GetWorldObjectClient(WOId);
        if (wo == null) return;

        if (SelectedWOId != WOId)
        {
            ClipboardManager.Preview = false;
            ClipboardManager.ShowPreview();
            PipeClient.SendCommand("clipboard_preview_paste_model|false");
        }

        SelectedWOId = WOId;

        Vector3 euler = wo.Rotation.eulerAngles;
        CommandHandler.currentEulerAngles = euler;

        // ID
        PipeClient.SendCommand($"properties_object_id|{WOId}");
        // POS
        PipeClient.SendCommand($"properties_position_x|{wo.Position.x}");
        PipeClient.SendCommand($"properties_position_y|{wo.Position.y}");
        PipeClient.SendCommand($"properties_position_z|{wo.Position.z}");
        // ROT
        PipeClient.SendCommand($"properties_rotation_x|{euler.x}");
        PipeClient.SendCommand($"properties_rotation_y|{euler.y}");
        PipeClient.SendCommand($"properties_rotation_z|{euler.z}");

        // MODEL
        ModelProperties.SendProperties(wo);
    }
}
