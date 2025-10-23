using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents a single particle in the 3D particle system.
/// </summary>
public class Particle
{
    /// <summary>
    /// Default gravity applied to particles.
    /// </summary>
    public const float DefaultGravity = 15f;

    /// <summary>
    /// Gets or sets the world position of the particle.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Gets or sets the particle velocity.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Remaining lifetime in seconds.
    /// </summary>
    public float LifeTime;

    /// <summary>
    /// Maximum lifetime assigned when the particle was spawned.
    /// </summary>
    public float MaxLifeTime;

    /// <summary>
    /// Base color of the particle.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Current size multiplier.
    /// </summary>
    public float Size;

    /// <summary>
    /// Gets or sets whether the particle is active.
    /// </summary>
    public bool IsActive;

    /// <summary>
    /// Current rotation (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 Rotation;

    /// <summary>
    /// Rotation speed per axis.
    /// </summary>
    public Vector3 RotationSpeed;

    /// <summary>
    /// Per-particle gravity scale.
    /// </summary>
    public float GravityScale { get; private set; } = DefaultGravity;

    /// <summary>
    /// Updates the particle state.
    /// </summary>
    /// <param name="deltaTime">Elapsed time in seconds.</param>
    public void Update(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        Velocity.Y -= GravityScale * deltaTime;
        Position += Velocity * deltaTime;
        Rotation += RotationSpeed * deltaTime;
        LifeTime -= deltaTime;

        if (LifeTime <= 0f)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Computes the alpha value based on remaining lifetime.
    /// </summary>
    public float GetAlpha()
    {
        return MathHelper.Clamp(LifeTime / MaxLifeTime, 0f, 1f);
    }

    /// <summary>
    /// Resets the particle with new parameters.
    /// </summary>
    public void Reset(
        Vector3 position,
        Vector3 velocity,
        float lifeTime,
        Color color,
        float size,
        Vector3 rotationSpeed,
        float gravityScale = DefaultGravity)
    {
        Position = position;
        Velocity = velocity;
        LifeTime = lifeTime;
        MaxLifeTime = lifeTime;
        Color = color;
        Size = size;
        IsActive = true;
        Rotation = Vector3.Zero;
        RotationSpeed = rotationSpeed;
        GravityScale = gravityScale;
    }
}
