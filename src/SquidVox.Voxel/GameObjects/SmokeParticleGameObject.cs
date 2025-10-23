using System;
using Microsoft.Xna.Framework;
using SquidVox.Core.GameObjects;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Emits billboarded smoke particles with configurable emission parameters.
/// </summary>
public sealed class SmokeParticleGameObject : Particle3dGameObject
{
    private float _emissionAccumulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeParticleGameObject"/> class.
    /// </summary>
    /// <param name="textureName">The texture name registered in the asset manager.</param>
    public SmokeParticleGameObject(string textureName)
    {
        SetTexture(textureName);
    }

    /// <summary>
    /// Gets or sets the emission rate in particles per second.
    /// </summary>
    public float EmissionRate { get; set; } = 40f;

    /// <summary>
    /// Gets or sets the horizontal spread radius.
    /// </summary>
    public float Spread { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the initial upward speed.
    /// </summary>
    public float RiseSpeed { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the particle lifetime in seconds.
    /// </summary>
    public float ParticleLifetime { get; set; } = 3.5f;

    /// <summary>
    /// Gets or sets the particle starting size.
    /// </summary>
    public float ParticleSize { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the gravity scale applied to particles (negative values make them rise).
    /// </summary>
    public float GravityScale { get; set; } = -0.5f;

    /// <summary>
    /// Gets or sets the starting color of the smoke.
    /// </summary>
    public Color StartColor { get; set; } = new Color(200, 200, 200, 180);

    /// <summary>
    /// Gets or sets the color multiplier applied over lifetime (for dithering the tint).
    /// </summary>
    public Color EndColor { get; set; } = new Color(80, 80, 80, 0);

    /// <summary>
    /// Gets or sets a value indicating whether the emitter automatically spawns particles.
    /// </summary>
    public bool AutoEmit { get; set; } = true;

    /// <summary>
    /// Emits a burst of particles immediately.
    /// </summary>
    /// <param name="count">Number of particles to spawn.</param>
    public void EmitBurst(int count)
    {
        var origin = GetAbsolutePosition();

        for (int i = 0; i < count; i++)
        {
            SpawnSmokeParticle(origin);
        }
    }

    /// <inheritdoc />
    protected override void OnUpdate(GameTime gameTime)
    {
        if (AutoEmit)
        {
            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _emissionAccumulator += EmissionRate * delta;
            var spawnCount = (int)MathF.Floor(_emissionAccumulator);
            if (spawnCount > 0)
            {
                var origin = GetAbsolutePosition();
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnSmokeParticle(origin);
                }

                _emissionAccumulator -= spawnCount;
            }
        }

        base.OnUpdate(gameTime);
    }

    private void SpawnSmokeParticle(Vector3 origin)
    {
        var horizontalOffset = new Vector3(
            NextFloat(-Spread, Spread),
            NextFloat(0f, Spread * 0.15f),
            NextFloat(-Spread, Spread)
        ) * 0.5f;

        var position = origin + horizontalOffset;

        var velocity = new Vector3(
            NextFloat(-0.2f, 0.2f) * Spread,
            RiseSpeed + NextFloat(0f, 0.5f),
            NextFloat(-0.2f, 0.2f) * Spread
        );

        var rotationSpeed = new Vector3(
            NextFloat(-0.2f, 0.2f),
            NextFloat(-0.2f, 0.2f),
            NextFloat(-0.2f, 0.2f)
        );

        var initialColor = Color.Lerp(StartColor, EndColor, NextFloat(0f, 0.3f));
        TrySpawnParticle(position, velocity, ParticleLifetime, ParticleSize, initialColor, rotationSpeed, GravityScale);
    }
}
