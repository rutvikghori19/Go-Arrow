using UnityEngine;

namespace _Game.ProceduralLevels
{
    public static class LevelDefinitionJson
    {
        public static string ToJson(LevelDefinition definition, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(definition, prettyPrint);
        }

        public static LevelDefinition FromJson(string json)
        {
            return JsonUtility.FromJson<LevelDefinition>(json);
        }
    }
}
