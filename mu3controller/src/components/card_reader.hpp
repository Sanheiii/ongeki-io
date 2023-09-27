#ifndef __CARD_READER_H__
#define __CARD_READER_H__

#include <PN532_HSU.h>
#include <PN532.h>
#include <stdio.h>

#define SerialDevice SerialUSB

#define NFC_STATE_NONE 0
#define NFC_STATE_AIME 1
#define NFC_STATE_FELICA 2
#define NFC_STATE_BANA 4
#define NFC_STATE_FAKE_AIME 8
#define NFC_STATE_FAKE_FELICA 16
#define NFC_STATE_OTHER 32
#define NFC_STATE_ERROR 64
#define NFC_STATE_NO_DEVICE 128

#define NFC_POLL_INTERVAL 2000

#define AIME_ALLOW 1
#define FELICA_ALLOW 1
#define BANA_ALLOW 1
#define MIFARE_FAKE_AIME 1
#define MIFARE_FAKE_FELICA 0

typedef union
{
    uint8_t luid[10];
    struct
    {
        uint8_t IDm[8];
        uint8_t PMm[8];
        union
        {
            uint16_t SystemCode;
            uint8_t System_Code[2];
        };
    };
} Card;

#define M2F_B 1

extern bool nfc_enable;
extern uint8_t card_type;
extern Card card;

extern void nfc_setup(void);
extern void nfc_poll(void);
extern void nfc_end(void);
extern void PrintHex(const uint8_t *data, const uint32_t numBytes);

#endif
