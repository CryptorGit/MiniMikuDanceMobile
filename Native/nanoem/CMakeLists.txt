#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

cmake_minimum_required(VERSION 3.16)
project(nanoem_core C CXX)

find_package(Bullet REQUIRED)
find_package(glm REQUIRED)

add_library(nanoem_core
    core/allocator.c
    core/pmx/nanoem.c
    core/pmx/loader.c
    core/version.c
    core/bone/transform.c
    core/ext/converter.c
    core/ext/json.c
    core/ext/mutable.c
    core/ext/parson/parson.c
    core/ik/ConstraintSolver.cc
    core/ik/motion.c
    core/ik/motion.pb-c.c
    core/ext/motion.c
    core/morph/motion.c
    core/render/Rendering.cc
    emapp/DirectionalLight.cc
    emapp/Grid.cc
    emapp/PerspectiveCamera.cc
    emapp/CommonInclude.cc
    emapp/PhysicsEngine.cc
    emapp/physics.c
    emapp/ik.c
    core/physics/physics_bullet.cc
    core/import/document.c
    core/import/mutable.c
)

target_include_directories(nanoem_core PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/core
    ${CMAKE_CURRENT_SOURCE_DIR}/core/pmx
    ${CMAKE_CURRENT_SOURCE_DIR}/core/ik
    ${CMAKE_CURRENT_SOURCE_DIR}/core/morph
    ${BULLET_INCLUDE_DIRS}
)

target_link_libraries(nanoem_core PRIVATE
    ${BULLET_LIBRARIES}
    glm::glm
)

target_compile_definitions(nanoem_core PRIVATE
    NANOEM_ENABLE_MUTABLE
    NANOEM_ENABLE_DOCUMENT
    NANOEM_ENABLE_JSON
    NANOEM_ENABLE_NMD
    NANOEM_ENABLE_BULLET
)

