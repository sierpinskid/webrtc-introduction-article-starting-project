using UnityEngine;

namespace WebRTCTutorial
{
    /// <summary>
    /// Base class for serializable objects. This class uses Unity's Json Utility https://docs.unity3d.com/ScriptReference/JsonUtility.html
    /// Suitable for simple objects but for more complex ones, use Newtonsoft.Json
    /// </summary>
    public abstract class UnityJsonObjectBase<TType> : IJsonObject<TType>
    {
        public string ToJson() => JsonUtility.ToJson(this);

        public TType FromJson(string serializedObject) => JsonUtility.FromJson<TType>(serializedObject);
    }
}