cmake_minimum_required(VERSION 3.16)
project(nanoem_native C CXX)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 17)

add_library(nanoem_native
    core/nanoem.c
    core/version.c
    ext/physics_bullet.cc
)

target_include_directories(nanoem_native
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/core
        ${CMAKE_CURRENT_SOURCE_DIR}/ext
)

find_package(Bullet REQUIRED)
find_package(glm REQUIRED)

target_compile_definitions(nanoem_native PRIVATE NANOEM_ENABLE_BULLET)
target_compile_options(nanoem_native PRIVATE -ffunction-sections -fdata-sections)
target_link_options(nanoem_native PRIVATE -Wl,--gc-sections)

# Bullet requires position independent code for shared usage
set_target_properties(nanoem_native PROPERTIES POSITION_INDEPENDENT_CODE ON)

target_link_libraries(nanoem_native PRIVATE ${BULLET_LIBRARIES})

target_include_directories(nanoem_native PRIVATE ${BULLET_INCLUDE_DIRS} ${GLM_INCLUDE_DIRS})
