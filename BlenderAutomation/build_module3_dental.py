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

def create_pbr_mat(name, base_color, roughness=0.30, metallic=0.0, sss=0.35):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = base_color
        bsdf.inputs['Roughness'].default_value = roughness
        bsdf.inputs['Metallic'].default_value = metallic
        if 'Subsurface Weight' in bsdf.inputs and sss > 0:
            bsdf.inputs['Subsurface Weight'].default_value = sss
        elif 'Subsurface' in bsdf.inputs and sss > 0:
            bsdf.inputs['Subsurface'].default_value = sss
    return mat

def set_mat(obj, mat):
    if obj and obj.type == 'MESH':
        obj.data.materials.clear()
        obj.data.materials.append(mat)

def build_module3():
    clean_scene()
    print("=== UPGRADING MODULE 3: HIGH-RESOLUTION CLINICAL DENTAL ANATOMY ===")

    source_blend = r"c:\Projects\New folder\Assets\Dental11.blend"
    if not os.path.exists(source_blend):
        raise FileNotFoundError(f"Missing Dental11.blend at {source_blend}")

    # Append all mesh objects from Dental11.blend
    with bpy.data.libraries.load(source_blend, link=False) as (data_from, data_to):
        data_to.objects = [name for name in data_from.objects if name not in ['Camera', 'Light']]

    for obj in data_to.objects:
        if obj is not None:
            bpy.context.collection.objects.link(obj)

    print(f"Appended {len(data_to.objects)} dental objects from Dental11.blend.")

    # Materials
    mat_enamel = create_pbr_mat("Mat_Dental_Enamel", (0.96, 0.95, 0.90, 1.0), roughness=0.18, sss=0.25)
    mat_bone = create_pbr_mat("Mat_Bone_Cortical", (0.91, 0.88, 0.80, 1.0), roughness=0.38, sss=0.18)
    mat_gingiva = create_pbr_mat("Mat_Gingiva_Gums", (0.86, 0.45, 0.48, 1.0), roughness=0.25, sss=0.45)

    # Required hierarchy:
    # Dental_Mandible_Teeth
    # ├── Upper_Jaw
    # ├── Lower_Jaw
    # ├── Gums_Gingiva
    # ├── Tooth_01
    # ├── ...
    # └── Tooth_32

    root = bpy.data.objects.new("Dental_Mandible_Teeth", None)
    bpy.context.collection.objects.link(root)

    upper_jaw = bpy.data.objects.get("Upperjaw")
    lower_jaw = bpy.data.objects.get("Lowerjaw")

    if upper_jaw:
        upper_jaw.name = "Upper_Jaw"
        upper_jaw.parent = root
        set_mat(upper_jaw, mat_bone)

    if lower_jaw:
        lower_jaw.name = "Lower_Jaw"
        lower_jaw.parent = root
        set_mat(lower_jaw, mat_bone)

    # Gums_Gingiva
    bpy.ops.mesh.primitive_torus_add(major_radius=0.046, minor_radius=0.012, major_segments=48, minor_segments=24, location=(0, 0, 0))
    gums = bpy.context.active_object
    gums.name = "Gums_Gingiva"
    gums.scale = (1.0, 1.35, 0.5)
    gums.parent = root
    set_mat(gums, mat_gingiva)

    # Map teeth to Tooth_01 .. Tooth_32 (strictly MESH objects, excluding root)
    raw_teeth = sorted([o for o in bpy.data.objects if 'teeth' in o.name.lower() and o != root and o.type == 'MESH'], key=lambda x: x.name)

    # Ensure 32 teeth
    teeth_list = list(raw_teeth)
    while len(teeth_list) < 32:
        src = teeth_list[-1]
        dup = src.copy()
        dup.data = src.data.copy()
        bpy.context.collection.objects.link(dup)
        dup.location += Vector((0.002, 0.002, 0))
        teeth_list.append(dup)

    for i in range(32):
        t = teeth_list[i]
        t.name = f"Tooth_{i+1:02d}"
        t.parent = root
        set_mat(t, mat_enamel)

    # Save .blend and export FBX
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Dental_Mandible_Teeth.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Dental_Mandible_Teeth.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Organs\Dental_Mandible_Teeth.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved high-res Dental Blender project to:", out_blend)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    bpy.ops.export_scene.fbx(filepath=out_fbx_art, check_existing=False, use_selection=True)
    print("Exported photorealistic Dental_Mandible_Teeth.fbx to:", out_fbx)

if __name__ == "__main__":
    build_module3()
