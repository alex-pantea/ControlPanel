#include <Arduino.h>
#include "Slide.h"

void Slide::goToTarget(int target) {
  int slideValue = round(currentPosition());
  _target = constrain(target, 0, 100);

  // If the distance from target somehow grows over 10, stop blocking execution
  while (abs(_target - slideValue) > 1 && abs(_target - slideValue) <= 10) {
    if (isTouched()) {
      _target = currentPosition();
      break;
    }
    setSpeed();  // No need for PID loop here since this function is blocking
    slideValue = currentPosition();
    if (_target < slideValue) {
      goBackwards();
    } else if (_target > slideValue) {
      goForwards();
    }
  }
}

void Slide::goToTargetBlocking(int target) {
  int slideValue = round(currentPosition());
  _target = constrain(target, 0, 100);

  while (abs(_target - slideValue) > 1) {
    if (isTouched()) {
      stopMotor();
      continue;
    }
    setSpeed(35);  // No need for PID loop here since this function is blocking
    slideValue = round(currentPosition());
    if (_target < slideValue) {
      goBackwards();
    } else if (_target > slideValue) {
      goForwards();
    }
  }
  stopMotor();
}

void Slide::goToTargetAsync(int target) {
  int slideValue = round(currentPosition());
  _target = constrain(target, 0, 100);

  if (isTouched()) {
    stopMotor();
    return;
  }

  if (_target != slideValue) {
    setSpeed(calculatePID());
    if (_target < slideValue) {
      goBackwards();
    } else if (_target > slideValue) {
      goForwards();
    }
  }
}

bool Slide::process() {
  bool moved = false;
  if (isTouched()) {
    _target = currentPosition();
    stopMotor();
  }

  if (_target == round(currentPosition())) {
    if (isMoving()) {
      stopMotor();
    }
  } else {
    moved = true;
    // Asynchronous movement can lead to overshooting and bouncing past the target,
    // so if we are close enough, use blocking movement to finish travelling.
    if (abs(currentPosition() - _target) < 5) {
      goToTarget(_target);
    } else {
      goToTargetAsync(_target);
    }
  }

  return moved;
}