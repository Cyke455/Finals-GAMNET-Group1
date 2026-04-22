using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public enum ObjectType
{
    Collectible,
    RandomObject
}

public class RandomSpawner : MonoBehaviour
{
    public GameObject Collectible;
    public Tilemap tilemap;
    public GameObject[] objectPrefabs;

    public float objectprobability = 0.5f;
    public int maxObjects = 20;
    public float objectTime = 10f;
    public float spawninterval = 0.5f;

    private List<Vector3> validSpawnPositions = new List<Vector3>();
    private List<GameObject> spawnObjects = new List<GameObject>();
    private bool isSpawning = false;

    void Start()
    {
        GatherValidPositions();
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }
    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
            StartSpawning();
    }
    void StartSpawning ()
    {
        if (!isSpawning)
            StartCoroutine(SpawnObjectsNeeded());
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }
    private int ActiveObjectCount()
    {
        spawnObjects.RemoveAll(item => item == null);
        return spawnObjects.Count;
    }

    private IEnumerator SpawnObjectsNeeded()
    {
        isSpawning = true;

        while (true)
        {
            if (ActiveObjectCount() < maxObjects)
            {
                SpawnObject();
            }

            yield return new WaitForSeconds(spawninterval);
        }
    }

    private bool PositionHasObject(Vector3 positionToCheck)
    {
        return spawnObjects.Any(obj =>
            obj != null &&
            Vector3.Distance(obj.transform.position, positionToCheck) < 1.0f);
    }

    private ObjectType RandomObjectType()
    {
        float randomChoice = Random.value;

        if (randomChoice <= objectprobability)
            return ObjectType.Collectible;
        else
            return ObjectType.RandomObject;
    }

    private void SpawnObject()
    {
        if (validSpawnPositions.Count == 0) return;

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

        int safetyCounter = 0;

        while (!validPositionFound && safetyCounter < 50)
        {
            safetyCounter++;

            int randomIndex = Random.Range(0, validSpawnPositions.Count);
            Vector3 potentialPosition = validSpawnPositions[randomIndex];

            Vector3 leftPosition = potentialPosition + Vector3.left;
            Vector3 rightPosition = potentialPosition + Vector3.right;

            if (!PositionHasObject(leftPosition) && !PositionHasObject(rightPosition))
            {
                spawnPosition = potentialPosition;
                validPositionFound = true;
            }
        }

        if (validPositionFound)
        {
            ObjectType objectType = RandomObjectType();
            GameObject prefabToSpawn;

            if (objectType == ObjectType.Collectible)
                prefabToSpawn = Collectible;
            else
            {
                if (objectPrefabs.Length == 0) return;
                prefabToSpawn = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
            }

            GameObject newObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            newObj.GetComponent<NetworkObject>().Spawn();

            spawnObjects.Add(newObj);
            Destroy(newObj, objectTime);
        }
    }

    private void GatherValidPositions()
    {
        validSpawnPositions.Clear();

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
        Vector3 start = tilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];

                if (tile != null)
                {
                    Vector3 place = start + new Vector3(x + 0.5f, y + 2f, 0);
                    validSpawnPositions.Add(place);
                }
            }
        }
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        isSpawning = false;
    }

}
