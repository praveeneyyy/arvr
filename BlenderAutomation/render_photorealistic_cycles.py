"""
PROJECT ATLAS: HIGH-END PHOTOREALISTIC CYCLES RENDERING PIPELINE
Renders cinematic, medical-grade, path-traced images using Cycles + OpenImageDenoise + AgX High Contrast.
"""

import bpy
import math
import os

WORKSPACE = r"C:\Projects\New folder"
BLENDS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "BlenderModels")
RENDERS_DIR = os.path.join(WORKSPACE, "Assets", "MedicalSimulation", "Renders")
os.makedirs(RENDERS_DIR, exist_ok=True)

def configure_cycles_high_end(scene, samples=64):
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = samples
    scene.cycles.preview_samples = 32
    scene.cycles.use_denoising = True
    scene.cycles.denoiser = 'OPENIMAGEDENOISE'
    scene.cycles.denoising_input_passes = 'RGB_ALBEDO_NORMAL'
    scene.cycles.denoising_prefilter = 'ACCURATE'
    
    # Path tracing bounce limits for realistic SSS and glass/translucency
    scene.cycles.max_bounces = 8
    scene.cycles.diffuse_bounces = 4
    scene.cycles.glossy_bounces = 4
    scene.cycles.transmission_bounces = 6
    scene.cycles.volume_bounces = 2
    
    # AgX Color Management (Cinematic tone curve)
    if hasattr(scene.view_settings, "view_transform"):
        scene.view_settings.view_transform = 'AgX'
    if hasattr(scene.view_settings, "look"):
        scene.view_settings.look = 'AgX - High Contrast'
    scene.view_settings.exposure = 0.2

    # High quality film transparency and clean dark neutral clinical studio
    world = scene.world or bpy.data.worlds.new("Studio_World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs['Color'].default_value = (0.025, 0.03, 0.04, 1.0) # Deep charcoal clinical studio
        bg.inputs['Strength'].default_value = 0.8

def render_cycles_target(blend_file, output_filename, cam_loc, cam_rot, cam_focal, target_loc, samples=64):
    blend_path = os.path.join(BLENDS_DIR, blend_file)
    print(f"\n[Cycles Photorealism] Loading {blend_path}...")
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    
    scene = bpy.context.scene
    configure_cycles_high_end(scene, samples=samples)
    
    # Setup high-end softbox surgical lighting
    # Clear existing lights
    for o in list(bpy.data.objects):
        if o.type == 'LIGHT':
            bpy.data.objects.remove(o, do_unlink=True)
            
    # Key Softbox (4800K daylight, 450W)
    key_d = bpy.data.lights.new("Cycles_Key", 'AREA')
    key_d.shape = 'RECTANGLE'
    key_d.size = 1.4
    key_d.size_y = 0.9
    key_d.energy = 450.0
    key_d.color = (1.0, 0.97, 0.92)
    key_o = bpy.data.objects.new("Cycles_Key", key_d)
    key_o.location = (target_loc[0] + 1.6, target_loc[1] - 2.0, target_loc[2] + 1.8)
    bpy.context.collection.objects.link(key_o)
    
    # Fill Softbox (Cool soft clinical fill, 160W)
    fill_d = bpy.data.lights.new("Cycles_Fill", 'AREA')
    fill_d.shape = 'RECTANGLE'
    fill_d.size = 2.0
    fill_d.size_y = 1.5
    fill_d.energy = 160.0
    fill_d.color = (0.86, 0.92, 1.0)
    fill_o = bpy.data.objects.new("Cycles_Fill", fill_d)
    fill_o.location = (target_loc[0] - 2.0, target_loc[1] - 1.2, target_loc[2] + 1.2)
    bpy.context.collection.objects.link(fill_o)
    
    # Crisp Rim Light (Edge separation, 280W)
    rim_d = bpy.data.lights.new("Cycles_Rim", 'AREA')
    rim_d.shape = 'DISK'
    rim_d.size = 1.0
    rim_d.energy = 280.0
    rim_d.color = (0.95, 0.98, 1.0)
    rim_o = bpy.data.objects.new("Cycles_Rim", rim_d)
    rim_o.location = (target_loc[0] + 0.3, target_loc[1] + 2.2, target_loc[2] + 1.6)
    bpy.context.collection.objects.link(rim_o)
    
    # Camera with subtle realistic Depth of Field
    cam_d = bpy.data.cameras.new("Cycles_Cam")
    cam_d.lens = cam_focal
    cam_d.dof.use_dof = True
    cam_d.dof.focus_distance = math.sqrt((cam_loc[0]-target_loc[0])**2 + (cam_loc[1]-target_loc[1])**2 + (cam_loc[2]-target_loc[2])**2)
    cam_d.dof.aperture_fstop = 5.6 # Clean, subtle focus falloff
    
    cam_o = bpy.data.objects.new("Cycles_Cam", cam_d)
    cam_o.location = cam_loc
    cam_o.rotation_euler = cam_rot
    bpy.context.collection.objects.link(cam_o)
    scene.camera = cam_o
    
    # Render Output settings
    out_path = os.path.join(RENDERS_DIR, output_filename)
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.render.filepath = out_path
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    
    print(f"  -> Path tracing with Cycles ({samples} samples + OIDN Denoise) to {output_filename}...")
    bpy.ops.render.render(write_still=True)
    print(f"  -> Render completed successfully: {out_path} ({os.path.getsize(out_path)/(1024*1024):.2f} MB)")

if __name__ == "__main__":
    print("=================================================================")
    print("     PROJECT ATLAS: HIGH-END CYCLES RAY-TRACED RENDERING         ")
    print("=================================================================")
    
    # 1. Cardiology Heart Detailed (Photorealistic SSS, wet serous sheen, coronary LAD/RCA vessels)
    render_cycles_target(
        blend_file="Heart_Detailed.blend",
        output_filename="Atlas_Cardiology_Heart_Cycles_Photoreal.png",
        cam_loc=(0, -2.1, 0.2),
        cam_rot=(math.radians(88), 0, 0),
        cam_focal=85,
        target_loc=(0, 0, 0),
        samples=48
    )
    
    # 2. Dental Mandible & Teeth (Enamel translucency, stippled vascular gingiva)
    render_cycles_target(
        blend_file="Dental_Mandible_Teeth.blend",
        output_filename="Atlas_Dental_Teeth_Cycles_Photoreal.png",
        cam_loc=(0, -0.65, 0.05),
        cam_rot=(math.radians(82), 0, 0),
        cam_focal=90,
        target_loc=(0, 0, 0),
        samples=48
    )
    
    # 3. Surgical Instruments Tray (AISI 316L brushed steel & mirror-polished bevels)
    render_cycles_target(
        blend_file="Surgical_Instruments_Tray.blend",
        output_filename="Atlas_Surgical_Tray_Cycles_Photoreal.png",
        cam_loc=(0, -0.60, 0.45),
        cam_rot=(math.radians(55), 0, 0),
        cam_focal=85,
        target_loc=(0, 0, 0),
        samples=48
    )

    # 4. Master Medical Anatomy Showcase (Full anatomical cutaway studio view)
    render_cycles_target(
        blend_file="Master_Medical_Anatomy_Showcase.blend",
        output_filename="Atlas_Master_Showcase_Cycles_Photoreal.png",
        cam_loc=(0, -4.5, 1.3),
        cam_rot=(math.radians(88), 0, 0),
        cam_focal=55,
        target_loc=(0, 0, 1.0),
        samples=48
    )
    
    print("\n[Cycles Photorealism] All 4 Photorealistic Ray-Traced Renders Finished!")
