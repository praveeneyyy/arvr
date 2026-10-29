using UnityEngine;
using MedicalSimulation.Anatomy;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Layer visibility controller. Maps named anatomy layer buttons to 
    /// the visibility of their associated GameObjects (layer groups).
    /// 
    /// Assign the root GameObjects for each anatomical layer in the Inspector.
    /// </summary>
    public class AnatomyLayerController : MonoBehaviour
    {
        [System.Serializable]
        public struct LayerEntry
        {
            public string       layerName;
            public GameObject[] rootObjects;
            [Range(0f, 1f)]
            public float        defaultAlpha;
        }

        [SerializeField] private LayerEntry[] layers = new LayerEntry[]
        {
            new LayerEntry { layerName = "Skin",           defaultAlpha = 1f },
            new LayerEntry { layerName = "Muscles",        defaultAlpha = 1f },
            new LayerEntry { layerName = "Skeleton",       defaultAlpha = 1f },
            new LayerEntry { layerName = "Internal Organs",defaultAlpha = 1f },
            new LayerEntry { layerName = "Nervous System", defaultAlpha = 1f },
            new LayerEntry { layerName = "Vascular System",defaultAlpha = 1f },
        };

        void Start()
        {
            foreach (var layer in layers)
                SetLayerVisible(layer.layerName, true);
        }

        public void SetLayerVisible(string layerName, bool visible)
        {
            foreach (var layer in layers)
            {
                if (layer.layerName != layerName) continue;
                foreach (var go in layer.rootObjects)
                    if (go != null) go.SetActive(visible);
            }
        }

        public void SetLayerAlpha(string layerName, float alpha)
        {
            foreach (var layer in layers)
            {
                if (layer.layerName != layerName) continue;
                foreach (var go in layer.rootObjects)
                {
                    if (go == null) continue;
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var mat in r.materials)
                        {
                            Color c  = mat.color;
                            c.a      = Mathf.Clamp01(alpha);
                            mat.color = c;
                        }
                    }
                }
            }
        }

        public void ToggleLayer(string layerName)
        {
            foreach (var layer in layers)
            {
                if (layer.layerName != layerName) continue;
                if (layer.rootObjects.Length == 0) continue;
                bool isActive = layer.rootObjects[0] != null && layer.rootObjects[0].activeSelf;
                SetLayerVisible(layerName, !isActive);
            }
        }

        public void ShowAllLayers()
        {
            foreach (var layer in layers)
                SetLayerVisible(layer.layerName, true);
        }

        public void HideAllLayers()
        {
            foreach (var layer in layers)
                SetLayerVisible(layer.layerName, false);
        }
    }
}
