/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "emapp/Project.h"
#include "emapp/Model.h"
#include "emapp/render/PerspectiveCamera.h"
#include "emapp/render/DirectionalLight.h"
#include "emapp/render/Grid.h"

namespace nanoem {

Project::Project()
    : m_camera(nullptr)
    , m_light(nullptr)
    , m_grid(nullptr)
{
}

Project::~Project() NANOEM_DECL_NOEXCEPT = default;

void Project::update()
{
    if (m_camera) {
        m_camera->update();
    }
    for (Model *model : m_models) {
        if (model) {
            model->update();
        }
    }
}

void Project::render()
{
    if (m_grid) {
        // Grid rendering requires a pass block. For now, this is a stub.
    }
    for (Model *model : m_models) {
        if (model) {
            model->render();
        }
    }
}

void Project::addModel(Model *model)
{
    if (model) {
        m_models.push_back(model);
    }
}

void Project::removeModel(Model *model)
{
    for (tinystl::vector<Model *, TinySTLAllocator>::iterator it = m_models.begin(); it != m_models.end(); ++it) {
        if (*it == model) {
            m_models.erase(it);
            break;
        }
    }
}

} /* namespace nanoem */
