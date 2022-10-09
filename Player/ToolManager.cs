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
        if (Input.GetKeyDown(KeyCode.R)) ChangeTool(1);
        if(Input.GetKeyDown(KeyCode.Alpha1)) ChangeTool(-CurrentTool);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeTool(-CurrentTool + 1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeTool(-CurrentTool + 2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeTool(-CurrentTool + 3);
    }
    void ChangeTool(int Change)
    {
        CurrentTool += Change;
        if (CurrentTool >= Tools.Count) CurrentTool = 0;
        if (CurrentTool <= Tools.Count - 1) CurrentTool = Tools.Count - 1;
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
