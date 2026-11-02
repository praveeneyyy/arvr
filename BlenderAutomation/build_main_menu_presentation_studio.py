import bpy
import math
import os

def create_main_menu_presentation_studio():
    print("=== Building Project Atlas Main Menu Presentation Studio ===")

    # Clear default scene objects non-destructively
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    # 1. World Lighting & Clinical Background
    world = bpy.data.worlds.new(name="Clinical_Atlas_World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg_node = world.node_tree.nodes.get("Background")
    if bg_node:
        bg_node.inputs['Color'].default_value = (0.015, 0.02, 0.03, 1.0) # Deep clinical graphite/navy
        bg_node.inputs['Strength'].default_value = 0.35

    # 2. Collections Setup
    studio_col = bpy.data.collections.new("00_Studio_Rig")
    bpy.context.scene.collection.children.link(studio_col)
    
    lighting_col = bpy.data.collections.new("01_Studio_Lighting")
    bpy.context.scene.collection.children.link(lighting_col)
    
    cameras_col = bpy.data.collections.new("02_Studio_Cameras")
    bpy.context.scene.collection.children.link(cameras_col)

    # 3. Create Circular Clinical Pedestal Stage
    bpy.ops.mesh.primitive_cylinder_add(radius=1.8, depth=0.08, vertices=64, location=(0, 0, -0.04))
    pedestal = bpy.context.active_object
    pedestal.name = "Clinical_Pedestal_Stage"
    if pedestal.name not in studio_col.objects:
        studio_col.objects.link(pedestal)
    if pedestal.name in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.unlink(pedestal)
    bpy.ops.object.shade_smooth()

    # Pedestal Material
    ped_mat = bpy.data.materials.new(name="Mat_Clinical_Pedestal")
    ped_mat.use_nodes = True
    bsdf = ped_mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (0.035, 0.045, 0.065, 1.0)
        bsdf.inputs['Roughness'].default_value = 0.18
        bsdf.inputs['Metallic'].default_value = 0.25
        if 'IOR' in bsdf.inputs:
            bsdf.inputs['IOR'].default_value = 1.45
    pedestal.data.materials.append(ped_mat)

    # Contact Shadow Ground Ring
    bpy.ops.mesh.primitive_cylinder_add(radius=2.6, depth=0.01, vertices=64, location=(0, 0, -0.085))
    ground = bpy.context.active_object
    ground.name = "Pedestal_Ground_Ring"
    if ground.name not in studio_col.objects:
        studio_col.objects.link(ground)
    if ground.name in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.unlink(ground)
    
    ground_mat = bpy.data.materials.new(name="Mat_Ground_Ring")
    ground_mat.use_nodes = True
    g_bsdf = ground_mat.node_tree.nodes.get("Principled BSDF")
    if g_bsdf:
        g_bsdf.inputs['Base Color'].default_value = (0.01, 0.015, 0.02, 1.0)
        g_bsdf.inputs['Roughness'].default_value = 0.45
    ground.data.materials.append(ground_mat)

    # 4. Studio 3-Point Lighting Rig
    # Key Light (Large Soft Area Light)
    key_light_data = bpy.data.lights.new(name="Studio_KeyLight", type='AREA')
    key_light_data.energy = 450.0
    key_light_data.size = 2.5
    key_light_data.color = (0.96, 0.98, 1.0) # 5000K crisp daylight
    key_light = bpy.data.objects.new("Studio_KeyLight", key_light_data)
    key_light.location = (1.8, -2.4, 3.2)
    key_light.rotation_euler = (math.radians(45), math.radians(15), math.radians(35))
    lighting_col.objects.link(key_light)

    # Rim Light (Restrained Medical Cyan Edge Illumination)
    rim_light_data = bpy.data.lights.new(name="Studio_RimLight", type='AREA')
    rim_light_data.energy = 600.0
    rim_light_data.size = 2.0
    rim_light_data.color = (0.0, 0.85, 1.0) # Medical Cyan
    rim_light = bpy.data.objects.new("Studio_RimLight", rim_light_data)
    rim_light.location = (-2.2, 2.5, 2.8)
    rim_light.rotation_euler = (math.radians(-35), math.radians(145), 0)
    lighting_col.objects.link(rim_light)

    # Fill Light (Subtle Ambient Soft Bounce)
    fill_light_data = bpy.data.lights.new(name="Studio_FillLight", type='AREA')
    fill_light_data.energy = 180.0
    fill_light_data.size = 3.5
    fill_light_data.color = (0.75, 0.82, 0.95)
    fill_light = bpy.data.objects.new("Studio_FillLight", fill_light_data)
    fill_light.location = (-2.5, -2.0, 1.8)
    fill_light.rotation_euler = (math.radians(30), math.radians(-50), 0)
    lighting_col.objects.link(fill_light)

    # Top Down Overhead Luminaire
    top_light_data = bpy.data.lights.new(name="Studio_TopSpot", type='SPOT')
    top_light_data.energy = 220.0
    top_light_data.spot_size = math.radians(65)
    top_light_data.spot_blend = 0.5
    top_light_data.color = (0.9, 0.95, 1.0)
    top_light = bpy.data.objects.new("Studio_TopSpot", top_light_data)
    top_light.location = (0, 0, 4.0)
    top_light.rotation_euler = (0, 0, 0)
    lighting_col.objects.link(top_light)

    # 5. Presentation Cameras for the 6 Modules
    cams = [
        ("Cam_01_FullAnatomy", (0.08, -2.15, 0.42), (math.radians(88), 0, 0), 55),
        ("Cam_02_Cardiology", (0.0, -1.25, 0.38), (math.radians(88), 0, 0), 50),
        ("Cam_03_Dental", (0.0, -1.20, 0.38), (math.radians(88), 0, 0), 50),
        ("Cam_04_Hepatic", (0.0, -1.35, 0.38), (math.radians(88), 0, 0), 50),
        ("Cam_05_Instruments", (0.0, -0.95, 0.40), (math.radians(85), 0, 0), 45),
        ("Cam_06_OperatingTheatre", (0.0, -2.95, 0.85), (math.radians(82), 0, 0), 38)
    ]

    for name, pos, rot, focal in cams:
        cam_data = bpy.data.cameras.new(name=name)
        cam_data.lens = focal
        cam_data.sensor_width = 36.0
        cam_obj = bpy.data.objects.new(name, cam_data)
        cam_obj.location = pos
        cam_obj.rotation_euler = rot
        cameras_col.objects.link(cam_obj)
        if name == "Cam_01_FullAnatomy":
            bpy.context.scene.camera = cam_obj

    # 6. Import Full Torso Model as the Showcase Hero Asset
    torso_fbx = "Assets/MedicalSimulation/Art/Anatomy/Human_Torso_Anatomy_Full.fbx"
    if os.path.exists(torso_fbx):
        bpy.ops.import_scene.fbx(filepath=torso_fbx)
        imported_objs = [o for o in bpy.context.selected_objects]
        hero_col = bpy.data.collections.new("03_Showcase_Hero_Anatomy")
        bpy.context.scene.collection.children.link(hero_col)
        for o in imported_objs:
            if o.name in bpy.context.scene.collection.objects:
                bpy.context.scene.collection.objects.unlink(o)
            hero_col.objects.link(o)
        print("Imported Human_Torso_Anatomy_Full into presentation studio.")

    # 7. Render Settings (Cycles Path Tracing, OpenImageDenoise, AgX)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    cycles = scene.cycles
    cycles.device = 'CPU'
    cycles.samples = 64
    cycles.use_denoising = True
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100

    # Color Management (AgX High Contrast)
    scene.display_settings.display_device = 'sRGB'
    try:
        scene.view_settings.view_transform = 'AgX'
        scene.view_settings.look = 'AgX - High Contrast'
    except Exception as e:
        print("Color management notice:", e)

    # Save .blend file
    os.makedirs("Assets/MedicalSimulation/BlenderModels", exist_ok=True)
    out_blend = "Assets/MedicalSimulation/BlenderModels/ProjectAtlas_Presentation_Studio.blend"
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print(f"Saved Studio Blend file to: {out_blend}")

    # Render High-End Reference Image
    out_render = os.path.abspath("Assets/MedicalSimulation/Art/ProjectAtlas_MainMenu_Presentation_Studio_Cycles.png")
    os.makedirs(os.path.dirname(out_render), exist_ok=True)
    scene.render.filepath = out_render
    bpy.ops.render.render(write_still=True)
    print(f"Rendered studio reference preview to: {out_render}")

if __name__ == "__main__":
    create_main_menu_presentation_studio()
