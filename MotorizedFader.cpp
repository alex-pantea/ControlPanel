/* Author: Alex Pantea
** Notes:  This is strictly just for prototyping functionality-with a single slider, Arduino Pro Micro (Leo), and L298N motor driver.
*/

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

double aggKp = .7, aggKi = 0, aggKd = 0;
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

	int slideValue = getSlideValue();
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
	int slideValue = getSlideValue();

	checkCapacitiveSensor(10, 300, slideValue);

	if (slideValue != level && currTouch) {
		if (abs(slideValue - touchStart_level) > 2) {
			touchStart_level = slideValue;
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

	// Bugged currently, resetting hold count not functional after a slide movement
	// if (holdCount > 0 && holds != holdCount) {
	// 	Serial.print("H");
	// 	Serial.println(holdCount);
	// 	Serial.flush();
	//   holds = holdCount;
	// }

	// Process any incoming input
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

			// Return the current level
			if (line.equals("GetState")) {
				Serial.print("L");
				Serial.println(slideValue);
				Serial.flush();
			}
			// Move to the target level
			else if (line.startsWith("L") && !currTouch) {
				level = line.substring(1).toInt();
				goToTarget(level, 10);
			}
			// Mute, and move to the appropriate level
			else if (line.startsWith("M") && !currTouch) {
				muted = line.substring(1).toInt() == 1 ? true : false;
				goToTarget(level, 10);
			}

			// Move onto next line
			if ((pos = lines.indexOf(delim)) != -1) {
				lines = lines.substring(pos + 1);
			}
		} while (pos != -1);

		// Serial.begin flushes incoming/outgoing queues
		Serial.begin(115200);
	}
	delay(50);
}

void goToTarget(int target, int delayTime) {
	int slideValue = getSlideValue();

	// We need some special handling for the endpoints, to prevent excessive hammering into the metal frame
	if (target <= 10 && slideValue > 50) {
		goToTarget(12, 5);
		goToTarget(target, delayTime);
		return;
	}
	else if (target >= 90 && slideValue < 50) {
		goToTarget(88, 5);
		goToTarget(target, delayTime);
		return;
	}

	pid1F.Setpoint(muted ? 0 : target);
	// For moving backwards, we just invert the mapping for the forwards PID
	pid1B.Setpoint(map(pid1F.GetSetpoint(), 0, 100, 100, 0));

	// If we're already at the target, return out of the function immediately
	if (slideValue == pid1F.GetSetpoint()) {
		return;
	}

	do {
		// Use conservative PID settings if we're nearing the setpoint
		if (abs(pid1F.GetSetpoint() - slideValue) < 15) {
			pid1F.SetTunings(consKp, consKi, consKd);
			pid1B.SetTunings(consKp, consKi, consKd);
		}
		// Use more aggressive PID settings if we're far enough from the setpoint
		else {
			pid1F.SetTunings(aggKp, aggKi, aggKd);
			pid1B.SetTunings(aggKp, aggKi, aggKd);
		}

		double output = 0;
		// If the current value of the slider is greater than the target, run backwards
		if (slideValue > pid1F.GetSetpoint()) {
			output = pid1B.Run(map(slideValue, 0, 100, 100, 0));
		}
		// Otherwise, run forwards
		else {
			output = pid1F.Run(slideValue);
		}

		// set motor speed from pid output
		motor.setSpeed(output);

		// This logic mirrors the PID runs, but depending on wiring could need to be inverted
		if (slideValue > pid1F.GetSetpoint()) {
			motor.forward();
		}
		else {
			motor.backward();
		}

		slideValue = getSlideValue();
	} while (abs(slideValue - pid1F.GetSetpoint()) > 1 && sensor1.capacitiveSensor(10) < 300);

	motor.stop();
	// Delay to allow the slider and it's momentum to stop, before we check if we reached the setpoint
	delay(delayTime);

	slideValue = getSlideValue();

	// If the PID loop over or under shoots the target, just repeat goToTarget
	if (abs(pid1F.GetSetpoint() - slideValue) > 1 && delay > 0) {
		goToTarget(target, delayTime);
	}
}

void checkCapacitiveSensor(int samples, int threshold, int currentLevel) {
	currTouch = sensor1.capacitiveSensor(samples) > threshold;
	if (currTouch && !prevTouch) {
		delay(20);
		currTouch = sensor1.capacitiveSensor(samples) > threshold;
		if (currTouch) {
			touchStart = millis();
			holdCount = 0;
			touchStart_level = currentLevel;
			if (millis() - lastClicked > clickInterval) {
				clicks = 0;
				holds = 0;
				holdCount = 0;
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
		}
		else {
			clicks = 1;
		}
	}

	// Sensor not touched
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

int getSlideValue() {
	int slideValue = analogRead(slide1);
	slideValue = map(slideValue, 0, 1000, 0, 100);
	slideValue = map(slideValue, 3, 96, 100, 0);
	slideValue = constrain(slideValue, 0, 100);

	return slideValue;
}