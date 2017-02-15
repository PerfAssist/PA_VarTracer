﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GraphItVariableBody
{
    Dictionary<string, Color> m_eventColors = new Dictionary<string, Color>();
    public Dictionary<string, Color> EventColors
    {
        get { return m_eventColors; }
        set { m_eventColors = value; }
    }
    private string channelName;
    public string ChannelName
    {
        get { return channelName; }
        set { channelName = value; }
    }

    private string m_variableBodyName;
    public string VariableBodyName
    {
        get { return m_variableBodyName; }
        set { m_variableBodyName = value; }
    }

    public GraphItVariableBody(string varBodyName)
    {
        m_variableBodyName = varBodyName;
    }

    private Dictionary<string, GraphItVariable> m_variableDict = new Dictionary<string, GraphItVariable>();
    public Dictionary<string, GraphItVariable> VariableDict
    {
        get { return m_variableDict; }
        set { m_variableDict = value; }
    }

    //eventName  eventData
    Dictionary<string, List<EventData>> eventInfos = new Dictionary<string, List<EventData>>();
    public Dictionary<string, List<EventData>> EventInfos
    {
        get { return eventInfos; }
        set { eventInfos = value; }
    }

    public void RegistEvent(string eventName,Color color)
    {
        if(string.IsNullOrEmpty(eventName))
            return ;
        if (!eventInfos.ContainsKey(eventName))
            eventInfos[eventName] = new List<EventData>();
        if(!m_eventColors.ContainsKey(eventName))
        {
            m_eventColors[eventName] = color;
        }
    }
}
