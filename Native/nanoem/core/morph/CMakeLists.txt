target_sources(nanoem PRIVATE
    model.c
    motion.c
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
