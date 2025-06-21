# Architecture Overview

This document summarizes the design of the smartphone
MMD-compatible dance video application. The app ships with a
standalone viewer implemented in pure C# using OpenTK.

## Goals

- Import 3D character models (VRM, FBX, PMX).
- Extract dance motion from user-provided videos via pose estimation.
- Apply generated motions to humanoid models in the custom viewer.
- Synchronize the virtual camera with device movement and record the result.

## Components

1. **Model Import**
   - VRM models are loaded at runtime using a VRM parsing library.
   - FBX/PMX require pre-conversion or future importer support.
2. **Pose Estimation**
   - Utilizes MediaPipe Pose models executed with an ONNX runtime.
   - Generates joint trajectories from input videos.
3. **Motion Generation**
   - Converts joint data into animation frames applied to the model.
4. **Camera Control**
   - Reads device sensors (gyroscope/AR tracking) to move the viewer camera.
5. **Recording**
   - Encodes rendered frames into a video file using platform APIs.
6. **UI System**
   - UI layout is defined via JSON and instantiated by scripts at runtime.

## Folder Structure

```text
PureViewer/
  Viewer/        - Source code for the OpenTK viewer
  Assets/        - Sample models
docs/            - Project documentation
```

### Why document these folders?

Documenting the directory layout clarifies where new files belong and helps
contributors and LLM tools generate code in the correct location. Each folder
has a specific role:

- **PureViewer** – stand‑alone OpenTK application and sample assets.
- **docs** – design documents and guides for contributors.

Keeping this structure documented makes it easier to understand the project and
maintain consistency as the codebase grows.
