/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#pragma once
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct nanoem_model_ik_constraint_link_t {
    int32_t bone_index;
    int32_t has_limit;
    float lower_limit[3];
    float upper_limit[3];
} nanoem_model_ik_constraint_link_t;

typedef struct nanoem_model_ik_constraint_info_t {
    int32_t target_bone_index;
    float angle_limit;
    int32_t num_iterations;
    int32_t num_links;
    const nanoem_model_ik_constraint_link_t *links;
} nanoem_model_ik_constraint_info_t;

void nanoem_emapp_initialize_ik(const nanoem_model_ik_constraint_info_t *constraints, int32_t num_constraints);
void nanoem_emapp_solve_ik(int32_t constraint_index, float position[3]);

#ifdef __cplusplus
}
#endif

