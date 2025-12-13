using UnityEngine;
using UnityEditor;

namespace GabrielBissonnette.Primo
{
    [CustomEditor(typeof(MainMenuManager))]
    public class MainMenuEditor : Editor
    {
        #region SerializedProperty
        private SerializedProperty showBackground;
        private SerializedProperty showSocial1;
        private SerializedProperty showSocial2;
        private SerializedProperty showSocial3;
        private SerializedProperty showVersion;
        private SerializedProperty showFade;
        private SerializedProperty social1Icon;
        private SerializedProperty social2Icon;
        private SerializedProperty social3Icon;
        private SerializedProperty version;
        private SerializedProperty play;
        private SerializedProperty settings;
        private SerializedProperty quit;
        private SerializedProperty social1Link;
        private SerializedProperty social2Link;
        private SerializedProperty social3Link;
        private SerializedProperty logo;
        private SerializedProperty background;
        private SerializedProperty buttons;
        private SerializedProperty mainColor;
        private SerializedProperty secondaryColor;
        private SerializedProperty sceneToLoad;
        private SerializedProperty defaultVolume;
        private SerializedProperty uiClick;
        private SerializedProperty uiHover;
        private SerializedProperty uiSpecial;
        private SerializedProperty inputActions;

        private SerializedProperty social1Image;
        private SerializedProperty social2Image;
        private SerializedProperty social3Image;
        private SerializedProperty playText;
        private SerializedProperty settingsText;
        private SerializedProperty quitText;
        private SerializedProperty versionText;
        private SerializedProperty logoImage;
        private SerializedProperty backgroundImage;
        private SerializedProperty mainColorImages;
        private SerializedProperty mainColorTexts;
        private SerializedProperty secondaryColorImages;
        private SerializedProperty secondaryColorTexts;
        private SerializedProperty buttonsElements;
        private SerializedProperty fadeAnimator;
        private SerializedProperty volumeSlider;
        private SerializedProperty resolutionDropdown;
        private SerializedProperty audioSource;
        #endregion

        #region Private
        private string[] m_Tabs = { "Values", "Components" };
        private int m_TabsSelected = 0;
        MainMenuManager mainMenuManager;
        SerializedObject soTarget;
        Texture2D texturePanel1;
        Texture2D texturePanel2;
        #endregion

        private void OnEnable()
        {
            mainMenuManager = (MainMenuManager)target;
            soTarget = new SerializedObject(target);

            texturePanel1 = Resources.Load<Texture2D>("InspectorBanner1");
            texturePanel2 = Resources.Load<Texture2D>("InspectorBanner2");

            #region FindProperty
            showBackground = soTarget.FindProperty("showBackground");
            showSocial1 = soTarget.FindProperty("showSocial1");
            showSocial2 = soTarget.FindProperty("showSocial2");
            showSocial3 = soTarget.FindProperty("showSocial3");
            version = soTarget.FindProperty("version");
            play = soTarget.FindProperty("play");
            settings = soTarget.FindProperty("settings");
            quit = soTarget.FindProperty("quit");
            social1Link = soTarget.FindProperty("social1Link");
            social2Link = soTarget.FindProperty("social2Link");
            social3Link = soTarget.FindProperty("social3Link");
            social1Icon = soTarget.FindProperty("social1Icon");
            social2Icon = soTarget.FindProperty("social2Icon");
            social3Icon = soTarget.FindProperty("social3Icon");
            logo = soTarget.FindProperty("logo");
            background = soTarget.FindProperty("background");
            buttons = soTarget.FindProperty("buttons");
            mainColor = soTarget.FindProperty("mainColor");
            secondaryColor = soTarget.FindProperty("secondaryColor");
            sceneToLoad = soTarget.FindProperty("sceneToLoad");
            defaultVolume = soTarget.FindProperty("defaultVolume");
            showVersion = soTarget.FindProperty("showVersion");
            showFade = soTarget.FindProperty("showFade");
            uiClick = soTarget.FindProperty("uiClick");
            uiHover = soTarget.FindProperty("uiHover");
            uiSpecial = soTarget.FindProperty("uiSpecial");
            inputActions = soTarget.FindProperty("inputActions");

            social1Image = soTarget.FindProperty("social1Image");
            social2Image = soTarget.FindProperty("social2Image");
            social3Image = soTarget.FindProperty("social3Image");
            playText = soTarget.FindProperty("playText");
            settingsText = soTarget.FindProperty("settingsText");
            quitText = soTarget.FindProperty("quitText");
            versionText = soTarget.FindProperty("versionText");
            logoImage = soTarget.FindProperty("logoImage");
            backgroundImage = soTarget.FindProperty("backgroundImage");
            mainColorImages = soTarget.FindProperty("mainColorImages");
            mainColorTexts = soTarget.FindProperty("mainColorTexts");
            secondaryColorImages = soTarget.FindProperty("secondaryColorImages");
            secondaryColorTexts = soTarget.FindProperty("secondaryColorTexts");
            buttonsElements = soTarget.FindProperty("buttonsElements");
            fadeAnimator = soTarget.FindProperty("fadeAnimator");
            volumeSlider = soTarget.FindProperty("volumeSlider");
            resolutionDropdown = soTarget.FindProperty("resolutionDropdown");
            audioSource = soTarget.FindProperty("audioSource");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            soTarget.Update();
            EditorGUI.BeginChangeCheck();

            #region Tabs

            EditorGUILayout.BeginHorizontal();
            m_TabsSelected = GUILayout.Toolbar(m_TabsSelected, m_Tabs);
            EditorGUILayout.EndHorizontal();

            if (m_TabsSelected >= 0 || m_TabsSelected < m_Tabs.Length)
            {
                switch (m_Tabs[m_TabsSelected])
                {
                    case "Values":
                        Values();
                        break;

                    case "Components":
                        Components();
                        break;

                    default:
                        break;
                }
            }

            #endregion

            if (EditorGUI.EndChangeCheck())
            {
                soTarget.ApplyModifiedProperties();
            }

            mainMenuManager = (MainMenuManager)target;
            mainMenuManager.UIEditorUpdate();
        }

