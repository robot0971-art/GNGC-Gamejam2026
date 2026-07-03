using System;
using UnityEngine;

namespace Gamejam2026.Gameplay
{
    [Serializable]
    public class EntrantData
    {
        public string displayName;
        public EntrantType type;
        public Sprite sprite;
        public int pairId;
        public int round;
    }
}
