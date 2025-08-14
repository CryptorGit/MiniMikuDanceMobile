/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#pragma once

#include "../nanoem_p.h"

NANOEM_DECL_API void APIENTRY nanoemModelBoneGetTransformMatrix(const nanoem_model_bone_t *bone, nanoem_f32_t *value);
NANOEM_DECL_API void APIENTRY nanoemModelBoneSetTransformMatrix(nanoem_model_bone_t *bone, const nanoem_f32_t *value);

