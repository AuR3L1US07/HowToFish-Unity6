using System;
using System.Text;

namespace Concentus.Common.CPlusPlus {
internal class Pointer<T>
{
	private const bool CHECK_UNINIT_MEM = false;

	private T[] _array;

	private int _offset;

	internal int Offset => _offset;

	internal T[] Data => _array;

	internal T this[int index]
	{
		get
		{
			return _array[index + _offset];
		}
		set
		{
			_array[index + _offset] = value;
		}
	}

	internal T this[uint index]
	{
		get
		{
			return this[(int)index];
		}
		set
		{
			this[(int)index] = value;
		}
	}

	internal Pointer(int capacity)
	{
		_array = new T[capacity];
		_offset = 0;
	}

	internal Pointer(T[] buffer)
	{
		_array = buffer;
		_offset = 0;
	}

	internal Pointer(T[] buffer, int absoluteOffset)
	{
		_array = buffer;
		_offset = absoluteOffset;
	}

	internal Pointer<T> Iterate(out T returnVal)
	{
		returnVal = _array[_offset];
		return Point(1);
	}

	internal Pointer<T> Point(int relativeOffset)
	{
		if (relativeOffset == 0)
		{
			return this;
		}
		return new Pointer<T>(_array, _offset + relativeOffset);
	}

	internal Pointer<T> Point(uint relativeOffset)
	{
		if (relativeOffset == 0)
		{
			return this;
		}
		return new Pointer<T>(_array, _offset + (int)relativeOffset);
	}

	private static string invert_endianness(string hexstring)
	{
		StringBuilder stringBuilder = new StringBuilder(hexstring.Length);
		for (int i = 0; i < hexstring.Length / 2; i++)
		{
			stringBuilder.Append(hexstring.Substring(hexstring.Length - (i + 1) * 2, 2));
		}
		return stringBuilder.ToString();
	}

	private static void PrintMemCopy<E>(E[] source, int sourceOffset, int length)
	{
		if (typeof(E) == typeof(int) || typeof(E) == typeof(uint))
		{
			string text = string.Empty;
			for (int i = 0; i < length; i++)
			{
				text += invert_endianness($"{source[i + sourceOffset]:x8}");
			}
		}
		else if (typeof(E) == typeof(short) || typeof(E) == typeof(ushort))
		{
			string text2 = string.Empty;
			for (int j = 0; j < length; j++)
			{
				text2 += invert_endianness($"{source[j + sourceOffset]:x4}");
			}
		}
		else if (typeof(E) == typeof(byte) || typeof(E) == typeof(sbyte))
		{
			string text3 = string.Empty;
			for (int k = 0; k < length; k++)
			{
				text3 += invert_endianness($"{source[k + sourceOffset]:x2}");
			}
		}
	}

	internal void MemCopyTo(Pointer<T> destination, int length)
	{
		if (destination != null)
		{
			Array.Copy(_array, _offset, destination._array, destination.Offset, length);
			return;
		}
		for (int i = 0; i < length; i++)
		{
			destination[i] = _array[i + _offset];
		}
	}

	internal void MemCopyTo(T[] destination, int offset, int length)
	{
		Array.Copy(_array, _offset, destination, offset, length);
	}

	internal void MemCopyFrom(T[] source, int sourceOffset, int length)
	{
		Array.Copy(source, sourceOffset, _array, _offset, length);
	}

	internal void MemSet(T value, int length)
	{
		MemSet(value, (uint)length);
	}

	internal void MemSet(T value, uint length)
	{
		for (int i = _offset; i < _offset + length; i++)
		{
			_array[i] = value;
		}
	}

	internal void MemMoveTo(Pointer<T> other, int length)
	{
		if (_array == other._array)
		{
			MemMove(other.Offset - Offset, length);
		}
		else
		{
			MemCopyTo(other, length);
		}
	}

	internal void MemMove(int move_dist, int length)
	{
		MemoryExtensions.AsSpan(_array, _offset, length).CopyTo(MemoryExtensions.AsSpan(_array, _offset + move_dist, length));
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		Pointer<T> pointer = (Pointer<T>)obj;
		if (pointer._offset == _offset)
		{
			return pointer._array == _array;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _array.GetHashCode() + _offset.GetHashCode();
	}
}
internal static class Pointer
{
	internal static Pointer<E> Malloc<E>(int capacity)
	{
		return new Pointer<E>(capacity);
	}

	internal static Pointer<E> GetPointer<E>(this E[] memory, int offset = 0)
	{
		if (memory == null)
		{
			return null;
		}
		return new Pointer<E>(memory, offset);
	}
}

}