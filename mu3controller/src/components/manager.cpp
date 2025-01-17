#include "stdinclude.hpp"
#include <EEPROM.h>
#include "led_board.hpp"

namespace component
{
    namespace manager
    {

        extern const uint8_t PIN_MAP[12] = {
            // L: A B C SIDE MENU
            9,
            8,
            7,
            16,
            15,
            // R: A B C SIDE MENU
            6,
            5,
            4,
            14,
            10,
            // FN1 FN2
            3,
            2,
        };
        bool key_status[12] = {0};

        bool running = false;
        bool keyboard_mode = false;
        bool switch_enabled = false;
        void start()
        {
            keyboard_mode = EEPROM.read(EEPROM_ADDR_KEYBOARD_MODE);
            // setup pin modes for button
            for (unsigned char i : manager::PIN_MAP)
            {
                pinMode(i, INPUT_PULLUP);
            }

            if (keyboard_mode)
            {
                keyboard::start();
            }
            else
            {
                raw_hid::start();
                ongeki_hardware::start();
                nfc_setup();
                #ifdef LED_BOARD
                led_board::start();
                #endif
            }
            running = true;
        }

        void update()
        {
            if (!running)
                return;

            // 按钮状态
            for (auto i = 0; i < 12; i++)
            {
                key_status[i] = digitalRead(PIN_MAP[i]) == LOW;
            }

            #ifdef HIGH_SIDE
            // 侧键
            key_status[3] = key_status[3] ^ 1;
            key_status[8] = key_status[8] ^ 1;
            #endif

            if (key_status[4] && key_status[9])
            {
                key_status[10] = true;
            }

            // 重置设置
            if (key_status[10] && key_status[11])
            {
                reset();
                return;
            }

            // 切换键盘与ongeki-io模式
            if (key_status[10] && key_status[7])
            {
                if (switch_enabled)
                {
                    switch_mode();
                    switch_enabled = false;
                }
                return;
            }
            else if (!switch_enabled)
            {
                switch_enabled = true;
            }

            if (keyboard_mode)
            {
                keyboard::update();
            }
            else
            {
                raw_hid::update();
                nfc_poll();
                #ifdef LED_BOARD
                led_board::update();
                #endif
            }
        }

        void end()
        {
            running = false;
            if (keyboard_mode)
            {
                keyboard::end();
            }
            else
            {
                raw_hid::end();
                nfc_end();
                ongeki_hardware::end();
                #ifdef LED_BOARD
                led_board::end();
                #endif
            }
        }

        void switch_mode()
        {
            if (!switch_enabled)
                return;
            end();
            EEPROM.put(EEPROM_ADDR_KEYBOARD_MODE, !keyboard_mode);
            start();
        }

        void reset()
        {
            end();
            EEPROM.put(EEPROM_ADDR_KEYBOARD_MODE, 0);
            EEPROM.put(EEPROM_ADDR_LEVER_LIMIT_1, 0);
            EEPROM.put(EEPROM_ADDR_LEVER_LIMIT_2, 1023);
            start();
        }
    }
}
