"""
PROJECT ATLAS: AAA MEDICAL VISUALIZATION & BLENDER ART DIRECTION PIPELINE
Builds the complete 24-material Global Medical Material Library, upgrades all 6 modules,
sets up physical lighting, renders reference images, and exports FBX and .blend assets.
"""

import bpy
import bmesh
import math
import os
import sys

WORKSPACE = r"C:\Projects\New folder"
MODELS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Resources", "Models")
BLENDS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "BlenderModels")
RENDERS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Renders")
MATERIALS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Materials")

os.makedirs(MODELS_DIR, exist_ok=True)
os.makedirs(BLENDS_DIR, exist_ok=True)
os.makedirs(RENDERS_DIR, exist_ok=True)
os.makedirs(MATERIALS_DIR, exist_ok=True)

# ---------------------------------------------------------------------------
# 1. SHADER NODE UTILITIES & 24 GLOBAL ATLAS MATERIALS
# ---------------------------------------------------------------------------
def get_or_create_material(name):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    return mat

def setup_principled_bsdf(mat, **kwargs):
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    output_node = nodes.new(type='ShaderNodeOutputMaterial')
    output_node.location = (400, 0)

    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    links.new(bsdf.outputs['BSDF'], output_node.inputs['Surface'])

    # Apply properties to BSDF inputs safely
    for key, value in kwargs.items():
        if key in bsdf.inputs:
            if isinstance(value, (tuple, list)):
                if len(value) == 3 and len(bsdf.inputs[key].default_value) == 3:
                    bsdf.inputs[key].default_value = value
                elif len(value) == 4 and len(bsdf.inputs[key].default_value) == 4:
                    bsdf.inputs[key].default_value = value
                elif len(value) == 3 and len(bsdf.inputs[key].default_value) == 4:
                    bsdf.inputs[key].default_value = (value[0], value[1], value[2], 1.0)
            else:
                bsdf.inputs[key].default_value = value

    return bsdf, nodes, links

def add_procedural_bump(nodes, links, bsdf, texture_type='NOISE', scale=25.0, detail=4.0, roughness=0.5, bump_strength=0.15):
    tex_node = nodes.new(type=f'ShaderNodeTex{texture_type.capitalize()}')
    tex_node.location = (-450, -100)
    if hasattr(tex_node, 'inputs'):
        if 'Scale' in tex_node.inputs:
            tex_node.inputs['Scale'].default_value = scale
        if 'Detail' in tex_node.inputs:
            tex_node.inputs['Detail'].default_value = detail
        if 'Roughness' in tex_node.inputs:
            tex_node.inputs['Roughness'].default_value = roughness

    bump_node = nodes.new(type='ShaderNodeBump')
    bump_node.location = (-200, -100)
    bump_node.inputs['Strength'].default_value = bump_strength
    bump_node.inputs['Distance'].default_value = 0.05

    output_socket = tex_node.outputs.get('Fac') or tex_node.outputs.get('Distance') or tex_node.outputs.get('Color')
    if output_socket:
        links.new(output_socket, bump_node.inputs['Height'])
    if 'Normal' in bsdf.inputs:
        links.new(bump_node.outputs['Normal'], bsdf.inputs['Normal'])

