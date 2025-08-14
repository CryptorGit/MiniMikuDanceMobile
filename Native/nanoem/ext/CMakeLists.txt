#
# Copyright (c) 2015-2023 hkrn All rights reserved
#
# This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
#

find_package(Bullet REQUIRED)
find_package(glm REQUIRED)

target_sources(nanoem PRIVATE
    physics_bullet.cc
)

target_include_directories(nanoem PRIVATE
    ${BULLET_INCLUDE_DIRS}
)

target_link_libraries(nanoem PRIVATE
    ${BULLET_LIBRARIES}
    glm::glm
)

target_compile_definitions(nanoem PRIVATE NANOEM_ENABLE_BULLET)
