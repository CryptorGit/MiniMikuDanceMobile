/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license.
   see LICENSE.md for more details.
*/
#include "nanoem.h"

typedef void nanoem_model_t;
typedef void nanoem_motion_t;
typedef void nanoem_buffer_t;
typedef void nanoem_unicode_string_factory_t;

extern "C" {

nanoem_model_t *nanoemModelImportPMX(const nanoem_u8_t *bytes, size_t length,
                     nanoem_unicode_string_factory_t *factory,
                     nanoem_status_t *status)
{
    (void) bytes; (void) length; (void) factory; (void) status;
    return NULL;
}

nanoem_rsize_t nanoemModelGetVertexCount(const nanoem_model_t *model)
{
    (void) model;
    return 0;
}

void nanoemModelDestroy(nanoem_model_t *model)
{
    (void) model;
}

nanoem_motion_t *nanoemMotionImportVMD(const nanoem_u8_t *bytes, size_t length,
                      nanoem_unicode_string_factory_t *factory,
                      nanoem_status_t *status)
{
    (void) bytes; (void) length; (void) factory; (void) status;
    return NULL;
}

nanoem_rsize_t nanoemMotionGetBoneKeyframeCount(const nanoem_motion_t *motion)
{
    (void) motion;
    return 0;
}

void nanoemMotionDestroy(nanoem_motion_t *motion)
{
    (void) motion;
}

} /* extern "C" */
