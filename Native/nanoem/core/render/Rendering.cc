/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include <cstdint>

namespace {

static int g_width = 0;
static int g_height = 0;
static uint64_t g_frameIndex = 0;

} /* namespace anonymous */

extern "C" {

void
nanoemRenderingInitialize(int width, int height)
{
    g_width = width;
    g_height = height;
    g_frameIndex = 0;
}

void
nanoemRenderingUpdateFrame()
{
    ++g_frameIndex;
    (void) g_width;
    (void) g_height;
}

void
nanoemRenderingShutdown()
{
    g_width = g_height = 0;
    g_frameIndex = 0;
}

} /* extern "C" */

