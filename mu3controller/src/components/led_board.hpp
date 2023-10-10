#include "stdinclude.hpp"
#include "comio.hpp"

namespace component
{
    namespace led_board
    {
        void start();
        void init_color();
        void set_color(uint8_t lr, uint8_t lg, uint8_t lb, uint8_t rr, uint8_t rg, uint8_t rb);
        void update();
        void end();
    }
}