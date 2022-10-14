using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
public class Inventory : MonoBehaviour
{
    public Camera RendCam; //This camera will ignore everything in the scene except for UI.s
    public Transform ItemPosition;
    public int IconSize;
    //Icon creation
    public int MaxStacks;
    public UnityEvent OnInventoryModified;
    public static Inventory Singleton;
    public Transform SlotParent;
    public InventoryItem[] AllItems;
    public List<ItemStack> Items = new List<ItemStack>();
    public GameObject InventorySlot, PickupInstance;
    List<InventoryUiSlot> uiSlots = new List<InventoryUiSlot>();
    public bool HighlightingEnabled;
    public int HighlightIndex;
    public Dictionary<string, InventoryItem> ItemDictionary = new Dictionary<string, InventoryItem>(); //For getting a runtime reference to items.
    public float PickupDistance;
    private void Awake()
    {
        Singleton = this;
    }
    public bool AllowDisplay = true;
    public ItemType Filter;
    //public InventoryItem GetItemByObj(GameObject obj) We don't actually need this anymore, since we're using a pickup system instead of a string system.
    private void Start()
    {
        LoadDatabase(out AllItems, out ItemDictionary);
        OnInventoryModified.AddListener(RedrawItems);
    }
    private void Update()
    {
        for (int i = Pickupables.Count - 1; i != -1; i--)
        {
            float Dist = Vector3.Distance(transform.position, Pickupables[i].transform.position);
            if(PickupDistance >= Dist)
            {
                List<ItemStack> stack = new List<ItemStack>() { Pickupables[i].Items };
                AddStacks(stack, out List<ItemStack> Overflow);
                Debug.Log("Picked up " + Pickupables[i].Items.UpgradeDef.ItemName);
                Destroy(Pickupables[i].gameObject);
                Pickupables.RemoveAt(i);
                if(Overflow.Count != 0)
                {
                    CreatePhysicalItem(Overflow[0].UpgradeDef,transform.position);
                }
            }
        }
        for (int i = 0; i < uiSlots.Count; i++)
        {
            bool Highlight = i == HighlightIndex && HighlightingEnabled;
            uiSlots[i].SlotText.transform.parent.gameObject.GetComponent<Image>().color = Highlight ? Color.blue: Color.white;
            //uiSlots[i].Dr.localRotation = Quaternion.Euler(uiSlots[i].ObjPoint.localEulerAngles + Vector3.up * 60f * Time.deltaTime);
        }
    }
    #region Helpers
    public static void LoadDatabase(out InventoryItem[] AllItems, out Dictionary<string, InventoryItem> AllItemDictionary)
    {
        InventoryItem[] Base = Resources.LoadAll<InventoryItem>("Items/Base");
        InventoryItem[] Consumed = Resources.LoadAll<InventoryItem>("Items/Consumed");
        List<InventoryItem> Total = new List<InventoryItem>();
        Total.AddRange(Base);
        Total.AddRange(Consumed);
        AllItems = Total.ToArray();
        AllItemDictionary = new Dictionary<string, InventoryItem>();
        for (int i = 0; i < AllItems.Length; i++)
        {
            AllItemDictionary.Add(AllItems[i].name, AllItems[i]);
        }
    }
    public void CreatePhysicalItem(InventoryItem item, Vector3 Position)
    {
        //Create an instance of this item in the 'real world'
        Debug.Log("Creating a 'physical' instance of " + item);
        GameObject Instance = Instantiate(PickupInstance, Position, Quaternion.identity);
        Instance.GetComponent<ItemPickupInstance>().Items = ItemStack.StackItemsSingular(item);
    }
    public void CreatePhysicalItem(ItemStack items, Vector3 Position)
    {
        //Create an instance of this item in the 'real world'
        Debug.Log("Creating a 'physical' instance of " + items.UpgradeDef + "x" + items.Count);
        GameObject Instance = Instantiate(PickupInstance, Position, Quaternion.identity);
        Instance.GetComponent<ItemPickupInstance>().Items = items;
    }
    public void SetFilter(ItemType type)
    {
        Filter = type;
        RedrawItems();
    }
    public string GetFilledFrac()
    {
        // 1/3
        return "Not implimented"; //$"{Items.Count} / {MaxStacks * StackSize}";
    }
    public string GetFilledPercent()
    {
        // 33.3%
        return "Not implimented"; //((float)Items.Count / (MaxStacks * StackSize) * 100f).ToString("n1");
    }
    public void SetAllowDisplay(bool set)
    {
        if (AllowDisplay != set)
        {
            AllowDisplay = set;
            RedrawItems();
        }
    }
    public List<ItemStack> RemoveStacks(List<ItemStack> stack)
    {
        //First off, see if we have these items.
        List<ItemStack> Remaining = ItemStack.SubtractStack(Items, stack);
        if(ItemStack.EvaluateStacks(Remaining).Count == 0) //Have these items.
        {
            //Our current inventory is now equal to the remaining.
            Items = Remaining;
            OnInventoryModified.Invoke();
            return stack;
        }
        else
        {
            //Fail
            //We do not.
            return null;
        }
    }
    public List<ItemStack> RemoveStacks(ItemStack stack)
    {
        //First off, see if we have these items.
        List<ItemStack> stck = new List<ItemStack>() { stack };
        List<ItemStack> Remaining = ItemStack.SubtractStack(Items, stck);
        if (ItemStack.EvaluateStacks(Remaining).Count == 0) //Have these items.
        {
            //Our current inventory is now equal to the remaining.
            Items = Remaining;
            OnInventoryModified.Invoke();
            return stck;
        }
        else
        {
            //Fail
            //We do not.
            return null;
        }
    }
    public void AddStacks(List<ItemStack> Input, out List<ItemStack> Overflow)
    {
        Items = ItemStack.AddStacks(Items, Input, MaxStacks, out Overflow);
    }
    void RedrawItems()
    {
        for (int i = 0; i < SlotParent.transform.childCount; i++)
        {
            Destroy(SlotParent.transform.GetChild(i).gameObject);
        }
        uiSlots.Clear();
        DisplayedStacks = ItemStack.FilterType(Items, Filter);
        if (!AllowDisplay) return;
        if (DisplayedStacks.Count == 0) return;
        for (int i = 0; i < DisplayedStacks.Count; i++)
        {
            InventoryUiSlot slot = new InventoryUiSlot();
            GameObject obj = Instantiate(InventorySlot, SlotParent.transform.position, SlotParent.rotation, SlotParent);
            obj.transform.localPosition = Vector3.zero;
            slot.Display = obj.transform.GetChild(0).GetComponent<Image>();
            slot.SlotText = obj.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>(); //Transform child out of bounds
            slot.SetSlot(DisplayedStacks[i]);
            uiSlots.Add(slot);
        }
    }
    public List<ItemStack> DisplayedStacks = new List<ItemStack>();
    public List<ItemPickupInstance> Pickupables = new List<ItemPickupInstance>();
    public InventoryItem QueryItem(string itemID)
    {
        InventoryItem item;
        ItemDictionary.TryGetValue(itemID, out item);
        return item;
    }
    public ItemStack QueryItemStack(string itemID, int Length)
    {
        InventoryItem item;
        ItemDictionary.TryGetValue(itemID, out item);
        ItemStack stack = new ItemStack();
        stack.UpgradeDef = item;
        stack.Count = Length;
        return stack;
    }
    public int GetItemIndex(InventoryItem item)
    {
        for (int i = 0; i < AllItems.Length; i++)
        {
            if (item == AllItems[i]) return i;
        }
        return -1;
    }
    #endregion
}
public class InventoryUiSlot
{
    public Image Display;
    public ItemStack item;
    public TextMeshProUGUI SlotText;
    public void SetSlot(ItemStack toset)
    {
        item = toset;
        SlotText.text = item.Count.ToString();
        Display.sprite = toset.UpgradeDef.DisplaySprite;
    }
}
//Add maximum stack size functionality.
[System.Serializable]
public class ItemStack
{
    public static bool ShowDialogue = true;
    public int Count;
    public InventoryItem UpgradeDef;
    //Stacks are correct.
    public static bool Contains(List<ItemStack> A, InventoryItem b)
    {
        //if a contains B
        for (int i = 0; i < A.Count; i++)
        {
            if (A[i].UpgradeDef == b) return true;
        }
        return false;
    }
    public static List<ItemStack> EvaluateStacks(List<ItemStack> Items)
    {
        List<ItemStack> Stacks = new List<ItemStack>();
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Count < 0) Stacks.Add(Items[i]); //This stack is negative!
        }
        return Stacks;
    }
    public static List<ItemStack> FilterType(List<ItemStack> Input, ItemType Type)
    {
        List<ItemStack> stacks = new List<ItemStack>();
        if (Type == ItemType.All)
        {
            for (int i = 0; i < Input.Count; i++)
            {
                stacks.Add(Input[i]);
            }
            return stacks;
        }
        else
        {
            for (int i = 0; i < Input.Count; i++)
            {
                if (Input[i].UpgradeDef.Type == Type) stacks.Add(Input[i]);
            }
            return stacks;
        }
    }
    public static List<ItemStack> SubtractStack(List<ItemStack> A, List<ItemStack> B) //Does A Contain B? (Does the Inventory (A) Contain the required items? (B).
    {
        List<ItemStack> itemStacks = new List<ItemStack>();
        for (int i = 0; i < A.Count; i++)
        {
            itemStacks.Add(A[i]);
        }
        for (int i = 0; i < B.Count; i++) //Loop through all items in B, see if they are contained && they have enough items.
        {
            ItemStack Stack = FindStackOfType(B[i].UpgradeDef, itemStacks);
            if (Stack != null)
            {
                //We have this stack.
                int Remaining = Stack.Count - B.Count;
                if (Remaining > 0)
                {
                    //Replace the actual stacks with a subtracted version of this stack.
                    Stack.Count = Remaining;
                }
                else if (Remaining == 0) itemStacks.Remove(Stack);
            }
        }
        return itemStacks;
    }
    public static ItemStack StackItemsSingular(InventoryItem item)
    {
        ItemStack stack = new ItemStack();
        stack.Count = 1;
        stack.UpgradeDef = item;
        return stack;
    }
    public static List<ItemStack> StackItems(List<InventoryItem> Items)
    {
        List<ItemStack> Stacks = new List<ItemStack>();
        //Create a stack of items with all the items provided.
        foreach (InventoryItem item in Items)
        {
            //Are there any stacks?
            if (Stacks.Count != 0)
            {
                int stackMatchindex = -1;
                //Is this item in any existing stacks?
                for (int i = 0; i < Stacks.Count; i++)
                {
                    if (Stacks[i].UpgradeDef == item && Stacks[i].Count < item.StackSize) stackMatchindex = i; //Match!
                }
                if (stackMatchindex != -1)
                {
                    //Yes
                    //Increment stack and continue.
                    Stacks[stackMatchindex].Count++;
                    continue;
                }
                else
                {
                    //No
                    //Create a new stack with the type of the current item.
                    ItemStack newStack = new ItemStack
                    {
                        Count = 1,
                        UpgradeDef = item
                    };
                    Stacks.Add(newStack);
                }
            }
            else
            {
                //No stacks, Create one with the type of the current item.
                ItemStack newStack = new ItemStack
                {
                    Count = 1,
                    UpgradeDef = item
                };
                Stacks.Add(newStack);
            }
        }
        //Return the created stacks.
        return Stacks;
    }
    public static ItemStack FindStackOfType(InventoryItem Type, List<ItemStack> stacks)
    {
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].UpgradeDef == Type) return stacks[i];
        }
        return null;
    }
    public static List<ItemStack> FilterItems(ItemType toFilter, List<ItemStack> Stacks)
    {
        List<ItemStack> StacksOfType = new List<ItemStack>();
        for (int i = 0; i < Stacks.Count; i++)
        {
            if(Stacks[i].UpgradeDef.Type == toFilter)
            {
                //Add this stack.
                StacksOfType.Add(Stacks[i]);
            }
        }
        return StacksOfType;
    }
    public static List<ItemStack> AddStacks(List<ItemStack> StackA, List<ItemStack> StackB, int MaxStacks, out List<ItemStack> Overflow)
    { 
        //Add another pass to condense all stacks as much as possible.
        List<ItemStack> StackC = new List<ItemStack>();
        Overflow = new List<ItemStack>();
        //Add A, iterate through B, check for duplicates & create multiple stacks if nessicary. (use same logic as createStacks).
        for (int i = 0; i < StackA.Count; i++)
        {
            StackC.Add(StackA[i]);
        }
        StackC = ExpandStacks(CondenseStacks(StackC));
        foreach (ItemStack item in StackB)
        {
            //Are there any stacks?
            if (StackC.Count != 0)
            {
                int stackMatchindex = -1;
                //Is this item in any existing stacks?
                for (int i = 0; i < StackC.Count; i++) //Iterate through existing stacks
                {
                    if (StackC[i].UpgradeDef == item.UpgradeDef && StackC[i].Count < item.UpgradeDef.StackSize && StackC.Count + 1 < MaxStacks)
                    {
                        stackMatchindex = i; //Match!
                    }
                }
                if (stackMatchindex != -1)
                {
                    //Yes
                    //Increment stack and continue.
                    StackC[stackMatchindex].Count++;
                    continue;
                }
                else
                {
                    //No
                    //Create a new stack with the type of the current item.
                    if(StackC.Count + 1 >= MaxStacks)
                    {
                        Overflow.Add(item);
                    }else StackC.Add(item);
                }
            }
            else
            {
                if (StackC.Count + 1 >= MaxStacks)
                {
                    Overflow.Add(item);
                }
                else StackC.Add(item);
            }
        }
        return StackC;
    }
    public static List<ItemStack> CondenseStacks(List<ItemStack> Input)
    {
        //Condenses all stacks, ignoring maximum size.
        List<ItemStack> Condensed = new List<ItemStack>();
        for (int i = 0; i < Input.Count; i++)
        {
            if(Condensed.Contains(Input[i]))
            {
                //Where?
                Condensed[Input.IndexOf(Input[i])].Count += Input[i].Count;
            }
            else
            {
                Condensed.Add(Input[i]);
            }
        }
        return Condensed;
    }
    public static List<ItemStack> ExpandStacks(List<ItemStack> Input)
    {
        //Expands all stacks to correlate with their size limits.
        List<ItemStack> Expanded = new List<ItemStack>();
        //So how do we do this?
        //Iterate through the input, use a while loop to iterate through the stacks. 
        for (int i = 0; i < Input.Count; i++)
        {
            int MaxIterations = 500000; //Just so i can sleep better.
            while(Input[i].Count != 0 && 0 < MaxIterations)
            {
                int Stacksize = Input[i].UpgradeDef.StackSize;
                if(Input[i].Count > Stacksize)
                {
                    ItemStack stack = new ItemStack()
                    {
                        Count = Stacksize,
                        UpgradeDef = Input[i].UpgradeDef
                    };
                    Expanded.Add(stack);
                    Input[i].Count -= Stacksize;
                }
                else
                {
                    ItemStack stack = new ItemStack()
                    {
                        Count = Stacksize,
                        UpgradeDef = Input[i].UpgradeDef
                    };
                    Expanded.Add(stack);
                    Input[i].Count = 0;
                }
                MaxIterations--;
            }
        }
        return Expanded;
    }
    public static List<ItemStack> SerialiseStack(List<ItemStackSerializeable> stackSerializeables)
    {
        List<ItemStack> stacks = new List<ItemStack>();
        foreach (ItemStackSerializeable ser in stackSerializeables)
        {
            ItemStack stack = new ItemStack();
            stack.Count = ser.Count;
            stack.UpgradeDef = Inventory.Singleton.AllItems[ser.UpgradeDefInt];
            stacks.Add(stack);
        }
        return stacks;
    }
}
[System.Serializable]
public class ItemStackSerializeable
{
    public static bool ShowDialogue = false;
    public int Count;
    public int UpgradeDefInt;
    //Stacks are correct.
    public static List<ItemStackSerializeable> StackItems(List<InventoryItem> Items)
    {
        List<ItemStackSerializeable> Stacks = new List<ItemStackSerializeable>();
        //Create a stack of items with all the items provided.
        foreach (InventoryItem item in Items)
        {
            //Are there any stacks?
            if (Stacks.Count != 0)
            {
                int stackMatchindex = -1;
                //Is this item in any existing stacks?
                for (int i = 0; i < Stacks.Count; i++)
                {
                    if (Stacks[i].UpgradeDefInt == Inventory.Singleton.GetItemIndex(item)) stackMatchindex = i; //Match!
                }
                if (stackMatchindex != -1)
                {
                    //Yes
                    //Increment stack and continue.
                    Stacks[stackMatchindex].Count++;
                    continue;
                }
                else
                {
                    //No
                    //Create a new stack with the type of the current item.
                    ItemStackSerializeable newStack = new ItemStackSerializeable
                    {
                        Count = 1,
                        UpgradeDefInt = Inventory.Singleton.GetItemIndex(item),
                    };
                    Stacks.Add(newStack);
                }
            }
            else
            {
                //No stacks, Create one with the type of the current item.
                ItemStackSerializeable newStack = new ItemStackSerializeable
                {
                    Count = 1,
                    UpgradeDefInt = Inventory.Singleton.GetItemIndex(item),
                };
                Stacks.Add(newStack);
            }
        }
        //Return the created stacks.
        return Stacks;
    }
    public static List<ItemStackSerializeable> DesetialiseStack(List<ItemStack> stacks)
    {
        List<ItemStackSerializeable> serializeables = new List<ItemStackSerializeable>();
        foreach (ItemStack stack in stacks)
        {
            ItemStackSerializeable serializeable = new ItemStackSerializeable();
            serializeable.Count = stack.Count;
            serializeable.UpgradeDefInt = Inventory.Singleton.GetItemIndex(stack.UpgradeDef);
        }
        return serializeables;
    }
}
//<Summary>
//A serializeable version of the Itemstack class, 
//Items are referenced as indexes, these indexes correspond to items within the AllItems array in the Database.
//</Summary>