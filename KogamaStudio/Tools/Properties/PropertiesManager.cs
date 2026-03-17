using KogamaModFramework.Operations;
using KogamaStudio.Clipboard;

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

        SelectedWOId = WOId;

        var euler = wo.Rotation.eulerAngles;
        CommandHandler.currentEulerAngles = euler;

        var model     = ModelProperties.GetModel(wo);
        bool isModel  = model != null;
        int  protoId  = isModel ? model.Pid : -1;
        ModelProperties.CubeModelBase = model;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        PipeClient.SendCommand(
            $"properties_update" +
            $"|{WOId}|{(int)wo.ItemId}|{wo.GroupId}" +
            $"|{wo.Position.x.ToString(inv)}|{wo.Position.y.ToString(inv)}|{wo.Position.z.ToString(inv)}" +
            $"|{euler.x.ToString(inv)}|{euler.y.ToString(inv)}|{euler.z.ToString(inv)}" +
            $"|{(isModel ? "1" : "0")}|{protoId}");
    }
}
