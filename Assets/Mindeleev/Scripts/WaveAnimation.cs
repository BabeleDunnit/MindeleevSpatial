using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Procedural animation system for Polytrons using cyclic wave modulation of scale, position, and rotation.
/// Supports sinusoid, square, triangle, sawtooth, and sample-and-hold waveforms.
/// Animations are defined by a modulation matrix, similar to a synth LFO matrix.
/// </summary>
public class WaveAnimation : MonoBehaviour
{
    public enum AnimationType { None, Breathe, Appear, Disappear, Think }

    // you can do a sawtooth wave using a triangle with duty 0 or 1
    public enum WaveShape { Sine, Square, Triangle, /* Sawtooth,*/ SampleHold }

    [Serializable]
    public struct Modulator
    {
        public WaveShape shape;
        public float frequency; // Hz
        public float amplitude;
        public float offset;
        public float dutyCycle; // For square wave (0..1)
        // public float slope;     // For sawtooth (0..1)
        public bool oneShot;    // If true, runs once then stops
        public float duration;  // Used for one-shot
        public float startTime; // Internal use
        public bool active;     // Internal use
    }

    [Serializable]
    public struct ModMatrix
    {
        public Modulator scaleX, scaleY, scaleZ;
        public Modulator posX, posY, posZ;
        public Modulator rotX, rotY, rotZ;
    }

    private AnimationType _animationType = AnimationType.None;
    private AnimationType animationType
    {
        get => _animationType;
        set
        {
            _animationType = value;
            switch (_animationType)
            {
                case AnimationType.Breathe:
                    {
                        float freq = 0.1f + UnityEngine.Random.value * 0.3f;
                        matrix = new ModMatrix
                        {
                            scaleX = new Modulator { shape = WaveShape.Sine, frequency = freq, amplitude = 0.05f, offset = 0f, dutyCycle = 0.5f },
                            scaleY = new Modulator { shape = WaveShape.Sine, frequency = freq, amplitude = 0.05f, offset = 0f, dutyCycle = 0.5f },
                            scaleZ = new Modulator { shape = WaveShape.Sine, frequency = freq, amplitude = 0.05f, offset = 0f, dutyCycle = 0.5f }
                        };
                    }
                    break;
                case AnimationType.Think:
                    {
                        float freqX = 0.1f + UnityEngine.Random.value * 0.3f;
                        float freqY = 0.1f + UnityEngine.Random.value * 0.3f;
                        matrix = new ModMatrix
                        {
                            scaleX = new Modulator { shape = WaveShape.Square, frequency = freqX, amplitude = 0.2f, offset = 0f, dutyCycle = 0.3f },
                            scaleY = new Modulator { shape = WaveShape.Triangle, frequency = freqY, amplitude = 0.2f, offset = 0f, dutyCycle = 0.2f },
                            scaleZ = new Modulator { shape = WaveShape.Sine, frequency = 0.5f, amplitude = 0.2f, offset = 0f }
                        };
                    }
                    break;
                default:
                    matrix = new ModMatrix();
                    break;
            }
            ResetAnimation();
        }
    }

    // public AnimationType animationName = AnimationType.None;
    public ModMatrix matrix;

    private Vector3 referenceLocalScale;
    private Vector3 referenceLocalPosition;
    private Quaternion referenceLocalRotation;

    private float appearStartTime;
    private float disappearStartTime;
    private bool appearDone;
    private bool disappearDone;

    bool isPaused = false;

    public void Pause(bool paused)
    {
        if (paused != isPaused)
        {
            transform.localScale = referenceLocalScale;
        }

        isPaused = paused;
    }

    public void SetReferenceTransform(Vector3 referenceLocalScale)
    {
        this.referenceLocalScale = referenceLocalScale;
        referenceLocalPosition = transform.localPosition;
        referenceLocalRotation = transform.localRotation;
    }

    void OnEnable()
    {
        ResetAnimation();
    }

    void ResetAnimation()
    {
        appearDone = false;
        disappearDone = false;
        appearStartTime = Time.time;
        disappearStartTime = Time.time;
    }

    void FixedUpdate()
    {
        if (isPaused) return;

        switch (animationType)
        {
            case AnimationType.Appear:
                UpdateAppear();
                break;

            case AnimationType.Disappear:
                UpdateDisappear();
                break;

            case AnimationType.Breathe:
            case AnimationType.Think:
                ApplyModMatrix();
                break;
        }
    }

