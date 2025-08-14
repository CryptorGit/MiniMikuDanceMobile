/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "nanoem_p.h"

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
#include <stdio.h>
#endif

static void *
nanoemDefaultMalloc(void *opaque, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return malloc(size);
}

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
static void *
nanoemDebugMalloc(void *opaque, nanoem_rsize_t size, const char *filename, int line)
{
    void *p = nanoemDefaultMalloc(opaque, size, filename, line);
    fprintf(stderr, "[nanoem] malloc %zu bytes at %s:%d -> %p\n", (size_t) size,
            filename ? filename : "(null)", line, p);
    return p;
}
#endif

static void *
nanoemDefaultCalloc(void *opaque, nanoem_rsize_t length, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return calloc(length, size);
}

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
static void *
nanoemDebugCalloc(void *opaque, nanoem_rsize_t length, nanoem_rsize_t size, const char *filename, int line)
{
    void *p = nanoemDefaultCalloc(opaque, length, size, filename, line);
    fprintf(stderr, "[nanoem] calloc %zu x %zu bytes at %s:%d -> %p\n", (size_t) length,
            (size_t) size, filename ? filename : "(null)", line, p);
    return p;
}
#endif

static void *
nanoemDefaultRealloc(void *opaque, void *ptr, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    return realloc(ptr, size);
}

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
static void *
nanoemDebugRealloc(void *opaque, void *ptr, nanoem_rsize_t size, const char *filename, int line)
{
    void *p = nanoemDefaultRealloc(opaque, ptr, size, filename, line);
    fprintf(stderr, "[nanoem] realloc %p to %zu bytes at %s:%d -> %p\n", ptr, (size_t) size,
            filename ? filename : "(null)", line, p);
    return p;
}
#endif

static void
nanoemDefaultFree(void *opaque, void *ptr, const char *filename, int line)
{
    nanoem_mark_unused(opaque);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    free(ptr);
}

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
static void
nanoemDebugFree(void *opaque, void *ptr, const char *filename, int line)
{
    nanoemDefaultFree(opaque, ptr, filename, line);
    fprintf(stderr, "[nanoem] free %p at %s:%d\n", ptr, filename ? filename : "(null)", line);
}
#endif

static const nanoem_global_allocator_t __nanoem_default_allocator = {
    NULL,
#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
    nanoemDebugMalloc,
    nanoemDebugCalloc,
    nanoemDebugRealloc,
    nanoemDebugFree
#else
    nanoemDefaultMalloc,
    nanoemDefaultCalloc,
    nanoemDefaultRealloc,
    nanoemDefaultFree
#endif
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
