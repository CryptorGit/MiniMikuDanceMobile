/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
 */

#include "nanoem.h"

const char *APIENTRY
nanoemGetVersionString(void)
{
    return "0.0.0";
}

int32_t APIENTRY
nanoemAdd(int32_t left, int32_t right)
{
    return left + right;
}
