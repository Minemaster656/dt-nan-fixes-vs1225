using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

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

        // Patch 1: Cap dt and sanitize NaN motion in EntityPlayer.OnGameTick
        [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.OnGameTick))]
        public class EntityPlayerPatch
        {
            static void Prefix(EntityPlayer __instance, ref float dt)
            {
                if (float.IsNaN(dt) || float.IsInfinity(dt))
                    dt = 1f / 60f;
                else if (dt > 0.5f)
                    dt = 0.5f;

                var pos = __instance.Pos;
                if (pos == null) return;

                if (double.IsNaN(pos.Motion.X)) pos.Motion.X = 0;
                if (double.IsNaN(pos.Motion.Y)) pos.Motion.Y = 0;
                if (double.IsNaN(pos.Motion.Z)) pos.Motion.Z = 0;
            }
        }

        // Patch 2: Sanitize NaN in ApplyTests and skip on NaN
        [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), nameof(EntityBehaviorControlledPhysics.ApplyTests))]
        public class ApplyTestsPatch
        {
            static bool Prefix(EntityPos pos, float dt)
            {
                if (pos == null) return true;

                bool hasNaN = double.IsNaN(pos.Motion.X) || double.IsNaN(pos.Motion.Y) || double.IsNaN(pos.Motion.Z)
                           || double.IsNaN(pos.X) || double.IsNaN(pos.Y) || double.IsNaN(pos.Z);

                if (double.IsNaN(pos.Motion.X)) pos.Motion.X = 0;
                if (double.IsNaN(pos.Motion.Y)) pos.Motion.Y = 0;
                if (double.IsNaN(pos.Motion.Z)) pos.Motion.Z = 0;

                if (hasNaN)
                    return false; // skip ApplyTests entirely to avoid ArgumentException

                return true;
            }
        }

        // Patch 3: Catch ArithmeticException in GetNearestBlockSoundSource
        [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.GetNearestBlockSoundSource))]
        public class GetNearestBlockSoundSourcePatch
        {
            static Exception Finalizer(Exception __exception, ref Block __result)
            {
                if (__exception is ArithmeticException)
                {
                    __result = null;
                    return null;
                }
                return __exception;
            }
        }
    }
}
