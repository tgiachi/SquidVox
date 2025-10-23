using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Manages animation state for chunk visual effects including fade-in and rotation.
/// </summary>
public sealed class ChunkAnimationController
{
    private float _rotationY;
    private float _currentOpacity;
    private readonly float _targetOpacity = 1f;
    private bool _isFadingIn;

    /// <summary>
    /// Gets or sets the manual rotation applied to the chunk (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 ManualRotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Enables a simple idle rotation animation around the Y axis.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Rotation speed in radians per second when AutoRotate is enabled.
    /// </summary>
    public float RotationSpeed { get; set; } = MathHelper.ToRadians(10f);

    /// <summary>
    /// Gets or sets the speed at which the chunk fades in (opacity units per second).
    /// </summary>
    public float FadeInSpeed { get; set; } = 2f;

    /// <summary>
    /// Gets or sets a value indicating whether the chunk should fade in when first rendered.
    /// </summary>
    public bool EnableFadeIn { get; set; } = true;

    /// <summary>
    /// Gets the current opacity of the chunk (0.0 to 1.0).
    /// </summary>
    public float Opacity => _currentOpacity;

    /// <summary>
    /// Gets a value indicating whether the chunk is currently fading in.
    /// </summary>
    public bool IsFadingIn => _isFadingIn;

    /// <summary>
    /// Initializes the animation controller with fade-in if enabled.
    /// </summary>
    public void Initialize()
    {
        if (EnableFadeIn)
        {
            _currentOpacity = 0f;
            _isFadingIn = true;
        }
        else
        {
            _currentOpacity = _targetOpacity;
            _isFadingIn = false;
        }
    }

    /// <summary>
    /// Resets the fade-in animation to start from the beginning.
    /// </summary>
    public void ResetFadeIn()
    {
        if (EnableFadeIn && _currentOpacity < 0.01f)
        {
            _currentOpacity = 0f;
            _isFadingIn = true;
        }
    }

    /// <summary>
    /// Updates animation state including fade-in and rotation.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(float deltaTime)
    {
        // Update fade-in animation
        if (_isFadingIn)
        {
            _currentOpacity += FadeInSpeed * deltaTime;
            if (_currentOpacity >= _targetOpacity)
            {
                _currentOpacity = _targetOpacity;
                _isFadingIn = false;
            }
        }

        // Update rotation animation
        if (AutoRotate)
        {
            _rotationY = (_rotationY + RotationSpeed * deltaTime) % MathHelper.TwoPi;
        }
    }

    /// <summary>
    /// Gets the rotation matrix for the current animation state.
    /// </summary>
    /// <returns>The combined rotation matrix.</returns>
    public Matrix GetRotationMatrix()
    {
        return Matrix.CreateFromYawPitchRoll(
            _rotationY + ManualRotation.Y,
            ManualRotation.X,
            ManualRotation.Z
        );
    }

    /// <summary>
    /// Sets the opacity directly (useful for external control).
    /// </summary>
    /// <param name="opacity">The opacity value (0.0 to 1.0).</param>
    public void SetOpacity(float opacity)
    {
        _currentOpacity = MathHelper.Clamp(opacity, 0f, 1f);
    }
}
