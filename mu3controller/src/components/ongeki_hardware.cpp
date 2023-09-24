#include "stdinclude.hpp"
#include <EEPROM.h>
#include <FastLED.h>

namespace component
{
    namespace ongeki_hardware
    {
        const int LEVER = PIN_A0;
        const int LED_PIN = PIN_A1;

        CRGB lightColors[6];

        void start()
        {
            // setup led_t
            FastLED.addLeds<WS2812B, LED_PIN, RGB>(lightColors, 6);
        }

        void read_io(raw_hid::output_data_t *data)
        {
            for (auto i = 0; i < 10; i++)
            {
                data->buttons[i] = manager::key_status[i];
            }

            int16_t lever = analogRead(LEVER);
            data->lever = map(lever, 0, 1023, -32768, 32767);

            uint8_t test_pressed = manager::key_status[10] && manager::key_status[0];
            uint8_t service_pressed = manager::key_status[10] && manager::key_status[1];
            uint8_t coin_pressed = manager::key_status[11];
            data->opt_buttons = (test_pressed << 0) | (service_pressed << 1) | (coin_pressed << 2);
        }

        void set_led(raw_hid::led_t &data)
        {
            FastLED.setBrightness(data.ledBrightness);

            for (int i = 0; i < 3; i++)
            {
                memcpy(&lightColors[i], &data.ledColors[i], 3);
                memcpy(&lightColors[i + 3], &data.ledColors[i + 5], 3);
            }

            FastLED.show();
        }

        void end()
        {
            FastLED.clear();
            FastLED.show();
        }
    }
}
