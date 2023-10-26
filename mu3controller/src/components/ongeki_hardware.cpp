#include "stdinclude.hpp"
#include <EEPROM.h>
#include <FastLED.h>
#include "components/card_reader.hpp"
#include "components/led_board.hpp"
#include <ResponsiveAnalogRead.h>

namespace component
{
    namespace ongeki_hardware
    {
        using namespace std;
        const int LEVER = PIN_A0;
        const int LED_PIN = PIN_A1;

        ResponsiveAnalogRead analog(LEVER, true, 0.1F);

        uint16_t lever_limit1 = 0;
        uint16_t lever_limit2 = 1023;
        uint16_t lever_limit_left;
        uint16_t lever_limit_right;

        CRGB lightColors[6];

        void start()
        {
            // setup led_t
            FastLED.addLeds<WS2812B, LED_PIN, RGB>(lightColors, 6);
            EEPROM.get(EEPROM_ADDR_LEVER_LIMIT_1, lever_limit1);
            EEPROM.get(EEPROM_ADDR_LEVER_LIMIT_2, lever_limit2);
        }

        void read_io(raw_hid::output_data_t *data)
        {
            for (int i = 0; i < 10; i++)
            {
                analog.update();
            }
            uint16_t lever = analog.getValue();

            // 设定摇杆范围
            if (manager::key_status[10] && manager::key_status[5])
            {
                lever_limit1 = lever;
                EEPROM.put(EEPROM_ADDR_LEVER_LIMIT_1, lever_limit1);
            }
            else if (manager::key_status[10] && manager::key_status[6])
            {
                lever_limit2 = lever;
                EEPROM.put(EEPROM_ADDR_LEVER_LIMIT_2, lever_limit2);
            }

            // lever_limit1大于lever_limit2 时，摇杆方向取反
            if (lever_limit1 < lever_limit2)
            {
                lever_limit_left = lever_limit1;
                lever_limit_right = lever_limit2;
            }
            else
            {
                lever = 1023 - lever;
                lever_limit_left = 1023 - lever_limit1;
                lever_limit_right = 1023 - lever_limit2;
            }

            // 禁止数值超出限制
            if (lever < lever_limit_left)
            {
                lever = lever_limit_left;
            }
            else if (lever > lever_limit_right)
            {
                lever = lever_limit_right;
            }

            // 摇杆数值映射到int16的范围
            data->lever = map(lever, lever_limit_left, lever_limit_right, -32768, 32767);

            // 设定opt按钮的状态
            uint8_t test_pressed = manager::key_status[10] && manager::key_status[0];
            uint8_t service_pressed = manager::key_status[10] && manager::key_status[1];
            uint8_t coin_pressed = manager::key_status[11];
            data->opt_buttons = (test_pressed << 0) | (service_pressed << 1) | (coin_pressed << 2);

            // 读取按钮状态
            for (auto i = 0; i < 10; i++)
            {
                data->buttons[i] = manager::key_status[i] && !manager::key_status[10];
            }
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
