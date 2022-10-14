using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickupInstance : MonoBehaviour
{
    //Animations are already being played.
    //Just for registering being picked up.
    public ItemStack Items;
    public Transform ItemPos;
    //item Pickups can have multiple of the same items, but cannot have multiple different items.
    public void OnGetItem()
    {
        Destroy(gameObject);
        //TODO:
        //Add a particle effect / sfx for picking up an item.
    }
    public void Start()
    {
        Inventory.Singleton.Pickupables.Add(this);
        Instantiate(Items.UpgradeDef.DisplayItem, ItemPos.position, ItemPos.rotation, ItemPos);
    }
    private void OnDestroy()
    {
        Inventory.Singleton.Pickupables.Remove(this);
    }
}
