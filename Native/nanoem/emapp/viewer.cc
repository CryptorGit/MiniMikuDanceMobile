/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include <cstdint>
#include <cmath>

#include "emapp/viewer.h"
#include "emapp/Allocator.h"
#include "emapp/Project.h"
#include "emapp/render/DirectionalLight.h"
#include "emapp/render/Grid.h"
#include "emapp/render/PerspectiveCamera.h"

#include "glm/gtc/constants.hpp"
#include "glm/gtc/type_ptr.hpp"

using namespace nanoem;

namespace {
struct ViewerContext {
    int width;
    int height;
    uint64_t frameIndex;
    PerspectiveCamera *camera;
    DirectionalLight *light;
    Grid *grid;
    Project *project;
    ViewerContext() noexcept
        : width(0)
        , height(0)
        , frameIndex(0)
        , camera(nullptr)
        , light(nullptr)
        , grid(nullptr)
        , project(nullptr)
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
    Allocator::initialize();
    g_context->project = new Project();
    g_context->camera = new PerspectiveCamera(g_context->project);
    g_context->light = new DirectionalLight(g_context->project);
    g_context->grid = new Grid(g_context->project);
    g_context->project->setCamera(g_context->camera);
    g_context->project->setDirectionalLight(g_context->light);
    g_context->project->setGrid(g_context->grid);
    g_context->grid->initialize();
}

void
nanoemRenderingUpdateFrame()
{
    if (g_context) {
        ++g_context->frameIndex;
        if (g_context->project) {
            g_context->project->update();
        }
    }
}

void
nanoemRenderingRenderFrame()
{
    if (g_context && g_context->project) {
        g_context->project->render();
    }
}

void
nanoemRenderingSetCamera(const nanoem_f32_t *position, const nanoem_f32_t *target)
{
    if (g_context && g_context->camera && position && target) {
        const Vector3 pos(glm::make_vec3(position));
        const Vector3 lookAt(glm::make_vec3(target));
        const Vector3 dir(lookAt - pos);
        const nanoem_f32_t distance = glm::length(dir);
        if (distance > 0) {
            const nanoem_f32_t kRadToDeg = nanoem_f32_t(180.0f / glm::pi<nanoem_f32_t>());
            const nanoem_f32_t yaw = nanoem_f32_t(std::atan2(dir.x, dir.z) * kRadToDeg);
            const nanoem_f32_t pitch =
                nanoem_f32_t(std::atan2(dir.y, std::sqrt(dir.x * dir.x + dir.z * dir.z)) * kRadToDeg);
            g_context->camera->setAngle(Vector3(-pitch, yaw, 0));
            g_context->camera->setDistance(distance);
        }
        g_context->camera->setLookAt(lookAt);
    }
}

void
nanoemRenderingSetLight(const nanoem_f32_t *direction)
{
    if (g_context && g_context->light && direction) {
        g_context->light->setDirection(glm::make_vec3(direction));
    }
}

void
nanoemRenderingSetGridVisible(int visible)
{
    if (g_context && g_context->grid) {
        g_context->grid->setVisible(visible != 0);
    }
}

void
nanoemRenderingSetStageSize(nanoem_f32_t value)
{
    if (g_context && g_context->grid) {
        g_context->grid->resize(Vector2(value));
    }
}

void
nanoemRenderingShutdown()
{
    if (g_context) {
        if (g_context->grid) {
            g_context->grid->destroy();
        }
        delete g_context->grid;
        delete g_context->light;
        delete g_context->camera;
        delete g_context->project;
        Allocator::destroy();
        delete g_context;
        g_context = nullptr;
    }
}

} /* extern "C" */

