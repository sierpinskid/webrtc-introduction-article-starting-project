namespace WebRTCTutorial
{
    /// <summary>
    /// Interface for objects that can be serialized to and deserialized from JSON
    /// </summary>
    public interface IJsonObject<TType>
    {
        string ToJson();

        TType FromJson(string serializedObject);
    }
}