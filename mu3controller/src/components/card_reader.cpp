#include "card_reader.hpp"

PN532_HSU pn532hsu(Serial1);
PN532 nfc(pn532hsu);

enum nfc_state {
    START_READ,
    FELICA_POLLING_PREPARE,
    READ_PASSIVE_TARGET_ID_PREPARE,
    AUTH_ENTICATE_BLOCK_PREPARE,
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
    AUTH_ENTICATE_BLOCK_RESULT,
};

nfc_state state = nfc_state::RESET;
nfc_state state_next;
uint8_t command;
uint8_t polling_type;
uint8_t buffer[64];
uint8_t buffer_length;
uint8_t buffer_index;
uint8_t tmp[2];

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

void nfc_poll()
{
    if(state == START_READ)
    {
        return;
    }
    else if(state == READ_PASSIVE_TARGET_ID_PREPARE)
    {
        command = PN532_COMMAND_INLISTPASSIVETARGET;
        polling_type = 1;
        return;
    }
    else if(state == AUTH_ENTICATE_BLOCK_PREPARE)
    {
        command = PN532_COMMAND_INDATAEXCHANGE;
        polling_type = 1;
        return;
    }
    else if (state == FELICA_POLLING_PREPARE)
    {
        buffer[0] = 0;
        buffer[1] = 0;
        buffer[2] = 255;
        buffer[3] = 9;
        buffer[4] = 247;
        buffer[5] = 212;
        buffer[6] = 74;
        buffer[7] = 1;
        buffer[8] = 1;
        buffer[9] = 0;
        buffer[10] = 255;
        buffer[11] = 255;
        buffer[12] = 1;
        buffer[13] = 0;
        buffer[14] = 225;
        buffer[15] = 0;
        buffer_length = 16;
        state = DUMP_SERIAL_BUFFER;
        command = PN532_COMMAND_INLISTPASSIVETARGET;
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

        if(command == PN532_COMMAND_INLISTPASSIVETARGET)
        {
            state = READ_PASSIVE_TARGET_ID_RESULT;
        }
        else if(command == PN532_COMMAND_INDATAEXCHANGE)
        {
            state = AUTH_ENTICATE_BLOCK_RESULT;
        }
        else if(command == PN532_COMMAND_INLISTPASSIVETARGET)
        {
            state = FELICA_POLL_RESULT;
        }
        return;
    }
    else if (state == FELICA_POLL_RESULT)
    {
        if (buffer[0] == 0)
        {
            SerialDevice.println("No card had detected.");
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
        if(polling_type == card_type)
        {
            card_type == 0;
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
