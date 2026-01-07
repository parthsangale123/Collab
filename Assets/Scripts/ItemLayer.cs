using UnityEngine;
using Unity.Netcode;

public class ItemNetworkLayer : NetworkBehaviour
{
    public NetworkVariable<int> syncedLayerIndex = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
 
        if (IsServer)
        {
            
            string selectedLayer = Random.value > 0.5f ? "p1" : "p2";
            syncedLayerIndex.Value = LayerMask.NameToLayer(selectedLayer);
        }

        SetLayer(syncedLayerIndex.Value);


        syncedLayerIndex.OnValueChanged += OnLayerChanged;
    }

    private void OnLayerChanged(int oldLayer, int newLayer)
    {
        SetLayer(newLayer);
    }

    private void SetLayer(int layerIndex)
    {
  
        if (layerIndex >= 0)
        {
            gameObject.layer = layerIndex;
            
            foreach(Transform child in transform)
            {
                child.gameObject.layer = layerIndex;
            }
        }
    }
}