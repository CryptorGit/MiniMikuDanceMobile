/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License.
   see LICENSE.md for more details.
*/
#include "nanoem/nanoem.h"

NANOEM_DECL_API const nanoem_model_bone_t *APIENTRY nanoemModelBoneGetParentBoneObject(const nanoem_model_bone_t *bone);
NANOEM_DECL_API nanoem_rsize_t APIENTRY nanoemModelGetBoneCount(const nanoem_model_t *model);

static void
multiply_matrix(const nanoem_f32_t *a, const nanoem_f32_t *b, nanoem_f32_t *out)
{
    for (int i = 0; i < 4; i++) {
        for (int j = 0; j < 4; j++) {
            nanoem_f32_t sum = 0.0f;
            for (int k = 0; k < 4; k++) {
                sum += a[i * 4 + k] * b[k * 4 + j];
            }
            out[i * 4 + j] = sum;
        }
    }
}

NANOEM_DECL_API void APIENTRY
nanoemModelBoneUpdate(const nanoem_model_bone_t *bone)
{
    if (!bone) {
        return;
    }
    nanoem_f32_t local[16];
    nanoemModelBoneGetTransformMatrix(bone, local);
    const nanoem_model_bone_t *parent = nanoemModelBoneGetParentBoneObject(bone);
    if (parent) {
        nanoem_f32_t parent_matrix[16], result[16];
        nanoemModelBoneGetTransformMatrix(parent, parent_matrix);
        multiply_matrix(parent_matrix, local, result);
        nanoemModelBoneSetTransformMatrix((nanoem_model_bone_t *) bone, result);
    }
    else {
        nanoemModelBoneSetTransformMatrix((nanoem_model_bone_t *) bone, local);
    }
}

NANOEM_DECL_API void APIENTRY
nanoemModelBoneUpdateAll(const nanoem_model_t *model)
{
    if (!model) {
        return;
    }
    nanoem_rsize_t count = nanoemModelGetBoneCount(model);
    for (nanoem_rsize_t i = 0; i < count; i++) {
        const nanoem_model_bone_t *bone = nanoemModelGetBoneObject(model, (int) i);
        nanoemModelBoneUpdate(bone);
    }
}
