using System;
using UnityEngine;

[CreateAssetMenu(fileName = "new ActiveWorldData", menuName = "Cubes/Active World Data")]
public class ActiveWorldData : ScriptableObject
{
    public string Seed;
    public string WorldName;

    public void SetData(WorldData data)
    {
        Seed = data.Seed;
        WorldName = data.WorldName;
    }
}

[Serializable]
public struct WorldData
{
    public string Seed;
    public string WorldName;
    public JsonDateTime AccessDate;
}

[Serializable]
public struct JsonDateTime
{
    public long value;
    public static implicit operator DateTime(JsonDateTime jdt)
    {
        return DateTime.FromFileTimeUtc(jdt.value);
    }
    public static implicit operator JsonDateTime(DateTime dt)
    {
        JsonDateTime jdt = new()
        {
            value = dt.ToFileTimeUtc()
        };
        return jdt;
    }
}
