/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license.
   see LICENSE.md for more details.
*/
#include "nanoem/nanoem.h"
#include <stdlib.h>
#include <string.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct nanoem_model_material_info_t {
    char *name;
    char *englishName;
    float diffuse[4];
    float specular[4];
    float ambient[4];
    int textureIndex;
} nanoem_model_material_info_t;

typedef struct nanoem_model_morph_vertex_offset_t {
    int vertexIndex;
    float offset[3];
} nanoem_model_morph_vertex_offset_t;

typedef struct nanoem_model_morph_uv_offset_t {
    int vertexIndex;
    float offset[4];
} nanoem_model_morph_uv_offset_t;

typedef struct nanoem_model_morph_group_offset_t {
    int morphIndex;
    float weight;
} nanoem_model_morph_group_offset_t;

typedef struct nanoem_model_morph_bone_offset_t {
    int boneIndex;
    float translation[3];
    float orientation[4];
} nanoem_model_morph_bone_offset_t;

typedef struct nanoem_model_morph_material_offset_t {
    int materialIndex;
    int isAll;
    int operation;
    float diffuse[4];
    float specular[3];
    float specularPower;
    float edgeColor[4];
    float edgeSize;
    float toonColor[3];
    float textureTint[4];
} nanoem_model_morph_material_offset_t;

typedef struct nanoem_morph_weight_t {
    float weight;
} nanoem_morph_weight_t;

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

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetTextureCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllTextureObjects(model, &count);
    return count;
}

NANOEM_DECL_API char *APIENTRY
nanoemModelGetTexturePathAt(const nanoem_model_t *model, nanoem_rsize_t index)
{
    nanoem_rsize_t numTextures = 0;
    const nanoem_model_texture_t *const *textures = nanoemModelGetAllTextureObjects(model, &numTextures);
    if (index < numTextures) {
        const nanoem_model_texture_t *texture = textures[index];
        return unicodeStringToUtf8(nanoemModelTextureGetPath(texture));
    }
    return NULL;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetMaterialCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllMaterialObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetMaterialInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_material_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numMaterials = 0;
    const nanoem_model_material_t *const *materials = nanoemModelGetAllMaterialObjects(model, &numMaterials);
    if (index >= numMaterials) {
        info->name = NULL;
        info->englishName = NULL;
        memset(info->diffuse, 0, sizeof(info->diffuse));
        memset(info->specular, 0, sizeof(info->specular));
        memset(info->ambient, 0, sizeof(info->ambient));
        info->textureIndex = -1;
        return;
    }
    const nanoem_model_material_t *material = materials[index];
    info->name = unicodeStringToUtf8(nanoemModelMaterialGetName(material, NANOEM_LANGUAGE_TYPE_JAPANESE));
    info->englishName = unicodeStringToUtf8(nanoemModelMaterialGetName(material, NANOEM_LANGUAGE_TYPE_ENGLISH));
    memcpy(info->diffuse, nanoemModelMaterialGetDiffuseColor(material), sizeof(info->diffuse));
    memcpy(info->specular, nanoemModelMaterialGetSpecularColor(material), sizeof(info->specular));
    memcpy(info->ambient, nanoemModelMaterialGetAmbientColor(material), sizeof(info->ambient));
    info->textureIndex = -1;
    {
        nanoem_rsize_t numTextures = 0;
        const nanoem_model_texture_t *const *textures = nanoemModelGetAllTextureObjects(model, &numTextures);
        const nanoem_model_texture_t *texture = nanoemModelMaterialGetDiffuseTextureObject(material);
        for (nanoem_rsize_t i = 0; i < numTextures; i++) {
            if (textures[i] == texture) {
                info->textureIndex = (int) i;
                break;
            }
        }
    }
}

NANOEM_DECL_API float APIENTRY
nanoemModelGetMorphInitialWeight(const nanoem_model_t *model, nanoem_rsize_t index)
{
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        const nanoem_user_data_t *ud = nanoemModelObjectGetUserData(nanoemModelMorphGetModelObject(morph));
        const nanoem_morph_weight_t *state =
            ud ? (const nanoem_morph_weight_t *) nanoemUserDataGetOpaqueData(ud) : NULL;
        if (state) {
            return state->weight;
        }
    }
    return 0.0f;
}