    // --- Animation implementations ---
    /*
        void ApplyBreathe()
        {
            float t = Time.time;
            float freq = 0.5f; // 2 seconds period
            float scaleMod = Wave(WaveShape.Sine, t, freq, 0.05f, 1f);
            Vector3 s = referenceLocalScale * (1f + scaleMod);
            transform.localScale = s;
        }
    */

    void UpdateAppear()
    {
        if (appearDone) return;
        float t = Time.time - appearStartTime;
        float duration = 1.0f;
        float progress = Mathf.Clamp01(t / duration);
        float scaleVal = Mathf.Lerp(0.01f, 1f, progress);
        transform.localScale = referenceLocalScale * scaleVal;
        if (progress >= 1f) appearDone = true;
    }

    void UpdateDisappear()
    {
        if (disappearDone) return;
        float t = Time.time - disappearStartTime;
        float duration = 1.0f;
        float progress = Mathf.Clamp01(t / duration);
        float scaleVal = Mathf.Lerp(1f, 0.01f, progress);
        transform.localScale = referenceLocalScale * scaleVal;
        if (progress >= 1f) disappearDone = true;
    }

    /*
        void ApplyThink()
        {
            float t = Time.time;
            float[] mods = new float[3];
            float freq = 0.5f; // 2 seconds per axis

            // X axis morph
            mods[0] = Wave(WaveShape.Square, t, freq, 0.2f, 1f);
            // Y axis morph (starts after X)
            mods[1] = Wave(WaveShape.Sine, t - 2f, freq, 0.2f, 1f);
            // Z axis morph (starts after Y)
            mods[2] = Wave(WaveShape.Sine, t - 4f, freq, 0.2f, 1f);

            Vector3 s = new Vector3(
                referenceLocalScale.x * (1f + mods[0]),
                referenceLocalScale.y * (1f + mods[1]),
                referenceLocalScale.z * (1f + mods[2])
            );
            transform.localScale = s;
        }
    */

    // --- Waveform generator ---
    float Wave(WaveShape shape, float t, float freq, float amp, float offset, float duty = 0.5f)
    {
        float phase = (t * freq) % 1f;
        switch (shape)
        {
            case WaveShape.Sine:
                return offset + amp * Mathf.Sin(phase * 2f * Mathf.PI);
            case WaveShape.Square:
                return offset + amp * (phase < duty ? 1f : -1f);
/*                
            case WaveShape.Triangle:
                return offset + amp * (4f * Mathf.Abs(phase - 0.5f) - 1f);
                */
            case WaveShape.Triangle:
                return offset + amp * (2f * (phase < duty ? phase / duty : (1f - phase) / (1f - duty)) - 1f);
            case WaveShape.SampleHold:
                return offset + amp * (UnityEngine.Random.value * 2f - 1f);
            default:
                return offset;
        }
    }

    // --- Utility for future expansion ---
    public void SetAnimation(AnimationType type)
    {
        animationType = type;
        ResetAnimation();

        if (type == AnimationType.Appear)
        {
            // Store the intended final scale
            // Set scale to nearly zero for the first frame
            transform.localScale = referenceLocalScale * 0.01f;
        }
    }

    // Example: For future, you can combine multiple modulators for scale, position, rotation
    void ApplyModMatrix()
    {
        // Example usage for scale
        float t = Time.time;
        Vector3 scaleMod = new Vector3(
            Wave(matrix.scaleX.shape, t, matrix.scaleX.frequency, matrix.scaleX.amplitude, matrix.scaleX.offset, matrix.scaleX.dutyCycle),
            Wave(matrix.scaleY.shape, t, matrix.scaleY.frequency, matrix.scaleY.amplitude, matrix.scaleY.offset, matrix.scaleY.dutyCycle),
            Wave(matrix.scaleZ.shape, t, matrix.scaleZ.frequency, matrix.scaleZ.amplitude, matrix.scaleZ.offset, matrix.scaleZ.dutyCycle)
        );
        transform.localScale = Vector3.Scale(referenceLocalScale, Vector3.one + scaleMod);

        // Similarly for position and rotation...
    }
}