target_sources(nanoem PRIVATE
    physics_bullet.cc
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
