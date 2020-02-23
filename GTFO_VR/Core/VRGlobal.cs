﻿using GTFO_VR.Core;
using GTFO_VR.Events;
using GTFO_VR.Input;
using Player;
using System;
using System.Text;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

namespace GTFO_VR
{
    public class VRGlobal : MonoBehaviour
    {

        public static VRGlobal instance;

        public static bool VR_ENABLED;

        public static bool Overlay_Active = true;

        static GameObject ingamePlayer;

        VR_UI_Overlay overlay;

        static string currentFrameInput = "";

        public static bool keyboardClosedThisFrame;

        void Awake()
        {
            if(!instance)
            {
                instance = this;
            } else
            {
                Debug.LogError("Trying to create duplicate VRGlobal class");
                return;
            }
            // Prevent SteamVR from adding a tracking script automatically. We handle this manually in HMD
            SteamVR_Camera.useHeadTracking = false;
            SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).Listen(OnKeyboardInput);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardDone).Listen(OnKeyboardDone);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).Listen(OnKeyboardDone);
            FocusStateEvents.OnFocusStateChange += FocusChanged;
            Setup();
        }

        public void OnKeyboardDone(VREvent_t arg0)
        {
            keyboardClosedThisFrame = true;
        }

        private void OnKeyboardInput(VREvent_t ev)
        {
            VREvent_Keyboard_t keyboard = ev.data.keyboard;
            byte[] inputBytes = new byte[] { keyboard.cNewInput0, keyboard.cNewInput1, keyboard.cNewInput2, keyboard.cNewInput3, keyboard.cNewInput4, keyboard.cNewInput5, keyboard.cNewInput6, keyboard.cNewInput7 };
            int len = 0;
            for (; inputBytes[len] != 0 && len < 7; len++) ;
            string input = System.Text.Encoding.UTF8.GetString(inputBytes, 0, len);
            input = HandleSpecialConversionAndShortcuts(input);
           
            currentFrameInput = input;
        }

        

        public static string GetKeyboardInput()
        {
            return currentFrameInput;
        }

        void LateUpdate()
        {
            currentFrameInput = "";
            keyboardClosedThisFrame = false;
        }
        
        void Update()
        {
            DoDebugOnKeyDown();
        }

        private void DoDebugOnKeyDown()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                DebugHelper.LogScene();
            }

            if(UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                foreach(Camera cam in FindObjectsOfType<Camera>())
                {
                    Debug.Log(cam.gameObject + " --- Enabled: " + cam.enabled);
                }
            }
        }

        private void Setup()
        {
            SteamVR.Initialize(false);
            gameObject.AddComponent<VRInput>();
            Invoke("SetupOverlay", .25f);
            gameObject.AddComponent<HMD>();
            gameObject.AddComponent<Controllers>();
           
            DontDestroyOnLoad(gameObject);
        }

        void SetupOverlay()
        {
            overlay = new GameObject("Overlay").AddComponent<VR_UI_Overlay>();
        }

        public void FocusChanged(eFocusState state)
        {
            if (state.Equals(eFocusState.FPS) || state.Equals(eFocusState.InElevator))
            {
                HandleIngameFocus();
            }

            if(state.Equals(eFocusState.MainMenu) || state.Equals(eFocusState.Map)) {
                HandleOutOfGameFocus();
            }

            if(state.Equals(eFocusState.ComputerTerminal))
            {
                SteamVR_Render.unfocusedRenderResolution = 1f;
                SteamVR.instance.overlay.ShowKeyboard(0, 0, "Terminal input", 256, "", true, 0);

                OrientKeyboard();
            }
            else
            {
                SteamVR.instance.overlay.HideKeyboard();
                SteamVR_Render.unfocusedRenderResolution = .5f;
            }
        }

        private static void OrientKeyboard()
        {
            Quaternion Rot = Quaternion.Euler(Vector3.Project(HMD.hmd.transform.rotation.eulerAngles, Vector3.up));
            Vector3 Pos = HMD.hmd.transform.localPosition + Rot * Vector3.forward * 1f;
            Pos.y = HMD.hmd.transform.position.y + .5f;
            Rot = Quaternion.LookRotation(HMD.hmd.transform.forward);
            Rot = Quaternion.Euler(0f, Rot.eulerAngles.y, 0f);
            var t = new SteamVR_Utils.RigidTransform(Pos, Rot).ToHmdMatrix34();
            SteamVR.instance.overlay.SetKeyboardTransformAbsolute(ETrackingUniverseOrigin.TrackingUniverseStanding, ref t);
        }

        private void HandleOutOfGameFocus()
        {
            if(!overlay)
            {
                return;
            }

            ToggleOverlay(true);
            TogglePlayerCam(false);
        }

        private void HandleIngameFocus()
        {
            if(!overlay)
            {
                return;
            }
            if(ingamePlayer == null)
            {
                Debug.Log("Creating VR Player...");
                ingamePlayer = new GameObject();
                ingamePlayer.AddComponent<PlayerVR>();
                
            }
           
            ToggleOverlay(false);
            TogglePlayerCam(true);
        }

        void ToggleOverlay(bool toggle)
        {
            if(!toggle)
            {
                overlay.DestroyOverlay();
            } else
            {
                overlay.SetupOverlay();
            }
            
            overlay.gameObject.SetActive(toggle);
            overlay.OrientateOverlay();
        }

        void TogglePlayerCam(bool toggle)
        {
            PlayerVR.LoadedAndInGame = toggle;
            SteamVR_Render.pauseRendering = !toggle;
            Invoke("DisableUnneccessaryCams",.2f);

        }

        void DisableUnneccessaryCams()
        {
            if(PlayerVR.VRCamera && PlayerVR.VRCamera.head)
            {
                foreach (Camera cam in PlayerVR.VRCamera.transform.root.GetComponentsInChildren<Camera>())
                {
                    cam.enabled = false;
                }
            }
        }

        private string HandleSpecialConversionAndShortcuts(string input)
        {
            switch (input)
            {
                case ("\n"):
                    {
                        return "\r";
                    }
                case ("-"):
                    {
                        return "_";
                    }
                case ("L"):
                    {
                        return "LIST ";
                    }
                case ("Q"):
                    {
                        return "QUERY ";
                    }
                case ("R"):
                    {
                        return "REACTOR";
                    }
                case ("V"):
                    {
                        return "REACTOR_VERIFY ";
                    }
                case ("P"):
                    {
                        return "PING ";
                    }
                case ("A"):
                    {
                        return "AMMOPACK";
                    }
                case ("T"):
                    {
                        return "TOOL_REFILL";
                    }
                case ("M"):
                    {
                        return "MEDIPACK";
                    }
            }
            return input;
        }

        void OnDestroy()
        {
            FocusStateEvents.OnFocusStateChange -= FocusChanged;
        }
    }
}