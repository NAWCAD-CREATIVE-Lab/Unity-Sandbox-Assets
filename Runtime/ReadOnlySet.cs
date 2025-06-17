using System;
using System.Collections;
using System.Collections.Generic;

namespace CREATIVE.SandboxAssets
{
	/**
		A close-enough re-implementation of .NET's IReadOnlySet.

		Unity supports .NET Framework instead of .NET Core, which is not only
		missing the System.Collections.Immutable namespace, but is also missing
		IReadOnlySet out of the System.Collections.ObjectModel namespace.

		For some reason, IReadOnlySet and ReadOnlySet seem to be the only items
		missing, so it seemed simplest to just re-implement them instead of
		finding a workaround.
	*/
	public interface IReadOnlySet<T> : IReadOnlyCollection<T>
	{
		public bool IsSubsetOf(IEnumerable<T> other);

		public bool IsSupersetOf(IEnumerable<T> other);

		public bool IsProperSupersetOf(IEnumerable<T> other);

		public bool IsProperSubsetOf(IEnumerable<T> other);

		public bool Overlaps(IEnumerable<T> other);

		public bool SetEquals(IEnumerable<T> other);

		public bool Contains(T item);
	}

	/**
		A close-enough re-implementation of .NET's ReadOnlySet.

		Unity supports .NET Framework instead of .NET Core, which is not only
		missing the System.Collections.Immutable namespace, but is also missing
		ReadOnlySet out of the System.Collections.ObjectModel namespace.

		For some reason, IReadOnlySet and ReadOnlySet seem to be the only items
		missing, so it seemed simplest to just re-implement them instead of
		finding a workaround.
	*/
	public class ReadOnlySet<T> : IReadOnlySet<T>
	{
		readonly ISet<T> _set;

		readonly NotSupportedException readOnlyException = new NotSupportedException("Set is a read only set.");

		public ReadOnlySet(ISet<T> set) => _set = set;

		public int Count => _set.Count;

		public bool IsReadOnly { get => true; }

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_set).GetEnumerator();

		public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

		public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

		public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

		public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

		public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

		public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

		public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

		public bool Contains(T item) => _set.Contains(item);
	}
}