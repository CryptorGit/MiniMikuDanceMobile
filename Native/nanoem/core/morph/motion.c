/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/
#include "motion.h"
#include "../nanoem_p.h"
#include "../ext/mutable_p.h"
#include "../ext/motion.pb-c.c"

NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphCount(const nanoem_model_t *model)
{
    nanoem_rsize_t num = 0;
    nanoemModelGetAllMorphObjects(model, &num);
    return (int) num;
}

NANOEM_DECL_API const char *APIENTRY
nanoemModelGetMorphName(const nanoem_model_t *model, int index)
{
    nanoem_rsize_t num = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &num);
    if (morphs && index >= 0 && (nanoem_rsize_t) index < num) {
        return nanoemModelMorphGetName(morphs[index], NANOEM_LANGUAGE_TYPE_JAPANESE);
    }
    return NULL;
}

NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphCategory(const nanoem_model_t *model, int index)
{
    nanoem_rsize_t num = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &num);
    if (morphs && index >= 0 && (nanoem_rsize_t) index < num) {
        return (int) nanoemModelMorphGetCategory(morphs[index]);
    }
    return 0;
}

NANOEM_DECL_API int APIENTRY
nanoemModelGetMorphType(const nanoem_model_t *model, int index)
{
    nanoem_rsize_t num = 0;
    const nanoem_model_morph_t *const *morphs = nanoemModelGetAllMorphObjects(model, &num);
    if (morphs && index >= 0 && (nanoem_rsize_t) index < num) {
        return (int) nanoemModelMorphGetType(morphs[index]);
    }
    return 0;
}

NANOEM_DECL_API void APIENTRY
nanoemModelSetMorphWeight(nanoem_model_t *model, int index, nanoem_f32_t weight)
{
    nanoem_mark_unused(model);
    nanoem_mark_unused(index);
    nanoem_mark_unused(weight);
}

void
nanoemMotionReadMorphKeyframeBundleUnitNMD(nanoem_motion_t *motion, const Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message, nanoem_frame_index_t offset, nanoem_status_t *status)
{
    const Nanoem__Motion__MorphKeyframe *morph_keyframe_message;
    const Nanoem__Motion__Track *track_message;
    nanoem_unicode_string_factory_t *factory = motion->factory;
    nanoem_motion_morph_keyframe_t *morph_keyframe;
    nanoem_rsize_t num_morph_keyframes, num_local_tracks, i;
    nanoem_motion_track_t track;
    int ret;
    num_morph_keyframes = motion->num_morph_keyframes = morph_keyframe_bundle_message->n_keyframes;
    if (num_morph_keyframes > 0) {
        if (motion->morph_keyframes) {
            nanoem_free(motion->morph_keyframes);
        }
        motion->morph_keyframes = (nanoem_motion_morph_keyframe_t **) nanoem_calloc(num_morph_keyframes, sizeof(*motion->morph_keyframes), status);
        if (nanoem_is_not_null(motion->morph_keyframes)) {
            num_local_tracks = morph_keyframe_bundle_message->n_local_tracks;
            for (i = 0; i < num_local_tracks; i++) {
                track_message = morph_keyframe_bundle_message->local_tracks[i];
                track.name = nanoemUnicodeStringFactoryCreateStringWithEncoding(factory, (const nanoem_u8_t *) track_message->name, nanoem_crt_strlen(track_message->name), NANOEM_CODEC_TYPE_UTF8, status);
                if (!nanoem_status_ptr_has_error(status)) {
                    track.factory = factory;
                    track.id = (nanoem_motion_track_index_t) track_message->index;
                    track.keyframes = kh_init_keyframe_map();
                    kh_put_motion_track_bundle(motion->local_morph_motion_track_bundle, track, &ret);
                    if (ret >= 0 && motion->local_morph_motion_track_allocated_id < track.id) {
                        motion->local_morph_motion_track_allocated_id = track.id;
                    }
                    else if (ret < 0) {
                        motion->num_morph_keyframes = i;
                        break;
                    }
                }
            }
            for (i = 0; i < num_morph_keyframes; i++) {
                morph_keyframe_message = morph_keyframe_bundle_message->keyframes[i];
                morph_keyframe = motion->morph_keyframes[i] = nanoemMotionMorphKeyframeCreate(motion, status);
                if (nanoem_is_not_null(morph_keyframe)) {
                    nanoemMotionReadKeyframeCommonNMD(&morph_keyframe->base, morph_keyframe_message->common, offset, status);
                    if (nanoem_status_ptr_has_error(status)) {
                        motion->num_morph_keyframes = i;
                        break;
                    }
                    morph_keyframe->morph_id = (nanoem_motion_track_index_t) morph_keyframe_message->track_index;
                    morph_keyframe->weight = morph_keyframe_message->weight;
                    nanoemMotionTrackBundleAddKeyframe(motion->local_morph_motion_track_bundle,
                                                       (nanoem_motion_keyframe_object_t *) morph_keyframe,
                                                       morph_keyframe->base.frame_index,
                                                       nanoemMotionTrackBundleResolveName(motion->local_morph_motion_track_bundle, morph_keyframe->morph_id),
                                                       factory,
                                                       &ret);
                    if (ret < 0) {
                        motion->num_morph_keyframes = i;
                        break;
                    }
                }
                else {
                    motion->num_morph_keyframes = i;
                    break;
                }
            }
        }
    }
}

