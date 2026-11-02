"""
Antigravity-Blender Bridge Client
Allows Antigravity agent or CLI tools to inspect, command, and sync with Blender interactively.
"""

import socket
import json
import sys
import os
import time
import subprocess
import argparse

DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 9876
BLENDER_EXE = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
WORKSPACE_ROOT = r"C:\Projects\New folder"

def send_command(command, params=None, host=DEFAULT_HOST, port=DEFAULT_PORT, timeout=10.0):
    if params is None:
        params = {}
    payload = {
        "command": command,
        "params": params
    }
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(timeout)
    try:
        sock.connect((host, port))
        msg = json.dumps(payload) + "\n"
        sock.sendall(msg.encode('utf-8'))
        
        chunks = []
        while True:
            try:
                chunk = sock.recv(8192)
                if not chunk:
                    break
                chunks.append(chunk)
                if b"\n" in chunk:
                    break
            except socket.timeout:
                break
        
        response_raw = b"".join(chunks).decode('utf-8').strip()
        if not response_raw:
            return {"status": "error", "error": "Empty response from Blender bridge"}
        return json.loads(response_raw)
    except ConnectionRefusedError:
        return {"status": "error", "error": f"Connection refused. Blender is not running or Antigravity Bridge server is offline on {host}:{port}"}
    except Exception as e:
        return {"status": "error", "error": f"{type(e).__name__}: {str(e)}"}
    finally:
        sock.close()

def ping():
    return send_command("ping")

def list_objects():
    return send_command("list_objects")

def select_object(name):
    return send_command("select_object", {"name": name})

def execute_code(code_string):
    return send_command("execute_code", {"code": code_string})

def export_fbx(dest_path=None, selected_only=False):
    params = {"selected_only": selected_only}
    if dest_path:
        params["path"] = dest_path
    return send_command("export_fbx", params)

def save_blend(dest_path=None):
    params = {}
    if dest_path:
        params["path"] = dest_path
    return send_command("save_blend", params)

def launch_blender_interactive(blend_file=None):
    addon_init = os.path.join(WORKSPACE_ROOT, "BlenderAutomation", "antigravity_bridge", "__init__.py")
    script = f"""
import bpy
import sys
sys.path.append(r"{os.path.dirname(addon_init)}")
import antigravity_bridge
antigravity_bridge.register()
print("[Antigravity] Blender interactive launched with bridge online!")
"""
    cmd = [BLENDER_EXE]
    if blend_file and os.path.exists(blend_file):
        cmd.append(blend_file)
    cmd.extend(["--python-expr", script])
    
    print(f"Launching Blender with Antigravity Bridge...")
    proc = subprocess.Popen(cmd)
    # Wait for bridge to come online
    for _ in range(15):
        time.sleep(1.0)
        res = ping()
        if res.get("status") == "ok":
            print("Successfully connected to live Blender session!")
            return res
    print("Blender launched. Bridge might take a few moments to initialize.")
    return None

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Antigravity Blender Bridge Client")
    parser.add_argument("--ping", action="store_true", help="Ping the Blender bridge server")
    parser.add_argument("--info", action="store_true", help="List all objects and statistics in Blender")
    parser.add_argument("--exec", type=str, help="Execute Python code inside Blender")
    parser.add_argument("--export-fbx", type=str, help="Export FBX to specified path (or default Unity path)")
    parser.add_argument("--save-blend", type=str, help="Save .blend file")
    parser.add_argument("--select", type=str, help="Select object by name in Blender")
    parser.add_argument("--launch", action="store_true", help="Launch interactive Blender with bridge preloaded")
    parser.add_argument("--file", type=str, help="Blend file to open with --launch")

    args = parser.parse_args()

    if args.ping:
        res = ping()
        print(json.dumps(res, indent=2))
    elif args.info:
        res = list_objects()
        print(json.dumps(res, indent=2))
    elif args.exec:
        res = execute_code(args.exec)
        print(json.dumps(res, indent=2))
    elif args.export_fbx:
        res = export_fbx(dest_path=args.export_fbx)
        print(json.dumps(res, indent=2))
    elif args.save_blend:
        res = save_blend(dest_path=args.save_blend)
        print(json.dumps(res, indent=2))
    elif args.select:
        res = select_object(args.select)
        print(json.dumps(res, indent=2))
    elif args.launch:
        launch_blender_interactive(blend_file=args.file)
    else:
        # Default action: check status
        res = ping()
        print(json.dumps(res, indent=2))
