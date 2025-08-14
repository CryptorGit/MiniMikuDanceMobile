target_sources(nanoem PRIVATE
    document.c
    mutable.c
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
