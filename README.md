# MiniMikuDance

This repository contains an experimental design for a smartphone application
that allows users to create dance videos using MMD-compatible 3D models.
Pose estimation runs locally on the device and the phone's motion acts as a
virtual camera.

The full development document is available in
[docs/development.md](docs/development.md). A shorter architecture summary is in
[docs/architecture.md](docs/architecture.md).

For instructions on preparing FBX or PMX models for runtime use, see
[docs/model_conversion.md](docs/model_conversion.md).

An overview of the planned features based on the current task cards is
available in [docs/features.md](docs/features.md).

## PureViewer
A simple OpenGL-based viewer implemented in pure C#. See [PureViewer/README.md](PureViewer/README.md) for details.
