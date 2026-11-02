#!/usr/bin/env python3
"""
Medical Simulation Platform - Virtual Operating Theatre Cinematic Renderer
Executes inside Blender 5.2+ LTS.
Constructs the complete 3D Operating Theatre with:
- Modern operating room walls, ceiling tiles, floor tiles
- Operating table with patient lying supine, draped with laparotomy aperture
- Lead surgeon, assistant surgeon, scrub nurse, anesthesiologist in surgical gowns
- Dual overhead surgical lamps focusing high-intensity illumination on the surgical field
- Anesthesia workstation, patient vital signs monitor, Mayo instrument stand
Renders a 1080p cinematic clinical preview into Assets/MedicalSimulation/Art/Renders/
"""

import sys
import os
import math
import bpy
import bmesh
from mathutils import Vector, Euler

BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
RENDERS_DIR = os.path.join(BASE_DIR, "Assets", "MedicalSimulation", "Art", "Renders")
os.makedirs(RENDERS_DIR, exist_ok=True)

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

    # Dark medical room ambient
    world = bpy.data.worlds.new("OR_World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.015, 0.03, 0.05, 1.0)
        bg.inputs["Strength"].default_value = 0.65

def create_mat(name, color, rough=0.5, metal=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metal
    return mat

def create_box(name, size, loc, mat=None):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    if mat: obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    return obj

def create_cylinder(name, radius, depth, loc, rot=Euler((0,0,0)), mat=None):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=24, radius1=radius, radius2=radius, depth=depth)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    if mat: obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    return obj

def build_operating_room():
    reset_scene()

    # Materials
    m_floor = create_mat("M_Floor", (0.15, 0.22, 0.26, 1.0), rough=0.25, metal=0.1)
    m_wall  = create_mat("M_Wall", (0.82, 0.88, 0.90, 1.0), rough=0.5)
    m_green = create_mat("M_SurgWall", (0.18, 0.38, 0.32, 1.0), rough=0.4)
    m_table = create_mat("M_Table", (0.10, 0.12, 0.14, 1.0), rough=0.35, metal=0.8)
    m_drape = create_mat("M_Drape", (0.08, 0.42, 0.48, 1.0), rough=0.8)
    m_skin  = create_mat("M_Skin", (0.86, 0.68, 0.58, 1.0), rough=0.45)
    m_gown  = create_mat("M_Gown", (0.07, 0.36, 0.42, 1.0), rough=0.7)
    m_metal = create_mat("M_Chrome", (0.90, 0.92, 0.95, 1.0), rough=0.15, metal=0.95)
    m_lamp  = create_mat("M_LampHousing", (0.92, 0.94, 0.96, 1.0), rough=0.2, metal=0.4)
    m_lamp_emit = create_mat("M_LampEmit", (1.0, 1.0, 1.0, 1.0), rough=0.1)
    if "Emission Color" in m_lamp_emit.node_tree.nodes["Principled BSDF"].inputs:
        m_lamp_emit.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1, 1, 1, 1)
        m_lamp_emit.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 8.0

    # 1. Room Architecture
    # Floor (8m x 8m)
    create_box("OR_Floor", (8.0, 8.0, 0.1), (0, 0, -0.05), m_floor)
    # Walls
    create_box("Wall_Back", (8.0, 0.1, 3.8), (0, 4.0, 1.9), m_wall)
    create_box("Wall_Left", (0.1, 8.0, 3.8), (-4.0, 0, 1.9), m_green)
    create_box("Wall_Right", (0.1, 8.0, 3.8), (4.0, 0, 1.9), m_wall)

    # 2. Operating Table
    create_box("Table_Base", (0.5, 0.9, 0.75), (0, 0.35, 0.375), m_table)
    create_box("Table_Top", (0.68, 2.0, 0.12), (0, 0.35, 0.82), m_table)

    # 3. Patient (Supine on Table)
    create_box("Patient_Body", (0.42, 1.75, 0.18), (0, 0.35, 0.92), m_drape)
    # Head
    create_cylinder("Patient_Head", 0.11, 0.22, (0, 1.15, 0.96), Euler((math.radians(90),0,0)), m_skin)
    # Surgical Aperture (Exposed abdomen)
    create_box("Surgical_Aperture", (0.24, 0.35, 0.05), (0, 0.35, 1.01), m_skin)

    # 4. Surgical Team Characters
    # Lead Surgeon (Right of table)
    create_cylinder("Surgeon_Lead_Body", 0.22, 1.15, (0.65, 0.35, 1.05), mat=m_gown)
    create_cylinder("Surgeon_Lead_Head", 0.11, 0.22, (0.65, 0.35, 1.68), mat=m_skin)
    # Arms reaching forward
    create_cylinder("Surgeon_Lead_Arm", 0.05, 0.45, (0.45, 0.35, 1.25), Euler((0, math.radians(65), 0)), m_gown)

    # Assistant Surgeon (Left of table)
    create_cylinder("Surgeon_Asst_Body", 0.22, 1.15, (-0.65, 0.35, 1.05), mat=m_gown)
    create_cylinder("Surgeon_Asst_Head", 0.11, 0.22, (-0.65, 0.35, 1.68), mat=m_skin)
    create_cylinder("Surgeon_Asst_Arm", 0.05, 0.45, (-0.45, 0.35, 1.25), Euler((0, math.radians(-65), 0)), m_gown)

    # Scrub Nurse (Foot of table)
    create_cylinder("Nurse_Body", 0.20, 1.10, (0.70, -0.65, 1.02), mat=m_gown)
    create_cylinder("Nurse_Head", 0.10, 0.20, (0.70, -0.65, 1.62), mat=m_skin)

    # Anesthesiologist (Head of table)
    create_cylinder("Anesth_Body", 0.22, 1.15, (-0.75, 1.15, 1.05), mat=m_gown)
    create_cylinder("Anesth_Head", 0.11, 0.22, (-0.75, 1.15, 1.68), mat=m_skin)

    # 5. Dual Overhead Surgical Lamps
    # Main Lamp Arm & Head
    create_cylinder("Lamp_Arm_1", 0.04, 1.2, (0.2, 0.35, 2.7), Euler((0, math.radians(25), 0)), m_metal)
    create_cylinder("Lamp_Head_1", 0.36, 0.10, (0.15, 0.35, 2.3), Euler((math.radians(15), 0, 0)), m_lamp)
    create_cylinder("Lamp_Glass_1", 0.32, 0.02, (0.15, 0.35, 2.24), Euler((math.radians(15), 0, 0)), m_lamp_emit)

    # Secondary Lamp
    create_cylinder("Lamp_Head_2", 0.28, 0.08, (-0.25, 0.55, 2.4), Euler((math.radians(-20), math.radians(15), 0)), m_lamp)

    # 6. Medical Equipment
    # Anesthesia Machine & Monitor (Left of patient head)
    create_box("Anesthesia_Station", (0.6, 0.7, 1.5), (-1.4, 1.1, 0.75), m_table)
    create_box("Patient_Monitor_Screen", (0.45, 0.05, 0.32), (-1.25, 1.1, 1.65), m_table)

    # Mayo Instrument Stand (Next to nurse)
    create_cylinder("Mayo_Pillar", 0.025, 0.95, (0.55, -0.45, 0.55), mat=m_metal)
    create_box("Mayo_Tray", (0.45, 0.65, 0.04), (0.40, -0.45, 1.05), m_metal)

    # IV Pole with Fluid Bags
    create_cylinder("IV_Pole", 0.02, 1.85, (-0.55, 0.95, 0.95), mat=m_metal)

    # 7. Cinematic Lights
    # Spotlight directly on surgical aperture
    spot_data = bpy.data.lights.new("Surgical_Spot_01", type='SPOT')
    spot_data.energy = 650.0
    spot_data.spot_size = math.radians(45)
    spot_data.color = (0.97, 0.99, 1.0)
    spot_obj = bpy.data.objects.new("Surgical_Spot_01", spot_data)
    spot_obj.location = (0.15, 0.35, 2.2)
    spot_obj.rotation_euler = (0, 0, 0)
    bpy.context.collection.objects.link(spot_obj)

    # Ambient Room Fill
    sun_data = bpy.data.lights.new("OR_Ambient_Fill", type='SUN')
    sun_data.energy = 1.2
    sun_data.color = (0.75, 0.88, 0.98)
    sun_obj = bpy.data.objects.new("OR_Ambient_Fill", sun_data)
    sun_obj.rotation_euler = (math.radians(50), math.radians(-35), 0)
    bpy.context.collection.objects.link(sun_obj)

    # Soft Rim Light (Cool Cyan)
    rim_data = bpy.data.lights.new("OR_Rim_Light", type='POINT')
    rim_data.energy = 180.0
    rim_data.color = (0.0, 0.82, 0.95)
    rim_obj = bpy.data.objects.new("OR_Rim_Light", rim_data)
    rim_obj.location = (1.8, -1.2, 1.6)
    bpy.context.collection.objects.link(rim_obj)

    # 8. Cinematic Camera (Surgeon Eye Perspective)
    cam_data = bpy.data.cameras.new("OR_Cinematic_Camera")
    cam_data.lens = 32
    cam_data.sensor_width = 36
    cam_obj = bpy.data.objects.new("OR_Cinematic_Camera", cam_data)
    cam_obj.location = (1.45, -0.85, 1.85)
    cam_obj.rotation_euler = (math.radians(65), 0, math.radians(48))
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj

    # 9. Render to Disk
    render_out = os.path.join(RENDERS_DIR, "Render_Operating_Theatre_Overview.png")
    bpy.context.scene.render.filepath = render_out
    bpy.ops.render.render(write_still=True)
    print(f"Cinematic Operating Theatre Rendered: {render_out}")

if __name__ == "__main__":
    print("==================================================")
    print("RENDERING 3D VIRTUAL OPERATING THEATRE PREVIEW")
    print("==================================================")
    build_operating_room()
    print("==================================================")
    print("OPERATING THEATRE PREVIEW RENDER COMPLETE (100%)")
    print("==================================================")
