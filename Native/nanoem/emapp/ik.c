/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "core/ik/IKSolver.h"
#include "core/ik/ConstraintSolver.h"

#include <stdlib.h>
#include <string.h>

typedef struct nanoem_emapp_constraint_t {
    int32_t target_bone_index;
    int32_t num_iterations;
    float angle_limit;
    int32_t num_links;
    nanoem_model_ik_constraint_link_t *links;
    float effector_position[3];
} nanoem_emapp_constraint_t;

static nanoem_emapp_constraint_t *g_constraints = NULL;
static int32_t g_num_constraints = 0;

void
nanoem_emapp_initialize_ik(const nanoem_model_ik_constraint_info_t *constraints, int32_t num_constraints)
{
    if (g_constraints) {
        for (int32_t i = 0; i < g_num_constraints; i++) {
            free(g_constraints[i].links);
        }
        free(g_constraints);
        g_constraints = NULL;
        g_num_constraints = 0;
    }
    if (constraints && num_constraints > 0) {
        g_num_constraints = num_constraints;
        g_constraints = (nanoem_emapp_constraint_t *) calloc((size_t) num_constraints, sizeof(*g_constraints));
        for (int32_t i = 0; i < num_constraints; i++) {
            const nanoem_model_ik_constraint_info_t *src = &constraints[i];
            nanoem_emapp_constraint_t *dst = &g_constraints[i];
            dst->target_bone_index = src->target_bone_index;
            dst->num_iterations = src->num_iterations;
            dst->angle_limit = src->angle_limit;
            dst->num_links = src->num_links;
            if (src->num_links > 0 && src->links) {
                dst->links = (nanoem_model_ik_constraint_link_t *) malloc(sizeof(nanoem_model_ik_constraint_link_t) * src->num_links);
                if (dst->links) {
                    memcpy(dst->links, src->links, sizeof(nanoem_model_ik_constraint_link_t) * src->num_links);
                } else {
                    dst->num_links = 0;
                }
            }
            dst->effector_position[0] = dst->effector_position[1] = dst->effector_position[2] = 0.0f;
        }
    }
}

void
nanoem_emapp_solve_ik(int32_t constraint_index, float position[3])
{
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

