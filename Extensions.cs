using HarmonyLib;
using UnityEngine;

namespace BBElevatorFieldTripMultiplier.Extensions
{
	public static class MatchExtensions
	{
		public static CodeMatcher LogAll(this CodeMatcher m, bool beginFromStart = false, int count = 0, int offset = 0)
		{
			var clone = m.Clone();

			if (beginFromStart)
				clone.Start();

			if (count <= 0)
				count = clone.Remaining;

			clone.Advance(offset);

			int i = 0;

			while (clone.IsValid && i++ <= count)
			{
				Debug.Log($"{clone.Pos}: {clone.Opcode} | {clone.Operand}");
				clone.Advance(1);
			}

			return m;

		}
		/// <summary>
		/// Creates a clone of the <paramref name="match"/> and calls <c>MatchForward()</c> with the desired parameters and returns the <paramref name="pos"/> from it
		/// <para>This is useful to dynamically set a branch destination</para>
		/// </summary>
		/// <param name="match"></param>
		/// <param name="pos"></param>
		/// <param name="useEnd"></param>
		/// <param name="matches"></param>
		/// <returns>The <paramref name="match"/> itself and the <paramref name="pos"/> from <c>MatchForward()</c></returns>
		public static CodeMatcher GetPositionFromMatchForward(this CodeMatcher match, out int pos, bool useEnd, params CodeMatch[] matches)
		{
			pos = match.Clone().MatchForward(useEnd, matches).Pos;
			return match;
		}
		/// <summary>
		/// Creates a clone of the <paramref name="match"/> and calls <c>MatchBack()</c> with the desired parameters and returns the <paramref name="pos"/> from it
		/// <para>This is useful to dynamically set a branch destination</para>
		/// </summary>
		/// <param name="match"></param>
		/// <param name="pos"></param>
		/// <param name="useEnd"></param>
		/// <param name="matches"></param>
		/// <returns>The <paramref name="match"/> itself and the <paramref name="pos"/> from <c>MatchBack()</c></returns>
		public static CodeMatcher GetPositionFromMatchBack(this CodeMatcher match, out int pos, bool useEnd, params CodeMatch[] matches)
		{
			pos = match.Clone().MatchBack(useEnd, matches).Pos;
			return match;
		}
		/// <summary>
		/// Just outputs the operand of the CodeMatcher as a variable
		/// </summary>
		/// <param name="m"></param>
		/// <param name="operand"></param>
		/// <returns>The CodeMatcher itself and the operand</returns>
		public static CodeMatcher GrabOperand(this CodeMatcher m, out object operand)
		{
			operand = m.Operand;
			return m;
		}
	}
}
