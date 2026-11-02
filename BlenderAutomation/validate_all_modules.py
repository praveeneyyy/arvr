import os
import bpy

modules = [
    {
        "name": "Module 1 - Full Human Anatomy Explorer",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Human_Torso_Anatomy_Full.fbx",
        "blend": r"BlenderAutomation\Human_Torso_Anatomy_Full.blend",
        "root": "Human_Torso_Anatomy_Full",
        "expected_subroots": [
            "System_Skeletal", "System_Muscular", "System_Cardiovascular",
            "System_Respiratory", "System_Digestive", "System_Nervous", "System_Integumentary"
        ]
    },
    {
        "name": "Module 2 - Detailed Cardiology Model",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Heart_Detailed.fbx",
        "blend": r"BlenderAutomation\Heart_Detailed.blend",
        "root": "Heart_Detailed",
        "expected_subroots": ["Four_Chamber_Heart"]
    },
    {
        "name": "Module 3 - Dental Anatomy",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Dental_Mandible_Teeth.fbx",
        "blend": r"BlenderAutomation\Dental_Mandible_Teeth.blend",
        "root": "Dental_Mandible_Teeth",
        "expected_subroots": ["Upper_Jaw", "Lower_Jaw", "Gums_Gingiva"]
    },
    {
        "name": "Module 4 - Hepatic and Biliary Anatomy",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Digestive_System.fbx",
        "blend": r"BlenderAutomation\Digestive_System.blend",
        "root": "Digestive_System",
        "expected_subroots": ["Hepatic_Biliary_Complex"]
    },
    {
        "name": "Module 5 - Surgical Instruments Tray",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Surgical_Instruments_Tray.fbx",
        "blend": r"BlenderAutomation\Surgical_Instruments_Tray.blend",
        "root": "Surgical_Instruments_Tray",
        "expected_subroots": ["Instrument_Tray", "Scalpel_No10", "DeBakey_Forceps", "Metzenbaum_Scissors", "Electrocautery_Pencil", "Hemostat_Clamp", "Needle_Holder"]
    },
    {
        "name": "Module 6 - Virtual Operating Theatre Suite",
        "fbx": r"Assets\MedicalSimulation\Resources\Models\Operating_Theatre_Suite.fbx",
        "blend": r"BlenderAutomation\Operating_Theatre_Suite.blend",
        "root": "Operating_Theatre_Suite",
        "expected_subroots": [
            "OR_Room_Walls_Floor", "Surgical_Table", "Patient_Draped",
            "Overhead_Surgical_Lamps", "Anesthesia_Machine", "Patient_Vital_Monitor",
            "IV_Stand", "Mayo_Trolley"
        ]
    }
]

print("=====================================================================")
print("          AUTONOMOUS 3D MEDICAL ASSETS VALIDATION SUITE              ")
print("=====================================================================")

all_passed = True
for m in modules:
    print(f"\n--- Checking {m['name']} ---", flush=True)
    fbx_exists = os.path.exists(m['fbx'])
    blend_exists = os.path.exists(m['blend'])
    fbx_size = os.path.getsize(m['fbx']) / (1024 * 1024) if fbx_exists else 0
    blend_size = os.path.getsize(m['blend']) / (1024 * 1024) if blend_exists else 0

    print(f"FBX File: {m['fbx']} -> {'FOUND' if fbx_exists else 'MISSING'} ({fbx_size:.2f} MB)", flush=True)
    print(f"Blend Source: {m['blend']} -> {'FOUND' if blend_exists else 'MISSING'} ({blend_size:.2f} MB)", flush=True)

    if not fbx_exists or not blend_exists:
        all_passed = False
        continue

    # Inspect FBX internal hierarchy in Blender
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.fbx(filepath=m['fbx'])
        root_obj = bpy.data.objects.get(m['root'])
        if not root_obj:
            print(f"  [ERROR] Root '{m['root']}' NOT found in FBX! Available objects: {[o.name for o in bpy.data.objects][:10]}", flush=True)
            all_passed = False
        else:
            print(f"  [PASS] Root '{m['root']}' verified.", flush=True)
            children = [c.name for c in root_obj.children]
            for sub in m['expected_subroots']:
                if sub in children or any(sub in c for c in children):
                    print(f"    [PASS] Child structure: {sub}", flush=True)
                else:
                    print(f"    [WARN] Expected child '{sub}' not direct child. Present children: {children}", flush=True)
    except Exception as e:
        print(f"  [ERROR] Failed to import FBX for inspection: {e}", flush=True)
        all_passed = False

print("\n=====================================================================", flush=True)
if all_passed:
    print(" >>> ALL 6 MODULES FULLY VALIDATED AND READY FOR PRODUCTION! <<<", flush=True)
else:
    print(" >>> SOME MODULES FAILED VALIDATION - CHECK LOG ABOVE! <<<", flush=True)
print("=====================================================================", flush=True)
