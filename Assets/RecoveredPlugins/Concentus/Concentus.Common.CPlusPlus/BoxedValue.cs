namespace Concentus.Common.CPlusPlus {
internal class BoxedValue<T>
{
	internal T Val;

	internal BoxedValue(T v = default(T))
	{
		Val = v;
	}

	public override string ToString()
	{
		if (Val != null)
		{
			return Val.ToString();
		}
		return "null";
	}
}

}