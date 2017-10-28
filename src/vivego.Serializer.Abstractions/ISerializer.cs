namespace vivego.Serializer.Abstractions
{
	public interface ISerializer<T>
	{
		T Serialize<TData>(TData source);
		TData Deserialize<TData>(T serializedData);
	}
}