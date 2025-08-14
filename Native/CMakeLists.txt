cmake_minimum_required(VERSION 3.15)
project(nanoem LANGUAGES C CXX)

file(GLOB CORE_SRC nanoem/core/*.c)
file(GLOB EMAPP_SRC nanoem/emapp/*.cc)

add_library(nanoem SHARED ${CORE_SRC} ${EMAPP_SRC})

add_subdirectory(nanoem/ext)

if (WIN32)
    set_target_properties(nanoem PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif ()

target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/core
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/emapp
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/ext
)
