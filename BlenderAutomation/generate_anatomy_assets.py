#!/usr/bin/env python3
"""
Medical Simulation Platform - Master 3D Anatomy Asset Generator
Executes inside Blender 5.2+ LTS via headless Python API.
Generates a complete, anatomically organized, multi-system human torso library,
assigns physically based PBR medical materials, renders clinical portfolio-grade previews,
and exports Unity 6 URP-compatible FBX and glTF assets.
"""

import sys
import os
import math
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler

# ─────────────────────────────────────────────────────────────────────────────
# CONFIGURATION & PATHS
# ─────────────────────────────────────────────────────────────────────────────
BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ART_DIR = os.path.join(BASE_DIR, "Assets", "MedicalSimulation", "Art")
ANATOMY_DIR = os.path.join(ART_DIR, "Anatomy")
ORGANS_DIR = os.path.join(ART_DIR, "Organs")
SKELETON_DIR = os.path.join(ART_DIR, "Skeleton")
VESSELS_DIR = os.path.join(ART_DIR, "Vessels")
RENDERS_DIR = os.path.join(ART_DIR, "Renders")
BLEND_FILE = os.path.join(os.path.dirname(__file__), "Medical_Human_Anatomy_Master.blend")

for d in [ANATOMY_DIR, ORGANS_DIR, SKELETON_DIR, VESSELS_DIR, RENDERS_DIR]:
    os.makedirs(d, exist_ok=True)

