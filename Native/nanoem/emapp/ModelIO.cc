/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license.
   see LICENSE.md for more details.
*/
#include "nanoem/nanoem.h"

extern "C" {

NANOEM_DECL_API nanoem_model_t *APIENTRY
nanoemModelImportPMX(const nanoem_u8_t *bytes, size_t length,
                     nanoem_unicode_string_factory_t *factory,
                     nanoem_status_t *status)
{
    nanoem_buffer_t *buffer = nanoemBufferCreate(bytes, length, status);
    nanoem_model_t *model = NULL;
    if (buffer && !nanoem_status_ptr_has_error(status)) {
        model = nanoemModelCreate(factory, status);
        if (model && !nanoem_status_ptr_has_error(status)) {
            nanoemModelParsePMX(model, buffer, status);
        }
    }
    nanoemBufferDestroy(buffer);
    return model;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetVertexCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllVertexObjects(model, &count);
    return count;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemModelGetMorphCount(const nanoem_model_t *model)
{
    nanoem_rsize_t count = 0;
    nanoemModelGetAllMorphObjects(model, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemModelDestroy(nanoem_model_t *model)
{
    nanoemModelDestroy(model);
}

NANOEM_DECL_API nanoem_motion_t *APIENTRY
nanoemMotionImportVMD(const nanoem_u8_t *bytes, size_t length,
                      nanoem_unicode_string_factory_t *factory,
                      nanoem_status_t *status)
{
    nanoem_buffer_t *buffer = nanoemBufferCreate(bytes, length, status);
    nanoem_motion_t *motion = NULL;
    if (buffer && !nanoem_status_ptr_has_error(status)) {
        motion = nanoemMotionCreate(factory, status);
        if (motion && !nanoem_status_ptr_has_error(status)) {
            nanoemMotionParseVMD(motion, buffer, 0, status);
        }
    }
    nanoemBufferDestroy(buffer);
    return motion;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemMotionGetBoneKeyframeCount(const nanoem_motion_t *motion)
{
    nanoem_rsize_t count = 0;
    nanoemMotionGetAllBoneKeyframeObjects(motion, &count);
    return count;
}

NANOEM_DECL_API nanoem_rsize_t APIENTRY
nanoemMotionGetMorphKeyframeCount(const nanoem_motion_t *motion)
{
    nanoem_rsize_t count = 0;
    nanoemMotionGetAllMorphKeyframeObjects(motion, &count);
    return count;
}

NANOEM_DECL_API void APIENTRY
nanoemMotionDestroy(nanoem_motion_t *motion)
{
    nanoemMotionDestroy(motion);
}

} /* extern "C" */
