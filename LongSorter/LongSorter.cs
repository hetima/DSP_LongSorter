using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
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
                foreach (var instruction in instructions)
                {
                    bool pass = true;
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

            //Ldc_R4 と OpCodes.Call の operand は同サイズ(4バイト)なので置き換え可能 メソッドの返り値がスタックに積まれる
            [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Inserter), "DeterminePreviews")]
            public static IEnumerable<CodeInstruction> BuildTool_Inserter_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo m = typeof(System.Collections.Generic.List<BuildTool_Inserter.PosePair>).GetMethod("Add");
                MethodInfo ac11 = typeof(LongSorter.Patch).GetMethod("AngleCorrection11");
                MethodInfo ac14 = typeof(LongSorter.Patch).GetMethod("AngleCorrection14");
                MethodInfo ac40 = typeof(LongSorter.Patch).GetMethod("AngleCorrection40");
                List<CodeInstruction> ins = instructions.ToList();

                //BuildTool_Inserter.PosePair.Add() するちょっと前に比較してる
                //ldc.r4 11 or 14
                for (int i = 0; i < ins.Count; i++)
                {
                    if (ins[i].opcode == OpCodes.Callvirt && ins[i].operand is MethodInfo o && o == m)
                    {
                        for (int j = i-1; j > i - 22; j--)
                        {
                            if (ins[j].opcode == OpCodes.Ldc_R4 && ins[j].operand is float num1)
                            {
                                if (num1 == 11f)
                                {
                                    ins[j].opcode = OpCodes.Call;
                                    ins[j].operand = ac11;
                                    break;
                                }
                                else if (num1 == 14f)
                                {
                                    ins[j].opcode = OpCodes.Call;
                                    ins[j].operand = ac14;
                                    break;
                                }
                            }
                        }
                    }

                    //IL_0c44: ldloc.s num1_V_54
                    //IL_0c46: ldc.r4  40
                    if (ac40 != null && ins[i].opcode == OpCodes.Ldloc_S && i + 1 < ins.Count)
                    {
                        if (ins[i + 1].opcode == OpCodes.Ldc_R4 && ins[i + 1].operand is float num2 && num2 == 40f)
                        {
                            ins[i + 1].opcode = OpCodes.Call;
                            ins[i + 1].operand = ac40;
                            ac40 = null; //1回だけ
                        }
                    }
                }
                return ins.AsEnumerable();
            }

            //角度なので90とか180とか返しとけば足りそう
            public static float AngleCorrection11()
            {
                return VFInput.control ? 1000f : 11f;
            }
            public static float AngleCorrection14()
            {
                return VFInput.control ? 1000f : 14f;
            }
            public static float AngleCorrection40()
            {
                return VFInput.control ? 1000f : 40f;
            }
        }


    }
}
