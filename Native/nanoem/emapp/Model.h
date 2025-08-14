/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#pragma once
#ifndef NANOEM_EMAPP_MODEL_H_
#define NANOEM_EMAPP_MODEL_H_

#include "emapp/Forward.h"

struct nanoem_model_t;

namespace nanoem {

class Project;

class Model NANOEM_DECL_SEALED : private NonCopyable {
public:
    Model(Project *project, nanoem_model_t *opaque) NANOEM_DECL_NOEXCEPT;
    ~Model() NANOEM_DECL_NOEXCEPT;

    void update();
    void render();
    nanoem_model_t *data() const NANOEM_DECL_NOEXCEPT { return m_opaque; }

private:
    Project *m_project;
    nanoem_model_t *m_opaque;
};

} /* namespace nanoem */

#endif /* NANOEM_EMAPP_MODEL_H_ */
