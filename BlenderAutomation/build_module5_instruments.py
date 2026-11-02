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

def create_pbr_mat(name, base_color, roughness=0.18, metallic=0.92, spec=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = base_color
        bsdf.inputs['Roughness'].default_value = roughness
        bsdf.inputs['Metallic'].default_value = metallic
        if 'Specular IOR Level' in bsdf.inputs:
            bsdf.inputs['Specular IOR Level'].default_value = spec
        elif 'Specular' in bsdf.inputs:
            bsdf.inputs['Specular'].default_value = spec
    return mat

def set_mat(obj, mat):
    if obj and obj.type == 'MESH':
        obj.data.materials.clear()
        obj.data.materials.append(mat)

def apply_smooth(obj):
    if obj and obj.type == 'MESH':
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()

def build_module5():
    clean_scene()
    print("=== BUILDING MODULE 5: SURGICAL INSTRUMENTS TRAY ===")

    # Materials
    mat_steel = create_pbr_mat("Mat_Surgical_Steel", (0.85, 0.86, 0.88, 1.0), roughness=0.15, metallic=0.95)
    mat_gold_tc = create_pbr_mat("Mat_Gold_Tungsten_Carbide", (0.95, 0.78, 0.22, 1.0), roughness=0.20, metallic=0.90)
    mat_pencil_white = create_pbr_mat("Mat_Electrocautery_Body", (0.92, 0.93, 0.95, 1.0), roughness=0.35, metallic=0.0)
    mat_btn_cut = create_pbr_mat("Mat_Btn_Cut_Yellow", (0.95, 0.82, 0.10, 1.0), roughness=0.3, metallic=0.0)
    mat_btn_coag = create_pbr_mat("Mat_Btn_Coag_Blue", (0.10, 0.45, 0.95, 1.0), roughness=0.3, metallic=0.0)

    root = bpy.data.objects.new("Surgical_Instruments_Tray", None)
    bpy.context.collection.objects.link(root)

    # 1. Instrument_Tray (Stainless steel autoclave mayo tray: 36cm x 26cm x 2.5cm)
    # Beveled rimmed surgical tray
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.0125))
    tray = bpy.context.active_object
    tray.name = "Instrument_Tray"
    tray.scale = (0.36, 0.26, 0.025)
    bpy.ops.object.transform_apply(scale=True)
    # Add bevel modifier for medical rolled rim
    bev = tray.modifiers.new(name="Bevel", type='BEVEL')
    bev.width = 0.006
    bev.segments = 4
    tray.parent = root
    set_mat(tray, mat_steel)
    apply_smooth(tray)

    # 2. Scalpel_No10 (Handle #3 with curved belly blade #10)
    # Positioned at X = -0.12, Y = 0
    sc_handle_data = bpy.data.curves.new('ScalpelHandleCurve', type='CURVE')
    sc_handle_data.dimensions = '3D'
    sc_spline = sc_handle_data.splines.new('BEZIER')
    sc_spline.bezier_points.add(2)
    sc_pts = [
        Vector((-0.12, -0.07, 0.028)),
        Vector((-0.12, 0.00, 0.028)),
        Vector((-0.12, 0.05, 0.028))
    ]
    for i, p in enumerate(sc_pts):
        sc_spline.bezier_points[i].co = p
        sc_spline.bezier_points[i].handle_left = p - Vector((0, 0.02, 0))
        sc_spline.bezier_points[i].handle_right = p + Vector((0, 0.02, 0))
    sc_handle_data.bevel_depth = 0.004
    sc_handle_data.bevel_resolution = 4
    scalpel = bpy.data.objects.new("Scalpel_No10", sc_handle_data)
    bpy.context.collection.objects.link(scalpel)
    bpy.context.view_layer.objects.active = scalpel
    bpy.ops.object.convert(target='MESH')
    scalpel = bpy.context.active_object
    # Flatten handle slightly for ergonomic medical grip
    scalpel.scale = (0.5, 1.0, 1.8)
    bpy.ops.object.transform_apply(scale=True)
    scalpel.parent = root
    set_mat(scalpel, mat_steel)
    apply_smooth(scalpel)

    # 3. DeBakey_Forceps (Atraumatic vascular tissue forceps: 16cm spring shanks)
    # Positioned at X = -0.07, Y = 0
    fb_data = bpy.data.curves.new('ForcepsCurve', type='CURVE')
    fb_data.dimensions = '3D'
    # Left shank
    s_l = fb_data.splines.new('BEZIER')
    s_l.bezier_points.add(2)
    s_l.bezier_points[0].co = Vector((-0.072, -0.07, 0.028))
    s_l.bezier_points[0].handle_left = Vector((-0.072, -0.08, 0.028))
    s_l.bezier_points[0].handle_right = Vector((-0.075, -0.03, 0.028))
    s_l.bezier_points[1].co = Vector((-0.076, 0.00, 0.028))
    s_l.bezier_points[1].handle_left = Vector((-0.076, -0.02, 0.028))
    s_l.bezier_points[1].handle_right = Vector((-0.074, 0.03, 0.028))
    s_l.bezier_points[2].co = Vector((-0.071, 0.07, 0.028))
    s_l.bezier_points[2].handle_left = Vector((-0.072, 0.05, 0.028))
    s_l.bezier_points[2].handle_right = Vector((-0.071, 0.07, 0.028))

    # Right shank
    s_r = fb_data.splines.new('BEZIER')
    s_r.bezier_points.add(2)
    s_r.bezier_points[0].co = Vector((-0.068, -0.07, 0.028))
    s_r.bezier_points[0].handle_left = Vector((-0.068, -0.08, 0.028))
    s_r.bezier_points[0].handle_right = Vector((-0.065, -0.03, 0.028))
    s_r.bezier_points[1].co = Vector((-0.064, 0.00, 0.028))
    s_r.bezier_points[1].handle_left = Vector((-0.064, -0.02, 0.028))
    s_r.bezier_points[1].handle_right = Vector((-0.066, 0.03, 0.028))
    s_r.bezier_points[2].co = Vector((-0.069, 0.07, 0.028))
    s_r.bezier_points[2].handle_left = Vector((-0.068, 0.05, 0.028))
    s_r.bezier_points[2].handle_right = Vector((-0.069, 0.07, 0.028))

    fb_data.bevel_depth = 0.0022
    fb_data.bevel_resolution = 6
    forceps = bpy.data.objects.new("DeBakey_Forceps", fb_data)
    bpy.context.collection.objects.link(forceps)
    bpy.context.view_layer.objects.active = forceps
    bpy.ops.object.convert(target='MESH')
    forceps = bpy.context.active_object
    forceps.parent = root
    set_mat(forceps, mat_steel)
    apply_smooth(forceps)

    # 4. Metzenbaum_Scissors (Curved dissecting scissors with finger rings and box lock)
    # Positioned at X = -0.02, Y = 0
    scis_data = bpy.data.curves.new('MetzenbaumCurve', type='CURVE')
    scis_data.dimensions = '3D'
    # Shank A with curved blade
    sa = scis_data.splines.new('BEZIER')
    sa.bezier_points.add(2)
    sa.bezier_points[0].co = Vector((-0.028, -0.06, 0.028))
    sa.bezier_points[0].handle_left = Vector((-0.028, -0.07, 0.028))
    sa.bezier_points[0].handle_right = Vector((-0.022, -0.02, 0.028))
    sa.bezier_points[1].co = Vector((-0.020, 0.01, 0.028)) # Box lock pivot
    sa.bezier_points[1].handle_left = Vector((-0.020, 0.00, 0.028))
    sa.bezier_points[1].handle_right = Vector((-0.018, 0.03, 0.028))
    sa.bezier_points[2].co = Vector((-0.015, 0.07, 0.029)) # Curved tip
    sa.bezier_points[2].handle_left = Vector((-0.016, 0.05, 0.028))
    sa.bezier_points[2].handle_right = Vector((-0.015, 0.07, 0.029))

    # Shank B
    sb = scis_data.splines.new('BEZIER')
    sb.bezier_points.add(2)
    sb.bezier_points[0].co = Vector((-0.012, -0.06, 0.028))
    sb.bezier_points[0].handle_left = Vector((-0.012, -0.07, 0.028))
    sb.bezier_points[0].handle_right = Vector((-0.018, -0.02, 0.028))
    sb.bezier_points[1].co = Vector((-0.020, 0.01, 0.028))
    sb.bezier_points[1].handle_left = Vector((-0.020, 0.00, 0.028))
    sb.bezier_points[1].handle_right = Vector((-0.022, 0.03, 0.028))
    sb.bezier_points[2].co = Vector((-0.017, 0.07, 0.027))
    sb.bezier_points[2].handle_left = Vector((-0.019, 0.05, 0.028))
    sb.bezier_points[2].handle_right = Vector((-0.017, 0.07, 0.027))

    # Finger ring A
    ring_a = scis_data.splines.new('BEZIER')
    ring_a.bezier_points.add(3)
    ring_pts = [
        Vector((-0.028, -0.065, 0.028)),
        Vector((-0.035, -0.073, 0.028)),
        Vector((-0.028, -0.081, 0.028)),
        Vector((-0.021, -0.073, 0.028))
    ]
    for i, p in enumerate(ring_pts):
        ring_a.bezier_points[i].co = p
        ring_a.bezier_points[i].handle_left = p + Vector((-0.004, 0, 0))
        ring_a.bezier_points[i].handle_right = p + Vector((0.004, 0, 0))
    ring_a.use_cyclic_u = True

    # Finger ring B
    ring_b = scis_data.splines.new('BEZIER')
    ring_b.bezier_points.add(3)
    ring_pts_b = [
        Vector((-0.012, -0.065, 0.028)),
        Vector((-0.005, -0.073, 0.028)),
        Vector((-0.012, -0.081, 0.028)),
        Vector((-0.019, -0.073, 0.028))
    ]
    for i, p in enumerate(ring_pts_b):
        ring_b.bezier_points[i].co = p
        ring_b.bezier_points[i].handle_left = p + Vector((-0.004, 0, 0))
        ring_b.bezier_points[i].handle_right = p + Vector((0.004, 0, 0))
    ring_b.use_cyclic_u = True

    scis_data.bevel_depth = 0.0018
    scis_data.bevel_resolution = 6
    scissors = bpy.data.objects.new("Metzenbaum_Scissors", scis_data)
    bpy.context.collection.objects.link(scissors)
    bpy.context.view_layer.objects.active = scissors
    bpy.ops.object.convert(target='MESH')
    scissors = bpy.context.active_object
    scissors.parent = root
    set_mat(scissors, mat_steel)
    apply_smooth(scissors)

    # 5. Electrocautery_Pencil (Surgical Diathermy Pencil with Cut/Coag pushbuttons)
    # Positioned at X = 0.03, Y = 0
    bpy.ops.mesh.primitive_cylinder_add(radius=0.0055, depth=0.15, vertices=16, location=(0.03, 0.00, 0.03), rotation=(math.radians(90), 0, 0))
    cautery = bpy.context.active_object
    cautery.name = "Electrocautery_Pencil"
    cautery.scale = (0.9, 0.9, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    cautery.parent = root
    set_mat(cautery, mat_pencil_white)
    apply_smooth(cautery)

    # 6. Hemostat_Clamp (Curved Kelly hemostatic clamp with ratchet lock)
    # Positioned at X = 0.07, Y = 0
    hemo_data = bpy.data.curves.new('HemostatCurve', type='CURVE')
    hemo_data.dimensions = '3D'
    ha_s = hemo_data.splines.new('BEZIER')
    ha_s.bezier_points.add(2)
    ha_s.bezier_points[0].co = Vector((0.062, -0.05, 0.028))
    ha_s.bezier_points[0].handle_left = Vector((0.062, -0.06, 0.028))
    ha_s.bezier_points[0].handle_right = Vector((0.068, -0.01, 0.028))
    ha_s.bezier_points[1].co = Vector((0.070, 0.015, 0.028)) # hinge
    ha_s.bezier_points[1].handle_left = Vector((0.070, 0.005, 0.028))
    ha_s.bezier_points[1].handle_right = Vector((0.072, 0.030, 0.028))
    ha_s.bezier_points[2].co = Vector((0.078, 0.060, 0.029)) # curved jaw
    ha_s.bezier_points[2].handle_left = Vector((0.074, 0.045, 0.028))
    ha_s.bezier_points[2].handle_right = Vector((0.078, 0.060, 0.029))

    hb_s = hemo_data.splines.new('BEZIER')
    hb_s.bezier_points.add(2)
    hb_s.bezier_points[0].co = Vector((0.078, -0.05, 0.028))
    hb_s.bezier_points[0].handle_left = Vector((0.078, -0.06, 0.028))
    hb_s.bezier_points[0].handle_right = Vector((0.072, -0.01, 0.028))
    hb_s.bezier_points[1].co = Vector((0.070, 0.015, 0.028))
    hb_s.bezier_points[1].handle_left = Vector((0.070, 0.005, 0.028))
    hb_s.bezier_points[1].handle_right = Vector((0.068, 0.030, 0.028))
    hb_s.bezier_points[2].co = Vector((0.076, 0.060, 0.027))
    hb_s.bezier_points[2].handle_left = Vector((0.069, 0.045, 0.028))
    hb_s.bezier_points[2].handle_right = Vector((0.076, 0.060, 0.027))

    # Finger rings
    for rx, ox in [((-0.008), 0.062), ((0.008), 0.078)]:
        r_spline = hemo_data.splines.new('BEZIER')
        r_spline.bezier_points.add(3)
        r_pts = [
            Vector((ox, -0.055, 0.028)),
            Vector((ox + rx, -0.063, 0.028)),
            Vector((ox, -0.071, 0.028)),
            Vector((ox - rx, -0.063, 0.028))
        ]
        for i, p in enumerate(r_pts):
            r_spline.bezier_points[i].co = p
            r_spline.bezier_points[i].handle_left = p + Vector((-0.003, 0, 0))
            r_spline.bezier_points[i].handle_right = p + Vector((0.003, 0, 0))
        r_spline.use_cyclic_u = True

    hemo_data.bevel_depth = 0.0020
    hemo_data.bevel_resolution = 6
    hemostat = bpy.data.objects.new("Hemostat_Clamp", hemo_data)
    bpy.context.collection.objects.link(hemostat)
    bpy.context.view_layer.objects.active = hemostat
    bpy.ops.object.convert(target='MESH')
    hemostat = bpy.context.active_object
    hemostat.parent = root
    set_mat(hemostat, mat_steel)
    apply_smooth(hemostat)

    # 7. Needle_Holder (Mayo-Hegar needle driver with gold handles)
    # Positioned at X = 0.12, Y = 0
    nh_data = bpy.data.curves.new('NeedleHolderCurve', type='CURVE')
    nh_data.dimensions = '3D'
    na_s = nh_data.splines.new('BEZIER')
    na_s.bezier_points.add(2)
    na_s.bezier_points[0].co = Vector((0.112, -0.05, 0.028))
    na_s.bezier_points[0].handle_left = Vector((0.112, -0.06, 0.028))
    na_s.bezier_points[0].handle_right = Vector((0.118, -0.01, 0.028))
    na_s.bezier_points[1].co = Vector((0.120, 0.020, 0.028))
    na_s.bezier_points[1].handle_left = Vector((0.120, 0.010, 0.028))
    na_s.bezier_points[1].handle_right = Vector((0.120, 0.035, 0.028))
    na_s.bezier_points[2].co = Vector((0.119, 0.050, 0.028)) # short sturdy jaws
    na_s.bezier_points[2].handle_left = Vector((0.120, 0.040, 0.028))
    na_s.bezier_points[2].handle_right = Vector((0.119, 0.050, 0.028))

    nb_s = nh_data.splines.new('BEZIER')
    nb_s.bezier_points.add(2)
    nb_s.bezier_points[0].co = Vector((0.128, -0.05, 0.028))
    nb_s.bezier_points[0].handle_left = Vector((0.128, -0.06, 0.028))
    nb_s.bezier_points[0].handle_right = Vector((0.122, -0.01, 0.028))
    nb_s.bezier_points[1].co = Vector((0.120, 0.020, 0.028))
    nb_s.bezier_points[1].handle_left = Vector((0.120, 0.010, 0.028))
    nb_s.bezier_points[1].handle_right = Vector((0.120, 0.035, 0.028))
    nb_s.bezier_points[2].co = Vector((0.121, 0.050, 0.028))
    nb_s.bezier_points[2].handle_left = Vector((0.120, 0.040, 0.028))
    nb_s.bezier_points[2].handle_right = Vector((0.121, 0.050, 0.028))

    # Gold rings
    for rx, ox in [((-0.008), 0.112), ((0.008), 0.128)]:
        r_spline = nh_data.splines.new('BEZIER')
        r_spline.bezier_points.add(3)
        r_pts = [
            Vector((ox, -0.055, 0.028)),
            Vector((ox + rx, -0.063, 0.028)),
            Vector((ox, -0.071, 0.028)),
            Vector((ox - rx, -0.063, 0.028))
        ]
        for i, p in enumerate(r_pts):
            r_spline.bezier_points[i].co = p
            r_spline.bezier_points[i].handle_left = p + Vector((-0.003, 0, 0))
            r_spline.bezier_points[i].handle_right = p + Vector((0.003, 0, 0))
        r_spline.use_cyclic_u = True

    nh_data.bevel_depth = 0.0022
    nh_data.bevel_resolution = 6
    needle_holder = bpy.data.objects.new("Needle_Holder", nh_data)
    bpy.context.collection.objects.link(needle_holder)
    bpy.context.view_layer.objects.active = needle_holder
    bpy.ops.object.convert(target='MESH')
    needle_holder = bpy.context.active_object
    needle_holder.parent = root
    set_mat(needle_holder, mat_gold_tc)
    apply_smooth(needle_holder)

    # -------------------------------------------------------------
    # EXPORT FBX & SAVE .BLEND SOURCE
    # -------------------------------------------------------------
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Surgical_Instruments_Tray.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Surgical_Instruments_Tray.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Organs\Surgical_Instruments_Tray.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved editable Blender project to:", out_blend)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    bpy.ops.export_scene.fbx(filepath=out_fbx_art, check_existing=False, use_selection=True)
    print("Module 5 FBX exported successfully to:", out_fbx)

if __name__ == "__main__":
    build_module5()
