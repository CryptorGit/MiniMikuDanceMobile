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

typedef int nanoem_bool_t;
enum {
    nanoem_false = 0,
    nanoem_true = 1
};

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

#ifndef nanoem_likely
#define nanoem_likely(expr) (expr)
#endif
#ifndef nanoem_unlikely
#define nanoem_unlikely(expr) (expr)
#endif
#ifndef nanoem_is_null
#define nanoem_is_null(cond) ((cond) == NULL)
#endif
#ifndef nanoem_is_not_null
#define nanoem_is_not_null(cond) ((cond) != NULL)
#endif
#ifndef nanoem_mark_unused
#define nanoem_mark_unused(cond) ((void) (cond))
#endif

#ifndef NANOEM_RSIZE_MAX
#ifdef RSIZE_MAX
#define NANOEM_RSIZE_MAX RSIZE_MAX
#else
#define NANOEM_RSIZE_MAX ((~(size_t) (0)) >> 1)
#endif
#endif /* NANOEM_RSIZE_MAX */

const nanoem_global_allocator_t *APIENTRY nanoemGlobalGetCustomAllocator(void);
void APIENTRY nanoemGlobalSetCustomAllocator(const nanoem_global_allocator_t *allocator);

void APIENTRY nanoemGlobalAllocatorInitialize(void);
void APIENTRY nanoemGlobalAllocatorTerminate(void);

const char *APIENTRY nanoemGetVersionString(void);
int32_t APIENTRY nanoemAdd(int32_t left, int32_t right);

#ifdef __cplusplus
}
#endif
