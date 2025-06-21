# Architecture Overview

This document summarizes the design of the smartphone MMD-compatible dance video application.

## Goals

- Import 3D character models (VRM, FBX, PMX).
- Extract dance motion from user-provided videos via pose estimation.
- Apply generated motions to humanoid models within Unity.
- Synchronize the virtual camera with device movement and record the result.

## Components

1. **Model Import**
   - VRM models are loaded at runtime using UniVRM.
   - FBX/PMX require pre-conversion or future importer support.
2. **Pose Estimation**
   - Utilizes MediaPipe Pose models executed with Unity Sentis.
   - Generates joint trajectories from input videos.
3. **Motion Generation**
   - Converts joint data into animation clips or runtime bone transforms.
4. **Camera Control**
   - Reads device sensors (gyroscope/AR tracking) to move the Unity camera.
5. **Recording**
   - Encodes rendered frames into a video file (e.g., using NatCorder).
6. **UI System**
   - UI layout is defined via JSON and instantiated by scripts at runtime.

## Folder Structure

```
Assets/
  Scenes/           - Minimal scenes (e.g., Main.unity)
  Scripts/
    App/            - Initialization and settings
    UI/             - Runtime UI generation
    Import/         - Model import logic
    PoseEstimation/ - Pose detection workers
    Motion/         - Motion data and playback
    Camera/         - Camera controllers
    Recording/      - Screen recording
    Util/           - Utility classes
  Plugins/          - Third-party packages (UniVRM, NatCorder, etc.)
  StreamingAssets/  - ML models and large data files
  Resources/        - Default assets such as stage prefabs
```

## Development Notes

- Entire scene and UI are created from code and JSON without manual editor work.
- Offline processing: pose estimation and video creation run locally on the device.
- Designed for future extensibility (multiple characters, AR background, etc.).
- LLM tools like ChatGPT can be used during development for code generation.

