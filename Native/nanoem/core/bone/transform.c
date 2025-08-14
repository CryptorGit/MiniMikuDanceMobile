/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "../nanoem_p.h"

const nanoem_model_bone_t *APIENTRY
nanoemModelGetBoneObject(const nanoem_model_t *model, int index)
{
    return nanoem_is_not_null(model) && index >= 0 && (nanoem_rsize_t) index < model->num_bones ?
        model->bones[index] : NULL;
}

void APIENTRY
nanoemModelBoneGetTransformMatrix(const nanoem_model_bone_t *bone, nanoem_f32_t *value)
{
    if (nanoem_is_not_null(bone) && nanoem_is_not_null(value)) {
        /* initialize as identity */
        value[0] = 1.0f; value[1] = value[2] = value[3] = 0.0f;
        value[4] = 0.0f; value[5] = 1.0f; value[6] = value[7] = 0.0f;
        value[8] = value[9] = 0.0f; value[10] = 1.0f; value[11] = 0.0f;
        value[15] = 1.0f; value[12] = value[13] = value[14] = 0.0f;
        const nanoem_f32_t *origin = bone->origin.values;
        value[12] = origin[0];
        value[13] = origin[1];
        value[14] = origin[2];
    }
}

void APIENTRY
nanoemModelBoneSetTransformMatrix(nanoem_model_bone_t *bone, const nanoem_f32_t *value)
{
    if (nanoem_is_not_null(bone) && nanoem_is_not_null(value)) {
        bone->origin.values[0] = value[12];
        bone->origin.values[1] = value[13];
        bone->origin.values[2] = value[14];
    }
}

void APIENTRY
nanoemModelBoneGetTransform(const nanoem_model_bone_t *bone, nanoem_f32_t *value)
{
    if (nanoem_is_not_null(bone) && nanoem_is_not_null(value)) {
        const nanoem_f32_t *q = bone->orientation.values;
        nanoem_f32_t x = q[0], y = q[1], z = q[2], w = q[3];
        nanoem_f32_t xx = x * x, yy = y * y, zz = z * z;
        nanoem_f32_t xy = x * y, xz = x * z, yz = y * z;
        nanoem_f32_t wx = w * x, wy = w * y, wz = w * z;
        value[0] = 1.0f - 2.0f * (yy + zz);
        value[1] = 2.0f * (xy + wz);
        value[2] = 2.0f * (xz - wy);
        value[3] = 0.0f;
        value[4] = 2.0f * (xy - wz);
        value[5] = 1.0f - 2.0f * (xx + zz);
        value[6] = 2.0f * (yz + wx);
        value[7] = 0.0f;
        value[8] = 2.0f * (xz + wy);
        value[9] = 2.0f * (yz - wx);
        value[10] = 1.0f - 2.0f * (xx + yy);
        value[11] = 0.0f;
        const nanoem_f32_t *origin = bone->origin.values;
        value[12] = origin[0];
        value[13] = origin[1];
        value[14] = origin[2];
        value[15] = 1.0f;
    }
}