void
nanoemMutableMotionWriteMorphTrackBundleNMD(const nanoem_motion_t *motion, Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message, nanoem_status_t *status)
{
    Nanoem__Motion__Track **track_bundle_message, *track_message;
    const nanoem_motion_track_t *track;
    kh_motion_track_bundle_t *track_bundle = motion->local_morph_motion_track_bundle;
    nanoem_unicode_string_factory_t *factory = motion->factory;
    nanoem_rsize_t length, i;
    khiter_t it, end;
    char *bytes;
    morph_keyframe_bundle_message->n_local_tracks = kh_size(track_bundle);
    track_bundle_message = morph_keyframe_bundle_message->local_tracks = (Nanoem__Motion__Track **) nanoem_calloc(kh_size(track_bundle), sizeof(*track_bundle_message), status);
    if (nanoem_is_not_null(track_bundle_message)) {
        i = 0;
        for (it = 0, end = kh_end(track_bundle); it != end; it++) {
            if (kh_exist(track_bundle, it)) {
                track_message = track_bundle_message[i++] = (Nanoem__Motion__Track *) nanoem_calloc(1, sizeof(*track_message), status);
                if (nanoem_is_not_null(track_message)) {
                    nanoem__motion__track__init(track_message);
                    track = &kh_key(track_bundle, it);
                    bytes = (char *) nanoemUnicodeStringFactoryGetByteArrayEncoding(factory, track->name, &length, NANOEM_CODEC_TYPE_UTF8, status);
                    if (bytes) {
                        track_message->index = (uint64_t) track->id;
                        track_message->name = nanoemUtilCloneString(bytes ,status);
                        nanoemUnicodeStringFactoryDestroyByteArray(factory, (nanoem_u8_t *) bytes);
                    }
                    else {
                        return;
                    }
                }
                else {
                    return;
                }
            }
        }
        nanoem_status_ptr_assign_succeeded(status);
    }
}