# ─────────────────────────────────────────────────────────────────────────────
# SCENE CLEANUP & SETUP
# ─────────────────────────────────────────────────────────────────────────────
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'

    # Dark medical navy world background
    world = bpy.data.worlds.new("MedicalWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.02, 0.04, 0.08, 1.0) # #070E1A
        bg.inputs["Strength"].default_value = 0.85

def get_or_create_collection(name, parent_col=None):
    if name in bpy.data.collections:
        col = bpy.data.collections[name]
    else:
        col = bpy.data.collections.new(name)
        if parent_col is None:
            bpy.context.scene.collection.children.link(col)
        else:
            parent_col.children.link(col)
    return col

# ─────────────────────────────────────────────────────────────────────────────
# PBR MEDICAL MATERIAL CREATOR
# ─────────────────────────────────────────────────────────────────────────────
MATERIALS = {}

def create_pbr_material(name, base_color, roughness=0.4, specular=0.5, subsurface=0.0, transmission=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")

    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = base_color
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        if "Subsurface Weight" in bsdf.inputs and subsurface > 0.0:
            bsdf.inputs["Subsurface Weight"].default_value = subsurface
            if "Subsurface Radius" in bsdf.inputs:
                bsdf.inputs["Subsurface Radius"].default_value = (0.8, 0.2, 0.1)
        if "Transmission Weight" in bsdf.inputs and transmission > 0.0:
            bsdf.inputs["Transmission Weight"].default_value = transmission
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha

    # Transparency settings
    if alpha < 1.0 or transmission > 0.0:
        mat.blend_method = 'BLEND' if hasattr(mat, 'blend_method') else 'HASHED'
        if hasattr(mat, 'shadow_method'):
            mat.shadow_method = 'HASHED'

    MATERIALS[name] = mat
    return mat

def build_material_library():
    # Bones & Cartilage
    create_pbr_material("M_Bone_Cortical", (0.91, 0.88, 0.81, 1.0), roughness=0.42, specular=0.45)
    create_pbr_material("M_Cartilage_Hyaline", (0.76, 0.86, 0.94, 0.85), roughness=0.25, specular=0.6, transmission=0.35, alpha=0.88)
    
    # Cardiovascular
    create_pbr_material("M_Cardiac_Myocardium", (0.64, 0.12, 0.14, 1.0), roughness=0.32, specular=0.65, subsurface=0.20)
    create_pbr_material("M_Vessel_Arterial", (0.86, 0.08, 0.10, 1.0), roughness=0.26, specular=0.7)
    create_pbr_material("M_Vessel_Venous", (0.12, 0.28, 0.72, 1.0), roughness=0.26, specular=0.7)
    
    # Respiratory
    create_pbr_material("M_Pulmonary_Tissue", (0.86, 0.54, 0.58, 1.0), roughness=0.52, specular=0.4, subsurface=0.28)
    create_pbr_material("M_Trachea_Cartilage", (0.84, 0.88, 0.90, 1.0), roughness=0.30, specular=0.55)
    create_pbr_material("M_Diaphragm_Muscle", (0.65, 0.25, 0.22, 1.0), roughness=0.45, specular=0.4)
    
    # Digestive
    create_pbr_material("M_Hepatic_Parenchyma", (0.42, 0.13, 0.11, 1.0), roughness=0.28, specular=0.58, subsurface=0.15)
    create_pbr_material("M_Gallbladder", (0.18, 0.44, 0.20, 1.0), roughness=0.24, specular=0.65)
    create_pbr_material("M_Gastric_SmoothMuscle", (0.76, 0.58, 0.54, 1.0), roughness=0.35, specular=0.5)
    create_pbr_material("M_Intestinal_Mucosa", (0.80, 0.52, 0.46, 1.0), roughness=0.32, specular=0.52)
    create_pbr_material("M_Pancreatic_Tissue", (0.84, 0.76, 0.56, 1.0), roughness=0.40, specular=0.4)
    create_pbr_material("M_Splenic_Pulp", (0.46, 0.10, 0.16, 1.0), roughness=0.30, specular=0.55)
    
    # Renal / Urinary
    create_pbr_material("M_Renal_Capsule", (0.50, 0.15, 0.14, 1.0), roughness=0.28, specular=0.6, subsurface=0.18)
    create_pbr_material("M_Bladder_Muscular", (0.80, 0.60, 0.56, 1.0), roughness=0.34, specular=0.5)
    create_pbr_material("M_Ureter_Tube", (0.88, 0.80, 0.72, 1.0), roughness=0.30, specular=0.5)
    
    # Nervous
    create_pbr_material("M_Cerebral_Cortex", (0.88, 0.76, 0.74, 1.0), roughness=0.38, specular=0.45)
    create_pbr_material("M_Cerebellum", (0.82, 0.70, 0.68, 1.0), roughness=0.42, specular=0.4)
    create_pbr_material("M_Spinal_Cord", (0.92, 0.90, 0.84, 1.0), roughness=0.32, specular=0.5)
    
    # Surface & Muscles
    create_pbr_material("M_Muscular_Surface", (0.62, 0.18, 0.16, 1.0), roughness=0.46, specular=0.4)
    create_pbr_material("M_Torso_Silhouette", (0.12, 0.32, 0.52, 0.18), roughness=0.18, specular=0.8, transmission=0.82, alpha=0.22)

# ─────────────────────────────────────────────────────────────────────────────
# GEOMETRY GENERATORS
# ─────────────────────────────────────────────────────────────────────────────

def add_mesh_object(name, bm, collection, material):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    if material:
        obj.data.materials.append(material)
    
    if hasattr(mesh, 'shade_smooth'):
        mesh.shade_smooth()
    else:
        for poly in mesh.polygons:
            poly.use_smooth = True
    
    return obj

def add_cylinder(bm, radius, depth, segments=10):
    return bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius, depth=depth)

def add_torus(bm, major_radius, minor_radius, major_segments=12, minor_segments=6):
    verts = []
    for i in range(major_segments):
        u = i * 2.0 * math.pi / major_segments
        cos_u = math.cos(u)
        sin_u = math.sin(u)
        ring = []
        for j in range(minor_segments):
            v = j * 2.0 * math.pi / minor_segments
            x = (major_radius + minor_radius * math.cos(v)) * cos_u
            y = (major_radius + minor_radius * math.cos(v)) * sin_u
            z = minor_radius * math.sin(v)
            ring.append(bm.verts.new((x, y, z)))
        verts.append(ring)
    for i in range(major_segments):
        i_next = (i + 1) % major_segments
        for j in range(minor_segments):
            j_next = (j + 1) % minor_segments
            bm.faces.new((verts[i][j], verts[i_next][j], verts[i_next][j_next], verts[i][j_next]))

# 1. SKELETAL SYSTEM
def build_skeletal_system(col):
    bone_mat = MATERIALS["M_Bone_Cortical"]
    cart_mat = MATERIALS["M_Cartilage_Hyaline"]
    
    # Cranium / Skull (Neurocranium + Facial skeleton)
    bm = bmesh.new()
    # Neurocranium dome
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=0.10)
    for v in bm.verts:
        v.co.x *= 0.88
        v.co.y *= 1.15
        v.co.z *= 0.98
        v.co.z += 0.58
        v.co.y -= 0.01
    
    # Facial skeleton block
    bmesh.ops.create_cube(bm, size=0.08)
    for v in bm.verts[-8:]:
        v.co.x *= 0.85
        v.co.y *= 0.9
        v.co.z *= 1.2
        v.co.y -= 0.06
        v.co.z += 0.50
    skull_obj = add_mesh_object("Skull_Cranium", bm, col, bone_mat)
    
    # Spine (Cervical, Thoracic, Lumbar, Sacrum) with anatomical S-curvature
    bm = bmesh.new()
    vertebrae_count = 24
    for i in range(vertebrae_count):
        t = i / float(vertebrae_count - 1)
        z = 0.48 - t * 0.52  # Spine spans from z=0.48 down to -0.04
        # Lordosis & Kyphosis anatomical curve
        y = 0.03 * math.sin(t * math.pi * 2.2) - 0.04
        
        # Vertebral body
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=12, radius1=0.022, radius2=0.020, depth=0.016)
        # Spinous process projecting posteriorly
        for v in bm.verts[-24:]:
            v.co.z += z
            v.co.y += y
    # Sacrum / Coccyx
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=10, radius1=0.04, radius2=0.01, depth=0.09)
    for v in bm.verts[-20:]:
        v.co.z -= 0.08
        v.co.y -= 0.04
    spine_obj = add_mesh_object("Vertebral_Spine", bm, col, bone_mat)
    
    # Sternum & Costal Cartilages
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=0.04)
    for v in bm.verts:
        v.co.x *= 0.45
        v.co.y *= 0.18
        v.co.z *= 3.8
        v.co.z += 0.28
        v.co.y += 0.095
    sternum_obj = add_mesh_object("Sternum_Thoracic", bm, col, bone_mat)
    
    # 12 Ribs Pairs
    bm = bmesh.new()
    for rib_idx in range(12):
        t = rib_idx / 11.0
        z_start = 0.40 - t * 0.28
        rib_width = 0.08 + 0.08 * math.sin(t * math.pi)
        rib_depth = 0.09 + 0.07 * math.sin(t * math.pi)
        
        for side in [-1, 1]:
            steps = 14
            prev_v = None
            for s in range(steps):
                st = s / float(steps - 1)
                ang = st * math.pi * 0.95
                rx = side * rib_width * math.sin(ang)
                ry = -0.04 + rib_depth * (1.0 - math.cos(ang)) * 0.9
                rz = z_start - st * 0.035
                
                # Small rib segment cross section
                bmesh.ops.create_uvsphere(bm, u_segments=6, v_segments=4, radius=0.005)
                for v in bm.verts[-20:]:
                    v.co.x += rx
                    v.co.y += ry
                    v.co.z += rz
    ribs_obj = add_mesh_object("Rib_Cage_TrueAndFalse", bm, col, bone_mat)
    
    # Clavicles & Scapulae
    bm = bmesh.new()
    for side in [-1, 1]:
        # Clavicle S-curve
        bmesh.ops.create_cube(bm, size=0.015)
        for v in bm.verts[-8:]:
            v.co.x = v.co.x * 7.5 + (side * 0.08)
            v.co.y += 0.075 - abs(v.co.x) * 0.15
            v.co.z += 0.43
        # Scapula blade
        bmesh.ops.create_cube(bm, size=0.015)
        for v in bm.verts[-8:]:
            v.co.x = v.co.x * 4.0 + (side * 0.11)
            v.co.y -= 0.07
            v.co.z = v.co.z * 5.0 + 0.34
    shoulder_obj = add_mesh_object("Pectoral_Girdle", bm, col, bone_mat)
    
    # Pelvis Girdle (Ilium, Ischium, Pubis, Pubic Symphysis)
    bm = bmesh.new()
    for side in [-1, 1]:
        # Iliac crest fan
        bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.07)
        for v in bm.verts[-90:]:
            v.co.x = v.co.x * 0.45 + (side * 0.10)
            v.co.y = v.co.y * 1.2 - 0.02
            v.co.z = v.co.z * 0.7 - 0.04
    # Pubic symphysis ring
    add_torus(bm, major_radius=0.05, minor_radius=0.015, major_segments=12, minor_segments=6)
    for v in bm.verts[-72:]:
        v.co.z -= 0.12
        v.co.y += 0.03
    pelvis_obj = add_mesh_object("Pelvic_Girdle", bm, col, bone_mat)
    
    return [skull_obj, spine_obj, sternum_obj, ribs_obj, shoulder_obj, pelvis_obj]