def build_global_material_library():
    print("[Project Atlas] Building 24 Global Atlas PBR Materials...")

    # 1. Bone Cortical (Warm ivory, micro-porosity)
    mat = get_or_create_material("MED_MAT_Bone_Cortical")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.88, 0.84, 0.76, 1.0),
            "Roughness": 0.45,
            "Subsurface Weight": 0.05,
            "Subsurface Radius": (0.8, 0.7, 0.5),
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'NOISE', scale=45.0, detail=5.0, bump_strength=0.08)

    # 2. Bone Cancellous (Deep honey-beige, porous trabeculae)
    mat = get_or_create_material("MED_MAT_Bone_Cancellous")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.82, 0.74, 0.62, 1.0),
            "Roughness": 0.60,
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'VORONOI', scale=60.0, bump_strength=0.25)

    # 3. Cartilage (Translucent bluish-grey fibrocartilage)
    mat = get_or_create_material("MED_MAT_Cartilage")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.72, 0.80, 0.85, 1.0),
            "Roughness": 0.25,
            "Subsurface Weight": 0.35,
            "Subsurface Radius": (0.4, 0.6, 0.8),
            "Coat Weight": 0.30,
            "Coat Roughness": 0.15,
        }
    )

    # 4. Skeletal Muscle (Directional muscle striations, deep red-brown)
    mat = get_or_create_material("MED_MAT_Skeletal_Muscle")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.50, 0.11, 0.10, 1.0),
            "Roughness": 0.35,
            "Subsurface Weight": 0.20,
            "Subsurface Radius": (0.6, 0.2, 0.1),
            "Coat Weight": 0.25,
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'WAVE', scale=30.0, bump_strength=0.18)

    # 5. Cardiac Muscle (Rich crimson myocardium, SSS, serous coat)
    mat = get_or_create_material("MED_MAT_Cardiac_Muscle")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.56, 0.08, 0.09, 1.0),
            "Roughness": 0.28,
            "Subsurface Weight": 0.25,
            "Subsurface Radius": (0.7, 0.15, 0.1),
            "Coat Weight": 0.45,
            "Coat Roughness": 0.12,
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'WAVE', scale=40.0, bump_strength=0.15)

    # 6. Tendon (Fibrous glistening ivory-white aponeurosis)
    mat = get_or_create_material("MED_MAT_Tendon")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.92, 0.90, 0.86, 1.0),
            "Roughness": 0.22,
            "Coat Weight": 0.50,
            "Coat Roughness": 0.10,
        }
    )

    # 7. Artery (High-gloss oxygenated arterial crimson)
    mat = get_or_create_material("MED_MAT_Artery")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.78, 0.04, 0.05, 1.0),
            "Roughness": 0.18,
            "Coat Weight": 0.50,
            "Coat Roughness": 0.10,
        }
    )

    # 8. Vein (Deep cyanotic venous plum-purple)
    mat = get_or_create_material("MED_MAT_Vein")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.28, 0.08, 0.34, 1.0),
            "Roughness": 0.24,
            "Coat Weight": 0.35,
        }
    )

    # 9. Heart Fat (Creamy pale-yellow epicardial adipose tissue)
    mat = get_or_create_material("MED_MAT_Heart_Fat")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.92, 0.85, 0.52, 1.0),
            "Roughness": 0.38,
            "Subsurface Weight": 0.30,
            "Subsurface Radius": (0.8, 0.7, 0.4),
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'VORONOI', scale=35.0, bump_strength=0.20)

    # 10. Lung (Spongy pinkish-red mottled pulmonary parenchyma)
    mat = get_or_create_material("MED_MAT_Lung")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.65, 0.26, 0.28, 1.0),
            "Roughness": 0.40,
            "Subsurface Weight": 0.35,
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'NOISE', scale=50.0, bump_strength=0.18)

    # 11. Liver (Burgundy-brown, smooth Glisson's capsule)
    mat = get_or_create_material("MED_MAT_Liver")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.35, 0.09, 0.08, 1.0),
            "Roughness": 0.22,
            "Coat Weight": 0.65,
            "Coat Roughness": 0.12,
        }
    )

    # 12. Gallbladder (Emerald-olive bile, translucent wall)
    mat = get_or_create_material("MED_MAT_Gallbladder")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.12, 0.36, 0.16, 1.0),
            "Roughness": 0.20,
            "Subsurface Weight": 0.30,
            "Subsurface Radius": (0.2, 0.6, 0.3),
            "Coat Weight": 0.55,
        }
    )

    # 13. Stomach (Gastric pink-salmon mucosal sheen)
    mat = get_or_create_material("MED_MAT_Stomach")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.62, 0.28, 0.28, 1.0),
            "Roughness": 0.22,
            "Subsurface Weight": 0.25,
            "Coat Weight": 0.45,
        }
    )

    # 14. Intestine (Moist warm peristaltic tissue)
    mat = get_or_create_material("MED_MAT_Intestine")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.58, 0.32, 0.28, 1.0),
            "Roughness": 0.22,
            "Subsurface Weight": 0.25,
            "Coat Weight": 0.40,
        }
    )

    # 15. Kidney (Deep renal maroon parenchyma)
    mat = get_or_create_material("MED_MAT_Kidney")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.38, 0.12, 0.14, 1.0),
            "Roughness": 0.25,
            "Subsurface Weight": 0.25,
            "Coat Weight": 0.50,
        }
    )

    # 16. Nerve (Ivory-cream cerebral and peripheral neural tissue)
    mat = get_or_create_material("MED_MAT_Nerve")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.94, 0.92, 0.82, 1.0),
            "Roughness": 0.32,
            "Coat Weight": 0.20,
        }
    )

    # 17. Skin Translucent (Frosted acrylic medical cutaway)
    mat = get_or_create_material("MED_MAT_Skin_Translucent")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.85, 0.88, 0.92, 1.0),
            "Roughness": 0.15,
            "IOR": 1.45,
            "Alpha": 0.12,
            "Transmission Weight": 0.85,
        }
    )
    mat.blend_method = 'BLEND' if hasattr(mat, 'blend_method') else 'HASHED'

    # 18. Dental Enamel (Highly polished enamel with incisal translucency)
    mat = get_or_create_material("MED_MAT_Dental_Enamel")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.96, 0.95, 0.91, 1.0),
            "Roughness": 0.12,
            "IOR": 1.62,
            "Subsurface Weight": 0.15,
            "Subsurface Radius": (0.9, 0.9, 0.8),
            "Coat Weight": 0.70,
            "Coat Roughness": 0.08,
        }
    )

    # 19. Dentin (Warm ivory-yellow inner core)
    mat = get_or_create_material("MED_MAT_Dentin")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.90, 0.82, 0.62, 1.0),
            "Roughness": 0.35,
            "Subsurface Weight": 0.20,
        }
    )

    # 20. Cementum (Matte pale ochre root surface)
    mat = get_or_create_material("MED_MAT_Cementum")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.85, 0.78, 0.65, 1.0),
            "Roughness": 0.55,
        }
    )

    # 21. Gingiva (Vascular pink stippled attached gums)
    mat = get_or_create_material("MED_MAT_Gingiva")
    bsdf, nodes, links = setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.78, 0.35, 0.42, 1.0),
            "Roughness": 0.30,
            "Subsurface Weight": 0.35,
            "Subsurface Radius": (0.8, 0.3, 0.3),
        }
    )
    add_procedural_bump(nodes, links, bsdf, 'VORONOI', scale=55.0, bump_strength=0.16)

    # 22. Surgical Steel AISI 316L (Medical-grade polished stainless steel)
    mat = get_or_create_material("MED_MAT_Surgical_Steel")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.95, 0.95, 0.95, 1.0),
            "Metallic": 1.0,
            "Roughness": 0.15,
            "Anisotropic": 0.35 if "Anisotropic" in bsdf.inputs else 0.0,
        }
    )

    # 23. Surgical Polymer (Medical blue autoclave instrument handles)
    mat = get_or_create_material("MED_MAT_Surgical_Polymer")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (0.12, 0.38, 0.72, 1.0),
            "Roughness": 0.28,
            "Coat Weight": 0.30,
        }
    )

    # 24. Clinical Glass (Borosilicate lab glass / vials)
    mat = get_or_create_material("MED_MAT_Clinical_Glass")
    setup_principled_bsdf(
        mat,
        **{
            "Base Color": (1.0, 1.0, 1.0, 1.0),
            "Roughness": 0.05,
            "IOR": 1.52,
            "Transmission Weight": 1.0,
        }
    )

    print("[Project Atlas] All 24 Global Materials defined successfully.")

