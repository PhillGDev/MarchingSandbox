using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
public class VisorOS : MonoBehaviour
{
    //Like the NotifcationManager, but with added bonuses.
    public TextMeshProUGUI DetailText; //For displaying information that will be continously updated, such as:
    public TextMeshProUGUI ConsoleText; //For events, Such as:
    public static VisorOS Singleton;
    public TMP_InputField ConsoleInput;
    public string GetTyped => ConsoleInput.text;
    public bool CommandsEnabled;
    #region Display
    public void Awake()
    {
        Singleton = this;
        ConsoleInput.onEndEdit.AddListener(OnEnterData);
        VisorCommand HELP = new VisorCommand("HELP", "Displays all avaliable commands", out UnityEvent OnRequestHelp);
        OnRequestHelp.AddListener(HelpCommand);
    }
    void HelpCommand()
    { 
        Singleton.RegisterEvent("Displaying HELP...");
        foreach(KeyValuePair<string, VisorCommand> Command in Commands)
        {
            Singleton.RegisterEvent($"'{Command.Key}',");
        }
    }
    private void Start()
    {
        InvokeRepeating(nameof(RunningTick), 0f, .5f);
        ConsoleText.text = "";
    }
    string SpinString = "*...";
    void RunningTick()
    {
        //For progressing the little spinning line.
         switch (SpinString)
         {
            case "*...": SpinString = ".*.."; break;
            case ".*..": SpinString = "..*."; break;
            case "..*.": SpinString = "...*"; break;
            case "...*": SpinString = "*..."; break;    
         }
        DetailsDirty = true;
    }
    string GetDetail()
    {
        string Detail = "";
        Detail += "VisorOS Running: " + SpinString + "\n";
        if (Details.Count == 0) Detail += "No Information\n";
        else
        {
            for (int i = 0; i < Details.Count; i++)
            {
                Detail += Details[i] + "\n";
            }
        }
        return Detail;
    }
    bool DetailsDirty, ConsoleDirty;
    private void Update()
    {
        if (DetailsDirty) { DetailsDirty = false; DetailText.text = GetDetail(); }
        if (ConsoleDirty) 
        {
            ConsoleDirty = false;
            for (int i = 0; i < AllEvnts.Count; i++)
            {
                ConsoleText.text += AllEvnts[i].EventDetail;
            }
        }
    }
    public void RegisterEvent(string EventDetail)
    {
        StartCoroutine(RegisterEventString(EventDetail));
    }
    public void RegisterEvent(string EventDetail, string Type)
    {
        ConsoleDirty = true;
        StartCoroutine(RegisterEventString(EventDetail));
    }
    IEnumerator RegisterEventString(string EventDetail)
    {
        VisorEvent Vevent = new VisorEvent("");
        Events.Enqueue(Vevent);
        AllEvnts.Add(Vevent);
        if (Events.Count > 8)
        {
            for (int i = 0; i < AllEvnts.Count; i++)
            {
                if (AllEvnts[i].id == Events.Peek().id) AllEvnts.RemoveAt(i);
            }
            Events.Dequeue();
        }
        string targetText = "\n" + EventDetail;
        for (int i = 0; i < targetText.Length; i++)
        {
            if (targetText[i] != " "[0]) yield return new WaitForSeconds(.025f);
            if (targetText[i] == "."[0]) yield return new WaitForSeconds(.1f);
            if (targetText[i] == ","[0]) yield return new WaitForSeconds(.1f);
            Vevent.EventDetail += targetText[i];
        }
    }
    public void AddDetail(string Detail, MonoBehaviour ScriptInvoked)
    {
        DetailsDirty = true;
        Details.Add(Detail);
        Inputs.Add(ScriptInvoked);
    }
    public void UpdateDetail(string NewDetail, MonoBehaviour ScriptInvoked)
    {
        DetailsDirty = true;
        if (Inputs.Contains(ScriptInvoked))
        {
            for (int i = 0; i < Inputs.Count; i++)
            { 
                if(Inputs[i] == ScriptInvoked)Details[i] = NewDetail;
            }
        }
        else Debug.LogWarning($"A script on {ScriptInvoked.name} Has Attempted to update a detail without adding itself to the details list (Attempted Set to: {NewDetail})");
    }
    public void RemoveDetail(MonoBehaviour Invoked)
    {
        if(Inputs.Contains(Invoked))
        {
            for (int i = 0; i < Inputs.Count; i++)
            {
                if (Inputs[i] == Invoked)
                {
                    Inputs.RemoveAt(i);
                    Details.RemoveAt(i);
                }
            }
        }
    }
    public List<string> Details = new List<string>();
    public List<MonoBehaviour> Inputs = new List<MonoBehaviour>();
    Queue<VisorEvent> Events = new Queue<VisorEvent>();
    List<VisorEvent> AllEvnts = new List<VisorEvent>();
    class VisorEvent
    {
        public string EventDetail;
        public int id;
        public VisorEvent(string EventDetail)
        {
            id = GetHashCode();
            this.EventDetail = EventDetail;
        }
    }
    #endregion
    #region Console
    public void OnEnterData(string input)
    {
        if(!CommandsEnabled)
        {
            CommandsEnabled = true;
            return;
        }
        RegisterEvent(input);
        Commands.TryGetValue(input, out VisorCommand RecognisedCommand);
        if (RecognisedCommand != null)
        {
            Debug.Log("Recognised Command.");
            RecognisedCommand.OnInvokedEvent.Invoke();
        }
        bool FoundHelp = false;
        if(RecognisedCommand == null)
        {
            foreach(KeyValuePair<string, VisorCommand> keyValuePair in Commands)
            {
                if("HELP " + keyValuePair.Key == input)
                {
                    //Player has requested help on this specific command.
                    DisplayHelp(keyValuePair.Value);
                    FoundHelp = true;
                }
            }
        }
        if(!FoundHelp && RecognisedCommand == null) RegisterEvent($"'{input}' Is not recognised as a command.");
        ConsoleInput.SetTextWithoutNotify("");
        //EquipmentSystem.Singleton.Typing = false;
    }
    void DisplayHelp(VisorCommand command)
    {
        RegisterEvent($"{command.Invoke} \n{command.Desc}.");
    }
    public void AddCommand(VisorCommand command)
    {
        Commands.Add(command.Invoke, command);
    }
    public Dictionary<string, VisorCommand> Commands = new Dictionary<string, VisorCommand>();
    #endregion
}
public class VisorCommand
{
    public string Invoke, Desc;
    public UnityEvent OnInvokedEvent;
    public VisorCommand(string Invoke, string Desc, out UnityEvent OnCommandInvoked)
    {
        this.Invoke = Invoke;
        this.Desc = Desc;
        UnityEvent Invoked = new UnityEvent();
        OnInvokedEvent = Invoked;
        VisorOS.Singleton.AddCommand(this);
        OnCommandInvoked = Invoked;
    }
}

