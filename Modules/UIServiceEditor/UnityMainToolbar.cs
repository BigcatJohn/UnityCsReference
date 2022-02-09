// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Connect;
using UnityEditor.EditorTools;
using UnityEditor.PackageManager.UI;
using System.Linq;

namespace UnityEditor
{
    [MainToolbar("Default")]
    class UnityMainToolbar : EditorToolbar
    {
        private const int k_MinWidthChangePreviewPackageInUseToIcon = 1100;

        [NonSerialized]
        private bool m_IsPreviewPackagesInUse;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private ApplicationProxy m_ApplicationProxy;

        static void InitializeIcons()
        {
            s_LayerContent = EditorGUIUtility.TrTextContent("Layers", "Which layers are visible in the Scene views.");

            s_PlayIcons = new GUIContent[]
            {
                EditorGUIUtility.TrIconContent("PlayButton", "Play"),
                EditorGUIUtility.TrIconContent("PauseButton", "Pause"),
                EditorGUIUtility.TrIconContent("StepButton", "Step"),
                EditorGUIUtility.TrIconContent("PlayButtonProfile", "Profiler Play"),
                EditorGUIUtility.IconContent("PlayButton On"),
                EditorGUIUtility.IconContent("PauseButton On"),
                EditorGUIUtility.IconContent("StepButton On"),
                EditorGUIUtility.IconContent("PlayButtonProfile On")
            };

            s_PreviewPackageContent = EditorGUIUtility.TrTextContent("Preview Packages in Use");
            s_PreviewPackageIcon = EditorGUIUtility.TrIconContent("PreviewPackageInUse", "Preview Packages in Use");

            s_CloudIcon = EditorGUIUtility.TrIconContent("CloudConnect", "Manage services");
            s_AccountContent = EditorGUIUtility.TrTextContent("Account", "Account profile");
        }

        static GUIContent   s_LayerContent;
        static GUIContent[] s_PlayIcons;
        static GUIContent s_PreviewPackageContent;
        static GUIContent s_PreviewPackageIcon;

        static GUIContent s_AccountContent;
        static GUIContent   s_CloudIcon;
        static class Styles
        {
            public static readonly GUIStyle dropdown = "Dropdown";
            public static readonly GUIStyle previewPackageInUseDropdown = "PreviewPackageInUse";
            public static readonly GUIStyle appToolbar = "AppToolbar";
            public static readonly GUIStyle command = "AppCommand";
            public static readonly GUIStyle buttonLeft = "AppToolbarButtonLeft";
            public static readonly GUIStyle buttonRight = "AppToolbarButtonRight";
            public static readonly GUIStyle commandLeft = "AppCommandLeft";
            public static readonly GUIStyle commandLeftOn = "AppCommandLeftOn";
            public static readonly GUIStyle commandMid = "AppCommandMid";
            public static readonly GUIStyle commandRight = "AppCommandRight";

            public static GUIContent[] snapToGridIcons = new GUIContent[]
            {
                EditorGUIUtility.TrIconContent("SceneViewSnap-Off", "Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global."),
                EditorGUIUtility.TrIconContent("SceneViewSnap-On", "Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global.")
            };
        }

        protected void OnEnable()
        {
            EditorApplication.modifierKeysChanged += Repaint;
            // when undo or redo is done, we need to reset global tools rotation
            Undo.undoRedoPerformed += OnSelectionChange;
            UnityConnect.instance.StateChanged += OnUnityConnectStateChanged;
            m_IsPreviewPackagesInUse = PackageManager.PackageInfo.GetAll().FirstOrDefault(info =>
            {
                var versionSplitOnTag = info.version.Split('-');
                return info.registry?.isDefault == true &&
                ((versionSplitOnTag.Length > 1 && (!string.IsNullOrEmpty(versionSplitOnTag[1])))
                    || info.version.StartsWith("0."));
            }) != null;
            m_PackageManagerPrefs = ServicesContainer.instance.Resolve<PackageManagerPrefs>();
            m_ApplicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
        }

