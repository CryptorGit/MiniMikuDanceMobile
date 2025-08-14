target_sources(nanoem PRIVATE
    bone.c
    mutable.c
    transform.c
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
