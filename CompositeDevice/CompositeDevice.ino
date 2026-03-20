#include <Arduino.h>
#include <ArduinoJson.h>
#include <ESP32Servo.h>

// ===== Config =====

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

struct Orientation {
  float pitch;
  bool  valid;
};

static char lineBuf[LINE_BUF_SIZE];
static size_t lineIndex = 0;

static Orientation targetOrientation = {0.0f, false};
static unsigned long lastPacketMillis = 0;

static Servo servoPitch;
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

bool parseOrientation(const char *line, Orientation &out) {
  StaticJsonDocument<256> doc;
  DeserializationError err = deserializeJson(doc, line);
  if (err) {
    return false;
  }

  float pitch = doc["pitch"] | 0.0f;
  pitch = clampf(pitch, -180.0f, 180.0f);

  out.pitch = pitch;
  out.valid = true;
  return true;
}

void handleCompleteLine(const char *line) {
  Orientation o = {0.0f, false};
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

void updateServo() {
  unsigned long now = millis();

  // timeout → go back to neutral
  if (lastPacketMillis > 0 && (now - lastPacketMillis) > SIGNAL_TIMEOUT_MS) {
    targetServoDeg = pitchCfg.neutralServoDeg;
  }

  if (now - lastServoUpdateMillis < SERVO_UPDATE_INTERVAL_MS) return;
  lastServoUpdateMillis = now;

  float diff = targetServoDeg - currentServoDeg;
  if (fabs(diff) <= MAX_SERVO_STEP_DEG) {
    currentServoDeg = targetServoDeg;
  } else {
    currentServoDeg += (diff > 0 ? MAX_SERVO_STEP_DEG : -MAX_SERVO_STEP_DEG);
  }

  float outDeg = clampf(currentServoDeg, 0.0f, 180.0f);
  servoPitch.write(outDeg);
}

// ===== Arduino lifecycle =====

void setup() {
  Serial.begin(SERIAL_BAUD);
  delay(500);

  Serial.println();
  Serial.println(F("PlaneGyro ESP32 receiver (PITCH ONLY)"));
  Serial.print(F("Serial baud: ")); Serial.println(SERIAL_BAUD);
  Serial.print(F("Pitch pin: ")); Serial.println(SERVO_PITCH_PIN);

  servoPitch.setPeriodHertz(50);
  servoPitch.attach(SERVO_PITCH_PIN, SERVO_MIN_US, SERVO_MAX_US);

  servoPitch.write(currentServoDeg);

  lastPacketMillis = 0;
  lastServoUpdateMillis = millis();
}

void loop() {
  pollSerial();
  updateServo();
}