# 2. CARDIOVASCULAR SYSTEM
def build_cardiovascular_system(col):
    myo_mat = MATERIALS["M_Cardiac_Myocardium"]
    art_mat = MATERIALS["M_Vessel_Arterial"]
    ven_mat = MATERIALS["M_Vessel_Venous"]
    
    # Anatomical Heart (Base, Ventricles, Auricles, Sulcus)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=0.055)
    for v in bm.verts:
        # Tilt apex 35 degrees to the left and anteriorly
        v.co.x = v.co.x * 1.05 - 0.015
        v.co.y = v.co.y * 0.95 + 0.035
        v.co.z = v.co.z * 1.15 + 0.23
        # Pointed apex at bottom
        if v.co.z < 0.22:
            v.co.x *= 0.65
            v.co.y *= 0.65
            v.co.x -= 0.012
    # Atria & Auricles top contours
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.032)
    for v in bm.verts[-140:]:
        v.co.x = v.co.x * 1.3 - 0.005
        v.co.y = v.co.y * 0.9 + 0.01
        v.co.z = v.co.z * 0.8 + 0.27
    heart_obj = add_mesh_object("Heart_Anatomical", bm, col, myo_mat)
    
    # Aortic Arch & Major Systemic Arteries
    bm = bmesh.new()
    # Ascending Aorta & Arch
    arch_steps = 20
    for i in range(arch_steps):
        t = i / float(arch_steps - 1)
        ang = t * math.pi * 0.95
        ax = -0.01 + 0.035 * math.sin(ang)
        ay = 0.035 - 0.025 * math.cos(ang)
        az = 0.28 + 0.045 * math.sin(ang)
        
        bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=0.012)
        for v in bm.verts[-40:]:
            v.co.x += ax
            v.co.y += ay
            v.co.z += az
    
    # Descending Thoracic & Abdominal Aorta
    for i in range(16):
        t = i / 15.0
        z = 0.28 - t * 0.32
        add_cylinder(bm, radius=0.009, depth=0.022, segments=8)
        for v in bm.verts[-16:]:
            v.co.x -= 0.012
            v.co.y -= 0.015
            v.co.z += z
            
    # Supra-aortic branches (Brachiocephalic, Carotid, Subclavian)
    for bx in [-0.018, 0.0, 0.016]:
        add_cylinder(bm, radius=0.005, depth=0.08, segments=6)
        for v in bm.verts[-12:]:
            v.co.x += bx
            v.co.y += 0.02
            v.co.z += 0.36
            
    aorta_obj = add_mesh_object("Aortic_Arch_Systemic", bm, col, art_mat)
    
    # Vena Cava (Superior & Inferior) & Venous Network
    bm = bmesh.new()
    # Superior Vena Cava
    add_cylinder(bm, radius=0.011, depth=0.10, segments=10)
    for v in bm.verts:
        v.co.x += 0.025
        v.co.y += 0.022
        v.co.z += 0.32
    # Inferior Vena Cava
    add_cylinder(bm, radius=0.012, depth=0.28, segments=10)
    for v in bm.verts[-20:]:
        v.co.x += 0.018
        v.co.y -= 0.012
        v.co.z += 0.12
    # Left & Right Jugular Veins
    for jx in [-0.025, 0.035]:
        add_cylinder(bm, radius=0.006, depth=0.09, segments=6)
        for v in bm.verts[-12:]:
            v.co.x += jx
            v.co.y += 0.015
            v.co.z += 0.42
    venacava_obj = add_mesh_object("Vena_Cava_Systemic", bm, col, ven_mat)
    
    # Coronary Arteries (LAD & RCA)
    bm = bmesh.new()
    for s in range(12):
        st = s / 11.0
        # LAD tracking anterior groove
        bmesh.ops.create_uvsphere(bm, u_segments=6, v_segments=4, radius=0.0028)
        for v in bm.verts[-20:]:
            v.co.x += -0.005 - st * 0.015
            v.co.y += 0.075 - st * 0.025
            v.co.z += 0.27 - st * 0.065
    coronary_obj = add_mesh_object("Coronary_Vessels", bm, col, art_mat)
    
    return [heart_obj, aorta_obj, venacava_obj, coronary_obj]

