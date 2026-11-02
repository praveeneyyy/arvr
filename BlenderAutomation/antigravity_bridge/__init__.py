bl_info = {
    "name": "Antigravity Bridge",
    "author": "Antigravity & Google DeepMind Team",
    "version": (1, 0, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar > Antigravity Tab",
    "description": "Live bidirectional communication and one-click Unity sync between Antigravity and Blender",
    "category": "Development",
}

import bpy
import json
import socket
import threading
import queue
import io
import sys
import os
import traceback
from bpy.props import StringProperty, IntProperty, BoolProperty, EnumProperty
from bpy.types import Panel, Operator, PropertyGroup

DEFAULT_PORT = 9876
_server_thread = None
_server_socket = None
_server_running = False
_task_queue = queue.Queue()
_request_counter = 0

def get_workspace_root():
    blend_path = bpy.data.filepath
    if blend_path and "New folder" in blend_path:
        idx = blend_path.find("New folder")
        return os.path.join(blend_path[:idx + len("New folder")])
    return r"C:\Projects\New folder"

def get_unity_models_dir():
    ws = get_workspace_root()
    path = os.path.join(ws, "Assets", "MedicalSimulation", "Resources", "Models")
    os.makedirs(path, exist_ok=True)
    return path

def get_unity_blends_dir():
    ws = get_workspace_root()
    path = os.path.join(ws, "Assets", "MedicalSimulation", "BlenderModels")
    os.makedirs(path, exist_ok=True)
    return path

def run_command_in_blender(command, params):
    res = {"status": "ok", "error": None, "result": None}
    try:
        if command == "ping":
            act = None
            try:
                if hasattr(bpy.context, "view_layer") and bpy.context.view_layer.objects.active:
                    act = bpy.context.view_layer.objects.active.name
                elif hasattr(bpy.context, "active_object") and bpy.context.active_object:
                    act = bpy.context.active_object.name
            except Exception:
                pass
            res["result"] = {
                "connected": True,
                "blender_version": ".".join(map(str, bpy.app.version)),
                "file": bpy.data.filepath or "Untitled.blend",
                "objects_count": len(bpy.data.objects),
                "active_object": act,
                "is_background": bpy.app.background
            }

        elif command == "list_objects":
            objs = []
            for obj in bpy.data.objects:
                v_count = len(obj.data.vertices) if obj.type == 'MESH' and obj.data else 0
                p_count = len(obj.data.polygons) if obj.type == 'MESH' and obj.data else 0
                objs.append({
                    "name": obj.name,
                    "type": obj.type,
                    "vertices": v_count,
                    "polygons": p_count,
                    "visible": obj.visible_get(),
                    "location": [round(c, 4) for c in obj.location]
                })
            res["result"] = objs

        elif command == "select_object":
            name = params.get("name")
            if name in bpy.data.objects:
                bpy.ops.object.select_all(action='DESELECT')
                obj = bpy.data.objects[name]
                obj.select_set(True)
                bpy.context.view_layer.objects.active = obj
                res["result"] = f"Selected {name}"
            else:
                res["status"] = "error"
                res["error"] = f"Object '{name}' not found"

        elif command == "execute_code":
            code = params.get("code", "")
            old_stdout = sys.stdout
            old_stderr = sys.stderr
            captured_out = io.StringIO()
            captured_err = io.StringIO()
            sys.stdout = captured_out
            sys.stderr = captured_err
            exec_globals = {"bpy": bpy, "context": bpy.context, "data": bpy.data}
            exec_locals = {}
            try:
                exec(code, exec_globals, exec_locals)
            finally:
                sys.stdout = old_stdout
                sys.stderr = old_stderr
            res["result"] = {
                "stdout": captured_out.getvalue(),
                "stderr": captured_err.getvalue()
            }

        elif command == "export_fbx":
            dest_path = params.get("path")
            selected_only = params.get("selected_only", False)
            if not dest_path:
                dest_path = os.path.join(get_unity_models_dir(), "Exported_From_Blender.fbx")
            os.makedirs(os.path.dirname(dest_path), exist_ok=True)
            
            bpy.ops.export_scene.fbx(
                filepath=dest_path,
                use_selection=selected_only,
                global_scale=1.0,
                apply_unit_scale=True,
                apply_scale_options='FBX_SCALE_ALL',
                axis_forward='-Z',
                axis_up='Y',
                bake_space_transform=True,
                object_types={'MESH', 'ARMATURE', 'EMPTY'},
                use_mesh_modifiers=True,
                mesh_smooth_type='FACE',
                add_leaf_bones=False
            )
            res["result"] = {"exported_path": dest_path, "size_bytes": os.path.getsize(dest_path)}

        elif command == "save_blend":
            dest_path = params.get("path")
            if not dest_path:
                dest_path = bpy.data.filepath
                if not dest_path:
                    dest_path = os.path.join(get_unity_blends_dir(), "Active_Work.blend")
            os.makedirs(os.path.dirname(dest_path), exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=dest_path)
            res["result"] = {"saved_path": dest_path}

        else:
            res["status"] = "error"
            res["error"] = f"Unknown command: '{command}'"

    except Exception as e:
        res["status"] = "error"
        res["error"] = f"{type(e).__name__}: {str(e)}\n{traceback.format_exc()}"

    return res

# ---------------------------------------------------------------------------
# Server / Timer logic
# ---------------------------------------------------------------------------
def _process_queue():
    global _task_queue, _server_running
    if not _server_running:
        return None  # Unregister timer

    while not _task_queue.empty():
        try:
            req_id, command, params, client_conn = _task_queue.get_nowait()
        except queue.Empty:
            break

        res = run_command_in_blender(command, params)
        res["id"] = req_id
        try:
            payload = (json.dumps(res) + "\n").encode('utf-8')
            client_conn.sendall(payload)
            client_conn.close()
        except Exception:
            pass

    return 0.05  # Re-run every 50ms

def _socket_listener(port):
    global _server_socket, _server_running, _request_counter
    _server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    _server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        _server_socket.bind(('127.0.0.1', port))
        _server_socket.listen(5)
        _server_socket.settimeout(0.5)
    except Exception as e:
        print(f"[Antigravity Bridge] Error binding to port {port}: {e}")
        _server_running = False
        return

    print(f"[Antigravity Bridge] Server listening on 127.0.0.1:{port}")
    while _server_running:
        try:
            client_conn, addr = _server_socket.accept()
        except socket.timeout:
            continue
        except Exception:
            break

        try:
            data = b""
            while b"\n" not in data:
                chunk = client_conn.recv(4096)
                if not chunk:
                    break
                data += chunk
            if not data:
                client_conn.close()
                continue
            line = data.decode('utf-8').strip()
            msg = json.loads(line)
            req_id = msg.get("id", _request_counter)
            _request_counter += 1
            cmd = msg.get("command", "ping")
            params = msg.get("params", {})

            # For ping or list_objects or background mode, process immediately
            if cmd in ("ping", "list_objects") or bpy.app.background:
                res = run_command_in_blender(cmd, params)
                res["id"] = req_id
                payload = (json.dumps(res) + "\n").encode('utf-8')
                client_conn.sendall(payload)
                client_conn.close()
            else:
                # In GUI mode, queue modifying actions to the main Blender UI thread
                _task_queue.put((req_id, cmd, params, client_conn))
        except Exception as e:
            try:
                err_payload = (json.dumps({"status": "error", "error": str(e)}) + "\n").encode('utf-8')
                client_conn.sendall(err_payload)
                client_conn.close()
            except Exception:
                pass

    if _server_socket:
        try:
            _server_socket.close()
        except Exception:
            pass
    print("[Antigravity Bridge] Server stopped")

def start_bridge_server(port=DEFAULT_PORT):
    global _server_thread, _server_running
    if _server_running:
        return True
    _server_running = True
    _server_thread = threading.Thread(target=_socket_listener, args=(port,), daemon=True)
    _server_thread.start()
    if not bpy.app.timers.is_registered(_process_queue):
        bpy.app.timers.register(_process_queue)
    return True

def stop_bridge_server():
    global _server_running, _server_socket
    _server_running = False
    if _server_socket:
        try:
            _server_socket.close()
        except Exception:
            pass
    return False

# ---------------------------------------------------------------------------
# Operators
# ---------------------------------------------------------------------------
class ANTIGRAVITY_OT_start_server(Operator):
    bl_idname = "antigravity.start_server"
    bl_label = "Start Bridge Server"
    bl_description = "Start the Antigravity live communication server on localhost:9876"

    def execute(self, context):
        props = context.scene.antigravity_props
        start_bridge_server(props.port)
        self.report({'INFO'}, f"Antigravity Bridge Server listening on port {props.port}")
        return {'FINISHED'}

class ANTIGRAVITY_OT_stop_server(Operator):
    bl_idname = "antigravity.stop_server"
    bl_label = "Stop Bridge Server"
    bl_description = "Stop the Antigravity live bridge server"

    def execute(self, context):
        stop_bridge_server()
        self.report({'INFO'}, "Antigravity Bridge Server stopped")
        return {'FINISHED'}

class ANTIGRAVITY_OT_export_unity_fbx(Operator):
    bl_idname = "antigravity.export_unity_fbx"
    bl_label = "Export to Unity FBX"
    bl_description = "Export active selection or model directly into Unity Resources/Models"

    filename: StringProperty(name="File Name", default="")
    selected_only: BoolProperty(name="Selected Only", default=True)

    def execute(self, context):
        props = context.scene.antigravity_props
        target_name = self.filename or props.target_module
        if not target_name.lower().endswith(".fbx"):
            target_name += ".fbx"

        dest_dir = get_unity_models_dir()
        dest_path = os.path.join(dest_dir, target_name)

        bpy.ops.export_scene.fbx(
            filepath=dest_path,
            use_selection=self.selected_only,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
            bake_space_transform=True,
            object_types={'MESH', 'ARMATURE', 'EMPTY'},
            use_mesh_modifiers=True,
            mesh_smooth_type='FACE',
            add_leaf_bones=False
        )
        file_size_mb = os.path.getsize(dest_path) / (1024 * 1024)
        self.report({'INFO'}, f"Exported: {target_name} ({file_size_mb:.2f} MB) to Unity")
        return {'FINISHED'}

class ANTIGRAVITY_OT_save_unity_blend(Operator):
    bl_idname = "antigravity.save_unity_blend"
    bl_label = "Save .Blend to Unity"
    bl_description = "Save current blend file directly into Assets/MedicalSimulation/BlenderModels"

    def execute(self, context):
        props = context.scene.antigravity_props
        target_name = props.target_module
        if not target_name.lower().endswith(".blend"):
            target_name += ".blend"

        dest_dir = get_unity_blends_dir()
        dest_path = os.path.join(dest_dir, target_name)
        bpy.ops.wm.save_as_mainfile(filepath=dest_path)
        self.report({'INFO'}, f"Saved .blend to {dest_path}")
        return {'FINISHED'}

# ---------------------------------------------------------------------------
# Properties & UI Panel
# ---------------------------------------------------------------------------
class AntigravityProperties(PropertyGroup):
    port: IntProperty(
        name="Port",
        description="Port for TCP Bridge server",
        default=DEFAULT_PORT,
        min=1024,
        max=65535
    )
    target_module: EnumProperty(
        name="Target Module",
        description="Select destination medical asset module for export",
        items=[
            ("Human_Torso_Anatomy_Full", "Torso Anatomy Full", "Full body organs and skeleton"),
            ("Heart_Detailed", "Cardiology Heart", "Detailed 4-chamber heart"),
            ("Dental_Mandible_Teeth", "Dental & Teeth", "Mandible, maxilla, and 32 individual teeth"),
            ("Digestive_System", "Digestive System", "Liver segments, gallbladder, intestines"),
            ("Surgical_Instruments_Tray", "Surgical Tray", "Scalpel, forceps, clamps, scissors"),
            ("Operating_Theatre_Suite", "Operating Suite", "Full hospital surgical room"),
        ],
        default="Human_Torso_Anatomy_Full"
    )

class ANTIGRAVITY_PT_main_panel(Panel):
    bl_label = "Antigravity Bridge"
    bl_idname = "ANTIGRAVITY_PT_main_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Antigravity'

    def draw(self, context):
        layout = self.layout
        props = context.scene.antigravity_props
        global _server_running

        # Header / Status Box
        box = layout.box()
        col = box.column(align=True)
        row = col.row()
        if _server_running:
            row.label(text="Bridge: CONNECTED", icon='RADIOBUT_ON')
            col.label(text=f"Listening on 127.0.0.1:{props.port}", icon='NETWORK_DRIVE')
            col.operator("antigravity.stop_server", text="Stop Server", icon='CANCEL')
        else:
            row.label(text="Bridge: OFFLINE", icon='RADIOBUT_OFF')
            col.prop(props, "port")
            col.operator("antigravity.start_server", text="Start Server", icon='PLAY')

        # Target Module Selector
        box = layout.box()
        box.label(text="Unity Medical Target", icon='COMMUNITY')
        box.prop(props, "target_module", text="")

        # Quick Export Buttons
        col = box.column(align=True)
        col.scale_y = 1.3
        op1 = col.operator("antigravity.export_unity_fbx", text="Sync Active/Selected to Unity FBX", icon='EXPORT')
        op1.selected_only = True
        
        op2 = col.operator("antigravity.export_unity_fbx", text="Sync Entire Scene to Unity FBX", icon='SCENE_DATA')
        op2.selected_only = False

        col.operator("antigravity.save_unity_blend", text="Save Native .Blend to Unity", icon='FILE_TICK')

        # Scene Stats Box
        box = layout.box()
        box.label(text="Scene Quick Stats", icon='INFO')
        active = context.active_object
        if active:
            box.label(text=f"Active: {active.name} ({active.type})")
            if active.type == 'MESH' and active.data:
                box.label(text=f"Verts: {len(active.data.vertices):,} | Faces: {len(active.data.polygons):,}")
        else:
            box.label(text="No active object selected")
        box.label(text=f"Total Scene Objects: {len(context.scene.objects)}")

# ---------------------------------------------------------------------------
# Registration
# ---------------------------------------------------------------------------
classes = (
    AntigravityProperties,
    ANTIGRAVITY_OT_start_server,
    ANTIGRAVITY_OT_stop_server,
    ANTIGRAVITY_OT_export_unity_fbx,
    ANTIGRAVITY_OT_save_unity_blend,
    ANTIGRAVITY_PT_main_panel,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.antigravity_props = bpy.props.PointerProperty(type=AntigravityProperties)
    # Automatically start server on load
    start_bridge_server(DEFAULT_PORT)
    print("[Antigravity Bridge] Add-on registered and server initialized.")

def unregister():
    stop_bridge_server()
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.antigravity_props
    print("[Antigravity Bridge] Add-on unregistered.")

if __name__ == "__main__":
    register()
