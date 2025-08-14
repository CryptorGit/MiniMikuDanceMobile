/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
*/

#pragma once
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

void nanoem_emapp_initialize_ik(void);
void nanoem_emapp_solve_ik(int32_t bone_index, const float position[3]);

#ifdef __cplusplus
}
#endif

