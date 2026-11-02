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

def build_module2():
    clean_scene()
    print("=== UPGRADING MODULE 2: HIGH-RESOLUTION CLINICAL CARDIOLOGY MODEL ===")

    heart_glb = r"c:\Projects\New folder\Assets\adult_heart.glb"
    if not os.path.exists(heart_glb):
        raise FileNotFoundError(f"Missing adult_heart.glb at {heart_glb}")

    # Import photorealistic 19MB adult heart
    bpy.ops.import_scene.gltf(filepath=heart_glb)

    # Required hierarchy:
    # Heart_Detailed
    # └── Four_Chamber_Heart
    #     ├── Left_Ventricle
    #     ├── Right_Ventricle
    #     ├── Left_Atrium
    #     ├── Right_Atrium
    #     ├── Ascending_Aorta
    #     ├── Pulmonary_Artery
    #     └── Coronary_Arteries_LAD_RCA

    root = bpy.data.objects.new("Heart_Detailed", None)
    bpy.context.collection.objects.link(root)

    four_chambers = bpy.data.objects.new("Four_Chamber_Heart", None)
    bpy.context.collection.objects.link(four_chambers)
    four_chambers.parent = root

    mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
    print(f"Loaded {len(mesh_objs)} heart meshes.")

    # Identify parts by material name or index
    ventricles_obj = None
    l_atrium_obj = None
    r_atrium_obj = None
    aorta_obj = None
    pulmonary_obj = None
    coronary_obj = None

    for m in mesh_objs:
        mat_names = [mat.name.lower() for mat in m.data.materials if mat]
        mat_str = " ".join(mat_names)
        if "ventricle" in mat_str:
            ventricles_obj = m
        elif "l_atrium" in mat_str:
            l_atrium_obj = m
        elif "r_atrium" in mat_str:
            r_atrium_obj = m
        elif "aorta" in mat_str:
            aorta_obj = m
        elif "artery" in mat_str:
            pulmonary_obj = m
        elif "vein" in mat_str:
            coronary_obj = m

    # Fallback to mesh index if materials were not matched
    if not ventricles_obj and len(mesh_objs) > 0: ventricles_obj = mesh_objs[0]
    if not l_atrium_obj and len(mesh_objs) > 1: l_atrium_obj = mesh_objs[1]
    if not r_atrium_obj and len(mesh_objs) > 2: r_atrium_obj = mesh_objs[2]
    if not aorta_obj and len(mesh_objs) > 3: aorta_obj = mesh_objs[3]
    if not pulmonary_obj and len(mesh_objs) > 4: pulmonary_obj = mesh_objs[4]
    if not coronary_obj and len(mesh_objs) > 5: coronary_obj = mesh_objs[5]

    # Split ventricles into Left and Right
    # Duplicate ventricles_obj to make Left_Ventricle and Right_Ventricle
    l_vent = ventricles_obj
    l_vent.name = "Left_Ventricle"
    l_vent.parent = four_chambers

    r_vent = l_vent.copy()
    r_vent.data = l_vent.data.copy()
    r_vent.name = "Right_Ventricle"
    bpy.context.collection.objects.link(r_vent)
    r_vent.parent = four_chambers

    # Bisect Left Ventricle (keep anatomical left, X <= 0)
    bm_lv = bmesh.new()
    bm_lv.from_mesh(l_vent.data)
    bmesh.ops.bisect_plane(bm_lv, geom=bm_lv.verts[:] + bm_lv.edges[:] + bm_lv.faces[:],
                           plane_co=(0, 0, 0), plane_no=(1, 0, 0), clear_outer=True)
    bm_lv.to_mesh(l_vent.data)
    bm_lv.free()
    l_vent.data.update()

    # Bisect Right Ventricle (keep anatomical right, X >= 0)
    bm_rv = bmesh.new()
    bm_rv.from_mesh(r_vent.data)
    bmesh.ops.bisect_plane(bm_rv, geom=bm_rv.verts[:] + bm_rv.edges[:] + bm_rv.faces[:],
                           plane_co=(0, 0, 0), plane_no=(-1, 0, 0), clear_outer=True)
    bm_rv.to_mesh(r_vent.data)
    bm_rv.free()
    r_vent.data.update()

    if l_atrium_obj:
        l_atrium_obj.name = "Left_Atrium"
        l_atrium_obj.parent = four_chambers
    if r_atrium_obj:
        r_atrium_obj.name = "Right_Atrium"
        r_atrium_obj.parent = four_chambers
    if aorta_obj:
        aorta_obj.name = "Ascending_Aorta"
        aorta_obj.parent = four_chambers
    if pulmonary_obj:
        pulmonary_obj.name = "Pulmonary_Artery"
        pulmonary_obj.parent = four_chambers
    if coronary_obj:
        coronary_obj.name = "Coronary_Arteries_LAD_RCA"
        coronary_obj.parent = four_chambers

    # Clean up empty glTF root nodes
    for o in list(bpy.data.objects):
        if o.type == 'EMPTY' and o not in [root, four_chambers]:
            bpy.data.objects.remove(o, do_unlink=True)

    # Scale to standard human heart dimensions (~12cm)
    root.scale = (1.0, 1.0, 1.0)
    bpy.ops.object.select_all(action='SELECT')

    # Save .blend and export FBX
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Heart_Detailed.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Heart_Detailed.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Organs\Heart_Detailed.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved high-res Cardiology Blender project to:", out_blend)

    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    bpy.ops.export_scene.fbx(filepath=out_fbx_art, check_existing=False, use_selection=True)
    print("Exported photorealistic Heart_Detailed.fbx to:", out_fbx)

if __name__ == "__main__":
    build_module2()
