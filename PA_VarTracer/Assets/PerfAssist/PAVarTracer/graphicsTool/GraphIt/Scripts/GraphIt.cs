﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GraphItDataInternal
{
    public GraphItDataInternal( int subgraph_index )
    {
        mDataPoints = new float[GraphItData.DEFAULT_SAMPLES];
        mCounter = 0.0f;
        mMin = 0.0f;
        mMax = 0.0f;
        mAvg = 0.0f;
        mFastAvg = 0.0f;
        mCurrentValue = 0.0f;
        switch(subgraph_index)
        {
            case 0:
                mColor = new Color( 0, 0.85f, 1, 1);
                break;
            case 1:
                mColor = Color.yellow;
                break;
            case 2:
                mColor = Color.green;
                break;
            case 3:
                mColor = Color.cyan;
                break;
            default:
                mColor = Color.gray;
                break;
        }
    }
    public float[] mDataPoints;
    public float mCounter;
    public float mMin;
    public float mMax;
    public float mAvg;
    public float mFastAvg;
    public float mCurrentValue;
    public Color mColor;
}

public class EventData
{
    public string m_eventName;
    public int m_eventFrameIndex;
    public EventData(int eventFrameIndex,string eventName)
    {
        m_eventFrameIndex = eventFrameIndex;
        m_eventName = eventName;
    }
}

public class GraphItData
{
    public const int DEFAULT_SAMPLES = 1048;
    public const int RECENT_WINDOW_SIZE = 120;
    
    public Dictionary<string, GraphItDataInternal> mData = new Dictionary<string, GraphItDataInternal>();

    public string mName;

    public int mCurrentIndex;
    public bool mInclude0;
    public bool mIsNormalized;

    public List<EventData> mEventData = new List<EventData>();

    public bool mReadyForUpdate;
    public bool mFixedUpdate;

    public int mWindowSize;
    public bool mFullArray;

    public bool mSharedYAxis;

    protected bool mHidden;
    protected float mHeight;

    public int mTotalIndex = 0;


    public GraphItData( string name)
    {
        mName = name;

        mData = new Dictionary<string, GraphItDataInternal>();

        mCurrentIndex = 0;
        mTotalIndex = 0;

        mInclude0 = true;

        mReadyForUpdate = false;
        mFixedUpdate = false;

        mWindowSize = DEFAULT_SAMPLES;
        mFullArray = false;

        mSharedYAxis = false; 
        mHidden = false;
        mHeight = 175;


        if (PlayerPrefs.HasKey(mName + "_height"))
        {
            SetHeight(PlayerPrefs.GetFloat(mName + "_height"));
        }
    }

    public int GraphLength()
    {
        if (mFullArray)
        {
            return GraphFullLength();
        }
        return mCurrentIndex;
    }

    public int GraphFullLength()
    {
        return mWindowSize;
    }

    public float GetMin( string subgraph )
    {
        if (mSharedYAxis)
        {
            bool min_set = false;
            float min = 0;
            foreach (KeyValuePair<string, GraphItDataInternal> entry in mData)
            {
                GraphItDataInternal g = entry.Value;
                if (!min_set)
                {
                    min = g.mMin;
                    min_set = true;
                }
                min = Math.Min(min, g.mMin);
            }
            return min;
        }
        else
        {
            if (!mData.ContainsKey(subgraph))
            {
                mData[subgraph] = new GraphItDataInternal(mData.Count);
            }
            return mData[subgraph].mMin;
        }
    }

    public float GetMax( string subgraph )
    {
        if (mSharedYAxis)
        {
            bool max_set = false;
            float max = 0;
            foreach (KeyValuePair<string, GraphItDataInternal> entry in mData)
            {
                GraphItDataInternal g = entry.Value;
                if (!max_set)
                {
                    max = g.mMax;
                    max_set = true;
                }
                max = Math.Max(max, g.mMax);
            }
            return max;
        }
        else
        {
            if (!mData.ContainsKey(subgraph))
            {
                mData[subgraph] = new GraphItDataInternal(mData.Count);
            }
            return mData[subgraph].mMax;
        }
    }

    public float GetHeight()
    {
        return mHeight;
    }
    public void SetHeight( float height )
    {
        mHeight = height;
    }
    public void DoHeightDelta(float delta)
    {
        SetHeight( Mathf.Max(mHeight + delta, 50) );
        PlayerPrefs.SetFloat( mName+"_height", GetHeight() );
    }

}

public class GraphItVar : MonoBehaviour
{

#if UNITY_EDITOR
    public const string BASE_GRAPH = "base";
    public const string VERSION = "1.2.0";
    public Dictionary<string, GraphItData> Graphs = new Dictionary<string, GraphItData>();
    public Dictionary<string, GraphItVariableBody> VariableBodys = new Dictionary<string, GraphItVariableBody>();

