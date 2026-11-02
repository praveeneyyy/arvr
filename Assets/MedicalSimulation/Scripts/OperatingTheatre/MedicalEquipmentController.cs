using System;
using UnityEngine;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Controls interactive equipment in the Operating Theatre:
    /// 1. Overhead Surgical Lights (intensity, angle, focus spot)
    /// 2. Operating Table (height elevation, Trendelenburg tilt)
    /// 3. Surgical Instrument Mayo Stand (Scalpel, Forceps, Scissors, Retractor, Suction, Cautery)
    /// 4. Electrosurgical Unit (Cut/Coag modes, wattage)
    /// </summary>
    public class MedicalEquipmentController : MonoBehaviour
    {
        public enum SurgicalInstrument
        {
            None,
            ScalpelNo10,
            MetzenbaumScissors,
            DeBakeyForceps,
            RichardsonRetractor,
            ElectrosurgicalBovie,
            YankauerSuction
        }

        [Header("Overhead Surgical Lamps")]
        [SerializeField] private Light mainSurgicalLight;
        [SerializeField] private Light secondarySurgicalLight;
        [Range(0f, 3f)] [SerializeField] private float lampIntensity = 1.4f;
        [Range(20f, 65f)] [SerializeField] private float lampSpotAngle = 38f;
        [SerializeField] private bool lampsActive = true;

        [Header("Operating Table")]
        [SerializeField] private Transform operatingTableRoot;
        [Range(-15f, 15f)] [SerializeField] private float trendelenburgTilt = 0f;
        [Range(0.75f, 1.25f)] [SerializeField] private float tableHeight = 0.95f;

        [Header("Instrument Tray (Mayo Stand)")]
        [SerializeField] private SurgicalInstrument activeInstrument = SurgicalInstrument.None;

        [Header("Electrosurgical Unit")]
        [SerializeField] private bool isCoagMode = false;
        [Range(10, 80)] [SerializeField] private int bovieWatts = 35;

        // Events
        public event Action<SurgicalInstrument> OnInstrumentEquipped;
        public event Action<float> OnLampIntensityChanged;
        public event Action<float> OnTableHeightChanged;

        public SurgicalInstrument ActiveInstrument => activeInstrument;
        public float LampIntensity => lampIntensity;
        public float TableHeight => tableHeight;
        public float TableTilt => trendelenburgTilt;

        void Start()
        {
            ApplyLampSettings();
            ApplyTableSettings();
        }

        public void ToggleSurgicalLamps()
        {
            lampsActive = !lampsActive;
            ApplyLampSettings();
        }

        public void SetLampIntensity(float intensity)
        {
            lampIntensity = Mathf.Clamp(intensity, 0f, 3f);
            ApplyLampSettings();
            OnLampIntensityChanged?.Invoke(lampIntensity);
        }

        public void SetLampFocus(float spotAngle)
        {
            lampSpotAngle = Mathf.Clamp(spotAngle, 15f, 75f);
            ApplyLampSettings();
        }

        private void ApplyLampSettings()
        {
            float targetIntensity = lampsActive ? lampIntensity : 0f;
            if (mainSurgicalLight != null)
            {
                mainSurgicalLight.intensity = targetIntensity;
                mainSurgicalLight.spotAngle = lampSpotAngle;
            }
            if (secondarySurgicalLight != null)
            {
                secondarySurgicalLight.intensity = targetIntensity * 0.75f;
                secondarySurgicalLight.spotAngle = lampSpotAngle * 1.15f;
            }
        }

        public void SetTableHeight(float height)
        {
            tableHeight = Mathf.Clamp(height, 0.75f, 1.25f);
            ApplyTableSettings();
            OnTableHeightChanged?.Invoke(tableHeight);
        }

        public void SetTableTilt(float tiltDegrees)
        {
            trendelenburgTilt = Mathf.Clamp(tiltDegrees, -20f, 20f);
            ApplyTableSettings();
        }

        private void ApplyTableSettings()
        {
            if (operatingTableRoot != null)
            {
                Vector3 pos = operatingTableRoot.localPosition;
                pos.y = tableHeight;
                operatingTableRoot.localPosition = pos;

                Vector3 rot = operatingTableRoot.localEulerAngles;
                rot.x = trendelenburgTilt;
                operatingTableRoot.localEulerAngles = rot;
            }
        }

        public void EquipInstrument(SurgicalInstrument instrument)
        {
            activeInstrument = instrument;
            OnInstrumentEquipped?.Invoke(activeInstrument);
        }

        public void SetElectrosurgicalMode(bool coag)
        {
            isCoagMode = coag;
        }

        public void SetBovieWatts(int watts)
        {
            bovieWatts = Mathf.Clamp(watts, 5, 100);
        }
    }
}
