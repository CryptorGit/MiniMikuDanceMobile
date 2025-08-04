# PMX Bone List & Morph List — Complete Technical Guide

## 1. Bone List — “Everything You Need to Move the Model”

### 1.1 Entry Structure (PMX 2.x)

| Field Order | Type / Size | Description |
|-------------|-------------|-------------|
| **Name (JP / EN)** | Variable‑length strings ×2 | Encoding (UTF‑8 / Shift‑JIS) follows header flags |
| **Position** | `float[3]` | Parent‑local coordinates (MMD convention: +Y is up) |
| **Parent Bone Index** | *N* byte | N is declared in header (1 / 2 / 4) |
| **Transform Level** | `int` | Larger value → later evaluation (IK → child → …) |
| **Transform Ratio** | `float` | 0–1, interpolation for physics linkage |
| **Flags (bit‑field)** | `ushort` | See table below |
| **Connection** | Depends on flags | *Offset vector* or *Connected bone index* |
| **Inherit Parent** | *N* byte | Bone whose rotation/translation is inherited |
| **Inherit Ratio** | `float` | 0 = disabled, 1 = fully inherited |
| **Fixed Axis** | `float[3]` | Constrain to a single axis |
| **Local Axes** | `float[3]×2` | X‑axis & Z‑axis vectors |
| **External Parent (Key, 2.1+)** | `int` | MMD “external parent” setting |
| **IK Block** | Variable | Present when bit 5 (IK) flag is set |

#### 1.1.1 Flag Breakdown

| Bit | Meaning (1 = enabled) | Notes |
|-----|-----------------------|-------|
| 0 | Rotatable |
| 1 | Translatable |
| 2 | Visible in UI |
| 3 | Operate Disabled (keyframes cannot be set) |
| 4 | **IK** (IK block follows) |
| 5 | Separate inherit (rotate / translate) |
| 6 | Local axis |
| 7 | Rotate + Translate inherit |
| 8 | Fixed axis |
| 9 | Local deformation |
| 10 | External parent |
| 11–15 | Reserved |

#### 1.1.2 IK Block Details

| Field | Description |
|-------|-------------|
| Target Bone Index | End effector (e.g., ankle) |
| Loop Count | 1–256 (> loops = higher precision) |
| Angle Limit (rad) | Example: 0.01745 ≈ 1° |
| Chain Length | Number of bones in chain |
| Chain Elements | `Index + lower limit + upper limit (vec3)` × n |

> **Implementation Tip**  
> Chains often require per‑joint signed limits (left / right limbs may have opposite signs).  
> The original MMD engine uses a CCD‑like (Cyclic Coordinate Descent) solver. In Unity/Three.js, CCD or FABRIK reproduces motion faithfully.

### 1.2 Example Data Model (C#)

```csharp
public sealed record PmxBone(
    string Name, string NameEn,
    Vector3 Position,
    int ParentIndex,
    int TransformLevel,
    float TransformRatio,
    BoneFlags Flags,
    Vector3? Offset,
    int? ConnectedIndex,
    int? InheritIndex,
    float InheritRatio,
    Vector3? FixedAxis,
    (Vector3 xAxis, Vector3 zAxis)? LocalAxes,
    int? ExternalParent,
    PmxIk? Ik);

[Flags]
public enum BoneFlags : ushort
{
    Rotate      = 1 << 0,
    Translate   = 1 << 1,
    Visible     = 1 << 2,
    Inoperable  = 1 << 3,
    Ik          = 1 << 4,
    // … other bits …
}
```

*Handle variable index sizes with `BinaryPrimitives.ReadInt32LittleEndian(span[..size])`.*

### 1.3 Common Bone Layout & Naming

| Purpose | Typical Bones (JP) | Notes |
|---------|--------------------|-------|
| Root | センター / 全ての親 | 全ての親 moves entire model including physics |
| IK | 左つま先IK / 右足ＩＫ | Contain IK blocks |
| Inherit | 腰 / 上半身2 | Use **Inherit Ratio** for torso bending |
| Decoration | グルーブ / ヒール | Connection points for physics |

### 1.4 IK Bone Dragging in Pose Editor

When an IK bone is selected, the editor draws a circular handle ("ドラッグ円") at the bone's position.
The circle lies on a plane perpendicular to the active camera ("カメラ基準平面"), and dragging moves the IK target only within this plane while depth is locked.
This mirrors MMD's IK controls and keeps manipulation intuitive regardless of model orientation.

---

## 2. Morph List — “Blend Deformation & Expressions”

### 2.1 Entry Structure

| Field Order | Type | Description |
|-------------|------|-------------|
| **Name (JP / EN)** | Variable‑length ×2 |
| **Panel (byte)** | `0:眉 1:目 2:口 3:他 4:システム 5:隠` |
| **Morph Type (byte)** | See table below |
| **Offset Count** | `int` |
| **Offset Block** | Format varies per morph type |

#### 2.1.1 Offsets by Type

| Type | Purpose | Structure |
|------|---------|-----------|
| 0: Group | Hierarchical morph | `int index, float ratio` |
| 1: Vertex | Vertex displacement | `int vtxIdx, Vector3 delta` |
| 2: Bone | Bone pos/rot morph | `int boneIdx, Vector3 pos, Quaternion rot` |
| 3–6: UV | Base UV + AddUV1‑4 | `int vtxIdx, Vector4 delta` |
| 7: Material | Material color / texture factors | ~10 fields |
| 8: RigidBody (2.1+) | Physics param change | Bullet‑specific |
| *AddUV count* depends on header “extra UV num” |

> **Performance Tip**  
> Large vertex morphs (10 k+ vertices × dozens of morphs) can bottleneck GPU uploads. In Three.js/Unity, use Compute Buffers + GPU blending.  
> Extract material morphs and apply via **MaterialPropertyBlock** to avoid extra draw calls.

### 2.2 MMD Morph Blending Algorithm

1. **Slider value (0–1)** per morph  
2. Apply morphs *in panel order*  
3. If multiple morphs affect the same vertex, **sum their deltas**  
4. After vertices, apply **material / bone offsets**, then draw

*Panel 4 (System) is for MMD internals; keeping the same order ensures motion compatibility.*

### 2.3 Engine‑Specific Implementation Patterns

| Engine | Recommended Approach |
|--------|----------------------|
| **OpenTK / OpenGL** | Store `<index, delta>` in SSBO → add in Compute Shader |
| **Unity** | • **Vertex**: Bake into SkinnedMeshRenderer BlendShapes<br>• **Material**: MaterialPropertyBlock<br>• **Bone**: Additional Transforms |
| **Three.js** | `BufferGeometry.morphAttributes` + WebGL2 instancing |

### 2.4 Version Differences

| Version | Changes |
|---------|---------|
| 2.0 → 2.1 | RigidBody morphs, External Parent, QDEF skinning, external Toon textures |
| 2.1 → 2.2 *(unofficial)* | Soft‑body formalization, extended inherit morph |

---

## 3. Put It All Together

- **Bones**: Decode hierarchy, flags, and IK blocks to replicate **MMD physics & editing behavior** precisely.  
- **Morphs**: Respect *panel order* and *offset merging rules* to achieve expressive **face & dynamic material changes**.  
- In C#/OpenTK, **map raw structs → high‑level objects** once, then reuse in renderer and editor UI.

> Need further help? Full loader source, IK CCD implementation, or compute‑shader morph blending examples are available on request!