def assign_material_to_obj(obj, mat_name):
    if not obj or obj.type != 'MESH':
        return
    mat = bpy.data.materials.get(mat_name)
    if not mat:
        return
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

# ---------------------------------------------------------------------------
# 2. STUDIO LIGHTING RIG & CAMERA CREATION
# ---------------------------------------------------------------------------
def setup_studio_lighting(rig_name="Studio_Atlas_Lighting", target_loc=(0, 0, 1.0)):
    # Remove existing lights
    for l in [o for o in bpy.data.objects if o.type == 'LIGHT']:
        bpy.data.objects.remove(l, do_unlink=True)

    # Key Light (Warm 4800K daylight surgical feel)
    key_data = bpy.data.lights.new(name=f"{rig_name}_Key", type='AREA')
    key_data.energy = 350.0
    key_data.size = 1.2
    key_data.color = (1.0, 0.98, 0.95)
    key_obj = bpy.data.objects.new(name=f"{rig_name}_Key", object_data=key_data)
    key_obj.location = (target_loc[0] + 1.8, target_loc[1] - 2.2, target_loc[2] + 2.0)
    bpy.context.collection.objects.link(key_obj)

    # Fill Light (Soft cool ambient)
    fill_data = bpy.data.lights.new(name=f"{rig_name}_Fill", type='AREA')
    fill_data.energy = 120.0
    fill_data.size = 2.0
    fill_data.color = (0.85, 0.92, 1.0)
    fill_obj = bpy.data.objects.new(name=f"{rig_name}_Fill", object_data=fill_data)
    fill_obj.location = (target_loc[0] - 2.2, target_loc[1] - 1.5, target_loc[2] + 1.2)
    bpy.context.collection.objects.link(fill_obj)

    # Rim / Hair Light (Crisp edge separation)
    rim_data = bpy.data.lights.new(name=f"{rig_name}_Rim", type='AREA')
    rim_data.energy = 220.0
    rim_data.size = 0.8
    rim_data.color = (1.0, 1.0, 1.0)
    rim_obj = bpy.data.objects.new(name=f"{rig_name}_Rim", object_data=rim_data)
    rim_obj.location = (target_loc[0], target_loc[1] + 2.5, target_loc[2] + 1.8)
    bpy.context.collection.objects.link(rim_obj)

    return key_obj, fill_obj, rim_obj

