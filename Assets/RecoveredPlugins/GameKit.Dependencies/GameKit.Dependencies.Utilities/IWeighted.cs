using GameKit.Dependencies.Utilities.Types;

namespace GameKit.Dependencies.Utilities {
public interface IWeighted
{
	float GetWeight();

	ByteRange GetQuantity();
}

}