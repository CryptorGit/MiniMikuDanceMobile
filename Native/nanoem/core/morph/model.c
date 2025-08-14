/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/
#include "../nanoem_p.h"
#include "motion.h"

const nanoem_unicode_string_t *APIENTRY
nanoemModelMorphGetName(const nanoem_model_morph_t *morph, nanoem_language_type_t language)
{
    const nanoem_unicode_string_t *name = NULL;
    if (nanoem_is_not_null(morph)) {
        switch (language) {
        case NANOEM_LANGUAGE_TYPE_JAPANESE:
            name = morph->name_ja;
            break;
        case NANOEM_LANGUAGE_TYPE_ENGLISH:
            name = morph->name_en;
            break;
        case NANOEM_LANGUAGE_TYPE_MAX_ENUM:
        case NANOEM_LANGUAGE_TYPE_UNKNOWN:
        default:
            break;
        }
    }
    return name;
}

nanoem_model_morph_category_t APIENTRY
nanoemModelMorphGetCategory(const nanoem_model_morph_t *morph)
{
    return nanoem_is_not_null(morph) ? morph->category : NANOEM_MODEL_MORPH_CATEGORY_UNKNOWN;
}

nanoem_model_morph_type_t APIENTRY
nanoemModelMorphGetType(const nanoem_model_morph_t *morph)
{
    return nanoem_is_not_null(morph) ? morph->type : NANOEM_MODEL_MORPH_TYPE_UNKNOWN;
}

nanoem_model_morph_bone_t *const *APIENTRY
nanoemModelMorphGetAllBoneMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_bone_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_BONE) {
        *num_objects = morph->num_objects;
        items = morph->u.bones;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_flip_t *const *APIENTRY
nanoemModelMorphGetAllFlipMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_flip_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_FLIP) {
        *num_objects = morph->num_objects;
        items = morph->u.flips;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_group_t *const *APIENTRY
nanoemModelMorphGetAllGroupMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_group_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_GROUP) {
        *num_objects = morph->num_objects;
        items = morph->u.groups;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_impulse_t *const *APIENTRY
nanoemModelMorphGetAllImpulseMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_impulse_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_IMPULUSE) {
        *num_objects = morph->num_objects;
        items = morph->u.impulses;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_material_t *const *APIENTRY
nanoemModelMorphGetAllMaterialMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_material_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_MATERIAL) {
        *num_objects = morph->num_objects;
        items = morph->u.materials;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_uv_t *const *APIENTRY
nanoemModelMorphGetAllUVMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_uv_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) &&
        morph->type >= NANOEM_MODEL_MORPH_TYPE_TEXTURE && morph->type <= NANOEM_MODEL_MORPH_TYPE_UVA4) {
        *num_objects = morph->num_objects;
        items = morph->u.uvs;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

nanoem_model_morph_vertex_t *const *APIENTRY
nanoemModelMorphGetAllVertexMorphObjects(const nanoem_model_morph_t *morph, nanoem_rsize_t *num_objects)
{
    nanoem_model_morph_vertex_t *const *items = NULL;
    if (nanoem_is_not_null(morph) && nanoem_is_not_null(num_objects) && morph->type == NANOEM_MODEL_MORPH_TYPE_VERTEX) {
        *num_objects = morph->num_objects;
        items = morph->u.vertices;
    }
    else if (nanoem_is_not_null(num_objects)) {
        *num_objects = 0;
    }
    return items;
}

const nanoem_model_object_t *APIENTRY
nanoemModelMorphGetModelObject(const nanoem_model_morph_t *morph)
{
    return nanoem_is_not_null(morph) ? &morph->base : NULL;
}

nanoem_model_object_t *APIENTRY
nanoemModelMorphGetModelObjectMutable(nanoem_model_morph_t *morph)
{
    return nanoem_is_not_null(morph) ? &morph->base : NULL;
}