        protected void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= Repaint;
            Undo.undoRedoPerformed -= OnSelectionChange;
            UnityConnect.instance.StateChanged -= OnUnityConnectStateChanged;
        }

        public static Toolbar get = null;

        static List<SubToolbar> s_SubToolbars = new List<SubToolbar>();

        void OnSelectionChange()
        {
            Tools.OnSelectionChange();
            Repaint();
        }

        protected void OnUnityConnectStateChanged(ConnectInfo state)
        {
            RepaintToolbar();
        }


        void ReserveWidthLeft(float width, ref Rect pos)
        {
            pos.x -= width;
            pos.width = width;
        }

        void ReserveWidthRight(float width, ref Rect pos)
        {
            pos.x += pos.width;
            pos.width = width;
        }

        public override void OnGUI()
        {
            const float space = 8;
            const float standardButtonWidth = 32;
            const float dropdownWidth = 80;
            const float playPauseStopWidth = 140;
            const float previewPackagesinUseWidth = 173;
            const float previewPackagesinUseIconWidth = 45;

            InitializeIcons();

            bool isOrWillEnterPlaymode = EditorApplication.isPlayingOrWillChangePlaymode;
            GUI.color = isOrWillEnterPlaymode ? HostView.kPlayModeDarken : Color.white;

            if (Event.current.type == EventType.Repaint)
                Styles.appToolbar.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            // Position left aligned controls controls - start from left to right.
            Rect pos = new Rect(0, 0, 0, 0);
            ReserveWidthRight(space, ref pos);

            // pos = EditorToolGUI.DoToolContextButton(EditorToolGUI.GetToolbarEntryRect(pos));

            ReserveWidthRight(standardButtonWidth * EditorToolGUI.k_ToolbarButtonCount, ref pos);
            EditorToolGUI.DoBuiltinToolbar(EditorToolGUI.GetToolbarEntryRect(pos));

            ReserveWidthRight(space, ref pos);

            pos.x += pos.width;
            const float pivotButtonsWidth = 128;
            pos.width = pivotButtonsWidth;
            DoToolSettings(EditorToolGUI.GetToolbarEntryRect(pos));

            ReserveWidthRight(space, ref pos);
            ReserveWidthRight(standardButtonWidth, ref pos);
            DoSnapButtons(EditorToolGUI.GetToolbarEntryRect(pos));

            // Position centered controls.
            int playModeControlsStart = Mathf.RoundToInt((position.width - playPauseStopWidth) / 2);
            pos = new Rect(playModeControlsStart, 0, 240, 0);

            if (ModeService.HasCapability(ModeCapability.Playbar, true))
            {
                GUILayout.BeginArea(EditorToolGUI.GetToolbarEntryRect(pos));
                GUILayout.BeginHorizontal();
                {
                    if (!ModeService.Execute("gui_playbar", isOrWillEnterPlaymode))
                        DoPlayButtons(isOrWillEnterPlaymode);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            // Position right aligned controls controls - start from right to left.
            pos = new Rect(position.width, 0, 0, 0);

            // Right spacing side
            if (ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
            {
                ReserveWidthLeft(space, ref pos);
                ReserveWidthLeft(dropdownWidth + 30, ref pos);
                DoLayoutDropDown(EditorToolGUI.GetToolbarEntryRect(pos));
            }

            if (ModeService.HasCapability(ModeCapability.Layers, true))
            {
                ReserveWidthLeft(space, ref pos);
                ReserveWidthLeft(dropdownWidth, ref pos);
                DoLayersDropDown(EditorToolGUI.GetToolbarEntryRect(pos));
            }

            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Master)
            {
                ReserveWidthLeft(space, ref pos);

                ReserveWidthLeft(dropdownWidth, ref pos);
                if (EditorGUI.DropdownButton(EditorToolGUI.GetToolbarEntryRect(pos), s_AccountContent, FocusType.Passive, Styles.dropdown))
                {
                    ShowUserMenu(EditorToolGUI.GetToolbarEntryRect(pos), true);
                }

                ReserveWidthLeft(space, ref pos);

                ReserveWidthLeft(standardButtonWidth, ref pos);
                if (GUI.Button(EditorToolGUI.GetToolbarEntryRect(pos), s_CloudIcon, Styles.command))
                    ServicesEditorWindow.ShowServicesWindow("cloud_icon");
            }

            foreach (SubToolbar subToolbar in s_SubToolbars)
            {
                ReserveWidthLeft(space, ref pos);
                ReserveWidthLeft(subToolbar.Width, ref pos);
                subToolbar.OnGUI(EditorToolGUI.GetToolbarEntryRect(pos));
            }

            if (Unsupported.IsDeveloperBuild() && ModeService.hasSwitchableModes)
            {
                EditorGUI.BeginChangeCheck();
                ReserveWidthLeft(space, ref pos);
                ReserveWidthLeft(dropdownWidth, ref pos);
                var selectedModeIndex = EditorGUI.Popup(EditorToolGUI.GetToolbarEntryRect(pos), ModeService.currentIndex, ModeService.modeNames, Styles.dropdown);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorApplication.delayCall += () => ModeService.ChangeModeByIndex(selectedModeIndex);
                    GUIUtility.ExitGUI();
                }
            }

            if (m_IsPreviewPackagesInUse && !m_PackageManagerPrefs.dismissPreviewPackagesInUse)
            {
                ReserveWidthLeft(space, ref pos);

                var useIcon = Toolbar.get.mainToolbar.position.width < k_MinWidthChangePreviewPackageInUseToIcon;
                ReserveWidthLeft(useIcon ? previewPackagesinUseIconWidth : previewPackagesinUseWidth, ref pos);

                var dropDownCustomColor = new GUIStyle(Styles.previewPackageInUseDropdown);

                if (EditorGUI.DropdownButton(EditorToolGUI.GetToolbarEntryRect(pos), useIcon ? s_PreviewPackageIcon : s_PreviewPackageContent, FocusType.Passive, dropDownCustomColor))
                    ShowPreviewPackageInUseMenu(EditorToolGUI.GetToolbarEntryRect(pos));
            }

            EditorGUI.ShowRepaints();
        }

        void ShowPreviewPackageInUseMenu(Rect rect)
        {
            var menu = new GenericMenu();

            // Here hide the button : what do for now mean, reappear after opening unity
            menu.AddItem(EditorGUIUtility.TrTextContent("Dismiss for now"), false, () => m_PackageManagerPrefs.dismissPreviewPackagesInUse = true);
            menu.AddSeparator("");

            // Here we open the package manager, In-Project open and search field have preview.
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Preview Packages..."), false, () =>
            {
                PackageManagerWindow.SelectPackageAndFilterStatic(string.Empty, PackageFilterTab.InProject, true, "preview");
            });
            menu.AddSeparator("");

            // Here we go to the link explaining why we see this...
            menu.AddItem(EditorGUIUtility.TrTextContent("Why am I seeing this?"), false, () =>
            {
                m_ApplicationProxy.OpenURL($"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-preview.html");
            });

            menu.DropDown(rect, true);
        }

