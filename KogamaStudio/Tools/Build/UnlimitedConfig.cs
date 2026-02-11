using HarmonyLib;
using Il2Cpp;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace KogamaStudio.Tools.Build;

[HarmonyPatch]
internal static class UnlimitedConfig
{
    internal static bool Enabled = false;
    internal static bool ClampValues = true;
    internal static float MinValue = 0;
    internal static float MaxValue = 1;

    [HarmonyPatch(typeof(SettingsSlider), "Initialize", new Type[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
    [HarmonyPrefix]
    private static void Initialize_Float(ref float value, ref float minValue, ref float maxValue)
    {
        if (Enabled)
        {
            minValue = MinValue;
            maxValue = MaxValue;
            if (ClampValues)
                value = Mathf.Clamp(value, MinValue, MaxValue);
        }
    }

    [HarmonyPatch(typeof(SettingsSlider), "Initialize", new Type[] { typeof(string), typeof(int), typeof(int), typeof(int) })]
    [HarmonyPrefix]
    private static void Initialize_Int(ref int value, ref int minValue, ref int maxValue)
    {
        if (Enabled)
        {
            minValue = (int)MinValue;
            maxValue = (int)MaxValue;
            if (ClampValues)
                value = (int)Mathf.Clamp(value, MinValue, MaxValue);
        }
    }

    [HarmonyPatch(typeof(SettingsSlider), "ValueChanged")]
    [HarmonyPostfix]
    private static void ValueChanged_Postfix(SettingsSlider __instance)
    {
        if (!Enabled) return;

        var sliderField = __instance.slider;
        if (sliderField == null) return;

        sliderField.minValue = MinValue;
        sliderField.maxValue = MaxValue;

        if (ClampValues)
        {
            sliderField.value = Mathf.Clamp(sliderField.value, MinValue, MaxValue);
        }
    }

    [HarmonyPatch(typeof(SettingsInputFieldSlider), "SliderValueChanged")]
    [HarmonyPostfix]
    private static void InputFieldSlider_SliderChanged(SettingsInputFieldSlider __instance)
    {
        if (!Enabled) return;

        var internalSlider = __instance.settingsSlider;
        if (internalSlider?.slider == null) return;

        internalSlider.slider.minValue = MinValue;
        internalSlider.slider.maxValue = MaxValue;
    }

    [HarmonyPatch(typeof(SettingsInputFieldSlider), "InputFieldValueChanged")]
    [HarmonyPostfix]
    private static void InputFieldSlider_TextChanged(SettingsInputFieldSlider __instance)
    {
        if (!Enabled) return;

        var internalSlider = __instance.settingsSlider;
        if (internalSlider?.slider == null) return;

        internalSlider.slider.minValue = MinValue;
        internalSlider.slider.maxValue = MaxValue;

        if (ClampValues)
        {
            internalSlider.slider.value = Mathf.Clamp(internalSlider.slider.value, MinValue, MaxValue);
        }
    }

    [HarmonyPatch(typeof(SettingsInputFieldSlider), "Initialize", new Type[] { typeof(string), typeof(float) })]
    [HarmonyPostfix]
    private static void InputFieldSlider_InitFloat(SettingsInputFieldSlider __instance)
    {
        if (!Enabled) return;

        var internalSlider = __instance.settingsSlider;
        if (internalSlider?.slider == null) return;

        internalSlider.slider.minValue = MinValue;
        internalSlider.slider.maxValue = MaxValue;
    }

    [HarmonyPatch(typeof(SettingsInputFieldSlider), "Initialize", new Type[] { typeof(string), typeof(int) })]
    [HarmonyPostfix]
    private static void InputFieldSlider_InitInt(SettingsInputFieldSlider __instance)
    {
        if (!Enabled) return;

        var internalSlider = __instance.settingsSlider;
        if (internalSlider?.slider == null) return;

        internalSlider.slider.minValue = MinValue;
        internalSlider.slider.maxValue = MaxValue;
    }
}