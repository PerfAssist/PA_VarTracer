using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public struct VariableParm
{
    public string VariableName;
    public float VariableValue;
};

public struct EventParm
{
    public string EventName;
    public float EventDuration;
};

public class VarTracerCmdCacher
{
    private Dictionary<string, NamePackage> _groupCmdPackage
        = new Dictionary<string, NamePackage>();
    public Dictionary<string, NamePackage> GroupCmdPackage
    {
        get { return _groupCmdPackage; }
    }

    public void SendEvent(string groupName, string eventName, float duration)
    {
        var group = GetGroupByName(groupName);
        if (group == null)
            return;
        var eventParm = GetEventParmByName(group,eventName);
        if (eventParm == null)
            return;
        eventParm._stamp = VarTracerUtils.GetTimeStamp();
        eventParm._duration = duration;
    }

    public void SendVariable(string groupName, string variableName, float value)
    {
        var group = GetGroupByName(groupName);
        if (group == null)
            return;

        var varParm = GetVariableParmByName(group, variableName);
        if (varParm == null)
            return;
        varParm._stamp = VarTracerUtils.GetTimeStamp();
        varParm._value = value;
    }

    private EventCmdParam GetEventParmByName(NamePackage group, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (!group.EventDict.ContainsKey(name))
        {
            var cmdParm = new EventCmdParam();
            group.EventDict.Add(name, new CacheList<EventCmdParam>(cmdParm));
            return cmdParm;
        }

        var reuseObj = group.EventDict[name].CheckReuse();
        if (reuseObj != null)
            return reuseObj as EventCmdParam;

        var parm = new EventCmdParam();
        group.EventDict[name].VarChacheList.Add(parm);
        group.EventDict[name].UseIndex++;
        return parm;
    }

    private VariableCmdParam GetVariableParmByName(NamePackage group, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (!group.VariableDict.ContainsKey(name))
        {
            var cmdParm = new VariableCmdParam();
            group.VariableDict.Add(name, new CacheList<VariableCmdParam>(cmdParm));
            return cmdParm;
        }

        var reuseObj = group.VariableDict[name].CheckReuse();
        if(reuseObj != null)
            return reuseObj as VariableCmdParam;
            
        var parm = new VariableCmdParam();
        group.VariableDict[name].VarChacheList.Add(parm);
        group.VariableDict[name].UseIndex++;
        return parm;
    }

    private NamePackage GetGroupByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (!_groupCmdPackage.ContainsKey(name))
            _groupCmdPackage.Add(name,new NamePackage());

        return _groupCmdPackage[name];
    }

    public void Clear()
    {
        foreach (var package in _groupCmdPackage)
        {
            package.Value.Reset();
        }
    }

    public int GetUsedGroupCount()
    {
        int groupCount = 0;
        foreach (var package in _groupCmdPackage)
        {
            if (package.Value.IsUse())
            {
                groupCount++;
            }
        }
        return groupCount;
    }

    public int GetUsedVariableCount(NamePackage packet)
    {
        int variableCount = 0;
        foreach (var list in packet.VariableDict.Values)
        {
            if (list.IsUse())
            {
                variableCount++;
            }
        }
        return variableCount;
    }

    public int GetUsedEventCount(NamePackage packet)
    {
        int eventCount = 0;
        foreach (var list in packet.EventDict.Values)
        {
            if (list.IsUse())
            {
                eventCount++;
            }
        }
        return eventCount;
    }
}

public class NamePackage
{
    private Dictionary<string, CacheList<VariableCmdParam>> _variableDict
        = new Dictionary<string, CacheList<VariableCmdParam>>();

    public Dictionary<string, CacheList<VariableCmdParam>> VariableDict
    {
        get { return _variableDict; }
        set { _variableDict = value; }
    }

    private Dictionary<string, CacheList<EventCmdParam>> _eventDict
    = new Dictionary<string, CacheList<EventCmdParam>>();
    public Dictionary<string, CacheList<EventCmdParam>> EventDict
    {
        get { return _eventDict; }
        set { _eventDict = value; }
    }

    public void Reset() {
        foreach (var list in _variableDict.Values)
            list.UseIndex = 0;

        foreach (var list in _eventDict.Values)
            list.UseIndex = 0;
    }

    public bool IsUse()
    {
        foreach (var list in _variableDict.Values)
        {
            if (list.UseIndex > 0)
                return true;
        }
        foreach (var list in _eventDict.Values)
        {
            if (list.UseIndex > 0)
                return true;
        }
        return false;
    }
}

public class CacheList<T>
{
    int _useIndex = 0;
    public CacheList()
    {
    }
    public CacheList (T t)
    {
        _varChacheList.Add(t);
        _useIndex++;
    }

    public int UseIndex
    {
        get { return _useIndex; }
        set { _useIndex = value; }
    }
    private List<T> _varChacheList = new List<T>();

    public List<T> VarChacheList
    {
        get { return _varChacheList; }
        set { _varChacheList = value; }
    }

    public object CheckReuse()
    {
        if (_varChacheList.Count > UseIndex)
            return _varChacheList[UseIndex++];
        return null;        
    }

    public bool IsUse()
    {
        return _useIndex >0;
    }
}

public class VariableCmdParam
{
    public long  _stamp=0;
    public float _value=0.0f;
}

public class EventCmdParam
{
    public long _stamp = 0;
    public float _duration = 0.0f;
}

