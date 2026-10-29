#!/usr/bin/env python3
"""
SOFA Medical Scene Generator (Surface & Volumetric Tissue Simulation)
Extended for Medical Unity VR Simulator with physiological tissue presets:
- Cardiac myocardium (Heart)
- Hepatic parenchyma (Liver)
- Pulmonary sponge (Lungs)
- Renal cortex & medulla (Kidneys)
- Alimentary smooth muscle (Stomach/Intestine)
"""

import sys
import os

TISSUE_PRESETS = {
    "heart": {
        "stiffness": 1500,
        "damping": 8,
        "total_mass": 0.35,  # ~350g adult human heart
        "color": "0.75 0.10 0.12 1.0",
        "description": "Cardiac muscle - contractile viscoelastic tissue"
    },
    "liver": {
        "stiffness": 800,
        "damping": 6,
        "total_mass": 1.5,   # ~1.5kg adult human liver
        "color": "0.48 0.14 0.11 1.0",
        "description": "Hepatic parenchyma - friable soft parenchymal tissue"
    },
    "lung": {
        "stiffness": 350,
        "damping": 4,
        "total_mass": 0.50,  # ~500g lung tissue
        "color": "0.88 0.55 0.60 1.0",
        "description": "Pulmonary tissue - high compliance, spongy matrix"
    },
    "kidney": {
        "stiffness": 1100,
        "damping": 7,
        "total_mass": 0.15,  # ~150g adult kidney
        "color": "0.55 0.18 0.14 1.0",
        "description": "Renal tissue - encapsulated cortex and medullary pyramids"
    },
    "stomach": {
        "stiffness": 600,
        "damping": 5,
        "total_mass": 0.20,
        "color": "0.82 0.65 0.58 1.0",
        "description": "Gastric smooth muscle wall"
    }
}

def generate_sofa_scene_surface(
    mesh_path,
    output_path,
    object_name="CustomObject",
    tissue_type=None,
    scale=(1.0, 1.0, 1.0),
    rotation=(0, 0, 0),
    color=None,
    total_mass=1.0,
    stiffness=1000,
    damping=5,
    fixed_box=None
):
    """
    Generate a SOFA scene for surface meshes (OBJ files) using Spring/FEM physics.
    Supports preset medical tissue types for accurate physiological deformation.
    """
    if tissue_type and tissue_type.lower() in TISSUE_PRESETS:
        preset = TISSUE_PRESETS[tissue_type.lower()]
        stiffness = preset["stiffness"]
        damping = preset["damping"]
        total_mass = preset["total_mass"]
        color = preset["color"]
        print(f"Applying medical tissue preset: [{tissue_type.upper()}] - {preset['description']}")
    
    if color is None:
        color = "0.2 0.5 0.8 1.0"
        
    if fixed_box is None:
        fixed_box = (-5, -5, -5, 5, 5, 5)
    
    scene_xml = f'''<?xml version="1.0" ?>
<Node name="root" dt="0.02" gravity="0 -9.81 0">
    <RequiredPlugin name="Sofa.Component.Collision.Detection.Algorithm"/>
    <RequiredPlugin name="Sofa.Component.Collision.Detection.Intersection"/>
    <RequiredPlugin name="Sofa.Component.Collision.Geometry"/>
    <RequiredPlugin name="Sofa.Component.Collision.Response.Contact"/>
    <RequiredPlugin name="Sofa.Component.Constraint.Projective"/>
    <RequiredPlugin name="Sofa.Component.Engine.Select"/>
    <RequiredPlugin name="Sofa.Component.IO.Mesh"/>
    <RequiredPlugin name="Sofa.Component.LinearSolver.Iterative"/>
    <RequiredPlugin name="Sofa.Component.Mapping.Linear"/>
    <RequiredPlugin name="Sofa.Component.Mass"/>
    <RequiredPlugin name="Sofa.Component.ODESolver.Backward"/>
    <RequiredPlugin name="Sofa.Component.SolidMechanics.Spring"/>
    <RequiredPlugin name="Sofa.Component.StateContainer"/>
    <RequiredPlugin name="Sofa.Component.Topology.Container.Dynamic"/>
    <RequiredPlugin name="Sofa.Component.Visual"/>
    <RequiredPlugin name="Sofa.GL.Component.Rendering3D"/>
    
    <VisualStyle displayFlags="showVisual showBehaviorModels" />
    <DefaultAnimationLoop />
    <DefaultVisualManagerLoop />
    
    <DefaultPipeline depth="6" verbose="0" draw="0" />
    <BruteForceBroadPhase/>
    <BVHNarrowPhase/>
    <MinProximityIntersection name="Proximity" alarmDistance="0.5" contactDistance="0.3" />
    <DefaultContactManager name="Response" response="PenalityContactForceField" />

    <Node name="{object_name}">
        <EulerImplicitSolver name="cg_odesolver" rayleighStiffness="0.1" rayleighMass="0.1" />
        <CGLinearSolver iterations="25" name="linear solver" tolerance="1.0e-9" threshold="1.0e-9" />
        
        <MeshOBJLoader name="loader" filename="{mesh_path}" />
        <MeshTopology src="@loader" />
        <MechanicalObject src="@loader" name="mechObject" scale3d="{scale[0]} {scale[1]} {scale[2]}" rotation="{rotation[0]} {rotation[1]} {rotation[2]}" />
        
        <UniformMass totalMass="{total_mass}" />
        
        <BoxROI template="Vec3d" box="{fixed_box[0]} {fixed_box[1]} {fixed_box[2]} {fixed_box[3]} {fixed_box[4]} {fixed_box[5]}" drawBoxes="0" name="FixedROI" />
        <FixedConstraint indices="@FixedROI.indices" />
        
        <MeshSpringForceField name="Springs" stiffness="{stiffness}" damping="{damping}" />
        
        <TriangleCollisionModel />
        <LineCollisionModel />
        <PointCollisionModel />
        
        <Node name="{object_name}Visual">
            <OglModel name="Visual" color="{color}" />
            <IdentityMapping input="@../mechObject" output="@Visual" />
        </Node>
    </Node>
</Node>'''
    
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(scene_xml)
    
    print(f"[OK] Medical SOFA simulation scene generated: {output_path}")
    print(f"  Organ/Object: {object_name}")
    print(f"  Stiffness: {stiffness} N/m | Damping: {damping} | Mass: {total_mass} kg")
    return output_path

if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(__file__), "SofaUnity", "Scenes", "SofaScenes")
    
    # Generate presets for Heart, Liver, Kidney, and Lung
    generate_sofa_scene_surface(
        mesh_path="mesh/heart.obj",
        output_path=os.path.join(out_dir, "heart_simulation.scn"),
        object_name="Heart_Myocardium",
        tissue_type="heart",
        scale=(1, 1, 1)
    )
    
    generate_sofa_scene_surface(
        mesh_path="mesh/kidney.obj",
        output_path=os.path.join(out_dir, "kidney_simulation.scn"),
        object_name="Kidney_Parenchyma",
        tissue_type="kidney",
        scale=(1, 1, 1)
    )

    print("\n[OK] All Medical SOFA tissue simulations generated successfully.")
