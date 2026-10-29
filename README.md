# 🏥 Medical Unity VR Simulator

> An advanced virtual reality medical training platform featuring realistic organ simulations and surgical procedures

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![SofaUnity](https://img.shields.io/badge/SofaUnity-FF6B35?style=for-the-badge&logo=physics&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![VR](https://img.shields.io/badge/Virtual%20Reality-0078D4?style=for-the-badge)
<img width="1914" height="888" alt="Main Menu" src="https://github.com/user-attachments/assets/4132edff-9ee5-4441-aef2-fb9c8f8b7c45" />

## 📖 About

Medical Unity VR is an immersive educational platform designed for medical training and anatomical exploration. Built with Unity and powered by SofaUnity for realistic soft-body physics, this simulator provides hands-on experience with various medical procedures in a safe, virtual environment.

## ✨ Features

### 🫘 Liver Surgery Simulation
- **Cutting mechanics** with realistic tissue separation
- **Squeezing and deformation** using SofaUnity physics engine
- Accurate soft-body dynamics for surgical practice

### ❤️ Cardiac System
- **Heart pumping simulator** with realistic cardiac cycle
- Dynamic heart deformation and motion
- Interactive cardiovascular anatomy exploration

### 👃 Nasal Cavity Interaction
- Real-time **deformation on collision**
- Physical feedback when a **Sphere Collider** interact with nasal structures
- Realistic collision physics

### 🦷 Dental Surgery
- **Teeth extraction simulator** with realistic mechanics
- Individual tooth manipulation and removal
- Surgical tool interaction system

### 🩸 Blood Flow Visualization
- **Interactive blood flow system** with visual feedback
- **Flow blocking mechanics** using physical objects
- Realistic Blood flow using **Blood flow path script**

## 🎮 Simulators Overview

| Simulator | Features | Physics Engine |
|-----------|----------|----------------|
| **Liver** | Cutting, Squeezing, Deformation | SofaUnity |
| **Heart** | Pumping Animation, Deformation | CustomSystem |
| **Nasal Cavity** | Collision Detection, Deformation | Unity Physics |
| **Teeth** | Extraction, Tool Interaction | Unity Physics |
| **Blood Flow** | Flow Visualization, Blocking | Unity Physics + Custom System |

## 🛠️ Built With

- **Unity** - Cross-platform game engine
- **C#** - Core programming language
- **SofaUnity** - Real-time soft tissue simulation
- **Unity XR** - VR integration framework

## 🚀 Getting Started

### Prerequisites

- Unity 2020.3 LTS or newer
- VR headset (Oculus Quest 2/3, Meta Quest, HTC Vive, or Valve Index)
- Windows 10/11 (recommended) or macOS
- 8GB RAM minimum, 16GB recommended
- SofaUnity plugin

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Ammar880121/Medical-Unity-VR.git
   ```

2. **Open in Unity Hub**
   - Launch Unity Hub
   - Click "Add" → "Add project from disk"
   - Select the cloned `Medical-Unity-VR` folder

3. **Install Dependencies**
   - Unity will automatically import required packages
   - Ensure SofaUnity plugin is properly installed
   - Configure XR settings for your VR headset

4. **Configure VR Settings**
   - Go to `Edit > Project Settings > XR Plug-in Management`
   - Enable your VR platform (Oculus, OpenVR, etc.)
   - Verify device connection

5. **Run the Project**
   - Open the main scene from `Assets/Scenes/`
   - Click Play button or build for your target platform

## 🎯 Usage

### VR Controls

| Action | VR Control | Mouse/Keyboard Control |
|--------|---------|---------|
| Grab/Hold Objects | Grip Button | Left Click or G |
| Use Tools | Trigger Button | Left Click |
| Navigate UI | Joystick/Touchpad | W/A/S/D-Q/E to move, Y/T/Space select VR Hand |
| Menu Access | Menu Button | Escape |

## 📂 Project Structure

```
Medical-Unity-VR/
├── Assets/
│   ├── Scenes/
│   │   ├── 1.unity (Main Menu)
│   │   ├── Liver Cut.unity
│   │   ├── Liver grap.unity
│   │   ├── Nasal.unity
│   │   ├── Teeth.unity
│   │   ├── Heart.unity
│   │   └── BloodFlow.unity
│   ├── Scripts/
│   │   ├── SimulationControllers/
│   │   ├── PhysicsHandlers/
│   │   ├── UI/
│   │   └── Bloodflowpath/
│   ├── Models/
│   │   ├── LiverMesh
│   │   ├── Dental (Teeth)
│   │   └── Nasal/
│   ├── Materials/
│   ├── Prefabs/
│   └── Plugins/
│       └── SofaUnity/
├── Packages/
└── ProjectSettings/
```

## 🎓 Use Cases

- **Medical Education** - Teaching anatomy and surgical procedures
- **Surgical Training** - Practice techniques in a risk-free environment
- **Patient Education** - Explaining procedures to patients
- **Research** - Studying medical VR applications and effectiveness
- **Skills Assessment** - Evaluating procedural competency

## 🔬 Technical Details

### Physics Implementation
- **SofaUnity Integration** for realistic soft tissue deformation
- **Real-time collision detection** and response
- **Custom blood flow algorithms** for circulatory visualization

### Performance Optimization
- LOD (Level of Detail) systems for complex models
- Occlusion culling for better frame rates
- Optimized mesh colliders for VR performance
- 90+ FPS target for smooth VR experience

## 📸 Screenshots
<img width="1918" height="887" alt="Liver Simulator" src="https://github.com/user-attachments/assets/b185b50c-133f-48ee-9962-ea52a5696cb8" />
<img width="1919" height="889" alt="Heart Pumping Simulator" src="https://github.com/user-attachments/assets/e9f055a7-8541-442f-b16c-03f8c75d94ca" />
<img width="1919" height="891" alt="Nasal Cavity Simulator" src="https://github.com/user-attachments/assets/25d18081-62ac-4c44-a9a0-e0e77c021a86" />
<img width="1913" height="898" alt="Teeth Extraction Simulator" src="https://github.com/user-attachments/assets/2a2f9443-dcdb-4265-ae28-84226f0a04fc" />
<img width="1916" height="883" alt="Blood Flow Simulator" src="https://github.com/user-attachments/assets/b5665d22-d2e4-4a02-b831-6afd38686274" />



⭐ Star this repository if you find it helpful!

Made with ❤️ for medical education
