#include <PID_v2.h>
#include <L298N.h>
#include <CapacitiveSensor.h>

static const int slide1 = A0;
static const int enA = 9;
static const int inA1 = 8;
static const int inA2 = 7;

static const int SEND_PIN = 2;
static const int RECEIVE_PIN1 = 3;

CapacitiveSensor sensor1(SEND_PIN, RECEIVE_PIN1);
bool prevTouch, currTouch;
unsigned long touchStart, holdInterval, lastClicked, clickInterval;
int holds, holdCount, clicks, clickCount, touchStart_level;

L298N motor(enA, inA1, inA2);
int level = 0;
bool muted = false;

double aggKp = .5, aggKi = 0, aggKd = 0;
double consKp = .5, consKi = 0, consKd = 0;
PID_v2 pid1F(consKp, consKi, consKd, PID::Direct);
PID_v2 pid1B(consKp, consKi, consKd, PID::Direct);

void setup() {
  pinMode(slide1, INPUT);

  Serial.begin(115200);
  while (!Serial && millis() < 2000)
    ;

  sensor1.set_CS_Timeout_Millis(50);
  sensor1.set_CS_AutocaL_Millis(10000);

  prevTouch = false;
  currTouch = false;
  touchStart = 0;
  holdInterval = 750;
  holdCount = 0;
  touchStart_level = 0;

  motor.setSpeed(55);

  int slideValue = analogRead(slide1) - 5;
  slideValue = constrain(slideValue, 0, 1000);
  slideValue = map(slideValue, 0, 1000, 100, 0);
  pid1F.Start(slideValue, 0, 0);
  pid1F.SetOutputLimits(55, 255);
  pid1B.Start(slideValue, 0, 0);
  pid1B.SetOutputLimits(55, 255);

  level = slideValue;

  clicks = 0;
  clickCount = 0;
  clickInterval = 500;
}

void loop() {
  int slideValue = analogRead(slide1) - 5;
  slideValue = constrain(slideValue, 0, 1000);
  slideValue = map(slideValue, 0, 1000, 100, 0);

  checkCapacitiveSensor(10, 300, slideValue);

  // Serial.print("touching: ");
  // Serial.print(currTouch);
  // Serial.print(", prevLevel: ");
  // Serial.print(level);
  // Serial.print(", currLevel: ");
  // Serial.println(slideValue);
  if (slideValue != level && currTouch) {
    if (abs(slideValue - touchStart_level) > 2) {
      touchStart_level = 0;
      holdCount = 0;
      holds = 0;
      clickCount = 0;
      clicks = 0;
    }
    level = slideValue;
    Serial.print("LT");
    Serial.println(level);
    Serial.flush();
  }

  if (clickCount > 0) {
    Serial.print("T");
    Serial.println(clickCount);
    Serial.flush();
  }

  if (holdCount > 0 && holds != holdCount) {
    Serial.print("H");
    Serial.println(holdCount);
    Serial.flush();
  }

  if (Serial.available() > 0) {
    String lines = Serial.readStringUntil('\n');
    lines.trim();

    int pos = 0;
    char delim = '\n';
    do {
      pos = lines.indexOf(delim);
      pos = pos == -1 ? lines.length() : pos;
      String line = lines.substring(0, pos);
      line.trim();
      if (line.equals("GetState")) {
        Serial.print("L");
        Serial.println(slideValue);
        Serial.flush();
      } else if (line.startsWith("L") && !currTouch) {
        level = line.substring(1).toInt();
        pid1F.Setpoint(level);
        goToTarget(10);
      } else if (line.startsWith("M")) {
        muted = line.substring(1).toInt() == 1 ? true : false;
        pid1F.Setpoint(muted ? 0 : level);
        goToTarget(10);
      }
      if ((pos = lines.indexOf(delim)) != -1) {
        lines = lines.substring(pos + 1);
      }
    } while (pos != -1);
  }
  delay(50);
}

void goToTarget(int delayTime) {
  int slideValue = analogRead(slide1) - 5;
  slideValue = constrain(slideValue, 0, 1000);
  slideValue = map(slideValue, 0, 1000, 100, 0);
  pid1B.Setpoint(map(pid1F.GetSetpoint(), 0, 100, 100, 0));

  while (slideValue != pid1F.GetSetpoint()) {
    if (abs(pid1F.GetSetpoint() - slideValue) < 30) {
      pid1F.SetTunings(consKp, consKi, consKd);
      pid1B.SetTunings(consKp, consKi, consKd);
    } else {
      pid1F.SetTunings(aggKp, aggKi, aggKd);
      pid1B.SetTunings(aggKp, aggKi, aggKd);
    }

    double output = 0;
    if (pid1F.GetSetpoint() < slideValue) {
      output = pid1B.Run(map(slideValue, 0, 100, 100, 0));
    } else {
      output = pid1F.Run(slideValue);
    }

    // set motor speed from pid output
    motor.setSpeed(output);

    if (pid1F.GetSetpoint() < slideValue) {
      motor.forward();
    } else {
      motor.backward();
    }

    slideValue = analogRead(slide1) - 5;
    slideValue = constrain(slideValue, 0, 1000);
    slideValue = map(slideValue, 0, 1000, 100, 0);
  }
  motor.stop();
  delay(delayTime);

  slideValue = analogRead(slide1) - 5;
  slideValue = constrain(slideValue, 0, 1000);
  slideValue = map(slideValue, 0, 1000, 100, 0);

  if (pid1F.GetSetpoint() != slideValue) {
    goToTarget(delayTime);
  }
}

void checkCapacitiveSensor(int samples, int threshold, int currentLevel) {
  currTouch = sensor1.capacitiveSensor(samples) > threshold;
  // Sensor touch started
  // Serial.print(sensor1.capacitiveSensor(samples));
  // Serial.print(", prevTouch: ");
  // Serial.print(prevTouch);
  // Serial.print(", currTouch: ");
  // Serial.println(currTouch);
  if (currTouch && !prevTouch) {
    delay(20);
    currTouch = sensor1.capacitiveSensor(samples) > threshold;
    if (currTouch) {
      touchStart = millis();
      holdCount = 0;
      touchStart_level = currentLevel;
      if (millis() - lastClicked > clickInterval) {
        clicks = 0;
      }
      lastClicked = millis();
    }
  }

  // Sensor held
  if (currTouch && prevTouch) {
    holds = holdCount;
    if ((millis() - touchStart) > (holdInterval * (holdCount + 1))) {
      holdCount++;
    }
  }

  // Sensor released
  if (!currTouch && prevTouch) {
    delay(20);
    currTouch = sensor1.capacitiveSensor(samples) > threshold;


    if (millis() - lastClicked < clickInterval) {
      clicks++;
    } else {
      clicks = 1;
    }
  }

  if (!currTouch && !prevTouch) {
    if (millis() - lastClicked > clickInterval) {
      clickCount = clicks;
      clicks = 0;
      holdCount = 0;
      holds = 0;
    }
  }

  prevTouch = currTouch;
}