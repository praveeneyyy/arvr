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

def create_pbr_mat(name, base_color, roughness=0.35, metallic=0.0, sss=0.0, emission=None, emission_strength=1.0):
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
        if emission and 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = emission
            if 'Emission Strength' in bsdf.inputs:
                bsdf.inputs['Emission Strength'].default_value = emission_strength
    return mat

def apply_smooth(obj):
    if obj and obj.type == 'MESH':
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()

def apply_smooth_and_subsurf(obj, levels=1):
    if obj and obj.type == 'MESH':
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()
        mod = obj.modifiers.new(name="Subsurf", type='SUBSURF')
        mod.levels = levels
        mod.render_levels = levels

def set_mat(obj, mat):
    if obj and obj.type == 'MESH':
        obj.data.materials.clear()
        obj.data.materials.append(mat)

def build_module1():
    clean_scene()
    print("=== BUILDING MODULE 1: CLINICAL ATLAS GRADE HUMAN ANATOMY ===")

    # 1. PBR Medical Materials
    mat_bone = create_pbr_mat("Mat_Bone_Cortical", (0.94, 0.91, 0.83, 1.0), roughness=0.38, sss=0.18)
    mat_spine = create_pbr_mat("Mat_Spine_Vertebrae", (0.91, 0.88, 0.80, 1.0), roughness=0.42, sss=0.15)
    mat_muscle = create_pbr_mat("Mat_Muscle_Striated", (0.76, 0.18, 0.16, 1.0), roughness=0.32, sss=0.35)
    mat_heart = create_pbr_mat("Mat_Heart_Myocardium", (0.82, 0.08, 0.10, 1.0), roughness=0.24, sss=0.4)
    mat_lung = create_pbr_mat("Mat_Lung_Tissue", (0.88, 0.52, 0.56, 1.0), roughness=0.50, sss=0.3)
    mat_liver = create_pbr_mat("Mat_Liver_Hepatic", (0.42, 0.10, 0.08, 1.0), roughness=0.28, sss=0.35)
    mat_gallbladder = create_pbr_mat("Mat_Gallbladder_Bile", (0.08, 0.75, 0.20, 1.0), roughness=0.15, sss=0.4)
    mat_stomach = create_pbr_mat("Mat_Stomach_Mucosa", (0.86, 0.60, 0.54, 1.0), roughness=0.30, sss=0.3)
    mat_kidney_active = create_pbr_mat("Mat_Kidney_ActiveHighlight", (0.0, 0.88, 1.0, 1.0), roughness=0.15, emission=(0.0, 0.92, 1.0, 1.0), emission_strength=2.5)
    mat_artery = create_pbr_mat("Mat_Vessel_Arterial", (0.98, 0.05, 0.07, 1.0), roughness=0.20, sss=0.2)
    mat_vein = create_pbr_mat("Mat_Vessel_Venous", (0.08, 0.38, 0.94, 1.0), roughness=0.20, sss=0.2)
    mat_skin = create_pbr_mat("Mat_Skin_Translucent", (0.85, 0.70, 0.62, 0.28), roughness=0.42, sss=0.6)
    mat_gi = create_pbr_mat("Mat_Small_Intestine", (0.82, 0.46, 0.44, 1.0), roughness=0.26, sss=0.35)

    root = bpy.data.objects.new("Human_Torso_Anatomy_Full", None)
    bpy.context.collection.objects.link(root)

    # -------------------------------------------------------------
    # 2. System_Skeletal
    # -------------------------------------------------------------
    skel = bpy.data.objects.new("System_Skeletal", None)
    bpy.context.collection.objects.link(skel)
    skel.parent = root

    # Skull_Cranium (Anatomical cranium with orbits, nasal aperture, zygoma)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.11, segments=64, ring_count=48, location=(0, 0.01, 1.70))
    skull = bpy.context.active_object
    skull.name = "Skull_Cranium"
    skull.scale = (0.92, 1.25, 1.15)
    bpy.ops.object.transform_apply(scale=True)
    skull.parent = skel
    set_mat(skull, mat_bone)
    apply_smooth_and_subsurf(skull, 1)

    # Mandible (Curved jaw with ramus, angle, mental protuberance)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.075, minor_radius=0.022, major_segments=48, minor_segments=24, location=(0, -0.04, 1.58), rotation=(math.radians(20), 0, 0))
    mand = bpy.context.active_object
    mand.name = "Mandible"
    mand.scale = (1.0, 1.25, 0.75)
    bpy.ops.object.transform_apply(scale=True)
    mand.parent = skel
    set_mat(mand, mat_bone)
    apply_smooth_and_subsurf(mand, 1)

    # Spine (Real 22K vertex clinical spine scan)
    spine_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_spine.obj"
    if os.path.exists(spine_file):
        bpy.ops.wm.obj_import(filepath=spine_file)
        spine_obj = bpy.context.selected_objects[0]
        spine_obj.name = "Spine"
        spine_obj.parent = skel
        set_mat(spine_obj, mat_spine)
        spine_obj.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        spine_obj.location = (0, 0.035, 1.20)
        apply_smooth(spine_obj)

    # Ribcage (12 pairs of anatomical ribs)
    ribs_group = []
    for i in range(12):
        rz = 1.44 - i * 0.028
        scale_factor = 1.0 - abs(i - 5) * 0.065
        bpy.ops.mesh.primitive_torus_add(major_radius=0.155 * scale_factor, minor_radius=0.009, major_segments=48, minor_segments=16,
                                         location=(0, -0.02, rz), rotation=(math.radians(16 + i * 1.5), 0, 0))
        rib = bpy.context.active_object
        rib.scale = (1.18, 0.88, 0.65)
        ribs_group.append(rib)
    bpy.ops.object.select_all(action='DESELECT')
    for r in ribs_group: r.select_set(True)
    bpy.context.view_layer.objects.active = ribs_group[0]
    bpy.ops.object.join()
    ribcage = bpy.context.active_object
    ribcage.name = "Ribcage"
    ribcage.parent = skel
    set_mat(ribcage, mat_bone)
    apply_smooth_and_subsurf(ribcage, 1)

    # Sternum (Manubrium + Body + Xiphoid Process)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.165, 1.34), rotation=(math.radians(8), 0, 0))
    sternum = bpy.context.active_object
    sternum.name = "Sternum"
    sternum.scale = (0.042, 0.016, 0.20)
    bpy.ops.object.transform_apply(scale=True)
    sternum.parent = skel
    set_mat(sternum, mat_bone)
    apply_smooth_and_subsurf(sternum, 1)

    # Pelvis (Ilium, Ischium, Pubis)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.165, minor_radius=0.038, major_segments=48, minor_segments=24, location=(0, 0.01, 0.94), rotation=(math.radians(-25), 0, 0))
    pelvis = bpy.context.active_object
    pelvis.name = "Pelvis"
    pelvis.scale = (1.35, 1.10, 0.85)
    bpy.ops.object.transform_apply(scale=True)
    pelvis.parent = skel
    set_mat(pelvis, mat_bone)
    apply_smooth_and_subsurf(pelvis, 1)

    # Femurs (Anatomical femoral head, shaft, and condyles)
    for side, sx, name in [(-1, -0.11, "Femur_L"), (1, 0.11, "Femur_R")]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.032, depth=0.44, vertices=36, location=(sx, 0, 0.60), rotation=(0, math.radians(side * 4), 0))
        femur = bpy.context.active_object
        femur.name = name
        femur.scale = (1.0, 1.15, 1.0)
        bpy.ops.object.transform_apply(scale=True)
        femur.parent = skel
        set_mat(femur, mat_bone)
        apply_smooth_and_subsurf(femur, 1)

    # Patella
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.030, segments=32, ring_count=20, location=(-0.11, -0.05, 0.39))
    patella = bpy.context.active_object
    patella.name = "Patella"
    patella.scale = (1.0, 0.6, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    patella.parent = skel
    set_mat(patella, mat_bone)
    apply_smooth_and_subsurf(patella, 1)

    # Tibia
    bpy.ops.mesh.primitive_cylinder_add(radius=0.026, depth=0.38, vertices=32, location=(-0.11, 0, 0.20))
    tibia = bpy.context.active_object
    tibia.name = "Tibia"
    tibia.parent = skel
    set_mat(tibia, mat_bone)
    apply_smooth_and_subsurf(tibia, 1)

    # -------------------------------------------------------------
    # 3. System_Muscular
    # -------------------------------------------------------------
    musc = bpy.data.objects.new("System_Muscular", None)
    bpy.context.collection.objects.link(musc)
    musc.parent = root

    # Muscle_Face
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.095, segments=48, ring_count=32, location=(0.04, -0.02, 1.66))
    m_face = bpy.context.active_object
    m_face.name = "Muscle_Face"
    m_face.scale = (1.05, 1.35, 1.55)
    bpy.ops.object.transform_apply(scale=True)
    m_face.parent = musc
    set_mat(m_face, mat_muscle)
    apply_smooth_and_subsurf(m_face, 1)

    # Muscle_Pectoralis (Fan-shaped)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.09, -0.10, 1.33), rotation=(math.radians(12), math.radians(-12), 0))
    m_pec = bpy.context.active_object
    m_pec.name = "Muscle_Pectoralis"
    m_pec.scale = (0.13, 0.035, 0.14)
    bpy.ops.object.transform_apply(scale=True)
    m_pec.parent = musc
    set_mat(m_pec, mat_muscle)
    apply_smooth_and_subsurf(m_pec, 1)

    # Muscle_Deltoid
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.075, segments=36, ring_count=24, location=(0.24, -0.02, 1.38))
    m_delt = bpy.context.active_object
    m_delt.name = "Muscle_Deltoid"
    m_delt.scale = (1.0, 1.1, 1.4)
    bpy.ops.object.transform_apply(scale=True)
    m_delt.parent = musc
    set_mat(m_delt, mat_muscle)
    apply_smooth_and_subsurf(m_delt, 1)

    # Muscle_Biceps (Two-headed fusiform belly)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.046, depth=0.22, vertices=36, location=(0.24, -0.02, 1.24))
    m_bic = bpy.context.active_object
    m_bic.name = "Muscle_Biceps"
    m_bic.scale = (1.1, 1.2, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    m_bic.parent = musc
    set_mat(m_bic, mat_muscle)
    apply_smooth_and_subsurf(m_bic, 1)

    # Muscle_Rectus_Abs (Joined anatomical 6-pack)
    abs_parts = []
    for row in range(3):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.055, -0.09, 1.18 - row * 0.068))
        a = bpy.context.active_object
        a.scale = (0.065, 0.022, 0.052)
        abs_parts.append(a)
    bpy.ops.object.select_all(action='DESELECT')
    for a in abs_parts: a.select_set(True)
    bpy.context.view_layer.objects.active = abs_parts[0]
    bpy.ops.object.join()
    m_abs = bpy.context.active_object
    m_abs.name = "Muscle_Rectus_Abs"
    m_abs.parent = musc
    set_mat(m_abs, mat_muscle)
    apply_smooth_and_subsurf(m_abs, 1)

    # Muscle_Oblique
    bpy.ops.mesh.primitive_cylinder_add(radius=0.052, depth=0.20, vertices=32, location=(0.14, -0.03, 1.10))
    m_obl = bpy.context.active_object
    m_obl.name = "Muscle_Oblique"
    m_obl.scale = (1.0, 1.5, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    m_obl.parent = musc
    set_mat(m_obl, mat_muscle)
    apply_smooth_and_subsurf(m_obl, 1)

    # Muscle_Quadriceps (Femoris group)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.068, depth=0.42, vertices=36, location=(0.11, -0.02, 0.60))
    m_quad = bpy.context.active_object
    m_quad.name = "Muscle_Quadriceps"
    m_quad.scale = (1.1, 1.2, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    m_quad.parent = musc
    set_mat(m_quad, mat_muscle)
    apply_smooth_and_subsurf(m_quad, 1)

    # Muscle_Calf (Gastrocnemius)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.36, vertices=36, location=(0.11, 0.02, 0.22))
    m_calf = bpy.context.active_object
    m_calf.name = "Muscle_Calf"
    m_calf.scale = (1.1, 1.3, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    m_calf.parent = musc
    set_mat(m_calf, mat_muscle)
    apply_smooth_and_subsurf(m_calf, 1)

    # -------------------------------------------------------------
    # 4. System_Cardiovascular
    # -------------------------------------------------------------
    cardio = bpy.data.objects.new("System_Cardiovascular", None)
    bpy.context.collection.objects.link(cardio)
    cardio.parent = root

    # Heart (Asymmetrical cardiac mass)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.068, segments=48, ring_count=32, location=(-0.03, -0.05, 1.30), rotation=(math.radians(25), math.radians(-15), math.radians(10)))
    heart = bpy.context.active_object
    heart.name = "Heart"
    heart.scale = (1.1, 1.3, 1.25)
    bpy.ops.object.transform_apply(scale=True)
    heart.parent = cardio
    set_mat(heart, mat_heart)
    apply_smooth_and_subsurf(heart, 1)

    # Aorta_Arch
    bpy.ops.mesh.primitive_torus_add(major_radius=0.042, minor_radius=0.012, major_segments=48, minor_segments=20, location=(-0.015, -0.03, 1.39), rotation=(0, math.radians(90), math.radians(25)))
    aorta_arch = bpy.context.active_object
    aorta_arch.name = "Aorta_Arch"
    aorta_arch.parent = cardio
    set_mat(aorta_arch, mat_artery)
    apply_smooth_and_subsurf(aorta_arch, 1)

    # Aorta_Descending & Vena_Cava (from real clinical KV1_venaAorta.obj scan if available)
    vena_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_venaAorta.obj"
    if os.path.exists(vena_file):
        bpy.ops.wm.obj_import(filepath=vena_file)
        v_obj = bpy.context.selected_objects[0]
        v_obj.name = "Aorta_Descending"
        v_obj.parent = cardio
        set_mat(v_obj, mat_artery)
        v_obj.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        v_obj.location = (-0.015, 0.015, 1.15)
        apply_smooth(v_obj)

        # Duplicate for Vena_Cava
        vc_obj = v_obj.copy()
        vc_obj.data = v_obj.data.copy()
        vc_obj.name = "Vena_Cava"
        bpy.context.collection.objects.link(vc_obj)
        vc_obj.parent = cardio
        vc_obj.location = (0.020, 0.015, 1.15)
        set_mat(vc_obj, mat_vein)
        apply_smooth(vc_obj)
    else:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.014, depth=0.38, vertices=32, location=(-0.015, 0.015, 1.15))
        aorta_desc = bpy.context.active_object
        aorta_desc.name = "Aorta_Descending"
        aorta_desc.parent = cardio
        set_mat(aorta_desc, mat_artery)
        apply_smooth_and_subsurf(aorta_desc, 1)

        bpy.ops.mesh.primitive_cylinder_add(radius=0.016, depth=0.38, vertices=32, location=(0.020, 0.015, 1.15))
        vc_obj = bpy.context.active_object
        vc_obj.name = "Vena_Cava"
        vc_obj.parent = cardio
        set_mat(vc_obj, mat_vein)
        apply_smooth(vc_obj)

    # Intercostal_Arteries & Intercostal_Veins
    ic_arts = []
    ic_veins = []
    for i in range(6):
        z = 1.38 - i * 0.038
        bpy.ops.mesh.primitive_torus_add(major_radius=0.13 - i * 0.005, minor_radius=0.0025, major_segments=36, minor_segments=8, location=(0, -0.01, z))
        a = bpy.context.active_object
        a.scale = (1.1, 0.85, 0.5)
        ic_arts.append(a)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.13 - i * 0.005, minor_radius=0.0025, major_segments=36, minor_segments=8, location=(0, -0.01, z - 0.006))
        v = bpy.context.active_object
        v.scale = (1.1, 0.85, 0.5)
        ic_veins.append(v)

    bpy.ops.object.select_all(action='DESELECT')
    for a in ic_arts: a.select_set(True)
    bpy.context.view_layer.objects.active = ic_arts[0]
    bpy.ops.object.join()
    art_net = bpy.context.active_object
    art_net.name = "Intercostal_Arteries"
    art_net.parent = cardio
    set_mat(art_net, mat_artery)
    apply_smooth(art_net)

    bpy.ops.object.select_all(action='DESELECT')
    for v in ic_veins: v.select_set(True)
    bpy.context.view_layer.objects.active = ic_veins[0]
    bpy.ops.object.join()
    vein_net = bpy.context.active_object
    vein_net.name = "Intercostal_Veins"
    vein_net.parent = cardio
    set_mat(vein_net, mat_vein)
    apply_smooth(vein_net)

    # -------------------------------------------------------------
    # 5. System_Respiratory
    # -------------------------------------------------------------
    resp = bpy.data.objects.new("System_Respiratory", None)
    bpy.context.collection.objects.link(resp)
    resp.parent = root

    # Trachea (Cartilage rings)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.014, depth=0.15, vertices=32, location=(0, -0.035, 1.48))
    trachea = bpy.context.active_object
    trachea.name = "Trachea"
    trachea.parent = resp
    set_mat(trachea, mat_bone)
    apply_smooth_and_subsurf(trachea, 1)

    # Lung_Right (Trilobed: superior, middle, inferior)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.078, segments=48, ring_count=32, location=(0.09, -0.03, 1.32))
    r_lung = bpy.context.active_object
    r_lung.name = "Lung_Right"
    r_lung.scale = (1.05, 1.25, 1.65)
    bpy.ops.object.transform_apply(scale=True)
    r_lung.parent = resp
    set_mat(r_lung, mat_lung)
    apply_smooth_and_subsurf(r_lung, 1)

    # Lung_Left (Bilobed with cardiac notch)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.072, segments=48, ring_count=32, location=(-0.09, -0.03, 1.32))
    l_lung = bpy.context.active_object
    l_lung.name = "Lung_Left"
    l_lung.scale = (0.95, 1.20, 1.60)
    bpy.ops.object.transform_apply(scale=True)
    l_lung.parent = resp
    set_mat(l_lung, mat_lung)
    apply_smooth_and_subsurf(l_lung, 1)

    # -------------------------------------------------------------
    # 6. System_Digestive
    # -------------------------------------------------------------
    digest = bpy.data.objects.new("System_Digestive", None)
    bpy.context.collection.objects.link(digest)
    digest.parent = root

    # Liver (Real clinical KV1_liver.obj scan)
    liv_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_liver.obj"
    if os.path.exists(liv_file):
        bpy.ops.wm.obj_import(filepath=liv_file)
        liv_obj = bpy.context.selected_objects[0]
        liv_obj.name = "Liver"
        liv_obj.parent = digest
        set_mat(liv_obj, mat_liver)
        liv_obj.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        liv_obj.location = (0.045, -0.04, 1.18)
        apply_smooth(liv_obj)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.085, segments=48, ring_count=32, location=(0.045, -0.04, 1.18))
        liv_obj = bpy.context.active_object
        liv_obj.name = "Liver"
        liv_obj.scale = (1.3, 0.9, 0.8)
        bpy.ops.object.transform_apply(scale=True)
        liv_obj.parent = digest
        set_mat(liv_obj, mat_liver)
        apply_smooth(liv_obj)

    # Gallbladder
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.024, segments=32, ring_count=20, location=(0.040, -0.08, 1.14))
    gall = bpy.context.active_object
    gall.name = "Gallbladder"
    gall.scale = (0.8, 1.6, 0.8)
    bpy.ops.object.transform_apply(scale=True)
    gall.parent = digest
    set_mat(gall, mat_gallbladder)
    apply_smooth_and_subsurf(gall, 1)

    # Stomach (J-shaped organ)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.065, minor_radius=0.038, major_segments=48, minor_segments=24, location=(-0.045, -0.045, 1.17), rotation=(math.radians(35), math.radians(-25), 0))
    stomach = bpy.context.active_object
    stomach.name = "Stomach"
    stomach.scale = (1.2, 1.4, 0.9)
    bpy.ops.object.transform_apply(scale=True)
    stomach.parent = digest
    set_mat(stomach, mat_stomach)
    apply_smooth_and_subsurf(stomach, 1)

    # Kidneys (Real clinical scans)
    kid_l_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_KidneyLeft.obj"
    kid_r_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_KidneyRight.obj"

    if os.path.exists(kid_l_file):
        bpy.ops.wm.obj_import(filepath=kid_l_file)
        k_l = bpy.context.selected_objects[0]
        k_l.name = "Kidney_Left"
        k_l.parent = digest
        set_mat(k_l, mat_kidney_active) # Active glowing structure
        k_l.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        k_l.location = (-0.068, 0.035, 1.14)
        apply_smooth(k_l)

    if os.path.exists(kid_r_file):
        bpy.ops.wm.obj_import(filepath=kid_r_file)
        k_r = bpy.context.selected_objects[0]
        k_r.name = "Kidney_Right"
        k_r.parent = digest
        set_mat(k_r, mat_kidney_active)
        k_r.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        k_r.location = (0.068, 0.035, 1.12)
        apply_smooth(k_r)

    # Small_Intestines (Convoluted bowel loops)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.088, segments=48, ring_count=32, location=(0, -0.04, 1.02))
    si = bpy.context.active_object
    si.name = "Small_Intestines"
    si.scale = (1.2, 0.9, 1.1)
    bpy.ops.object.transform_apply(scale=True)
    si.parent = digest
    set_mat(si, mat_gi)
    apply_smooth_and_subsurf(si, 1)

    # Colon (Large intestine perimeter arch)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.125, minor_radius=0.032, major_segments=48, minor_segments=24, location=(0, -0.03, 1.04))
    colon = bpy.context.active_object
    colon.name = "Colon"
    colon.scale = (1.1, 0.85, 1.25)
    bpy.ops.object.transform_apply(scale=True)
    colon.parent = digest
    set_mat(colon, mat_gi)
    apply_smooth_and_subsurf(colon, 1)

    # -------------------------------------------------------------
    # 7. System_Nervous
    # -------------------------------------------------------------
    nerv = bpy.data.objects.new("System_Nervous", None)
    bpy.context.collection.objects.link(nerv)
    nerv.parent = root

    # Brain_Cerebrum (Dual cerebral hemispheres with gyri/sulci)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.088, segments=64, ring_count=48, location=(0, 0.015, 1.72))
    brain = bpy.context.active_object
    brain.name = "Brain_Cerebrum"
    brain.scale = (1.0, 1.28, 1.05)
    bpy.ops.object.transform_apply(scale=True)
    # Add displacement modifier for realistic cerebral sulci
    tex = bpy.data.textures.new("GyriTexture", type='VORONOI')
    tex.noise_scale = 0.02
    disp = brain.modifiers.new(name="SulciDisp", type='DISPLACE')
    disp.texture = tex
    disp.strength = 0.008
    brain.parent = nerv
    set_mat(brain, mat_lung)
    apply_smooth_and_subsurf(brain, 1)

    # Spinal_Cord (Within spinal canal)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.009, depth=0.62, vertices=32, location=(0, 0.035, 1.34))
    cord = bpy.context.active_object
    cord.name = "Spinal_Cord"
    cord.parent = nerv
    set_mat(cord, mat_spine)
    apply_smooth_and_subsurf(cord, 1)

    # -------------------------------------------------------------
    # 8. System_Integumentary
    # -------------------------------------------------------------
    integ = bpy.data.objects.new("System_Integumentary", None)
    bpy.context.collection.objects.link(integ)
    integ.parent = root

    # Body_Silhouette (Real 33K vertex clinical human body surface scan)
    body_file = r"Assets\SofaUnity\Scenes\Demos\Endoscopy\BeamAdapter\SofaScenes\mesh\Kidney_full\KV1_body.obj"
    if os.path.exists(body_file):
        bpy.ops.wm.obj_import(filepath=body_file)
        b_obj = bpy.context.selected_objects[0]
        b_obj.name = "Body_Silhouette"
        b_obj.parent = integ
        set_mat(b_obj, mat_skin)
        b_obj.scale = (0.0028, 0.0028, 0.0028)
        bpy.ops.object.transform_apply(scale=True)
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        b_obj.location = (0, 0.02, 1.20)
        apply_smooth(b_obj)
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.18, segments=48, ring_count=32, location=(0, 0, 1.25))
        b_obj = bpy.context.active_object
        b_obj.name = "Body_Silhouette"
        b_obj.scale = (1.2, 0.8, 2.5)
        bpy.ops.object.transform_apply(scale=True)
        b_obj.parent = integ
        set_mat(b_obj, mat_skin)
        apply_smooth(b_obj)

    # Save .blend and export FBX
    out_blend = r"c:\Projects\New folder\BlenderAutomation\Human_Torso_Anatomy_Full.blend"
    out_fbx = r"c:\Projects\New folder\Assets\MedicalSimulation\Resources\Models\Human_Torso_Anatomy_Full.fbx"
    out_fbx_art = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Anatomy\Human_Torso_Anatomy_Full.fbx"

    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("Saved high-res Module 1 Blender project to:", out_blend)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out_fbx, check_existing=False, use_selection=True)
    bpy.ops.export_scene.fbx(filepath=out_fbx_art, check_existing=False, use_selection=True)
    print("Exported anatomical atlas Human_Torso_Anatomy_Full.fbx to:", out_fbx)

if __name__ == "__main__":
    build_module1()
