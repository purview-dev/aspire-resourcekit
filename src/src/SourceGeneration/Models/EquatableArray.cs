using System.Collections;
using System.Collections.Immutable;

namespace Purview.Aspire.ResourceIsolation.SourceGeneration.Models;

readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new([]);

	readonly ImmutableArray<T> _array = array;

	public int Count => _array.IsDefault ? 0 : _array.Length;

	public bool IsEmpty => Count == 0;

	public T this[int index] => _array[index];

	public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

	public static EquatableArray<T> Create(params T[] items) => new(ImmutableArray.Create(items));

	public bool Equals(EquatableArray<T> other) => AsImmutableArray().SequenceEqual(other.AsImmutableArray());

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		// HashCode is netstandard2.1+ and unavailable in source generator projects.
		// Use unchecked prime multiplication, the canonical netstandard2.0 alternative.
		unchecked
		{
			var hash = 17;
			foreach (var item in AsImmutableArray())
				hash = hash * 31 + (item?.GetHashCode() ?? 0);
			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

	public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
