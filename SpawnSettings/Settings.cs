using UnityModManagerNet;
namespace SpawnSettings
{
    public class Settings : UnityModManager.ModSettings
    {
        public float SpawnIntervalMultiplier { get; set; } = 1.0f;
        public float SpawnAmountMultiplier { get; set; } = 1.0f;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}