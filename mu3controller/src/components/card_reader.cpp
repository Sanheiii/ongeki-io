#include "card_reader.hpp"

PN532_HSU pn532hsu(Serial1);
PN532 nfc(pn532hsu);

enum NfcState
{
    START_READ,
    FELICA_POLLING_PREPARE,
    READ_PASSIVE_TARGET_ID_PREPARE,
    AUTHENTICATE_BLOCK_PREPARE,
    READ_DATA_BLOCK_PREPARE,
    DUMP_SERIAL_BUFFER,
    WRITE,
    READ_ACK_FRAME,
    COMPARE_ACK_FRAME,
    READ,
    RESET,
    READ_RESPONSE,
    CHECK_PREAMBLE,
    READ_LENGTH,
    READ_COMMAND,
    READ_BODY,
    CHECK_BODY,
    FELICA_POLL_RESULT,
    READ_PASSIVE_TARGET_ID_RESULT,
    AUTHENTICATE_BLOCK_RESULT,
    READ_DATA_BLOCK_RESULT,
};

enum PollingCommand
{
    FELICA_POLLING,
    READ_PASSIVE_TARGET_ID,
    AUTHENTICATE_BLOCK,
    READ_DATA_BLOCK,
};

uint8_t AimeKey[6] = {0x57, 0x43, 0x43, 0x46, 0x76, 0x32};
uint8_t BanaKey[6] = {0x60, 0x90, 0xD0, 0x06, 0x32, 0xF5};
uint8_t MifareKey[6] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

NfcState state = NfcState::RESET;
NfcState state_next;
PollingCommand polling_command;
uint8_t command;
uint8_t polling_type;
uint8_t mifare_type;
uint8_t buffer[64];
uint8_t buffer_length;
uint8_t buffer_index;
uint8_t tmp[2];
uint8_t uid[4], uL;

uint8_t card_type; // 0 not detected, 1 felica, 2 mifare
Card card;

bool firmwareVersionChecked = false; // Declaration of global variable

void nfc_setup()
{

    nfc.begin();

    uint32_t firmwareVersion;
    if (!firmwareVersionChecked) // Only execute if the firmware version has not been checked
    {
        if (nfc.getFirmwareVersion())
        {
            firmwareVersion = nfc.getFirmwareVersion(); // Store the firmware version
            firmwareVersionChecked = true;              // Mark as firmware version checked
        }
    }

    // Continue with the subsequent configuration and operations
    nfc.setPassiveActivationRetries(0);
    nfc.SAMConfig();
}

void receive(uint8_t length)
{
    buffer_length = length;
    buffer_index = 0;
    state = READ;
}

void make_felica_polling_command(uint16_t systemCode, uint8_t requestCode, uint8_t *pn532_packetbuffer)
{
    pn532_packetbuffer[0] = PN532_COMMAND_INLISTPASSIVETARGET;
    pn532_packetbuffer[1] = 1;
    pn532_packetbuffer[2] = 1;
    pn532_packetbuffer[3] = FELICA_CMD_POLLING;
    pn532_packetbuffer[4] = (systemCode >> 8) & 0xFF;
    pn532_packetbuffer[5] = systemCode & 0xFF;
    pn532_packetbuffer[6] = requestCode;
    pn532_packetbuffer[7] = 0;
}

void make_read_passive_target_id_command(uint8_t cardbaudrate, uint8_t *pn532_packetbuffer)
{
    pn532_packetbuffer[0] = PN532_COMMAND_INLISTPASSIVETARGET;
    pn532_packetbuffer[1] = 1; // max 1 cards at once (we can set this to 2 later)
    pn532_packetbuffer[2] = cardbaudrate;
}

void make_authenticate_block_command(uint8_t *uid, uint8_t uidLen, uint32_t blockNumber, uint8_t keyNumber, uint8_t *keyData, uint8_t *pn532_packetbuffer)
{
    pn532_packetbuffer[0] = PN532_COMMAND_INDATAEXCHANGE; /* Data Exchange Header */
    pn532_packetbuffer[1] = 1;                            /* Max card numbers */
    pn532_packetbuffer[2] = (keyNumber) ? MIFARE_CMD_AUTH_B : MIFARE_CMD_AUTH_A;
    pn532_packetbuffer[3] = blockNumber; /* Block Number (1K = 0..63, 4K = 0..255 */
    memcpy(pn532_packetbuffer + 4, keyData, 6);
    for (uint8_t i = 0; i < uidLen; i++)
    {
        pn532_packetbuffer[10 + i] = uid[i]; /* 4 bytes card ID */
    }
}

