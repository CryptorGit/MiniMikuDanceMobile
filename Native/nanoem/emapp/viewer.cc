/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include <cstdint>

#include "emapp/Allocator.h"
#include "emapp/render/DirectionalLight.h"
#include "emapp/render/Grid.h"
#include "emapp/render/PerspectiveCamera.h"

using namespace nanoem;

namespace {
struct ViewerContext {
    int width;
    int height;
    uint64_t frameIndex;
    Allocator allocator;
    PerspectiveCamera *camera;
    DirectionalLight *light;
    Grid *grid;
    ViewerContext() noexcept
        : width(0)
        , height(0)
        , frameIndex(0)
        , allocator(nullptr)
        , camera(nullptr)
        , light(nullptr)
        , grid(nullptr)
    {
    }
};
static ViewerContext *g_context = nullptr;
} /* namespace anonymous */

extern "C" {

void
nanoemRenderingInitialize(int width, int height)
{
    if (!g_context) {
        g_context = new ViewerContext();
    }
    g_context->width = width;
    g_context->height = height;
    g_context->frameIndex = 0;
    /* TODO: initialize camera/light/grid based on emapp project */
}

void
nanoemRenderingUpdateFrame()
{
    if (g_context) {
        ++g_context->frameIndex;
        /* TODO: update emapp rendering pipeline */
    }
}

void
nanoemRenderingShutdown()
{
    delete g_context;
    g_context = nullptr;
}

} /* extern "C" */

