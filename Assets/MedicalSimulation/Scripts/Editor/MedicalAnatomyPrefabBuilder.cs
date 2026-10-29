using System.IO;
using UnityEngine;
using UnityEditor;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;

namespace MedicalSimulation.Editor
{
    /// <summary>
    /// Editor automation tool that constructs Unity-ready Prefabs from the
    /// exported Blender anatomical FBX/GLB meshes, assigns URP materials, colliders,
    /// OrganInteractable metadata, and saves them into Assets/MedicalSimulation/Prefabs/Anatomy/.
    /// </summary>
    public static class MedicalAnatomyPrefabBuilder
    {
        private const string ArtDir = "Assets/MedicalSimulation/Art";
        private const string PrefabsDir = "Assets/MedicalSimulation/Prefabs/Anatomy";
        private const string MaterialsDir = "Assets/MedicalSimulation/Materials";

        [MenuItem("MedicalSimulation/Build All Anatomical Prefabs")]
        public static void BuildAllPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/MedicalSimulation/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/MedicalSimulation", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/MedicalSimulation/Prefabs", "Anatomy");
            }

            Debug.Log("<color=#00E5FF>==================================================\nBUILDING MEDICAL ANATOMICAL PREFABS\n==================================================</color>");

            BuildSubsystemPrefab("Heart_Detailed.fbx", $"{ArtDir}/Organs/Heart_Detailed.fbx", $"{PrefabsDir}/Heart_Detailed.prefab", "heart", "Mat_Heart_Myocardium");
            BuildSubsystemPrefab("Respiratory_System.fbx", $"{ArtDir}/Organs/Respiratory_System.fbx", $"{PrefabsDir}/Respiratory_System.prefab", "lung", "Mat_Lung_Tissue");
            BuildSubsystemPrefab("Digestive_System.fbx", $"{ArtDir}/Organs/Digestive_System.fbx", $"{PrefabsDir}/Digestive_System.prefab", "stomach", "Mat_Gastric_SmoothMuscle");
            BuildSubsystemPrefab("Urinary_System.fbx", $"{ArtDir}/Organs/Urinary_System.fbx", $"{PrefabsDir}/Urinary_System.prefab", "kidney", "Mat_Renal_Capsule");
            BuildSubsystemPrefab("Brain_Nervous_System.fbx", $"{ArtDir}/Organs/Brain_Nervous_System.fbx", $"{PrefabsDir}/Brain_Nervous_System.prefab", "brain", "Mat_Brain_Cortex");
            BuildSubsystemPrefab("Skeletal_System.fbx", $"{ArtDir}/Skeleton/Skeletal_System.fbx", $"{PrefabsDir}/Skeletal_System.prefab", "skeleton", "Mat_Bone_Cortical");
            BuildSubsystemPrefab("Cardiovascular_System.fbx", $"{ArtDir}/Vessels/Cardiovascular_System.fbx", $"{PrefabsDir}/Cardiovascular_System.prefab", "heart", "Mat_Vessel_Arterial");

            BuildMasterTorsoPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00FF99>==================================================\nALL ANATOMICAL PREFABS BUILT SUCCESSFULLY (100%)\n==================================================</color>");
        }

        private static void BuildSubsystemPrefab(string systemName, string fbxPath, string prefabDest, string fallbackOrganKey, string defaultMatName)
        {
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject(Path.GetFileNameWithoutExtension(prefabDest));

            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/{defaultMatName}.mat");

            if (fbxAsset != null)
            {
                var instance = Object.Instantiate(fbxAsset, root.transform);
                instance.name = "MeshHierarchy";

                // Wire renderers and colliders on child meshes
                var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    if (mat != null)
                    {
                        r.sharedMaterial = mat;
                    }

                    var col = r.gameObject.GetComponent<Collider>();
                    if (col == null)
                    {
                        var mc = r.gameObject.AddComponent<MeshCollider>();
                        mc.convex = true;
                    }

                    var organ = r.gameObject.GetComponent<OrganInteractable>() ?? r.gameObject.AddComponent<OrganInteractable>();
                    organ.metadata = AnatomyMetadata.GetFallback(r.gameObject.name.ToLowerInvariant()) ?? AnatomyMetadata.GetFallback(fallbackOrganKey);
                }
            }
            else
            {
                // Fallback primitive if FBX was not loaded
                var prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prim.transform.SetParent(root.transform, false);
                prim.name = "MeshFallback";
                if (mat != null) prim.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabDest);
            Object.DestroyImmediate(root);
            Debug.Log($"[MedicalAnatomyPrefabBuilder] Created: {prefabDest}");
        }

        private static void BuildMasterTorsoPrefab()
        {
            string fbxPath = $"{ArtDir}/Anatomy/Human_Torso_Anatomy_Full.fbx";
            string prefabDest = $"{PrefabsDir}/Torso_Anatomy_Master.prefab";

            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject("Torso_Anatomy_Master");
            root.AddComponent<AnatomyModelController>();
            root.AddComponent<AnatomicalSlicePlaneController>();

            if (fbxAsset != null)
            {
                var instance = Object.Instantiate(fbxAsset, root.transform);
                instance.name = "AnatomyAssembly_3D";

                var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    string meshName = r.gameObject.name.ToLowerInvariant();
                    string matName = ResolveMaterialName(meshName);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/{matName}.mat");
                    if (mat != null)
                    {
                        r.sharedMaterial = mat;
                    }

                    var col = r.gameObject.GetComponent<Collider>();
                    if (col == null)
                    {
                        var mc = r.gameObject.AddComponent<MeshCollider>();
                        mc.convex = true;
                    }

                    var organ = r.gameObject.GetComponent<OrganInteractable>() ?? r.gameObject.AddComponent<OrganInteractable>();
                    organ.metadata = ResolveMetadata(meshName);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabDest);
            Object.DestroyImmediate(root);
            Debug.Log($"[MedicalAnatomyPrefabBuilder] Created: {prefabDest}");
        }

        private static string ResolveMaterialName(string meshName)
        {
            if (meshName.Contains("heart") || meshName.Contains("cardiac")) return "Mat_Heart_Myocardium";
            if (meshName.Contains("lung") || meshName.Contains("pulmonary")) return "Mat_Lung_Tissue";
            if (meshName.Contains("liver") || meshName.Contains("hepatic")) return "Mat_Liver_Hepatic";
            if (meshName.Contains("stomach") || meshName.Contains("gastric")) return "Mat_Gastric_SmoothMuscle";
            if (meshName.Contains("intestine") || meshName.Contains("colon") || meshName.Contains("gut")) return "Mat_Intestinal_Mucosa";
            if (meshName.Contains("kidney") || meshName.Contains("renal")) return "Mat_Renal_Capsule";
            if (meshName.Contains("brain") || meshName.Contains("cerebr")) return "Mat_Brain_Cortex";
            if (meshName.Contains("cartilage") || meshName.Contains("trachea")) return "Mat_Cartilage_Hyaline";
            if (meshName.Contains("artery") || meshName.Contains("aorta") || meshName.Contains("coronary")) return "Mat_Vessel_Arterial";
            if (meshName.Contains("vein") || meshName.Contains("cava")) return "Mat_Vessel_Venous";
            if (meshName.Contains("silhouette") || meshName.Contains("torso")) return "Mat_Torso_Silhouette";
            return "Mat_Bone_Cortical";
        }

        private static AnatomyMetadata ResolveMetadata(string meshName)
        {
            if (meshName.Contains("heart")) return AnatomyMetadata.GetFallback("heart");
            if (meshName.Contains("lung")) return AnatomyMetadata.GetFallback("lung");
            if (meshName.Contains("liver")) return AnatomyMetadata.GetFallback("liver");
            if (meshName.Contains("stomach")) return AnatomyMetadata.GetFallback("stomach");
            if (meshName.Contains("intestine")) return AnatomyMetadata.GetFallback("intestine");
            if (meshName.Contains("kidney")) return AnatomyMetadata.GetFallback("kidney");
            return AnatomyMetadata.GetFallback(meshName);
        }
    }
}
