using System;
using UnityEngine;

[CreateAssetMenu(fileName = "new ActiveWorldData", menuName = "Cubes/Active World Data")]
public class ActiveWorldData : ScriptableObject
{
    public string seed;
    public string worldName;

    public void SetData(WorldData data)
    {
        seed = data.seed;
        worldName = data.worldName;
    }
}

[Serializable]
public struct WorldData
{
    public string seed;
    public string worldName;
    public JsonDateTime accessDate;
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
