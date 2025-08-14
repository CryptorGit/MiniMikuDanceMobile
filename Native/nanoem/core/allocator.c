/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "nanoem_p.h"

typedef struct nanoem_allocator_context_t {
    nanoem_rsize_t num_allocated;
} nanoem_allocator_context_t;

static void *
nanoemAllocatorMalloc(void *opaque, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_allocator_context_t *ctx = (nanoem_allocator_context_t *) opaque;
    void *ptr = malloc(size);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    if (ptr && ctx) {
        ctx->num_allocated++;
    }
    return ptr;
}

static void *
nanoemAllocatorCalloc(void *opaque, nanoem_rsize_t length, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_allocator_context_t *ctx = (nanoem_allocator_context_t *) opaque;
    void *ptr = calloc(length, size);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    if (ptr && ctx) {
        ctx->num_allocated++;
    }
    return ptr;
}

static void *
nanoemAllocatorRealloc(void *opaque, void *ptr, nanoem_rsize_t size, const char *filename, int line)
{
    nanoem_allocator_context_t *ctx = (nanoem_allocator_context_t *) opaque;
    void *new_ptr = realloc(ptr, size);
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    if (!ptr && new_ptr && ctx) {
        ctx->num_allocated++;
    }
    else if (ptr && size == 0 && ctx && ctx->num_allocated > 0) {
        ctx->num_allocated--;
    }
    return new_ptr;
}

static void
nanoemAllocatorFree(void *opaque, void *ptr, const char *filename, int line)
{
    nanoem_allocator_context_t *ctx = (nanoem_allocator_context_t *) opaque;
    nanoem_mark_unused(filename);
    nanoem_mark_unused(line);
    if (ptr) {
        free(ptr);
        if (ctx && ctx->num_allocated > 0) {
            ctx->num_allocated--;
        }
    }
}

static nanoem_global_allocator_t g_allocator = {
    NULL,
    nanoemAllocatorMalloc,
    nanoemAllocatorCalloc,
    nanoemAllocatorRealloc,
    nanoemAllocatorFree
};

void APIENTRY
nanoemGlobalAllocatorInitialize(void)
{
    nanoem_allocator_context_t *ctx =
        (nanoem_allocator_context_t *) malloc(sizeof(*ctx));
    if (ctx) {
        ctx->num_allocated = 0;
        g_allocator.opaque = ctx;
        nanoemGlobalSetCustomAllocator(&g_allocator);
    }
}

void APIENTRY
nanoemGlobalAllocatorTerminate(void)
{
    nanoem_allocator_context_t *ctx =
        (nanoem_allocator_context_t *) g_allocator.opaque;
    nanoemGlobalSetCustomAllocator(NULL);
    if (ctx) {
        /* TODO: メモリリーク検出の詳細なログ出力 */
        free(ctx);
        g_allocator.opaque = NULL;
    }
}

#if defined(_WIN32)
#include <windows.h>
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    switch (fdwReason) {
    case DLL_PROCESS_ATTACH:
        nanoemGlobalAllocatorInitialize();
        break;
    case DLL_PROCESS_DETACH:
        nanoemGlobalAllocatorTerminate();
        break;
    default:
        break;
    }
    return TRUE;
}
#else
static void nanoemAllocatorOnLoad(void) __attribute__((constructor));
static void nanoemAllocatorOnLoad(void)
{
    nanoemGlobalAllocatorInitialize();
}
static void nanoemAllocatorOnUnload(void) __attribute__((destructor));
static void nanoemAllocatorOnUnload(void)
{
    nanoemGlobalAllocatorTerminate();
}
#endif

