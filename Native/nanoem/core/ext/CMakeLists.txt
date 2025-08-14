target_sources(nanoem PRIVATE
    cfstring.c
    converter.c
    document.c
    icu.c
    json.c
    mbwc.c
    motion.c
    motion.pb-c.c
    mutable.c
    parson/parson.c
)
target_include_directories(nanoem PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/parson
)
