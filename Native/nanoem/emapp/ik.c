/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "core/ik/IKSolver.h"
#include "core/ik/ConstraintSolver.h"

#include <stdlib.h>

typedef struct nanoem_emapp_constraint_t {
    float effector_position[3];
} nanoem_emapp_constraint_t;

static nanoem_emapp_constraint_t *g_constraints = NULL;
static int32_t g_num_constraints = 0;

void
nanoem_emapp_initialize_ik(int32_t num_constraints)
{
    if (g_constraints) {
        free(g_constraints);
        g_constraints = NULL;
        g_num_constraints = 0;
    }
    if (num_constraints > 0) {
        g_num_constraints = num_constraints;
        g_constraints = (nanoem_emapp_constraint_t *) calloc((size_t) num_constraints, sizeof(*g_constraints));
    }
}

void
nanoem_emapp_solve_ik(int32_t constraint_index, int32_t bone_index, float position[3])
{
    (void) bone_index;
    if (!position || constraint_index < 0 || constraint_index >= g_num_constraints) {
        return;
    }
    nanoem_constraint_joint_t joint;
    const float transform[16] = {
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f
    };
    nanoem_emapp_constraint_solve_axis_angle(transform, g_constraints[constraint_index].effector_position, position, &joint);
    g_constraints[constraint_index].effector_position[0] = position[0];
    g_constraints[constraint_index].effector_position[1] = position[1];
    g_constraints[constraint_index].effector_position[2] = position[2];
}

