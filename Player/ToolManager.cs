using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public List<string> ToolsAvaliable = new List<string>();
    public List<GameObject> Tools = new List<GameObject>();
    public int CurrentTool;
    public Transform ToolParent;
    //just use the R method.
    public bool ToolsEnabled;
    private void Start()
    {
        //No need for a child check.
        CreateTools(); 
        UpdateTools();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) SetTool(CurrentTool + 1);
        if(Input.GetKeyDown(KeyCode.Alpha1)) SetTool(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTool(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTool(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTool(3);
    }
    void SetTool(int Index)
    {
        if (Index < Tools.Count - 1 && Index > -1) CurrentTool = Index;
        else CurrentTool = 0;
        UpdateTools();
    }
    void CreateTools()
    {
        Tools.Clear();
        for (int i = 0; i < ToolParent.childCount; i++)
        {
            Destroy(ToolParent.GetChild(i).gameObject);
        }
        for (int i = 0; i < ToolsAvaliable.Count; i++)
        {
            object obj = Resources.Load(ToolsAvaliable[i]);
            if (obj != null)
            {
                Tools.Add(Instantiate((GameObject)obj, ToolParent.position, ToolParent.rotation, ToolParent));
            }
        }
    }
    void UpdateTools()
    {
        //Update tool states.
        for (int i = 0; i < ToolParent.childCount; i++)
        {
            bool state = i == CurrentTool && ToolsEnabled;
            if (state != ToolParent.GetChild(i).gameObject.activeInHierarchy)
            {
                ToolParent.GetChild(i).gameObject.SetActive(state);
            }
        }
    }
}
