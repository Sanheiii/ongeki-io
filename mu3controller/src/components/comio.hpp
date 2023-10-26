#include "stdinclude.hpp"

#ifdef LED_BOARD
enum io_packet_type_t : uint8_t
{
    PACKET_TYPE_REQUEST,
    PACKET_TYPE_RESPONSE,
};

enum io_ack_status_t : uint8_t
{
    ACK_OK = 1,
    ACK_SUM_ERROR,
    ACK_PARITY_ERROR,
    ACK_FARMING_ERROR,
    ACK_OVER_RUN_ERROR,
    ACK_RECV_BUFFER_OVERFLOW,
    ACK_INVALID = 255,
};

enum io_report_status_t : uint8_t
{
    REPORT_OK = 1,
    REPORT_BUSY,
    REPORT_UNKNOWN_COMMAND,
    REPORT_PARAM_ERROR,
    REPORT_INVALID = 255,
};

enum io_command_num_t : uint8_t
{
    CMD_RESET = 16,
    CMD_SET_TIMEOUT,

    CMD_SET_DISABLE = 20,

    CMD_SET_LED_DIRECT = 130,

    CMD_BOARD_INFO = 240,
    CMD_BOARD_STATUS,
    CMD_FIRM_SUM,
    CMD_PROTOCOL_VERSION,
};

struct io_reqeust_t
{
    io_command_num_t command;
    uint8_t data[1];
    // last byte: uint8_t checksum;
};

struct io_response_t
{
    io_ack_status_t status;
    io_command_num_t command;
    io_report_status_t report;
    uint8_t data[1];
    // last byte: uint8_t checksum;
};

union io_packet_t
{
    uint8_t buffer[1];
    struct {
        uint8_t sync;
        uint8_t dstNodeId;
        uint8_t srcNodeId;
        uint8_t length;
        union {
            io_reqeust_t request;
            io_response_t response;
        };
    };
};

inline io_packet_t* io_alloc(io_packet_type_t type, size_t size)
{
    size += 5;

    if (type == PACKET_TYPE_RESPONSE)
        size += 2;

    return (io_packet_t*)malloc(size);
}

inline void io_fill_data(io_packet_t* packet, uint8_t dstNodeId, uint8_t srcNodeId)
{
    packet->sync = 224;
    packet->dstNodeId = dstNodeId;
    packet->srcNodeId = srcNodeId;
}

inline size_t io_get_packet_size(io_packet_t* packet, io_packet_type_t type)
{
    return packet->length + 5;
}

inline void io_apply_checksum(io_packet_t* packet)
{
    packet->length += 3;
    auto totalLen = io_get_packet_size(packet, PACKET_TYPE_RESPONSE);

    uint8_t checksum = 0;
    checksum += packet->dstNodeId;
    checksum += packet->srcNodeId;
    checksum += packet->length;
    checksum += packet->response.status;
    checksum += packet->response.command;
    checksum += packet->response.report;

    for (auto i = 0; i < packet->length - 3; i++)
        checksum += packet->response.data[i];

    packet->request.data[totalLen - 6] = checksum;
}

inline size_t io_build_board_info(uint8_t* buffer, size_t bufferSize, const char* boardNo, const char* chipNo, uint8_t revision)
{
    size_t boardNoLen = strlen(boardNo);
    size_t chipNoLen = strlen(chipNo);
    size_t totalLen = strlen(boardNo) + strlen(chipNo) + 3;

    if (bufferSize <= totalLen)
        return -1;

    memcpy(buffer, boardNo, boardNoLen);
    memcpy(buffer + (boardNoLen + 1), chipNo, chipNoLen);

    buffer[boardNoLen] = 10;
    buffer[boardNoLen + chipNoLen + 1] = 255;
    buffer[boardNoLen + chipNoLen + 2] = revision;

    return totalLen;
}

inline size_t io_build_firmsum(uint8_t* buffer, size_t bufferSize, uint16_t sum)
{
    const size_t totalLen = 2;

    if (bufferSize <= totalLen)
        return -1;

    // big-endian
    uint8_t* buf = (uint8_t*)&sum;
    buffer[0] = buf[1];
    buffer[1] = buf[0];

    return totalLen;
}

inline size_t io_build_protocol_version(uint8_t* buffer, size_t bufferSize, uint8_t major, uint8_t minor)
{
    const size_t totalLen = 3;

    if (bufferSize <= totalLen)
        return -1;

    buffer[0] = 1;
    buffer[1] = major;
    buffer[2] = minor;

    return totalLen;
}

inline size_t io_build_timeout(uint8_t* buffer, size_t bufferSize, uint16_t timeout)
{
    const size_t totalLen = 2;

    if (bufferSize <= totalLen)
        return -1;

    // big-endian
    uint8_t* buf = (uint8_t*)&timeout;
    buffer[0] = buf[1];
    buffer[1] = buf[0];

    return totalLen;
}

inline size_t io_build_board_status(uint8_t* buffer, size_t bufferSize, uint8_t boardFlag, uint8_t uartFlag, uint8_t cmdFlag)
{
    const size_t totalLen = 3;

    if (bufferSize <= totalLen)
        return -1;

    buffer[0] = boardFlag;
    buffer[1] = uartFlag;
    buffer[2] = cmdFlag;

    return totalLen;
}

inline size_t io_build_set_disable(uint8_t* buffer, size_t bufferSize, uint8_t disable)
{
    const size_t totalLen = 1;

    if (bufferSize <= totalLen)
        return -1;

    buffer[0] = disable;

    return totalLen;
}
#endif