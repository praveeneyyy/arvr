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

def create_pbr_mat(name, base_color, roughness=0.25, metallic=0.0, sss=0.35):
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

def build_module4():
    clean_scene()
    print("=== UPGRADING MODULE 4: VISIBLE HUMAN PROJECT HEPATIC & BILIARY ANATOMY ===")

    liver_glb = r"c:\Projects\New folder\Assets\Scenes\VH_M_Liver.glb"
    if not os.path.exists(liver_glb):
        raise FileNotFoundError(f"Missing VH_M_Liver.glb at {liver_glb}")

    # Import Visible Human Male Liver (13,930 vertex capsule + anatomical segments)
    bpy.ops.import_scene.gltf(filepath=liver_glb)

    mat_liver = create_pbr_mat("Mat_Liver_Hepatic", (0.42, 0.11, 0.08, 1.0), roughness=0.28, sss=0.35)
    mat_gallbladder = create_pbr_mat("Mat_Gallbladder_Bile", (0.09, 0.68, 0.22, 1.0), roughness=0.15, sss=0.45)
    mat_bile_duct = create_pbr_mat("Mat_Bile_Duct", (0.30, 0.85, 0.35, 1.0), roughness=0.20, sss=0.30)
    mat_portal_vein = create_pbr_mat("Mat_Portal_Vein", (0.12, 0.32, 0.82, 1.0), roughness=0.22, sss=0.25)
    mat_hepatic_artery = create_pbr_mat("Mat_Hepatic_Artery", (0.95, 0.08, 0.08, 1.0), roughness=0.20, sss=0.25)

    root = bpy.data.objects.new("Digestive_System", None)
    bpy.context.collection.objects.link(root)

    complex_root = bpy.data.objects.new("Hepatic_Biliary_Complex", None)
    bpy.context.collection.objects.link(complex_root)
    complex_root.parent = root

    # Group right lobe segments and left lobe segments
    r_objs = [o for o in bpy.data.objects if o.type == 'MESH' and ('right' in o.name.lower() or 'capsule' in o.name.lower() or 'diaphragm' in o.name.lower())]
    l_objs = [o for o in bpy.data.objects if o.type == 'MESH' and 'left' in o.name.lower()]
    c_objs = [o for o in bpy.data.objects if o.type == 'MESH' and 'caudate' in o.name.lower()]

    # Join right lobe components into Liver_Right_Lobe
    if r_objs:
        bpy.ops.object.select_all(action='DESELECT')
        for o in r_objs: o.select_set(True)
        bpy.context.view_layer.objects.active = r_objs[0]
        bpy.ops.object.join()
        r_lobe = bpy.context.active_object
        r_lobe.name = "Liver_Right_Lobe"
        r_lobe.parent = complex_root
        set_mat(r_lobe, mat_liver)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.085, location=(0.04, 0, 0))
        r_lobe = bpy.context.active_object
        r_lobe.name = "Liver_Right_Lobe"
        r_lobe.parent = complex_root
        set_mat(r_lobe, mat_liver)

    # Join left lobe components into Liver_Left_Lobe
    if l_objs:
        bpy.ops.object.select_all(action='DESELECT')
        for o in l_objs: o.select_set(True)
        bpy.context.view_layer.objects.active = l_objs[0]
        bpy.ops.object.join()
        l_lobe = bpy.context.active_object
        l_lobe.name = "Liver_Left_Lobe"
        l_lobe.parent = complex_root
        set_mat(l_lobe, mat_liver)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.065, location=(-0.04, 0, 0))
        l_lobe = bpy.context.active_object
        l_lobe.name = "Liver_Left_Lobe"
        l_lobe.parent = complex_root
        set_mat(l_lobe, mat_liver)

    # Caudate lobe
    if c_objs:
        c_lobe = c_objs[0]
        c_lobe.name = "Caudate_Lobe"
        c_lobe.parent = complex_root
        set_mat(c_lobe, mat_liver)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.028, location=(0, 0.04, 0.02))
        c_lobe = bpy.context.active_object
        c_lobe.name = "Caudate_Lobe"
        c_lobe.parent = complex_root
        set_mat(c_lobe, mat_liver)

    # Gallbladder_Bile (Realistic pear-shaped bile reservoir)
    curve_data = bpy.data.curves.new('GallbladderCurve', type='CURVE')
    curve_data.dimensions = '3D'
    polyline = curve_data.splines.new('BEZIER')
    polyline.bezier_points.add(3)
    pts = [
        Vector((0.028, -0.055, -0.035)),
        Vector((0.022, -0.035, -0.020)),
        Vector((0.014, -0.018, -0.015)),
        Vector((0.008, -0.008, -0.018))
    ]
    radii = [1.8, 1.4, 0.8, 0.4]
    for i, pt in enumerate(pts):
        polyline.bezier_points[i].co = pt
        polyline.bezier_points[i].handle_left = pt - Vector((0, 0.01, 0))
        polyline.bezier_points[i].handle_right = pt + Vector((0, 0.01, 0))
        polyline.bezier_points[i].radius = radii[i]

    curve_data.bevel_depth = 0.013
    curve_data.bevel_resolution = 12
    gall_obj = bpy.data.objects.new("Gallbladder_Bile", curve_data)
    bpy.context.collection.objects.link(gall_obj)
    bpy.context.view_layer.objects.active = gall_obj
    bpy.ops.object.convert(target='MESH')
    gall_obj = bpy.context.active_object
    gall_obj.parent = complex_root
    set_mat(gall_obj, mat_gallbladder)

    # Bile_Duct (Common Bile Duct descending with branches)
    bd_data = bpy.data.curves.new('BileDuctCurve', type='CURVE')
    bd_data.dimensions = '3D'
    bd_spline = bd_data.splines.new('BEZIER')
    bd_spline.bezier_points.add(2)
    bd_pts = [
        Vector((0.006, -0.005, -0.015)),
        Vector((0.004, -0.012, -0.038)),
        Vector((0.002, -0.018, -0.065))
    ]
    for i, pt in enumerate(bd_pts):
        bd_spline.bezier_points[i].co = pt
        bd_spline.bezier_points[i].handle_left = pt - Vector((0, 0, 0.008))
        bd_spline.bezier_points[i].handle_right = pt + Vector((0, 0, 0.008))
    bd_data.bevel_depth = 0.0035
    bd_data.bevel_resolution = 8
    bd_obj = bpy.data.objects.new("Bile_Duct", bd_data)
    bpy.context.collection.objects.link(bd_obj)
    bpy.context.view_layer.objects.active = bd_obj
    bpy.ops.object.convert(target='MESH')
    bd_obj = bpy.context.active_object
    bd_obj.parent = complex_root
    set_mat(bd_obj, mat_bile_duct)

    # Portal_Vein (Main venous trunk branching into right and left branches)
    pv_data = bpy.data.curves.new('PortalVeinCurve', type='CURVE')
    pv_data.dimensions = '3D'
    pv_main = pv_data.splines.new('BEZIER')
    pv_main.bezier_points.add(2)
    pv_pts = [
        Vector((-0.002, 0.008, -0.065)),
        Vector((-0.001, 0.010, -0.035)),
        Vector((0.000, 0.012, -0.012))
    ]
    for i, pt in enumerate(pv_pts):
        pv_main.bezier_points[i].co = pt
        pv_main.bezier_points[i].handle_left = pt - Vector((0, 0, 0.01))
        pv_main.bezier_points[i].handle_right = pt + Vector((0, 0, 0.01))
    pv_data.bevel_depth = 0.0065
    pv_data.bevel_resolution = 10
    pv_obj = bpy.data.objects.new("Portal_Vein", pv_data)
    bpy.context.collection.objects.link(pv_obj)
    bpy.context.view_layer.objects.active = pv_obj
    bpy.ops.object.convert(target='MESH')
    pv_obj = bpy.context.active_object
    pv_obj.parent = complex_root
    set_mat(pv_obj, mat_portal_vein)

    # Hepatic_Artery (Proper hepatic artery branching)
    ha_data = bpy.data.curves.new('HepaticArteryCurve', type='CURVE')
    ha_data.dimensions = '3D'
    ha_main = ha_data.splines.new('BEZIER')
    ha_main.bezier_points.add(2)
    ha_pts = [
        Vector((-0.010, -0.004, -0.065)),
        Vector((-0.008, -0.002, -0.035)),
        Vector((-0.005, 0.000, -0.014))
    ]
    for i, pt in enumerate(ha_pts):
        ha_main.bezier_points[i].co = pt
        ha_main.bezier_points[i].handle_left = pt - Vector((0, 0, 0.01))
        ha_main.bezier_points[i].handle_right = pt + Vector((0, 0, 0.01))
    ha_data.bevel_depth = 0.0040
    ha_data.bevel_resolution = 8
    ha_obj = bpy.data.objects.new("Hepatic_Artery", ha_data)
    bpy.context.collection.objects.link(ha_obj)
    bpy.context.view_layer.objects.active = ha_obj
    bpy.ops.object.convert(target='MESH')
    ha_obj = bpy.context.active_object
    ha_obj.parent = complex_root
    set_mat(ha_obj, mat_hepatic_artery)

    # Clean up unparented objects
    for o in list(bpy.data.objects):
        if o not in [root, complex_root, r_lobe, l_lobe, c_lobe, gall_obj, bd_obj, pv_obj, ha_obj]:
            bpy.data.objects.remove(o, do_unlink=True)

    # Export FBX & Save .blend
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Digestive_System.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Digestive_System.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Organs\Digestive_System.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved Visible Human Digestive Blender project to:", out_blend)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    bpy.ops.export_scene.fbx(filepath=out_fbx_art, check_existing=False, use_selection=True)
    print("Exported Visible Human Digestive_System.fbx to:", out_fbx)

if __name__ == "__main__":
    build_module4()
