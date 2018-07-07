﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace HeapExplorer
{
    public class AbstractNativeObjectsView : HeapExplorerView
    {
        protected string m_EditorPrefsKey;
        protected NativeObjectsControl m_NativeObjectsControl;

        NativeObjectControl m_NativeObjectControl;
        HeSearchField m_SearchField;
        ConnectionsView m_ConnectionsView;
        PackedNativeUnityEngineObject? m_Selected;
        RootPathView m_RootPathView;
        NativeObjectPreviewView m_PreviewView;
        float m_SplitterHorz = 0.33333f;
        float m_SplitterVert = 0.32f;
        float m_PreviewSplitterVert = 0.32f;
        float m_RootPathSplitterVert = 0.32f;
        Rect m_FilterButtonRect;

        protected bool showAssets
        {
            get
            {
                return EditorPrefs.GetBool(m_EditorPrefsKey + ".showAssets", true);
            }
            set
            {
                EditorPrefs.SetBool(m_EditorPrefsKey + ".showAssets", value);
            }
        }

        protected bool showSceneObjects
        {
            get
            {
                return EditorPrefs.GetBool(m_EditorPrefsKey + ".showSceneObjects", true);
            }
            set
            {
                EditorPrefs.SetBool(m_EditorPrefsKey + ".showSceneObjects", value);
            }
        }

        protected bool showRuntimeObjects
        {
            get
            {
                return EditorPrefs.GetBool(m_EditorPrefsKey + ".showRuntimeObjects", true);
            }
            set
            {
                EditorPrefs.SetBool(m_EditorPrefsKey + ".showRuntimeObjects", value);
            }
        }

        protected bool showDestroyOnLoadObjects
        {
            get
            {
                return EditorPrefs.GetBool(m_EditorPrefsKey + ".showDestroyOnLoadObjects", true);
            }
            set
            {
                EditorPrefs.SetBool(m_EditorPrefsKey + ".showDestroyOnLoadObjects", value);
            }
        }

        protected bool showDontDestroyOnLoadObjects
        {
            get
            {
                return EditorPrefs.GetBool(m_EditorPrefsKey + ".showDontDestroyOnLoadObjects", true);
            }
            set
            {
                EditorPrefs.SetBool(m_EditorPrefsKey + ".showDontDestroyOnLoadObjects", value);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ConnectionsView = CreateView<ConnectionsView>();
            m_ConnectionsView.editorPrefsKey = m_EditorPrefsKey + ".m_connectionsView";

            m_RootPathView = CreateView<RootPathView>();
            m_RootPathView.editorPrefsKey = m_EditorPrefsKey + ".m_rootPathView";

            // The list at the left that contains all native objects
            m_NativeObjectsControl = new NativeObjectsControl(m_EditorPrefsKey + ".m_nativeObjectsControl", new TreeViewState());
            m_NativeObjectsControl.onSelectionChange += OnListViewSelectionChange;
            m_NativeObjectsControl.gotoCB += Goto;

            m_SearchField = new HeSearchField(window);
            m_SearchField.downOrUpArrowKeyPressed += m_NativeObjectsControl.SetFocusAndEnsureSelectedItem;
            m_NativeObjectsControl.findPressed += m_SearchField.SetFocus;

            // The list at the right that shows the selected native object
            m_NativeObjectControl = new NativeObjectControl(m_EditorPrefsKey + ".m_nativeObjectControl", new TreeViewState());
            m_PreviewView = CreateView<NativeObjectPreviewView>();

            m_SplitterHorz = EditorPrefs.GetFloat(m_EditorPrefsKey + ".m_splitterHorz", m_SplitterHorz);
            m_SplitterVert = EditorPrefs.GetFloat(m_EditorPrefsKey + ".m_splitterVert", m_SplitterVert);
            m_PreviewSplitterVert = EditorPrefs.GetFloat(m_EditorPrefsKey + ".m_PreviewSplitterVert", m_PreviewSplitterVert);
            m_RootPathSplitterVert = EditorPrefs.GetFloat(m_EditorPrefsKey + ".m_RootPathSplitterVert", m_RootPathSplitterVert);

            OnRebuild();
        }

        protected override void OnHide()
        {
            base.OnHide();

            m_NativeObjectsControl.SaveLayout();
            m_NativeObjectControl.SaveLayout();

            EditorPrefs.SetFloat(m_EditorPrefsKey + ".m_splitterHorz", m_SplitterHorz);
            EditorPrefs.SetFloat(m_EditorPrefsKey + ".m_splitterVert", m_SplitterVert);
            EditorPrefs.SetFloat(m_EditorPrefsKey + ".m_PreviewSplitterVert", m_PreviewSplitterVert);
            EditorPrefs.SetFloat(m_EditorPrefsKey + ".m_RootPathSplitterVert", m_RootPathSplitterVert);
        }

        protected virtual void OnRebuild()
        {
            // Derived classes overwrite this method to trigger their
            // individual tree rebuild jobs
        }
        
        protected void DrawFilterToolbarButton()
        {
            var hasFilter = false;
            if (!showAssets) hasFilter = true;
            if (!showSceneObjects) hasFilter = true;
            if (!showRuntimeObjects) hasFilter = true;
            if (!showDestroyOnLoadObjects) hasFilter = true;
            if (!showDontDestroyOnLoadObjects) hasFilter = true;

            var oldColor = GUI.color;
            if (hasFilter)
                GUI.color = new Color(oldColor.r, oldColor.g * 0.75f, oldColor.b * 0.75f, oldColor.a);

            if (GUILayout.Button(new GUIContent("Filter"), EditorStyles.toolbarDropDown, GUILayout.Width(70)))
            {
                PopupWindow.Show(m_FilterButtonRect, new NativeObjectsFilterWindowContent(this));
            }

            if (Event.current.type == EventType.Repaint)
                m_FilterButtonRect = GUILayoutUtility.GetLastRect();

            GUI.color = oldColor;
        }

        public override GotoCommand GetRestoreCommand()
        {
            var command = m_Selected.HasValue ? new GotoCommand(new RichNativeObject(m_snapshot, m_Selected.Value.nativeObjectsArrayIndex)) : null;
            return command;
        }

        public void Select(PackedNativeUnityEngineObject packed)
        {
            m_NativeObjectsControl.Select(packed);
        }

        public override void OnToolbarGUI()
        {
            base.OnToolbarGUI();

            DrawFilterToolbarButton();
        }

        public override void OnGUI()
        {
            base.OnGUI();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // Native objects list at the left side
                    using (new EditorGUILayout.VerticalScope(HeEditorStyles.panel))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            OnDrawHeader();
                            
                            if (m_SearchField.OnToolbarGUI())
                                m_NativeObjectsControl.Search(m_SearchField.text);
                        }
                        GUILayout.Space(2);

                        m_NativeObjectsControl.OnGUI();
                    }

                    HeEditorGUILayout.VerticalSplitter("m_splitterVert".GetHashCode(), ref m_SplitterVert, 0.1f, 0.8f, window);

                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(window.position.height * m_SplitterVert)))
                    {
                        m_ConnectionsView.OnGUI();
                    }
                }

                HeEditorGUILayout.HorizontalSplitter("m_splitterHorz".GetHashCode(), ref m_SplitterHorz, 0.1f, 0.8f, window);

                // Various panels at the right side
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(window.position.width * m_SplitterHorz)))
                {
                    using (new EditorGUILayout.VerticalScope(HeEditorStyles.panel))
                    {
                        using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(16)))
                        {
                            if (m_Selected.HasValue)
                            {
                                HeEditorGUI.NativeObjectIcon(GUILayoutUtility.GetRect(16, 16), m_Selected.Value);
                                //GUI.DrawTexture(r, HeEditorStyles.assetImage);
                            }

                            EditorGUILayout.LabelField("Native UnityEngine object", EditorStyles.boldLabel);
                        }

                        GUILayout.Space(2);
                        m_NativeObjectControl.OnGUI();
                    }

                    HeEditorGUILayout.VerticalSplitter("m_PreviewSplitterVert".GetHashCode(), ref m_PreviewSplitterVert, 0.1f, 0.8f, window);

                    using (new EditorGUILayout.VerticalScope(HeEditorStyles.panel, GUILayout.Height(window.position.height * m_PreviewSplitterVert)))
                    {
                        m_PreviewView.OnGUI();
                    }

                    HeEditorGUILayout.VerticalSplitter("m_RootPathSplitterVert".GetHashCode(), ref m_RootPathSplitterVert, 0.1f, 0.8f, window);

                    using (new EditorGUILayout.VerticalScope(HeEditorStyles.panel, GUILayout.Height(window.position.height * m_RootPathSplitterVert)))
                    {
                        m_RootPathView.OnGUI();
                    }
                }
            }
        }

        protected virtual void OnDrawHeader()
        {
        }

        void OnListViewSelectionChange(PackedNativeUnityEngineObject? nativeObject)
        {
            m_Selected = nativeObject;
            if (!m_Selected.HasValue)
            {
                m_RootPathView.Clear();
                m_ConnectionsView.Clear();
                m_NativeObjectControl.Clear();
                m_PreviewView.Clear();
                return;
            }

            m_RootPathView.Inspect(m_Selected.Value);
            m_ConnectionsView.Inspect(m_Selected.Value);
            m_NativeObjectControl.Inspect(m_snapshot, m_Selected.Value);
            m_PreviewView.Inspect(m_Selected.Value);
        }

        // The 'Filer' menu displays this content
        class NativeObjectsFilterWindowContent : PopupWindowContent
        {
            AbstractNativeObjectsView m_Owner;
            bool m_ShowAssets;
            bool m_ShowSceneObjects;
            bool m_ShowRuntimeObjects;
            bool m_ShowDestroyOnLoadObjects;
            bool m_ShowDontDestroyOnLoadObjects;

            public NativeObjectsFilterWindowContent(AbstractNativeObjectsView owner)
            {
                m_Owner = owner;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(280, 190);
            }

            public override void OnGUI(Rect rect)
            {
                if (m_Owner == null)
                {
                    editorWindow.Close();
                    return;
                }

                GUILayout.Space(4);

                GUILayout.Label("Object types", EditorStyles.boldLabel);
                m_ShowAssets = GUILayout.Toggle(m_ShowAssets, new GUIContent("Show assets", HeEditorStyles.assetImage), GUILayout.Height(18));
                m_ShowSceneObjects = GUILayout.Toggle(m_ShowSceneObjects, new GUIContent("Show scene objects", HeEditorStyles.sceneImage), GUILayout.Height(18));
                m_ShowRuntimeObjects = GUILayout.Toggle(m_ShowRuntimeObjects, new GUIContent("Show runtime objects", HeEditorStyles.instanceImage), GUILayout.Height(18));

                GUILayout.Space(4);
                GUILayout.Label("Object flags", EditorStyles.boldLabel);
                m_ShowDestroyOnLoadObjects = GUILayout.Toggle(m_ShowDestroyOnLoadObjects, new GUIContent("Show 'Destroy on load' assets/objects"), GUILayout.Height(18));
                m_ShowDontDestroyOnLoadObjects = GUILayout.Toggle(m_ShowDontDestroyOnLoadObjects, new GUIContent("Show 'Don't destroy on load' assets/objects"), GUILayout.Height(18));

                GUILayout.Space(14);
                if (GUILayout.Button("Apply"))
                {
                    Apply();
                    editorWindow.Close();
                }
            }

            void Apply()
            {
                m_Owner.showAssets = m_ShowAssets;
                m_Owner.showSceneObjects = m_ShowSceneObjects;
                m_Owner.showRuntimeObjects = m_ShowRuntimeObjects;
                m_Owner.showDestroyOnLoadObjects = m_ShowDestroyOnLoadObjects;
                m_Owner.showDontDestroyOnLoadObjects = m_ShowDontDestroyOnLoadObjects;

                m_Owner.OnRebuild();
            }

            public override void OnOpen()
            {
                m_ShowAssets = m_Owner.showAssets;
                m_ShowSceneObjects = m_Owner.showSceneObjects;
                m_ShowRuntimeObjects = m_Owner.showRuntimeObjects;
                m_ShowDestroyOnLoadObjects = m_Owner.showDestroyOnLoadObjects;
                m_ShowDontDestroyOnLoadObjects = m_Owner.showDontDestroyOnLoadObjects;
            }

            public override void OnClose()
            {
            }
        }
    }
}