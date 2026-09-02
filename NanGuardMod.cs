using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.Common;

namespace NanGuard
{
    public class NanGuardMod : ModSystem
    {
        private static Harmony harmony;

        public override void StartClientSide(ICoreClientAPI api)
        {
            harmony = new Harmony("nanguard");
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("nanguard");
        }

        // Patch 1: Cap dt in GameTickListener.OnTriggered
        [HarmonyPatch(typeof(GameTickListener), nameof(GameTickListener.OnTriggered))]
        public class GameTickListenerPatch
        {
            static FieldInfo lastUpdateField = AccessTools.Field(typeof(GameTickListener), "LastUpdateMilliseconds");

            static void Prefix(GameTickListener __instance, long ellapsedMilliseconds)
            {
                long lastMs = (long)lastUpdateField.GetValue(__instance);
                float dt = (float)(ellapsedMilliseconds - lastMs) / 1000f;

                if (float.IsNaN(dt) || float.IsInfinity(dt))
                {
                    lastUpdateField.SetValue(__instance, ellapsedMilliseconds - 50);
                }
                else if (dt > 1.0f)
                {
                    lastUpdateField.SetValue(__instance, ellapsedMilliseconds - 100);
                }
            }
        }

        // Patch 2: Sanitize NaN in EntityPlayer.OnGameTick
        [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.OnGameTick))]
        public class EntityPlayerPatch
        {
            static void Prefix(EntityPlayer __instance, float dt)
            {
                var pos = __instance.Pos;
                if (pos == null) return;

                if (double.IsNaN(pos.X) || double.IsNaN(pos.Y) || double.IsNaN(pos.Z))
                {
                    pos.X = 0;
                    pos.Y = 100;
                    pos.Z = 0;
                    pos.Motion.X = 0;
                    pos.Motion.Y = 0;
                    pos.Motion.Z = 0;
                }

                if (double.IsNaN(pos.Motion.X))
                    pos.Motion.X = 0;
                if (double.IsNaN(pos.Motion.Y))
                    pos.Motion.Y = 0;
                if (double.IsNaN(pos.Motion.Z))
                    pos.Motion.Z = 0;
            }
        }

        // Patch 3: Sanitize NaN in ApplyTests (defense-in-depth)
        [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), nameof(EntityBehaviorControlledPhysics.ApplyTests))]
        public class ApplyTestsPatch
        {
            static void Prefix(EntityPos pos)
            {
                if (pos == null) return;

                if (double.IsNaN(pos.Motion.X))
                    pos.Motion.X = 0;
                if (double.IsNaN(pos.Motion.Y))
                    pos.Motion.Y = 0;
                if (double.IsNaN(pos.Motion.Z))
                    pos.Motion.Z = 0;
            }
        }

        // Patch 4: Catch ArithmeticException in GetNearestBlockSoundSource
        [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.GetNearestBlockSoundSource))]
        public class GetNearestBlockSoundSourcePatch
        {
            static Exception Finalizer(Exception __exception)
            {
                if (__exception is ArithmeticException)
                {
                    return null;
                }
                return __exception;
            }
        }
    }
}