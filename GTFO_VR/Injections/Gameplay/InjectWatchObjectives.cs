﻿using GameData;
using GTFO_VR.Core;
using GTFO_VR.UI;
using HarmonyLib;
using LevelGeneration;


namespace GTFO_VR.Injections
{
    /// <summary>
    /// Replicate new objectives on the VR watch
    /// </summary>


    [HarmonyPatch(typeof(PlayerGuiLayer), nameof(PlayerGuiLayer.UpdateObjectives))]
    class InjectWatchObjectives
    {
        static void Postfix(LG_LayerType layer,
    string mainObjective,
    WardenObjectiveDataBlock data,
    eWardenSubObjectiveStatus sub,
    bool visible = true,
    bool isAdditionalHelp = false)
        {
            Watch.Current?.UpdateMainObjective(mainObjective);
            Log.Debug($"Got new objective! - {mainObjective}");
        }
    }


    [HarmonyPatch(typeof(PUI_GameObjectives), nameof(PUI_GameObjectives.SetSubObjective))]
    class InjectWatchSubObjectives
    {
        static void Postfix(PUI_GameObjectives __instance)
        {
            Watch.Current?.UpdateSubObjective(__instance.m_subObjective.text);
            Log.Debug($"Got new subobjective! - {__instance.m_subObjective.text}");
        }
    }
}