cmake_minimum_required(VERSION 3.15)
project(nanoem_ios LANGUAGES C CXX)

set(CMAKE_SYSTEM_NAME iOS)
set(CMAKE_OSX_SYSROOT iphoneos)
set(CMAKE_OSX_ARCHITECTURES arm64)
set(CMAKE_OSX_DEPLOYMENT_TARGET 12.0)

file(GLOB CORE_SRC ../nanoem/core/*.c)
file(GLOB EMAPP_SRC ../nanoem/emapp/*.cc)
file(GLOB EXT_SRC ../nanoem/ext/*.c ../nanoem/ext/*.cc)

add_library(nanoem STATIC ${CORE_SRC} ${EMAPP_SRC} ${EXT_SRC})

target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/../nanoem/core
    ${CMAKE_CURRENT_SOURCE_DIR}/../nanoem/emapp
    ${CMAKE_CURRENT_SOURCE_DIR}/../nanoem/ext
)

target_compile_options(nanoem PRIVATE
    -fembed-bitcode
    -fno-exceptions
)

target_link_options(nanoem PRIVATE
    -fembed-bitcode
    -Wl,-dead_strip
)

target_link_libraries(nanoem
    "-framework Foundation"
    "-framework Metal"
    "-framework QuartzCore"
)
