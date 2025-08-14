cmake_minimum_required(VERSION 3.15)
project(nanoem LANGUAGES C CXX)

find_package(Bullet REQUIRED)
find_package(glm REQUIRED)

add_library(nanoem SHARED)

file(GLOB_RECURSE EMAPP_SRC nanoem/emapp/*.cc nanoem/emapp/*.c)
target_sources(nanoem PRIVATE
    nanoem/core/allocator.c
    nanoem/core/nanoem.c
    nanoem/core/version.c
    ${EMAPP_SRC}
)

add_subdirectory(nanoem/core/bone)
add_subdirectory(nanoem/core/ext)
add_subdirectory(nanoem/core/ik)
add_subdirectory(nanoem/core/import)
add_subdirectory(nanoem/core/morph)
add_subdirectory(nanoem/core/physics)
add_subdirectory(nanoem/ext)

if (WIN32)
    set_target_properties(nanoem PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif ()
if (IOS)
    set(CMAKE_OSX_SYSROOT iphoneos)
    set(CMAKE_OSX_DEPLOYMENT_TARGET 12.0)
    set(CMAKE_OSX_ARCHITECTURES arm64)
    target_compile_options(nanoem PRIVATE -fembed-bitcode -fno-exceptions)
    target_link_options(nanoem PRIVATE -fembed-bitcode -Wl,-dead_strip)
    target_link_libraries(nanoem
        "-framework Foundation"
        "-framework Metal"
        "-framework QuartzCore"
    )
endif ()

target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/core
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/emapp
    ${CMAKE_CURRENT_SOURCE_DIR}/nanoem/ext
    ${BULLET_INCLUDE_DIRS}
)

target_link_libraries(nanoem PRIVATE
    ${BULLET_LIBRARIES}
    glm::glm
)

target_compile_definitions(nanoem PRIVATE
    NANOEM_ENABLE_MUTABLE
    NANOEM_ENABLE_DOCUMENT
    NANOEM_ENABLE_JSON
    NANOEM_ENABLE_NMD
    NANOEM_ENABLE_BULLET
)
