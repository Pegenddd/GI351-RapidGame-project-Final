using UnityEngine;
using UnityEditor;
using NecroDefense.Managers;

namespace NecroDefense.Editor
{
    [CustomEditor(typeof(GameSceneSetup))]
    public class GameSceneSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.5f);
            if (GUILayout.Button("⚡ Generate Complete Hierarchy In Scene", GUILayout.Height(36)))
            {
                NecroDefenseSceneBuilder.SetupCompleteHierarchy();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}

