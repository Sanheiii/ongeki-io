#include "stdinclude.hpp"

void setup()
{
    Serial.begin(9600);
    component::manager::start();
}

void loop()
{
    component::manager::update();
}
