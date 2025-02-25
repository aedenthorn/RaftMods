using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StackingSails
{
    [BepInPlugin("aedenthorn.StackingSails", "Stacking Sails", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static double lastTime = 1;
        public static bool pausedMenu = false; 
        public static bool wasActive = false;

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
            speedMult = Config.Bind<float>("General", "SpeedMult", 1f, "Sail speed multiplier");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
        }

		[HarmonyPatch(typeof(Raft), "FixedUpdate")]
		static class Raft_FixedUpdate_Patch
		{
			static void Postfix(Raft __instance, ref float ___speed, Vector3 ___moveDirection, ref float ___currentMovementSpeed, ref Rigidbody ___body, ref StudioEventEmitter ___eventEmitter_idle, ref Vector3 ___previousPosition, ref float ___maxVelocity, ref float ___maxSpeed)
			{
				if (!modEnabled.Value)
					return;

				if (!Raft_Network.IsHost)
				{
					return;
				}
				if (GameModeValueManager.GetCurrentGameModeValue().raftSpecificVariables.isRaftAlwaysAnchored)
				{
					return;
				}
				if (!__instance.IsAnchored)
				{
					Vector3 moveDirection = Vector3.zero;
					List<Sail> allSails = Sail.AllSails;
					Vector3 vector = Vector3.zero;
					for (int i = 0; i < allSails.Count; i++)
					{
						Sail sail = allSails[i];
						if (sail.open)
						{
							vector += sail.GetNormalizedDirection();
						}
					}
					if (vector.z < 0f)
					{
						if ((double)Mathf.Abs(vector.x) > 0.7)
						{
							vector.z = (moveDirection.z = 0f);
						}
						else
						{
							vector.z = -0.8f;
						}
					}
					moveDirection += vector;

					if (moveDirection != Vector3.zero)
					{
						moveDirection *= speedMult.Value;
						//Dbgl("speed: " + ___speed + " current: " + ___currentMovementSpeed + " move magnitude: " + moveDirection.magnitude);
						___maxVelocity = 10f;
						___maxSpeed = 10f;
						___body.AddForce(moveDirection);
						___eventEmitter_idle.SetParameter("velocity", ___body.velocity.magnitude);
						___previousPosition = ___body.transform.position;
					}
				}
			}
		}
	}
}
