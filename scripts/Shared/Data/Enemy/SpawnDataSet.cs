using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spawn Data Set")]
public class SpawnDataSet : ScriptableObject
{
    public SpawnData[] spawnList;
}

[System.Serializable]
public class SpawnData
{
    public int spriteType;
    public float spawnTime;
    public int health;
    public float speed;
}