def setup_camera(cam_name="Studio_Atlas_Cam", location=(0, -2.8, 1.1), rotation=(math.radians(82), 0, 0), focal_length=75):
    cam = bpy.data.objects.get(cam_name)
    if not cam:
        cam_data = bpy.data.cameras.new(cam_name)
        cam = bpy.data.objects.new(cam_name, cam_data)
        bpy.context.collection.objects.link(cam)
    cam.location = location
    cam.rotation_euler = rotation
    cam.data.lens = focal_length
    bpy.context.scene.camera = cam
    return cam

def export_fbx_to_unity(filepath):
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH', 'ARMATURE', 'EMPTY'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False
    )
    print(f"  -> Exported FBX: {filepath} ({os.path.getsize(filepath) / (1024*1024):.2f} MB)")

# ---------------------------------------------------------------------------
# 3. MODULE PROCESSING FUNCTIONS
# ---------------------------------------------------------------------------
def process_module_heart():
    blend_path = os.path.join(BLENDS_DIR, "Heart_Detailed.blend")
    print(f"\n--- [Project Atlas] Processing Module 01: Heart Detailed ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    # Create epicardial fat pads along anterior/posterior IV sulci if not already present
    fat_obj = bpy.data.objects.get("Adipose_Fat_Pads")
    if not fat_obj:
        bm = bmesh.new()
        # Create subtle lobulated organic curves along anterior IV groove
        for z in [0.0, -0.2, -0.4, -0.6]:
            r = 0.08 + (0.02 * math.sin(z * 10))
            bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=r)
            for v in bm.verts[-96:]:
                v.co.x += 0.12 + 0.05 * z
                v.co.y += -0.25 - 0.1 * z
                v.co.z += z + 0.2
        mesh_data = bpy.data.meshes.new("Adipose_Fat_Pads_Mesh")
        bm.to_mesh(mesh_data)
        bm.free()
        fat_obj = bpy.data.objects.new("Adipose_Fat_Pads", mesh_data)
        bpy.context.collection.objects.link(fat_obj)
        root = bpy.data.objects.get("Four_Chamber_Heart")
        if root:
            fat_obj.parent = root

    # Assign shaders
    for o in bpy.data.objects:
        name = o.name
        if "Ventricle" in name or "Atrium" in name or "Myocardium" in name:
            assign_material_to_obj(o, "MED_MAT_Cardiac_Muscle")
        elif "Aorta" in name or "Pulmonary" in name:
            assign_material_to_obj(o, "MED_MAT_Artery")
        elif "Coronary" in name or "Arter" in name:
            assign_material_to_obj(o, "MED_MAT_Artery")
        elif "Fat" in name or "Adipose" in name:
            assign_material_to_obj(o, "MED_MAT_Heart_Fat")
        elif "Vein" in name or "Cava" in name or "Sinus" in name:
            assign_material_to_obj(o, "MED_MAT_Vein")

    setup_studio_lighting("Heart_Light", target_loc=(0, 0, 0))
    setup_camera("Heart_Cam", location=(0, -2.2, 0.2), rotation=(math.radians(88), 0, 0), focal_length=80)

    # Render preview
    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Cardiology_Heart_Detailed.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Heart_Detailed.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_module_torso():
    blend_path = os.path.join(BLENDS_DIR, "Human_Torso_Anatomy_Full.blend")
    print(f"\n--- [Project Atlas] Processing Module 02: Full Torso Anatomy ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    for o in bpy.data.objects:
        if o.type != 'MESH':
            continue
        name = o.name
        if any(w in name for w in ["Cranium", "Mandible", "Spine", "Ribcage", "Sternum", "Pelvis", "Femur", "Patella", "Tibia", "Bone", "Skeleton"]):
            assign_material_to_obj(o, "MED_MAT_Bone_Cortical")
        elif "Cartilage" in name or "Disc" in name:
            assign_material_to_obj(o, "MED_MAT_Cartilage")
        elif "Muscle" in name:
            assign_material_to_obj(o, "MED_MAT_Skeletal_Muscle")
        elif "Tendon" in name:
            assign_material_to_obj(o, "MED_MAT_Tendon")
        elif "Heart" in name:
            assign_material_to_obj(o, "MED_MAT_Cardiac_Muscle")
        elif "Aorta" in name or "Artery" in name or "Arteries" in name:
            assign_material_to_obj(o, "MED_MAT_Artery")
        elif "Vein" in name or "Vena_Cava" in name:
            assign_material_to_obj(o, "MED_MAT_Vein")
        elif "Lung" in name or "Trachea" in name:
            assign_material_to_obj(o, "MED_MAT_Lung")
        elif "Liver" in name:
            assign_material_to_obj(o, "MED_MAT_Liver")
        elif "Gallbladder" in name:
            assign_material_to_obj(o, "MED_MAT_Gallbladder")
        elif "Stomach" in name:
            assign_material_to_obj(o, "MED_MAT_Stomach")
        elif "Kidney" in name:
            assign_material_to_obj(o, "MED_MAT_Kidney")
        elif "Intestine" in name or "Colon" in name:
            assign_material_to_obj(o, "MED_MAT_Intestine")
        elif "Brain" in name or "Spinal_Cord" in name or "Nerve" in name:
            assign_material_to_obj(o, "MED_MAT_Nerve")
        elif "Silhouette" in name or "Skin" in name:
            assign_material_to_obj(o, "MED_MAT_Skin_Translucent")

    setup_studio_lighting("Torso_Light", target_loc=(0, 0, 1.0))
    setup_camera("Torso_Cam", location=(0, -3.2, 1.0), rotation=(math.radians(90), 0, 0), focal_length=65)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Torso_Cutaway_Anatomy.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Human_Torso_Anatomy_Full.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_module_dental():
    blend_path = os.path.join(BLENDS_DIR, "Dental_Mandible_Teeth.blend")
    print(f"\n--- [Project Atlas] Processing Module 03: Dental Mandible & Teeth ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    for o in bpy.data.objects:
        if o.type != 'MESH':
            continue
        name = o.name
        if "Jaw" in name or "Mandible" in name or "Maxilla" in name:
            assign_material_to_obj(o, "MED_MAT_Bone_Cortical")
        elif "Gum" in name or "Gingiva" in name:
            assign_material_to_obj(o, "MED_MAT_Gingiva")
        elif "Tooth" in name or "Incisor" in name or "Molar" in name or "Canine" in name:
            assign_material_to_obj(o, "MED_MAT_Dental_Enamel")

    setup_studio_lighting("Dental_Light", target_loc=(0, 0, 0))
    setup_camera("Dental_Cam", location=(0, -0.65, 0.05), rotation=(math.radians(82), 0, 0), focal_length=90)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Dental_Mandible_Teeth.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Dental_Mandible_Teeth.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_module_digestive():
    blend_path = os.path.join(BLENDS_DIR, "Digestive_System.blend")
    print(f"\n--- [Project Atlas] Processing Module 04: Hepatic & Digestive ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    for o in bpy.data.objects:
        if o.type != 'MESH':
            continue
        name = o.name
        if "Liver" in name or "Caudate" in name:
            assign_material_to_obj(o, "MED_MAT_Liver")
        elif "Gallbladder" in name or "Bile_Duct" in name:
            assign_material_to_obj(o, "MED_MAT_Gallbladder")
        elif "Hepatic_Artery" in name or "Artery" in name:
            assign_material_to_obj(o, "MED_MAT_Artery")
        elif "Portal_Vein" in name or "Vein" in name:
            assign_material_to_obj(o, "MED_MAT_Vein")

    setup_studio_lighting("Digestive_Light", target_loc=(0, 0, 0))
    setup_camera("Digestive_Cam", location=(0, -1.8, 0.1), rotation=(math.radians(88), 0, 0), focal_length=75)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Digestive_Hepatic_System.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Digestive_System.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_module_surgical():
    blend_path = os.path.join(BLENDS_DIR, "Surgical_Instruments_Tray.blend")
    print(f"\n--- [Project Atlas] Processing Module 05: Surgical Instruments ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    for o in bpy.data.objects:
        if o.type != 'MESH':
            continue
        if "Handle" in o.name or "Polymer" in o.name:
            assign_material_to_obj(o, "MED_MAT_Surgical_Polymer")
        else:
            assign_material_to_obj(o, "MED_MAT_Surgical_Steel")

    setup_studio_lighting("Surgical_Light", target_loc=(0, 0, 0))
    setup_camera("Surgical_Cam", location=(0, -1.2, 0.8), rotation=(math.radians(65), 0, 0), focal_length=70)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Surgical_Instruments_Tray.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Surgical_Instruments_Tray.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_module_operating_theatre():
    blend_path = os.path.join(BLENDS_DIR, "Operating_Theatre_Suite.blend")
    print(f"\n--- [Project Atlas] Processing Module 06: Operating Theatre Suite ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    # Add 4800K surgical light emitters
    for i, loc in enumerate([(0.5, 0.0, 2.5), (-0.5, 0.0, 2.5)]):
        lamp_data = bpy.data.lights.new(name=f"Surgical_Luminaire_Head_{i+1}", type='SPOT')
        lamp_data.energy = 800.0
        lamp_data.spot_size = math.radians(45)
        lamp_data.spot_blend = 0.25
        lamp_data.color = (1.0, 0.98, 0.95)
        lamp_obj = bpy.data.objects.new(name=f"Surgical_Luminaire_Head_{i+1}", object_data=lamp_data)
        lamp_obj.location = loc
        lamp_obj.rotation_euler = (math.radians(10 * (1 if i==0 else -1)), 0, 0)
        bpy.context.collection.objects.link(lamp_obj)

    setup_camera("OR_Cam", location=(2.8, -3.5, 2.0), rotation=(math.radians(72), 0, math.radians(35)), focal_length=45)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Operating_Theatre_Suite.png")
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 960
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    export_fbx_to_unity(os.path.join(MODELS_DIR, "Operating_Theatre_Suite.fbx"))
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

def process_master_showcase():
    blend_path = os.path.join(BLENDS_DIR, "Master_Medical_Anatomy_Showcase.blend")
    print(f"\n--- [Project Atlas] Processing Master Showcase ({blend_path}) ---")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    build_global_material_library()

    setup_studio_lighting("Master_Light", target_loc=(0, 0, 1.0))
    setup_camera("Master_Cam", location=(0, -4.5, 1.3), rotation=(math.radians(88), 0, 0), focal_length=50)

    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.filepath = os.path.join(RENDERS_DIR, "Atlas_Master_Showcase_Studio.png")
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    bpy.ops.render.render(write_still=True)
    print(f"  -> Rendered preview: {bpy.context.scene.render.filepath}")

    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

# ---------------------------------------------------------------------------
# 4. UNITY URP LIT MATERIAL GENERATOR
# ---------------------------------------------------------------------------
def generate_unity_urp_materials():
    print("\n--- [Project Atlas] Generating 24 Unity URP Lit Materials ---")
    urp_specs = {
        "MED_MAT_Bone_Cortical": {"color": (0.88, 0.84, 0.76, 1.0), "smoothness": 0.55, "metallic": 0.0},
        "MED_MAT_Bone_Cancellous": {"color": (0.82, 0.74, 0.62, 1.0), "smoothness": 0.40, "metallic": 0.0},
        "MED_MAT_Cartilage": {"color": (0.72, 0.80, 0.85, 0.85), "smoothness": 0.75, "metallic": 0.0},
        "MED_MAT_Skeletal_Muscle": {"color": (0.50, 0.11, 0.10, 1.0), "smoothness": 0.65, "metallic": 0.0},
        "MED_MAT_Cardiac_Muscle": {"color": (0.56, 0.08, 0.09, 1.0), "smoothness": 0.72, "metallic": 0.0},
        "MED_MAT_Tendon": {"color": (0.92, 0.90, 0.86, 1.0), "smoothness": 0.78, "metallic": 0.0},
        "MED_MAT_Artery": {"color": (0.78, 0.04, 0.05, 1.0), "smoothness": 0.82, "metallic": 0.0},
        "MED_MAT_Vein": {"color": (0.28, 0.08, 0.34, 1.0), "smoothness": 0.76, "metallic": 0.0},
        "MED_MAT_Heart_Fat": {"color": (0.92, 0.85, 0.52, 1.0), "smoothness": 0.62, "metallic": 0.0},
        "MED_MAT_Lung": {"color": (0.65, 0.26, 0.28, 1.0), "smoothness": 0.60, "metallic": 0.0},
        "MED_MAT_Liver": {"color": (0.35, 0.09, 0.08, 1.0), "smoothness": 0.78, "metallic": 0.0},
        "MED_MAT_Gallbladder": {"color": (0.12, 0.36, 0.16, 1.0), "smoothness": 0.80, "metallic": 0.0},
        "MED_MAT_Stomach": {"color": (0.62, 0.28, 0.28, 1.0), "smoothness": 0.78, "metallic": 0.0},
        "MED_MAT_Intestine": {"color": (0.58, 0.32, 0.28, 1.0), "smoothness": 0.78, "metallic": 0.0},
        "MED_MAT_Kidney": {"color": (0.38, 0.12, 0.14, 1.0), "smoothness": 0.75, "metallic": 0.0},
        "MED_MAT_Nerve": {"color": (0.94, 0.92, 0.82, 1.0), "smoothness": 0.68, "metallic": 0.0},
        "MED_MAT_Skin_Translucent": {"color": (0.85, 0.88, 0.92, 0.15), "smoothness": 0.85, "metallic": 0.0},
        "MED_MAT_Dental_Enamel": {"color": (0.96, 0.95, 0.91, 1.0), "smoothness": 0.88, "metallic": 0.0},
        "MED_MAT_Dentin": {"color": (0.90, 0.82, 0.62, 1.0), "smoothness": 0.65, "metallic": 0.0},
        "MED_MAT_Cementum": {"color": (0.85, 0.78, 0.65, 1.0), "smoothness": 0.45, "metallic": 0.0},
        "MED_MAT_Gingiva": {"color": (0.78, 0.35, 0.42, 1.0), "smoothness": 0.70, "metallic": 0.0},
        "MED_MAT_Surgical_Steel": {"color": (0.95, 0.95, 0.95, 1.0), "smoothness": 0.85, "metallic": 1.0},
        "MED_MAT_Surgical_Polymer": {"color": (0.12, 0.38, 0.72, 1.0), "smoothness": 0.72, "metallic": 0.0},
        "MED_MAT_Clinical_Glass": {"color": (1.0, 1.0, 1.0, 0.10), "smoothness": 0.95, "metallic": 0.0},
    }

    for name, spec in urp_specs.items():
        mat_path = os.path.join(MATERIALS_DIR, f"{name}.mat")
        c = spec["color"]
        s = spec["smoothness"]
        m = spec["metallic"]
        content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4a8a427976e1088d2, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _Smoothness: {s}
    - _Metallic: {m}
    - _Surface: 0
    m_Colors:
    - _BaseColor: {{r: {c[0]}, g: {c[1]}, b: {c[2]}, a: {c[3]}}}
"""
        with open(mat_path, "w", encoding="utf-8") as f:
            f.write(content)

    print(f"  -> Generated {len(urp_specs)} Unity URP materials in {MATERIALS_DIR}")

# ---------------------------------------------------------------------------
# MAIN EXECUTION
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    print("=================================================================")
    print("      PROJECT ATLAS: AAA MEDICAL VISUALIZATION PIPELINE          ")
    print("=================================================================")
    process_module_heart()
    process_module_torso()
    process_module_dental()
    process_module_digestive()
    process_module_surgical()
    process_module_operating_theatre()
    process_master_showcase()
    generate_unity_urp_materials()
    print("\n=================================================================")
    print("      PROJECT ATLAS: EXECUTION COMPLETED SUCCESSFULLY!           ")
    print("=================================================================")
