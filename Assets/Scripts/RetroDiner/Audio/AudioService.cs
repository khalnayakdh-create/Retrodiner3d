using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroDiner.Audio
{
    /// <summary>
    /// Pooled SFX player with separate SFX/music buses. Until real recordings exist, every clip is
    /// synthesized at startup (short sine/noise bursts) so the loop has sound from day one.
    /// Real AudioClips assigned on ItemDefinitions override the procedural bank automatically.
    /// </summary>
    public class AudioService
    {
        const int SampleRate = 22050;
        readonly AudioSource[] pool;
        readonly AudioSource music;
        readonly Dictionary<string, AudioClip> bank = new Dictionary<string, AudioClip>();
        int next;

        public bool SfxEnabled { get; set; } = true;
        public float SfxVolume = 0.9f;

        public AudioService(Transform parent, int poolSize = 10)
        {
            var root = new GameObject("[Audio]");
            if (parent != null) root.transform.SetParent(parent, false);
            else UnityEngine.Object.DontDestroyOnLoad(root);
            pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var s = root.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
            music = root.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.4f;
            BuildBank();
        }

        public void Play(string key, float volume = 1f, float pitch = 1f)
        {
            if (!SfxEnabled || string.IsNullOrEmpty(key)) return;
            AudioClip clip;
            if (!bank.TryGetValue(key, out clip)) { if (!bank.TryGetValue("pickup", out clip)) return; }
            PlayClip(clip, volume, pitch);
        }

        public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (!SfxEnabled || clip == null) return;
            var s = pool[next];
            next = (next + 1) % pool.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume * SfxVolume);
        }

        public void SetMusicPitch(float pitch) { music.pitch = pitch; }

        bool musicEnabledField = true;
        public bool MusicEnabled
        {
            get { return musicEnabledField; }
            set { musicEnabledField = value; if (music != null) music.mute = !value; }
        }

        /// <summary>Starts the procedural rockabilly loop (built lazily on first call).</summary>
        public void StartMusic()
        {
            if (music.clip == null) music.clip = BuildMusicLoop();
            music.mute = !musicEnabledField;
            if (!music.isPlaying) music.Play();
        }

        public void StopMusic() { if (music.isPlaying) music.Stop(); }

        // ---------------- procedural music ----------------

        /// <summary>
        /// 8 bars of swung 12-bar-blues-style rockabilly in A at 128 BPM: walking bass, brushed hat,
        /// backbeat snare and off-beat guitar stabs. ~15 s loop, mono, built once at startup.
        /// </summary>
        static AudioClip BuildMusicLoop()
        {
            const float bpm = 128f;
            const int bars = 8;
            float beat = 60f / bpm;
            float barLen = beat * 4f;
            float total = barLen * bars;
            int n = Mathf.CeilToInt(total * SampleRate);
            var data = new float[n];
            var rng = new System.Random(7);

            // Chord roots per bar (A blues: I I I I IV IV I I), in Hz for the bass octave.
            float A = 55f, D = 73.42f;
            float[] roots = { A, A, A, A, D, D, A, A };
            // Walking pattern in semitones from the root: root, 3rd(maj), 5th, 6th / root, 6th, 5th, 3rd
            int[][] walks = { new[] { 0, 4, 7, 9 }, new[] { 0, 9, 7, 4 } };

            for (int bar = 0; bar < bars; bar++)
            {
                float root = roots[bar];
                var walk = walks[bar % 2];
                for (int b = 0; b < 4; b++)
                {
                    float t0 = bar * barLen + b * beat;
                    float freq = root * Mathf.Pow(2f, walk[b] / 12f);
                    AddTone(data, t0, beat * 0.95f, freq, 0.55f, 0.012f, 0.35f, 2);           // bass pluck
                    AddNoise(data, t0, 0.025f, 0.10f, 0.001f, rng);                              // hat on the beat
                    AddNoise(data, t0 + beat * 0.66f, 0.02f, 0.07f, 0.001f, rng);                // swung off-beat hat
                    if (b == 1 || b == 3)
                    {
                        AddNoise(data, t0, 0.09f, 0.22f, 0.002f, rng);                           // brushed snare backbeat
                        float g = root * 4f;                                                     // guitar stab two octaves up
                        AddTone(data, t0 + beat * 0.66f, 0.12f, g, 0.10f, 0.003f, 0.08f, 1);
                        AddTone(data, t0 + beat * 0.66f, 0.12f, g * Mathf.Pow(2f, 4f / 12f), 0.08f, 0.003f, 0.08f, 1);
                        AddTone(data, t0 + beat * 0.66f, 0.12f, g * Mathf.Pow(2f, 7f / 12f), 0.08f, 0.003f, 0.08f, 1);
                    }
                }
            }
            // Soft clip + gentle fade at the loop seam so it repeats cleanly.
            for (int i = 0; i < n; i++)
            {
                float v = data[i];
                v = v / (1f + Mathf.Abs(v));
                int edge = SampleRate / 50;
                if (i < edge) v *= i / (float)edge;
                if (i > n - edge) v *= (n - i) / (float)edge;
                data[i] = v;
            }
            var clip = AudioClip.Create("RockabillyLoop", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddTone(float[] data, float start, float dur, float freq, float amp, float attack, float decay, int harmonics)
        {
            int i0 = Mathf.Max(0, (int)(start * SampleRate));
            int i1 = Mathf.Min(data.Length, (int)((start + dur) * SampleRate));
            for (int i = i0; i < i1; i++)
            {
                float t = (i - i0) / (float)SampleRate;
                float env = Mathf.Clamp01(t / attack) * Mathf.Exp(-t / decay);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t);
                if (harmonics >= 2) v += 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
                data[i] += v * env * amp;
            }
        }

        static void AddNoise(float[] data, float start, float dur, float amp, float attack, System.Random rng)
        {
            int i0 = Mathf.Max(0, (int)(start * SampleRate));
            int i1 = Mathf.Min(data.Length, (int)((start + dur) * SampleRate));
            float last = 0f;
            for (int i = i0; i < i1; i++)
            {
                float t = (i - i0) / (float)SampleRate;
                float env = Mathf.Clamp01(t / attack) * (1f - t / dur);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = last * 0.6f + white * 0.4f;     // slight low-pass = brush feel
                data[i] += last * env * amp;
            }
        }

        // ---------------- procedural clip bank ----------------

        void BuildBank()
        {
            bank["pickup"] = Tone("pickup", 0.09f, t => Sine(660f, t) * Env(t, 0.09f, 0.004f));
            bank["pickup_soft"] = Tone("pickup_soft", 0.11f, t => Sine(440f, t) * 0.8f * Env(t, 0.11f, 0.01f));
            bank["pickup_clink"] = Tone("pickup_clink", 0.12f, t => (Sine(1800f, t) * 0.6f + Sine(2600f, t) * 0.3f) * Env(t, 0.12f, 0.002f));
            bank["pickup_thud"] = Tone("pickup_thud", 0.14f, t => Sine(180f, t) * Env(t, 0.14f, 0.004f));
            bank["pickup_chime"] = Tone("pickup_chime", 0.18f, t => (Sine(1320f, t) + Sine(1980f, t) * 0.4f) * 0.5f * Env(t, 0.18f, 0.003f));
            bank["pickup_snap"] = Tone("pickup_snap", 0.06f, t => Noise(t) * Env(t, 0.06f, 0.001f));
            bank["pickup_tap"] = Tone("pickup_tap", 0.07f, t => Sine(990f, t) * Env(t, 0.07f, 0.002f));
            bank["pickup_plop"] = Tone("pickup_plop", 0.12f, t => Sine(300f - 600f * t, t) * Env(t, 0.12f, 0.005f));
            bank["clear"] = Tone("clear", 0.45f, t => (Sine(880f, t) * (t < 0.15f ? 1f : 0.3f) + Sine(1320f, t) * (t > 0.12f ? 1f : 0f)) * 0.5f * Env(t, 0.45f, 0.005f));
            // 13A: soft filtered-noise swell for the tray->ticket flight (one per match), bell only on completion.
            bank["whoosh"] = Tone("whoosh", 0.38f, t => { float w = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.38f)); return (Noise(t) * 0.22f + Sine(220f + 380f * t, t) * 0.12f) * w * w; });
            bank["bell"] = Tone("bell", 0.8f, t => (Sine(1760f, t) + Sine(2640f, t) * 0.5f + Sine(4400f, t) * 0.2f) * 0.45f * Env(t, 0.8f, 0.002f));
            bank["open"] = Tone("open", 0.35f, t => (Sine(1500f, t) + Sine(2250f, t) * 0.5f) * 0.5f * Env(t, 0.35f, 0.002f));
            bank["untie"] = Tone("untie", 0.2f, t => Noise(t) * 0.5f * Env(t, 0.2f, 0.02f));
            bank["siren"] = Tone("siren", 1.2f, t => Sine(500f + 300f * Mathf.Sin(t * 10f), t) * 0.5f * Env(t, 1.2f, 0.05f));
            bank["win"] = Tone("win", 1.2f, t =>
            {
                float f = t < 0.25f ? 523f : t < 0.5f ? 659f : t < 0.75f ? 784f : 1046f;
                return Sine(f, t) * 0.5f * Env(t % 0.25f, 0.25f, 0.01f);
            });
            bank["lose"] = Tone("lose", 1.0f, t =>
            {
                float f = t < 0.33f ? 440f : t < 0.66f ? 370f : 294f;
                return Sine(f, t) * 0.5f * Env(t % 0.33f, 0.33f, 0.01f);
            });
            bank["ui"] = Tone("ui", 0.06f, t => Sine(1200f, t) * 0.5f * Env(t, 0.06f, 0.002f));
            bank["freeze"] = Tone("freeze", 0.5f, t => (Sine(2200f - 1200f * t, t)) * 0.4f * Env(t, 0.5f, 0.01f));
            bank["rewind"] = Tone("rewind", 0.5f, t => (Sine(300f + 900f * t, t)) * 0.4f * Env(t, 0.5f, 0.01f));
            bank["spatula"] = Tone("spatula", 0.3f, t => (Noise(t) * 0.3f + Sine(2000f + 1500f * t, t) * 0.5f) * Env(t, 0.3f, 0.01f));
            bank["coin"] = Tone("coin", 0.15f, t => (Sine(2093f, t) * (t < 0.07f ? 1f : 0f) + Sine(2637f, t) * (t >= 0.07f ? 1f : 0f)) * 0.4f * Env(t, 0.15f, 0.002f));
        }

        static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
        static float Env(float t, float dur, float attack)
        {
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float r = Mathf.Clamp01(1f - t / dur);
            return a * r * r;
        }
        static readonly System.Random noiseRng = new System.Random(1);
        static float Noise(float t) => (float)(noiseRng.NextDouble() * 2.0 - 1.0);

        static AudioClip Tone(string name, float duration, Func<float, float> generator)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(generator(t), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
