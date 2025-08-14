#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

cmake_minimum_required(VERSION 3.16)
project(nanoem_core C)

add_library(nanoem_core
    core/allocator.c
    core/nanoem.c
    core/version.c
    core/ext/converter.c
    core/ext/json.c
    core/ext/motion.c
    core/ext/parson/parson.c
    core/import/document.c
    core/import/mutable.c
)

target_include_directories(nanoem_core PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/core
)

target_compile_definitions(nanoem_core PRIVATE
    NANOEM_ENABLE_MUTABLE
    NANOEM_ENABLE_DOCUMENT
    NANOEM_ENABLE_JSON
    NANOEM_ENABLE_NMD
)

