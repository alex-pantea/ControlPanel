#include <RunningAverage.h>
#include <Arduino.h>
#include <PID_v1.h>
#ifndef SLIDE_H
#define SLIDE_H

static const int TOUCH_LIMIT = 20, AVERAGE_COUNT = 20;
static const int MIN_SPEED = 30, MAX_SPEED = 150;

static int slideCounter = 0;

static double aggKp = 1.0, aggKi = 0.0, aggKd = 0.0;
static double conKp = 0.2, conKi = 0.0, conKd = 0.0;

class Slide {
private:
  int _analogPin;
  int _touchPin;
  int _pwmPin;
  int _inAPin;
  int _inBPin;
  int _ledChannel;

  int _target;
  int _lastOutgoing;
  int _lastIncoming = -1;
  RunningAverage reported = RunningAverage(AVERAGE_COUNT);

  double _pidInput;
  double _pidOutput;
  double _pidSetpoint;

  PID pid = PID(&_pidInput, &_pidOutput, &_pidSetpoint, conKp, conKi, conKd, DIRECT);

  void goForwards() {
    digitalWrite(_inAPin, LOW);
    digitalWrite(_inBPin, HIGH);
  }
  void goBackwards() {
    digitalWrite(_inAPin, HIGH);
    digitalWrite(_inBPin, LOW);
  }
  void stopMotor() {
    setSpeed(0);
    digitalWrite(_inAPin, LOW);
    digitalWrite(_inBPin, LOW);
  }

  bool isMoving() {
    return digitalRead(_inAPin) || digitalRead(_inBPin);
  }

  double calculatePID() {
    _pidSetpoint = _target;
    _pidInput = currentPosition();

    // Use conservative PID settings if we're nearing the target
    if (abs(_target - currentPosition()) < 15) {
      pid.SetTunings(conKp, conKi, conKd);
    }
    // Use more aggressive PID settings if we're far enough from the setpoint
    else {
      pid.SetTunings(aggKp, aggKi, aggKd);
    }

    if (_target > currentPosition()) {
      pid.SetControllerDirection(DIRECT);
    } else {
      pid.SetControllerDirection(REVERSE);
    }

    pid.Compute();

    return _pidOutput;
  }
public:
  Slide(int analog, int touch, int pwm, int inA, int inB) {
    _analogPin = analog;
    _touchPin = touch;
    _pwmPin = pwm;
    _inAPin = inA;
    _inBPin = inB;
    _ledChannel = slideCounter++;

    pinMode(_analogPin, INPUT);
    pinMode(_touchPin, INPUT);

    pinMode(_inAPin, OUTPUT);
    pinMode(_inBPin, OUTPUT);
    pinMode(_pwmPin, OUTPUT);
    ledcSetup(_ledChannel, 1000, 8);
    ledcAttachPin(_pwmPin, _ledChannel);

    _target = currentPosition();

    _pidInput = currentPosition();
    _pidSetpoint = currentPosition();
    pid.SetMode(AUTOMATIC);

    reported.fillValue(currentPosition(), 5);
  }
  bool isTouched() {
    return touchRead(_touchPin) < TOUCH_LIMIT;
  }
  float currentPosition() {
    return analogRead(_analogPin) / 4095.0 * 100.0;
  }
  int currentTarget() {
    return _target;
  }
  void setSpeed(int speed = 0) {
    speed = constrain(speed, MIN_SPEED, MAX_SPEED);
    ledcWrite(_ledChannel, speed);
  }
  bool hasUpdate() {
    reported.addValue(currentPosition());
    return abs(_lastOutgoing - reported.getAverage()) > 0.625 || (_lastIncoming != -1 && _lastIncoming != _lastOutgoing);
  }
  int pushUpdate() {
    _lastOutgoing = (int)round(reported.getAverage());
    return _lastOutgoing;
  }
  void getUpdate(int incoming) {
    _lastIncoming = incoming;
  }


  void goToTarget(int target);
  void goToTargetBlocking(int target);
  void goToTargetAsync(int target);
  bool process();
};

#endif