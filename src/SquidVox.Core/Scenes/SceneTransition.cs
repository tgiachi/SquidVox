using FontStashSharp.Interfaces;
using Silk.NET.Maths;
using SquidVox.Core.Data.Graphics;
using SquidVox.Core.Interfaces.Scenes;
using TrippyGL;

namespace SquidVox.Core.Scenes;

/// <summary>
/// Abstract base class for scene transitions, providing common functionality.
/// </summary>
public abstract class SceneTransition : ISVoxSceneTransition
{
    /// <summary>
    /// Gets or sets the position (not used for transitions, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;

    /// <summary>
    /// Gets or sets the scale (not used for transitions, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual Vector2D<float> Scale { get; set; } = Vector2D<float>.One;

    /// <summary>
    /// Gets or sets the rotation (not used for transitions, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual float Rotation { get; set; }

    /// <summary>
    /// Gets the scene being transitioned from.
    /// </summary>
    public ISVoxScene? FromScene { get; protected set; }

    /// <summary>
    /// Gets the scene being transitioned to.
    /// </summary>
    public ISVoxScene? ToScene { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the transition has completed.
    /// </summary>
    public bool IsCompleted { get; protected set; }

    /// <summary>
    /// Gets the progress of the transition (0.0 to 1.0).
    /// </summary>
    public float Progress { get; protected set; }

    /// <summary>
    /// Gets the duration of the transition in seconds.
    /// </summary>
    public float Duration { get; protected set; }

    /// <summary>
    /// Event fired when the transition completes.
    /// </summary>
    public event EventHandler? Completed;

    private float _elapsedTime;

    /// <summary>
    /// Initializes a new instance of the SceneTransition class.
    /// </summary>
    /// <param name="duration">Duration of the transition in seconds.</param>
    protected SceneTransition(float duration)
    {
        Duration = duration;
        IsCompleted = false;
        Progress = 0f;
        _elapsedTime = 0f;
    }

    /// <summary>
    /// Starts the transition between two scenes.
    /// </summary>
    /// <param name="fromScene">The scene to transition from (null for initial scene).</param>
    /// <param name="toScene">The scene to transition to.</param>
    public virtual void Start(ISVoxScene? fromScene, ISVoxScene toScene)
    {
        FromScene = fromScene;
        ToScene = toScene ?? throw new ArgumentNullException(nameof(toScene));
        IsCompleted = false;
        Progress = 0f;
        _elapsedTime = 0f;

        OnStart();
    }

    /// <summary>
    /// Updates the transition.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void Update(GameTime gameTime)
    {
        if (IsCompleted)
        {
            return;
        }

        _elapsedTime += (float)gameTime.DeltaTime;
        Progress = Math.Clamp(_elapsedTime / Duration, 0f, 1f);

        OnUpdate(gameTime);

        if (Progress >= 1f && !IsCompleted)
        {
            IsCompleted = true;
            OnCompleted();
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Renders the transition.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public abstract void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer);

    /// <summary>
    /// Disposes of resources used by the transition.
    /// </summary>
    public virtual void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called when the transition starts.
    /// Override this to add custom initialization logic.
    /// </summary>
    protected virtual void OnStart()
    {
    }

    /// <summary>
    /// Called during Update().
    /// Override this to add custom update logic.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnUpdate(GameTime gameTime)
    {
    }

    /// <summary>
    /// Called when the transition completes.
    /// Override this to add custom completion logic.
    /// </summary>
    protected virtual void OnCompleted()
    {
    }

    /// <summary>
    /// Called during Dispose().
    /// Override this to cleanup transition-specific resources.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Easing function for smooth transitions.
    /// </summary>
    /// <param name="t">Progress value (0.0 to 1.0).</param>
    /// <returns>Eased value.</returns>
    protected static float EaseInOut(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : 1f - (float)Math.Pow(-2f * t + 2f, 2f) / 2f;
    }

    /// <summary>
    /// Linear interpolation helper using .NET 9's built-in float.Lerp.
    /// </summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor (0.0 to 1.0).</param>
    /// <returns>Interpolated value.</returns>
    protected static float Lerp(float a, float b, float t)
    {
        return float.Lerp(a, b, t);
    }
}