    static GraphItVar mInstance = null;
#endif

    public static GraphItVar Instance
    {
        get
        {
#if UNITY_EDITOR
            if( mInstance == null )
            {
                GameObject go = new GameObject("GraphIt");
                go.hideFlags = HideFlags.HideAndDontSave;
                mInstance = go.AddComponent<GraphItVar>();
            }
            return mInstance;
#else
            return null;
#endif
        }
    }
        
    void StepGraphInternal(GraphItData graph)
    {
#if UNITY_EDITOR
        foreach (KeyValuePair<string, GraphItDataInternal> entry in graph.mData)
        {
            GraphItDataInternal g = entry.Value;

            g.mDataPoints[graph.mCurrentIndex] = g.mCounter;
            g.mCounter = 0.0f;
        }

        graph.mTotalIndex++;
        graph.mCurrentIndex = (graph.mCurrentIndex + 1) % graph.mWindowSize;
        if (graph.mCurrentIndex == 0)
        {
            graph.mFullArray = true;
        }

        foreach (KeyValuePair<string, GraphItDataInternal> entry in graph.mData)
        {
            GraphItDataInternal g = entry.Value;

            float sum = g.mDataPoints[0];
            float min = g.mDataPoints[0];
            float max = g.mDataPoints[0];
            for (int i = 1; i < graph.GraphLength(); ++i)
            {
                sum += g.mDataPoints[i];
                min = Mathf.Min(min,g.mDataPoints[i]);
                max = Mathf.Max(max,g.mDataPoints[i]);
            }
            if (graph.mInclude0)
            {
                min = Mathf.Min(min, 0.0f);
                max = Mathf.Max(max, 0.0f);
            }

            //Calculate the recent average
            int recent_start = graph.mCurrentIndex - GraphItData.RECENT_WINDOW_SIZE;
            int recent_count = GraphItData.RECENT_WINDOW_SIZE;
            if (recent_start < 0)
            {
                if (graph.mFullArray)
                {
                    recent_start += g.mDataPoints.Length;
                }
                else
                {
                    recent_count = graph.GraphLength();
                    recent_start = 0;
                }
            }

            float recent_sum = 0.0f;
            for (int i = 0; i < recent_count; ++i)
            {
                recent_sum += g.mDataPoints[recent_start];
                recent_start = (recent_start + 1) % g.mDataPoints.Length;
            }

            if (graph.mIsNormalized)
            {
                min = 0;
                max = 1;
            }

            g.mMin = min;
            g.mMax = max;
            g.mAvg = sum / graph.GraphLength();
            g.mFastAvg = recent_sum / recent_count;
        }
#endif
    }

    // Update is called once per frame
    void LateUpdate()
    {
#if UNITY_EDITOR
        foreach (KeyValuePair<string, GraphItData> kv in Graphs)
        {
            GraphItData g = kv.Value;
            if (g.mReadyForUpdate && !g.mFixedUpdate)
            {
                StepGraphInternal(g);
            }
        }
#endif
    }

    // Update is called once per fixed frame
    void FixedUpdate()
    {
#if UNITY_EDITOR
        foreach (KeyValuePair<string, GraphItData> kv in Graphs)
        {
            GraphItData g = kv.Value;
            if (g.mReadyForUpdate && g.mFixedUpdate )
            {
                StepGraphInternal(g);
            }
        }
#endif
    }
    

    /// <summary>
    /// Optional setup function that allows you to specify both the inclusion of Y-axis 0, and how many samples to track.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="include_0"></param>
    /// <param name="sample_window"></param>
    public static void GraphSetup(string graph, bool include_0, int sample_window)
    {
#if UNITY_EDITOR
        GraphSetupInclude0(graph, include_0);
        GraphSetupSampleWindowSize(graph, sample_window);
#endif
    }
    
    /// <summary>
    /// Optional setup function that allows you to specify both the inclusion of Y-axis 0.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    /// <param name="include_0"></param>
    public static void GraphSetupInclude0(string graph, bool include_0)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.mInclude0 = include_0;
#endif
    }

    /// <summary>
    /// Optional setup function that allows you to specify the initial height of a graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    /// <param name="height"></param>
    public static void GraphSetupHeight(string graph, float height)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.SetHeight(height);
#endif
    }

    /// <summary>
    /// Optional setup function that allows you to specify how many samples to track.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="sample_window"></param>
    public static void GraphSetupSampleWindowSize(string graph, int sample_window)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        int samples = Math.Max(sample_window, GraphItData.RECENT_WINDOW_SIZE + 1);
        g.mWindowSize = samples;
        foreach (KeyValuePair<string, GraphItDataInternal> entry in g.mData)
        {
            GraphItDataInternal _g = entry.Value;
            _g.mDataPoints = new float[samples];
        }
