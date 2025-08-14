#include "nanoem.h"
#include "nanoem_p.h"
#include <stdlib.h>
#include <string.h>

// global unicode factory
static nanoem_unicode_string_factory_t *g_factory = NULL;

static nanoem_unicode_string_factory_t *ensure_factory(nanoem_status_t *status) {
    if (!g_factory) {
        g_factory = nanoemUnicodeStringFactoryCreate(status);
    }
    return g_factory;
}

typedef struct nanoem_model_info_t {
    char *name;
    char *english_name;
} nanoem_model_info_t;

typedef struct nanoem_model_bone_info_t {
    char *name;
    char *english_name;
    int parent_bone_index;
    float origin_x;
    float origin_y;
    float origin_z;
} nanoem_model_bone_info_t;

typedef struct nanoem_model_morph_info_t {
    char *name;
    char *english_name;
    int category;
    int type;
} nanoem_model_morph_info_t;

typedef struct nanoem_model_material_info_t {
    char *name;
    char *english_name;
    float diffuse_r, diffuse_g, diffuse_b, diffuse_a;
    float specular_r, specular_g, specular_b, specular_a;
    float ambient_r, ambient_g, ambient_b, ambient_a;
    int texture_index;
} nanoem_model_material_info_t;

static char *copy_string(const nanoem_unicode_string_t *s, nanoem_status_t *status) {
    if (!s) {
        return NULL;
    }
    nanoem_unicode_string_factory_t *factory = ensure_factory(status);
    nanoem_rsize_t length = 0;
    nanoem_u8_t *bytes = nanoemUnicodeStringFactoryGetByteArray(factory, s, &length, status);
    if (!bytes) {
        return NULL;
    }
    char *dst = (char *) malloc(length + 1);
    if (dst) {
        memcpy(dst, bytes, length);
        dst[length] = '\0';
    }
    nanoemUnicodeStringFactoryDestroyByteArray(factory, bytes);
    return dst;
}

nanoem_model_t *APIENTRY nanoemModelImportPMX(const nanoem_u8_t *bytes, nanoem_rsize_t length, void *factory, nanoem_status_t *status) {
    nanoem_mark_unused(factory);
    nanoem_unicode_string_factory_t *f = ensure_factory(status);
    if (!f) {
        return NULL;
    }
    nanoem_buffer_t *buffer = nanoemBufferCreate(bytes, length, status);
    if (!buffer) {
        return NULL;
    }
    nanoem_model_t *model = nanoemModelCreate(f, status);
    if (!model) {
        nanoemBufferDestroy(buffer);
        return NULL;
    }
    if (!nanoemModelLoadFromBufferPMX(model, buffer, status)) {
        nanoemBufferDestroy(buffer);
        nanoemModelDestroy(model);
        return NULL;
    }
    nanoemBufferDestroy(buffer);
    return model;
}

nanoem_u32_t APIENTRY nanoemModelGetVertexCount(const nanoem_model_t *model) {
    nanoem_rsize_t num_vertices = 0;
    nanoemModelGetAllVertexObjects(model, &num_vertices);
    return (nanoem_u32_t) num_vertices;
}

void APIENTRY nanoemModelGetInfo(const nanoem_model_t *model, nanoem_model_info_t *info) {
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    info->name = copy_string(nanoemModelGetName(model, NANOEM_LANGUAGE_TYPE_JAPANESE), &status);
    info->english_name = copy_string(nanoemModelGetName(model, NANOEM_LANGUAGE_TYPE_ENGLISH), &status);
}

void APIENTRY nanoemModelGetBoneInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_bone_info_t *info) {
    nanoem_rsize_t num_bones = 0;
    const nanoem_model_bone_t *const *bones = nanoemModelGetAllBoneObjects(model, &num_bones);
    if (index >= num_bones) {
        memset(info, 0, sizeof(*info));
        return;
    }
    const nanoem_model_bone_t *bone = bones[index];
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    info->name = copy_string(nanoemModelBoneGetName(bone, NANOEM_LANGUAGE_TYPE_JAPANESE), &status);
    info->english_name = copy_string(nanoemModelBoneGetName(bone, NANOEM_LANGUAGE_TYPE_ENGLISH), &status);
    const nanoem_model_bone_t *parent = nanoemModelBoneGetParentBoneObject(bone);
    info->parent_bone_index = parent ? (int) nanoemModelObjectGetIndex(nanoemModelBoneGetModelObject(parent)) : -1;
    const nanoem_f32_t *origin = nanoemModelBoneGetOrigin(bone);
    info->origin_x = origin ? origin[0] : 0.0f;
    info->origin_y = origin ? origin[1] : 0.0f;
    info->origin_z = origin ? origin[2] : 0.0f;
}

