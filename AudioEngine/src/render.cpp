#include <Bela.h>                           // Bela API, let's us read analog inputs, etc.
#include <cmath>                          
#include <algorithm>
#include <libraries/OscSender/OscSender.h>  // Bela's built in OSC sender library
#include <atomic>
#include <unistd.h>

// ========== SSH RUN COMMAND ==========
// ssh -tt root@bela.local "cd /root/Bela && make PROJECT=IT4 run"


// ========== Parameters to tune ==========
static const int   PIEZO_CH = 0;          // analog in channel (A0), the physical pin on Bela
static const float DC_R = 0.99f;          // DC offset filter coefficient, closer to 1 = slower
static const float THRESH = 0.001f;        // minimum hit level after DC removal and baseline
static const float PEAK_WIN_MS = 8.0f;    // after a hit, watch signal for this long to find peak and measure velocity
static const float REFRACT_MS = 25.0f;    // lockout to avoid double hits
static const float VEL_GAIN = 6.0f;       // peak * gain -> velocity; scale to get good 0..1 range
static const float BASE_ALPHA = 0.999f;   // noise floor; closer to 1 = slower baseline

// ===== OSC target (Unity machine) =====
static const char* TARGET_IP = "192.168.7.1";
static const int TARGET_PORT = 7000;

// ========== Bela OSC sender ==========
OscSender gOscSender;
AuxiliaryTask gOscTask = nullptr;

// ========== one detected drum hit ==========
struct HitEvent
{
    int32_t tHigh;
    int32_t tLow;
    int32_t pad;
    int32_t vel;
};
// ========== HitEvent queue for communicating between Bela's audio thread and OSC thread ==========
static const unsigned int QUEUE_SIZE = 128;

static HitEvent gQueue[QUEUE_SIZE];

static std::atomic<unsigned int> gWriteIndex{0};
static std::atomic<unsigned int> gReadIndex{0};

// Add hit event to queue
bool enqueueHit(const HitEvent& ev)
{
    unsigned int w = gWriteIndex.load();
    unsigned int r = gReadIndex.load();

    unsigned int next = (w + 1) % QUEUE_SIZE;

    if(next == r)
        return false; // queue full

    gQueue[w] = ev;

    gWriteIndex.store(next);

    return true;
}

// Get hit event from queue
bool dequeueHit(HitEvent& ev)
{
    unsigned int r = gReadIndex.load();
    unsigned int w = gWriteIndex.load();

    if(r == w)
        return false; // empty

    ev = gQueue[r];

    gReadIndex.store((r + 1) % QUEUE_SIZE);

    return true;
}

// ========== OSC SENDER THREAD ==========
void oscSenderLoop(void*)
{
    while(!Bela_stopRequested())
    {
        HitEvent ev;

        while(dequeueHit(ev))
        {
            gOscSender.newMessage("/it4/hit")
              .add((int)ev.tHigh)
              .add((int)ev.tLow)
              .add((int)ev.pad)
              .add((int)ev.vel)
              .send();
        }

        usleep(1000);
    }
} // checks queue, sends OSC messages, sleeps for a bit, repeat

// ========== STATE ==========
static float gAnalogRate = 0.0f;          // Bela's analog sample rate
static int   gAudioPerAnalog = 0;         // how many audio frames per analog frame because audio & analog run at different rates

static float gLastAnalog = 0.0f;          // last raw analog reading for DC removal/filtering
static float gPrevDC = 0.0f;              // previous DC output sample for DC removal/filtering

static float gBaseline = 0.0f;            // noise floor tracking 

static bool  gPeaking = false;            // are we currently tracking a peak after threshold crossing?
static float gPeak = 0.0f;                // highest signal during hit window
static int   gPeakCount = 0;              // how many samples into the peak window are we?

static int   gRefractCount = 0;           // lockout counter after a hit
static int   gPeakWinSamples = 0;         // number of samples in peak window
static int   gRefractSamples = 0;         // number of samples in refractory period

static inline float clamp01(float x) { return std::min(1.0f, std::max(0.0f, x)); }  // function to clamp values between 0 and 1

