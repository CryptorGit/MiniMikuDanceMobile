/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of emapp component and it's licensed under Mozilla Public License. see LICENSE.md for more details.
 */

#pragma once
#include <cstdint>
#include <string>

namespace nanoem {

class UUID {
public:
    template <typename TRng>
    static UUID create(TRng &rng) {
        UUID uuid;
        for (size_t i = 0; i < sizeof(uuid.m_value); ++i) {
            uuid.m_value[i] = static_cast<std::uint8_t>(rng.gen() % 16);
        }
        return uuid;
    }
    UUID();
    UUID(const UUID &uuid);
    ~UUID() noexcept;

    const std::uint8_t *bytes() const noexcept;
    std::string toString() const;

private:
    std::uint8_t m_value[31];
};

} // namespace nanoem
