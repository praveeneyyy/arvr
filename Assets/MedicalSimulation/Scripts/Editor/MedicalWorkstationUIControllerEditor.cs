using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MedicalSimulation.UI;

namespace MedicalSimulation.Editor
{
    [CustomEditor(typeof(MedicalWorkstationUIController))]
    public class MedicalWorkstationUIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (MedicalWorkstationUIController)target;

            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField("PROCEDURELAB CLINICAL WORKSTATION CONTROLS", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select any 3D Medical Component below to immediately view, animate, and inspect it in the viewport:", MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("01  Open Anatomy Explorer (Torso & Organs)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.FullAnatomy, "04_AnatomyLab");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("02  Open Cardiology Suite (Pulsing 3D Heart)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.Cardiology, "heart");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("03  Open Dental Surgery (Mandible & Teeth)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.Dental, "Teeth");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("04  Open Soft Tissue Surgery (Liver Resection)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.SoftTissue, "Liver Cut");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("05  Open Clinical Diagnosis (Vitals & Vessels)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.Diagnosis, "05_DiagnosisLab");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("06  Open Procedure Suite (Neural & Skeletal)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.Procedure, "06_ProcedureLab");
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("07  Open Virtual Operating Theatre (Full Room)", GUILayout.Height(30)))
            {
                controller.SelectMedicalComponent(MedicalComponentType.OperatingTheatre, "08_VirtualOperatingTheatre");
                EditorUtility.SetDirty(controller.gameObject);
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Rebuild Complete Workstation UI", GUILayout.Height(28)))
            {
                controller.InitializeWorkstation();
                EditorUtility.SetDirty(controller.gameObject);
            }

            if (GUILayout.Button("Force Reload Clinical 3D Models (Blender FBX)", GUILayout.Height(28)))
            {
                var viewer = Object.FindFirstObjectByType<Medical3DComponentViewer>();
                if (viewer != null) viewer.ForceReloadModels();
                controller.InitializeWorkstation();
                EditorUtility.SetDirty(controller.gameObject);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
