/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#pragma once

#include "nanoem.h"

#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

/* status helper macros */
#define nanoem_status_ptr_assign(status_ptr, value)                                                     \
    do {                                                                                                \
        if ((status_ptr) != NULL) {                                                                     \
            *(status_ptr) = (value);                                                                    \
        }                                                                                               \
    } while (0)
#define nanoem_status_ptr_has_error(status_ptr)                                                         \
    ((status_ptr) ? (*(status_ptr) != NANOEM_STATUS_SUCCESS ? nanoem_true : nanoem_false) : nanoem_false)
#define nanoem_status_ptr_assign_succeeded(status_ptr)                                                  \
    nanoem_status_ptr_assign((status_ptr), NANOEM_STATUS_SUCCESS)
#define nanoem_status_ptr_assign_null_object(status_ptr)                                                \
    nanoem_status_ptr_assign((status_ptr), NANOEM_STATUS_ERROR_NULL_OBJECT)
#define nanoem_status_ptr_assign_select(status_ptr, error)                                              \
    nanoem_status_ptr_assign((status_ptr),                                                             \
                             nanoem_status_ptr_has_error((status_ptr)) ? (error) : NANOEM_STATUS_SUCCESS)

/* TLS declaration */
#define NANOEM_DECL_TLS

/* global allocator */
NANOEM_DECL_TLS extern const nanoem_global_allocator_t *__nanoem_global_allocator;

/* allocation helpers */
static void *
nanoemMemoryAllocate(nanoem_rsize_t size, nanoem_status_t *status, const char *filename, int line)
{
    void *p = NULL;
    if (nanoem_likely(size <= NANOEM_RSIZE_MAX)) {
        if (__nanoem_global_allocator != NULL) {
            p = __nanoem_global_allocator->malloc(__nanoem_global_allocator->opaque, size, filename, line);
        }
        else {
            p = malloc(size);
        }
    }
    if (nanoem_is_null(p)) {
        nanoem_status_ptr_assign(status, NANOEM_STATUS_ERROR_MALLOC_FAILED);
    }
    return p;
}

static void *
nanoemMemoryResize(void *ptr, nanoem_rsize_t size, nanoem_status_t *status, const char *filename, int line)
{
    void *p = NULL;
    if (nanoem_likely(size > 0 && size <= NANOEM_RSIZE_MAX)) {
        if (__nanoem_global_allocator != NULL) {
            p = __nanoem_global_allocator->realloc(__nanoem_global_allocator->opaque, ptr, size, filename, line);
        }
        else {
            p = realloc(ptr, size);
        }
    }
    if (nanoem_is_null(p)) {
        nanoem_status_ptr_assign(status, NANOEM_STATUS_ERROR_REALLOC_FAILED);
    }
    return p;
}

static void *
nanoemMemoryAllocateSafe(nanoem_rsize_t length, nanoem_rsize_t size, nanoem_status_t *status, const char *filename, int line)
{
    void *p = NULL;
    if (nanoem_likely(length > 0 && size > 0 && length <= (NANOEM_RSIZE_MAX / size))) {
        if (__nanoem_global_allocator != NULL) {
            p = __nanoem_global_allocator->calloc(__nanoem_global_allocator->opaque, length, size, filename, line);
        }
        else {
            p = calloc(length, size);
        }
    }
    if (nanoem_is_null(p)) {
        nanoem_status_ptr_assign(status, NANOEM_STATUS_ERROR_MALLOC_FAILED);
    }
    return p;
}

static void
nanoemMemoryRelease(void *ptr, const char *filename, int line)
{
    if (ptr) {
        if (__nanoem_global_allocator != NULL) {
            __nanoem_global_allocator->free(__nanoem_global_allocator->opaque, ptr, filename, line);
        }
        else {
            free(ptr);
        }
    }
}

/* allocation macros */
#define nanoem_malloc_loc(size, status, filename, line) nanoemMemoryAllocate((size), (status), (filename), (line))
#define nanoem_realloc_loc(ptr, size, status, filename, line)                                            \
    nanoemMemoryResize((ptr), (size), (status), (filename), (line))
#define nanoem_calloc_loc(length, size, status, filename, line)                                          \
    nanoemMemoryAllocateSafe((length), (size), (status), (filename), (line))
#define nanoem_free_loc(ptr, filename, line) nanoemMemoryRelease((ptr), (filename), (line))

#ifdef NANOEM_ENABLE_DEBUG_ALLOCATOR
#define nanoem_malloc(size, status) nanoem_malloc_loc((size), (status), __FILE__, __LINE__)
#define nanoem_realloc(ptr, size, status) nanoem_realloc_loc((ptr), (size), (status), __FILE__, __LINE__)
#define nanoem_calloc(length, size, status) nanoem_calloc_loc((length), (size), (status), __FILE__, __LINE__)
#define nanoem_free(ptr) nanoem_free_loc((ptr), __FILE__, __LINE__)
#else
#define nanoem_malloc(size, status) nanoem_malloc_loc((size), (status), NULL, 0)
#define nanoem_realloc(ptr, size, status) nanoem_realloc_loc((ptr), (size), (status), NULL, 0)
#define nanoem_calloc(length, size, status) nanoem_calloc_loc((length), (size), (status), NULL, 0)
#define nanoem_free(ptr) nanoem_free_loc((ptr), NULL, 0)
#endif /* NANOEM_ENABLE_DEBUG_ALLOCATOR */

#ifdef __cplusplus
}
#endif
