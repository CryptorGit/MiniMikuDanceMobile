# Architecture Overview

This document summarizes the design of the smartphone
MMD-compatible dance video application.

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

```text
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
```

### Why document these folders?

Documenting the directory layout clarifies where new files belong and helps
contributors and LLM tools generate code in the correct location. Each folder
has a specific role:

- **Scenes** – contains minimal Unity scenes that bootstrap the app.
- **Scripts** – holds all runtime C# code organised by feature so the project
  can be maintained without manual editor work.
- **Plugins** – third-party packages (e.g. UniVRM) kept separate from custom
  code for easy updates.
- **StreamingAssets** – large assets like the pose estimation model that must be
  included verbatim in the build.
- **Resources** – small prefabs and data loaded via `Resources.Load`.

Keeping this structure documented makes it easier to understand the project and
maintain consistency as the codebase grows.
