#include <WiFi.h>
#include <ArduinoJson.h>
#include <PubSubClient.h>
#include "src/Slide.h"

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

const char* ssid = "WIFI_HOST_HERE";
const char* pswd = "WIFI_PSWD_HERE";
const char* mqtt_host = "MQTT_IP_ADDRESS";
const char* mqtt_user = "MQTT_USER";
const char* mqtt_pswd = "MQTT_PSWD";
const int mqtt_port = 1883;

// Variables for MQTT Discovery
const char* deviceModel = "ESP32Device";
const char* deviceVersion = "1.0";
const char* manufacturer = "AlexPantea";
String deviceName = "ControlPanel";
String mqttStatus = "esp32iotsensor/" + deviceName;

WiFiClient wifiClient;
PubSubClient pubSubClient(wifiClient);
bool initSystem = true;
String uniqueId;
unsigned long _lastSent;

void setup() {
	Serial.begin(115200);

	setupWifi();
	pubSubClient.setServer(mqtt_host, mqtt_port);
	pubSubClient.setCallback(MqttReceiverCallback);

	slides[0] = new Slide(slide1, touch1, pwm1, in1A, in1B);
	slides[1] = new Slide(slide2, touch2, pwm2, in2A, in2B);
	slides[2] = new Slide(slide3, touch3, pwm3, in3A, in3B);
	slides[3] = new Slide(slide4, touch4, pwm4, in4A, in4B);

}

void setupWifi() {
	int counter = 0;
	byte mac[6]{};
	delay(10);

	// Attempt to connect to the wireless network
	WiFi.begin(ssid, pswd);

	// Use the esp's MAC address as a unique device ID for Home Assistant
	WiFi.macAddress(mac);
	uniqueId = String(mac[0], HEX) + String(mac[1], HEX) + String(mac[2], HEX) + String(mac[3], HEX) + String(mac[4], HEX) + String(mac[5], HEX);

	// Attempt to connect to WiFi a few times
	while (WiFi.status() != WL_CONNECTED && counter++ < 8) {
		delay(1000);
	}
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
			}
			else if (line.equals("auto")) {
				resetSlides();

				slides[1]->goToTargetBlocking(100);
				slides[3]->goToTargetBlocking(100);

				for (int i = 0; i < 20; i++) {
					for (int j = 0; j < sizeof(slides) / sizeof(*slides); j++) {
						Slide* slide = slides[j];
						if (slide) {
							if (slide->currentTarget() == 100) {
								slide->goToTargetAsync(0);
							}
							else {
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

void MqttReconnect() {
	int mqttConnectionCounter = 0;
	while (!pubSubClient.connected() && mqttConnectionCounter++ < 4) {
		pubSubClient.connect(deviceName.c_str(), mqtt_user, mqtt_pswd);
	}
	if (pubSubClient.connected()) {
		Serial.println("Subscribing to homeassistant/status..");
		Serial.println("Subscribing to " + mqttStatus);
		pubSubClient.subscribe("homeassistant/status");
		pubSubClient.subscribe((mqttStatus + "/in").c_str(), 1);
	}
	delay(100);
}

void MqttReceiverCallback(char* topic, byte* inFrame, unsigned int length) {
	String messageTopic = String(topic);
	String payload;
	for (int i = 0; i < length; i++) {
		payload += (char)inFrame[i];
	}

	if (messageTopic == "homeassistant/status") {
		MqttHomeAssistantDiscovery();
	}
	else if (messageTopic.startsWith(mqttStatus)) {
		StaticJsonDocument<200> doc;
		deserializeJson(doc, payload);
		for (int index = 0; index < sizeof(slides) / sizeof(*slides); index++) {
			Slide* slide = slides[index];
			if (slide) {
				if (doc.containsKey("slide" + String(index))) {
					int level = doc["slide" + String(index)];
					slide->getUpdate(level);
					if (!slide->isTouched()) {
						slide->goToTargetAsync(level);
					}
				}
			}
		}
	}
}

void MqttHomeAssistantDiscovery() {
	MqttReconnect();

	if (pubSubClient.connected()) {
		for (int index = 0; index < sizeof(slides) / sizeof(*slides); index++) {
			Slide* slide = slides[index];
			if (slide) {
				StaticJsonDocument<600> payload;
				JsonObject device;
				JsonArray identifiers;

				String discoveryTopic = "homeassistant/sensor/esp32iotsensor/" + deviceName + "_slide" + String(index) + "/config";
				String strPayload;

				payload["name"] = deviceName + ".slide" + String(index);
				payload["uniq_id"] = uniqueId + "_slide" + String(index);
				payload["stat_t"] = mqttStatus;
				payload["dev_cla"] = "none";
				payload["val_tpl"] = "{{ value_json.slide" + String(index) + " | is_defined }}";
				device = payload.createNestedObject("device");
				device["name"] = deviceName;
				device["model"] = deviceModel;
				device["sw_version"] = deviceVersion;
				device["manufacturer"] = manufacturer;
				identifiers = device.createNestedArray("identifiers");
				identifiers.add(uniqueId);

				serializeJson(payload, strPayload);

				pubSubClient.publish(discoveryTopic.c_str(), strPayload.c_str());
			}
		}
	}
}

void loop() {
	if (initSystem) {
		initSystem = false;
		MqttHomeAssistantDiscovery();
	}

	if (pubSubClient.connected()) {
		pubSubClient.loop();
	}

	//processMIDI();
	processSerial();
	processSlides();
}