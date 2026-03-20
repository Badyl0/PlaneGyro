#include <Arduino.h>
#include <ArduinoJson.h>
#include <TM1638plus.h>

// ===== Config =====
#define  STROBE_TM 16 // strobe = GPIO connected to strobe line of module
#define  CLOCK_TM 17  // clock = GPIO connected to clock line of module
#define  DIO_TM 18 // data = GPIO connected to data line of module
bool high_freq = false; //default false,, If using a high freq CPU > ~100 MHZ set to true.

//Constructor object (GPIO STB , GPIO CLOCK , GPIO DIO, use high freq MCU)
TM1638plus tm(STROBE_TM, CLOCK_TM , DIO_TM, high_freq);

static const unsigned long SERIAL_BAUD = 115200;

static const size_t LINE_BUF_SIZE = 256;

// One servo (pitch only)
static const int SERVO_PITCH_PIN = 14;
static const int SERVO_MIN_US = 500;
static const int SERVO_MAX_US = 2500;

// Map pitch degrees to servo
struct AxisConfig {
  float minDeg;
  float maxDeg;
  float neutralDeg;
  float neutralServoDeg;
};

static const AxisConfig PITCH_CFG = {
  -30.0f,  // min pitch deg
  +30.0f,  // max pitch deg
  0.0f,    // logical neutral
  90.0f    // servo neutral (center)
};

static const float MAX_SERVO_STEP_DEG = 3.0f;
static const unsigned long SERVO_UPDATE_INTERVAL_MS = 20;
static const unsigned long SIGNAL_TIMEOUT_MS = 2000;

// ===== Data =====

struct Plane {
  float pitch;
  float roll;
  float yaw;
  int flaps;
  bool gear;
  bool  valid;
};

static char lineBuf[LINE_BUF_SIZE];
static size_t lineIndex = 0;

static Plane targetOrientation = {0.0f, false};
static unsigned long lastPacketMillis = 0;
Plane cachedPlane = {0.0f, 0.0f, 0.0f, 0, false, false};


static AxisConfig pitchCfg = PITCH_CFG;
static float currentServoDeg = PITCH_CFG.neutralServoDeg;
static float targetServoDeg  = PITCH_CFG.neutralServoDeg;
static unsigned long lastServoUpdateMillis = 0;

// ===== Helpers =====

static float clampf(float v, float lo, float hi) {
  if (v < lo) return lo;
  if (v > hi) return hi;
  return v;
}

static float mapPitchToServo(const AxisConfig &cfg, float deg) {
  float clamped = clampf(deg, cfg.minDeg, cfg.maxDeg);
  float spanDeg = (cfg.maxDeg - cfg.minDeg);
  if (spanDeg <= 0.0f) return cfg.neutralServoDeg;

  float t = (clamped - cfg.minDeg) / spanDeg; // 0..1
  float servoDeg = t * 180.0f;
  return clampf(servoDeg, 0.0f, 180.0f);
}

bool parseOrientation(const char *line, Plane &out) {
  StaticJsonDocument<256> doc;
  DeserializationError err = deserializeJson(doc, line);
  if (err) {
    return false;
  }
  int flaps = doc["flaps"] | 0;
  bool gear = doc["gear"];

  out.pitch = 0.0f;
  out.roll = 0.0f;
  out.yaw = 0.0f;
  out.flaps = flaps;
  out.gear = gear;
  out.valid = true;
  return true;
}

void handleCompleteLine(const char *line) {
  Plane o = {0.0f, false};
  if (!parseOrientation(line, o)) {
    // Uncomment for debugging parse failures if needed:
    // Serial.print(F("Parse failed for line: "));
    // Serial.println(line);
    return;
  }

  Serial.print(F("Received line: "));
  Serial.println(line);
  Serial.print(F("Parsed pitch: "));
  Serial.println(o.pitch, 2);

  targetOrientation = o;
  lastPacketMillis = millis();
    switch(o.flaps){
      case 0:
        tm.setLED(0, 0);
        tm.setLED(1, 0);
        tm.setLED(2, 0);
        tm.setLED(3, 0);
        break;
      case 1:
        tm.setLED(0, 1);
        tm.setLED(1, 0);
        tm.setLED(2, 0);
        tm.setLED(3, 0);
        break;
      case 2:
        tm.setLED(0, 1);
        tm.setLED(1, 1);
        tm.setLED(2, 0);
        tm.setLED(3, 0);
        break;
      case 3:
        tm.setLED(0, 1);
        tm.setLED(1, 1);
        tm.setLED(2, 1);
        tm.setLED(3, 0);
        break;
    }
    tm.setLED(7, o.gear);

  targetServoDeg = mapPitchToServo(pitchCfg, o.pitch);
}

void pollSerial() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();

    if (c == '\r') continue;

    if (c == '\n') {
      if (lineIndex < LINE_BUF_SIZE) {
        lineBuf[lineIndex] = '\0';
        if (lineIndex > 0) {
          handleCompleteLine(lineBuf);
        }
      }
      lineIndex = 0;
      return;
    }

    if (lineIndex < LINE_BUF_SIZE - 1) {
      lineBuf[lineIndex++] = c;
    } else {
      // overflow: drop line
      lineIndex = 0;
    }
  }
}


// ===== Arduino lifecycle =====

void setup() {
  Serial.begin(SERIAL_BAUD);
  delay(500);
  tm.displayBegin();
  tm.reset();

  Serial.println();
  Serial.println(F("PlaneGyro ESP32 receiver (PITCH ONLY)"));
  Serial.print(F("Serial baud: ")); Serial.println(SERIAL_BAUD);
  Serial.print(F("Pitch pin: ")); Serial.println(SERVO_PITCH_PIN);

  lastPacketMillis = 0;
  lastServoUpdateMillis = millis();
}

void loop() {
  pollSerial();
}