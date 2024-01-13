using BBElevatorFieldTripMultiplier.Extensions;
using BBElevatorFieldTripMultiplier.Plugin;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BBElevatorFieldTripMultiplier.Patches
{
	[HarmonyPatch(typeof(LevelGenerator), "Generate", MethodType.Enumerator)]
	internal class MultiplyStuff
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> MultipleFieldTrips(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
		{
			var rngField = AccessTools.Field(typeof(LevelBuilder), "controlledRNG");

			var matcher = new CodeMatcher(instructions, gen);
			var fieldTripDir = matcher.MatchForward(false,
				new CodeMatch(OpCodes.Ldloc_2),
				new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelBuilder), "ld")),
				new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelObject), "fieldTrip")),
				new CodeMatch(OpCodes.Brfalse, name: "IL_07DA")
				).Advance(-1).Operand;
			var fieldTrip = matcher.MatchForward(true,
				new CodeMatch(OpCodes.Ldc_I4_5),
				new CodeMatch(OpCodes.Stloc_3),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldnull)
				).Advance(1).Operand;

			matcher.End();
			matcher.MatchBack(true, // Get the latest if block with fieldTrip thingy
				new CodeMatch(OpCodes.Ldloc_2),
				new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelBuilder), "ld")),
				new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelObject), "fieldTrip")),
				new CodeMatch(OpCodes.Brfalse, name: "IL_2894")
				);



			var forInt = matcher.Advance(1).InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Stloc_S, gen.DeclareLocal(typeof(int)))

				).Advance(-1).Operand;

			var lastLineoftrip = matcher
				.Clone() // Don't forget this is cloned
				.MatchForward(true,
				new CodeMatch(OpCodes.Ldstr, name: "No valid spawn points were found for the field trip spawn!"),
				new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Debug), "LogWarning", [typeof(object)]))
				).Pos;

			matcher.Advance(1).InsertBranchAndAdvance(OpCodes.Br, lastLineoftrip); // destination is Placeholder, it is expected to be changed anyways

			int lineToRepeat = matcher.Pos;

			matcher.MatchForward(true,
				new CodeMatch(CodeInstruction.LoadField(typeof(TileController), "position")),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld, name: "<fieldTripDir>"),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld, name: "<fieldTrip>"),
				new CodeMatch(OpCodes.Call, AccessTools.Method("LevelBuilder:CreateTripEntrance", [typeof(IntVector2), typeof(Direction), typeof(FieldTripObject)])))
				.Advance(1).SetJumpTo(OpCodes.Br_S, lastLineoftrip, out _); // Branch to nothing, I'm done with this code

			matcher.Advance(lastLineoftrip - matcher.Pos + 2) // Goes to that same pos
				.InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Ldfld, rngField),
				new CodeInstruction(OpCodes.Call, AccessTools.Method("Directions:ControlledRandomDirection", [typeof(System.Random)])),
				new CodeInstruction(OpCodes.Stfld, fieldTripDir) // Basically put a random direction to the same fieldTripDir field
				);

			// Setting up random field trip

			var array = matcher.InsertAndAdvance( // making an array
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LevelBuilder), "ld")),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(LevelObject), "fieldTrips")),
				new CodeInstruction(OpCodes.Stloc_S, gen.DeclareLocal(typeof(WeightedSelection<FieldTripObject>[])))
				).Advance(-1).Operand;

			matcher.Advance(1).InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldloc_S, array),
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Ldfld, rngField),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WeightedSelection<FieldTripObject>), "ControlledRandomSelection", [typeof(WeightedSelection<FieldTripObject>[]), typeof(System.Random)])),
				new CodeInstruction(OpCodes.Stfld, fieldTrip)

				);
			// Ending for loop
			matcher.InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldloc_S, forInt),
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Add),
				new CodeInstruction(OpCodes.Stloc_S, forInt),

				new CodeInstruction(OpCodes.Ldloc_S, forInt),
				Transpilers.EmitDelegate(() => BasePlugin.i.fieldTripAmount.Value) // Replace this later with a delegate or manual call on the settings to get the maximum amount
				).InsertBranchAndAdvance(OpCodes.Blt, lineToRepeat);

			int targetIdx = matcher.Pos - 3;
			matcher.Advance(lineToRepeat - 1 - matcher.Pos).
				SetJumpTo(OpCodes.Br, targetIdx, out _); // Goes back to tell the branch where to go


			return matcher.InstructionEnumeration();
		}

		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> MultipleElevators(IEnumerable<CodeInstruction> instructions) => // Basically multiply elevators and include more directions in order to work
			new CodeMatcher(instructions)
			.MatchForward(true, 
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(CodeInstruction.Call("Directions:All")),
				new CodeMatch(OpCodes.Stfld, name: "<potentailExitDirections>") // Not a
				)
			.GrabOperand(out object potentialExitsLocalVar)
			.Advance(1)
			.InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, potentialExitsLocalVar),
				Transpilers.EmitDelegate(() =>
				{
					var dirs = new List<Direction>();
					for (int i = 0; i < BasePlugin.i.elevatorMultiplier.Value - 1; i++)
						dirs.AddRange(Directions.All());
					
					return dirs;
				}),
				new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Direction>), "AddRange", [typeof(IEnumerable<Direction>)]))
				)
			.Advance(4)
			.InsertAndAdvance(
				new CodeInstruction(Transpilers.EmitDelegate(() => BasePlugin.i.elevatorMultiplier.Value)),
				new CodeInstruction(OpCodes.Mul)
				)
			//.LogAll(count: 50, offset: -30)
			.InstructionEnumeration();
	}
}
