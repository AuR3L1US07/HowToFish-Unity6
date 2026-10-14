using FishNet.Serializing;
using UnityEngine.Scripting;

namespace FishNet.Component.Prediction {
[Preserve]
public static class RigidbodyStateSerializers
{
	public static void WriteRigidbodyState(this Writer writer, RigidbodyState value)
	{
		writer.WriteVector3(value.Position);
		writer.WriteQuaternion32(value.Rotation);
		writer.WriteBoolean(value.IsKinematic);
		if (!value.IsKinematic)
		{
			writer.WriteVector3(value.Velocity);
			writer.WriteVector3(value.AngularVelocity);
		}
	}

	public static RigidbodyState ReadRigidbodyState(this Reader reader)
	{
		RigidbodyState result = new RigidbodyState
		{
			Position = reader.ReadVector3(),
			Rotation = reader.ReadQuaternion32(),
			IsKinematic = reader.ReadBoolean()
		};
		if (!result.IsKinematic)
		{
			result.Velocity = reader.ReadVector3();
			result.AngularVelocity = reader.ReadVector3();
		}
		return result;
	}

	public static void WriteRigidbody2DState(this Writer writer, Rigidbody2DState value)
	{
		writer.WriteVector3(value.Position);
		writer.WriteQuaternion32(value.Rotation);
		writer.WriteBoolean(value.Simulated);
		writer.WriteBoolean(value.IsKinematic);
		if (value.Simulated)
		{
			writer.WriteVector2(value.Velocity);
			writer.WriteSingle(value.AngularVelocity);
		}
	}

	public static Rigidbody2DState ReadRigidbody2DState(this Reader reader)
	{
		Rigidbody2DState result = new Rigidbody2DState
		{
			Position = reader.ReadVector3(),
			Rotation = reader.ReadQuaternion32(),
			Simulated = reader.ReadBoolean(),
			IsKinematic = reader.ReadBoolean()
		};
		if (result.Simulated)
		{
			result.Velocity = reader.ReadVector2();
			result.AngularVelocity = reader.ReadSingle();
		}
		return result;
	}
}

}