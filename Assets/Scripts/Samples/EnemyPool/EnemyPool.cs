using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public List<SpawnData> SpawnDatas;

    [SerializeField] private Enemies _spawnedEnemies;

    public Enemies SpawnedEnemies => _spawnedEnemies;

    public void Reset()
    {
        InitializeSpawnData();
        _spawnedEnemies = 0;
    }

    public void InitializeSpawnData()
    {
        SpawnDatas = new List<SpawnData>();

        int enemyTypesCount = System.Enum.GetNames(typeof(Enemies)).Length;
        for (int i = 0; i < enemyTypesCount; i++)
        {
        SpawnData spawnData = new SpawnData();
            spawnData.EnemiesType = (Enemies)(1 << i);
            SpawnDatas.Add(spawnData);
        }
    }
}
