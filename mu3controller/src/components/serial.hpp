#pragma once
#include "stdinclude.hpp"
namespace component
{
    namespace serial
    {

        bool read(uint8_t &out);
        void write(uint8_t byte);
        void write_head();
        bool available();

        void flush();

        void init();
    }
}