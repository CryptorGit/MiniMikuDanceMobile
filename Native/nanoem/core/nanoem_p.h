/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#pragma once

#include "nanoem.h"

#ifdef __cplusplus
extern "C" {
#endif

/* status helper macros */
#define nanoem_status_ptr_assign(status_ptr, value) \
  do { \
    if ((status_ptr) != NULL) { \
      *(status_ptr) = (value); \
    } \
  } while (0)

#define nanoem_status_ptr_assign_null_object(status_ptr) \
  nanoem_status_ptr_assign((status_ptr), NANOEM_STATUS_ERROR_NULL_OBJECT)

/* TLS declaration */
#define NANOEM_DECL_TLS

/* global allocator */
NANOEM_DECL_TLS extern const nanoem_global_allocator_t *__nanoem_global_allocator;

#ifdef __cplusplus
}
#endif
