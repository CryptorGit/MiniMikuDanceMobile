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

typedef struct nanoem_model_rigid_body_info_t {
    char *name;
    char *englishName;
    int boneIndex;
    float origin[3];
    float orientation[3];
    float size[3];
    float mass;
    float linearDamping;
    float angularDamping;
    float restitution;
    float friction;
    int group;
    int mask;
    int shapeType;
    int transformType;
} nanoem_model_rigid_body_info_t;

typedef struct nanoem_model_joint_info_t {
    char *name;
    char *englishName;
    int rigidBodyA;
    int rigidBodyB;
    float origin[3];
    float orientation[3];
    float linearLowerLimit[3];
    float linearUpperLimit[3];
    float angularLowerLimit[3];
    float angularUpperLimit[3];
    float linearStiffness[3];
    float angularStiffness[3];
} nanoem_model_joint_info_t;

typedef struct nanoem_model_ik_constraint_info_t {
    int effectorBoneIndex;
    int targetBoneIndex;
    int numIterations;
    float angleLimit;
} nanoem_model_ik_constraint_info_t;

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

static int
findBoneIndex(const nanoem_model_t *model, const nanoem_model_bone_t *bone)
{
    nanoem_rsize_t numBones = 0;
    const nanoem_model_bone_t *const *bones = nanoemModelGetAllBoneObjects(model, &numBones);
    for (nanoem_rsize_t i = 0; i < numBones; i++) {
        if (bones[i] == bone) {
            return (int) i;
        }
    }
    return -1;
}

static int
findRigidBodyIndex(const nanoem_model_t *model, const nanoem_model_rigid_body_t *body)
{
    nanoem_rsize_t numBodies = 0;
    const nanoem_model_rigid_body_t *const *bodies = nanoemModelGetAllRigidBodyObjects(model, &numBodies);
    for (nanoem_rsize_t i = 0; i < numBodies; i++) {
        if (bodies[i] == body) {
            return (int) i;
        }
    }
    return -1;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetRigidBodyCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllRigidBodyObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetRigidBodyInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_rigid_body_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numBodies = 0;
    const nanoem_model_rigid_body_t *const *bodies = nanoemModelGetAllRigidBodyObjects(model, &numBodies);
    if (index >= numBodies) {
        memset(info, 0, sizeof(*info));
        info->boneIndex = -1;
        info->group = 0;
        info->mask = 0;
        info->shapeType = 0;
        info->transformType = 0;
        info->name = NULL;
        info->englishName = NULL;
        return;
    }
    const nanoem_model_rigid_body_t *body = bodies[index];
    info->name = unicodeStringToUtf8(nanoemModelRigidBodyGetName(body, NANOEM_LANGUAGE_TYPE_JAPANESE));
    info->englishName = unicodeStringToUtf8(nanoemModelRigidBodyGetName(body, NANOEM_LANGUAGE_TYPE_ENGLISH));
    info->boneIndex = findBoneIndex(model, nanoemModelRigidBodyGetBoneObject(body));
    memcpy(info->origin, nanoemModelRigidBodyGetOrigin(body), sizeof(info->origin));
    memcpy(info->orientation, nanoemModelRigidBodyGetOrientation(body), sizeof(info->orientation));
    memcpy(info->size, nanoemModelRigidBodyGetShapeSize(body), sizeof(info->size));
    info->mass = nanoemModelRigidBodyGetMass(body);
    info->linearDamping = nanoemModelRigidBodyGetLinearDamping(body);
    info->angularDamping = nanoemModelRigidBodyGetAngularDamping(body);
    info->restitution = nanoemModelRigidBodyGetRestitution(body);
    info->friction = nanoemModelRigidBodyGetFriction(body);
    info->group = nanoemModelRigidBodyGetCollisionGroupId(body);
    info->mask = nanoemModelRigidBodyGetCollisionMask(body);
    info->shapeType = (int) nanoemModelRigidBodyGetShapeType(body);
    info->transformType = (int) nanoemModelRigidBodyGetTransformType(body);
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetJointCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllJointObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetJointInfo(const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_joint_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numJoints = 0;
    const nanoem_model_joint_t *const *joints = nanoemModelGetAllJointObjects(model, &numJoints);
    if (index >= numJoints) {
        memset(info, 0, sizeof(*info));
        info->rigidBodyA = -1;
        info->rigidBodyB = -1;
        info->name = NULL;
        info->englishName = NULL;
        return;
    }
    const nanoem_model_joint_t *joint = joints[index];
    info->name = unicodeStringToUtf8(nanoemModelJointGetName(joint, NANOEM_LANGUAGE_TYPE_JAPANESE));
    info->englishName = unicodeStringToUtf8(nanoemModelJointGetName(joint, NANOEM_LANGUAGE_TYPE_ENGLISH));
    info->rigidBodyA = findRigidBodyIndex(model, nanoemModelJointGetRigidBodyAObject(joint));
    info->rigidBodyB = findRigidBodyIndex(model, nanoemModelJointGetRigidBodyBObject(joint));
    memcpy(info->origin, nanoemModelJointGetOrigin(joint), sizeof(info->origin));
    memcpy(info->orientation, nanoemModelJointGetOrientation(joint), sizeof(info->orientation));
    memcpy(info->linearLowerLimit, nanoemModelJointGetLinearLowerLimit(joint), sizeof(info->linearLowerLimit));
    memcpy(info->linearUpperLimit, nanoemModelJointGetLinearUpperLimit(joint), sizeof(info->linearUpperLimit));
    memcpy(info->angularLowerLimit, nanoemModelJointGetAngularLowerLimit(joint), sizeof(info->angularLowerLimit));
    memcpy(info->angularUpperLimit, nanoemModelJointGetAngularUpperLimit(joint), sizeof(info->angularUpperLimit));
    memcpy(info->linearStiffness, nanoemModelJointGetLinearStiffness(joint), sizeof(info->linearStiffness));
    memcpy(info->angularStiffness, nanoemModelJointGetAngularStiffness(joint), sizeof(info->angularStiffness));
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetIKConstraintCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllConstraintObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelGetIKConstraintInfo(
    const nanoem_model_t *model, nanoem_rsize_t index, nanoem_model_ik_constraint_info_t *info)
{
    if (!info) {
        return;
    }
    nanoem_rsize_t numConstraints = 0;
    const nanoem_model_constraint_t *const *constraints =
        nanoemModelGetAllConstraintObjects(model, &numConstraints);
    if (index >= numConstraints) {
        memset(info, 0, sizeof(*info));
        info->effectorBoneIndex = -1;
        info->targetBoneIndex = -1;
        return;
    }
    const nanoem_model_constraint_t *constraint = constraints[index];
    info->effectorBoneIndex =
        findBoneIndex(model, nanoemModelConstraintGetEffectorBoneObject(constraint));
    info->targetBoneIndex = findBoneIndex(model, nanoemModelConstraintGetTargetBoneObject(constraint));
    info->numIterations = nanoemModelConstraintGetNumIterations(constraint);
    info->angleLimit = nanoemModelConstraintGetAngleLimit(constraint);
}

#ifdef __cplusplus
}
#endif

