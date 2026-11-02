using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MedicalSimulation.UI;

namespace MedicalSimulation.Editor
{
    [CustomEditor(typeof(Medical3DComponentViewer))]
    public class Medical3DComponentViewerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var viewer = (Medical3DComponentViewer)target;
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("3D CLINICAL COMPONENT SELECTOR", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click any button below to instantly render and inspect that 3D medical component in the viewport.", MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("01  Open Anatomy Explorer (Torso & Organs)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.FullAnatomy);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("02  Open Cardiology Suite (3D Pulsing Heart)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.Cardiology);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("03  Open Dental Surgery (Mandible & Dentition)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.Dental);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("04  Open Soft Tissue Surgery (Liver Resection)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.SoftTissue);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("05  Open Clinical Diagnosis (Vitals & Vessels)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.Diagnosis);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("06  Open Procedure Suite (Neural & Skeletal)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.Procedure);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            if (GUILayout.Button("07  Open Virtual Operating Theatre (Full Suite)", GUILayout.Height(32)))
            {
                viewer.ShowComponent(MedicalComponentType.OperatingTheatre);
                EditorUtility.SetDirty(viewer.gameObject);
            }

            EditorGUILayout.EndVertical();
        }

        [MenuItem("MedicalSimulation/Open 3D Component/01 Anatomy Explorer")]
        public static void MenuOpenAnatomy() => OpenComponent(MedicalComponentType.FullAnatomy);

        [MenuItem("MedicalSimulation/Open 3D Component/02 Cardiology Suite")]
        public static void MenuOpenCardiology() => OpenComponent(MedicalComponentType.Cardiology);

        [MenuItem("MedicalSimulation/Open 3D Component/03 Dental Surgery")]
        public static void MenuOpenDental() => OpenComponent(MedicalComponentType.Dental);

        [MenuItem("MedicalSimulation/Open 3D Component/04 Soft Tissue Surgery")]
        public static void MenuOpenSoftTissue() => OpenComponent(MedicalComponentType.SoftTissue);

        [MenuItem("MedicalSimulation/Open 3D Component/05 Clinical Diagnosis")]
        public static void MenuOpenDiagnosis() => OpenComponent(MedicalComponentType.Diagnosis);

        [MenuItem("MedicalSimulation/Open 3D Component/06 Procedure Lab")]
        public static void MenuOpenProcedure() => OpenComponent(MedicalComponentType.Procedure);

        [MenuItem("MedicalSimulation/Open 3D Component/07 Virtual Operating Theatre")]
        public static void MenuOpenOperatingTheatre() => OpenComponent(MedicalComponentType.OperatingTheatre);

        private static void OpenComponent(MedicalComponentType type)
        {
            var viewer = Object.FindFirstObjectByType<Medical3DComponentViewer>();
            if (viewer == null)
            {
                var go = new GameObject("3D_Medical_Component_MasterRig");
                viewer = go.AddComponent<Medical3DComponentViewer>();
            }

            viewer.ShowComponent(type);
            EditorUtility.SetDirty(viewer.gameObject);
            SceneView.RepaintAll();
        }
    }
}