#endif
    }

    /// <summary>
    /// Optional setup function that allows you to specify the color of the graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="color"></param>
    public static void GraphSetupColour(string graph, Color color)
    {
#if UNITY_EDITOR
        GraphSetupColour(graph, BASE_GRAPH, color);
#endif
    }

    /// <summary>
    /// Optional setup function that allows you to specify the color of the subgraph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    /// <param name="color"></param>
    public static void GraphSetupColour(string graph, string subgraph, Color color)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        if (!g.mData.ContainsKey(subgraph))
        {
            g.mData[subgraph] = new GraphItDataInternal(g.mData.Count);
        }
        g.mData[subgraph].mColor = color;
#endif
    }

    /// <summary>
    /// Log floating point data for this frame. Mutiple calls to this with the same graph will add logged values together.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="f"></param>
    public static void Log(string graph, float f)
    {
#if UNITY_EDITOR
        Log(graph, BASE_GRAPH, f);
#endif
    }

    /// <summary>
    /// Log floating point data for this frame. Mutiple calls to this with the same graph will add logged values together.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    /// <param name="f"></param>
    public static void Log(string graph, string subgraph, float f)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        if (!g.mData.ContainsKey(subgraph))
        {
            g.mData[subgraph] = new GraphItDataInternal(g.mData.Count);
        }
        g.mData[subgraph].mCounter += f;

        g.mReadyForUpdate = true;
#endif
    }

    /// <summary>
    /// Log a value of one to the specified graph. This can be used for counting occurances of a code path in a frame.
    /// </summary>
    /// <param name="graph"></param>
    public static void Log(string graph)
    {
#if UNITY_EDITOR
        Log(graph, BASE_GRAPH, 1.0f);
#endif
    }

    /// <summary>
    /// Log a value of one to the specified graph. This can be used for counting occurances of a code path in a frame.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    public static void Log(string graph, string subgraph)
    {
#if UNITY_EDITOR
        Log(graph, subgraph, 1.0f);
#endif
    }
    
    /// <summary>
    /// Log floating point data for this fixed frame. Mutiple calls to this with the same graph will add logged values together.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="f"></param>
    public static void LogFixed(string graph, float f)
    {
#if UNITY_EDITOR
        LogFixed(graph, BASE_GRAPH, f);
#endif
    }

    /// <summary>
    /// Log floating point data for this fixed frame. Mutiple calls to this with the same graph will add logged values together.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    /// <param name="f"></param>
    public static void LogFixed(string graph, string subgraph, float f)
    {
#if UNITY_EDITOR
        Log(graph, subgraph, f);
        Instance.Graphs[graph].mFixedUpdate = true;
#endif
    }

    /// <summary>
    /// Log an event to the specified graph this fixed frame. This can be used for counting occurances of a code path in a frame.
    /// </summary>
    /// <param name="graph"></param>
    public static void LogFixed(string graph)
    {
#if UNITY_EDITOR
        LogFixed(graph, BASE_GRAPH, 1.0f);
#endif
    }

    /// <summary>
    /// Log an event to the specified graph/subgraph this fixed frame. This can be used for counting occurances of a code path in a frame.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="subgraph"></param>
    public static void LogFixed(string graph, string subgraph)
    {
#if UNITY_EDITOR
        LogFixed(graph, subgraph, 1.0f);
#endif
    }

    /// <summary>
    /// StepGraph allows you to step this graph to the next frame manually. This is useful if you want to log multiple frames worth of data on a single frame.
    /// </summary>
    /// <param name="graph"></param>
    public static void StepGraph(string graph)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }
        Instance.StepGraphInternal(Instance.Graphs[graph]);
#endif
    }

    /// <summary>
    /// Allows you to manually pause a graph. The graph will unpause as soon as you Log new data to it, or call UnpauseGraph.
    /// </summary>
    /// <param name="graph"></param>
    public static void PauseGraph(string graph)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.mReadyForUpdate = false;
#endif
    }

    /// <summary>
    /// Allows you to manually unpause a graph. Graphs are paused initially until you Log data to them.
    /// </summary>
    /// <param name="graph"></param>
    public static void UnpauseGraph(string graph)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.mReadyForUpdate = true;
#endif
    }

    /// <summary>
    /// Allows you to switch between sharing the y-axis on a graph for all subgraphs, or for them to be independent.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="shared_y_axis"></param>
    public static void ShareYAxis(string graph, bool shared_y_axis)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.mSharedYAxis = shared_y_axis;
