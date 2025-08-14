/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "../ext/physics.h"

#include <stdlib.h>

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
}

nanoem_physics_rigid_body_t *APIENTRY
nanoemEmappPhysicsRigidBodyCreate(
    const nanoem_model_rigid_body_t *value, nanoem_emapp_physics_world_t *world, nanoem_status_t *status)
{
    (void) world;
    return nanoemPhysicsRigidBodyCreate(value, NULL, status);
}

void APIENTRY
nanoemEmappPhysicsRigidBodyDestroy(nanoem_physics_rigid_body_t *body)
{
    nanoemPhysicsRigidBodyDestroy(body);
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

