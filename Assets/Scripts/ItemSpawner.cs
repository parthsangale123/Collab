using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        public string name; // Just for labeling in Inspector
        public GameObject itemPrefab;
        public int quantity;
    }

    [Header("Spawn Settings")]
    public Vector2 spawnAreaSize = new Vector2(20f, 20f); // Width (X) and Length (Z)
    public float yHeight = 1f; // How high off the ground to spawn
    
    [Header("Loot Table")]
    // This acts as your "Dictionary" of Item -> Quantity
    public List<SpawnEntry> itemsToSpawn;

    public override void OnNetworkSpawn()
    {
        // STRICT RULE: Only the Server (Host) can spawn Network Objects
        if (!IsServer) return;

        SpawnAllItems();
    }

    private void SpawnAllItems()
    {
        foreach (var entry in itemsToSpawn)
        {
            if (entry.itemPrefab == null) continue;

            for (int i = 0; i < entry.quantity; i++)
            {
                SpawnSingleItem(entry.itemPrefab);
            }
        }
    }

    private void SpawnSingleItem(GameObject prefab)
    {
        // 1. Calculate Random Position
        // transform.position is the center of the square
        float randomX = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
        float randomZ = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
        
        Vector3 spawnPos = transform.position + new Vector3(randomX, yHeight, randomZ);

        // 2. Instantiate (Standard Unity)
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 3. Spawn (Netcode)
        // This makes it appear on all clients automatically
        instance.GetComponent<NetworkObject>().Spawn();
    }

    // --- VISUALIZATION (Gizmos) ---
    // This draws the square in the Editor so you can see where items will spawn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y);
        Gizmos.DrawWireCube(center, size);
    }
}