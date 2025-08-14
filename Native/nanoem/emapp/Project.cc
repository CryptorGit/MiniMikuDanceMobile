/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "emapp/Project.h"
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
}

void Project::render()
{
    if (m_grid) {
        // Grid rendering requires a pass block. For now, this is a stub.
    }
}

} /* namespace nanoem */
