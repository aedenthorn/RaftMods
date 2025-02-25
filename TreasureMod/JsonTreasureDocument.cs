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
        public string itemName;
        public float weight;
        public JsonTreasureObject[] randomItems;
        public JsonTreasureObject[] guaranteedItems;
        public Dictionary<string, JsonTreasureObject> objects;
        public int minItems = 1;
        public int maxItems = 1;
    }
}