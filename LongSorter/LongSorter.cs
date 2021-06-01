using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LongSorter
{
    [BepInPlugin(__GUID__, __NAME__, "1.0.0")]
    public class LongSorter : BaseUnityPlugin
    {
        public const string __NAME__ = "LongSorter";
        public const string __GUID__ = "com.hetima.dsp." + __NAME__;

        new internal static ManualLogSource Logger;
        void Awake()
        {
            Logger = base.Logger;
            //Logger.LogInfo("Awake");

            new Harmony(__GUID__).PatchAll(typeof(Patch));
        }


        static class Patch
        {

            public static IEnumerable<CodeInstruction> CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                float multiplier = 5f;
                int standBy = 0;
                MethodInfo m = typeof(PlanetGrid).GetMethod("CalcSegmentsAcross");
                bool pass = true;
                foreach (var instruction in instructions)
                {
                    if (standBy > 0)
                    {
                        if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float num)
                        {
                            if (num > 3f)
                            {
                                if (num < 5f) num = 5f;
                                instruction.operand = num * multiplier;
                            }
                        }
                        standBy--;
                    }
                    else if (m != null && instruction.operand is MethodInfo o && o == m)
                    {
                        standBy = 56;
                        m = null;
                    }

                    if (pass)
                    {
                        yield return instruction;
                    }
                }
            }


            [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Inserter), "CheckBuildConditions")]
            public static IEnumerable<CodeInstruction> BuildTool_Inserter_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return CheckBuildConditions_Transpiler(instructions);
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
            public static IEnumerable<CodeInstruction> BuildTool_Click_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return CheckBuildConditions_Transpiler(instructions);
            }
        }


    }
}
