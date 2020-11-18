/* Copyright 2015-2017 Jack Humbert
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#include QMK_KEYBOARD_H
#include "muse.h"
//functions&constants for random numbers
static unsigned long int next = 1234;
void randomNumberGenerator(void);
void sRandomNumberGenerator(unsigned int seed);
static unsigned int savedRandomNumber = 1;
static bool newRand = true;

enum preonic_keycodes {
  ENCRIPT = SAFE_RANGE,
};

const uint16_t PROGMEM keymaps[][MATRIX_ROWS][MATRIX_COLS] = {

/* Qwerty
 * ,-----------------------------------------------------------------------------------.
 * | ESC  |   1  |   2  |   3  |   4  |   5  |   6  |   7  |   8  |   9  |   0  | Bksp |
 * |------+------+------+------+------+------+------+------+------+------+------+------|
 * | Tab  |   Q  |   W  |   E  |   R  |   T  |   Y  |   U  |   I  |   O  |   P  | Del  |
 * |------+------+------+------+------+-------------+------+------+------+------+------|
 * |  `   |   A  |   S  |   D  |   F  |   G  |   H  |   J  |   K  |   L  |   ;  |Enter |
 * |------+------+------+------+------+------|------+------+------+------+------+------|
 * | Shift|   Z  |   X  |   C  |   V  |   B  |   N  |   M  |   ,  |   .  |   /  |  "   |
 * |------+------+------+------+------+------+------+------+------+------+------+------|
 * | Ctrl |Reset | Alt  | GUI  |Lower |    Space    |Raise | Left | Down |  Up  |Right |
 * `-----------------------------------------------------------------------------------'
 */
[0] = LAYOUT_preonic_grid(
  KC_ESC,  KC_1,    KC_2,    KC_3,    KC_4,    KC_5,    KC_6,    KC_7,    KC_8,    KC_9,    KC_0,    KC_BSPC,
  KC_TAB,  KC_Q,    KC_W,    KC_E,    KC_R,    KC_T,    KC_Y,    KC_U,    KC_I,    KC_O,    KC_P,    KC_DEL,
  KC_GRAVE, KC_A,    KC_S,    KC_D,    KC_F,    KC_G,    KC_H,    KC_J,    KC_K,    KC_L,    KC_SCLN, KC_ENT,
  KC_LSFT, KC_Z,    KC_X,    KC_C,    KC_V,    KC_B,    KC_N,    KC_M,    KC_COMM, KC_DOT,  KC_SLSH, KC_QUOT,
  KC_LCTL, RESET,   KC_LALT, KC_LGUI, _______, KC_SPC,  KC_SPC,  ENCRIPT, KC_LEFT, KC_DOWN, KC_UP,   KC_RGHT
)
};

bool process_record_user(uint16_t keycode, keyrecord_t *record) {
  static bool encript = false;
  switch (keycode) {
        case ENCRIPT: // toggle encription
          if (record->event.pressed) {
            if (encript) {
              encript = false;
            } else {
              encript = true;
            }
          }
          return false;
          break;
        case KC_A ... KC_Z:
            // re-seed rng if R key is pressed
          
          if (encript) { // encrypted
            keycode -= KC_A; // move keycodes to 0
            keycode = (keycode + 13+ savedRandomNumber()) % 26; // ROT 13 + saved andom number
            keycode += KC_A; // move keycodes back to 4
            if (record->event.pressed) { // send keypresses
              register_code(keycode);
            } else {
              unregister_code(keycode);
            }
            return false;
          } else { // not encripted
            return true;
          }
      }
  // if backspace is pressed and no keys are down then reseed the rng
  if (keycode == KC_BSPC&&newRand) {
      randomNumberGenerator();
  }
    return true;
};
// function for generating rng  that stores a value between 1 and 25
void randomNumberGenerator(void)
{
    next = next * 1103515245 + 12345;
    savedRandomNumber=(unsigned int)((next / 65536) % 25)+1;
}
// function for seeding rng
void sRandomNumberGenerator(unsigned int seed)
{
    next = seed;
}

/*
need to add function

if (key any pressed down)
    newRand=false;
if (a key is released)
    newRand=true
    
*/