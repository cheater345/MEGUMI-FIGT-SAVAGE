using UnityEngine;

namespace SteelTempest.Core.Events
{
    /// <summary>Fired whenever a damage event is applied to an actor.</summary>
    public readonly struct DamageEvent
    {
        public readonly GameObject Source;
        public readonly GameObject Target;
        public readonly float Amount;
        public readonly bool IsCritical;
        public readonly bool IsFinisher;

        public DamageEvent(GameObject source, GameObject target, float amount, bool isCritical, bool isFinisher)
        {
            Source = source;
            Target = target;
            Amount = amount;
            IsCritical = isCritical;
            IsFinisher = isFinisher;
        }
    }

    /// <summary>Fired when an actor is defeated.</summary>
    public readonly struct ActorDefeatedEvent
    {
        public readonly GameObject Actor;
        public readonly bool IsPlayer;

        public ActorDefeatedEvent(GameObject actor, bool isPlayer)
        {
            Actor = actor;
            IsPlayer = isPlayer;
        }
    }

    /// <summary>Fired on successful parry/perfect block.</summary>
    public readonly struct ParryEvent
    {
        public readonly GameObject Defender;

        public ParryEvent(GameObject defender) => Defender = defender;
    }

    /// <summary>Fired with any HUD-visible notification.</summary>
    public readonly struct NotificationEvent
    {
        public readonly string Text;

        public NotificationEvent(string text) => Text = text;
    }
}
