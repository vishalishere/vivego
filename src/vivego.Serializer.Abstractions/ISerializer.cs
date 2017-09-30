namespace vivego.Serializer.Abstractions
{
	public interface ISerializer<T>
	{
		T Serialize<TData>(TData t);
		TData Deserialize<TData>(T serializedData);
	}
}