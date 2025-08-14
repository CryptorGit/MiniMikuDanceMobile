#include "IKSolver.h"
#include "../pmx/nanoem.h"
#include <stdlib.h>
#include <string.h>

void APIENTRY
nanoemModelGetIKConstraintInfo(const nanoem_model_t *model, nanoem_rsize_t bone_index, nanoem_model_ik_constraint_info_t *info)
{
    memset(info, 0, sizeof(*info));
    info->target_bone_index = -1;
    if (!model) {
        return;
    }
    nanoem_rsize_t num_bones = 0;
    const nanoem_model_bone_t *const *bones = nanoemModelGetAllBoneObjects(model, &num_bones);
    if (bone_index >= num_bones) {
        return;
    }
    const nanoem_model_bone_t *bone = bones[bone_index];
    const nanoem_model_constraint_t *constraint = nanoemModelBoneGetConstraintObject(bone);
    if (!constraint) {
        return;
    }
    const nanoem_model_bone_t *target = nanoemModelConstraintGetTargetBoneObject(constraint);
    info->target_bone_index = target ? (int32_t) nanoemModelObjectGetIndex(nanoemModelBoneGetModelObject(target)) : -1;
    info->angle_limit = nanoemModelConstraintGetAngleLimit(constraint);
    info->num_iterations = nanoemModelConstraintGetNumIterations(constraint);
    nanoem_rsize_t num_joints = 0;
    nanoem_model_constraint_joint_t *const *joints = nanoemModelConstraintGetAllJointObjects(constraint, &num_joints);
    info->num_links = (int32_t) num_joints;
    if (num_joints > 0) {
        info->links = (nanoem_model_ik_constraint_link_t *) malloc(sizeof(*info->links) * num_joints);
        if (!info->links) {
            info->num_links = 0;
            return;
        }
        for (nanoem_rsize_t i = 0; i < num_joints; i++) {
            const nanoem_model_constraint_joint_t *joint = joints[i];
            nanoem_model_ik_constraint_link_t *link = &info->links[i];
            const nanoem_model_bone_t *jb = nanoemModelConstraintJointGetBoneObject(joint);
            link->bone_index = jb ? (int32_t) nanoemModelObjectGetIndex(nanoemModelBoneGetModelObject(jb)) : -1;
            link->has_limit = nanoemModelConstraintJointHasAngleLimit(joint);
            const nanoem_f32_t *lower = nanoemModelConstraintJointGetLowerLimit(joint);
            const nanoem_f32_t *upper = nanoemModelConstraintJointGetUpperLimit(joint);
            if (lower) {
                link->lower_limit[0] = lower[0];
                link->lower_limit[1] = lower[1];
                link->lower_limit[2] = lower[2];
            }
            else {
                link->lower_limit[0] = link->lower_limit[1] = link->lower_limit[2] = 0.0f;
            }
            if (upper) {
                link->upper_limit[0] = upper[0];
                link->upper_limit[1] = upper[1];
                link->upper_limit[2] = upper[2];
            }
            else {
                link->upper_limit[0] = link->upper_limit[1] = link->upper_limit[2] = 0.0f;
            }
        }
    }
}
