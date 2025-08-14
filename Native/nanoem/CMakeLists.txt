#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

cmake_minimum_required(VERSION 3.16)
project(nanoem_core C)

add_subdirectory(core)

add_library(nanoem_core
    core/nanoem.c
    core/version.c
    core/protobuf-c.c
    ${NANOEM_CORE_PROTO_SRCS}
)

target_include_directories(nanoem_core PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/core
)

