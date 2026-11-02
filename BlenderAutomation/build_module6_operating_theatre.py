import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0

def build_module6():
    clean_scene()
    print("=== BUILDING MODULE 6: VIRTUAL OPERATING THEATRE SUITE ===")

    existing_or_fbx = r"Assets\MedicalSimulation\Art\OperatingTheatre\Operating_Theatre_Suite.fbx"
    patient_fbx = r"Assets\MedicalSimulation\Art\Characters\Character_Patient_OperatingTable.fbx"

    # Root object
    root = bpy.data.objects.new("Operating_Theatre_Suite", None)
    bpy.context.collection.objects.link(root)

    # 1. Required child parent empties / groups
    grp_room = bpy.data.objects.new("OR_Room_Walls_Floor", None)
    grp_table = bpy.data.objects.new("Surgical_Table", None)
    grp_patient = bpy.data.objects.new("Patient_Draped", None)
    grp_lamps = bpy.data.objects.new("Overhead_Surgical_Lamps", None)
    grp_anesthesia = bpy.data.objects.new("Anesthesia_Machine", None)
    grp_monitor = bpy.data.objects.new("Patient_Vital_Monitor", None)
    grp_iv = bpy.data.objects.new("IV_Stand", None)
    grp_mayo = bpy.data.objects.new("Mayo_Trolley", None)

    for g in [grp_room, grp_table, grp_patient, grp_lamps, grp_anesthesia, grp_monitor, grp_iv, grp_mayo]:
        bpy.context.collection.objects.link(g)
        g.parent = root

    # Import existing OR suite
    if os.path.exists(existing_or_fbx):
        print("Importing existing OR Suite components...")
        bpy.ops.import_scene.fbx(filepath=existing_or_fbx)
        imported = [o for o in bpy.context.selected_objects if o != root and o.parent is None]
        print(f"Imported {len(imported)} top-level OR objects.")

        for o in imported:
            name_lower = o.name.lower()
            if "light" in name_lower or "lamp" in name_lower or "arm" in name_lower:
                o.parent = grp_lamps
            elif "table" in name_lower:
                o.parent = grp_table
            elif "anesthesia" in name_lower:
                o.parent = grp_anesthesia
            elif "monitor" in name_lower or "carrier" in name_lower:
                o.parent = grp_monitor
            elif "infusion" in name_lower or "pole" in name_lower:
                o.parent = grp_iv
            elif "cart" in name_lower or "trolley" in name_lower or "tray" in name_lower or "wheel" in name_lower:
                o.parent = grp_mayo
            else:
                o.parent = grp_room

    # Import Patient Draped
    if os.path.exists(patient_fbx):
        print("Importing Patient Draped model...")
        bpy.ops.import_scene.fbx(filepath=patient_fbx)
        pat_objs = [o for o in bpy.context.selected_objects if o not in [root, grp_patient] and o.parent is None]
        for po in pat_objs:
            po.parent = grp_patient
            # Align patient height to surgical tabletop (Y=up or Z=up)
            po.location = (0, 0, 0.85)

    # -------------------------------------------------------------
    # EXPORT FBX & SAVE .BLEND SOURCE
    # -------------------------------------------------------------
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Operating_Theatre_Suite.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Operating_Theatre_Suite.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\OperatingTheatre\Operating_Theatre_Suite_Clean.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved editable Blender project to:", out_blend)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    print("Module 6 FBX exported successfully to:", out_fbx)

if __name__ == "__main__":
    build_module6()
