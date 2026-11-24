# VR Zombie Shooter

A Unity-based Virtual Reality Zombie Shooter developed for **Meta Quest 3**. The project focuses on immersive VR interactions, weapon mechanics, zombie AI, wave-based survival and controller haptic feedback.

## 🎮 Project Overview

The player must defend their position against incoming waves of zombies using three different weapons. The game features two zombie types, Walker and Runner, each with different movement speeds and behaviours.

Developed using Unity and the XR Interaction Toolkit, the project combines VR interaction, AI navigation, physics-based shooting mechanics and immersive audio-visual feedback.

## ✨ Key Features

### Weapon System
- Three playable weapons: Pistol, SMG and Pulse Rifle.
- Weapon switching and individual ammunition management.
- Reloading mechanics.
- Raycast-based shooting for Pistol and SMG.
- Projectile-based shooting for Pulse Rifle.
- Weapon-specific muzzle flash and firing effects.
- Individual weapon firing and reloading audio.

### VR Interaction
- Meta Quest 3 controller integration.
- Trigger-based shooting.
- Controller-based weapon switching and reloading.
- Weapon-specific haptic feedback.
- Wrist-mounted VR HUD displaying gameplay information.

### Zombie AI
- Two zombie types: Walker and Runner.
- NavMesh-based navigation and pathfinding.
- Different movement speeds and attack behaviours.
- Player detection and attack distance checks.
- Zombie health and damage system.
- Visual damage feedback.
- Idle, attack, hurt and death audio.
- Navigation recovery and stuck detection.

### Wave-Based Survival
- Automatic zombie spawning.
- Progressive wave management.
- Increasing zombie encounters.
- Zombie elimination tracking.
- Score management and game-over handling.

### Player Systems
- Player health and damage management.
- Visual damage feedback.
- Score tracking.
- Game-over and restart functionality.
- VR wrist-based gameplay information.

## 🎮 Controls

| Action | Controller Input |
|---|---|
| Shoot | Right Trigger |
| Next Weapon | Right Controller A |
| Previous Weapon | Right Controller B |
| Reload | Left Controller X |
| Select Pistol | Keyboard 1 |
| Select SMG | Keyboard 2 |
| Select Pulse Rifle | Keyboard 3 |
| Reload | Keyboard R |
| Shoot (Desktop Testing) | Mouse Click |

## 🛠️ Technologies Used

- **Game Engine:** Unity
- **Programming Language:** C#
- **VR Platform:** Meta Quest 3
- **VR Framework:** Unity XR Interaction Toolkit
- **Input:** Unity XR Input System
- **AI Navigation:** Unity NavMesh
- **Audio:** Unity Audio System
- **Development Environment:** Visual Studio

## 📂 Project Structure

```text
VR-Zombie-Shooter/
│
├── Assets/
│   ├── Animations/
│   ├── Audio/
│   ├── Materials/
│   ├── Models/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   └── Textures/
│
├── Packages/
├── ProjectSettings/
└── README.md
```

*The folder structure above represents the intended organisation. Actual folders may vary depending on the project setup.*

## 🚀 Getting Started

1. Clone or download this repository.
2. Open Unity Hub.
3. Select **Add Project from Disk**.
4. Choose the downloaded project folder.
5. Open the project using a compatible Unity Editor version.
6. Allow Unity to import assets and resolve packages.
7. Open the main game scene.
8. Connect your Meta Quest 3 or use the supported desktop testing controls.
9. Run the project.

## 📱 Building for Meta Quest 3

1. Open the project in Unity.
2. Switch the build platform to Android.
3. Verify the XR and Android build settings.
4. Configure the required Meta Quest settings.
5. Build the project as an Android APK.
6. Install the APK using Meta Quest Developer Hub.
7. Launch the game directly from the headset's Apps library under Unknown Sources.

## 🎓 Project Context

This project was developed as part of an academic Augmented and Virtual Reality project, demonstrating the application of VR interaction, controller input, haptic feedback, game AI, navigation and immersive gameplay systems.

## 👨‍💻 Author

**Harshit Chowdary Jasty**

MSc Computer Science  
University College Dublin

## 📄 License

This project was developed for academic purposes. Please contact the author before redistributing or reusing project assets.
