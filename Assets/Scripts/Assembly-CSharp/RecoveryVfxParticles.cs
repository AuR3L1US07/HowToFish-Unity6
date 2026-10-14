using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Functional reconstruction of exported VFX graphs, not recovered original graph source.
// Keeps event names and world positions/directions while replacing broken GPU programs.
public sealed class RecoveryVfxParticles : IDisposable
{
    private sealed class Profile
    {
        public Color Color;
        public int Count;
        public float Speed, Size, Lifetime, Gravity, Spread;
        public bool Upward;
        public Profile(Color color, int count, float speed, float size, float lifetime,
            float gravity, float spread, bool upward = false)
        { Color = color; Count = count; Speed = speed; Size = size; Lifetime = lifetime;
          Gravity = gravity; Spread = spread; Upward = upward; }
    }

    private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>
    {
        { "WaterSplash", new Profile(new Color(.5f,.8f,1f,.85f), 32, 4f, .12f, .8f, 1f, .65f, true) },
        { "WaterExplosion", new Profile(new Color(.65f,.85f,1f,.9f), 110, 9f, .22f, 1.3f, 1f, 1f, true) },
        { "LavaExplosion", new Profile(new Color(1f,.25f,.03f,1f), 100, 8f, .25f, 1.4f, .8f, 1f, true) },
        { "VolcanoEruption", new Profile(new Color(1f,.35f,.05f,1f), 200, 18f, .5f, 3f, 1f, .45f, true) },
        { "LavaBarf", new Profile(new Color(1f,.35f,.04f,.95f), 70, 7f, .18f, 1.5f, .7f, .3f, true) },
        { "Poison", new Profile(new Color(.35f,.85f,.12f,.65f), 70, 2f, .4f, 2.1f, -.03f, 1f) },
        { "Angry", new Profile(new Color(1f,.18f,.1f,.9f), 18, 1.4f, .18f, .65f, 0f, 1f, true) },
        { "BossDeath", new Profile(new Color(.45f,.95f,.12f,.8f), 140, 7f, .3f, 1.8f, .25f, 1f) },
        { "SmallBloodExplosion", new Profile(new Color(.7f,.025f,.025f,.95f), 26, 3f, .09f, .65f, .6f, 1f) },
        { "DustExplosion", new Profile(new Color(.55f,.47f,.35f,.6f), 65, 3.2f, .4f, 1.5f, -.05f, 1f) },
        { "PufferfishDeath", new Profile(new Color(.45f,.95f,.12f,.8f), 140, 7f, .3f, 1.8f, .25f, 1f) }
    };

    private readonly Dictionary<string, ParticleSystem> _systems = new Dictionary<string, ParticleSystem>();
    private readonly System.Random _random = new System.Random(93671);
    private Material _material;
    public Transform Owner { get; private set; }

    public RecoveryVfxParticles(Transform owner) { Owner = owner; }

    public static bool HasProfile(string type) { return Profiles.ContainsKey(type); }

    public void Play(string type, Vector3 position, Vector3 direction)
    {
        Profile profile;
        if (Owner == null || !Profiles.TryGetValue(type, out profile)) return;
        if (type == "SmallBloodExplosion" && PlayerPrefs.GetInt("Blood", 1) == 0) return;
        ParticleSystem system;
        if (!_systems.TryGetValue(type, out system))
        {
            system = Create(type, profile);
            if (system == null) return;
            _systems.Add(type, system);
        }
        Vector3 axis = direction.sqrMagnitude > .000001f ? direction.normalized : Vector3.up;
        // Starting a stopped system after Emit clears the manually emitted burst.
        // Start first; subsequent events append without restarting existing particles.
        if (!system.isPlaying) system.Play();
        for (int i = 0; i < profile.Count; i++)
        {
            Vector3 random = RandomDirection();
            if (profile.Upward && direction.sqrMagnitude < .000001f) random.y = Mathf.Abs(random.y);
            Vector3 velocity = profile.Spread < 1f || direction.sqrMagnitude > .000001f
                ? (axis + random * profile.Spread).normalized : random;
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity * profile.Speed * Range(.45f, 1.1f),
                startLifetime = profile.Lifetime * Range(.65f, 1.2f),
                startSize = profile.Size * Range(.7f, 1.4f),
                startColor = profile.Color,
                rotation = Range(0f, 360f)
            };
            system.Emit(emit, 1);
        }
    }

    private ParticleSystem Create(string type, Profile profile)
    {
        if (_material == null)
        {
            var shader = Resources.Load<Shader>("RecoveryVfxParticles");
            if (shader == null) { Debug.LogError("Recovered VFX particle shader is missing."); return null; }
            _material = new Material(shader) { name = "Recovered VFX particle material" };
        }
        var go = new GameObject("Recovered VFX - " + type);
        go.transform.SetParent(Owner, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = profile.Lifetime * 1.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4096;
        main.gravityModifier = profile.Gravity;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        var color = system.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) },
            new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(.8f,.35f), new GradientAlphaKey(0,1) });
        color.color = gradient;
        var size = system.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0,1,1,.15f));
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return system;
    }

    private float Range(float min, float max) { return min + (max - min) * (float)_random.NextDouble(); }
    private Vector3 RandomDirection()
    {
        float y = Range(-1,1), angle = Range(0,Mathf.PI * 2), radius = Mathf.Sqrt(1-y*y);
        return new Vector3(radius * Mathf.Cos(angle),y,radius * Mathf.Sin(angle));
    }

    public void Dispose()
    {
        foreach (var system in _systems.Values)
            if (system != null) DestroyOwned(system.gameObject);
        _systems.Clear();
        if (_material != null) DestroyOwned(_material);
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }
}
