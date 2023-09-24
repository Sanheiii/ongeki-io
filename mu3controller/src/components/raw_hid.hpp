#pragma once

namespace component {
    namespace raw_hid {

#pragma pack(push, 1)
        struct aimi_id_t {
            uint8_t buffer[10];
        };

        struct output_data_t {
            union {
                char buffer[64];
                struct {
                    uint8_t buttons[10];
                    int16_t lever;
                    uint8_t scan;
                    aimi_id_t aimi_id;
                    uint8_t opt_buttons;
                };
            };
        };

        typedef uint8_t color_t[3];

        struct led_t {
            uint8_t ledBrightness;
            color_t ledColors[10];
        };

        struct option_t {
            aimi_id_t aimiId;
        };

        struct input_data_t {
            uint8_t type;
            union {
                char buffer[63];
                led_t led;
                option_t option;
            };
        };
#pragma pack(pop)

        void start();
        void update();
        void end();
    }
}
