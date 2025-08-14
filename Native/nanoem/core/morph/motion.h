/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/
#pragma once
#ifndef NANOEM_CORE_MORPH_MOTION_H_
#define NANOEM_CORE_MORPH_MOTION_H_

#include "../nanoem.h"

NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphCount(const nanoem_model_t *model);
NANOEM_DECL_API const char *APIENTRY
nanoemModelGetMorphName(const nanoem_model_t *model, int index);
NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphCategory(const nanoem_model_t *model, int index);
NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphType(const nanoem_model_t *model, int index);
NANOEM_DECL_API void APIENTRY
nanoemModelSetMorphWeight(nanoem_model_t *model, int index, nanoem_f32_t weight);

NANOEM_DECL_API void
nanoemMotionReadMorphKeyframeBundleUnitNMD(nanoem_motion_t *motion, const Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message, nanoem_frame_index_t offset, nanoem_status_t *status);
NANOEM_DECL_API void
nanoemMutableMotionWriteMorphTrackBundleNMD(const nanoem_motion_t *motion, Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message, nanoem_status_t *status);
NANOEM_DECL_API void
nanoemMutableMotionWriteMorphKeyframeBundleUnitNMD(const nanoem_motion_t *motion, Nanoem__Motion__Motion *motion_message, nanoem_rsize_t *keyframe_bundle_index, nanoem_status_t *status);
NANOEM_DECL_API void
nanoemMutableMotionDestroyMorphKeyframeBundleNMD(Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message);

#endif /* NANOEM_CORE_MORPH_MOTION_H_ */