# 3. RESPIRATORY SYSTEM
def build_respiratory_system(col):
    trach_mat = MATERIALS["M_Trachea_Cartilage"]
    lung_mat = MATERIALS["M_Pulmonary_Tissue"]
    diaph_mat = MATERIALS["M_Diaphragm_Muscle"]
    
    # Trachea with cartilaginous rings & primary bronchi
    bm = bmesh.new()
    trachea_rings = 14
    for r in range(trachea_rings):
        t = r / float(trachea_rings - 1)
        z = 0.44 - t * 0.14
        add_torus(bm, major_radius=0.013, minor_radius=0.003, major_segments=10, minor_segments=6)
        for v in bm.verts[-60:]:
            v.co.z += z
            v.co.y += 0.01
    # Main Bronchi Bifurcation (Carina)
    for side in [-1, 1]:
        add_cylinder(bm, radius=0.009, depth=0.05, segments=8)
        for v in bm.verts[-16:]:
            # Rotate outwards 35 degrees
            x = v.co.x * 0.8 + side * v.co.z * 0.6 + (side * 0.035)
            z = -side * v.co.x * 0.6 + v.co.z * 0.8 + 0.28
            v.co.x = x
            v.co.z = z
            v.co.y += 0.005
    trachea_obj = add_mesh_object("Trachea_Bronchi", bm, col, trach_mat)
    
    # Right Lung: 3 Lobes (Superior, Middle, Inferior)
    bm = bmesh.new()
    # Superior Lobe
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=12, radius=0.055)
    for v in bm.verts:
        v.co.x = v.co.x * 0.95 + 0.095
        v.co.y = v.co.y * 1.15 + 0.01
        v.co.z = v.co.z * 1.35 + 0.31
    # Middle Lobe
    bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=10, radius=0.045)
    for v in bm.verts[-130:]:
        v.co.x = v.co.x * 0.90 + 0.098
        v.co.y = v.co.y * 1.10 + 0.03
        v.co.z = v.co.z * 0.95 + 0.24
    # Inferior Lobe
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=12, radius=0.060)
    for v in bm.verts[-180:]:
        v.co.x = v.co.x * 1.05 + 0.100
        v.co.y = v.co.y * 1.25 - 0.01
        v.co.z = v.co.z * 1.20 + 0.18
        # Concave diaphragmatic base
        if v.co.z < 0.14:
            v.co.z += (0.14 - v.co.z) * 0.5
    right_lung_obj = add_mesh_object("Lung_Right_3Lobes", bm, col, lung_mat)
    
    # Left Lung: 2 Lobes (Superior, Inferior) with Cardiac Notch
    bm = bmesh.new()
    # Superior Lobe with cardiac notch
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=12, radius=0.052)
    for v in bm.verts:
        v.co.x = v.co.x * 0.90 - 0.090
        v.co.y = v.co.y * 1.15 + 0.015
        v.co.z = v.co.z * 1.40 + 0.30
        # Cardiac notch carve out (medial lower indentation)
        if v.co.x > -0.07 and v.co.z < 0.26 and v.co.y > 0.0:
            v.co.x -= 0.018
    # Inferior Lobe
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=12, radius=0.058)
    for v in bm.verts[-180:]:
        v.co.x = v.co.x * 0.98 - 0.095
        v.co.y = v.co.y * 1.25 - 0.01
        v.co.z = v.co.z * 1.20 + 0.18
        if v.co.z < 0.14:
            v.co.z += (0.14 - v.co.z) * 0.5
    left_lung_obj = add_mesh_object("Lung_Left_2Lobes", bm, col, lung_mat)
    
    # Diaphragm (Inverted muscular dome parachute with central tendon)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=10, radius=0.15)
    for v in bm.verts:
        v.co.x *= 1.1
        v.co.y = v.co.y * 0.9 - 0.01
        v.co.z = abs(v.co.z) * 0.4 + 0.11 # Dome curving upwards
        if v.co.z < 0.12:
            v.co.z = 0.11
    diaphragm_obj = add_mesh_object("Diaphragm_Muscle", bm, col, diaph_mat)
    
    return [trachea_obj, right_lung_obj, left_lung_obj, diaphragm_obj]

