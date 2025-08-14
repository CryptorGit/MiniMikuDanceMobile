/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "../../ext/physics.h"
#include "../private/CommonInclude.h"
#include "../../core/ext/mutable.h"

#include <stdlib.h>

typedef struct nanoem_emapp_physics_rigid_body_config_t {
    nanoem_f32_t mass;
    int shape;
    nanoem_f32_t size[3];
    nanoem_f32_t position[3];
    nanoem_f32_t rotation[3];
    nanoem_f32_t linear_lower[3];
    nanoem_f32_t linear_upper[3];
    nanoem_f32_t angular_lower[3];
    nanoem_f32_t angular_upper[3];
} nanoem_emapp_physics_rigid_body_config_t;

typedef struct nanoem_emapp_physics_joint_config_t {
    nanoem_physics_rigid_body_t *body_a;
    nanoem_physics_rigid_body_t *body_b;
    nanoem_f32_t position[3];
    nanoem_f32_t rotation[3];
    nanoem_f32_t translation_lower[3];
    nanoem_f32_t translation_upper[3];
    nanoem_f32_t rotation_lower[3];
    nanoem_f32_t rotation_upper[3];
    nanoem_f32_t translation_spring[3];
    nanoem_f32_t rotation_spring[3];
} nanoem_emapp_physics_joint_config_t;

typedef struct nanoem_emapp_physics_rigid_body_entry_t {
    nanoem_physics_rigid_body_t *body;
    nanoem_model_rigid_body_t *model;
    struct nanoem_emapp_physics_rigid_body_entry_t *next;
} nanoem_emapp_physics_rigid_body_entry_t;

static nanoem_unicode_string_factory_t *g_factory = NULL;
static nanoem_model_t *g_model = NULL;
static nanoem_emapp_physics_rigid_body_entry_t *g_rigid_bodies = NULL;

static void
nanoem_emapp_physics_initialize(void)
{
    if (!g_factory) {
        nanoem_status_t status = NANOEM_STATUS_SUCCESS;
        g_factory = nanoemUnicodeStringFactoryCreateEXT(&status);
        if (status == NANOEM_STATUS_SUCCESS && !g_model) {
            g_model = nanoemModelCreate(g_factory, &status);
        }
    }
}

static nanoem_model_rigid_body_t *
nanoem_emapp_physics_lookup_model_rigid_body(nanoem_physics_rigid_body_t *body)
{
    nanoem_emapp_physics_rigid_body_entry_t *entry = g_rigid_bodies;
    while (entry) {
        if (entry->body == body) {
            return entry->model;
        }
        entry = entry->next;
    }
    return NULL;
}

struct nanoem_emapp_physics_world_t {
    nanoem_physics_world_t *world;
};

nanoem_emapp_physics_world_t *APIENTRY
nanoemEmappPhysicsWorldCreate(void *opaque, nanoem_status_t *status)
{
    nanoem_emapp_physics_world_t *wrapper =
        (nanoem_emapp_physics_world_t *) calloc(1, sizeof(nanoem_emapp_physics_world_t));
    if (wrapper) {
        wrapper->world = nanoemPhysicsWorldCreate(opaque, status);
    }
    return wrapper;
}

void APIENTRY
nanoemEmappPhysicsWorldDestroy(nanoem_emapp_physics_world_t *world)
{
    if (world) {
        nanoemPhysicsWorldDestroy(world->world);
        free(world);
    }
    while (g_rigid_bodies) {
        nanoem_emapp_physics_rigid_body_entry_t *entry = g_rigid_bodies->next;
        free(g_rigid_bodies);
        g_rigid_bodies = entry;
    }
    if (g_model) {
        nanoemModelDestroy(g_model);
        g_model = NULL;
    }
    if (g_factory) {
        nanoemUnicodeStringFactoryDestroyEXT(g_factory);
        g_factory = NULL;
    }
}

nanoem_physics_rigid_body_t *APIENTRY
nanoemEmappPhysicsRigidBodyCreate(
    const nanoem_model_rigid_body_t *value, nanoem_emapp_physics_world_t *world, nanoem_status_t *status)
{
    (void) world;
    return nanoemPhysicsRigidBodyCreate(value, NULL, status);
}

