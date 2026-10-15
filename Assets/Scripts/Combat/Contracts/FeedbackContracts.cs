using UnityEngine;

namespace ExpiryGame.Combat
{
    public interface IHitStop
    {
        void Play(float seconds);
    }

    public interface ICameraShaker
    {
        void Shake(Vector2 direction, float amplitude, float seconds);
    }

    public enum ParticleKind { Crumb, Juice, Rot, Spark }

    public interface IHitFx
    {
        void PlaySpark(Vector2 position);
        void PlaySlash(Vector2 position, float angleDegrees);
        void BurstParticles(ParticleKind kind, Vector2 position, Vector2 direction, int count, float spreadDegrees, float speedScale);
    }
}
