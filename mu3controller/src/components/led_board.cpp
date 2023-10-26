#include "comio.hpp"
#include <FastLED.h>
#include "serial.hpp"

#ifdef LED_BOARD
namespace component
{
    namespace led_board
    {
        const int SIDElightL = A2;
        const int SIDElightR = A3;

        io_packet_t *out;

        io_packet_t *in;
        uint8_t in_size, checksum;

        int buffer_index = 0;
        int mode = 0;

        CRGB rightColors[6];
        CRGB leftColors[6];

        void set_color(uint8_t lr, uint8_t lg, uint8_t lb, uint8_t rr, uint8_t rg, uint8_t rb)
        {
            fill_solid(leftColors, 6, CRGB(lr, lg, lb));
            fill_solid(rightColors, 6, CRGB(rr, rg, rb));
        }

        void init_color()
        {
            set_color(91, 207, 250, 255, 97, 145);
        }

        void start()
        {
            serial::init();

            out = io_alloc(PACKET_TYPE_RESPONSE, 128);
            in = io_alloc(PACKET_TYPE_REQUEST, 128);

            FastLED.addLeds<WS2812B, SIDElightL, GRB>(leftColors, 6);
            FastLED.addLeds<WS2812B, SIDElightR, GRB>(rightColors, 6);

            init_color();
        }

        void parse_led_data(const uint8_t *data, int count)
        {
            uint8_t base = 1 + 59 * 3;
            fill_solid(rightColors, 6, CRGB(data[base], data[base + 1], data[base + 2]));

            uint8_t leftBase = 1;
            fill_solid(leftColors, 6, CRGB(data[leftBase], data[leftBase + 1], data[leftBase + 2]));
            FastLED.show();
        }

        void on_packet(io_packet_t *packet)
        {
            bool response = true;

            io_fill_data(out, packet->srcNodeId, packet->dstNodeId);
            out->response.status = ACK_OK;
            out->response.report = REPORT_OK;
            out->response.command = packet->request.command;

            switch (packet->request.command)
            {
            case CMD_RESET:
            {
                // uart_puts(uart1,"LED Board: Reset\n");

                fill_solid(rightColors, 6, CRGB(0, 0, 0));
                fill_solid(leftColors, 6, CRGB(0, 0, 0));
                FastLED.show();
                out->length = 0;
                break;
            }
            case CMD_SET_TIMEOUT:
            {
                auto timeout = packet->request.data[0] << 8 | packet->request.data[1];
                // uart_puts(uart1,"LED Board: Set Timeout: %d\n", timeout);
                out->length = io_build_timeout(out->response.data, 1024, timeout);
                break;
            }
            case CMD_SET_DISABLE:
            {
                // uart_puts(uart1,"LED Board: Disabled: %d\n", packet->request.data[0]);
                out->length = io_build_set_disable(out->response.data, 1024, packet->request.data[0]);
                break;
            }
            case CMD_SET_LED_DIRECT:
            {
                // uart_puts(uart1,"LED Board: Recv LED Data\n");
                parse_led_data(packet->response.data, packet->length - 3);
                response = false;
                break;
            }
            case CMD_BOARD_INFO:
            {
                // uart_puts(uart1,"LED Board: Report Board Information\n");
                out->length = io_build_board_info(out->response.data, 1024, "15093-06", "6710A", 0xA0);
                break;
            }
            case CMD_BOARD_STATUS:
            {
                // uart_puts(uart1,"LED Board: Report Board Status\n");
                out->length = io_build_board_status(out->response.data, 1024, 0, 0, 0);
                break;
            }
            case CMD_FIRM_SUM:
            {
                // uart_puts(uart1,"LED Board: Report Board Firmware Checksum\n");
                out->length = io_build_firmsum(out->response.data, 1024, 0xAA53);
                break;
            }
            case CMD_PROTOCOL_VERSION:
            {
                // uart_puts(uart1,"LED Board: Report Protocol Version\n");
                out->length = io_build_protocol_version(out->response.data, 1024, 1, 0);
                break;
            }
            default:
            {
                // uart_puts(uart1,"LED Board: Got Unknown Message\n");
                out->length = 0;
                out->response.status = ACK_INVALID;
                break;
            }
            }

            if (response)
            {
                io_apply_checksum(out);
                buffer_index = 0;
                mode = 1;
            }
        }

        void update()
        {
            int i = 0;

            for (i = 0; i < 18; i++)
            {
                if (mode == 1)
                {
                    if (buffer_index == 0)
                    {
                        serial::write_head();
                        buffer_index++;
                    }
                    else if (buffer_index < out->length + 5)
                    {
                        serial::write(out->buffer[buffer_index]);
                        buffer_index++;
                    }
                    else
                    {
                        serial::flush();
                        mode = 0;
                    }
                }
                else
                {
                    if (serial::available())
                    {
                        uint8_t byte;
                        bool is_escaped = serial::read(byte);

                        if (byte == 0xE0 && !is_escaped)
                        {
                            // uart_puts(uart1,"LED Board: Recv Sync\n");
                            in_size = 0;
                            checksum = 0;
                        }

                        in->buffer[in_size++] = byte;

                        // uart_puts(uart1,"LED Board: in_size %d, in->length %d, checksum %d, byte %d\n", in_size, in->length, checksum, byte);
                        if (in_size > 5 && in_size - 5 == in->length && checksum == byte)
                        {
                            // uart_puts(uart1,"LED Board: Recv %d bytes, checksum %d\n", in_size, checksum);
                            on_packet(in);
                        }

                        if (byte != 0xE0 || is_escaped)
                        {
                            checksum += byte;
                        }
                    }
                }
            }
        }

        void end()
        {
            Serial.end();
        }
    }
}
#endif