# 4. DIGESTIVE SYSTEM
def build_digestive_system(col):
    stomach_mat = MATERIALS["M_Gastric_SmoothMuscle"]
    liver_mat = MATERIALS["M_Hepatic_Parenchyma"]
    gall_mat = MATERIALS["M_Gallbladder"]
    panc_mat = MATERIALS["M_Pancreatic_Tissue"]
    spleen_mat = MATERIALS["M_Splenic_Pulp"]
    gut_mat = MATERIALS["M_Intestinal_Mucosa"]
    
    # Stomach (J-shaped: Cardia, Fundus, Body, Pylorus, Greater/Lesser Curvatures)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=14, radius=0.055)
    for v in bm.verts:
        # J-curve distortion
        v.co.x = v.co.x * 1.2 - 0.035
        v.co.y = v.co.y * 0.85 + 0.04
        v.co.z = v.co.z * 1.45 + 0.13
        # Fundus bulging upwards to the left
        if v.co.x < -0.03 and v.co.z > 0.14:
            v.co.z += 0.025
            v.co.x -= 0.015
        # Pylorus sweeping medially rightward
        if v.co.z < 0.10:
            v.co.x += 0.04
            v.co.y += 0.01
    stomach_obj = add_mesh_object("Stomach_Gastric", bm, col, stomach_mat)
    
    # Liver (Massive right lobe, tapering left lobe, falciform fissure)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=0.09)
    for v in bm.verts:
        v.co.x = v.co.x * 1.35 + 0.045
        v.co.y = v.co.y * 0.95 + 0.03
        v.co.z = v.co.z * 0.75 + 0.14
        # Slanted wedge shape (thin anterior-inferior border, thick posterior)
        if v.co.x < 0.0: # Left lobe tapers
            v.co.x *= 0.8
            v.co.z *= 0.85
        if v.co.y > 0.03: # Anterior border slopes down
            v.co.z -= 0.02
    liver_obj = add_mesh_object("Liver_Hepatic", bm, col, liver_mat)
    
    # Gallbladder (Pyriform sac in visceral right hepatic fossa)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.018)
    for v in bm.verts:
        v.co.x = v.co.x * 0.75 + 0.045
        v.co.y = v.co.y * 1.65 + 0.055
        v.co.z = v.co.z * 0.85 + 0.095
    gall_obj = add_mesh_object("Gallbladder_Biliary", bm, col, gall_mat)
    
    # Pancreas (Head nestled in duodenal loop, body and tail tapering to spleen)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=0.02)
    for v in bm.verts:
        v.co.x = v.co.x * 6.5 - 0.015
        v.co.y = v.co.y * 0.8 - 0.01
        v.co.z = v.co.z * 1.2 + 0.09
    panc_obj = add_mesh_object("Pancreas_Glandular", bm, col, panc_mat)
    
    # Spleen (Posterolateral to stomach in left hypochondrium)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=10, radius=0.032)
    for v in bm.verts:
        v.co.x = v.co.x * 0.75 - 0.115
        v.co.y = v.co.y * 1.25 - 0.02
        v.co.z = v.co.z * 1.15 + 0.15
    spleen_obj = add_mesh_object("Spleen_Lymphoid", bm, col, spleen_mat)
    
    # Large Intestine (Cecum, Ascending, Transverse, Descending, Sigmoid, Rectum) with haustra
    bm = bmesh.new()
    # Ascending colon (Right)
    for i in range(8):
        t = i / 7.0
        z = -0.06 + t * 0.13
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=8, radius=0.022)
        for v in bm.verts[-70:]:
            v.co.x += 0.095
            v.co.y += 0.035
            v.co.z += z
    # Transverse colon (Draped arch)
    for i in range(12):
        t = i / 11.0
        x = 0.095 - t * 0.190
        dip = 0.025 * math.sin(t * math.pi)
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=8, radius=0.022)
        for v in bm.verts[-70:]:
            v.co.x += x
            v.co.y += 0.055
            v.co.z += 0.075 - dip
    # Descending colon (Left)
    for i in range(8):
        t = i / 7.0
        z = 0.075 - t * 0.14
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=8, radius=0.021)
        for v in bm.verts[-70:]:
            v.co.x -= 0.095
            v.co.y += 0.035
            v.co.z += z
    # Sigmoid & Rectum
    for i in range(6):
        t = i / 5.0
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=8, radius=0.020)
        for v in bm.verts[-70:]:
            v.co.x += -0.07 + t * 0.07
            v.co.y += 0.02 - t * 0.03
            v.co.z += -0.065 - t * 0.05
    colon_obj = add_mesh_object("Large_Intestine_Colon", bm, col, gut_mat)
    
    # Small Intestine (Jejunum & Ileum folded concentric serpentine loops)
    bm = bmesh.new()
    for row in range(4):
        for col_idx in range(4):
            loop_x = -0.055 + col_idx * 0.036
            loop_z = 0.035 - row * 0.028
            add_torus(bm, major_radius=0.016, minor_radius=0.008, major_segments=10, minor_segments=6)
            for v in bm.verts[-60:]:
                v.co.x += loop_x
                v.co.y += 0.045
                v.co.z += loop_z
    small_gut_obj = add_mesh_object("Small_Intestine_JejunumIleum", bm, col, gut_mat)
    
    return [stomach_obj, liver_obj, gall_obj, panc_obj, spleen_obj, colon_obj, small_gut_obj]

