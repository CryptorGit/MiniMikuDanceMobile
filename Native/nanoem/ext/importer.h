/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#pragma once
#ifndef NANOEM_EXT_IMPORTER_H_
#define NANOEM_EXT_IMPORTER_H_

#include "../core/nanoem.h"

/**
 * \defgroup nanoem nanoem
 * @{
 */

/**
 * \defgroup nanoem_importer Importer Interface
 * @{
 */
NANOEM_DECL_OPAQUE(nanoem_model_t);
NANOEM_DECL_OPAQUE(nanoem_motion_t);

NANOEM_DECL_API nanoem_model_t *APIENTRY
nanoemModelImportPMX(const nanoem_u8_t *bytes, nanoem_rsize_t length, void *factory, nanoem_status_t *status);
NANOEM_DECL_API nanoem_u32_t APIENTRY
nanoemModelGetVertexCount(const nanoem_model_t *model);
NANOEM_DECL_API void APIENTRY
nanoemModelDestroy(nanoem_model_t *model);

NANOEM_DECL_API nanoem_motion_t *APIENTRY
nanoemMotionImportVMD(const nanoem_u8_t *bytes, nanoem_rsize_t length, void *factory, nanoem_status_t *status);
NANOEM_DECL_API nanoem_u32_t APIENTRY
nanoemMotionGetBoneKeyframeCount(const nanoem_motion_t *motion);
NANOEM_DECL_API void APIENTRY
nanoemMotionDestroy(nanoem_motion_t *motion);

/** @} */ /* end of nanoem_importer */
/** @} */ /* end of nanoem */

#endif /* NANOEM_EXT_IMPORTER_H_ */
