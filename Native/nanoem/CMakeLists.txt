cmake_minimum_required(VERSION 3.15)
project(nanoem LANGUAGES C CXX)

add_library(nanoem_core
    core/nanoem.c
    core/version.c
)

target_include_directories(nanoem_core PUBLIC core)

add_library(nanoem_emapp
    emapp/UUID.cc
    emapp/BaseApplicationService.cc
    emapp/plugins/src/init.cc
    emapp/plugins/src/aviutl.cc
)

target_include_directories(nanoem_emapp PUBLIC
    .
    emapp
    emapp/plugins/src
    emapp/internal
)

