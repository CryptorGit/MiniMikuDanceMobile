# PureViewer

This directory contains a minimal OpenGL viewer implemented in pure C# without Unity.
It loads a 3D model via AssimpNet and displays it using OpenTK.

## Requirements
- .NET 8 SDK
- OpenGL capable device

## Running
```
dotnet run --project Viewer [path/to/model]
```
If no model path is provided, a sample cube OBJ is loaded from `Assets/Models/sample.obj`.
