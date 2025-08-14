/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "nanoem_p.h"

NANOEM_DECL_TLS const nanoem_global_allocator_t *__nanoem_global_allocator = NULL;

const nanoem_global_allocator_t *APIENTRY
nanoemGlobalGetCustomAllocator(void)
{
    return __nanoem_global_allocator;
}

void APIENTRY
nanoemGlobalSetCustomAllocator(const nanoem_global_allocator_t *allocator)
{
    __nanoem_global_allocator = allocator;
}
