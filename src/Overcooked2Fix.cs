using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Overcooked2Fix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class O2Fix : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public static ConfigEntry<bool> CustomResolution;
        public static ConfigEntry<float> DesiredResolutionX;
        public static ConfigEntry<float> DesiredResolutionY;
        public static ConfigEntry<float> PhysicsUpdateRate;
        public static ConfigEntry<bool> UIFix;

        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            CustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                true,
                                "Enable the usage of a custom resolution.");

            DesiredResolutionX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default
                                "Set desired resolution width.");

            DesiredResolutionY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default
                                "Set desired resolution height.");

            PhysicsUpdateRate = Config.Bind("Physics Update Rate",
                                "UpdateRate",
                                (float)50, 
                                "Set desired physics update rate. (You can try raising this to improve physics smoothness.)");

            if (CustomResolution.Value) { Harmony.CreateAndPatchAll(typeof(CustomResolution)); }

            if (PhysicsUpdateRate.Value > 50) 
            {
                Harmony.CreateAndPatchAll(typeof(FixedDeltaTime));
                Time.fixedDeltaTime = (float)1 / PhysicsUpdateRate.Value;
                O2Fix.Log.LogInfo($"fixedDeltaTime set to {Time.fixedDeltaTime}");
            }
        }
    }

    [HarmonyPatch]
    public class CustomResolution
    {
        // Stop rect being adjusted to 16:9
        [HarmonyPatch(typeof(FixedAspectRatio), "ComputeAspectRatioRect")]
        [HarmonyPostfix]
        public static void FixAR(ref Rect __result)
        {
            __result = new Rect(0f, 0f, 1f, 1f);
            O2Fix.Log.LogInfo($"ComputeAspectRatioRect patched.");
        }

        // Add custom resolution to list
        [HarmonyPatch(typeof(Resolution), "GetResolutions")]
        [HarmonyPostfix]
        public static void AddResolution(ref UnityEngine.Resolution[] __result)
        {
            UnityEngine.Resolution customResolution = new UnityEngine.Resolution
            {
                width = (int)O2Fix.DesiredResolutionX.Value,
                height = (int)O2Fix.DesiredResolutionY.Value,
                refreshRate = 0 // 0 = use highest available
            };

            List<UnityEngine.Resolution> resolutions = __result.ToList();
            resolutions.Add(customResolution);
            __result = resolutions.ToArray();
            O2Fix.Log.LogInfo($"{customResolution} added to resolution list.");
        }

        // Set screen match mode when object has CanvasScaler enabled
        [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
        [HarmonyPostfix]
        public static void SetScreenMatchMode(CanvasScaler __instance)
        {
            __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
    }

    public class FixedDeltaTime
    {
        // Don't let it set a fixedDeltaTime
        [HarmonyPatch(typeof(GameDebugManager), "Update")]
        [HarmonyPrefix]
        public static bool NerfFixedDeltaTime(GameDebugManager __instance)
        {
            return false;
        }
    }
}
