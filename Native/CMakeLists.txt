cmake_minimum_required(VERSION 3.15)
project(nanoem LANGUAGES C CXX)

add_subdirectory(nanoem/core)

file(GLOB CORE_SRC nanoem/core/*.c)
file(GLOB EMAPP_SRC nanoem/emapp/*.cc)
file(GLOB EXT_SRC nanoem/ext/*.c nanoem/ext/*.cc)

add_library(nanoem SHARED ${CORE_SRC} ${EMAPP_SRC} ${EXT_SRC})

target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/core
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/emapp
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/ext
)
