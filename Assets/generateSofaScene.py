#!/usr/bin/env python3
"""
SOFA Scene Generator for OBJ Files (Surface-Only Version)
This version works with OBJ files and doesn't require volumetric tetrahedrons
"""

def generate_sofa_scene_surface(
    mesh_path,
    output_path,
    object_name="CustomObject",
    scale=(20, 20, 20),
    rotation=(0, 0, 0),
    color="blue",
    total_mass=1.0,
    stiffness=1000,
    damping=5,
    fixed_box=None
):
    """
    Generate a SOFA scene for surface meshes (OBJ files).
    This uses spring-based deformation instead of FEM tetrahedral elements.
    
    Parameters:
    -----------
    mesh_path : str
        Path to the OBJ mesh file
    output_path : str
        Output .scn file path
    object_name : str
        Name for the object
    scale : tuple
        3D scale (x, y, z)
    rotation : tuple
        Rotation in degrees (x, y, z)
    color : str
        Visual color
    total_mass : float
        Total mass of the object
    stiffness : float
        Spring stiffness for deformation
    damping : float
        Damping factor
    fixed_box : tuple or None
        Box for fixed constraints (x_min, y_min, z_min, x_max, y_max, z_max)
    """
    
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
        
        <BoxROI template="Vec3d" box="{fixed_box[0]} {fixed_box[1]} {fixed_box[2]} {fixed_box[3]} {fixed_box[4]} {fixed_box[5]}" drawBoxes="1" name="FixedROI" />
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
    
    with open(output_path, 'w') as f:
        f.write(scene_xml)
    
    print(f"✓ Surface-based SOFA scene generated: {output_path}")
    print(f"  Object: {object_name}")
    print(f"  Mesh: {mesh_path} (OBJ)")
    print(f"  Physics: Spring-based deformation")
    return output_path


# Example usage
if __name__ == "__main__":
    
    # For OBJ files - use this simpler surface version
    generate_sofa_scene_surface(
        mesh_path="mesh/your_object.obj",  # ← YOUR PATH HERE
        output_path="my_object_scene.scn",
        object_name="MyObject",
        scale=(1, 1, 1),              # Start with scale 1
        rotation=(0, 0, 0),
        color="red",
        total_mass=1.0,
        stiffness=1000,               # Adjust for softness
        fixed_box=(-2, -2, -2, 2, 2, 2)  # Adjust based on your mesh
    )
    
    print("\n✓ Scene generated!")
    print("\n📋 Troubleshooting tips:")
    print("  1. Make sure the mesh path is correct relative to the .scn file")
    print("  2. Check Unity console for specific error messages")
    print("  3. Start with small/simple meshes to test")
    print("  4. Verify SofaUnity plugin is properly installed")