NANOEM_DECL_API nanoem_model_morph_vertex_offset_t *APIENTRY
nanoemModelMorphGetVertexOffsets(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_rsize_t *num_offsets)
{
    nanoem_model_morph_vertex_offset_t *offsets = NULL;
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        nanoem_rsize_t count = 0;
        nanoem_model_morph_vertex_t *const *items = nanoemModelMorphGetAllVertexMorphObjects(morph, &count);
        if (count > 0) {
            offsets = (nanoem_model_morph_vertex_offset_t *) malloc(sizeof(*offsets) * count);
            if (offsets) {
                for (nanoem_rsize_t i = 0; i < count; i++) {
                    const nanoem_model_morph_vertex_t *v = items[i];
                    const nanoem_model_vertex_t *vertex = nanoemModelMorphVertexGetVertexObject(v);
                    offsets[i].vertexIndex = vertex ? (int) nanoemModelObjectGetIndex(nanoemModelVertexGetModelObject(vertex)) : -1;
                    const nanoem_f32_t *pos = nanoemModelMorphVertexGetPosition(v);
                    if (pos) {
                        memcpy(offsets[i].offset, pos, sizeof(offsets[i].offset));
                    }
                    else {
                        memset(offsets[i].offset, 0, sizeof(offsets[i].offset));
                    }
                }
            }
        }
        if (num_offsets) {
            *num_offsets = count;
        }
    }
    else if (num_offsets) {
        *num_offsets = 0;
    }
    return offsets;
}

NANOEM_DECL_API nanoem_model_morph_uv_offset_t *APIENTRY
nanoemModelMorphGetUVOffsets(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_rsize_t *num_offsets)
{
    nanoem_model_morph_uv_offset_t *offsets = NULL;
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        nanoem_rsize_t count = 0;
        nanoem_model_morph_uv_t *const *items = nanoemModelMorphGetAllUVMorphObjects(morph, &count);
        if (count > 0) {
            offsets = (nanoem_model_morph_uv_offset_t *) malloc(sizeof(*offsets) * count);
            if (offsets) {
                for (nanoem_rsize_t i = 0; i < count; i++) {
                    const nanoem_model_morph_uv_t *v = items[i];
                    const nanoem_model_vertex_t *vertex = nanoemModelMorphUVGetVertexObject(v);
                    offsets[i].vertexIndex = vertex ? (int) nanoemModelObjectGetIndex(nanoemModelVertexGetModelObject(vertex)) : -1;
                    const nanoem_f32_t *pos = nanoemModelMorphUVGetPosition(v);
                    if (pos) {
                        memcpy(offsets[i].offset, pos, sizeof(offsets[i].offset));
                    }
                    else {
                        memset(offsets[i].offset, 0, sizeof(offsets[i].offset));
                    }
                }
            }
        }
        if (num_offsets) {
            *num_offsets = count;
        }
    }
    else if (num_offsets) {
        *num_offsets = 0;
    }
    return offsets;
}

NANOEM_DECL_API nanoem_model_morph_group_offset_t *APIENTRY
nanoemModelMorphGetGroupOffsets(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_rsize_t *num_offsets)
{
    nanoem_model_morph_group_offset_t *offsets = NULL;
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        nanoem_rsize_t count = 0;
        nanoem_model_morph_group_t *const *items = nanoemModelMorphGetAllGroupMorphObjects(morph, &count);
        if (count > 0) {
            offsets = (nanoem_model_morph_group_offset_t *) malloc(sizeof(*offsets) * count);
            if (offsets) {
                for (nanoem_rsize_t i = 0; i < count; i++) {
                    const nanoem_model_morph_group_t *g = items[i];
                    const nanoem_model_morph_t *target = nanoemModelMorphGroupGetMorphObject(g);
                    offsets[i].morphIndex = target ? (int) nanoemModelObjectGetIndex(nanoemModelMorphGetModelObject(target)) : -1;
                    offsets[i].weight = nanoemModelMorphGroupGetWeight(g);
                }
            }
        }
        if (num_offsets) {
            *num_offsets = count;
        }
    }
    else if (num_offsets) {
        *num_offsets = 0;
    }
    return offsets;
}

