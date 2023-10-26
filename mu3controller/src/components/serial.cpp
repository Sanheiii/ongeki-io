#define configUSE_CORE_AFFINITY 1

#include "stdinclude.hpp"

namespace component {
    namespace serial {
        void init() {
            Serial.begin(115200);
        }

        void write(uint8_t byte) {
            if(byte == 0xE0 || byte == 0xD0) {
                Serial.write((uint8_t)0xD0);
                Serial.write((uint8_t)(byte - 1));
            } else {
                Serial.write((uint8_t)byte);
            }
        }

        void write_head() {
            Serial.write((uint8_t)0xE0);
        }

        bool read(uint8_t &out) {
            auto byte = (uint8_t) Serial.read();

            if(byte == 0xD0) {
                out = (uint8_t)(Serial.read() + 1);
                return true;
            }

            out = byte;
            return false;
        }

        bool available() {
            auto avail = Serial.available();
            if(avail == 1) {
                uint8_t peek = Serial.peek();

                if(peek == 0xD0)
                    return false;

                return true;
            } else if(avail > 0) {
                return true;
            }

            return false;
        }

        void flush() {
            Serial.flush();
        }
    }
}