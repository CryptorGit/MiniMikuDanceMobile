/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "emapp/Model.h"

#include "emapp/Project.h"
#include "nanoem/nanoem.h"

extern "C" {
void nanoemModelDestroy(nanoem_model_t *model);
}

namespace nanoem {

Model::Model(Project *project, nanoem_model_t *opaque) NANOEM_DECL_NOEXCEPT
    : m_project(project)
    , m_opaque(opaque)
{
}

Model::~Model() NANOEM_DECL_NOEXCEPT
{
    if (m_opaque) {
        nanoemModelDestroy(m_opaque);
        m_opaque = nullptr;
    }
}

void Model::update()
{
    /* 現状は特に処理なし */
}

void Model::render()
{
    /* 現状は特に処理なし */
}

} /* namespace nanoem */

