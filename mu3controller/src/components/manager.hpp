#pragma once

namespace component {
    namespace manager {
        extern const uint8_t PIN_MAP[12];
        extern bool key_status[];
        void start();
        void update();
        void end();
        void switch_mode();
    }
}

#include "raw_hid.hpp"
#include "ongeki_hardware.hpp"
#include "keyboard.hpp"