void
nanoemMutableMotionWriteMorphKeyframeBundleUnitNMD(const nanoem_motion_t *motion, Nanoem__Motion__Motion *motion_message, nanoem_rsize_t *keyframe_bundle_index, nanoem_status_t *status)
{
    static const uint8_t fixed_interpolation_values[] = { 20, 20, 107, 107 };
    Nanoem__Motion__KeyframeBundleUnit *keyframe_unit_message;
    Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message;
    Nanoem__Motion__MorphKeyframe *morph_keyframe_message;
    Nanoem__Motion__MorphKeyframeInterpolation *interpolation_message;
    const nanoem_motion_morph_keyframe_t *morph_keyframe;
    nanoem_rsize_t num_morph_keyframes, i;
    if (motion->num_morph_keyframes > 0) {
        keyframe_unit_message = motion_message->keyframe_bundles[*keyframe_bundle_index] = (Nanoem__Motion__KeyframeBundleUnit *) nanoem_calloc(1, sizeof(*keyframe_unit_message), status);
        *keyframe_bundle_index += 1;
        if (nanoem_is_not_null(keyframe_unit_message)) {
            nanoem__motion__keyframe_bundle_unit__init(keyframe_unit_message);
            keyframe_unit_message->unit_case = NANOEM__MOTION__KEYFRAME_BUNDLE_UNIT__UNIT_MORPH;
            morph_keyframe_bundle_message = keyframe_unit_message->morph = (Nanoem__Motion__MorphKeyframeBundle *) nanoem_calloc(1, sizeof(*morph_keyframe_bundle_message), status);
            if (nanoem_is_not_null(morph_keyframe_bundle_message)) {
                nanoem__motion__morph_keyframe_bundle__init(morph_keyframe_bundle_message);
                nanoemMutableMotionWriteMorphTrackBundleNMD(motion, morph_keyframe_bundle_message, status);
                if (*status == NANOEM_STATUS_SUCCESS) {
                    num_morph_keyframes = morph_keyframe_bundle_message->n_keyframes = motion->num_morph_keyframes;
                    morph_keyframe_bundle_message->keyframes = (Nanoem__Motion__MorphKeyframe **) nanoem_calloc(num_morph_keyframes, sizeof(*morph_keyframe_bundle_message->keyframes), status);
                    for (i = 0; i < num_morph_keyframes; i++) {
                        morph_keyframe = motion->morph_keyframes[i];
                        morph_keyframe_message = morph_keyframe_bundle_message->keyframes[i] = (Nanoem__Motion__MorphKeyframe *) nanoem_calloc(1, sizeof(*morph_keyframe_message), status);
                        if (nanoem_is_not_null(morph_keyframe_message)) {
                            nanoem__motion__morph_keyframe__init(morph_keyframe_message);
                            nanoemMutableMotionCreateKeyframeCommonNMD(&morph_keyframe_message->common, &morph_keyframe->base, status);
                            if (nanoem_status_ptr_has_error(status)) {
                                num_morph_keyframes = morph_keyframe_bundle_message->n_keyframes = i;
                                return;
                            }
                            morph_keyframe_message->weight = morph_keyframe->weight;
                            morph_keyframe_message->track_index = (uint64_t) morph_keyframe->morph_id;
                            interpolation_message = morph_keyframe_message->interpolation = (Nanoem__Motion__MorphKeyframeInterpolation *) nanoem_calloc(1, sizeof(*interpolation_message), status);
                            nanoem__motion__morph_keyframe_interpolation__init(interpolation_message);
                            nanoemMutableMotionCreateInterpolationNMD(&interpolation_message->weight, fixed_interpolation_values, status);
                        }
                        else {
                            morph_keyframe_bundle_message->n_keyframes = i;
                            return;
                        }
                    }
                    nanoem_status_ptr_assign_succeeded(status);
                }
            }
        }
    }
}

void
nanoemMutableMotionDestroyMorphKeyframeBundleNMD(Nanoem__Motion__MorphKeyframeBundle *morph_keyframe_bundle_message)
{
    Nanoem__Motion__MorphKeyframe *keyframe;
    Nanoem__Motion__MorphKeyframeInterpolation *interpolation;
    size_t num_keyframes = morph_keyframe_bundle_message->n_keyframes, i;
    nanoemMutableMotionDestroyAnnotationsNMD(morph_keyframe_bundle_message->annotations, morph_keyframe_bundle_message->n_annotations);
    nanoemMutableMotionDestroyTrackBundleNMD(morph_keyframe_bundle_message->local_tracks, morph_keyframe_bundle_message->n_local_tracks);
    for (i = 0; i < num_keyframes; i++) {
        keyframe = morph_keyframe_bundle_message->keyframes[i];
        if (keyframe) {
            interpolation = keyframe->interpolation;
            nanoemMutableMotionDestroyKeyframeCommonNMD(keyframe->common);
            nanoemMutableMotionDestroyKeyframeInterpolationNMD(interpolation->weight);
            nanoem_free(interpolation);
            nanoem_free(keyframe);
        }
    }
    nanoem_free(morph_keyframe_bundle_message->keyframes);
    nanoem_free(morph_keyframe_bundle_message);
}
