#include "Archiver.h"

namespace nanoem {

struct Archiver::Opaque { };

Archiver::Entry::Entry()
    : m_crc(0),
      m_compressedSize(0),
      m_uncompressedSize(0),
      m_method(0),
      m_level(0),
      m_raw(0) {
}

String Archiver::Entry::basePath() const { return String(); }
String Archiver::Entry::lastPathComponent() const { return String(); }
const char *Archiver::Entry::filenamePtr() const NANOEM_DECL_NOEXCEPT { return m_path.c_str(); }
const char *Archiver::Entry::extensionPtr() const NANOEM_DECL_NOEXCEPT { return ""; }
bool Archiver::Entry::isDirectory() const NANOEM_DECL_NOEXCEPT { return false; }

Archiver::Archiver(ISeekableReader *reader)
    : m_opaque(nullptr) {
    (void) reader;
}

Archiver::Archiver(ISeekableWriter *writer)
    : m_opaque(nullptr) {
    (void) writer;
}

Archiver::~Archiver() NANOEM_DECL_NOEXCEPT = default;

bool Archiver::open(Error &) { return false; }
bool Archiver::close(Error &) { return false; }
bool Archiver::addEntry(const Entry &, const ByteArray &, Error &) { return false; }
bool Archiver::addEntry(const Entry &, IReader *, Error &) { return false; }
bool Archiver::findEntry(const String &, Entry &, Error &) const { return false; }
bool Archiver::extract(const Entry &, ByteArray &, Error &) const { return false; }
bool Archiver::extract(const Entry &, IWriter *, Error &) const { return false; }
Archiver::EntryList Archiver::allEntries(Error &) const { return EntryList(); }
Archiver::EntryList Archiver::entries(const String &, Error &) const { return EntryList(); }

} // namespace nanoem

