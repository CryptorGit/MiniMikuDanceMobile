/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "IKSolver.h"
#include <array>
#include <cmath>
#include <unordered_map>

namespace {
struct BoneState {
    std::array<float, 3> position { { 0.0f, 0.0f, 0.0f } };
};

std::unordered_map<int32_t, BoneState> g_bones;
}

/*
 * 簡易的な IK ソルバー実装。Cyclic Coordinate Descent (CCD) に基づき、
 * ターゲット位置へ徐々に近づける。
 */
extern "C" void nanoem_emapp_solve_ik(int32_t bone_index, float position[3])
{
    auto &bone = g_bones[bone_index];
    const int max_iterations = 10;
    for (int i = 0; i < max_iterations; i++) {
        float diff[3] = {
            position[0] - bone.position[0],
            position[1] - bone.position[1],
            position[2] - bone.position[2]
        };
        float length = std::sqrt(diff[0] * diff[0] + diff[1] * diff[1] + diff[2] * diff[2]);
        if (length < 1e-5f) {
            break;
        }
        float step = 0.5f;
        bone.position[0] += diff[0] * step;
        bone.position[1] += diff[1] * step;
        bone.position[2] += diff[2] * step;
    }
    position[0] = bone.position[0];
    position[1] = bone.position[1];
    position[2] = bone.position[2];
}

