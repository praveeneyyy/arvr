from PIL import Image
import os

renders_dir = r"c:\Projects\New folder\Assets\MedicalSimulation\Art\Renders"

mappings = [
    ("Thumb_Level1_Anatomy.png", "Render_01_Complete_Anatomical_Torso.png"),
    ("Thumb_Level2_Cardiology.png", "Render_05_Detailed_Heart.png"),
    ("Thumb_Level3_Dental.png", "Thumb_Level3_Dental.png"),
    ("Thumb_Level4_Surgery.png", "Render_07_Digestive_System.png"),
    ("Thumb_Level5_Diagnosis.png", "Render_04_Cardiovascular_System.png"),
    ("Thumb_Level6_Procedure.png", "Render_09_Brain_Nervous_System.png"),
    ("Thumb_Level7_OperatingTheatre.png", "Thumb_Level7_OperatingTheatre.png"),
]

for target_name, src_name in mappings:
    src_path = os.path.join(renders_dir, src_name)
    dst_path = os.path.join(renders_dir, target_name)
    if os.path.exists(src_path):
        im = Image.open(src_path)
        im_resized = im.resize((320, 180), Image.Resampling.LANCZOS)
        im_resized.save(dst_path, "PNG")
        print(f"Created {target_name} ({im_resized.size})")
    else:
        print(f"Source not found: {src_path}")
