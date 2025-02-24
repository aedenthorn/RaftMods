using System.Collections.Generic;

namespace TreasureMod
{
    public class JsonTreasureDocument
    {
        public Dictionary<string, JsonLandmark> landmarks = new Dictionary<string, JsonLandmark>();
        public Dictionary<string, JsonTreasureObject> treasures = new Dictionary<string, JsonTreasureObject>();
    }

    public class JsonLandmark
    {
        public int minTreasures;
        public int maxTreasures;
    }

    public class JsonTreasureObject
    {
        public float weight = 1f;
        public JsonRandomItem[] randomItems;
        public JsonGuaranteedItem[] guaranteedItems;
        public Dictionary<string, JsonTreasureObject> objects;
        public int minItems;
        public int maxItems;
    }

    public class JsonRandomItem
    {
        public string itemName;
        public float weight = 1f;
    }
    public class JsonGuaranteedItem
    {
        public string itemName;
        public int amount;
    }
}