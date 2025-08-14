/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "nanoem_p.h"

static void *
nanoemDefaultMalloc(void *opaque, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return malloc(size);
}

static void *
nanoemDefaultCalloc(void *opaque, nanoem_rsize_t length, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return calloc(length, size);
}

static void *
nanoemDefaultRealloc(void *opaque, void *ptr, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return realloc(ptr, size);
}

static void
nanoemDefaultFree(void *opaque, void *ptr, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    free(ptr);
}

static const nanoem_global_allocator_t __nanoem_default_allocator = {
    NULL,
    nanoemDefaultMalloc,
    nanoemDefaultCalloc,
    nanoemDefaultRealloc,
    nanoemDefaultFree
};

NANOEM_DECL_TLS const nanoem_global_allocator_t *__nanoem_global_allocator = &__nanoem_default_allocator;

const nanoem_global_allocator_t *APIENTRY
nanoemGlobalGetCustomAllocator(void)
{
    return __nanoem_global_allocator;
}

void APIENTRY
nanoemGlobalSetCustomAllocator(const nanoem_global_allocator_t *allocator)
{
    if (nanoem_is_not_null(allocator)) {
        __nanoem_global_allocator = allocator;
    }
    else {
        __nanoem_global_allocator = &__nanoem_default_allocator;
    }
}

typedef struct nanoem_model_t {
    nanoem_u32_t num_vertices;
} nanoem_model_t;

typedef struct nanoem_motion_t {
    nanoem_u32_t num_bone_keyframes;
} nanoem_motion_t;

nanoem_model_t *APIENTRY
nanoemModelImportPMX(const nanoem_u8_t *bytes, nanoem_rsize_t length, void *factory, nanoem_status_t *status)
{
    nanoem_mark_unused(factory);
    if (nanoem_is_null(bytes) || length < 9) {
        nanoem_status_ptr_assign_null_object(status);
        return NULL;
    }
    nanoem_model_t *model = nanoem_malloc(sizeof(*model), status);
    if (nanoem_status_ptr_has_error(status)) {
        return NULL;
    }
    nanoem_u32_t count = 0;
    nanoem_u8_t header_size = bytes[8];
    nanoem_rsize_t offset = 9 + header_size;
    if (length >= offset + 4) {
        const nanoem_u8_t *p = bytes + offset;
        count = (nanoem_u32_t) p[0] |
                ((nanoem_u32_t) p[1] << 8) |
                ((nanoem_u32_t) p[2] << 16) |
                ((nanoem_u32_t) p[3] << 24);
    }
    model->num_vertices = count;
    nanoem_status_ptr_assign_succeeded(status);
    return model;
}

nanoem_u32_t APIENTRY
nanoemModelGetVertexCount(const nanoem_model_t *model)
{
    return model ? model->num_vertices : 0;
}

void APIENTRY
nanoemModelDestroy(nanoem_model_t *model)
{
    nanoem_free(model);
}

nanoem_motion_t *APIENTRY
nanoemMotionImportVMD(const nanoem_u8_t *bytes, nanoem_rsize_t length, void *factory, nanoem_status_t *status)
{
    nanoem_mark_unused(factory);
    if (nanoem_is_null(bytes) || length < 54) {
        nanoem_status_ptr_assign_null_object(status);
        return NULL;
    }
    nanoem_motion_t *motion = nanoem_malloc(sizeof(*motion), status);
    if (nanoem_status_ptr_has_error(status)) {
        return NULL;
    }
    const nanoem_u8_t *p = bytes + 50;
    nanoem_u32_t count = (nanoem_u32_t) p[0] |
                         ((nanoem_u32_t) p[1] << 8) |
                         ((nanoem_u32_t) p[2] << 16) |
                         ((nanoem_u32_t) p[3] << 24);
    motion->num_bone_keyframes = count;
    nanoem_status_ptr_assign_succeeded(status);
    return motion;
}

nanoem_u32_t APIENTRY
nanoemMotionGetBoneKeyframeCount(const nanoem_motion_t *motion)
{
    return motion ? motion->num_bone_keyframes : 0;
}

void APIENTRY
nanoemMotionDestroy(nanoem_motion_t *motion)
{
    nanoem_free(motion);
}
