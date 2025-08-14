/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "emapp/private/CommonInclude.h"
#include "core/pmx/nanoem_p.h"

#include <math.h>
#include <stdlib.h>
#include <string.h>

typedef struct nanoem_morph_weight_t {
    nanoem_f32_t weight;
} nanoem_morph_weight_t;

typedef struct nanoem_vertex_state_t {
    nanoem_f32_t origin[4];
    nanoem_f32_t uv[4];
    nanoem_f32_t auv[4][4];
} nanoem_vertex_state_t;

typedef struct nanoem_bone_state_t {
    nanoem_f32_t translation[4];
    nanoem_f32_t orientation[4];
} nanoem_bone_state_t;

typedef struct nanoem_material_state_t {
    nanoem_f32_t diffuse[4];
    nanoem_f32_t specular[4];
    nanoem_f32_t ambient[4];
    nanoem_f32_t edge[4];
    nanoem_f32_t tex[4];
    nanoem_f32_t sph[4];
    nanoem_f32_t toon[4];
    nanoem_f32_t diffuse_opacity;
    nanoem_f32_t specular_power;
    nanoem_f32_t edge_opacity;
    nanoem_f32_t edge_size;
} nanoem_material_state_t;

static void
destroy_user_data(void *opaque, nanoem_model_object_t *)
{
    free(opaque);
}

static nanoem_morph_weight_t *
get_morph_weight(const nanoem_model_morph_t *morph)
{
    const nanoem_user_data_t *ud = nanoemModelObjectGetUserData(nanoemModelMorphGetModelObject(morph));
    return ud ? static_cast<nanoem_morph_weight_t *>(nanoemUserDataGetOpaqueData(ud)) : NULL;
}

static nanoem_vertex_state_t *
ensure_vertex_state(nanoem_model_vertex_t *vertex)
{
    nanoem_model_object_t *object = nanoemModelVertexGetModelObjectMutable(vertex);
    nanoem_user_data_t *ud = nanoemModelObjectGetUserData(object);
    nanoem_vertex_state_t *state = ud ? static_cast<nanoem_vertex_state_t *>(nanoemUserDataGetOpaqueData(ud)) : NULL;
    if (!state) {
        nanoem_status_t status = NANOEM_STATUS_SUCCESS;
        ud = nanoemUserDataCreate(&status);
        state = static_cast<nanoem_vertex_state_t *>(calloc(1, sizeof(*state)));
        if (ud && state) {
            memcpy(state->origin, vertex->origin, sizeof(state->origin));
            memcpy(state->uv, vertex->uv, sizeof(state->uv));
            for (int i = 0; i < 4; i++) {
                memcpy(state->auv[i], vertex->additional_uv[i], sizeof(state->auv[i]));
            }
            nanoemUserDataSetOpaqueData(ud, state);
            nanoemUserDataSetOnDestroyModelObjectCallback(ud, destroy_user_data);
            nanoemModelObjectSetUserData(object, ud);
        }
    }
    return state;
}

static nanoem_bone_state_t *
ensure_bone_state(nanoem_model_bone_t *bone)
{
    nanoem_model_object_t *object = nanoemModelBoneGetModelObjectMutable(bone);
    nanoem_user_data_t *ud = nanoemModelObjectGetUserData(object);
    nanoem_bone_state_t *state = ud ? static_cast<nanoem_bone_state_t *>(nanoemUserDataGetOpaqueData(ud)) : NULL;
    if (!state) {
        nanoem_status_t status = NANOEM_STATUS_SUCCESS;
        ud = nanoemUserDataCreate(&status);
        state = static_cast<nanoem_bone_state_t *>(calloc(1, sizeof(*state)));
        if (ud && state) {
            memcpy(state->translation, bone->origin, sizeof(state->translation));
            memcpy(state->orientation, bone->orientation, sizeof(state->orientation));
            nanoemUserDataSetOpaqueData(ud, state);
            nanoemUserDataSetOnDestroyModelObjectCallback(ud, destroy_user_data);
            nanoemModelObjectSetUserData(object, ud);
        }
    }
    return state;
}

static nanoem_material_state_t *
ensure_material_state(nanoem_model_material_t *material)
{
    nanoem_model_object_t *object = nanoemModelMaterialGetModelObjectMutable(material);
    nanoem_user_data_t *ud = nanoemModelObjectGetUserData(object);
    nanoem_material_state_t *state = ud ? static_cast<nanoem_material_state_t *>(nanoemUserDataGetOpaqueData(ud)) : NULL;
    if (!state) {
        nanoem_status_t status = NANOEM_STATUS_SUCCESS;
        ud = nanoemUserDataCreate(&status);
        state = static_cast<nanoem_material_state_t *>(calloc(1, sizeof(*state)));
        if (ud && state) {
            memcpy(state->diffuse, material->diffuse_color, sizeof(state->diffuse));
            memcpy(state->specular, material->specular_color, sizeof(state->specular));
            memcpy(state->ambient, material->ambient_color, sizeof(state->ambient));
            memcpy(state->edge, material->edge_color, sizeof(state->edge));
            state->diffuse_opacity = material->diffuse_opacity;
            state->specular_power = material->specular_power;
            state->edge_opacity = material->edge_opacity;
            state->edge_size = material->edge_size;
            memcpy(state->tex, material->diffuse_texture_blend, sizeof(state->tex));
            memcpy(state->sph, material->sphere_map_texture_blend, sizeof(state->sph));
            memcpy(state->toon, material->toon_texture_blend, sizeof(state->toon));
            nanoemUserDataSetOpaqueData(ud, state);
            nanoemUserDataSetOnDestroyModelObjectCallback(ud, destroy_user_data);
            nanoemModelObjectSetUserData(object, ud);
        }
    }
    return state;
}

