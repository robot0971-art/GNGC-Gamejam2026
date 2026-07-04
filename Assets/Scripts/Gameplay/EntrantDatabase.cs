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
        [SerializeField] private bool logWaveSelection = true;
        [SerializeField] private string characterArtFolder = "Assets/Charactor-art";

        public bool HasEnoughData => humans != null && humans.Length > 0 && ais != null && ais.Length > 0;

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
            if (humans == null || humans.Length == 0)
            {
                return new EntrantData { displayName = "Human", type = EntrantType.Human };
            }

            return humans[Random.Range(0, humans.Length)];
        }

        public EntrantData GetRandomAI()
        {
            if (ais == null || ais.Length == 0)
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
                if (!TryTakeRandomAvoidingPairs(aiPool, usedPairIds, out EntrantData ai))
                {
                    Debug.LogWarning($"No AI sprites found with non-matching pair ids for stage art round {round}.");
                    break;
                }

                wave.Entrants.Add(ai);
            }

            while (wave.Entrants.Count < entrantCount)
            {
                if (!TryTakeRandomAvoidingPairs(humanPool, usedPairIds, out EntrantData human))
                {
                    Debug.LogWarning("Not enough human sprites with non-matching pair ids for this wave.");
                    break;
                }

                wave.Entrants.Add(human);
            }

            RemoveDuplicatePairIds(wave.Entrants);
            Shuffle(wave.Entrants);
            if (logWaveSelection)
            {
                LogWaveSelection(wave, round);
            }
            return wave;
        }

        public bool TryDowngradeOneAI(WaveData wave, int currentRound)
        {
            if (wave == null || currentRound <= 1)
            {
                return false;
            }

            List<int> aiIndices = new List<int>();

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                if (wave.Entrants[i] != null && wave.Entrants[i].type == EntrantType.AI)
                {
                    aiIndices.Add(i);
                }
            }

            if (aiIndices.Count == 0)
            {
                return false;
            }

            Shuffle(aiIndices);

            for (int i = 0; i < aiIndices.Count; i++)
            {
                int targetIndex = aiIndices[i];
                int lowerRound = Mathf.Max(1, currentRound - 1);
                List<EntrantData> lowerRoundPool = BuildRoundPool(ais, lowerRound);
                HashSet<int> blockedPairIds = BuildUsedPairIdsExcept(wave.Entrants, targetIndex);

                if (TryTakeRandomAvoidingPairs(lowerRoundPool, blockedPairIds, out EntrantData downgradedAI))
                {
                    wave.Entrants[targetIndex] = downgradedAI;
                    Debug.Log($"Downgraded one AI to art round {lowerRound}: {GetEntrantDebugName(downgradedAI)}");
                    return true;
                }
            }

            Debug.LogWarning($"Could not downgrade AI for stage round {currentRound}. No lower-round AI matched pair rules.");
            return false;
        }

        private static void LogWaveSelection(WaveData wave, int round)
        {
            if (wave == null)
            {
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append($"Wave generated for art round {round}: ");

            for (int i = 0; i < wave.Entrants.Count; i++)
            {
                EntrantData entrant = wave.Entrants[i];
                builder.Append('[');
                builder.Append(i + 1);
                builder.Append(' ');
                builder.Append(entrant != null ? entrant.type.ToString() : "null");
                builder.Append(' ');
                builder.Append(GetEntrantDebugName(entrant));
                builder.Append(" pair=");
                builder.Append(GetPairId(entrant));
                builder.Append(" round=");
                builder.Append(GetRound(entrant));
                builder.Append(']');

                if (i < wave.Entrants.Count - 1)
                {
                    builder.Append(' ');
                }
            }

            Debug.Log(builder.ToString());
        }

        private static List<EntrantData> BuildPool(EntrantData[] source)
        {
            return source == null ? new List<EntrantData>() : new List<EntrantData>(source);
        }

        private static List<EntrantData> BuildRoundPool(EntrantData[] source, int round)
        {
            List<EntrantData> all = BuildPool(source);
            return all.FindAll(entrant => GetRound(entrant) == round);
        }

        private static bool TryTakeRandomAvoidingPairs(List<EntrantData> pool, HashSet<int> blockedPairIds, out EntrantData entrant)
        {
            if (pool == null || pool.Count == 0)
            {
                entrant = null;
                return false;
            }

            List<int> candidates = new List<int>();

            for (int i = 0; i < pool.Count; i++)
            {
                if (!HasBlockedPair(pool[i], blockedPairIds))
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                entrant = null;
                return false;
            }

            int selectedIndex = candidates[Random.Range(0, candidates.Count)];
            entrant = pool[selectedIndex];
            pool.RemoveAt(selectedIndex);
            AddPairId(blockedPairIds, entrant);
            return true;
        }

        private static bool HasBlockedPair(EntrantData entrant, HashSet<int> blockedPairIds)
        {
            int pairId = GetPairId(entrant);
            return pairId > 0 && blockedPairIds.Contains(pairId);
        }

        private static void AddPairId(HashSet<int> usedPairIds, EntrantData entrant)
        {
            int pairId = GetPairId(entrant);

            if (pairId > 0)
            {
                usedPairIds.Add(pairId);
            }
        }

        private static void RemoveDuplicatePairIds(List<EntrantData> entrants)
        {
            HashSet<int> seenPairIds = new HashSet<int>();

            for (int i = 0; i < entrants.Count; i++)
            {
                if (entrants[i] == null || entrants[i].type != EntrantType.AI)
                {
                    continue;
                }

                int aiPairId = GetPairId(entrants[i]);

                if (aiPairId > 0)
                {
                    seenPairIds.Add(aiPairId);
                }
            }

            for (int i = entrants.Count - 1; i >= 0; i--)
            {
                int pairId = GetPairId(entrants[i]);

                if (pairId <= 0)
                {
                    continue;
                }

                if (entrants[i] != null && entrants[i].type == EntrantType.AI)
                {
                    continue;
                }

                if (seenPairIds.Contains(pairId))
                {
                    Debug.LogWarning($"Removed duplicate entrant pair id {pairId}: {GetEntrantDebugName(entrants[i])}");
                    entrants.RemoveAt(i);
                    continue;
                }

                seenPairIds.Add(pairId);
            }
        }

        private static HashSet<int> BuildUsedPairIdsExcept(List<EntrantData> entrants, int excludedIndex)
        {
            HashSet<int> usedPairIds = new HashSet<int>();

            if (entrants == null)
            {
                return usedPairIds;
            }

            for (int i = 0; i < entrants.Count; i++)
            {
                if (i == excludedIndex)
                {
                    continue;
                }

                AddPairId(usedPairIds, entrants[i]);
            }

            return usedPairIds;
        }

        private static string GetEntrantDebugName(EntrantData entrant)
        {
            if (entrant == null)
            {
                return "<null>";
            }

            if (!string.IsNullOrEmpty(entrant.displayName))
            {
                return entrant.displayName;
            }

            return entrant.sprite != null ? entrant.sprite.name : "<unnamed>";
        }

        private static int GetPairId(EntrantData entrant)
        {
            if (entrant == null)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(entrant.displayName))
            {
                int displayNamePairId = ParsePairId(entrant.displayName);

                if (displayNamePairId > 0)
                {
                    return displayNamePairId;
                }
            }

            if (entrant.sprite != null)
            {
                int spriteNamePairId = ParsePairId(entrant.sprite.name);

                if (spriteNamePairId > 0)
                {
                    return spriteNamePairId;
                }
            }

            return entrant.pairId;
        }

        private static int GetRound(EntrantData entrant)
        {
            if (entrant == null)
            {
                return 0;
            }

            if (entrant.round > 0)
            {
                return entrant.round;
            }

            if (!string.IsNullOrEmpty(entrant.displayName))
            {
                int displayNameRound = ParseRound(string.Empty, entrant.displayName);

                if (displayNameRound > 0)
                {
                    return displayNameRound;
                }
            }

            return entrant.sprite != null ? ParseRound(string.Empty, entrant.sprite.name) : 0;
        }

        private static void Shuffle<T>(IList<T> entrants)
        {
            for (int i = entrants.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                T temporary = entrants[i];
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

                bool isAI = IsAiSprite(sprite.name);
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
            Match characterMatch = Regex.Match(value, @"cha[_-]?(\d+)", RegexOptions.IgnoreCase);

            if (characterMatch.Success && int.TryParse(characterMatch.Groups[1].Value, out int characterId))
            {
                return characterId;
            }

            Match aiMatch = Regex.Match(value, @"ai[_-]?(\d+)", RegexOptions.IgnoreCase);

            if (aiMatch.Success && int.TryParse(aiMatch.Groups[1].Value, out int aiId))
            {
                return aiId;
            }

            MatchCollection matches = Regex.Matches(value, @"\d+");

            if (matches.Count == 0)
            {
                return 0;
            }

            return int.TryParse(matches[matches.Count - 1].Value, out int id) ? id : 0;
        }

        private static bool IsAiSprite(string spriteName)
        {
            string lowerName = spriteName.ToLowerInvariant();
            return lowerName.StartsWith("ai_")
                || lowerName.StartsWith("ai-")
                || Regex.IsMatch(lowerName, @"(^|[-_])ai[-_]\d+");
        }

        private static int ParseRound(string path, string spriteName)
        {
            Match pathMatch = Regex.Match(path, @"(?:Round|Ground)(\d+)", RegexOptions.IgnoreCase);

            if (pathMatch.Success && int.TryParse(pathMatch.Groups[1].Value, out int pathRound))
            {
                return pathRound;
            }

            Match nameMatch = Regex.Match(spriteName, @"(?:round|ground)(\d+)", RegexOptions.IgnoreCase);

            if (nameMatch.Success && int.TryParse(nameMatch.Groups[1].Value, out int nameRound))
            {
                return nameRound;
            }

            return 0;
        }
    }
}
