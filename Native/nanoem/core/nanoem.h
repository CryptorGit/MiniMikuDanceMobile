/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef _WIN32
#define APIENTRY __stdcall
#else
#define APIENTRY
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef size_t nanoem_rsize_t;
typedef uint8_t nanoem_u8_t;
typedef uint32_t nanoem_u32_t;
typedef uint32_t nanoem_frame_index_t;

typedef enum nanoem_status_t {
    NANOEM_STATUS_SUCCESS = 0,
    NANOEM_STATUS_ERROR_NULL_OBJECT = -1,
    NANOEM_STATUS_ERROR_MALLOC_FAILED = -2,
    NANOEM_STATUS_ERROR_REALLOC_FAILED = -3
} nanoem_status_t;

typedef void *(*nanoem_global_allocator_malloc_t)(void *, nanoem_rsize_t, const char *, int);
typedef void *(*nanoem_global_allocator_calloc_t)(void *, nanoem_rsize_t, nanoem_rsize_t, const char *, int);
typedef void *(*nanoem_global_allocator_realloc_t)(void *, void *, nanoem_rsize_t, const char *, int);
typedef void (*nanoem_global_allocator_free_t)(void *, void *, const char *, int);

typedef struct nanoem_global_allocator_t {
    void *opaque;
    nanoem_global_allocator_malloc_t malloc;
    nanoem_global_allocator_calloc_t calloc;
    nanoem_global_allocator_realloc_t realloc;
    nanoem_global_allocator_free_t free;
} nanoem_global_allocator_t;

const nanoem_global_allocator_t *APIENTRY nanoemGlobalGetCustomAllocator(void);
void APIENTRY nanoemGlobalSetCustomAllocator(const nanoem_global_allocator_t *allocator);

const char *APIENTRY nanoemGetVersionString(void);

#ifdef __cplusplus
}
#endif
