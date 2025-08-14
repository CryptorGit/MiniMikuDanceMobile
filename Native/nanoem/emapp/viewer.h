#pragma once
#ifndef NANOEM_EMAPP_VIEWER_H_
#define NANOEM_EMAPP_VIEWER_H_

#include "emapp/Forward.h"

#ifdef __cplusplus
extern "C" {
#endif

void nanoemRenderingInitialize(int width, int height);
void nanoemRenderingUpdateFrame(void);
void nanoemRenderingRenderFrame(void);
void nanoemRenderingSetCamera(const nanoem_f32_t *position, const nanoem_f32_t *target);
void nanoemRenderingSetLight(const nanoem_f32_t *direction);
void nanoemRenderingSetGridVisible(int visible);
void nanoemRenderingSetStageSize(nanoem_f32_t value);
void nanoemRenderingShutdown(void);

#ifdef __cplusplus
}
#endif

#endif /* NANOEM_EMAPP_VIEWER_H_ */
