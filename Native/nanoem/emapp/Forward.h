#pragma once
#include <cstdint>
#include <string>
#include <vector>

#if __cplusplus >= 201103L
#define NANOEM_DECL_SEALED final
#define NANOEM_DECL_NOEXCEPT_OVERRIDE noexcept override
#define NANOEM_DECL_OVERRIDE override
#define NANOEM_DECL_NOEXCEPT noexcept
#else
#define NANOEM_DECL_SEALED
#define NANOEM_DECL_NOEXCEPT_OVERRIDE
#define NANOEM_DECL_OVERRIDE
#define NANOEM_DECL_NOEXCEPT
#endif

namespace nanoem {

class NonCopyable {
    NonCopyable(const NonCopyable&) = delete;
    NonCopyable& operator=(const NonCopyable&) = delete;
protected:
    NonCopyable() = default;
    ~NonCopyable() NANOEM_DECL_NOEXCEPT = default;
};

class Error;
class ISeekableReader;
class ISeekableWriter;
class IReader;
class IWriter;

using String = std::string;
using ByteArray = std::vector<std::uint8_t>;

} // namespace nanoem