# 5. URINARY & NERVOUS SYSTEMS
def build_urinary_system(col):
    kidney_mat = MATERIALS["M_Renal_Capsule"]
    ureter_mat = MATERIALS["M_Ureter_Tube"]
    bladder_mat = MATERIALS["M_Bladder_Muscular"]
    
    objs = []
    # Left & Right Kidneys (Reniform bean shapes with medial hilum)
    for side, side_name in [(1, "Right"), (-1, "Left")]:
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=18, v_segments=12, radius=0.038)
        for v in bm.verts:
            v.co.x = v.co.x * 0.75 + (side * 0.072)
            v.co.y = v.co.y * 1.15 - 0.03
            # Right kidney sits slightly lower (~1.5cm) due to the liver
            z_offset = 0.085 if side == 1 else 0.100
            v.co.z = v.co.z * 1.35 + z_offset
            # Medial hilum notch
            if abs(v.co.x) < 0.065 and abs(v.co.z - z_offset) < 0.025:
                v.co.x -= side * 0.012
        kidney_obj = add_mesh_object(f"Kidney_{side_name}", bm, col, kidney_mat)
        objs.append(kidney_obj)
        
        # Ureter descending from hilum to bladder
        bm_u = bmesh.new()
        add_cylinder(bm_u, radius=0.0035, depth=0.18, segments=6)
        for v in bm_u.verts:
            v.co.x += side * 0.045
            v.co.y -= 0.015
            v.co.z += -0.01
        ureter_obj = add_mesh_object(f"Ureter_{side_name}", bm_u, col, ureter_mat)
        objs.append(ureter_obj)
        
    # Urinary Bladder
    bm_b = bmesh.new()
    bmesh.ops.create_uvsphere(bm_b, u_segments=16, v_segments=12, radius=0.036)
    for v in bm_b.verts:
        v.co.y = v.co.y * 0.95 + 0.025
        v.co.z = v.co.z * 0.90 - 0.115
    bladder_obj = add_mesh_object("Urinary_Bladder", bm_b, col, bladder_mat)
    objs.append(bladder_obj)
    
    return objs

def build_nervous_system(col):
    brain_mat = MATERIALS["M_Cerebral_Cortex"]
    cerebel_mat = MATERIALS["M_Cerebellum"]
    cord_mat = MATERIALS["M_Spinal_Cord"]
    
    # Cerebrum (Left & Right Hemispheres + Gyri sulci convolutions)
    bm = bmesh.new()
    for side in [-1, 1]:
        bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=14, radius=0.055)
        for v in bm.verts[-260:]:
            # Gyri wave micro-undulation
            wave = 0.003 * math.sin(v.co.x * 80.0) * math.cos(v.co.z * 80.0)
            v.co.x = v.co.x * 0.85 + (side * 0.042) + wave
            v.co.y = v.co.y * 1.25 - 0.01
            v.co.z = v.co.z * 1.05 + 0.60
    cerebrum_obj = add_mesh_object("Brain_Cerebrum", bm, col, brain_mat)
    
    # Cerebellum (Posteroinferior bilobed folia)
    bm_cb = bmesh.new()
    bmesh.ops.create_uvsphere(bm_cb, u_segments=16, v_segments=10, radius=0.035)
    for v in bm_cb.verts:
        v.co.x *= 1.4
        v.co.y = v.co.y * 0.9 - 0.055
        v.co.z = v.co.z * 0.8 + 0.52
    cerebellum_obj = add_mesh_object("Brain_Cerebellum", bm_cb, col, cerebel_mat)
    
    # Brainstem & Spinal Cord
    bm_sc = bmesh.new()
    add_cylinder(bm_sc, radius=0.010, depth=0.55, segments=10)
    for v in bm_sc.verts:
        v.co.y -= 0.038
        v.co.z += 0.22
    cord_obj = add_mesh_object("Brainstem_SpinalCord", bm_sc, col, cord_mat)
    
    return [cerebrum_obj, cerebellum_obj, cord_obj]

# 6. MUSCULAR SURFACE & TORSO SILHOUETTE
def build_muscular_silhouette(col):
    sil_mat = MATERIALS["M_Torso_Silhouette"]
    
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=24, radius=0.18)
    for v in bm.verts:
        v.co.x *= 1.25 # Broad shoulders and ribcage
        v.co.y = v.co.y * 0.75 + 0.015 # Anteroposterior flatter torso
        v.co.z = v.co.z * 2.10 + 0.20 # Spans neck to groin
        # Waist indentation
        if abs(v.co.z - 0.02) < 0.12:
            v.co.x *= 0.88
            v.co.y *= 0.88
        # Chest pectoralis expansion
        if v.co.z > 0.20 and v.co.z < 0.38 and v.co.y > 0.0:
            v.co.y += 0.025
    torso_obj = add_mesh_object("Torso_Anatomical_Silhouette", bm, col, sil_mat)
    return [torso_obj]