        void ShowUserMenu(Rect dropDownRect, bool shouldDiscardMenuOnSecondClick = false)
        {
            var menu = new GenericMenu();
            if (!UnityConnect.instance.online || UnityConnect.instance.isDisableUserLogin)
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Sign in..."));

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"));
                }
            }
            else
            {
                string accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPortal);
                if (UnityConnect.instance.loggedIn)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Go to account"), false, () => UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));

                if (UnityConnect.instance.loggedIn)
                {
                    string name = "Sign out " + UnityConnect.instance.userInfo.displayName;
                    menu.AddItem(new GUIContent(name), false, () => { UnityConnect.instance.Logout(); });
                }
                else
                    menu.AddItem(EditorGUIUtility.TrTextContent("Sign in..."), false, () => { UnityConnect.instance.ShowLogin(); });

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    // Debug.log()
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"), false, () => Application.OpenURL("https://store.unity.com/"));
                }
            }

            menu.DropDown(dropDownRect, shouldDiscardMenuOnSecondClick);
        }


        void DoToolSettings(Rect rect)
        {
            rect = EditorToolGUI.GetToolbarEntryRect(rect);
            EditorToolGUI.DoBuiltinToolSettings(rect, Styles.buttonLeft, Styles.buttonRight);
        }

        void DoSnapButtons(Rect rect)
        {
            using (new EditorGUI.DisabledScope(!EditorSnapSettings.activeToolSupportsGridSnap))
            {
                var snap = EditorSnapSettings.gridSnapEnabled;
                var icon = snap ? Styles.snapToGridIcons[1] : Styles.snapToGridIcons[0];
                rect = EditorToolGUI.GetToolbarEntryRect(rect);
                EditorSnapSettings.gridSnapEnabled = GUI.Toggle(rect, snap, icon, Styles.command);
            }
        }

        void DoPlayButtons(bool isOrWillEnterPlaymode)
        {
            // Enter / Exit Playmode
            bool isPlaying = EditorApplication.isPlaying;
            GUI.changed = false;

            int buttonOffset = isPlaying ? 4 : 0;

            Color c = GUI.color + new Color(.01f, .01f, .01f, .01f);
            GUI.contentColor = new Color(1.0f / c.r, 1.0f / c.g, 1.0f / c.g, 1.0f / c.a);
            GUI.SetNextControlName("ToolbarPlayModePlayButton");
            GUILayout.Toggle(isOrWillEnterPlaymode, s_PlayIcons[buttonOffset], isPlaying ? Styles.commandLeftOn : Styles.commandLeft);
            GUI.backgroundColor = Color.white;
            if (GUI.changed)
            {
                EditorApplication.TogglePlaying();
                GUIUtility.ExitGUI();
            }

            // Pause game
            GUI.changed = false;

            buttonOffset = EditorApplication.isPaused ? 4 : 0;
            GUI.SetNextControlName("ToolbarPlayModePauseButton");
            bool isPaused = GUILayout.Toggle(EditorApplication.isPaused, s_PlayIcons[buttonOffset + 1], Styles.commandMid);
            if (GUI.changed)
            {
                EditorApplication.isPaused = isPaused;
                GUIUtility.ExitGUI();
            }

            using (new EditorGUI.DisabledScope(!isPlaying))
            {
                // Step playmode
                GUI.SetNextControlName("ToolbarPlayModeStepButton");
                if (GUILayout.Button(s_PlayIcons[2], Styles.commandRight))
                {
                    EditorApplication.Step();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DoLayersDropDown(Rect rect)
        {
            if (EditorGUI.DropdownButton(rect, s_LayerContent, FocusType.Passive, Styles.dropdown))
            {
                if (LayerVisibilityWindow.ShowAtPosition(rect))
                {
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DoLayoutDropDown(Rect rect)
        {
            // Layout DropDown
            if (EditorGUI.DropdownButton(rect, EditorGUIUtility.TrTextContent(Toolbar.lastLoadedLayoutName, "Select editor layout"), FocusType.Passive, Styles.dropdown))
            {
                Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                rect.x = temp.x;
                rect.y = temp.y;
                EditorUtility.Internal_DisplayPopupMenu(rect, "Window/Layouts", this, 0, true);
            }
        }

        internal static void AddSubToolbar(SubToolbar subToolbar)
        {
            s_SubToolbars.Add(subToolbar);
        }

        internal static void RepaintToolbar()
        {
            if (get != null)
                get.Repaint();
        }
    }
}