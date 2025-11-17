// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;

namespace CREATIVE.SandboxAssets
{
	/**
		Implementations of functions in IReadOnlyList that are inexplicably
		missing.
	*/
	public static class IReadOnlyListExtensions
	{
		/**
			IndexOf is inexplicably missing from .NET's IReadOnlyList.
		*/
		public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
		{
			for (int i = 0; i < self.Count; i++)
				if (Equals(self[i], elementToFind))
					return i;

			return -1;
		}
	}
}