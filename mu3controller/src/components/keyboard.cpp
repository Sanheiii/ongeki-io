#include "stdinclude.hpp"
namespace component
{
    namespace keyboard
    {
        const KeyboardKeycode KEY_MAP[12] = {
            // L: A B C SIDE MENU
            KeyboardKeycode::KEY_A,
            KeyboardKeycode::KEY_S,
            KeyboardKeycode::KEY_D,
            KeyboardKeycode::KEY_LEFT_SHIFT,
            KeyboardKeycode::KEY_ESC,
            // R: A B C SIDE MENU
            KeyboardKeycode::KEY_L,
            KeyboardKeycode::KEY_SEMICOLON,
            KeyboardKeycode::KEY_QUOTE,
            KeyboardKeycode::KEY_RIGHT_SHIFT,
            KeyboardKeycode::KEY_BACKSPACE,
            // FN1 FN2
            KeyboardKeycode::KEY_RESERVED,
            KeyboardKeycode::KEY_E,
        };


        void start()
        {
            Keyboard.begin();
            // 所有键盘按键松开/Releasekeyboard
            Keyboard.releaseAll();
        }

        void end()
        {
            Keyboard.releaseAll();
            Keyboard.end();
        }

        void update()
        {
            for (auto i = 0; i < 12; i++)
            {
                bool status = manager::key_status[i];
                if (status)
                {
                    Keyboard.press(KEY_MAP[i]);
                }
                else
                {
                    Keyboard.release(KEY_MAP[i]);
                }
            }
        }
    }
}