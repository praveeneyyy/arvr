@echo off
title Antigravity Medical Blender Launcher
echo ========================================================
echo   Starting Blender 5.2 LTS with Antigravity Bridge
echo ========================================================
echo.
echo Workstation directory: %~dp0
echo Models target: %~dp0Assets\MedicalSimulation\Resources\Models
echo Blends target: %~dp0Assets\MedicalSimulation\BlenderModels
echo.

set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
set MASTER_SHOWCASE="%~dp0Assets\MedicalSimulation\BlenderModels\Master_Medical_Anatomy_Showcase.blend"

if exist %MASTER_SHOWCASE% (
    echo Opening Master Medical Anatomy Showcase...
    start "" %BLENDER% %MASTER_SHOWCASE%
) else (
    echo Opening Blender...
    start "" %BLENDER%
)

echo.
echo Blender is running!
echo In the 3D Viewport, press 'N' to open the sidebar and click the 'Antigravity' tab.
echo.
pause