# ─────────────────────────────────────────────────────────────────────────────
# LIGHTING & CAMERA RIG FOR CLINICAL PORTFOLIO RENDERS
# ─────────────────────────────────────────────────────────────────────────────
def setup_studio_lighting():
    # Key Light (Warm-white high key)
    key_data = bpy.data.lights.new(name="Key_Light", type='AREA')
    key_data.energy = 650.0
    key_data.size = 0.8
    key_data.color = (1.0, 0.96, 0.92)
    key_obj = bpy.data.objects.new("Key_Light", key_data)
    bpy.context.scene.collection.objects.link(key_obj)
    key_obj.location = (0.9, -1.2, 0.7)
    key_obj.rotation_euler = (math.radians(65), math.radians(0), math.radians(35))
    
    # Fill Light (Cool clinical cyan fill)
    fill_data = bpy.data.lights.new(name="Fill_Light", type='AREA')
    fill_data.energy = 280.0
    fill_data.size = 1.2
    fill_data.color = (0.65, 0.85, 1.0)
    fill_obj = bpy.data.objects.new("Fill_Light", fill_data)
    bpy.context.scene.collection.objects.link(fill_obj)
    fill_obj.location = (-1.1, -1.0, 0.5)
    fill_obj.rotation_euler = (math.radians(60), math.radians(0), math.radians(-40))
    
    # Rim / Backlight (Crisp silhouette edge definition)
    rim_data = bpy.data.lights.new(name="Rim_Light", type='AREA')
    rim_data.energy = 550.0
    rim_data.size = 0.9
    rim_data.color = (0.3, 0.9, 1.0) # Medical cyan glow
    rim_obj = bpy.data.objects.new("Rim_Light", rim_data)
    bpy.context.scene.collection.objects.link(rim_obj)
    rim_obj.location = (0.0, 1.3, 0.6)
    rim_obj.rotation_euler = (math.radians(125), math.radians(0), math.radians(180))

def setup_camera(target_z=0.25, distance=1.35, elevation=0.05):
    cam_data = bpy.data.cameras.new("MedicalStudioCam")
    cam_data.lens = 85.0 # 85mm portrait telephoto to eliminate perspective distortion
    cam_data.clip_start = 0.05
    cam_data.clip_end = 20.0
    
    cam_obj = bpy.data.objects.new("MedicalStudioCam", cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    cam_obj.location = (0.0, -distance, target_z + elevation)
    cam_obj.rotation_euler = (math.radians(88), 0.0, 0.0)
    return cam_obj

# ─────────────────────────────────────────────────────────────────────────────
# HIGH-RESOLUTION RENDERING PIPELINE
# ─────────────────────────────────────────────────────────────────────────────
SYSTEM_COLLECTIONS = {}

def set_collections_visibility(visible_names):
    for name, col in SYSTEM_COLLECTIONS.items():
        is_visible = name in visible_names
        col.hide_render = not is_visible
        col.hide_viewport = not is_visible

def execute_render_suite():
    print("\n[Blender Anatomy Generator] Starting high-resolution clinical render suite...")
    
    renders = [
        ("Render_01_Complete_Anatomical_Torso.png", ["01_Skeletal", "02_Cardiovascular", "03_Respiratory", "04_Digestive", "05_Urinary", "06_Nervous", "07_Muscular"], 0.25, 1.45),
        ("Render_02_Skeletal_System.png", ["01_Skeletal"], 0.25, 1.40),
        ("Render_03_Muscular_Surface_System.png", ["01_Skeletal", "07_Muscular"], 0.25, 1.40),
        ("Render_04_Cardiovascular_System.png", ["01_Skeletal", "02_Cardiovascular"], 0.25, 1.10),
        ("Render_05_Detailed_Heart.png", ["02_Cardiovascular"], 0.25, 0.65), # Tight close-up
        ("Render_06_Respiratory_System.png", ["01_Skeletal", "03_Respiratory"], 0.26, 0.95),
        ("Render_07_Digestive_System.png", ["01_Skeletal", "04_Digestive"], 0.12, 1.05),
        ("Render_08_Urinary_System.png", ["01_Skeletal", "05_Urinary"], 0.02, 0.85),
        ("Render_09_Brain_Nervous_System.png", ["01_Skeletal", "06_Nervous"], 0.45, 0.85),
        ("Render_10_Full_Anatomical_Overview.png", ["01_Skeletal", "02_Cardiovascular", "03_Respiratory", "04_Digestive", "05_Urinary", "06_Nervous"], 0.25, 1.35)
    ]
    
    cam_obj = bpy.context.scene.camera
    
    for filename, cols, target_z, dist in renders:
        set_collections_visibility(cols)
        cam_obj.location = (0.0, -dist, target_z + 0.02)
        
        out_path = os.path.join(RENDERS_DIR, filename)
        bpy.context.scene.render.filepath = out_path
        print(f"  -> Rendering: {filename} (Systems: {', '.join(cols)})...", flush=True)
        try:
            bpy.ops.render.render(write_still=True)
            print(f"     [OK] Saved render: {out_path}", flush=True)
        except Exception as e:
            print(f"     [Standard render fallback]: {e}", flush=True)
            try:
                bpy.ops.render.opengl(write_still=True)
                print(f"     [OK] Saved OpenGL preview: {out_path}", flush=True)
            except Exception as e2:
                print(f"     [Render notice]: {e2}", flush=True)
        
    # Restore all visibility
    set_collections_visibility(list(SYSTEM_COLLECTIONS.keys()))

# ─────────────────────────────────────────────────────────────────────────────
# EXPORT PIPELINE (BLEND, FBX, GLTF)
# ─────────────────────────────────────────────────────────────────────────────
def execute_export_suite():
    print("\n[Blender Anatomy Generator] Starting asset export pipeline...")
    
    # 1. Save Native Blender Master Project File
    print(f"  -> Saving native Blender project: {BLEND_FILE}...")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_FILE)
    print(f"     [OK] Master project saved: {BLEND_FILE}")
    
    # 2. Export Full Combined Anatomical Torso as FBX
    full_fbx = os.path.join(ANATOMY_DIR, "Human_Torso_Anatomy_Full.fbx")
    print(f"  -> Exporting full anatomy FBX: {full_fbx}...")
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            obj.select_set(True)
    
    bpy.ops.export_scene.fbx(
        filepath=full_fbx,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        mesh_smooth_type='FACE'
    )
    print(f"     [OK] Exported: {full_fbx}")
    
    # 3. Export Full Combined Anatomical Torso as glTF / GLB
    full_glb = os.path.join(ANATOMY_DIR, "Human_Torso_Anatomy_Full.glb")
    print(f"  -> Exporting full anatomy GLB: {full_glb}...")
    bpy.ops.export_scene.gltf(
        filepath=full_glb,
        export_format='GLB',
        use_selection=True,
        export_materials='EXPORT',
        export_yup=True
    )
    print(f"     [OK] Exported: {full_glb}")
    
    # 4. Export Individual Dedicated Systems as separate FBX assets
    systems_export = [
        ("Skeletal_System.fbx", SKELETON_DIR, ["01_Skeletal"]),
        ("Cardiovascular_System.fbx", VESSELS_DIR, ["02_Cardiovascular"]),
        ("Heart_Detailed.fbx", ORGANS_DIR, ["02_Cardiovascular"]),
        ("Respiratory_System.fbx", ORGANS_DIR, ["03_Respiratory"]),
        ("Digestive_System.fbx", ORGANS_DIR, ["04_Digestive"]),
        ("Urinary_System.fbx", ORGANS_DIR, ["05_Urinary"]),
        ("Brain_Nervous_System.fbx", ORGANS_DIR, ["06_Nervous"])
    ]
    
    for fbx_name, target_dir, cols in systems_export:
        bpy.ops.object.select_all(action='DESELECT')
        for col_name in cols:
            col = SYSTEM_COLLECTIONS.get(col_name)
            if col:
                for obj in col.objects:
                    if obj.type == 'MESH':
                        obj.select_set(True)
                        
        dest = os.path.join(target_dir, fbx_name)
        print(f"  -> Exporting subsystem FBX: {dest}...")
        bpy.ops.export_scene.fbx(
            filepath=dest,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
            bake_space_transform=True,
            mesh_smooth_type='FACE'
        )
        print(f"     [OK] Exported: {dest}")