nanoem_physics_rigid_body_t *APIENTRY
nanoemEmappPhysicsRigidBodyCreateWithConfig(
    const nanoem_emapp_physics_rigid_body_config_t *config, nanoem_emapp_physics_world_t *world, nanoem_status_t *status)
{
    nanoem_physics_rigid_body_t *body = NULL;
    (void) world;
    if (config) {
        nanoem_emapp_physics_initialize();
        if (g_model) {
            nanoem_status_t s = NANOEM_STATUS_SUCCESS;
            nanoem_mutable_model_rigid_body_t *rb = nanoemMutableModelRigidBodyCreate(g_model, &s);
            if (rb && s == NANOEM_STATUS_SUCCESS) {
                nanoem_f32_t v[4];
                nanoemMutableModelRigidBodySetMass(rb, config->mass);
                nanoemMutableModelRigidBodySetShapeType(rb, (nanoem_model_rigid_body_shape_type_t) config->shape);
                v[0] = config->size[0]; v[1] = config->size[1]; v[2] = config->size[2]; v[3] = 0;
                nanoemMutableModelRigidBodySetShapeSize(rb, v);
                v[0] = config->position[0]; v[1] = config->position[1]; v[2] = config->position[2]; v[3] = 0;
                nanoemMutableModelRigidBodySetOrigin(rb, v);
                v[0] = config->rotation[0]; v[1] = config->rotation[1]; v[2] = config->rotation[2]; v[3] = 0;
                nanoemMutableModelRigidBodySetOrientation(rb, v);
                nanoem_model_rigid_body_t *model_body = nanoemMutableModelRigidBodyGetOriginObject(rb);
                body = nanoemPhysicsRigidBodyCreate(model_body, NULL, status);
                if (body) {
                    nanoem_emapp_physics_rigid_body_entry_t *entry =
                        (nanoem_emapp_physics_rigid_body_entry_t *) malloc(sizeof(*entry));
                    if (entry) {
                        entry->body = body;
                        entry->model = model_body;
                        entry->next = g_rigid_bodies;
                        g_rigid_bodies = entry;
                    }
                }
                nanoemMutableModelRigidBodyDestroy(rb);
            }
        }
    }
    return body;
}

