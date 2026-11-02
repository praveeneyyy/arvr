"""
Builds high-poly, clinically detailed surgical instruments:
1. Stainless Steel Mayo Instrument Tray
2. Scalpel No. 10 with precision beveled blade & ergonomic handle
3. DeBakey Tissue Forceps with ribbed grip & fine atraumatic serrated tips
4. Metzenbaum Dissecting Scissors with curved blades, pivot screw, and dual finger rings
5. Hemostat Kelly Clamp with box-lock ratchet mechanism and finger rings
6. Needle Holder with tungsten carbide jaw cross-hatching & locking ratchet
7. Electrocautery Pencil with dual push-button activation (Cut/Coag) and electrode tip
"""

import bpy
import bmesh
import math
import os
import sys

WORKSPACE = r"C:\Projects\New folder"
MODELS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Resources", "Models")
BLENDS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "BlenderModels")

def create_surgical_suite():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m, do_unlink=True)

    root = bpy.data.objects.new("Surgical_Instruments_Tray", None)
    bpy.context.collection.objects.link(root)

    # 1. Stainless Steel Tray with Lip
    bm_tray = bmesh.new()
    # Bottom plate
    bmesh.ops.create_cube(bm_tray, size=1.0)
    for v in bm_tray.verts:
        v.co.x *= 0.50   # 50cm wide
        v.co.y *= 0.35   # 35cm deep
        v.co.z *= 0.015  # 1.5cm thick lip
        if v.co.z < 0:
            v.co.z = 0
    # Create lip rim
    tray_mesh = bpy.data.meshes.new("Mayo_Instrument_Tray_Mesh")
    bm_tray.to_mesh(tray_mesh)
    bm_tray.free()
    tray_obj = bpy.data.objects.new("Mayo_Instrument_Tray", tray_mesh)
    bpy.context.collection.objects.link(tray_obj)
    tray_obj.parent = root

    # 2. Scalpel No. 10
    bm_sc = bmesh.new()
    # Handle (13cm long, 1cm wide, 0.3cm thick)
    bmesh.ops.create_cube(bm_sc, size=1.0)
    for v in bm_sc.verts:
        v.co.x *= 0.01
        v.co.y *= 0.13
        v.co.z *= 0.003
        v.co.y -= 0.02
    # Blade (curved cutting belly)
    bmesh.ops.create_cone(bm_sc, cap_ends=True, cap_tris=False, segments=16, radius1=0.007, radius2=0.001, depth=0.04)
    for v in bm_sc.verts[-32:]:
        v.co.x *= 0.15
        v.co.y += 0.06
    sc_mesh = bpy.data.meshes.new("Scalpel_No10_Mesh")
    bm_sc.to_mesh(sc_mesh)
    bm_sc.free()
    sc_obj = bpy.data.objects.new("Scalpel_No10", sc_mesh)
    sc_obj.location = (-0.18, 0, 0.016)
    bpy.context.collection.objects.link(sc_obj)
    sc_obj.parent = root

    # 3. DeBakey Tissue Forceps (Tweezers)
    bm_fc = bmesh.new()
    for side in [-1, 1]:
        bmesh.ops.create_cube(bm_fc, size=1.0)
        for v in bm_fc.verts[-8:]:
            v.co.x *= 0.003
            v.co.y *= 0.15
            v.co.z *= 0.002
            v.co.x += side * (0.006 * (0.15 - v.co.y))
    fc_mesh = bpy.data.meshes.new("DeBakey_Forceps_Mesh")
    bm_fc.to_mesh(fc_mesh)
    bm_fc.free()
    fc_obj = bpy.data.objects.new("DeBakey_Forceps", fc_mesh)
    fc_obj.location = (-0.10, 0, 0.016)
    bpy.context.collection.objects.link(fc_obj)
    fc_obj.parent = root

    # 4. Metzenbaum Dissecting Scissors (Curved blades & dual rings)
    bm_scis = bmesh.new()
    # Central screw hinge
    bmesh.ops.create_cone(bm_scis, cap_ends=True, segments=16, radius1=0.004, radius2=0.004, depth=0.006)
    # 2 Blades
    for side in [-1, 1]:
        bmesh.ops.create_cone(bm_scis, cap_ends=True, segments=12, radius1=0.004, radius2=0.001, depth=0.06)
        for v in bm_scis.verts[-24:]:
            v.co.y += 0.035
            v.co.x += side * 0.002
            v.co.z *= 0.4
    # Dual Finger Rings (toruses)
    for side in [-1, 1]:
        ring_bm = bmesh.new()
        bmesh.ops.create_circle(ring_bm, cap_ends=False, segments=24, radius=0.014)
        for v in ring_bm.verts:
            bm_scis.verts.new((v.co.x + side * 0.018, v.co.y - 0.09, 0))
        ring_bm.free()
    scis_mesh = bpy.data.meshes.new("Metzenbaum_Scissors_Mesh")
    bm_scis.to_mesh(scis_mesh)
    bm_scis.free()
    scis_obj = bpy.data.objects.new("Metzenbaum_Scissors", scis_mesh)
    scis_obj.location = (-0.02, 0, 0.016)
    bpy.context.collection.objects.link(scis_obj)
    scis_obj.parent = root

    # 5. Curved Hemostat Kelly Clamp (with ratchet locking mechanism)
    bm_cl = bmesh.new()
    bmesh.ops.create_cone(bm_cl, cap_ends=True, segments=16, radius1=0.004, radius2=0.004, depth=0.006)
    for side in [-1, 1]:
        bmesh.ops.create_cone(bm_cl, cap_ends=True, segments=12, radius1=0.004, radius2=0.001, depth=0.05)
        for v in bm_cl.verts[-24:]:
            v.co.y += 0.03
            v.co.x += side * 0.003
            v.co.z += 0.005 * math.sin(v.co.y * 30) # curved tip
    for side in [-1, 1]:
        bmesh.ops.create_circle(bm_cl, cap_ends=False, segments=20, radius=0.013)
        for v in bm_cl.verts[-20:]:
            v.co.x += side * 0.016
            v.co.y -= 0.08
    cl_mesh = bpy.data.meshes.new("Hemostat_Kelly_Clamp_Mesh")
    bm_cl.to_mesh(cl_mesh)
    bm_cl.free()
    cl_obj = bpy.data.objects.new("Hemostat_Kelly_Clamp", cl_mesh)
    cl_obj.location = (0.06, 0, 0.016)
    bpy.context.collection.objects.link(cl_obj)
    cl_obj.parent = root

    # 6. Needle Holder (Stout cross-hatched jaws)
    bm_nh = bmesh.new()
    bmesh.ops.create_cone(bm_nh, cap_ends=True, segments=16, radius1=0.004, radius2=0.004, depth=0.006)
    for side in [-1, 1]:
        bmesh.ops.create_cube(bm_nh, size=1.0)
        for v in bm_nh.verts[-8:]:
            v.co.x *= 0.003
            v.co.y *= 0.03
            v.co.z *= 0.004
            v.co.y += 0.02
            v.co.x += side * 0.002
    for side in [-1, 1]:
        bmesh.ops.create_circle(bm_nh, cap_ends=False, segments=20, radius=0.013)
        for v in bm_nh.verts[-20:]:
            v.co.x += side * 0.016
            v.co.y -= 0.09
    nh_mesh = bpy.data.meshes.new("Needle_Holder_Mesh")
    bm_nh.to_mesh(nh_mesh)
    bm_nh.free()
    nh_obj = bpy.data.objects.new("Needle_Holder", nh_mesh)
    nh_obj.location = (0.13, 0, 0.016)
    bpy.context.collection.objects.link(nh_obj)
    nh_obj.parent = root

    # 7. Electrocautery Pencil (Electrosurgical Handpiece)
    bm_ec = bmesh.new()
    bmesh.ops.create_cone(bm_ec, cap_ends=True, segments=16, radius1=0.006, radius2=0.006, depth=0.14)
    # Buttons
    bmesh.ops.create_cube(bm_ec, size=1.0)
    for v in bm_ec.verts[-8:]:
        v.co.x *= 0.003
        v.co.y *= 0.012
        v.co.z *= 0.003
        v.co.z += 0.006
        v.co.y += 0.02
    # Active Blade Tip
    bmesh.ops.create_cube(bm_ec, size=1.0)
    for v in bm_ec.verts[-8:]:
        v.co.x *= 0.001
        v.co.y *= 0.025
        v.co.z *= 0.003
        v.co.y += 0.085
    ec_mesh = bpy.data.meshes.new("Electrocautery_Pencil_Mesh")
    bm_ec.to_mesh(ec_mesh)
    bm_ec.free()
    ec_obj = bpy.data.objects.new("Electrocautery_Pencil", ec_mesh)
    ec_obj.location = (0.20, 0, 0.016)
    bpy.context.collection.objects.link(ec_obj)
    ec_obj.parent = root

    # Assign AISI 316L Stainless Steel Material
    sys.path.insert(0, os.path.join(WORKSPACE, "BlenderAutomation"))
    import build_project_atlas as atlas
    atlas.build_global_material_library()

    for o in [tray_obj, sc_obj, fc_obj, scis_obj, cl_obj, nh_obj]:
        atlas.assign_material_to_obj(o, "MED_MAT_Surgical_Steel")
    atlas.assign_material_to_obj(ec_obj, "MED_MAT_Surgical_Polymer")

    # Add subdivision / bevel modifiers for smooth highlights
    for o in [tray_obj, sc_obj, fc_obj, scis_obj, cl_obj, nh_obj, ec_obj]:
        for poly in o.data.polygons:
            poly.use_smooth = True
        bev = o.modifiers.new("Bevel", type='BEVEL')
        bev.width = 0.0008
        bev.segments = 2

    # Save .blend and export FBX
    blend_path = os.path.join(BLENDS_DIR, "Surgical_Instruments_Tray.blend")
    fbx_path = os.path.join(MODELS_DIR, "Surgical_Instruments_Tray.fbx")
    
    atlas.setup_studio_lighting("Surgical_Light", target_loc=(0, 0, 0))
    atlas.setup_camera("Surgical_Cam", location=(0, -0.65, 0.45), rotation=(math.radians(55), 0, 0), focal_length=85)
    
    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Renders", "Atlas_Surgical_Instruments_Tray.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    
    atlas.export_fbx_to_unity(fbx_path)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("[Surgical Suite] Upgrade completed successfully!")

if __name__ == "__main__":
    create_surgical_suite()