static void
normalize_quaternion(nanoem_f32_t *q)
{
    nanoem_f32_t len = sqrtf(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]);
    if (len > 0) {
        q[0] /= len;
        q[1] /= len;
        q[2] /= len;
        q[3] /= len;
    }
}

NANOEM_DECL_API void APIENTRY
nanoemModelUpdateMorph(nanoem_model_t *model)
{
    nanoem_rsize_t num_vertices = 0, num_bones = 0, num_materials = 0, num_morphs = 0;
    nanoem_model_vertex_t *const *vertices = nanoemModelGetAllVertexObjects(model, &num_vertices);
    for (nanoem_rsize_t i = 0; i < num_vertices; i++) {
        nanoem_model_vertex_t *vertex = vertices[i];
        nanoem_vertex_state_t *state = ensure_vertex_state(vertex);
        if (state) {
            memcpy(vertex->origin, state->origin, sizeof(state->origin));
            memcpy(vertex->uv, state->uv, sizeof(state->uv));
            for (int j = 0; j < 4; j++) {
                memcpy(vertex->additional_uv[j], state->auv[j], sizeof(state->auv[j]));
            }
        }
    }
    nanoem_model_bone_t *const *bones = nanoemModelGetAllBoneObjects(model, &num_bones);
    for (nanoem_rsize_t i = 0; i < num_bones; i++) {
        nanoem_model_bone_t *bone = bones[i];
        nanoem_bone_state_t *state = ensure_bone_state(bone);
        if (state) {
            memcpy(bone->origin, state->translation, sizeof(state->translation));
            memcpy(bone->orientation, state->orientation, sizeof(state->orientation));
        }
    }
    nanoem_model_material_t *const *materials = nanoemModelGetAllMaterialObjects(model, &num_materials);
    for (nanoem_rsize_t i = 0; i < num_materials; i++) {
        nanoem_model_material_t *m = materials[i];
        nanoem_material_state_t *state = ensure_material_state(m);
        if (state) {
            memcpy(m->diffuse_color, state->diffuse, sizeof(state->diffuse));
            memcpy(m->specular_color, state->specular, sizeof(state->specular));
            memcpy(m->ambient_color, state->ambient, sizeof(state->ambient));
            memcpy(m->edge_color, state->edge, sizeof(state->edge));
            m->diffuse_opacity = state->diffuse_opacity;
            m->specular_power = state->specular_power;
            m->edge_opacity = state->edge_opacity;
            m->edge_size = state->edge_size;
            memcpy(m->diffuse_texture_blend, state->tex, sizeof(state->tex));
            memcpy(m->sphere_map_texture_blend, state->sph, sizeof(state->sph));
            memcpy(m->toon_texture_blend, state->toon, sizeof(state->toon));
        }
    }
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &num_morphs);
    for (nanoem_rsize_t i = 0; i < num_morphs; i++) {
        const nanoem_model_morph_t *morph = morphs[i];
        const nanoem_morph_weight_t *mw = get_morph_weight(morph);
        nanoem_f32_t weight = mw ? mw->weight : 0;
        if (weight == 0) {
            continue;
        }
        switch (nanoemModelMorphGetType(morph)) {
        case NANOEM_MODEL_MORPH_TYPE_VERTEX: {
            nanoem_rsize_t count = 0;
            nanoem_model_morph_vertex_t *const *children = nanoemModelMorphGetAllVertexMorphObjects(morph, &count);
            for (nanoem_rsize_t j = 0; j < count; j++) {
                nanoem_model_morph_vertex_t *child = children[j];
                nanoem_model_vertex_t *vertex = nanoemModelMorphVertexGetVertexObject(child);
                if (vertex) {
                    const nanoem_f32_t *pos = nanoemModelMorphVertexGetPosition(child);
                    for (int k = 0; k < 3; k++) {
                        vertex->origin[k] += pos[k] * weight;
                    }
                }
            }
            break;
        }
        case NANOEM_MODEL_MORPH_TYPE_TEXTURE:
        case NANOEM_MODEL_MORPH_TYPE_UVA1:
        case NANOEM_MODEL_MORPH_TYPE_UVA2:
        case NANOEM_MODEL_MORPH_TYPE_UVA3:
        case NANOEM_MODEL_MORPH_TYPE_UVA4: {
            nanoem_rsize_t count = 0;
            int index = static_cast<int>(nanoemModelMorphGetType(morph)) - static_cast<int>(NANOEM_MODEL_MORPH_TYPE_TEXTURE);
            nanoem_model_morph_uv_t *const *children = nanoemModelMorphGetAllUVMorphObjects(morph, &count);
            for (nanoem_rsize_t j = 0; j < count; j++) {
                nanoem_model_morph_uv_t *child = children[j];
                nanoem_model_vertex_t *vertex = nanoemModelMorphUVGetVertexObject(child);
                if (vertex) {
                    const nanoem_f32_t *pos = nanoemModelMorphUVGetPosition(child);
                    nanoem_f32_t *dest = index == 0 ? vertex->uv : vertex->additional_uv[index - 1];
                    for (int k = 0; k < 4; k++) {
                        dest[k] += pos[k] * weight;
                    }
                }
            }
            break;
        }
        case NANOEM_MODEL_MORPH_TYPE_BONE: {
            nanoem_rsize_t count = 0;
            nanoem_model_morph_bone_t *const *children = nanoemModelMorphGetAllBoneMorphObjects(morph, &count);
            for (nanoem_rsize_t j = 0; j < count; j++) {
                nanoem_model_morph_bone_t *child = children[j];
                nanoem_model_bone_t *bone = nanoemModelMorphBoneGetBoneObject(child);
                if (bone) {
                    const nanoem_f32_t *t = nanoemModelMorphBoneGetTranslation(child);
                    const nanoem_f32_t *o = nanoemModelMorphBoneGetOrientation(child);
                    for (int k = 0; k < 3; k++) {
                        bone->origin[k] += t[k] * weight;
                        bone->orientation[k] += o[k] * weight;
                    }
                    bone->orientation[3] += o[3] * weight;
                    normalize_quaternion(bone->orientation);
                }
            }
            break;
        }
        case NANOEM_MODEL_MORPH_TYPE_MATERIAL: {
            nanoem_rsize_t count = 0;
            nanoem_model_morph_material_t *const *children = nanoemModelMorphGetAllMaterialMorphObjects(morph, &count);
            for (nanoem_rsize_t j = 0; j < count; j++) {
                nanoem_model_morph_material_t *child = children[j];
                nanoem_model_material_t *material = nanoemModelMorphMaterialGetMaterialObject(child);
                if (material) {
                    nanoem_model_morph_material_operation_type_t op = nanoemModelMorphMaterialGetOperationType(child);
                    const nanoem_f32_t *diffuse = nanoemModelMorphMaterialGetDiffuseColor(child);
                    const nanoem_f32_t *specular = nanoemModelMorphMaterialGetSpecularColor(child);
                    const nanoem_f32_t *ambient = nanoemModelMorphMaterialGetAmbientColor(child);
                    const nanoem_f32_t *edge = nanoemModelMorphMaterialGetEdgeColor(child);
                    const nanoem_f32_t *tex = nanoemModelMorphMaterialGetDiffuseTextureBlend(child);
                    const nanoem_f32_t *sph = nanoemModelMorphMaterialGetSphereMapTextureBlend(child);
                    const nanoem_f32_t *toon = nanoemModelMorphMaterialGetToonTextureBlend(child);
                    nanoem_f32_t opacity = nanoemModelMorphMaterialGetDiffuseOpacity(child);
                    nanoem_f32_t edge_opacity = nanoemModelMorphMaterialGetEdgeOpacity(child);
                    nanoem_f32_t specular_power = nanoemModelMorphMaterialGetSpecularPower(child);
                    nanoem_f32_t edge_size = nanoemModelMorphMaterialGetEdgeSize(child);
                    for (int k = 0; k < 4; k++) {
                        if (op == NANOEM_MODEL_MORPH_MATERIAL_OPERATION_TYPE_MULTIPLY) {
                            material->diffuse_color[k] *= 1.0f + diffuse[k] * weight;
                            material->specular_color[k] *= 1.0f + specular[k] * weight;
                            material->ambient_color[k] *= 1.0f + ambient[k] * weight;
                            material->edge_color[k] *= 1.0f + edge[k] * weight;
                        }
                        else {
                            material->diffuse_color[k] += diffuse[k] * weight;
                            material->specular_color[k] += specular[k] * weight;
                            material->ambient_color[k] += ambient[k] * weight;
                            material->edge_color[k] += edge[k] * weight;
                        }
                        material->diffuse_texture_blend[k] += tex[k] * weight;
                        material->sphere_map_texture_blend[k] += sph[k] * weight;
                        material->toon_texture_blend[k] += toon[k] * weight;
                    }
                    if (op == NANOEM_MODEL_MORPH_MATERIAL_OPERATION_TYPE_MULTIPLY) {
                        material->diffuse_opacity *= 1.0f + opacity * weight;
                        material->edge_opacity *= 1.0f + edge_opacity * weight;
                        material->specular_power *= 1.0f + specular_power * weight;
                        material->edge_size *= 1.0f + edge_size * weight;
                    }
                    else {
                        material->diffuse_opacity += opacity * weight;
                        material->edge_opacity += edge_opacity * weight;
                        material->specular_power += specular_power * weight;
                        material->edge_size += edge_size * weight;
                    }
                }
            }
            break;
        }
        default:
            break;
        }
    }
}
