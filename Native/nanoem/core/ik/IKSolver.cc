/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "IKSolver.h"
#include "ConstraintSolver.h"

static float g_effector_position[3];

void
nanoem_emapp_initialize_ik(void)
{
    g_effector_position[0] = 0.0f;
    g_effector_position[1] = 0.0f;
    g_effector_position[2] = 0.0f;
}

void
nanoem_emapp_solve_ik(int32_t bone_index, float position[3])
{
    (void) bone_index;
    nanoem_constraint_joint_t joint;
    const float transform[16] = {
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f
    };
    nanoem_emapp_constraint_solve_axis_angle(transform, g_effector_position, position, &joint);
    g_effector_position[0] = position[0];
    g_effector_position[1] = position[1];
    g_effector_position[2] = position[2];
}

