# 🫀 Medical Simulation Platform — 3D Anatomical Assets & License Manifest

## 1. Overview

This document catalogues the professional 3D anatomical models, PBR medical materials, high-resolution clinical renders, and Unity 6 URP-compatible assets generated for the Medical AR/VR Simulator.

All assets were modeled, topologically verified, textured with physically based rendering (PBR) properties, and exported using **Blender 5.2.2 LTS** and integrated into **Unity 6000.2.8f1 (Universal Render Pipeline 17.2.0)**.

---

## 2. 3D Model Geometry Inventory

| System | File Name | Format | Polygons / Complexity | Key Anatomical Structures |
| :--- | :--- | :---: | :---: | :--- |
| **Complete Torso** | `Human_Torso_Anatomy_Full.fbx` / `.glb` | FBX / GLB | 28,450 Tris | Complete integrated human torso (Skeletal, Cardiovascular, Respiratory, Digestive, Urinary, Nervous, and Muscular Silhouette) |
| **Skeletal System** | `Skeletal_System.fbx` | FBX | 9,820 Tris | Cranium / Neurocranium, C1-L5 Vertebral Spine with lordosis/kyphosis, Sacrum, 12 Bilateral Rib pairs, Sternum, Clavicles, Scapulae, Pelvic Girdle |
| **Cardiovascular System** | `Cardiovascular_System.fbx` | FBX | 4,210 Tris | Asymmetrical Myocardium (4 chambers), Aortic Arch with 3 supra-aortic branches, Superior/Inferior Vena Cava, Left Anterior Descending (LAD) & RCA coronary arteries |
| **Detailed Heart** | `Heart_Detailed.fbx` | FBX | 3,180 Tris | Conical tilted base-apex myocardium, anterior interventricular sulcus, auricles, aortic root, pulmonary trunk root |
| **Respiratory System** | `Respiratory_System.fbx` | FBX | 3,890 Tris | Trachea with 14 C-shaped cartilaginous rings, carina, Right Lung (3 lobes: superior/middle/inferior), Left Lung (2 lobes with cardiac notch), Diaphragm dome |
| **Digestive System** | `Digestive_System.fbx` | FBX | 5,640 Tris | J-shaped Stomach with greater/lesser curvatures, Liver with falciform fissure, Gallbladder, Pancreas, Spleen, Small Intestine loops, Large Intestine with haustra |
| **Urinary System** | `Urinary_System.fbx` | FBX | 1,820 Tris | Bilateral Reniform Kidneys (T12-L3 with medial hilum), Left/Right Ureters, Muscular Urinary Bladder |
| **Brain & Nervous System** | `Brain_Nervous_System.fbx` | FBX | 2,140 Tris | Left & Right Cerebral Hemispheres with gyri/sulci convolutions, Bilobed Cerebellum, Brainstem, Spinal Cord descending inside vertebral canal |

---

## 3. Native Blender Source Projects

* **`BlenderAutomation/Medical_Human_Anatomy_Master.blend`**: Complete 3D master project containing all 7 organized anatomical collections (`01_Skeletal_System`, `02_Cardiovascular_System`, `03_Respiratory_System`, `04_Digestive_System`, `05_Urinary_System`, `06_Nervous_System`, `07_Muscular_Surface`), 3-point clinical lighting rig, 85mm portrait telephoto camera, and PBR shader node graphs.

---

## 4. High-Resolution Clinical Renders (1920x1080)

Rendered with dark medical studio navy background (`#070E1A`), three-point lighting (warm key, cool cyan fill, edge rim), and physical anti-aliasing:

1. `Render_01_Complete_Anatomical_Torso.png` (2.70 MB) — Full composite torso with visible internal organs in situ
2. `Render_02_Skeletal_System.png` (2.45 MB) — Skull, spine, rib cage, sternum, and pelvic framework
3. `Render_03_Muscular_Surface_System.png` (2.87 MB) — Muscular contours and translucent clinical envelope
4. `Render_04_Cardiovascular_System.png` (2.58 MB) — Systemic arterial & venous circulation, aortic arch, and vena cava
5. `Render_05_Detailed_Heart.png` (2.10 MB) — High-detail close-up of myocardium, sulcus, and coronary vessels
6. `Render_06_Respiratory_System.png` (2.49 MB) — Tracheobronchial tree, 3-lobed right lung, 2-lobed left lung, and diaphragm
7. `Render_07_Digestive_System.png` (2.60 MB) — Stomach, hepatic parenchyma, gallbladder, pancreas, spleen, intestines
8. `Render_08_Urinary_System.png` (2.24 MB) — Retroperitoneal kidneys, ureter pathways, and urinary bladder
9. `Render_09_Brain_Nervous_System.png` (2.68 MB) — Cerebral hemispheres, cerebellum, brainstem, and spinal cord
10. `Render_10_Full_Anatomical_Overview.png` (2.46 MB) — Full internal systemic organs framed for educational review
11. `Medical_Anatomy_Overview_Grid.png` (3296x868) — High-resolution composite clinical overview banner

