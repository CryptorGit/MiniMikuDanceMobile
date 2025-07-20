# IK & Constraint Guide for VRM 0.x (Alicia Solid)

> **Target use‑case:** custom engine for MikuMikuDance‑style choreography  
> **Rig base:** VRM 0.x (Unity Humanoid) – full body FK/IK, toe support, optional twist bones

---

## 1  Canonical IK Chains

| Chain | Bones (Root → … → Effector) | Pole / Aux | Notes |
|-------|-----------------------------|-----------|-------|
| **Leg** | `hips` → `upperLeg` → `lowerLeg` → `foot` | `kneePole` in front of knee | 2‑bone CCD / FABRIK; keeps feet planted |
| **Toe** | `foot` → `toes` | — | Simple hinge / mini‑IK to let heel lift (tiptoe) |
| **Arm** | `upperArm` → `lowerArm` → `hand` | `elbowPole` behind elbow | Shoulder can be passive or add **shoulder parent** for high lifts |
| **Head** | `spine` → `neck` → `head` | — | HMD / target‑look or dance stylisation |
| **Eye LookAt** | `head` → `leftEye` / `rightEye` | — | Copies rotation to eye bones |
| **Finger FK / micro‑IK** | `hand` → 3‑bone finger chains | — | Usually FK; for auto‑grip use mini‑IK |

---

## 2  Solver Pipeline

1. **Read skeleton** from VRM – build local/world matrices.  
2. **Leg IK** (Two‑Bone)  
   ```text
   kneePole = hip + 0.5 × (foot−hip) + up × shinLen × 0.1
   ```  
   Solve with CCD/FABRIK (≤10 iterations).  
3. **Toe hinge** – after leg IK, rotate the `toes` bone so the foot front remains on ground if ankle overshoots.  
4. **Arm IK** – Two‑Bone with elbow pole. Optionally distribute some rotation to `shoulder`.  
5. **Twist constraints** *(optional)* – add helper bones `upperArmTwist`, `lowerArmTwist`, `thighTwist` copying 50 % of parent roll to reduce mesh candy‑wrap.  
6. **Apply rotation limits** – clamp each bone’s Euler angles against *VRM_HumanLimits.json*.  
7. **SpringBone / cloth** – evaluate **after** IK so skirt/hair react to final pose.

---

## 3  Rotation Limits Reference

| Region | DOF & Range (deg) |
|--------|------------------|
| **Head / Neck** | Pitch ±50 / Yaw ±80 / Roll ±50 |
| **Spine‑Chest** | Bend ±35 / Twist ±45 |
| **Shoulder** | Elevate –20→+60 / Twist ±40 |
| **UpperArm** | Pitch –45→+100 / Swing ±90 |
| **LowerArm** | Flex 0→140 / Twist ±90 |
| **UpperLeg** | Pitch –30→+120 / AbAd ±45 |
| **LowerLeg** | Flex 0→140 |
| **Foot** | Pitch ±45 / Yaw ±45 |
| **Toes** | Pitch 0→45 (tiptoe) |
| **Fingers** | Curl 0→90 |

Full per‑bone limits are supplied in *VRM_HumanLimits.json*.

---

## 4  Implementation Hints

* Use **FABRIK** for smooth, one‑pass convergence on 2‑bone chains.  
* Clamp **after each IK iteration** to avoid solver fighting limits.  
* Cache pole vector directions on bind‑pose; update dynamically if hip/shoulder rotates >90°.  
* Toe hinge can be a simple single‑axis constraint: rotate about local X until foot bottom plane ≤ ground plane.  
* When adding twist bones, drive them by `Quaternion.Lerp(identity, parentRoll, 0.5)` so deformation distributes evenly.

---

*Generated 2025‑07‑20 • conforms to Unity Humanoid defaults*  
