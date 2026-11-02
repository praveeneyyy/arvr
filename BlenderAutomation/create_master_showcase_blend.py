import bpy
import os

def build_master_showcase():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0

    print("=== ASSEMBLING MASTER MEDICAL ANATOMY SHOWCASE BLENDER SCENE ===")

    blend_files = [
        ("Human_Torso_Anatomy_Full.blend", (0, 0, 0)),
        ("Heart_Detailed.blend", (1.5, 0, 1.2)),
        ("Dental_Mandible_Teeth.blend", (-1.5, 0, 1.2)),
        ("Digestive_System.blend", (1.5, 0, 0.4)),
        ("Surgical_Instruments_Tray.blend", (-1.5, 0, 0.4)),
        ("Operating_Theatre_Suite.blend", (0, 3.5, 0))
    ]

    base_dir = r"c:\Projects\New folder\BlenderAutomation"

    for bname, offset in blend_files:
        fpath = os.path.join(base_dir, bname)
        if os.path.exists(fpath):
            with bpy.data.libraries.load(fpath, link=False) as (data_from, data_to):
                data_to.objects = [n for n in data_from.objects if n not in ['Camera', 'Light']]
            
            top_roots = []
            for obj in data_to.objects:
                if obj is not None:
                    bpy.context.collection.objects.link(obj)
                    if obj.parent is None:
                        top_roots.append(obj)
            
            for root in top_roots:
                root.location += bpy.path.mathutils.Vector(offset) if hasattr(bpy.path, 'mathutils') else bpy.data.objects[root.name].location + bpy.data.objects[root.name].matrix_world.to_translation()
                root.location = (root.location[0] + offset[0], root.location[1] + offset[1], root.location[2] + offset[2])
            print(f"Loaded {bname} at offset {offset}")

    # Studio Lighting Setup
    # 1. Key Light
    key_light_data = bpy.data.lights.new(name="Key_Light", type='AREA')
    key_light_data.energy = 450
    key_light_data.size = 2.0
    key_light_data.color = (0.95, 0.98, 1.0)
    key_light_obj = bpy.data.objects.new("Key_Light", key_light_data)
    bpy.context.collection.objects.link(key_light_obj)
    key_light_obj.location = (2.5, -3.5, 3.0)
    key_light_obj.rotation_euler = (1.0, 0.4, 0.6)

    # 2. Fill Light
    fill_light_data = bpy.data.lights.new(name="Fill_Light", type='AREA')
    fill_light_data.energy = 220
    fill_light_data.size = 3.0
    fill_light_data.color = (0.75, 0.85, 1.0)
    fill_light_obj = bpy.data.objects.new("Fill_Light", fill_light_data)
    bpy.context.collection.objects.link(fill_light_obj)
    fill_light_obj.location = (-3.0, -3.0, 2.0)
    fill_light_obj.rotation_euler = (1.1, -0.5, -0.7)

    # 3. Rim / Contour Light (Cyan medical accent)
    rim_light_data = bpy.data.lights.new(name="Rim_Light_Cyan", type='SPOT')
    rim_light_data.energy = 600
    rim_light_data.spot_size = 1.2
    rim_light_data.color = (0.0, 0.9, 1.0)
    rim_light_obj = bpy.data.objects.new("Rim_Light_Cyan", rim_light_data)
    bpy.context.collection.objects.link(rim_light_obj)
    rim_light_obj.location = (0, 3.0, 3.5)
    rim_light_obj.rotation_euler = (-1.0, 0, 3.14)

    # Camera
    cam_data = bpy.data.cameras.new(name="Studio_Camera")
    cam_data.lens = 50
    cam_obj = bpy.data.objects.new("Studio_Camera", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    cam_obj.location = (0, -4.2, 1.6)
    cam_obj.rotation_euler = (1.45, 0, 0)
    scene.camera = cam_obj

    # Save Master Showcase Blend
    out_master = os.path.join(base_dir, "Master_Medical_Anatomy_Showcase.blend")
    bpy.ops.wm.save_as_mainfile(filepath=out_master)
    print("Successfully created Master Medical Anatomy Showcase at:", out_master)

if __name__ == "__main__":
    build_master_showcase()