void APIENTRY
nanoemEmappPhysicsRigidBodyDestroy(nanoem_physics_rigid_body_t *body)
{
    if (body) {
        nanoem_emapp_physics_rigid_body_entry_t **entry = &g_rigid_bodies;
        while (*entry) {
            if ((*entry)->body == body) {
                nanoem_emapp_physics_rigid_body_entry_t *next = (*entry)->next;
                free(*entry);
                *entry = next;
                break;
            }
            entry = &(*entry)->next;
        }
        nanoemPhysicsRigidBodyDestroy(body);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldAddRigidBody(
    nanoem_emapp_physics_world_t *world, nanoem_physics_rigid_body_t *body)
{
    if (world && body) {
        nanoemPhysicsWorldAddRigidBody(world->world, body);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldRemoveRigidBody(
    nanoem_emapp_physics_world_t *world, nanoem_physics_rigid_body_t *body)
{
    if (world && body) {
        nanoemPhysicsWorldRemoveRigidBody(world->world, body);
    }
}

nanoem_physics_joint_t *APIENTRY
nanoemEmappPhysicsJointCreate(
    const nanoem_model_joint_t *value, nanoem_emapp_physics_world_t *world, nanoem_status_t *status)
{
    return world ? nanoemPhysicsJointCreate(value, world->world, status) : NULL;
}

nanoem_physics_joint_t *APIENTRY
nanoemEmappPhysicsJointCreateWithConfig(
    const nanoem_emapp_physics_joint_config_t *config, nanoem_emapp_physics_world_t *world, nanoem_status_t *status)
{
    nanoem_physics_joint_t *joint = NULL;
    if (world && config) {
        nanoem_emapp_physics_initialize();
        nanoem_model_rigid_body_t *body_a =
            nanoem_emapp_physics_lookup_model_rigid_body(config->body_a);
        nanoem_model_rigid_body_t *body_b =
            nanoem_emapp_physics_lookup_model_rigid_body(config->body_b);
        if (body_a && body_b) {
            nanoem_status_t s = NANOEM_STATUS_SUCCESS;
            nanoem_mutable_model_joint_t *j = nanoemMutableModelJointCreate(g_model, &s);
            if (j && s == NANOEM_STATUS_SUCCESS) {
                nanoem_f32_t v[4];
                nanoemMutableModelJointSetType(j, NANOEM_MODEL_JOINT_TYPE_GENERIC_6DOF_SPRING_CONSTRAINT);
                nanoemMutableModelJointSetRigidBodyAObject(j, body_a);
                nanoemMutableModelJointSetRigidBodyBObject(j, body_b);
                v[0] = config->position[0]; v[1] = config->position[1]; v[2] = config->position[2]; v[3] = 0;
                nanoemMutableModelJointSetOrigin(j, v);
                v[0] = config->rotation[0]; v[1] = config->rotation[1]; v[2] = config->rotation[2]; v[3] = 0;
                nanoemMutableModelJointSetOrientation(j, v);
                v[0] = config->translation_lower[0]; v[1] = config->translation_lower[1]; v[2] = config->translation_lower[2]; v[3] = 0;
                nanoemMutableModelJointSetLinearLowerLimit(j, v);
                v[0] = config->translation_upper[0]; v[1] = config->translation_upper[1]; v[2] = config->translation_upper[2]; v[3] = 0;
                nanoemMutableModelJointSetLinearUpperLimit(j, v);
                v[0] = config->rotation_lower[0]; v[1] = config->rotation_lower[1]; v[2] = config->rotation_lower[2]; v[3] = 0;
                nanoemMutableModelJointSetAngularLowerLimit(j, v);
                v[0] = config->rotation_upper[0]; v[1] = config->rotation_upper[1]; v[2] = config->rotation_upper[2]; v[3] = 0;
                nanoemMutableModelJointSetAngularUpperLimit(j, v);
                v[0] = config->translation_spring[0]; v[1] = config->translation_spring[1]; v[2] = config->translation_spring[2]; v[3] = 0;
                nanoemMutableModelJointSetLinearStiffness(j, v);
                v[0] = config->rotation_spring[0]; v[1] = config->rotation_spring[1]; v[2] = config->rotation_spring[2]; v[3] = 0;
                nanoemMutableModelJointSetAngularStiffness(j, v);
                nanoem_model_joint_t *model_joint = nanoemMutableModelJointGetOriginObject(j);
                joint = nanoemPhysicsJointCreate(model_joint, world->world, status);
                nanoemMutableModelJointDestroy(j);
            }
        }
    }
    return joint;
}

void APIENTRY
nanoemEmappPhysicsJointDestroy(nanoem_physics_joint_t *joint)
{
    nanoemPhysicsJointDestroy(joint);
}

void APIENTRY
nanoemEmappPhysicsWorldAddJoint(
    nanoem_emapp_physics_world_t *world, nanoem_physics_joint_t *joint)
{
    if (world && joint) {
        nanoemPhysicsWorldAddJoint(world->world, joint);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldRemoveJoint(
    nanoem_emapp_physics_world_t *world, nanoem_physics_joint_t *joint)
{
    if (world && joint) {
        nanoemPhysicsWorldRemoveJoint(world->world, joint);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldStepSimulation(nanoem_emapp_physics_world_t *world, nanoem_f32_t delta)
{
    if (world) {
        nanoemPhysicsWorldStepSimulation(world->world, delta);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldReset(nanoem_emapp_physics_world_t *world)
{
    if (world) {
        nanoemPhysicsWorldReset(world->world);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldSetPreferredFPS(nanoem_emapp_physics_world_t *world, int value)
{
    if (world) {
        nanoemPhysicsWorldSetPreferredFPS(world->world, value);
    }
}

void APIENTRY
nanoemEmappPhysicsWorldSetActive(nanoem_emapp_physics_world_t *world, nanoem_bool_t value)
{
    if (world) {
        nanoemPhysicsWorldSetActive(world->world, value);
    }
}

nanoem_bool_t APIENTRY
nanoemEmappPhysicsWorldIsActive(const nanoem_emapp_physics_world_t *world)
{
    return world ? nanoemPhysicsWorldIsActive(world->world) : nanoem_false;
}

void APIENTRY
nanoemEmappPhysicsRigidBodyGetWorldTransform(const nanoem_physics_rigid_body_t *body, nanoem_f32_t *value)
{
    nanoemPhysicsRigidBodyGetWorldTransform(body, value);
}