NANOEM_DECL_API nanoem_model_morph_bone_offset_t *APIENTRY
nanoemModelMorphGetBoneOffsets(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_rsize_t *num_offsets)
{
    nanoem_model_morph_bone_offset_t *offsets = NULL;
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        nanoem_rsize_t count = 0;
        nanoem_model_morph_bone_t *const *items = nanoemModelMorphGetAllBoneMorphObjects(morph, &count);
        if (count > 0) {
            offsets = (nanoem_model_morph_bone_offset_t *) malloc(sizeof(*offsets) * count);
            if (offsets) {
                for (nanoem_rsize_t i = 0; i < count; i++) {
                    const nanoem_model_morph_bone_t *b = items[i];
                    const nanoem_model_bone_t *bone = nanoemModelMorphBoneGetBoneObject(b);
                    offsets[i].boneIndex = bone ? (int) nanoemModelObjectGetIndex(nanoemModelBoneGetModelObject(bone)) : -1;
                    const nanoem_f32_t *t = nanoemModelMorphBoneGetTranslation(b);
                    const nanoem_f32_t *o = nanoemModelMorphBoneGetOrientation(b);
                    if (t) {
                        memcpy(offsets[i].translation, t, sizeof(offsets[i].translation));
                    }
                    else {
                        memset(offsets[i].translation, 0, sizeof(offsets[i].translation));
                    }
                    if (o) {
                        memcpy(offsets[i].orientation, o, sizeof(offsets[i].orientation));
                    }
                    else {
                        memset(offsets[i].orientation, 0, sizeof(offsets[i].orientation));
                        offsets[i].orientation[3] = 1.0f;
                    }
                }
            }
        }
        if (num_offsets) {
            *num_offsets = count;
        }
    }
    else if (num_offsets) {
        *num_offsets = 0;
    }
    return offsets;
}

NANOEM_DECL_API nanoem_model_morph_material_offset_t *APIENTRY
nanoemModelMorphGetMaterialOffsets(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_rsize_t *num_offsets)
{
    nanoem_model_morph_material_offset_t *offsets = NULL;
    nanoem_rsize_t numMorphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &numMorphs);
    if (index < numMorphs) {
        const nanoem_model_morph_t *morph = morphs[index];
        nanoem_rsize_t count = 0;
        nanoem_model_morph_material_t *const *items = nanoemModelMorphGetAllMaterialMorphObjects(morph, &count);
        if (count > 0) {
            offsets = (nanoem_model_morph_material_offset_t *) malloc(sizeof(*offsets) * count);
            if (offsets) {
                for (nanoem_rsize_t i = 0; i < count; i++) {
                    const nanoem_model_morph_material_t *m = items[i];
                    const nanoem_model_material_t *material = nanoemModelMorphMaterialGetMaterialObject(m);
                    offsets[i].isAll = material ? 0 : 1;
                    offsets[i].materialIndex = -1;
                    if (material) {
                        nanoem_rsize_t numMaterials = 0;
                        const nanoem_model_material_t *const *materials = nanoemModelGetAllMaterialObjects(model, &numMaterials);
                        for (nanoem_rsize_t j = 0; j < numMaterials; j++) {
                            if (materials[j] == material) {
                                offsets[i].materialIndex = (int) j;
                                break;
                            }
                        }
                    }
                    offsets[i].operation = (int) nanoemModelMorphMaterialGetOperationType(m);
                    const nanoem_f32_t *v = nanoemModelMorphMaterialGetDiffuseColor(m);
                    if (v) {
                        memcpy(offsets[i].diffuse, v, sizeof(offsets[i].diffuse));
                    } else {
                        memset(offsets[i].diffuse, 0, sizeof(offsets[i].diffuse));
                    }
                    v = nanoemModelMorphMaterialGetSpecularColor(m);
                    if (v) {
                        memcpy(offsets[i].specular, v, sizeof(offsets[i].specular));
                    } else {
                        memset(offsets[i].specular, 0, sizeof(offsets[i].specular));
                    }
                    offsets[i].specularPower = nanoemModelMorphMaterialGetSpecularPower(m);
                    v = nanoemModelMorphMaterialGetEdgeColor(m);
                    if (v) {
                        memcpy(offsets[i].edgeColor, v, sizeof(offsets[i].edgeColor));
                    } else {
                        memset(offsets[i].edgeColor, 0, sizeof(offsets[i].edgeColor));
                    }
                    offsets[i].edgeSize = nanoemModelMorphMaterialGetEdgeSize(m);
                    v = nanoemModelMorphMaterialGetToonTextureBlend(m);
                    if (v) {
                        memcpy(offsets[i].toonColor, v, sizeof(offsets[i].toonColor));
                    } else {
                        memset(offsets[i].toonColor, 0, sizeof(offsets[i].toonColor));
                    }
                    v = nanoemModelMorphMaterialGetDiffuseTextureBlend(m);
                    if (v) {
                        memcpy(offsets[i].textureTint, v, sizeof(offsets[i].textureTint));
                    } else {
                        memset(offsets[i].textureTint, 0, sizeof(offsets[i].textureTint));
                    }
                }
            }
        }
        if (num_offsets) {
            *num_offsets = count;
        }
    }
    else if (num_offsets) {
        *num_offsets = 0;
    }
    return offsets;
}

#ifdef __cplusplus
}
#endif

