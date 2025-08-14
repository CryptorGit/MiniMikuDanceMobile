/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#pragma once
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct nanoem_constraint_joint_t {
    float axis[3];
    float angle;
} nanoem_constraint_joint_t;

int nanoem_emapp_constraint_solve_axis_angle(
    const float transform[16],
    const float effector_position[3],
    const float target_position[3],
    nanoem_constraint_joint_t *result);

int nanoem_emapp_constraint_solve_axis_angle_chain(
    const float *transforms,
    int32_t num_joints,
    const float effector_position[3],
    const float target_position[3],
    nanoem_constraint_joint_t *results);

#ifdef __cplusplus
}
#endif
