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

    public List<EventData> mEventData = new List<EventData>();

    public bool mReadyForUpdate;
    public bool mFixedUpdate;

    public int mWindowSize;
    public bool mFullArray;

    public float m_maxValue;

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
        return 0;
    }

    public float GetMax( string subgraph )
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

        int resultValue = 1;
        while (resultValue<max)
        {
            resultValue *= 10;
        }
        m_maxValue = resultValue;
        return resultValue;
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

public class VarTracer : MonoBehaviour
{

#if UNITY_EDITOR
    public const string BASE_GRAPH = "base";
    public const string VERSION = "1.2.0";
    public Dictionary<string, GraphItData> Graphs = new Dictionary<string, GraphItData>();
    public Dictionary<string, GraphItVariableBody> VariableBodys = new Dictionary<string, GraphItVariableBody>();

    static VarTracer mInstance = null;
#endif

    public static VarTracer Instance
    {
        get
        {
#if UNITY_EDITOR
            if( mInstance == null )
            {
                GameObject go = new GameObject("GraphIt");
                go.hideFlags = HideFlags.HideAndDontSave;
                mInstance = go.AddComponent<VarTracer>();
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
            //float min = g.mDataPoints[0];
            float max = g.mDataPoints[0];
            for (int i = 1; i < graph.GraphLength(); ++i)
            {
                sum += g.mDataPoints[i];
                //min = Mathf.Min(min,g.mDataPoints[i]);
                max = Mathf.Max(max,g.mDataPoints[i]);
            }
            if (graph.mInclude0)
            {
                //min = Mathf.Min(min, 0.0f);
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

            g.mMin = 0;
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

    public static void DefineVisualChannel(string channel, float height, bool isShareY = false)
    {
        GraphSetupHeight(channel, height);
        ShareYAxis(channel, isShareY);
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

    public static void ClearAllVariable()
    {
#if UNITY_EDITOR
        foreach (var VarBody in Instance.VariableBodys.Values)
        {
            foreach (var var in VarBody.VariableDict.Values)
            {
                foreach (var graphName in VarTracer.Instance.Graphs.Keys)
                {
                    if (var.ChannelDict.ContainsKey(graphName))
                        var.DetachChannel(graphName);
                }
            }
        }
#endif
    }

}
