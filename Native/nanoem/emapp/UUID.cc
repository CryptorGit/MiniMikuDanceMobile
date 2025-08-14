/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#include "UUID.h"
#include <cstring>

namespace nanoem {
namespace {

static void fillBuffer(const std::uint8_t *p, int s, int e, char *&ptr) {
    for (int i = s; i < e; i++) {
        std::uint8_t v = p[i];
        if (v < 10) {
            *ptr++ = static_cast<char>(v + '0');
        }
        else if (v >= 10 && v < 16) {
            *ptr++ = static_cast<char>((v - 10) + 'a');
        }
    }
}

} // namespace

UUID::UUID() {
    std::memset(m_value, 0, sizeof(m_value));
}

UUID::UUID(const UUID &uuid) {
    std::memcpy(m_value, uuid.m_value, sizeof(m_value));
}

UUID::~UUID() noexcept = default;

const std::uint8_t *UUID::bytes() const noexcept {
    return m_value;
}

std::string UUID::toString() const {
    char buffer[37], *ptr = buffer;
    fillBuffer(m_value, 0, 8, ptr);
    *ptr++ = '-';
    fillBuffer(m_value, 8, 12, ptr);
    *ptr++ = '-';
    *ptr++ = static_cast<char>('4');
    fillBuffer(m_value, 12, 15, ptr);
    *ptr++ = '-';
    fillBuffer(m_value, 15, 19, ptr);
    *ptr++ = '-';
    fillBuffer(m_value, 19, 31, ptr);
    *ptr++ = 0;
    return std::string(buffer, sizeof(buffer) - 1);
}

} // namespace nanoem
