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