        private void Values()
        {
            // Image
            EditorGUI.DrawPreviewTexture(new Rect(18, 30, 520, 80), texturePanel1, mat: null, ScaleMode.ScaleToFit);

            EditorGUILayout.Space(90);
            EditorGUILayout.PropertyField(showBackground);
            EditorGUILayout.PropertyField(showSocial1);
            EditorGUILayout.PropertyField(showSocial2);
            EditorGUILayout.PropertyField(showSocial3);
            EditorGUILayout.PropertyField(showVersion);
            EditorGUILayout.PropertyField(showFade);
            EditorGUILayout.HelpBox("Enable and disable specific elements in the menu", MessageType.None);

            EditorGUILayout.PropertyField(sceneToLoad);
            EditorGUILayout.HelpBox("The name of the first scene to load", MessageType.None);

            EditorGUILayout.PropertyField(logo);
            EditorGUILayout.PropertyField(background);
            EditorGUILayout.PropertyField(buttons);
            EditorGUILayout.HelpBox("Sprites applied to specific elements. Do not edit directly in the component", MessageType.None);

            EditorGUILayout.PropertyField(mainColor);
            EditorGUILayout.PropertyField(secondaryColor);
            EditorGUILayout.HelpBox("The main and secondary color to be applied to every UI Element", MessageType.None);

            EditorGUILayout.PropertyField(version);
            EditorGUILayout.HelpBox("A reference for development. Can be disabled in the On/Off section", MessageType.None);

            EditorGUILayout.PropertyField(play);
            EditorGUILayout.PropertyField(settings);
            EditorGUILayout.PropertyField(quit);
            EditorGUILayout.HelpBox("Texts for the main buttons. Do not edit directly in the component", MessageType.None);

            EditorGUILayout.PropertyField(social1Icon);
            EditorGUILayout.PropertyField(social1Link);
            EditorGUILayout.PropertyField(social2Icon);
            EditorGUILayout.PropertyField(social2Link);
            EditorGUILayout.PropertyField(social3Icon);
            EditorGUILayout.PropertyField(social3Link);
            EditorGUILayout.HelpBox("Social links and sprites. Do not edit directly in the component", MessageType.None);

            EditorGUILayout.PropertyField(defaultVolume);
            EditorGUILayout.HelpBox("Default volume for the AudioListener. Volume is saved in PlayerPrefs.", MessageType.None);

            EditorGUILayout.PropertyField(uiClick);
            EditorGUILayout.PropertyField(uiHover);
            EditorGUILayout.PropertyField(uiSpecial);
            EditorGUILayout.HelpBox("Sounds when buttons are clicked or hovered", MessageType.None);

            #if USE_NEW_INPUT_SYSTEM
            EditorGUILayout.PropertyField(inputActions);
            #endif
        }

        private void Components()
        {
            // Image
            EditorGUI.DrawPreviewTexture(new Rect(18, 30, 520, 80), texturePanel2, mat: null, ScaleMode.ScaleToFit);

            EditorGUILayout.Space(120);
            EditorGUILayout.HelpBox("To link an element with the colors, add it in a category below", MessageType.Info);
            EditorGUILayout.PropertyField(mainColorImages);
            EditorGUILayout.PropertyField(mainColorTexts);
            EditorGUILayout.PropertyField(secondaryColorImages);
            EditorGUILayout.PropertyField(secondaryColorTexts);
            EditorGUILayout.PropertyField(buttonsElements);

            EditorGUILayout.Space(50);
            EditorGUILayout.HelpBox("You shouldn't need to edit the rest of theses components", MessageType.Info);
            EditorGUILayout.PropertyField(social1Image);
            EditorGUILayout.PropertyField(social2Image);
            EditorGUILayout.PropertyField(social3Image);
            EditorGUILayout.PropertyField(fadeAnimator);
            EditorGUILayout.PropertyField(playText);
            EditorGUILayout.PropertyField(settingsText);
            EditorGUILayout.PropertyField(quitText);
            EditorGUILayout.PropertyField(versionText);
            EditorGUILayout.PropertyField(logoImage);
            EditorGUILayout.PropertyField(backgroundImage);
            EditorGUILayout.PropertyField(volumeSlider);
            EditorGUILayout.PropertyField(resolutionDropdown);

            EditorGUILayout.PropertyField(audioSource);
        }
    }
}