void make_read_data_block_command(uint8_t blockNumber, uint8_t *pn532_packetbuffer)
{
    pn532_packetbuffer[0] = PN532_COMMAND_INDATAEXCHANGE;
    pn532_packetbuffer[1] = 1;               /* Card number */
    pn532_packetbuffer[2] = MIFARE_CMD_READ; /* Mifare Read command = 0x30 */
    pn532_packetbuffer[3] = blockNumber;     /* Block Number (0..63 for 1K, 0..255 for 4K) */
}

void append_to_buffer(const uint8_t tmp)
{
    buffer[buffer_index] = tmp;
    buffer_index++;
    buffer_length++;
}

void append_to_buffer(const uint8_t *tmp_buffer, uint8_t length)
{
    for (uint8_t i = 0; i < length; i++)
    {
        uint8_t tmp = tmp_buffer[i];
        append_to_buffer(tmp);
    }
}

void make_buffer(const uint8_t *header, uint8_t hlen, const uint8_t *body, uint8_t blen)
{
    buffer_index = 0;
    buffer_length = 0;
    append_to_buffer(PN532_PREAMBLE);
    append_to_buffer(PN532_STARTCODE1);
    append_to_buffer(PN532_STARTCODE2);

    uint8_t length = hlen + blen + 1; // length of data field: TFI + DATA
    append_to_buffer(length);
    append_to_buffer((uint8_t)(~length + 1)); // checksum of length

    append_to_buffer(PN532_HOSTTOPN532);
    uint8_t sum = PN532_HOSTTOPN532; // sum of TFI + DATA

    append_to_buffer(header, hlen);
    for (uint8_t i = 0; i < hlen; i++)
    {
        sum += header[i];
    }

    append_to_buffer(body, blen);
    for (uint8_t i = 0; i < blen; i++)
    {
        sum += body[i];
    }

    uint8_t checksum = ~sum + 1; // checksum of TFI + DATA
    append_to_buffer(checksum);
    append_to_buffer(PN532_POSTAMBLE);
}
void change_key()
{
    mifare_type++;
    if (mifare_type > 3)
        mifare_type = 0;
}
void nfc_poll()
{
    if (state == START_READ)
    {
        if (polling_type == 2)
        {
            mifare_type = 0;
            state = READ_PASSIVE_TARGET_ID_PREPARE;
        }
        else
        {
            state = FELICA_POLLING_PREPARE;
        }
        return;
    }
    else if (state == READ_PASSIVE_TARGET_ID_PREPARE)
    {
        uint8_t pn532_packetbuffer[3];
        make_read_passive_target_id_command(PN532_MIFARE_ISO14443A, pn532_packetbuffer);
        make_buffer(pn532_packetbuffer, 3, NULL, 0);
        command = pn532_packetbuffer[0];
        state = DUMP_SERIAL_BUFFER;
        polling_command = READ_PASSIVE_TARGET_ID;
        polling_type = 1;
        return;
    }
    else if (state == AUTHENTICATE_BLOCK_PREPARE)
    {
        uint8_t pn532_packetbuffer[10 + uL];
        // aime
        if (mifare_type == 0)
        {
            make_authenticate_block_command(uid, uL, 1, 1, AimeKey, pn532_packetbuffer);
        }
        // bana
        else if (mifare_type == 1)
        {
            make_authenticate_block_command(uid, uL, 1, 0, BanaKey, pn532_packetbuffer);
        }
        // other
        else if (mifare_type == 2)
        {
            make_authenticate_block_command(uid, uL, 1, 0, MifareKey, pn532_packetbuffer);
        }
        // fake id
        else
        {
            for (auto i = 0; i < 4; i++)
            {
                card.luid[i * 2 + 2] = (uid[i] / 16 / 8) * 16 + (uid[i] / 16 % 8);
                card.luid[i * 2 + 2 + 1] = (uid[i] % 16 / 8) * 16 + (uid[i] % 16 % 8);
            }
            card.luid[0] = 0;
            card.luid[1] = 0;
            card_type = 1;
            state = START_READ;
            return;
        }
        make_buffer(pn532_packetbuffer, 10 + uL, NULL, 0);
        command = pn532_packetbuffer[0];
        state = DUMP_SERIAL_BUFFER;
        polling_command = AUTHENTICATE_BLOCK;
        polling_type = 1;
        return;
    }
    else if (state == READ_DATA_BLOCK_PREPARE)
    {
        uint8_t pn532_packetbuffer[4];
        make_read_data_block_command(2, pn532_packetbuffer);
        make_buffer(pn532_packetbuffer, 4, NULL, 0);
        command = pn532_packetbuffer[0];
        state = DUMP_SERIAL_BUFFER;
        polling_command = READ_DATA_BLOCK;
        polling_type = 1;
        return;
    }
    else if (state == FELICA_POLLING_PREPARE)
    {
        uint8_t pn532_packetbuffer[8];
        make_felica_polling_command(0xFFFF, 0x01, pn532_packetbuffer);
        make_buffer(pn532_packetbuffer, 8, NULL, 0);
        command = pn532_packetbuffer[0];
        state = DUMP_SERIAL_BUFFER;
        polling_command = FELICA_POLLING;
        polling_type = 2;
        return;
    }
    else if (state == DUMP_SERIAL_BUFFER)
    {
        if (Serial1.available())
        {
            uint8_t ret = Serial1.read();
            return;
        }
        buffer_index = 0;
        state = WRITE;
        state_next = READ_ACK_FRAME;
        return;
    }
    else if (state == WRITE)
    {
        if (buffer_index < buffer_length)
        {
            Serial1.write(buffer[buffer_index]);
        }
        else
        {
            state = state_next;
            return;
        }
        buffer_index++;
        return;
    }
    else if (state == READ_ACK_FRAME)
    {
        receive(6);
        state_next = COMPARE_ACK_FRAME;
        return;
    }
    else if (state == READ)
    {
        if (buffer_index < buffer_length)
        {
            buffer[buffer_index] = Serial1.read();
            if (false)
            {
                state = RESET;
                return;
            }
        }
        else
        {
            state = state_next;
            return;
        }
        buffer_index++;
        return;
    }
    else if (state == COMPARE_ACK_FRAME)
    {
        const uint8_t PN532_ACK[] = {0, 0, 0xFF, 0, 0xFF, 0};
        if (memcmp(buffer, PN532_ACK, sizeof(PN532_ACK)))
        {
            state = RESET;
            return;
        }
        else
        {
            state = READ_RESPONSE;
        }
        return;
    }
    else if (state == READ_RESPONSE)
    {
        receive(3);
        state_next = CHECK_PREAMBLE;
        return;
    }
    else if (state == CHECK_PREAMBLE)
    {
        if (0 != buffer[0] || 0 != buffer[1] || (0xFF != buffer[2] && 0x0 != buffer[2]))
        {
            if (polling_command == AUTHENTICATE_BLOCK)
            {
                change_key();
                state = READ_PASSIVE_TARGET_ID_PREPARE;
                return;
            }
            state = RESET;
            return;
        }
        state = READ_LENGTH;
        return;
    }
    else if (state == READ_LENGTH)
    {
        receive(2);
        state_next = READ_COMMAND;
        return;
    }
    else if (state == READ_COMMAND)
    {
        // check length
        if (0 != (uint8_t)(buffer[0] + buffer[1]))
        {
            state = RESET;
            return;
        }
        buffer[0] -= 2;
        if (buffer[0] > 22)
        {
            state = RESET;
            return;
        }

        tmp[0] = buffer[0];

        receive(2);
        state_next = READ_BODY;
        return;
    }
    else if (state == READ_BODY)
    {
        uint8_t cmd = command + 1;
        if (PN532_PN532TOHOST != buffer[0] || cmd != buffer[1])
        {
            state = RESET;
            return;
        }
        receive(tmp[0]);
        state_next = CHECK_BODY;
        return;
    }

    else if (state == CHECK_BODY)
    {
        uint8_t cmd = command + 1;
        uint8_t sum = PN532_PN532TOHOST + cmd;
        for (uint8_t i = 0; i < tmp[0]; i++)
        {
            sum += buffer[i];
        }

        // read 2 bytes to tmp
        Serial1.readBytes(tmp, 2);

        if (0 != (uint8_t)(sum + tmp[0]) || 0 != tmp[1])
        {
            state = RESET;
            return;
        }

        if (polling_command == READ_PASSIVE_TARGET_ID)
        {
            state = READ_PASSIVE_TARGET_ID_RESULT;
        }
        else if (polling_command == AUTHENTICATE_BLOCK)
        {
            state = AUTHENTICATE_BLOCK_RESULT;
        }
        else if (polling_command == READ_DATA_BLOCK)
        {
            state = READ_DATA_BLOCK_RESULT;
        }
        else if (polling_command == FELICA_POLLING)
        {
            state = FELICA_POLL_RESULT;
        }
        return;
    }
    else if (state == READ_PASSIVE_TARGET_ID_RESULT)
    {
        if (buffer[0] != 1)
        {
            // Unhandled number of targets inlisted.
            state = RESET;
            return;
        }

        /* Card appears to be Mifare Classic */
        uL = buffer[5];

        for (uint8_t i = 0; i < buffer[5]; i++)
        {
            uid[i] = buffer[6 + i];
        }

        state = AUTHENTICATE_BLOCK_PREPARE;
        return;
    }
    else if (state == AUTHENTICATE_BLOCK_RESULT)
    {
        if (buffer[0] != 0x00)
        {
            // Authentication failed
            change_key();
            state = AUTHENTICATE_BLOCK_PREPARE;
            return;
        }
        state = READ_DATA_BLOCK_PREPARE;
        return;
    }
    else if (state == READ_DATA_BLOCK_RESULT)
    {
        /* If byte 8 isn't 0x00 we probably have an error */
        if (buffer[0] != 0x00)
        {
            state = RESET;
            return;
        }
        memcpy(card.luid, buffer + 7, 10);
        card_type = 1;
        state = START_READ;
        return;
    }

    else if (state == FELICA_POLL_RESULT)
    {
        if (buffer[0] == 0)
        {
            // No card had detected
            state = RESET;
            return;
        }
        else if (buffer[0] != 1)
        {
            // Unhandled number of targets inlisted. NbTg: {buffer[7]}
            state = RESET;
            return;
        }

        // length check
        uint8_t responseLength = buffer[2];
        if (responseLength != 18 && responseLength != 20)
        {
            // Wrong response length
            state = RESET;
            return;
        }
        // 成功
        uint8_t i;
        for (i = 0; i < 8; ++i)
        {
            card.IDm[i] = buffer[4 + i];
            card.PMm[i] = buffer[12 + i];
        }
        if (responseLength == 20)
        {
            card.System_Code[0] = buffer[20];
            card.System_Code[1] = buffer[21];
        }
        card_type = 2;

        state = START_READ;
        return;
    }
    else if (state == RESET)
    {
        if (polling_type == card_type)
        {
            card_type = 0;
        }
        state = START_READ;
        return;
    }
}

// void nfc_reset(void)
// {
//     for (auto i = 0; i < 10; i++)
//     {
//         Aime_Code[i] = 0;
//         if (i < 8)
//         {
//             Felica_ID[i] = 0;
//         }
//         nfc_stat = NFC_STATE_NONE;
//     }
// }

void nfc_end()
{
    Serial1.end();
}

void PrintHex(const uint8_t *data, const uint32_t numBytes)
{
    for (uint8_t i = 0; i < numBytes; i++)
    {
        if (data[i] < 0x10)
        {
            SerialDevice.print(" 0");
        }
        else
        {
            SerialDevice.print(' ');
        }
        SerialDevice.print(data[i], HEX);
    }
    SerialDevice.println("");
}
