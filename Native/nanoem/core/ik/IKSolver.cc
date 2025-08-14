/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "IKSolver.h"
#include "ConstraintSolver.h"

void
nanoem_emapp_initialize_ik(void)
{
    /* TODO: solver initialization */
}

void
nanoem_emapp_solve_ik(int32_t bone_index, const float position[3])
{
    (void) bone_index;
    nanoem_constraint_joint_t joint;
    const float transform[16] = {
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f
    };
    const float origin[3] = { 0.0f, 0.0f, 0.0f };
    nanoem_emapp_constraint_solve_axis_angle(transform, origin, position, &joint);
    (void) joint;
}

