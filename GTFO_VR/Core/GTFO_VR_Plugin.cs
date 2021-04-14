﻿using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System.Diagnostics;
using BepInEx.IL2CPP;
using System.Collections.Generic;
using UnhollowerRuntimeLib;
using GTFO_VR.UI;
using Mathf = SteamVR_Standalone_IL2CPP.Util.Mathf;
using GTFO_VR.Detours;
using GTFO_VR.Core.PlayerBehaviours;
using GTFO_VR.Core.VR_Input;
using GTFO_VR.Core.UI;
using BepInEx.Logging;

namespace GTFO_VR.Core
{
    /// <summary>
    /// Main entry point of the mod. Responsible for managing the config and running all patches if the mod is enabled.
    /// </summary>
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class GTFO_VR_Plugin : BasePlugin
    {
        public const string
            MODNAME = "GTFO_VR_Plugin",
            AUTHOR = "Spartan",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "0.8.1.1";


        public static GTFO_VR_Plugin Current;

        public override void Load()
        {
            Current = this;
            
            Core.Log.Setup(Logger.CreateLogSource(MODNAME));
            Core.Log.Info("Loading VR plugin...");
            SetupConfig();
            Harmony harmony = new Harmony("com.github.dsprtn.gtfovr");

            if (VRSettings.enabled && SteamVRRunningCheck())
            {
                InjectVR(harmony);
            }
            else
            {
                Log.LogWarning("VR launch aborted, VR is disabled or SteamVR is off!");
            }
        }

        private void InjectVR(Harmony harmony)
        {
            SetupIL2CPPClassInjections();
            TerminalInputDetours.HookAll();
            BioscannerDetours.HookAll();
            harmony.PatchAll();
        }

        void SetupIL2CPPClassInjections()
        {
            ClassInjector.RegisterTypeInIl2Cpp<VRAssets>();
            ClassInjector.RegisterTypeInIl2Cpp<VRSystems>();
            ClassInjector.RegisterTypeInIl2Cpp<VRRendering>();
            ClassInjector.RegisterTypeInIl2Cpp<CollisionFade>();
            ClassInjector.RegisterTypeInIl2Cpp<LaserPointer>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerOrigin>();
            ClassInjector.RegisterTypeInIl2Cpp<VRPlayer>();
            ClassInjector.RegisterTypeInIl2Cpp<Snapturn>();
            ClassInjector.RegisterTypeInIl2Cpp<VRKeyboard>();
            ClassInjector.RegisterTypeInIl2Cpp<DividedBarShaderController>();
            ClassInjector.RegisterTypeInIl2Cpp<VR_UI_Overlay>();
            ClassInjector.RegisterTypeInIl2Cpp<VRWorldSpaceUI>();
            ClassInjector.RegisterTypeInIl2Cpp<Watch>();
            ClassInjector.RegisterTypeInIl2Cpp<Controllers>();
            ClassInjector.RegisterTypeInIl2Cpp<HMD>();
        }

        private bool SteamVRRunningCheck()
        {
            if (!VRSettings.toggleVRBySteamVRRunning)
            {
                return true;
            }

            List<Process> possibleVRProcesses = new List<Process>();

            possibleVRProcesses.AddRange(Process.GetProcessesByName("vrserver"));
            possibleVRProcesses.AddRange(Process.GetProcessesByName("vrcompositor"));

            Core.Log.Debug("VR processes found - " + possibleVRProcesses.Count);
            foreach (Process p in possibleVRProcesses)
            {
                Core.Log.Debug(p.ToString());
            }
            return possibleVRProcesses.Count > 0;
        }

        public ConfigEntry<bool> configEnableVR;
        public ConfigEntry<bool> configToggleVRBySteamVR;
        public ConfigEntry<bool> configUseControllers;
        public ConfigEntry<bool> configIRLCrouch;
        public ConfigEntry<bool> configUseLeftHand;
        public ConfigEntry<int> configLightResMode;
        public ConfigEntry<bool> configAlternateEyeRendering;
        public ConfigEntry<bool> configUseTwoHanded;
        public ConfigEntry<bool> configAlwaysDoubleHanded;
        public ConfigEntry<float> configSnapTurnAmount;
        public ConfigEntry<bool> configSmoothSnapTurn;
        public ConfigEntry<float> configWatchScaling;
        public ConfigEntry<bool> configUseNumbersForAmmoDisplay;
        public ConfigEntry<string> configWatchColorHex;
        public ConfigEntry<float> configCrouchHeight;

        private void SetupConfig()
        {
            configEnableVR = Config.Bind("Startup", "Run VR plugin?", true, "If true, game will start in VR");
            configToggleVRBySteamVR = Config.Bind("Startup", "Start in pancake if SteamVR is off?", true, "If true, will start the game in pancake mode if SteamVR is not detected");

            configUseControllers = Config.Bind("Input", "Use VR Controllers?", true, "If true, will use VR controllers. You can play with a gamepad and head aiming if you set this to false");
            configIRLCrouch = Config.Bind("Input", "Crouch in-game when you crouch IRL?", true, "If true, when crouching down below a certain threshold IRL, the in-game character will also crouch");
            configUseLeftHand = Config.Bind("Input", "Use left hand as main hand?", false, "If true, all items will appear in the left hand");
            configLightResMode = Config.Bind("Experimental performance tweaks", "Light render resolution tweak - the lower resolution the greater the performance gain!", 1, "0 = Native HMD resolution 1 = 1920x1080, 2 = 1024x768 (Small artifacting, big performance increase), \n 3=640x480 (medium artifacting on lights, great performance increase)");
            configUseTwoHanded = Config.Bind("Input", "Use both hands to aim?", true, "If true, two-handed weapons will be allowed to be aimed with both hands.");
            configAlwaysDoubleHanded = Config.Bind("Input", "Always use double handed aiming? (Where it applies)", false, "If true, double handed weapons will always use double handed aiming (RECOMMENDED FOR GUN STOCK USERS)");
            configSnapTurnAmount = Config.Bind("Input", "Snap turn angle", 60f, "The amount of degrees to turn on a snap turn (or turn per half a second if smooth turn is enabled)");
            configSmoothSnapTurn = Config.Bind("Input", "Use smooth turning?", false, "If true, will use smooth turn instead of snap turn");
            configWatchScaling = Config.Bind("Misc", "Watch scale multiplier", 1.00f, "Size of the watch in-game will be multiplied by this value down to half of its default size or up to double (0.5 or 2.0)");
            configUseNumbersForAmmoDisplay = Config.Bind("Misc", "Use numbers for ammo display?", false, "If true, current ammo and max ammo will be displayed as numbers on the watch");
            configWatchColorHex = Config.Bind("Misc", "Hex color to use for watch", "#ffffff", "Google hexcolor and paste whatever color you want here");
            configCrouchHeight = Config.Bind("Input", "Crouch height in meters", 1.15f, "In-game character will be crouching if your head is lower than this height above the playspace (clamped to 1-1.35m)");
            configAlternateEyeRendering = Config.Bind("Experimental performance tweaks", "Alternate light and shadow rendering per frame per eye", false, "If true will alternate between eyes when drawing lights and shadows each frame, \n might look really janky so only use this if you absolutely want to play this in VR but don't have the rig for it!");
            
            

            Core.Log.Debug("VR enabled?" + configEnableVR.Value);
            Core.Log.Debug("Toggle VR by SteamVR running?" + configToggleVRBySteamVR.Value);
            Core.Log.Debug("Use VR Controllers? : " + configUseControllers.Value);
            Core.Log.Debug("Crouch on IRL crouch? : " + configIRLCrouch.Value);
            Core.Log.Debug("Use left hand as main hand? : " + configUseLeftHand.Value);
            Core.Log.Debug("Light resolution mode: " + configLightResMode.Value.ToString());
            Core.Log.Debug("Use two handed aiming: " + configUseTwoHanded.Value);
            Core.Log.Debug("Start with double handed aiming: " + configAlwaysDoubleHanded.Value);
            Core.Log.Debug("Snapturn amount: " + configSnapTurnAmount.Value);
            Core.Log.Debug("Use smooth turn?: " + configSmoothSnapTurn.Value);
            Core.Log.Debug("Watch size multiplier: " + configWatchScaling.Value);
            Core.Log.Debug("Use numbers for number display?: " + configUseNumbersForAmmoDisplay.Value);
            Core.Log.Debug("Watch color - " + configWatchColorHex.Value);
            Core.Log.Debug("Crouching height - " + configCrouchHeight.Value);
            Core.Log.Debug("Alternate eye rendering? - " + configAlternateEyeRendering.Value);

            VRSettings.useVRControllers = configUseControllers.Value;
            VRSettings.crouchOnIRLCrouch = configIRLCrouch.Value;
            VRSettings.lightRenderMode = configLightResMode.Value;
            VRSettings.twoHandedAimingEnabled = configUseTwoHanded.Value;
            VRSettings.alwaysDoubleHanded = configAlwaysDoubleHanded.Value;
            VRSettings.snapTurnAmount = configSnapTurnAmount.Value;
            VRSettings.useSmoothTurn = configSmoothSnapTurn.Value;
            VRSettings.watchScale = Mathf.Clamp(configWatchScaling.Value, 0.5f, 2f);
            VRSettings.toggleVRBySteamVRRunning = configToggleVRBySteamVR.Value;
            VRSettings.useNumbersForAmmoDisplay = configUseNumbersForAmmoDisplay.Value;
            VRSettings.watchColor = ColorExt.Hex(configWatchColorHex.Value);
            VRSettings.IRLCrouchBorder = Mathf.Clamp(configCrouchHeight.Value, 1f, 1.35f);
            VRSettings.alternateLightRenderingPerEye = configAlternateEyeRendering.Value;

            if (configUseLeftHand.Value)
            {
                VRSettings.mainHand = HandType.Left;
            }
        }

    }
}