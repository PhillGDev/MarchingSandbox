using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
public class ObjInventory : MonoBehaviour
{
    public bool Interactable;
    public int MaxStacks, StackSize;
    public string FilterPrefix, InvName;
    public UnityEvent OnInventoryModified;
    public List<InventoryItem> Items;
    public List<ItemStack> Stacks;
    public bool AddToInventory(InventoryItem item)
    {
        if (Items.Count + 1 > MaxStacks * StackSize) return false;
        if (FilterPrefix != "") if (!item.StrType.Contains(FilterPrefix)) return false;
        Items.Add(item);
        OnInventoryModified.Invoke();
        return true;
    }
    void CreateStacks()
    {
        Stacks.Clear();
        Stacks = ItemStack.StackItems(Items);
    }

    private void OnDestroy()
    {

        for (int i = 0; i < Items.Count; i++)
        {
            Inventory.Singleton.CreatePhysicalItem(Items[i], transform.position);
        }
    }
    private void Start()
    {
        OnInventoryModified.AddListener(CreateStacks);
    }
    public bool AddStacktoInventory(ItemStack stack)
    {
        if (Items.Count + stack.Count > MaxStacks * StackSize) return false;
        if (FilterPrefix != "") if (!stack.UpgradeDef.StrType.Contains(FilterPrefix)) return false;
        for (int i = 0; i < stack.Count; i++)
        {
            Items.Add(stack.UpgradeDef);
        }
        OnInventoryModified.Invoke();
        return true;
    }
    public void RemoveStack(ItemStack stack)
    {
        for (int i = 0; i < stack.Count; i++)
        {
            RemoveFromInventory(stack.UpgradeDef);
        }
    }
    public InventoryItem RemoveFromInventory (InventoryItem item)
    {
        if (Items.Contains(item)) Items.Remove(item);
        else return null;
        OnInventoryModified.Invoke();
        return item;
    }
    public List<ItemStack> EnqeueStacks(List<ItemStack> stacks, bool AllowPartial)
    {
        List<ItemStack> Stacks = new List<ItemStack>();
        //First off, see if we have these items.
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack Owned = ItemStack.FindStackOfType(stacks[i].UpgradeDef, Stacks);
            if(Owned.Count >= stacks[i].Count)
            {
                Stacks.Add(stacks[i]);
                RemoveStack(stacks[i]);
            }
            else
            {
                Stacks.Add(Owned);
                RemoveStack(Owned);
            }
        }
        return Stacks;
    }
}
