/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#include "ConstraintSolver.h"

#include <math.h>

int
nanoem_emapp_constraint_solve_axis_angle(
    const float transform[16],
    const float effector_position[3],
    const float target_position[3],
    nanoem_constraint_joint_t *result)
{
    (void) transform; /* currently unused */
    float ex = effector_position[0];
    float ey = effector_position[1];
    float ez = effector_position[2];
    float tx = target_position[0];
    float ty = target_position[1];
    float tz = target_position[2];
    float edx = ex;
    float edy = ey;
    float edz = ez;
    float tdx = tx;
    float tdy = ty;
    float tdz = tz;
    float ed_norm = sqrtf(edx * edx + edy * edy + edz * edz);
    float td_norm = sqrtf(tdx * tdx + tdy * tdy + tdz * tdz);
    if (ed_norm < 1e-6f || td_norm < 1e-6f) {
        return 1;
    }
    edx /= ed_norm;
    edy /= ed_norm;
    edz /= ed_norm;
    tdx /= td_norm;
    tdy /= td_norm;
    tdz /= td_norm;
    result->axis[0] = edy * tdz - edz * tdy;
    result->axis[1] = edz * tdx - edx * tdz;
    result->axis[2] = edx * tdy - edy * tdx;
    float axis_norm = sqrtf(result->axis[0] * result->axis[0] + result->axis[1] * result->axis[1] + result->axis[2] * result->axis[2]);
    if (axis_norm < 1e-6f) {
        return 1;
    }
    result->axis[0] /= axis_norm;
    result->axis[1] /= axis_norm;
    result->axis[2] /= axis_norm;
    float dot = edx * tdx + edy * tdy + edz * tdz;
    if (dot > 1.0f) {
        dot = 1.0f;
    }
    else if (dot < -1.0f) {
        dot = -1.0f;
    }
    result->angle = acosf(dot);
    return 0;
}

