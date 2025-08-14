#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

find_program(PROTOC_C_EXECUTABLE protoc-c)
if(PROTOC_C_EXECUTABLE)
    set(MOTION_PROTO ${CMAKE_CURRENT_SOURCE_DIR}/motion.proto)
    set(MOTION_PROTO_SRC ${CMAKE_CURRENT_SOURCE_DIR}/motion.pb-c.c)
    set(MOTION_PROTO_HDR ${CMAKE_CURRENT_SOURCE_DIR}/motion.pb-c.h)
    add_custom_command(
        OUTPUT ${MOTION_PROTO_SRC} ${MOTION_PROTO_HDR}
        COMMAND ${PROTOC_C_EXECUTABLE} --c_out=${CMAKE_CURRENT_SOURCE_DIR} --proto_path=${CMAKE_CURRENT_SOURCE_DIR} ${MOTION_PROTO}
        DEPENDS ${MOTION_PROTO}
        VERBATIM)
    set(NANOEM_CORE_PROTO_SRCS ${MOTION_PROTO_SRC} PARENT_SCOPE)
else()
    message(WARNING "protoc-c not found; using existing generated sources")
    set(NANOEM_CORE_PROTO_SRCS ${CMAKE_CURRENT_SOURCE_DIR}/motion.pb-c.c PARENT_SCOPE)
endif()
