cmake_minimum_required(VERSION 3.16)
project(nanoem_emapp CXX)

add_library(nanoem_emapp
    Archiver.cc
    IKSolver.cc
    ModelIO.cc
    UUID.cc
)

target_include_directories(nanoem_emapp PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/../core
)

target_link_libraries(nanoem_emapp PUBLIC nanoem_core)

