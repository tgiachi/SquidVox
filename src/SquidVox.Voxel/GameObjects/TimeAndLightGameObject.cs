using System;
using Microsoft.Xna.Framework;
using Serilog;
using SquidVox.Core.GameObjects;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Synchronizes world lighting with dynamic sky colors and sun direction.
/// Updates ambient light and light direction every frame based on time of day.
/// </summary>
public class TimeAndLightGameObject : Base3dGameObject
{
    private readonly ILogger _logger = Log.ForContext<TimeAndLightGameObject>();
    private readonly WorldGameObject _world;
    private readonly DynamicSkyGameObject _sky;
    private Vector3 _lastAmbientLight = Vector3.Zero;
    private Vector3 _lastLightDirection = Vector3.Zero;
    private int _logFrameCounter = 0;

    public TimeAndLightGameObject(WorldGameObject world, DynamicSkyGameObject sky)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _sky = sky ?? throw new ArgumentNullException(nameof(sky));
    }

    public override void Update(GameTime gameTime)
    {
        if (_world == null || _sky == null)
        {
            return;
        }

        // Convert Color (byte 0-255) to Vector3 (float 0-1)
        var ambientColor = _sky.AmbientLightColor;
        var newAmbientLight = new Vector3(
            ambientColor.R / 255f,
            ambientColor.G / 255f,
            ambientColor.B / 255f
        );

        var newLightDirection = _sky.SunDirection;

        // Update world lighting
        _world.AmbientLight = newAmbientLight;
        _world.LightDirection = newLightDirection;

        // Log changes periodically (every 60 frames = ~1 second at 60fps)
        _logFrameCounter++;
        if (_logFrameCounter % 60 == 0)
        {
            _logFrameCounter = 0;

            if (!newAmbientLight.Equals(_lastAmbientLight) || !newLightDirection.Equals(_lastLightDirection))
            {
                _logger.Verbose(
                    "Lighting updated: Ambient=({R:F2}, {G:F2}, {B:F2}), SunDir=({X:F2}, {Y:F2}, {Z:F2}), TimeOfDay={TimeOfDay:F2}",
                    newAmbientLight.X, newAmbientLight.Y, newAmbientLight.Z,
                    newLightDirection.X, newLightDirection.Y, newLightDirection.Z,
                    _sky.TimeOfDay
                );
            }

            _lastAmbientLight = newAmbientLight;
            _lastLightDirection = newLightDirection;
        }

        base.Update(gameTime);
    }
}
