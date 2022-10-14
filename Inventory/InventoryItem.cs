using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/NewItem")]
public class InventoryItem : ScriptableObject
{
    public int StackSize;
    public string ItemName, StrType;
    [TextArea(1, 4)]
    public string Description;
    public GameObject DisplayItem;
    public ItemType Type;
    public Sprite DisplaySprite;
}
public enum ItemType
{
    Block, //Base block
    Item, //Item, as in consumeable
    Tool, //Tool, enough said
    All
}