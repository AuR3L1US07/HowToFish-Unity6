using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FishNet.Transporting.UTP {
internal struct BatchedSendQueue : IDisposable
{
	private NativeList<byte> m_Data;

	private NativeArray<int> m_HeadTailIndices;

	private int m_MaximumCapacity;

	private int m_MinimumCapacity;

	public const int PerMessageOverhead = 4;

	internal const int MinimumMinimumCapacity = 4096;

	private const int k_HeadInternalIndex = 0;

	private const int k_TailInternalIndex = 1;

	private int HeadIndex
	{
		get
		{
			return m_HeadTailIndices[0];
		}
		set
		{
			m_HeadTailIndices[0] = value;
		}
	}

	private int TailIndex
	{
		get
		{
			return m_HeadTailIndices[1];
		}
		set
		{
			m_HeadTailIndices[1] = value;
		}
	}

	public int Length => TailIndex - HeadIndex;

	public int Capacity => m_Data.Length;

	public bool IsEmpty => HeadIndex == TailIndex;

	public bool IsCreated => m_Data.IsCreated;

	public BatchedSendQueue(int capacity)
	{
		m_MaximumCapacity = capacity + (capacity & 1);
		m_MinimumCapacity = m_MaximumCapacity;
		while (m_MinimumCapacity / 2 >= 4096)
		{
			m_MinimumCapacity /= 2;
		}
		m_Data = new NativeList<byte>(m_MinimumCapacity, Allocator.Persistent);
		m_HeadTailIndices = new NativeArray<int>(2, Allocator.Persistent);
		m_Data.ResizeUninitialized(m_MinimumCapacity);
		HeadIndex = 0;
		TailIndex = 0;
	}

	public void Dispose()
	{
		if (IsCreated)
		{
			m_Data.Dispose();
			m_HeadTailIndices.Dispose();
		}
	}

	private unsafe void WriteBytes(ref DataStreamWriter writer, byte* data, int length)
	{
		writer.WriteBytesUnsafe(data, length);
	}

	private unsafe void AppendDataAtTail(ArraySegment<byte> data)
	{
		DataStreamWriter writer = new DataStreamWriter(m_Data.GetUnsafePtr() + TailIndex, Capacity - TailIndex);
		writer.WriteInt(data.Count);
		fixed (byte* array = data.Array)
		{
			WriteBytes(ref writer, array + data.Offset, data.Count);
		}
		TailIndex += 4 + data.Count;
	}

	public unsafe bool PushMessage(ArraySegment<byte> message)
	{
		if (!IsCreated)
		{
			return false;
		}
		if (Capacity - TailIndex >= 4 + message.Count)
		{
			AppendDataAtTail(message);
			return true;
		}
		if (HeadIndex > 0 && Length > 0)
		{
			UnsafeUtility.MemMove(m_Data.GetUnsafePtr(), m_Data.GetUnsafePtr() + HeadIndex, Length);
			TailIndex = Length;
			HeadIndex = 0;
		}
		if (Capacity - TailIndex >= 4 + message.Count)
		{
			AppendDataAtTail(message);
			while (TailIndex < Capacity / 4 && Capacity > m_MinimumCapacity)
			{
				m_Data.ResizeUninitialized(Capacity / 2);
			}
			return true;
		}
		while (Capacity - TailIndex < 4 + message.Count)
		{
			if (Capacity * 2 > m_MaximumCapacity)
			{
				return false;
			}
			m_Data.ResizeUninitialized(Capacity * 2);
		}
		AppendDataAtTail(message);
		return true;
	}

	public unsafe int FillWriterWithMessages(ref DataStreamWriter writer, int softMaxBytes = 0)
	{
		if (!IsCreated || Length == 0)
		{
			return 0;
		}
		softMaxBytes = ((softMaxBytes == 0) ? writer.Capacity : Math.Min(softMaxBytes, writer.Capacity));
		DataStreamReader dataStreamReader = new DataStreamReader(m_Data.AsArray());
		int num = HeadIndex;
		dataStreamReader.SeekSet(num);
		int num2 = dataStreamReader.ReadInt();
		int num3 = num2 + 4;
		if (num3 > softMaxBytes && num3 <= writer.Capacity)
		{
			writer.WriteInt(num2);
			WriteBytes(ref writer, m_Data.GetUnsafePtr() + dataStreamReader.GetBytesRead(), num2);
			return num3;
		}
		int num4 = 0;
		while (num < TailIndex)
		{
			dataStreamReader.SeekSet(num);
			num2 = dataStreamReader.ReadInt();
			num3 = num2 + 4;
			if (num4 + num3 > softMaxBytes)
			{
				break;
			}
			writer.WriteInt(num2);
			WriteBytes(ref writer, m_Data.GetUnsafePtr() + dataStreamReader.GetBytesRead(), num2);
			num += num3;
			num4 += num3;
		}
		return num4;
	}

	public unsafe int FillWriterWithBytes(ref DataStreamWriter writer, int maxBytes = 0)
	{
		if (!IsCreated || Length == 0)
		{
			return 0;
		}
		int num = Math.Min((maxBytes == 0) ? writer.Capacity : Math.Min(maxBytes, writer.Capacity), Length);
		WriteBytes(ref writer, m_Data.GetUnsafePtr() + HeadIndex, num);
		return num;
	}

	public void Consume(int size)
	{
		if (size >= Length)
		{
			HeadIndex = 0;
			TailIndex = 0;
			m_Data.ResizeUninitialized(m_MinimumCapacity);
		}
		else
		{
			HeadIndex += size;
		}
	}
}

}