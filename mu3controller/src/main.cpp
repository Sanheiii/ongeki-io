#include "stdinclude.hpp"

void setup()
{
    component::manager::start();
}

void loop()
{
    component::manager::update();
}
