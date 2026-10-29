import os
import bpy

RENDERS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "MedicalSimulation", "Art", "Renders"))
os.makedirs(RENDERS_DIR, exist_ok=True)

cols_map = {
    "01_Skeletal": bpy.data.collections.get("01_Skeletal_System"),
    "02_Cardiovascular": bpy.data.collections.get("02_Cardiovascular_System"),
    "03_Respiratory": bpy.data.collections.get("03_Respiratory_System"),
    "04_Digestive": bpy.data.collections.get("04_Digestive_System"),
    "05_Urinary": bpy.data.collections.get("05_Urinary_System"),
    "06_Nervous": bpy.data.collections.get("06_Nervous_System"),
    "07_Muscular": bpy.data.collections.get("07_Muscular_Surface"),
}

def set_vis(visible_keys):
    for k, col in cols_map.items():
        if col:
            is_vis = k in visible_keys
            col.hide_render = not is_vis
            col.hide_viewport = not is_vis

renders = [
    ("Render_07_Digestive_System.png", ["01_Skeletal", "04_Digestive"], 0.12, 1.05),
    ("Render_08_Urinary_System.png", ["01_Skeletal", "05_Urinary"], 0.02, 0.85),
    ("Render_09_Brain_Nervous_System.png", ["01_Skeletal", "06_Nervous"], 0.45, 0.85),
    ("Render_10_Full_Anatomical_Overview.png", ["01_Skeletal", "02_Cardiovascular", "03_Respiratory", "04_Digestive", "05_Urinary", "06_Nervous"], 0.25, 1.35)
]

cam = bpy.context.scene.camera

for filename, keys, target_z, dist in renders:
    set_vis(keys)
    if cam:
        cam.location = (0.0, -dist, target_z + 0.02)
    out_path = os.path.join(RENDERS_DIR, filename)
    bpy.context.scene.render.filepath = out_path
    print(f"Rendering: {filename}...", flush=True)
    bpy.ops.render.render(write_still=True)
    print(f"Saved: {out_path}", flush=True)

print("Remaining renders complete!", flush=True)
