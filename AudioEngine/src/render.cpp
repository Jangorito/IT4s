#include <Bela.h>                         // Bela API, let's us read analog inputs, etc.
#include <cmath>                          
#include <algorithm>

// ========== SSH RUN COMMAND ==========
// ssh -tt root@bela.local "cd /root/Bela && make PROJECT=IT4 run"


// ========== Parameters to tune ==========
static const int   PIEZO_CH = 0;          // analog in channel (A0), the physical pin on Bela
static const float DC_R = 0.99f;          // DC offset filter coefficient, closer to 1 = slower
static const float THRESH = 0.01f;        // minimum hit level after DC removal and baseline
static const float PEAK_WIN_MS = 8.0f;    // after a hit, watch signal for this long to find peak and measure velocity
static const float REFRACT_MS = 25.0f;    // lockout to avoid double hits
static const float VEL_GAIN = 4.0f;       // peak * gain -> velocity; scale to get good 0..1 range
static const float BASE_ALPHA = 0.999f;   // noise floor; closer to 1 = slower baseline

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
        double t = (context->audioFramesElapsed + n) / (double)context->audioSampleRate;

        rt_printf("HIT t=%.6f vel=%.3f peak=%.5f base=%.5f\n", t, vel, gPeak, gBaseline);
      }
    }
  }
}

void cleanup(BelaContext *context, void *userData) {}
