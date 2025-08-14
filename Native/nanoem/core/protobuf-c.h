/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/

#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
#define PROTOBUF_C__BEGIN_DECLS extern "C" {
#define PROTOBUF_C__END_DECLS }
#else
#define PROTOBUF_C__BEGIN_DECLS
#define PROTOBUF_C__END_DECLS
#endif

#define PROTOBUF_C_VERSION_NUMBER 1000000
#define PROTOBUF_C_MIN_COMPILER_VERSION 0
#define PROTOBUF_C__ENUM_DESCRIPTOR_MAGIC 0
#define PROTOBUF_C__MESSAGE_DESCRIPTOR_MAGIC 0
#define PROTOBUF_C__FIELD_DESCRIPTOR_MAGIC 0

#define PROTOBUF_C__FORCE_ENUM_TO_BE_INT_SIZE(enum_name)
#define PROTOBUF_C_MESSAGE_INIT(descriptor) { descriptor }

typedef int protobuf_c_boolean;

typedef enum {
    PROTOBUF_C_LABEL_REQUIRED = 0,
    PROTOBUF_C_LABEL_OPTIONAL = 1,
    PROTOBUF_C_LABEL_REPEATED = 2
} ProtobufCLabel;

typedef enum {
    PROTOBUF_C_TYPE_INT32 = 0,
    PROTOBUF_C_TYPE_SINT32 = 1,
    PROTOBUF_C_TYPE_SFIXED32 = 2,
    PROTOBUF_C_TYPE_INT64 = 3,
    PROTOBUF_C_TYPE_SINT64 = 4,
    PROTOBUF_C_TYPE_SFIXED64 = 5,
    PROTOBUF_C_TYPE_UINT32 = 6,
    PROTOBUF_C_TYPE_FIXED32 = 7,
    PROTOBUF_C_TYPE_UINT64 = 8,
    PROTOBUF_C_TYPE_FIXED64 = 9,
    PROTOBUF_C_TYPE_FLOAT = 10,
    PROTOBUF_C_TYPE_DOUBLE = 11,
    PROTOBUF_C_TYPE_BOOL = 12,
    PROTOBUF_C_TYPE_ENUM = 13,
    PROTOBUF_C_TYPE_STRING = 14,
    PROTOBUF_C_TYPE_BYTES = 15,
    PROTOBUF_C_TYPE_MESSAGE = 16
} ProtobufCType;

typedef struct ProtobufCEnumValue ProtobufCEnumValue;
typedef struct ProtobufCEnumValueIndex ProtobufCEnumValueIndex;
typedef struct ProtobufCEnumDescriptor ProtobufCEnumDescriptor;
typedef struct ProtobufCFieldDescriptor ProtobufCFieldDescriptor;
typedef struct ProtobufCMessage ProtobufCMessage;
typedef struct ProtobufCMessageDescriptor ProtobufCMessageDescriptor;
typedef struct ProtobufCBuffer ProtobufCBuffer;
typedef struct ProtobufCAllocator ProtobufCAllocator;

struct ProtobufCEnumValue {
    const char *name;
    unsigned value;
};

struct ProtobufCEnumValueIndex {
    const char *name;
    unsigned value;
};

struct ProtobufCEnumDescriptor {
    unsigned magic;
    const char *name;
    const char *short_name;
    const char *c_name;
    const char *package_name;
    unsigned n_values;
    const ProtobufCEnumValue *values;
    unsigned n_value_ranges;
    const void *value_ranges;
    const ProtobufCEnumValueIndex *values_by_name;
    const void *reserved1;
    const void *reserved2;
    const void *reserved3;
    const void *reserved4;
};

struct ProtobufCFieldDescriptor {
    unsigned magic;
    const char *name;
    unsigned id;
    ProtobufCLabel label;
    ProtobufCType type;
    unsigned flags;
    size_t offset;
    void *descriptor;
    size_t quantifier_offset;
    size_t element_size;
    void *default_value;
    const void *reserved1;
    const void *reserved2;
    const void *reserved3;
};

struct ProtobufCMessageDescriptor {
    unsigned magic;
    const char *name;
    const char *short_name;
    const char *c_name;
    const char *package_name;
    unsigned sizeof_message;
    unsigned n_fields;
    const ProtobufCFieldDescriptor *fields;
    unsigned n_field_ranges;
    const void *field_ranges;
    const void *message_init;
    const void *reserved1;
    const void *reserved2;
    const void *reserved3;
    const void *reserved4;
};

struct ProtobufCMessage {
    const ProtobufCMessageDescriptor *descriptor;
};

struct ProtobufCBuffer {
    void (*append)(ProtobufCBuffer *buffer, size_t len, const uint8_t *data);
};

struct ProtobufCAllocator {
    void *(*alloc)(void *allocator_data, size_t size);
    void (*free)(void *allocator_data, void *pointer);
    void *allocator_data;
};

static inline size_t
protobuf_c_message_get_packed_size(const ProtobufCMessage *message)
{
    (void) message;
    return 0;
}

static inline size_t
protobuf_c_message_pack(const ProtobufCMessage *message, uint8_t *out)
{
    (void) message;
    (void) out;
    return 0;
}

static inline size_t
protobuf_c_message_pack_to_buffer(const ProtobufCMessage *message, ProtobufCBuffer *buffer)
{
    (void) message;
    (void) buffer;
    return 0;
}

static inline ProtobufCMessage *
protobuf_c_message_unpack(const ProtobufCMessageDescriptor *descriptor, ProtobufCAllocator *allocator, size_t len, const uint8_t *data)
{
    (void) descriptor;
    (void) allocator;
    (void) len;
    (void) data;
    return NULL;
}

static inline void
protobuf_c_message_free_unpacked(ProtobufCMessage *message, ProtobufCAllocator *allocator)
{
    (void) message;
    (void) allocator;
}

static inline int
protobuf_c_message_check(const ProtobufCMessage *message)
{
    (void) message;
    return 0;
}

