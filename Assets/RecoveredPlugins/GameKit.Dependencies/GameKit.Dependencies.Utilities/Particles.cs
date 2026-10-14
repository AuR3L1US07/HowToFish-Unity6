using UnityEngine;

namespace GameKit.Dependencies.Utilities {
public static class Particles
{
	public static float StopParticleSystem(ParticleSystem[] systems, bool stopLoopingOnly)
	{
		return StopParticleSystem(systems, stopLoopingOnly, ParticleSystemStopBehavior.StopEmitting);
	}

	public static float StopParticleSystem(ParticleSystem[] systems, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
	{
		return StopParticleSystem(systems, stopLoopingOnly: false, stopBehavior);
	}

	public static float StopParticleSystem(ParticleSystem[] systems, bool stopLoopingOnly, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
	{
		if (systems == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < systems.Length; i++)
		{
			num = Mathf.Max(num, StopParticleSystem(systems[i], stopLoopingOnly, stopBehavior));
		}
		return num;
	}

	public static float StopParticleSystem(ParticleSystem system, bool stopLoopingOnly, bool stopChildren = false)
	{
		return StopParticleSystem(system, stopLoopingOnly, ParticleSystemStopBehavior.StopEmitting, stopChildren);
	}

	public static float StopParticleSystem(ParticleSystem system, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting, bool stopChildren = false)
	{
		return StopParticleSystem(system, stopLoopingOnly: false, stopBehavior, stopChildren);
	}

	public static float StopParticleSystem(ParticleSystem system, bool stopLoopingOnly, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting, bool stopChildren = false)
	{
		if (system == null)
		{
			return 0f;
		}
		if (stopChildren)
		{
			StopParticleSystem(system.GetComponentsInChildren<ParticleSystem>(), stopLoopingOnly, stopBehavior);
		}
		float b = system.main.duration - system.time;
		float result = Mathf.Max(0f, b);
		if (stopLoopingOnly)
		{
			if (system.main.loop)
			{
				system.Stop(withChildren: false, stopBehavior);
				return result;
			}
		}
		else
		{
			system.Stop(withChildren: false, stopBehavior);
		}
		return result;
	}

	public static float ReturnLongestCycle(ParticleSystem[] systems)
	{
		float num = 0f;
		for (int i = 0; i < systems.Length; i++)
		{
			float b = systems[i].main.duration - systems[i].time;
			num = Mathf.Max(num, b);
		}
		return num;
	}
}

}