nanoem_u32_t APIENTRY nanoemModelGetBoneCount(const nanoem_model_t *model) {
    nanoem_rsize_t num_bones = 0;
    nanoemModelGetAllBoneObjects(model, &num_bones);
    return (nanoem_u32_t) num_bones;
}

void APIENTRY nanoemModelGetMorphInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_morph_info_t *info) {
    nanoem_rsize_t num_morphs = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &num_morphs);
    if (index >= num_morphs) {
        memset(info, 0, sizeof(*info));
        return;
    }
    const nanoem_model_morph_t *morph = morphs[index];
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    info->name = copy_string(nanoemModelMorphGetName(morph, NANOEM_LANGUAGE_TYPE_JAPANESE), &status);
    info->english_name = copy_string(nanoemModelMorphGetName(morph, NANOEM_LANGUAGE_TYPE_ENGLISH), &status);
    info->category = (int) nanoemModelMorphGetCategory(morph);
    info->type = (int) nanoemModelMorphGetType(morph);
}

nanoem_u32_t APIENTRY nanoemModelGetMorphCount(const nanoem_model_t *model) {
    nanoem_rsize_t num_morphs = 0;
    nanoemModelGetAllMorphObjects(model, &num_morphs);
    return (nanoem_u32_t) num_morphs;
}

nanoem_u32_t APIENTRY nanoemModelGetTextureCount(const nanoem_model_t *model) {
    nanoem_rsize_t num_textures = 0;
    nanoemModelGetAllTextureObjects(model, &num_textures);
    return (nanoem_u32_t) num_textures;
}

char *APIENTRY nanoemModelGetTexturePathAt(const nanoem_model_t *model, nanoem_rsize_t index) {
    nanoem_rsize_t num_textures = 0;
    const nanoem_model_texture_t *const *textures = nanoemModelGetAllTextureObjects(model, &num_textures);
    if (index >= num_textures) {
        return NULL;
    }
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    return copy_string(nanoemModelTextureGetPath(textures[index]), &status);
}

nanoem_u32_t APIENTRY nanoemModelGetMaterialCount(const nanoem_model_t *model) {
    nanoem_rsize_t num_materials = 0;
    nanoemModelGetAllMaterialObjects(model, &num_materials);
    return (nanoem_u32_t) num_materials;
}

void APIENTRY nanoemModelGetMaterialInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_material_info_t *info) {
    nanoem_rsize_t num_materials = 0;
    const nanoem_model_material_t *const *materials = nanoemModelGetAllMaterialObjects(model, &num_materials);
    if (index >= num_materials) {
        memset(info, 0, sizeof(*info));
        return;
    }
    const nanoem_model_material_t *material = materials[index];
    nanoem_status_t status = NANOEM_STATUS_SUCCESS;
    info->name = copy_string(nanoemModelMaterialGetName(material, NANOEM_LANGUAGE_TYPE_JAPANESE), &status);
    info->english_name = copy_string(nanoemModelMaterialGetName(material, NANOEM_LANGUAGE_TYPE_ENGLISH), &status);
    const nanoem_f32_t *diffuse = nanoemModelMaterialGetDiffuseColor(material);
    const nanoem_f32_t *specular = nanoemModelMaterialGetSpecularColor(material);
    const nanoem_f32_t *ambient = nanoemModelMaterialGetAmbientColor(material);
    info->diffuse_r = diffuse ? diffuse[0] : 0.0f;
    info->diffuse_g = diffuse ? diffuse[1] : 0.0f;
    info->diffuse_b = diffuse ? diffuse[2] : 0.0f;
    info->diffuse_a = nanoemModelMaterialGetDiffuseOpacity(material);
    info->specular_r = specular ? specular[0] : 0.0f;
    info->specular_g = specular ? specular[1] : 0.0f;
    info->specular_b = specular ? specular[2] : 0.0f;
    info->specular_a = nanoemModelMaterialGetEdgeOpacity(material);
    info->ambient_r = ambient ? ambient[0] : 0.0f;
    info->ambient_g = ambient ? ambient[1] : 0.0f;
    info->ambient_b = ambient ? ambient[2] : 0.0f;
    info->ambient_a = 0.0f;
    info->texture_index = (int) nanoemModelMaterialGetToonTextureIndex(material);
}

float APIENTRY nanoemModelGetMorphInitialWeight(const nanoem_model_t *model, nanoem_rsize_t index) {
    nanoem_mark_unused(model);
    nanoem_mark_unused(index);
    return 0.0f;
}

void APIENTRY nanoemModelIOFree(void *ptr) {
    free(ptr);
}
