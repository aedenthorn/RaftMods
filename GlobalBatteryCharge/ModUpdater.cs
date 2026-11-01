using UnityEngine;

namespace GlobalBatteryCharge
{
    public class ModUpdater : MonoBehaviour
    {
        public void Update()
        {
            if (!BepInExPlugin.modEnabled.Value || !BepInExPlugin.proportionateCharge.Value || !Raft_Network.IsHost)
                return;
            BepInExPlugin.timerEventer.DoUpdate(Time.deltaTime * BepInExPlugin.currentEfficiency, true);
        }

        public void UpdateEfficiency()
        {
            if (!BepInExPlugin.modEnabled.Value || !BepInExPlugin.proportionateCharge.Value || !Raft_Network.IsHost)
                return;
            BepInExPlugin.GetTotalEfficiency();
        }
    }
}