// ========== BELA CONFIG ==========
bool setup(BelaContext *context, void *userData)
{
  if(context->analogInChannels < 1) {
    rt_fprintf(stderr, "Need at least 1 analog input channel\n");
    return false;
  }

  // calculating audio frames -> analog frames
  gAnalogRate = context->analogSampleRate;
  gAudioPerAnalog = context->analogFrames ? (context->audioFrames / context->analogFrames) : 0;


  if(gAudioPerAnalog <= 0) gAudioPerAnalog = 1;

  // calculating sample counts from ms times
  gPeakWinSamples   = (int)(gAnalogRate * (PEAK_WIN_MS / 1000.0f));
  gRefractSamples   = (int)(gAnalogRate * (REFRACT_MS / 1000.0f));

  if(gPeakWinSamples < 1) gPeakWinSamples = 1;

  rt_printf("Piezo trigger ready: analogRate=%.1fHz peakWin=%d samples refract=%d samples\n",
            gAnalogRate, gPeakWinSamples, gRefractSamples);
  rt_printf("Tune: THRESH=%.4f DC_R=%.3f VEL_GAIN=%.2f\n", THRESH, DC_R, VEL_GAIN);

  // Setup OSC sender
  
  gOscSender.setup(TARGET_PORT, TARGET_IP);
  rt_printf("OSC sender ready -> %s:%d\n", TARGET_IP, TARGET_PORT);

  gOscTask = Bela_createAuxiliaryTask(oscSenderLoop, 50, "osc-sender");
  if(!gOscTask) {
    rt_fprintf(stderr, "Failed to create OSC sender task\n");
    return false;
  }

  Bela_scheduleAuxiliaryTask(gOscTask);
  rt_printf("OSC sender task started\n");
    return true;
}

// ========== RENDER LOOP ==========
void render(BelaContext *context, void *userData)
{
  // tracker for which analog frame we’re on
  static unsigned int analogFrame = 0;

  // Loop through audio frames (Bela syncs audio and analog frames), n = current audio frame
  for(unsigned int n = 0; n < context->audioFrames; ++n)
  {
    // +========= ANALOG INPUT PROCESSING =========+
    // Read analog input at the appropriate audio frame
    if((n % gAudioPerAnalog) == 0)
    {
      if(analogFrame >= context->analogFrames)
        analogFrame = 0;


      // +========= DC OFFSET REMOVAL =========+
      // Read piezo input (sensor signal)
      float x = analogRead(context, analogFrame, PIEZO_CH);
      analogFrame++;

      // DC offset filter (slow drift removal) following formula: y[n] = x[n] - x[n-1] + R*y[n-1]
      float dc = x - gLastAnalog + (DC_R * gPrevDC);
      gLastAnalog = x;
      gPrevDC = dc;


      // +========= SIGNAL RECTIFICATION =========+
      // Turn signal into absolute value/magnitude
      float s = std::fabs(dc);

      // Only compute noise baseline when there's no hit or lockout 
      if(!gPeaking && gRefractCount == 0) {
        gBaseline = BASE_ALPHA * gBaseline + (1.0f - BASE_ALPHA) * s;
      }


      // +========= BASELINE REMOVAL =========+
      // Remove baseline
      s = std::max(0.0f, s - gBaseline);


      // +========= REFRACTORY LOCKOUT PERIOD =========+
      // if we're in refractory period, skip
      if(gRefractCount > 0) {
        gRefractCount--;
        continue;
      }

      // +========= THRESHOLD DETECTION =========+
      // if not in a hit window, check for a tangible hit
      if(!gPeaking) {
        if(s >= THRESH) {
          gPeaking = true;
          gPeak = s;
          gPeakCount = 0;
        }
        continue;
      }

      // +========= PEAK TRACKING DURING HIT WINDOW =========+
      gPeak = std::max(gPeak, s);
      gPeakCount++;

      // if we've reached the end of the peak window, finalise hit
      if(gPeakCount >= gPeakWinSamples) {
        gPeaking = false;
        gRefractCount = gRefractSamples;

        // Map hit -> velocity between 0..1
        float vel = clamp01(gPeak * VEL_GAIN);

        // Calculate hit time in seconds (approximation)
        // double t = (context->audioFramesElapsed + n) / (double)context->audioSampleRate;

        // rt_printf("HIT t=%.6f vel=%.3f peak=%.5f base=%.5f\n", t, vel, gPeak, gBaseline);

        // Create hit event and add to queue for OSC thread to send
        int64_t tSamples = (int64_t)context->audioFramesElapsed + (int64_t)n;       
        int32_t midiVel = (int32_t)roundf(vel * 127.0f);
        if(midiVel < 0) midiVel = 0;
        if(midiVel > 127) midiVel = 127;

        rt_printf("HIT samples=%lld vel=%d\n", (long long)tSamples, midiVel);

        HitEvent ev;
        ev.tHigh = (int32_t)((uint64_t)tSamples >> 32);
        ev.tLow  = (int32_t)((uint64_t)tSamples & 0xFFFFFFFF);
        ev.pad   = 0;
        ev.vel   = midiVel;

        enqueueHit(ev);
      }
    }
  }
}

void cleanup(BelaContext *context, void *userData) {}