#endif
    }


    public static void NormalizedGraph(string graph, bool isNormalized)
    {
#if UNITY_EDITOR
        if (!Instance.Graphs.ContainsKey(graph))
        {
            Instance.Graphs[graph] = new GraphItData(graph);
        }

        GraphItData g = Instance.Graphs[graph];
        g.mIsNormalized = isNormalized;
#endif
    }

    public static void SetGraphEvent(string graph, string eventName)
    {
#if UNITY_EDITOR
        if (Instance.Graphs.ContainsKey(graph))
        {
            GraphItData g = Instance.Graphs[graph];
            g.mEventData.Add(new EventData(g.mTotalIndex, eventName));
        }
#endif
    }

    public static void DefineVariable(string variableName,string variableBody, Color color)
    {
#if UNITY_EDITOR
        foreach (var varBody in Instance.VariableBodys.Values)
        { 
            if(varBody.VariableDict.ContainsKey(variableName))
            {
                Debug.LogFormat("variableName {0} ,Already Exsit!", variableName);
                return;
            }
        }
        
        if (!Instance.VariableBodys.ContainsKey(variableBody))
        {
            var body =new GraphItVariableBody(variableBody);
            body.VariableDict[variableName] = new GraphItVariable(variableName);
            Instance.VariableBodys[variableBody] = body;
        }

        var variableDict =Instance.VariableBodys[variableBody].VariableDict;
        if(!variableDict.ContainsKey(variableName))
        {
            variableDict[variableName] = new GraphItVariable(variableName);
        }
        variableDict[variableName].Color = color;
#endif
    }

    public static void DefineEvent(string eventName,string variableBody)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(eventName))
            return;
        if (!Instance.VariableBodys.ContainsKey(variableBody))
        {
            var body = new GraphItVariableBody(variableBody);
            Instance.VariableBodys[variableBody] = body;
        }

        Instance.VariableBodys[variableBody].registEvent(eventName);
#endif
    }

    public static void SendEvent(string eventName)
    {
        List<string> needChannel = new List<string>();
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(eventName))
            return;
        foreach(var variableBodys in Instance.VariableBodys.Values)
        {
            if(variableBodys.RegistEventList.ContainsKey(eventName))
            {
                foreach(var var in variableBodys.VariableDict.Values)
                {
                    foreach(var channelName in var.ChannelDict.Keys)
                    {
                        if (!needChannel.Contains(channelName))
                            needChannel.Add(channelName);
                    }
                }
            }
        }
        
        foreach(string channelName  in needChannel)
        {
            SetGraphEvent(channelName,eventName); 
        }
#endif
    }

    public static void AttachVariable(string variableName, string ChannelName)
    {
#if UNITY_EDITOR
        foreach (var VarBody in Instance.VariableBodys.Values)
        {
            if(VarBody.VariableDict.ContainsKey(variableName))
            {
                var var = VarBody.VariableDict[variableName];
                var.AttchChannel(ChannelName);
            }
        }
#endif
    }

    public static void UpdateVariable(string variableName,float value)
    {
#if UNITY_EDITOR
        foreach (var VarBody in Instance.VariableBodys.Values)
        {
            if (VarBody.VariableDict.ContainsKey(variableName))
            {
                var var = VarBody.VariableDict[variableName];
                var.InsertValue(value);
            }
        }
#endif
    }

    public static void DefineVisualChannel(string channel, float height, bool isShareY = false, bool isNormalized = false)
    {
        GraphSetupHeight(channel, height);
        ShareYAxis(channel, isShareY);
        NormalizedGraph(channel, isNormalized);
    }

    public static GraphItVariable GetGraphItVariableByVariableName(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

#if UNITY_EDITOR
        foreach (var VarBody in Instance.VariableBodys.Values)
        {
            if (VarBody.VariableDict.ContainsKey(variableName))
            {
                var var = VarBody.VariableDict[variableName];
                return var;
            }
        }
#endif    
        return null;
    }


    public static bool IsVariableOnShow(string  variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;

        var graphVar = GetGraphItVariableByVariableName(variableName);
        if (graphVar == null)
            return false;

        if (graphVar.ChannelDict.Count > 0)
            return true;

        return false;
    }

    public static void AddChannel()
    {
        string newChannelName = Instance.Graphs.Count.ToString();
        DefineVisualChannel(newChannelName,200,true);
    }

    public static void RemoveChannel()
    {
        if (Instance.Graphs.Count <= 1)
            return;

        string removeChannelName = (Instance.Graphs.Count-1).ToString();
#if UNITY_EDITOR
        foreach (var VarBody in Instance.VariableBodys.Values)
        {
            foreach(var var in VarBody.VariableDict.Values)
            {
                var.DetachChannel(removeChannelName);
            }
        }
        Instance.Graphs.Remove(removeChannelName);
#endif
    }
}
