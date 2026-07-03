using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gamejam2026.Gameplay
{
    public class EntrantDatabase : MonoBehaviour
    {
        [SerializeField] private EntrantData[] humans;
        [SerializeField] private EntrantData[] ais;
        [SerializeField] private bool autoFillFromCharacterArt = true;
        [SerializeField] private bool refreshAutoFillOnValidate = true;
        [SerializeField] private string characterArtFolder = "Assets/Charactor-art";

        public bool HasEnoughData => true;

        private void Awake()
        {
            AutoFillIfNeeded();
        }

        private void OnValidate()
        {
            AutoFillIfNeeded();
        }

        public EntrantData GetRandomHuman()
        {
            if (humans.Length == 0)
            {
                return new EntrantData { displayName = "Human", type = EntrantType.Human };
            }

            return humans[Random.Range(0, humans.Length)];
        }

        public EntrantData GetRandomAI()
        {
            if (ais.Length == 0)
            {
                return new EntrantData { displayName = "AI", type = EntrantType.AI };
            }

            return ais[Random.Range(0, ais.Length)];
        }

        public WaveData BuildWave(StageConfig config, int stageRound)
        {
            WaveData wave = new WaveData();
            int entrantCount = Mathf.Max(1, config.entrantsPerWave);
            int aiCount = Random.Range(config.minAiCount, config.maxAiCount + 1);
            aiCount = Mathf.Clamp(aiCount, 1, entrantCount);
            int round = config.artRound > 0 ? config.artRound : Mathf.Max(1, stageRound);

            List<EntrantData> aiPool = BuildRoundPool(ais, round);
            List<EntrantData> humanPool = BuildPool(humans);
            HashSet<int> usedPairIds = new HashSet<int>();

            for (int i = 0; i < aiCount; i++)
            {
                EntrantData ai = TakeRandom(aiPool, GetRandomAI());
                wave.Entrants.Add(ai);
                AddPairId(usedPairIds, ai);
            }

            while (wave.Entrants.Count < entrantCount)
            {
                wave.Entrants.Add(TakeRandomAvoidingPairs(humanPool, GetRandomHuman(), usedPairIds));
            }

            Shuffle(wave.Entrants);
            return wave;
        }

        private static List<EntrantData> BuildPool(EntrantData[] source)
        {
            return source == null ? new List<EntrantData>() : new List<EntrantData>(source);
        }

        private static List<EntrantData> BuildRoundPool(EntrantData[] source, int round)
        {
            List<EntrantData> all = BuildPool(source);
            List<EntrantData> matchingRound = all.FindAll(entrant => entrant.round == round);
            return matchingRound.Count > 0 ? matchingRound : all;
        }

        private static EntrantData TakeRandom(List<EntrantData> pool, EntrantData fallback)
        {
            if (pool == null || pool.Count == 0)
            {
                return fallback;
            }

            int index = Random.Range(0, pool.Count);
            EntrantData entrant = pool[index];
            pool.RemoveAt(index);
            return entrant;
        }

        private static EntrantData TakeRandomAvoidingPairs(List<EntrantData> pool, EntrantData fallback, HashSet<int> blockedPairIds)
        {
            if (pool == null || pool.Count == 0)
            {
                return fallback;
            }

            List<int> candidates = new List<int>();

            for (int i = 0; i < pool.Count; i++)
            {
                if (!HasBlockedPair(pool[i], blockedPairIds))
                {
                    candidates.Add(i);
                }
            }

            int selectedIndex = candidates.Count > 0
                ? candidates[Random.Range(0, candidates.Count)]
                : Random.Range(0, pool.Count);
            EntrantData entrant = pool[selectedIndex];
            pool.RemoveAt(selectedIndex);
            AddPairId(blockedPairIds, entrant);
            return entrant;
        }

        private static bool HasBlockedPair(EntrantData entrant, HashSet<int> blockedPairIds)
        {
            return entrant != null && entrant.pairId > 0 && blockedPairIds.Contains(entrant.pairId);
        }

        private static void AddPairId(HashSet<int> usedPairIds, EntrantData entrant)
        {
            if (entrant != null && entrant.pairId > 0)
            {
                usedPairIds.Add(entrant.pairId);
            }
        }

        private static void Shuffle(IList<EntrantData> entrants)
        {
            for (int i = entrants.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                EntrantData temporary = entrants[i];
                entrants[i] = entrants[swapIndex];
                entrants[swapIndex] = temporary;
            }
        }

        private void AutoFillIfNeeded()
        {
#if UNITY_EDITOR
            if (!autoFillFromCharacterArt || BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            bool hasHumans = !refreshAutoFillOnValidate && humans != null && humans.Length > 0;
            bool hasAis = !refreshAutoFillOnValidate && ais != null && ais.Length > 0;

            if (hasHumans && hasAis)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { characterArtFolder });
            List<EntrantData> foundHumans = new List<EntrantData>();
            List<EntrantData> foundAis = new List<EntrantData>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (sprite == null)
                {
                    continue;
                }

                string lowerName = sprite.name.ToLowerInvariant();
                bool isAI = lowerName.StartsWith("ai_") || lowerName.StartsWith("ai") || lowerName.Contains("-ai-");
                EntrantData data = new EntrantData
                {
                    displayName = sprite.name,
                    type = isAI ? EntrantType.AI : EntrantType.Human,
                    sprite = sprite,
                    pairId = ParsePairId(sprite.name),
                    round = ParseRound(path, sprite.name)
                };

                if (isAI)
                {
                    foundAis.Add(data);
                }
                else
                {
                    foundHumans.Add(data);
                }
            }

            if (!hasHumans && foundHumans.Count > 0)
            {
                humans = foundHumans.ToArray();
            }

            if (!hasAis && foundAis.Count > 0)
            {
                ais = foundAis.ToArray();
            }
#endif
        }

        private static int ParsePairId(string value)
        {
            MatchCollection matches = Regex.Matches(value, @"\d+");

            if (matches.Count == 0)
            {
                return 0;
            }

            return int.TryParse(matches[matches.Count - 1].Value, out int id) ? id : 0;
        }

        private static int ParseRound(string path, string spriteName)
        {
            Match pathMatch = Regex.Match(path, @"Round(\d+)", RegexOptions.IgnoreCase);

            if (pathMatch.Success && int.TryParse(pathMatch.Groups[1].Value, out int pathRound))
            {
                return pathRound;
            }

            Match nameMatch = Regex.Match(spriteName, @"round(\d+)", RegexOptions.IgnoreCase);

            if (nameMatch.Success && int.TryParse(nameMatch.Groups[1].Value, out int nameRound))
            {
                return nameRound;
            }

            return 0;
        }
    }
}
