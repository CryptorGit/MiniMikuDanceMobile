target_sources(nanoem PRIVATE
    ConstraintSolver.cc
    motion.c
    motion.pb-c.c
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
