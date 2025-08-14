/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license.
   see LICENSE.md for more details.
*/
#include "nanoem/nanoem.h"
#include <stdlib.h>
#include <string.h>

extern "C" {

NANOEM_DECL_API nanoem_model_t *APIENTRY
nanoemModelImportPMX(const nanoem_u8_t *bytes, size_t length,
                     nanoem_unicode_string_factory_t *factory,
                     nanoem_status_t *status)
{
    nanoem_buffer_t *buffer = nanoemBufferCreate(bytes, length, status);
    nanoem_model_t *model = NULL;
    if (buffer && !nanoem_status_ptr_has_error(status)) {
        model = nanoemModelCreate(factory, status);
        if (model && !nanoem_status_ptr_has_error(status)) {
            nanoemModelParsePMX(model, buffer, status);
        }
    }
    nanoemBufferDestroy(buffer);
    return model;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetVertexCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllVertexObjects(model, &count);
    return count;
}

typedef struct nanoem_model_info_t {
    char *name;
    char *englishName;
} nanoem_model_info_t;

typedef struct nanoem_model_bone_info_t {
    char *name;
    char *englishName;
    int parentBoneIndex;
    float origin[3];
} nanoem_model_bone_info_t;

typedef struct nanoem_model_morph_info_t {
    char *name;
    char *englishName;
    int type;
} nanoem_model_morph_info_t;

static char *
unicodeStringToUtf8(const nanoem_unicode_string_t *s)
{
    char *result = NULL;
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    nanoem_unicode_string_factory_t *factory = nanoemUnicodeStringFactoryCreate(&status);
    if (factory && !nanoem_status_has_error(status)) {
        nanoem_rsize_t length = 0;
        nanoem_u8_t *bytes = nanoemUnicodeStringFactoryGetByteArray(factory, s, &length, &status);
        if (bytes && !nanoem_status_has_error(status)) {
            result = (char *) malloc(length + 1);
            if (result) {
                memcpy(result, bytes, length);
                result[length] = 0;
            }
            nanoemUnicodeStringFactoryDestroyByteArray(factory, bytes);
        }
        nanoemUnicodeStringFactoryDestroy(factory);
    }
    return result;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetInfo(const nanoem_model_t *model, nanoem_model_info_t *info)
{
    if (info) {
        info->name = unicodeStringToUtf8(nanoemModelGetName(model, NANOEM_LANGUAGE_TYPE_JAPANESE));
        info->englishName = unicodeStringToUtf8(nanoemModelGetName(model, NANOEM_LANGUAGE_TYPE_ENGLISH));
    }
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetBoneCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllBoneObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetBoneInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_bone_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numBones = 0;
    const nanoem_model_bone_t *const *bones = nanoemModelGetAllBoneObjects(model, &numBones);
    if (index >= numBones) {
        info->name = NULL;
        info->englishName = NULL;
        info->parentBoneIndex = -1;
        info->origin[0] = info->origin[1] = info->origin[2] = 0.0f;
        return;
    }
    const nanoem_model_bone_t *bone = bones[index];
    info->name = unicodeStringToUtf8(nanoemModelBoneGetName(bone, NANOEM_LANGUAGE_TYPE_JAPANESE));
    info->englishName = unicodeStringToUtf8(nanoemModelBoneGetName(bone, NANOEM_LANGUAGE_TYPE_ENGLISH));
    const nanoem_model_bone_t *parent = nanoemModelBoneGetParentBoneObject(bone);
    info->parentBoneIndex = -1;
    if (parent) {
        for (nanoem_rsize_t i = 0; i < numBones; i++) {
            if (bones[i] == parent) {
                info->parentBoneIndex = (int) i;
                break;
            }
        }
    }
    const nanoem_f32_t *origin = nanoemModelBoneGetOrigin(bone);
    info->origin[0] = origin[0];
    info->origin[1] = origin[1];
    info->origin[2] = origin[2];
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetMorphCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllMorphObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetMorphInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_morph_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index >= numMorphs) {
        info->name = NULL;
        info->englishName = NULL;
        info->type = 0;
        return;
    }
    const nanoem_model_morph_t *morph = morphs[index];
    info->name = unicodeStringToUtf8(nanoemModelMorphGetName(morph, NANOEM_LANGUAGE_TYPE_JAPANESE));
    info->englishName = unicodeStringToUtf8(nanoemModelMorphGetName(morph, NANOEM_LANGUAGE_TYPE_ENGLISH));
    info->type = (int) nanoemModelMorphGetType(morph);
}

NANOEM_DECL_API void APIENTRY
nanoemModelIOFree(void *ptr)
{
    free(ptr);
}

NANOEM_DECL_API void APIENTRY
nanoemModelDestroy(nanoem_model_t *model)
{
    nanoemModelDestroy(model);
}

NANOEM_DECL_API nanoem_motion_t *APIENTRY
nanoemMotionImportVMD(const nanoem_u8_t *bytes, size_t length,
                      nanoem_unicode_string_factory_t *factory,
                      nanoem_status_t *status)
{
    nanoem_buffer_t *buffer = nanoemBufferCreate(bytes, length, status);
    nanoem_motion_t *motion = NULL;
    if (buffer && !nanoem_status_ptr_has_error(status)) {
        motion = nanoemMotionCreate(factory, status);
        if (motion && !nanoem_status_ptr_has_error(status)) {
            nanoemMotionParseVMD(motion, buffer, 0, status);
        }
    }
    nanoemBufferDestroy(buffer);
    return motion;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemMotionGetBoneKeyframeCount(const nanoem_motion_t *motion)
{
    nanoem_rsize_t count = 0;
    nanoemMotionGetAllBoneKeyframeObjects(motion, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemMotionDestroy(nanoem_motion_t *motion)
{
    nanoemMotionDestroy(motion);
}

} /* extern "C" */
