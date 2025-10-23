using System.Collections.Generic;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Object pool for managing particle instances efficiently.
/// </summary>
public class ParticlePool
{
    private readonly Stack<Particle> _pool = new();
    private readonly List<Particle> _activeParticles = new();

    /// <summary>
    /// Gets the total number of particles allocated for the pool.
    /// </summary>
    public int PoolSize { get; private set; }

    /// <summary>
    /// Pre-allocates particles for the pool.
    /// </summary>
    /// <param name="size">Number of particles to allocate.</param>
    public void Initialize(int size)
    {
        PoolSize = size;
        _pool.Clear();
        _activeParticles.Clear();

        for (int i = 0; i < size; i++)
        {
            _pool.Push(new Particle());
        }
    }

    /// <summary>
    /// Gets an available particle from the pool.
    /// </summary>
    public Particle? GetParticle()
    {
        if (_pool.Count == 0)
        {
            return null;
        }

        var particle = _pool.Pop();
        _activeParticles.Add(particle);
        return particle;
    }

    /// <summary>
    /// Returns a particle to the pool and marks it as inactive.
    /// </summary>
    public void ReturnParticle(Particle particle)
    {
        particle.IsActive = false;
        _activeParticles.Remove(particle);
        _pool.Push(particle);
    }

    /// <summary>
    /// Enumerates active particles.
    /// </summary>
    public IEnumerable<Particle> GetActiveParticles() => _activeParticles;

    /// <summary>
    /// Updates all active particles.
    /// </summary>
    public void Update(float deltaTime)
    {
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime);
            if (!particle.IsActive)
            {
                ReturnParticle(particle);
            }
        }
    }

    /// <summary>
    /// Stops all particles and returns them to the pool.
    /// </summary>
    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            particle.IsActive = false;
            _pool.Push(particle);
        }

        _activeParticles.Clear();
    }
}
