/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "../nanoem_p.h"

const nanoem_model_bone_t *APIENTRY
nanoemModelBoneGetParentBoneObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? nanoemModelGetOneBoneObject(nanoemModelBoneGetParentModel(bone), bone->parent_bone_index) : NULL;
}

const nanoem_model_bone_t *APIENTRY
nanoemModelBoneGetInherentParentBoneObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? nanoemModelGetOneBoneObject(nanoemModelBoneGetParentModel(bone), bone->parent_inherent_bone_index) : NULL;
}

const nanoem_model_bone_t *APIENTRY
nanoemModelBoneGetEffectorBoneObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? nanoemModelGetOneBoneObject(nanoemModelBoneGetParentModel(bone), bone->effector_bone_index) : NULL;
}

const nanoem_model_bone_t *APIENTRY
nanoemModelBoneGetTargetBoneObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? nanoemModelGetOneBoneObject(nanoemModelBoneGetParentModel(bone), bone->target_bone_index) : NULL;
}

const nanoem_model_constraint_t *APIENTRY
nanoemModelBoneGetConstraintObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->constraint : NULL;
}

nanoem_model_constraint_t *APIENTRY
nanoemModelBoneGetConstraintObjectMutable(nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->constraint : NULL;
}

const nanoem_f32_t *APIENTRY
nanoemModelBoneGetOrigin(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->origin.values : __nanoem_null_vector4;
}

const nanoem_f32_t *APIENTRY
nanoemModelBoneGetDestinationOrigin(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->destination_origin.values : __nanoem_null_vector4;
}

const nanoem_f32_t *APIENTRY
nanoemModelBoneGetFixedAxis(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->fixed_axis.values : __nanoem_null_vector4;
}

const nanoem_f32_t *APIENTRY
nanoemModelBoneGetLocalXAxis(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->local_x_axis.values : __nanoem_null_vector4;
}

const nanoem_f32_t *APIENTRY
nanoemModelBoneGetLocalZAxis(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->local_z_axis.values : __nanoem_null_vector4;
}

nanoem_f32_t APIENTRY
nanoemModelBoneGetInherentCoefficient(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->inherent_coefficient : 0;
}

int APIENTRY
nanoemModelBoneGetStageIndex(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->stage_index : 0;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasDestinationBone(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_destination_bone_index : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneIsRotateable(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.is_rotateable : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneIsMovable(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.is_movable : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneIsVisible(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.is_visible : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneIsUserHandleable(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.is_user_handleable : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasConstraint(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_constraint : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasLocalInherent(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_local_inherent : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasInherentTranslation(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_inherent_translation : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasInherentOrientation(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_inherent_orientation : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasFixedAxis(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_fixed_axis : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasLocalAxes(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_local_axes : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneIsAffectedByPhysicsSimulation(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.is_affected_by_physics_simulation : nanoem_false;
}

nanoem_bool_t APIENTRY
nanoemModelBoneHasExternalParentBone(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? bone->u.flags.has_external_parent_bone : nanoem_false;
}

const nanoem_model_object_t *APIENTRY
nanoemModelBoneGetModelObject(const nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? &bone->base : NULL;
}

nanoem_model_object_t *APIENTRY
nanoemModelBoneGetModelObjectMutable(nanoem_model_bone_t *bone)
{
    return nanoem_is_not_null(bone) ? &bone->base : NULL;
}

void APIENTRY
nanoemModelBoneGetTranslation(const nanoem_model_bone_t *bone, nanoem_f32_t *value)
{
    if (nanoem_is_not_null(bone) && nanoem_is_not_null(value)) {
        const nanoem_f32_t *origin = nanoemModelBoneGetOrigin(bone);
        value[0] = origin[0];
        value[1] = origin[1];
        value[2] = origin[2];
    }
}

void APIENTRY
nanoemModelBoneSetTranslation(nanoem_model_bone_t *bone, const nanoem_f32_t *value)
{
    if (nanoem_is_not_null(bone) && nanoem_is_not_null(value)) {
        bone->origin.values[0] = value[0];
        bone->origin.values[1] = value[1];
        bone->origin.values[2] = value[2];
    }
}

