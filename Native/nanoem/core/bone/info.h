/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/
#pragma once
#ifndef NANOEM_CORE_BONE_INFO_H_
#define NANOEM_CORE_BONE_INFO_H_

#include "../nanoem.h"

NANOEM_DECL_API const char *APIENTRY
nanoemModelGetBoneName(const nanoem_model_t *model, int index);
NANOEM_DECL_API int APIENTRY
nanoemModelBoneGetParent(const nanoem_model_bone_t *bone);

#endif /* NANOEM_CORE_BONE_INFO_H_ */
