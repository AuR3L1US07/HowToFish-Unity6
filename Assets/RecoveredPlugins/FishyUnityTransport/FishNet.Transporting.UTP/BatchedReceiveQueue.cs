using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FishNet.Transporting.UTP {
internal class BatchedReceiveQueue
{
	private byte[] m_Data;

	private int m_Offset;

	private int m_Length;

	public bool IsEmpty => m_Length <= 0;

	public unsafe BatchedReceiveQueue(DataStreamReader reader)
	{
		m_Data = new byte[reader.Length];
		fixed (byte* data = m_Data)
		{
			reader.ReadBytesUnsafe(data, reader.Length);
		}
		m_Offset = 0;
		m_Length = reader.Length;
	}

	public unsafe void PushReader(DataStreamReader reader)
	{
		if (m_Data.Length - (m_Offset + m_Length) < reader.Length)
		{
			if (m_Length > 0)
			{
				Array.Copy(m_Data, m_Offset, m_Data, 0, m_Length);
			}
			m_Offset = 0;
			while (m_Data.Length - m_Length < reader.Length)
			{
				Array.Resize(ref m_Data, m_Data.Length * 2);
			}
		}
		fixed (byte* data = m_Data)
		{
			reader.ReadBytesUnsafe(data + m_Offset + m_Length, reader.Length);
		}
		m_Length += reader.Length;
	}

	public ArraySegment<byte> PopMessage()
	{
		if (m_Length < 4)
		{
			return default(ArraySegment<byte>);
		}
		int num = BitConverter.ToInt32(m_Data, m_Offset);
		if (m_Length - 4 < num)
		{
			return default(ArraySegment<byte>);
		}
		ArraySegment<byte> result = new ArraySegment<byte>(m_Data, m_Offset + 4, num);
		m_Offset += 4 + num;
		m_Length -= 4 + num;
		return result;
	}
}

}