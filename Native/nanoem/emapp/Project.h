/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#pragma once
#ifndef NANOEM_EMAPP_PROJECT_H_
#define NANOEM_EMAPP_PROJECT_H_

#include "emapp/Forward.h"

namespace nanoem {

class PerspectiveCamera;
class DirectionalLight;
class Grid;
class Model;

class Project NANOEM_DECL_SEALED : private NonCopyable {
public:
    Project();
    ~Project() NANOEM_DECL_NOEXCEPT;

    void setCamera(PerspectiveCamera *camera) NANOEM_DECL_NOEXCEPT { m_camera = camera; }
    void setDirectionalLight(DirectionalLight *light) NANOEM_DECL_NOEXCEPT { m_light = light; }
    void setGrid(Grid *grid) NANOEM_DECL_NOEXCEPT { m_grid = grid; }

    void addModel(Model *model);
    void removeModel(Model *model);
    void update();
    void render();

    PerspectiveCamera *camera() const NANOEM_DECL_NOEXCEPT { return m_camera; }
    DirectionalLight *light() const NANOEM_DECL_NOEXCEPT { return m_light; }
    Grid *grid() const NANOEM_DECL_NOEXCEPT { return m_grid; }

private:
    PerspectiveCamera *m_camera;
    DirectionalLight *m_light;
    Grid *m_grid;
    tinystl::vector<Model *, TinySTLAllocator> m_models;
};

} /* namespace nanoem */

#endif /* NANOEM_EMAPP_PROJECT_H_ */
