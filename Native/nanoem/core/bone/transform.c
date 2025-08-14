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

