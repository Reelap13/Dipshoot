using System;
using System.Collections.Generic;
using Game.TickSystem;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Players
{
    public class PlayerCharacter : NetworkBehaviour
    {
        [SerializeField] private List<PlayerCharacterComponent> _components;

        public TickManager TickManager { get; private set; }
        public InputBuffer InputBuffet { get; } = new();

        public void Initialize(TickManager tick_manager)
        {
            TickManager = tick_manager;

            foreach (var component in _components)
                component.Initialize(this);
        }
    }
}