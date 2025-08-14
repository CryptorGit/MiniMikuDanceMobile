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
    nanoemModelGetAllMorphObjects(model, &numMorphs);
    (void) model;
    (void) index;
    (void) numMorphs;
    /* TODO: expose actual morph weight from nanoem */
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

#ifdef __cplusplus
}
#endif

