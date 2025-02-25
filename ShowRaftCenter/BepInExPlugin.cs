using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ShowRaftCenter
{
    [BepInPlugin("aedenthorn.ShowRaftCenter", "Show Raft Center", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> showKey;
        public static ConfigEntry<bool> holdToShow;
        public static ConfigEntry<Color> colorOne;
        public static ConfigEntry<Color> colorTwo;
        public static ConfigEntry<float> colorChangeRate;
        public static GameObject marker;

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = false)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
            showKey = Config.Bind<string>("Options", "ShowKey", "=", "Key to show center of raft");
            holdToShow = Config.Bind<bool>("Options", "HoldToShow", true, "Hold to show?");
            colorOne = Config.Bind<Color>("Options", "ColorOne", Color.white, "Color One");
            colorTwo = Config.Bind<Color>("Options", "ColorTwo", new Color(0.75f, 0.75f, 0.75f, 1), "Color Two");

        }
        public void Update()
        {
            if (AedenthornUtils.CheckKeyDown(showKey.Value))
            {
                Dbgl("Pressed key");
                if(marker != null)
                {
                    Dbgl("destroying marker");
                    Destroy(marker);
                    marker = null;
                    return;
                }
                Raft raft = ComponentManager<Raft>.Value;
                if (raft is null)
                {
                    Dbgl("Raft is null");
                    return;
                }
                Dbgl("creating marker");
                marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.GetComponent<Collider>().enabled = false;
                marker.GetComponent<MeshRenderer>().material.color = colorOne.Value;
                marker.transform.localScale = new Vector3(0.2f, 100, 0.2f);
                marker.transform.SetParent(raft.transform, false);
            }
            else if (holdToShow.Value && marker != null && !AedenthornUtils.CheckKeyHeld(showKey.Value))
            {
                Dbgl("destroying marker");
                Destroy(marker);
                marker = null;
            }
            if(marker != null && colorOne.Value != colorTwo.Value)
            {
                marker.GetComponent<MeshRenderer>().material.color = Color.Lerp(colorOne.Value, colorTwo.Value, Mathf.PingPong(Time.time, 1));
                if(marker.GetComponent<MeshRenderer>().material.color == colorTwo.Value)
                {
                    marker.GetComponent<MeshRenderer>().material.color = colorOne.Value;
                }
            }
        }
    }
}
