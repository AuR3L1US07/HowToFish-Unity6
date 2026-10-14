using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types {
public class TimedOperation
{
	private readonly float _interval;

	private readonly bool _scaledTime;

	private Dictionary<string, float> _operationTimes = new Dictionary<string, float>();

	private float _lastGlobalTime;

	public TimedOperation(float interval, bool scaledTime = false)
	{
		_interval = interval;
		_scaledTime = scaledTime;
	}

	public bool TryUseOperation()
	{
		float num = (_scaledTime ? Time.time : Time.unscaledTime);
		if (num - _lastGlobalTime >= _interval)
		{
			_lastGlobalTime = num + _interval;
			return true;
		}
		return false;
	}

	public bool TryUseOperation(string key)
	{
		float num = (_scaledTime ? Time.time : Time.unscaledTime);
		if (_operationTimes.TryGetValue(key, out var value))
		{
			if (num - value >= _interval)
			{
				_operationTimes[key] = num + _interval;
				return true;
			}
			return false;
		}
		_operationTimes[key] = num + _interval;
		return true;
	}
}

}