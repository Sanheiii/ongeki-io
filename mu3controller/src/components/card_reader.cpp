#include "card_reader.hpp"

PN532_HSU pn532hsu(Serial1);
PN532 nfc(pn532hsu);

enum nfc_state
{
    felica_prepare,
    felica_dump_serial_buffer,
    felica_write,
    felica_read_ack_frame,
    felica_compare_ack_frame,
    felica_read,
    felica_reset,
    felica_read_response,
    felica_check_preamble,
    felica_read_length,
    felica_read_command,
    felica_read_body,
    felica_check_body,
    felica_parse_data
};

nfc_state state = nfc_state::felica_reset;
nfc_state state_next;
;
uint8_t buffer[64];
uint8_t buffer_length;
uint8_t buffer_index;
uint8_t tmp[2];

uint8_t detected;
uint8_t idm[8];
uint8_t pmm[8];
uint16_t systemCode;

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
    state = felica_read;
}

void nfc_poll()
{
    if (state == felica_prepare)
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
        state = felica_dump_serial_buffer;
        return;
    }
    else if (state == felica_dump_serial_buffer)
    {
        if (Serial1.available())
        {
            uint8_t ret = Serial1.read();
            return;
        }
        buffer_index = 0;
        state = felica_write;
        state_next = felica_read_ack_frame;
        return;
    }
    else if (state == felica_write)
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
    else if (state == felica_read_ack_frame)
    {
        receive(6);
        state_next = felica_compare_ack_frame;
        return;
    }
    else if (state == felica_read)
    {
        if (buffer_index < buffer_length)
        {
            buffer[buffer_index] = Serial1.read();
            if (false)
            {
                state = felica_reset;
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
    else if (state == felica_compare_ack_frame)
    {
        const uint8_t PN532_ACK[] = {0, 0, 0xFF, 0, 0xFF, 0};
        if (memcmp(buffer, PN532_ACK, sizeof(PN532_ACK)))
        {
            state = felica_reset;
            return;
        }
        else
        {
            state = felica_read_response;
        }
        return;
    }
    else if (state == felica_read_response)
    {
        receive(3);
        state_next = felica_check_preamble;
        return;
    }
    else if (state == felica_check_preamble)
    {
        if (0 != buffer[0] || 0 != buffer[1] || (0xFF != buffer[2] && 0x0 != buffer[2]))
        {
            state = felica_reset;
            return;
        }
        state = felica_read_length;
        return;
    }
    else if (state == felica_read_length)
    {
        receive(2);
        state_next = felica_read_command;
        return;
    }
    else if (state == felica_read_command)
    {
        // check length
        if (0 != (uint8_t)(buffer[0] + buffer[1]))
        {
            state = felica_reset;
            return;
        }
        buffer[0] -= 2;
        if (buffer[0] > 22)
        {
            state = felica_reset;
            return;
        }

        tmp[0] = buffer[0];

        receive(2);
        state_next = felica_read_body;
        return;
    }
    else if (state == felica_read_body)
    {
        uint8_t cmd = PN532_COMMAND_INLISTPASSIVETARGET + 1;
        if (PN532_PN532TOHOST != buffer[0] || cmd != buffer[1])
        {
            state = felica_reset;
            return;
        }
        receive(tmp[0]);
        state_next = felica_check_body;
        return;
    }

    else if (state == felica_check_body)
    {
        uint8_t cmd = PN532_COMMAND_INLISTPASSIVETARGET + 1;
        uint8_t sum = PN532_PN532TOHOST + cmd;
        for (uint8_t i = 0; i < tmp[0]; i++)
        {
            sum += buffer[i];
        }

        // read 2 bytes to tmp
        Serial1.readBytes(tmp, 2);

        if (0 != (uint8_t)(sum + tmp[0]) || 0 != tmp[1])
        {
            state = felica_reset;
            return;
        }

        state = felica_parse_data;
        return;
    }
    else if (state == felica_parse_data)
    {
        if (buffer[0] == 0)
        {
            SerialDevice.println("No card had detected.");
            // No card had detected
            state = felica_reset;
            return;
        }
        else if (buffer[0] != 1)
        {
            // Unhandled number of targets inlisted. NbTg: {buffer[7]}
            state = felica_reset;
            return;
        }

        // length check
        uint8_t responseLength = buffer[2];
        if (responseLength != 18 && responseLength != 20)
        {
            // Wrong response length
            state = felica_reset;
            return;
        }

        // 成功
        uint8_t i;
        for (i = 0; i < 8; ++i)
        {
            idm[i] = buffer[4 + i];
            pmm[i] = buffer[12 + i];
        }
        if (responseLength == 20)
        {
            systemCode = (uint16_t)((buffer[20] << 8) + buffer[21]);
        }
        detected = true;

        //dump buffer
        for(uint8_t i = 0; i < 8; i++)
        {
           SerialDevice.print(idm[i], HEX);
           SerialDevice.print(" ");
        }
        SerialDevice.print("\n");

        state = felica_prepare;
        return;
    }
    else if (state == felica_reset)
    {
        detected = false;
        state = felica_prepare;
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
