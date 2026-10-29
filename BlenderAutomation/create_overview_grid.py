import os
from PIL import Image, ImageDraw, ImageFont

RENDERS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "MedicalSimulation", "Art", "Renders"))

images_info = [
    ("Render_01_Complete_Anatomical_Torso.png", "Complete Anatomical Torso"),
    ("Render_02_Skeletal_System.png", "Skeletal Framework"),
    ("Render_03_Muscular_Surface_System.png", "Muscular & Surface Anatomy"),
    ("Render_04_Cardiovascular_System.png", "Cardiovascular Circulation"),
    ("Render_05_Detailed_Heart.png", "Detailed Cardiac Myocardium"),
    ("Render_06_Respiratory_System.png", "Respiratory Lungs & Trachea"),
    ("Render_07_Digestive_System.png", "Digestive Alimentary Canal"),
    ("Render_08_Urinary_System.png", "Renal & Urinary Tract"),
    ("Render_09_Brain_Nervous_System.png", "Brain & Central Nervous System"),
    ("Render_10_Full_Anatomical_Overview.png", "Comprehensive Organ Systems")
]

# Create a 5 column x 2 row grid
cols = 5
rows = 2
thumb_w = 640
thumb_h = 360
pad = 16
header_h = 100

grid_w = cols * thumb_w + (cols + 1) * pad
grid_h = rows * thumb_h + (rows + 1) * pad + header_h

canvas = Image.new("RGBA", (grid_w, grid_h), (7, 14, 26, 255)) # Clinical dark navy #070E1A
draw = ImageDraw.Draw(canvas)

# Header Banner
draw.rectangle([(0, 0), (grid_w, header_h)], fill=(12, 22, 38, 255))
draw.line([(0, header_h), (grid_w, header_h)], fill=(0, 229, 255, 255), width=2) # Cyan accent

# Title text
draw.text((pad + 10, 25), "MEDICAL VR/AR SIMULATION PLATFORM — 3D ANATOMICAL ASSET LIBRARY", fill=(0, 229, 255, 255))
draw.text((pad + 10, 55), "High-Fidelity Physically Based Anatomical Systems | Blender 5.2 LTS -> Unity 6 URP", fill=(160, 210, 240, 255))

for idx, (filename, label) in enumerate(images_info):
    r = idx // cols
    c = idx % cols
    
    x = pad + c * (thumb_w + pad)
    y = header_h + pad + r * (thumb_h + pad)
    
    img_path = os.path.join(RENDERS_DIR, filename)
    if os.path.exists(img_path):
        img = Image.open(img_path).convert("RGBA")
        img = img.resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        canvas.paste(img, (x, y))
        
        # Draw frame border
        draw.rectangle([(x, y), (x + thumb_w, y + thumb_h)], outline=(0, 180, 216, 180), width=2)
        
        # Draw label banner at bottom of thumbnail
        draw.rectangle([(x, y + thumb_h - 32), (x + thumb_w, y + thumb_h)], fill=(4, 10, 18, 220))
        draw.text((x + 10, y + thumb_h - 24), label, fill=(0, 255, 153, 255))

out_path = os.path.join(RENDERS_DIR, "Medical_Anatomy_Overview_Grid.png")
canvas.save(out_path, "PNG")
print(f"[OK] Overview grid created: {out_path} ({grid_w}x{grid_h})")
