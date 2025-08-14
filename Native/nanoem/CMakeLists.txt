#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

cmake_minimum_required(VERSION 3.16)
project(nanoem_core C)

add_library(nanoem_core
    core/nanoem.c
    core/version.c
)

target_include_directories(nanoem_core PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/core
)

# Enable debug memory allocator
target_compile_definitions(nanoem_core PUBLIC NANOEM_ENABLE_DEBUG_ALLOCATOR)

