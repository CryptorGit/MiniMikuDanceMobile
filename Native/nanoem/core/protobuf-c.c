/*
   Copyright (c) 2015-2023 hkrn All rights reserved

   This file is part of nanoem component and it's licensed under MIT license. see LICENSE.md for more details.
*/

#include "protobuf-c.h"
#include <stdlib.h>

size_t
protobuf_c_message_get_packed_size(const ProtobufCMessage *message)
{
    (void) message;
    return 0;
}

size_t
protobuf_c_message_pack(const ProtobufCMessage *message, uint8_t *out)
{
    (void) message;
    (void) out;
    return 0;
}

size_t
protobuf_c_message_pack_to_buffer(const ProtobufCMessage *message, ProtobufCBuffer *buffer)
{
    (void) message;
    (void) buffer;
    return 0;
}

ProtobufCMessage *
protobuf_c_message_unpack(const ProtobufCMessageDescriptor *descriptor, ProtobufCAllocator *allocator, size_t len, const uint8_t *data)
{
    (void) descriptor;
    (void) len;
    (void) data;
    if (!allocator || !allocator->alloc) {
        return NULL;
    }
    ProtobufCMessage *message = (ProtobufCMessage *) allocator->alloc(allocator->allocator_data, sizeof(ProtobufCMessage));
    if (message) {
        message->descriptor = descriptor;
    }
    return message;
}

void
protobuf_c_message_free_unpacked(ProtobufCMessage *message, ProtobufCAllocator *allocator)
{
    if (!message || !allocator || !allocator->free) {
        return;
    }
    allocator->free(allocator->allocator_data, message);
}

int
protobuf_c_message_check(const ProtobufCMessage *message)
{
    (void) message;
    return 1;
}
