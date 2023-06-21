#include <RunningAverage.h>
#include <PID_v1.h>
#include <BLEMIDI_Transport.h>
#include <hardware/BLEMIDI_ESP32.h>
#include "src/Slide.h"

BLEMIDI_CREATE_INSTANCE("Control Panel", MIDI)

static const int NUM_OF_SLIDERS = 4;

static const int slide1 = 36;
static const int touch1 = T6;
static const int in1A = 4;
static const int in1B = 2;
static const int pwm1 = 15;

static const int slide2 = 39;
static const int touch2 = T7;
static const int in2A = 17;
static const int in2B = 16;
static const int pwm2 = 5;

static const int slide3 = 35;
static const int touch3 = T8;
static const int in3A = 21;
static const int in3B = 19;
static const int pwm3 = 18;

static const int slide4 = 34;
static const int touch4 = T9;
static const int in4A = 23;
static const int in4B = 22;
static const int pwm4 = 25;

Slide* slides[NUM_OF_SLIDERS];
unsigned long _lastSent;

void setup() {
  Serial.begin(115200);

  slides[0] = new Slide(slide1, touch1, pwm1, in1A, in1B);
  slides[1] = new Slide(slide2, touch2, pwm2, in2A, in2B);
  slides[2] = new Slide(slide3, touch3, pwm3, in3A, in3B);
  slides[3] = new Slide(slide4, touch4, pwm4, in4A, in4B);

  MIDI.begin();

  BLEMIDI.setHandleConnected([]() {
    Serial.println("connected.");
  });

  BLEMIDI.setHandleDisconnected([]() {
    Serial.println("disconnected.");
  });

  MIDI.setHandleNoteOn([](byte channel, byte note, byte velocity) {});
  MIDI.setHandleNoteOff([](byte channel, byte note, byte velocity) {});

  MIDI.setHandleControlChange([](byte controlNumber, byte controlValue, byte channel) {
    Slide* slide = slides[controlNumber];
    if (slide) {
      Serial.printf("received command to go to %d on slider #%d.\n", map(controlValue, 0, 127, 0, 100), controlNumber);
      slide->goToTargetAsync(map(controlValue, 0, 127, 0, 100));
    }
  });
}

void processMIDI() {
  MIDI.read();
}

void processSerial() {
  if (Serial.available()) {
    String lines = Serial.readStringUntil('\n');
    lines.trim();

    int position = 0;
    char delimiter = '\n';
    do {
      position = lines.indexOf(delimiter);
      position = position == -1 ? lines.length() : position;
      String line = lines.substring(0, position);
      line.trim();

      if (line.startsWith("diag")) {
        for (int index = 0; index < sizeof(slides) / sizeof(*slides); index++) {
          Slide* slide = slides[index];
          if (slide) {
            Serial.printf("Slide #%d configured:\n", index + 1);
            Serial.printf("\tLocation: %f\n", slide->currentPosition());
            Serial.printf("\tTarget  : %d\n", slide->currentTarget());
            Serial.printf("\tTouched : %d\n", slide->isTouched());
            Serial.flush();
          }
        }
      } else if (line.equals("auto")) {
        resetSlides();

        slides[1]->goToTargetBlocking(100);
        slides[3]->goToTargetBlocking(100);

        for (int i = 0; i < 20; i++) {
          for (int j = 0; j < sizeof(slides) / sizeof(*slides); j++) {
            Slide* slide = slides[j];
            if (slide) {
              if (slide->currentTarget() == 100) {
                slide->goToTargetAsync(0);
              } else {
                slide->goToTargetAsync(100);
              }
            }
          }
          while (processSlides())
            ;
        }

        resetSlides();

        slides[0]->goToTargetBlocking(100);
        slides[1]->goToTargetBlocking(100);
        slides[2]->goToTargetBlocking(100);
        slides[3]->goToTargetBlocking(100);

        resetSlides();

        slides[0]->goToTargetBlocking(100);
        slides[1]->goToTargetBlocking(100);
        slides[2]->goToTargetBlocking(100);
        slides[3]->goToTargetBlocking(100);

        resetSlides();
      }

      int slideNumber = 0;
      switch (line.charAt(0)) {
        case '1':
          slideNumber = 0;
          line = line.substring(1);
          break;
        case '2':
          slideNumber = 1;
          line = line.substring(1);
          break;
        case '3':
          slideNumber = 2;
          line = line.substring(1);
          break;
        case '4':
          slideNumber = 3;
          line = line.substring(1);
          break;
        default: slideNumber = 0;
      }

      // Return the current level
      if (line.equals("GetState")) {
        Serial.print("L");
        Serial.println(slides[slideNumber]->currentPosition());
      }
      // Move to the target level asynchronously
      else if (line.startsWith("LA")) {
        slides[slideNumber]->goToTargetAsync(line.substring(2).toInt());
      }
      // Move to the target level
      else if (line.startsWith("L")) {
        slides[slideNumber]->goToTarget(line.substring(1).toInt());
      }

      // Move onto next line, if available
      if ((position = lines.indexOf(delimiter)) != -1) {
        lines = lines.substring(position + 1);
      }
    } while (position != -1);

    Serial.begin(115200);
  }
}

bool processSlides() {
  bool slideMoved = false;
  bool hasUpdate = false;
  for (int index = 0; index < sizeof(slides) / sizeof(*slides); index++) {
    Slide* slide = slides[index];
    if (slide) {
      if (slide->process()) {
        slideMoved = true;
      }

      if (slide->hasUpdate() && slide->isTouched()) {
        int position = slide->pushUpdate();
        hasUpdate = true;

        // Send MIDI event
        MIDI.sendControlChange(index, map(position, 0, 100, 0, 127), 1);
        Serial.printf("sending MIDI to channel #%d with value: %d", index, map(position, 0, 100, 0, 127));
      }
    }
  }

  return slideMoved;
}

void resetSlides() {
  for (int index = 0; index < sizeof(slides) / sizeof(*slides); index++) {
    Slide* slide = slides[index];
    if (slide) {
      slide->goToTargetBlocking(0);
    }
  }
}

void loop() {
  processMIDI();
  processSerial();
  processSlides();
}