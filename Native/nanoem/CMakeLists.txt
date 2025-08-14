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
    core/nanoem.c
    core/version.c
    core/ext/converter.c
    core/ext/document.c
    core/ext/json.c
    core/ext/mutable.c
    core/ext/motion.c
    core/ext/parson/parson.c
    core/physics/physics_bullet.cc
)

target_include_directories(nanoem_core PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/core
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