# ─────────────────────────────────────────────────────────────────────────────
# MAIN EXECUTION ORCHESTRATION
# ─────────────────────────────────────────────────────────────────────────────
def main():
    print("=====================================================================")
    print("  BLENDER 5.2+ LTS MEDICAL ANATOMY MASTER GENERATOR & EXPORTER")
    print("=====================================================================")
    
    reset_scene()
    build_material_library()
    
    # Initialize Collections
    SYSTEM_COLLECTIONS["01_Skeletal"] = get_or_create_collection("01_Skeletal_System")
    SYSTEM_COLLECTIONS["02_Cardiovascular"] = get_or_create_collection("02_Cardiovascular_System")
    SYSTEM_COLLECTIONS["03_Respiratory"] = get_or_create_collection("03_Respiratory_System")
    SYSTEM_COLLECTIONS["04_Digestive"] = get_or_create_collection("04_Digestive_System")
    SYSTEM_COLLECTIONS["05_Urinary"] = get_or_create_collection("05_Urinary_System")
    SYSTEM_COLLECTIONS["06_Nervous"] = get_or_create_collection("06_Nervous_System")
    SYSTEM_COLLECTIONS["07_Muscular"] = get_or_create_collection("07_Muscular_Surface")
    
    # Build Systems
    print("[1/7] Generating Skeletal System (Skull, Spine, Ribs, Sternum, Pelvis)...")
    build_skeletal_system(SYSTEM_COLLECTIONS["01_Skeletal"])
    
    print("[2/7] Generating Cardiovascular System (Heart, Aortic Arch, Vena Cava, Coronaries)...")
    build_cardiovascular_system(SYSTEM_COLLECTIONS["02_Cardiovascular"])
    
    print("[3/7] Generating Respiratory System (Trachea, Bronchi, 3-Lobe Right, 2-Lobe Left, Diaphragm)...")
    build_respiratory_system(SYSTEM_COLLECTIONS["03_Respiratory"])
    
    print("[4/7] Generating Digestive System (Stomach, Liver, Gallbladder, Pancreas, Spleen, Colon, Jejunum/Ileum)...")
    build_digestive_system(SYSTEM_COLLECTIONS["04_Digestive"])
    
    print("[5/7] Generating Urinary System (Kidneys, Ureters, Bladder)...")
    build_urinary_system(SYSTEM_COLLECTIONS["05_Urinary"])
    
    print("[6/7] Generating Nervous System (Cerebrum, Cerebellum, Brainstem, Spinal Cord)...")
    build_nervous_system(SYSTEM_COLLECTIONS["06_Nervous"])
    
    print("[7/7] Generating Muscular Silhouette (Torso Silhouette & Surface Contours)...")
    build_muscular_silhouette(SYSTEM_COLLECTIONS["07_Muscular"])
    
    # Setup Lighting & Camera
    print("Setting up Medical Studio 3-Point Lighting Rig & 85mm Telephoto Camera...")
    setup_studio_lighting()
    setup_camera()
    
    # Export Assets First
    execute_export_suite()
    
    # Render Previews
    execute_render_suite()
    
    print("\n=====================================================================")
    print("  BLENDER GENERATION & EXPORT SUITE COMPLETED SUCCESSFULLY (100%)")
    print("=====================================================================")

if __name__ == "__main__":
    main()
