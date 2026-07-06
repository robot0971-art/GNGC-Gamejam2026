using System.Collections.Generic;

namespace Gamejam2026.Gameplay
{
    public class WaveData
    {
        public readonly List<EntrantData> Entrants = new List<EntrantData>();

        public int AiCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < Entrants.Count; i++)
                {
                    if (Entrants[i] != null && Entrants[i].type == EntrantType.AI)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