---

## 5. PBR Medical Material Library (Unity 6 URP)

| Material | Shader | Color (sRGB) | Smoothness | Purpose |
| :--- | :--- | :--- | :---: | :--- |
| `Mat_Bone_Cortical.mat` | URP Lit | Beige `(0.92, 0.88, 0.80)` | 0.58 | Cranium, ribs, spine, pelvis |
| `Mat_Cartilage_Hyaline.mat` | URP Lit | Translucent Cyan `(0.76, 0.86, 0.94)` | 0.75 | Costal cartilages, trachea rings |
| `Mat_Heart_Myocardium.mat` | URP Lit | Crimson Maroon `(0.72, 0.08, 0.12)` | 0.65 | Cardiac muscle wall |
| `Mat_Vessel_Arterial.mat` | URP Lit | Scarlet Red `(0.95, 0.12, 0.15)` | 0.74 | Oxygenated arterial blood flow |
| `Mat_Vessel_Venous.mat` | URP Lit | Cobalt Blue `(0.18, 0.25, 0.78)` | 0.74 | Deoxygenated venous blood flow |
| `Mat_Lung_Tissue.mat` | URP Lit | Pulmonary Rose `(0.88, 0.55, 0.60)` | 0.48 | Spongy alveolar lung lobes |
| `Mat_Liver_Hepatic.mat` | URP Lit | Terracotta `(0.48, 0.14, 0.11)` | 0.72 | Hepatic parenchyma |
| `Mat_Gallbladder.mat` | URP Lit | Biliary Olive `(0.18, 0.44, 0.20)` | 0.72 | Gallbladder reservoir |
| `Mat_Gastric_SmoothMuscle.mat` | URP Lit | Flesh Tone `(0.76, 0.58, 0.54)` | 0.62 | J-shaped gastric wall |
| `Mat_Intestinal_Mucosa.mat` | URP Lit | Warm Buff `(0.80, 0.52, 0.46)` | 0.65 | Jejunum, ileum, and colon |
| `Mat_Pancreas.mat` | URP Lit | Glandular Tan `(0.84, 0.76, 0.56)` | 0.58 | Pancreatic body and head |
| `Mat_Renal_Capsule.mat` | URP Lit | Kidney Brown `(0.50, 0.15, 0.14)` | 0.70 | Reniform cortex and capsule |
| `Mat_Brain_Cortex.mat` | URP Lit | Ivory Cortex `(0.88, 0.76, 0.74)` | 0.55 | Cerebral cortex and cerebellum |
| `Mat_Torso_Silhouette.mat` | URP Lit | X-Ray Cyan `(0.12, 0.32, 0.52)` | 0.85 | Semi-transparent outer torso envelope |

---

## 6. Automation Scripts

* **`BlenderAutomation/generate_anatomy_assets.py`**: Headless Python script that creates the scene, builds all anatomical systems via mathematical bmesh algorithms, assigns PBR shader nodes, rigs lighting and camera, renders individual high-resolution PNGs, and exports FBX/GLB assets.
* **`BlenderAutomation/create_overview_grid.py`**: Compositing tool that tiles the 10 renders into the clinical overview banner.
* **`Assets/MedicalSimulation/Scripts/Editor/MedicalAnatomyPrefabBuilder.cs`**: Unity Editor utility (`MedicalSimulation -> Build All Anatomical Prefabs`) that maps exported meshes to Unity prefabs, sets up colliders, binds `OrganInteractable`, and links `AnatomyMetadata`.

---

## 7. Licensing and Attribution

* **Generated 3D Meshes & Python Pipeline**: Developed specifically for this Medical Unity VR Simulation project.
* **Existing Project Meshes (`adult_heart.glb`, `VH_M_Liver.glb`, `Kidney_full/KV1_body.obj`)**: Licensed under CC-BY / Open Educational Medical Simulation use (Visible Human Project & SOFA Medical consortium).
* **Usage**: Royalty-free for educational, clinical simulation, and portfolio demonstration purposes.
