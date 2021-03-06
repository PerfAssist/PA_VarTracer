﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace VariableTracer
{
    public class VarTracerWindow : EditorWindow
    {
        const int InValidNum = -1;
        static Vector2 mGraphViewScrollPos;

        static float mWidth;

        public static int mMouseSelectedGraphNum = 1;

        static float x_offset = 2.0f;
        static float y_gap = 80.0f;
        static float y_offset = 20;

        static Material mLineMaterial;

        public static float m_winWidth = 0.0f;
        public static float m_winHeight = 0.0f;

        public static float m_controlScreenHeight = 0.0f;
        public static float m_controlScreenPosY = 0.0f;

        public static float m_navigationScreenHeight = 0.0f;
        public static float m_navigationScreenPosY = 0.0f;

        public static int m_variableBarIndex = InValidNum;

        const int variableNumPerLine = 12;
        const int variableLineHight = 20;

        const int variableLineStartY = 25;

        string _IPField = VarTracerConst.RemoteIPDefaultText;

        bool _connectPressed = false;

        static bool m_isDrawLine = true;

        public static bool m_isStart = true;
        public static bool m_isPaused = false;


        public static GUIStyle NameLabel;
        public static GUIStyle SmallLabel;
        public static GUIStyle HoverText;
        public static GUIStyle FracGS;
        public static GUIStyle VarSelectedBtnStyle;
        public static GUIStyle EventButtonStyle;

        //static bool testFlag = false;

        [MenuItem("Window/PerfAssist" + "/VarTracer")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            VarTracerWindow window = (VarTracerWindow)EditorWindow.GetWindow(typeof(VarTracerWindow), false, "VarTracerWindow");
            window.minSize = new Vector2(230f, 50f);
            window.Show();
        }
        void Awake()
        {
            InitNet();
            _connectPressed = true;
        }

        void InitNet()
        {
            if (NetManager.Instance == null)
            {
                NetUtil.LogHandler = Debug.LogFormat;
                NetUtil.LogErrorHandler = Debug.LogErrorFormat;

                NetManager.Instance = new NetManager();
                NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_VarTracerInfo, VarTracerNet.Instance.Handle_VarTracerInfo);
            }
        }

        void OnEnable()
        {
            Application.runInBackground = true;
            EditorApplication.update += Update;
            if (VarTracer.Instance != null)
            {
                if (VarTracer.Instance.Graphs.Count == 0)
                    VarTracer.AddChannel();

                bool constainsCamera = VarTracer.Instance.groups.ContainsKey("Camera");
                if (!constainsCamera || VarTracer.Instance.groups["Camera"].VariableDict.Count == 0)
                {
                    //VarTracerHandler.DefineVariable("CameraV_X", "Camera");
                    //VarTracerHandler.DefineVariable("CameraV_Y", "Camera");
                    //VarTracerHandler.DefineVariable("CameraV_Z", "Camera");
                    //VarTracerHandler.DefineVariable("CameraV_T", "Camera");

                    VarTracerHandler.DefineVariable("PlayerV_X", "Player");
                    VarTracerHandler.DefineVariable("PlayerV_Y", "Player");
                    VarTracerHandler.DefineVariable("PlayerV_Z", "Player");
                    VarTracerHandler.DefineVariable("CameraV_T", "Camera");

                    VarTracerHandler.DefineVariable("FPS", "System");

                    VarTracerHandler.DefineEvent("JUMP", "Camera");
                    VarTracerHandler.DefineEvent("ATTACK", "Camera");

                    //VarTracerHandler.DefineVariable("NpcV_X", "Npc");
                    //VarTracerHandler.DefineVariable("NpcV_Y", "Npc");
                    //VarTracerHandler.DefineVariable("NpcV_Z", "Npc");
                    //VarTracerHandler.DefineVariable("NpcV_T", "Npc");
                }
            }
            VarTracer.AddChannel();
        }


        public static void StartVarTracer()
        {
            m_isStart = true;
            m_isPaused = false;
        }

        public static void StopVarTracer()
        {
            VarTracerUtils.StopTimeStamp = VarTracerUtils.GetTimeStamp();
            m_isPaused = true;
        }

        public static bool isVarTracerStart()
        {
            return m_isStart;
        }

        void OnDestroy()
        {
            if (NetManager.Instance != null)
            {
                NetManager.Instance.Dispose();
                NetManager.Instance = null;
            }
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            VarTracer.Instance.Graphs.Clear();
            VarTracer.Instance.groups.Clear();
        }

        void Update()
        {
#if UNITY_EDITOR
            foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
            {
                VarTracerGraphItData g = kv.Value;
                if (g.mReadyForUpdate && !g.mFixedUpdate)
                {
                    VarTracer.Instance.StepGraphInternal(g);
                }
            }
#endif
            if (_connectPressed)
            {
                VarTracerNetUtils.Connect(_IPField);
                _connectPressed = false;
            }
            VarTracerNet.Instance.Upate();
            Repaint();
        }
        public void CheckForResizing()
        {
            if (Mathf.Approximately(position.width, m_winWidth) &&
                Mathf.Approximately(position.height, m_winHeight))
                return;

            m_winWidth = position.width;
            m_winHeight = position.height;

            UpdateVariableAreaHight();

            m_controlScreenPosY = 0.0f;

            m_navigationScreenHeight = m_winHeight - m_controlScreenHeight;
            m_navigationScreenPosY = m_controlScreenHeight;
        }


        void OnGUI()
        {
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName.Equals("AppStarted"))
            {
                InitNet();
                _connectPressed = true;
            }

            CheckForResizing();
            InitializeStyles();

            Handles.BeginGUI();
            Handles.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, 1));

            //if (!testFlag)
            //{
            //    CreateLineMaterial();
            //    //Material material = new Material(Shader.Find("Diffuse"));
            //    mLineMaterial.SetPass(0);

            //    GL.Color(new Color(1, 1, 1));

            //    // 构建三角形的三个顶点，并赋值给Mesh.vertices
            //    Mesh mesh = new Mesh();
            //    mesh.vertices = new Vector3[] {
            //    new Vector3 (50, 50, 0),
            //    new Vector3 (200, 200, 0),
            //    new Vector3 (500,500, 0),
            //};

            //    // 构建三角形的顶点顺序，因为这里只有一个三角形，
            //    // 所以只能是(0, 1, 2)这个顺序。
            //    mesh.triangles = new int[3] { 0, 1, 2 };

            //    mesh.RecalculateNormals();
            //    mesh.RecalculateBounds();

            //    //// 使用Shader构建一个材质，并设置材质的颜色。
            //    //material.SetColor("_Color", Color.yellow);

            //    Graphics.DrawMeshNow(mesh, Handles.matrix);

            //    GL.Begin(GL.LINES);
            //    GL.Color(new Color(1, 1, 1));
            //    Plot(10, 10, 500, 500);
            //    Plot(0, 500, 500, 500);
            //    GL.End();
            //    //testFlag = !testFlag;
            //}

            //control窗口内容
            GUILayout.BeginArea(new Rect(0, m_controlScreenPosY, m_winWidth, m_controlScreenHeight));
            {
                DrawVariableBar();
            }
            GUILayout.EndArea();
            //navigation窗口内容
            GUILayout.BeginArea(new Rect(VarTracerConst.NavigationAreaStartX, m_navigationScreenPosY, m_winWidth, m_winHeight));
            {
                DrawGraphs(position, this);
            }
            GUILayout.EndArea();

            //Attribute窗口内容
            GUILayout.BeginArea(new Rect(VarTracerConst.AttributeAreaStartX, m_navigationScreenPosY, m_winWidth, m_winHeight));
            {
                DrawAttributeArea(position, this);
            }
            GUILayout.EndArea();

            //刻度线窗口内容
            GUILayout.BeginArea(new Rect(VarTracerConst.TickMarkAreaStartX, m_navigationScreenPosY - 8, m_winWidth, m_winHeight));
            {
                DrawTickMark(position, this);
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        void UpdateVariableAreaHight()
        {
            var lineNum = CalculateVariableLineNum();
            var ry = variableLineStartY * 2 + lineNum * variableLineHight;

            y_offset = ry;
            m_controlScreenHeight = ry;
        }

        int CalculateVariableLineNum()
        {
            List<VarTracerVariable> variableList = new List<VarTracerVariable>();
            foreach (var varBody in VarTracer.Instance.groups.Values)
            {
                foreach (var var in varBody.VariableDict.Values)
                {
                    variableList.Add(var);
                }
            }

            int lineNum = variableList.Count / variableNumPerLine;
            int mod = variableList.Count % variableNumPerLine;
            if (mod > 0)
                lineNum += 1;

            return lineNum;
        }

        List<VarTracerVariable> GetVariableList()
        {
            List<VarTracerVariable> variableList = new List<VarTracerVariable>();
            foreach (var varBody in VarTracer.Instance.groups.Values)
            {
                foreach (var var in varBody.VariableDict.Values)
                {
                    variableList.Add(var);
                }
            }
            return variableList;
        }

        void DrawVariableBar()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            var variableCombineList = VarTracer.Instance.Graphs[(mMouseSelectedGraphNum - 1).ToString()].VariableCombineList;
            for (int i = 0; i < variableCombineList.Count; i++)
            {
                if (GUILayout.Button(variableCombineList[i], VarSelectedBtnStyle, GUILayout.Width(90)))
                {
                    variableCombineList.Remove(variableCombineList[i]);
                    ShowVariableCombine();
                }
            }
            GUILayout.Space(20);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(25)))
            {
                variableCombineList.Clear();
                ShowVariableCombine();
            }

            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(100)))
                VarTracer.ClearAll();

            GUI.SetNextControlName("LoginIPTextField");
            var currentStr = GUILayout.TextField(_IPField, GUILayout.Width(120));
            if (!_IPField.Equals(currentStr))
            {
                _IPField = currentStr;
            }

            if (GUI.GetNameOfFocusedControl().Equals("LoginIPTextField") && _IPField.Equals(VarTracerConst.RemoteIPDefaultText))
            {
                _IPField = "";
            }

            bool savedState = GUI.enabled;

            bool connected = NetManager.Instance != null && NetManager.Instance.IsConnected;

            GUI.enabled = !connected;
            if (GUILayout.Button("Connect", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _connectPressed = true;
            }
            GUI.enabled = connected;
            GUI.enabled = savedState;

            string buttonName;
            if (m_isDrawLine)
                buttonName = "Draw Point";
            else
                buttonName = "Draw Line";
            if (GUILayout.Button(buttonName, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                m_isDrawLine = !m_isDrawLine;
            }

            if (m_isPaused)
                buttonName = "Resume";
            else
                buttonName = "Pause";
            if (GUILayout.Button(buttonName, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                m_isPaused = !m_isPaused;
                if (m_isPaused)
                    StopVarTracer();
                else
                    StartVarTracer();
            }
            GUILayout.EndHorizontal();

            var lineNum = CalculateVariableLineNum();
            var varList = GetVariableList();

            for (int i = 0; i < lineNum; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                for (int j = 0; j < variableNumPerLine; j++)
                {
                    if (j + i * variableNumPerLine >= varList.Count)
                        continue;
                    var var = varList[j + i * variableNumPerLine];
                    var saveColor = GUI.color;
                    if (VarTracer.IsVariableOnShow(var.VarName))
                        GUI.color = Color.white;

                    if (GUILayout.Button(var.VarName, EditorStyles.toolbarButton, GUILayout.Width(100)))
                    {
                        if (!variableCombineList.Contains(var.VarName))
                        {
                            variableCombineList.Add(var.VarName);
                            ShowVariableCombine();
                        }
                        else
                        {
                            variableCombineList.Remove(variableCombineList[i]);
                            ShowVariableCombine();
                        }
                    }

                    GUI.color = saveColor;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private static void ShowVariableCombine()
        {
            var targetGraphName = (mMouseSelectedGraphNum - 1).ToString();
            VarTracer.ClearGraph(targetGraphName);
            foreach (var varName in VarTracer.Instance.Graphs[targetGraphName].VariableCombineList)
            {
                VarTracer.AttachVariable(varName, targetGraphName);
            }
        }

        static void DrawGraphGridLines(float y_pos, float width, float height, bool isMouseOverGraph)
        {
            GL.Color(new Color(0.3f, 0.3f, 0.3f));
            float steps = 8;
            float x_step = width / steps;
            float y_step = height / steps;
            for (int i = 0; i < steps + 1; ++i)
            {
                Plot(x_offset + x_step * i, y_pos, x_offset + x_step * i, y_pos + height);
                Plot(x_offset, y_pos + y_step * i, x_offset + width, y_pos + y_step * i);
            }

            steps = 4;
            x_step = width / steps;
            y_step = height / steps;
            for (int i = 0; i < steps + 1; ++i)
            {
                if ((i == 0 || i == 4) && isMouseOverGraph)
                    GL.Color(new Color(1, 1, 1));
                else
                    GL.Color(new Color(0.4f, 0.4f, 0.4f));
                Plot(x_offset + x_step * i, y_pos, x_offset + x_step * i, y_pos + height);
                Plot(x_offset, y_pos + y_step * i, x_offset + width, y_pos + y_step * i);
            }
        }


        static void Plot(float x0, float y0, float x1, float y1)
        {
            GL.Vertex3(x0, y0, 0);
            GL.Vertex3(x1, y1, 0);
        }

        static void CreateLineMaterial()
        {
            if (!mLineMaterial)
            {
                mLineMaterial = new Material(Shader.Find("Custom/VarTracerGraphIt"));
                mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
                mLineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public static void DrawTickMark(Rect rect, EditorWindow window)
        {
            int graph_index = 0;
            foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
            {
                graph_index++;
                float height = kv.Value.GetHeight();

                GUIStyle s = new GUIStyle();
                s.fixedHeight = height + y_gap;
                s.stretchWidth = true;
                EditorGUILayout.BeginVertical(s);

                //skip subgraph title if only one, and it's the same.
                NameLabel.normal.textColor = Color.white;

                if (kv.Value.mData.Count > 0)
                {
                    GUILayout.BeginVertical();

                    float GraphGap = kv.Value.m_maxValue - kv.Value.m_minValue;
                    float unitHeight = GraphGap / VarTracerConst.Graph_Grid_Row_Num;

                    for (int i = 0; i < VarTracerConst.Graph_Grid_Row_Num + 1; i++)
                    {
                        GUILayout.Space(6);
                        if (unitHeight == 0)
                            EditorGUILayout.LabelField("", NameLabel);
                        else
                            EditorGUILayout.LabelField((kv.Value.m_maxValue - i * unitHeight).ToString(VarTracerConst.NUM_FORMAT_1), NameLabel);
                    }
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
        }


        public static void DrawAttributeArea(Rect rect, EditorWindow window)
        {
            int graph_index = 0;
            foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
            {
                graph_index++;
                bool isSelected = graph_index == mMouseSelectedGraphNum;
                if (isSelected)
                {
                    try
                    {
                        DrawGraphAttribute(kv);
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("");
                    }
                    break;
                }
                else
                {
                    continue;
                }
            }
        }

        public static void DrawGraphs(Rect rect, EditorWindow window)
        {
            if (VarTracer.Instance)
            {
                CreateLineMaterial();

                mLineMaterial.SetPass(0);

                int graph_index = 0;

                //use this to get the starting y position for the GL rendering
                Rect find_y = EditorGUILayout.BeginVertical(GUIStyle.none);
                EditorGUILayout.EndVertical();

                int currentFrameIndex = VarTracerNet.Instance.GetCurrentFrameFromTimestamp(VarTracerUtils.GetTimeStamp());
                if (m_isPaused)
                    currentFrameIndex = VarTracerNet.Instance.GetCurrentFrameFromTimestamp(VarTracerUtils.StopTimeStamp);

                float scrolled_y_pos = y_offset - mGraphViewScrollPos.y;
                if (Event.current.type == EventType.Repaint)
                {
                    GL.PushMatrix();
                    float start_y = find_y.y;
                    GL.Viewport(new Rect(0, 0, rect.width, rect.height - start_y));
                    GL.LoadPixelMatrix(0, rect.width, rect.height - start_y, 0);

                    //Draw grey BG
                    GL.Begin(GL.QUADS);
                    GL.Color(new Color(0.2f, 0.2f, 0.2f));

                    foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
                    {
                        float height = kv.Value.GetHeight();

                        GL.Vertex3(x_offset, scrolled_y_pos, 0);
                        GL.Vertex3(x_offset + mWidth, scrolled_y_pos, 0);
                        GL.Vertex3(x_offset + mWidth, scrolled_y_pos + height, 0);
                        GL.Vertex3(x_offset, scrolled_y_pos + height, 0);

                        scrolled_y_pos += (height + y_gap);
                    }
                    GL.End();

                    scrolled_y_pos = y_offset - mGraphViewScrollPos.y;
                    //Draw Lines
                    GL.Begin(GL.LINES);

                    foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
                    {
                        graph_index++;
                        float height = kv.Value.GetHeight();
                        DrawGraphGridLines(scrolled_y_pos, mWidth, height, graph_index == mMouseSelectedGraphNum);

                        foreach (KeyValuePair<string, VarTracerDataInternal> entry in kv.Value.mData)
                        {
                            VarTracerDataInternal g = entry.Value;

                            float y_min = kv.Value.GetMin(entry.Key);
                            float y_max = kv.Value.GetMax(entry.Key);
                            float y_range = Mathf.Max(y_max - y_min, 0.00001f);

                            //draw the 0 line
                            if (y_min != 0.0f)
                            {
                                GL.Color(Color.white);
                                float y = scrolled_y_pos + height * (1 - (0.0f - y_min) / y_range);
                                Plot(x_offset, y, x_offset + mWidth, y);
                            }

                            GL.Color(g.mColor);

                            float previous_value = 0, value = 0;
                            int dataInfoIndex = 0, frameIndex = 0;
                            for (int i = 0; i <= currentFrameIndex; i++)
                            {
                                int dataCount = g.mDataInfos.Count;
                                if (dataCount != 0)
                                {
                                    int lastFrame = g.mDataInfos[dataCount - 1].FrameIndex;
                                    float lastValue = g.mDataInfos[dataCount - 1].Value;
                                    frameIndex = g.mDataInfos[dataInfoIndex].FrameIndex;

                                    if (dataInfoIndex >= 1)
                                        value = g.mDataInfos[dataInfoIndex - 1].Value;

                                    if (dataInfoIndex == 0 && i < frameIndex)
                                        value = 0;

                                    if (i >= frameIndex)
                                    {
                                        while (g.mDataInfos[dataInfoIndex].FrameIndex == frameIndex && dataInfoIndex < dataCount - 1)
                                        {
                                            dataInfoIndex++;
                                        }
                                    }

                                    if (i > lastFrame)
                                        value = lastValue;
                                }
                                else
                                {
                                    value = 0;
                                }

                                if (i >= 1)
                                {
                                    float x0 = x_offset + (i - 1) * kv.Value.XStep - kv.Value.ScrollPos.x;
                                    if (x0 <= x_offset - kv.Value.XStep) continue;
                                    if (x0 >= mWidth + x_offset) break;
                                    float y0 = scrolled_y_pos + height * (1 - (previous_value - y_min) / y_range);

                                    if (i == 1)
                                    {
                                        x0 = x_offset;
                                        y0 = scrolled_y_pos + height;
                                    }

                                    float x1 = x_offset + i * kv.Value.XStep - kv.Value.ScrollPos.x;
                                    float y1 = scrolled_y_pos + height * (1 - (value - y_min) / y_range);

                                    if (m_isDrawLine)
                                        Plot(x0, y0, x1, y1);
                                    else
                                        Plot(x0, y0, x0 + 1, y0 + 1);
                                }
                                previous_value = value;
                            }
                        }
                        scrolled_y_pos += (height + y_gap);
                    }
                    GL.End();

                    scrolled_y_pos = y_offset - mGraphViewScrollPos.y;
                    scrolled_y_pos = ShowEventLabel(scrolled_y_pos);
                    GL.PopMatrix();

                    GL.Viewport(new Rect(0, 0, rect.width, rect.height));
                    GL.LoadPixelMatrix(0, rect.width, rect.height, 0);
                }

                float allGraphHieght = (VarTracerConst.DefaultChannelHieght + y_gap) * VarTracer.Instance.Graphs.Count - y_gap - mGraphViewScrollPos.y;
                Rect rec = new Rect(10, allGraphHieght, 100, 1000);
                GUILayout.BeginArea(rec);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    VarTracer.AddChannel();
                }

                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    VarTracer.RemoveChannel();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                mGraphViewScrollPos = EditorGUILayout.BeginScrollView(mGraphViewScrollPos, GUIStyle.none);
                GUILayout.Label("", GUILayout.Height((VarTracerConst.DefaultChannelHieght + y_gap) * VarTracer.Instance.Graphs.Count + y_offset + m_controlScreenHeight));
                graph_index = 0;
                mWidth = window.position.width - VarTracerConst.NavigationAreaRWidth;
                foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
                {
                    graph_index++;
                    float height = kv.Value.GetHeight();
                    float width = currentFrameIndex * kv.Value.XStep;
                    if (width < mWidth)
                    {
                        width = mWidth - x_offset;
                    }
                    else
                    {
                        if (!m_isPaused)
                            kv.Value.ScrollPos = new Vector2(width - mWidth, kv.Value.ScrollPos.y);
                    }

                    GUIStyle s = new GUIStyle();
                    s.fixedHeight = height + y_gap;
                    s.stretchWidth = true;
                    Rect r = EditorGUILayout.BeginVertical(s);

                    //skip subgraph title if only one, and it's the same.
                    NameLabel.normal.textColor = Color.white;

                    r.height = height + 50;
                    r.width = width;
                    r.x = x_offset - 35;
                    r.y = (height + y_gap) * (graph_index - 1) - 10;

                    if (kv.Value.mData.Count > 0)
                    {
                        GUILayout.BeginArea(r);
                        GUILayout.BeginVertical();
                        for (int i = 0; i < VarTracerConst.Graph_Grid_Row_Num + 1; i++)
                        {
                            GUILayout.Space(6);
                            EditorGUILayout.LabelField("");
                        }
                        GUILayout.BeginHorizontal();
                        kv.Value.ScrollPos = GUILayout.BeginScrollView(kv.Value.ScrollPos, GUILayout.Width(mWidth), GUILayout.Height(0));
                        GUILayout.Label("", GUILayout.Width(width), GUILayout.Height(0));
                        GUILayout.EndScrollView();
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        GUILayout.EndArea();
                    }

                    r.width = mWidth+ 35;
                    ////Respond to mouse input!
                    if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition - Event.current.delta))
                    {
                        if (Event.current.button == 0)
                        {
                            kv.Value.ScrollPos = new Vector2(kv.Value.ScrollPos.x + Event.current.delta.x, kv.Value.ScrollPos.y);
                        }
                        window.Repaint();
                    }
                    else if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        mMouseSelectedGraphNum = graph_index;
                    }

                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawGraphAttribute(KeyValuePair<string, VarTracerGraphItData> kv)
        {
            GUILayout.Label("Graph:" + kv.Key, NameLabel);
            int colorIndex = kv.Value.mData.Count;
            foreach (var varBodyName in GetAllVariableBodyFromChannel(kv.Key))
            {
                NameLabel.normal.textColor = Color.white;

                GUILayout.Label("LogicName:" + varBodyName, NameLabel);

                foreach (var entry in kv.Value.mData)
                {
                    var variable = VarTracer.GetGraphItVariableByVariableName(entry.Key);
                    VarTracerDataInternal g = entry.Value;
                    if (variable.VarBodyName.Equals(varBodyName))
                    {
                        if (kv.Value.mData.Count >= 1)
                        {
                            NameLabel.normal.textColor = g.mColor;
                        }
                        GUILayout.Label("     [Variable]   " + entry.Key + ":" + g.mCurrentValue.ToString(VarTracerConst.NUM_FORMAT_2), NameLabel);
                    }
                }

                var varBody = VarTracer.Instance.groups[varBodyName];

                foreach (var eventName in varBody.EventInfos.Keys)
                {
                    colorIndex++;
                    NameLabel.normal.textColor = VarTracerUtils.GetColorByIndex(colorIndex);
                    GUILayout.BeginHorizontal();

                    Color saveColor = GUI.backgroundColor;
                    GUI.backgroundColor = NameLabel.normal.textColor;
                    GUILayout.Button("", EventButtonStyle, GUILayout.Width(10));
                    GUI.backgroundColor = saveColor;
                    var flag = EditorGUILayout.Toggle(varBody.EventInfos[eventName].IsCutFlag, GUILayout.Width(10));
                    if (flag != varBody.EventInfos[eventName].IsCutFlag)
                    {
                        varBody.EventInfos[eventName].IsCutFlag = flag;
                        if (flag)
                            varBody.EventInfos[eventName].TimeStamp = VarTracerUtils.GetTimeStamp();
                    }
                    GUILayout.Label("     <Event>    " + eventName, NameLabel);
                    GUILayout.EndHorizontal();
                }
            }

            if (kv.Value.mData.Count >= 1)
            {
                HoverText.normal.textColor = Color.white;
                GUILayout.Label("duration:" + (mWidth / kv.Value.XStep / VarTracerConst.FPS).ToString(VarTracerConst.NUM_FORMAT_3) + "(s)", HoverText, GUILayout.Width(140));
                kv.Value.XStep = GUILayout.HorizontalSlider(kv.Value.XStep, 0.1f, 15, GUILayout.Width(160));
            }
        }

        static List<string> GetAllVariableBodyFromChannel(string channelName)
        {
            List<string> result = new List<string>();
            foreach (var varBody in VarTracer.Instance.groups)
            {
                foreach (var var in varBody.Value.VariableDict.Values)
                {
                    foreach (var channel in var.ChannelDict.Values)
                    {
                        if (channelName.Equals(channel))
                        {
                            if (!result.Contains(var.VarBodyName))
                                result.Add(var.VarBodyName);
                        }
                    }
                }
            }
            return result;
        }

        private static bool IsEventBtnIntersect(float x1, float x2, float width1, float width2)
        {
            return System.Math.Abs(x1 - x2) <= (width1 + width2) / 2;
        }


        private static float ShowEventLabel(float scrolled_y_pos)
        {
            foreach (KeyValuePair<string, VarTracerGraphItData> kv in VarTracer.Instance.Graphs)
            {
                float height = kv.Value.GetHeight();
                List<EventData> sortedEventList = new List<EventData>();
                int colorIndex = kv.Value.mData.Count;
                Dictionary<string, int> colorIndexDict = new Dictionary<string, int>();
                foreach (var varBodyName in GetAllVariableBodyFromChannel(kv.Key))
                {
                    var varBody = VarTracer.Instance.groups[varBodyName];
                    foreach (var eventName in varBody.EventInfos.Keys)
                    {
                        var eventInfo = varBody.EventInfos[eventName];
                        colorIndex++;
                        colorIndexDict.Add(eventName, colorIndex);
                        foreach (var data in eventInfo.EventDataList)
                        {
                            if (eventInfo.IsCutFlag && data.TimeStamp > eventInfo.TimeStamp)
                            {
                                eventInfo.TimeStamp = data.TimeStamp;
                                StopVarTracer();
                                break;
                            }
                            if (data.EventFrameIndex > 0)
                            {
                                float x = x_offset + data.EventFrameIndex * kv.Value.XStep - kv.Value.ScrollPos.x;
                                if (x <= x_offset - kv.Value.XStep) continue;
                                if (x >= mWidth + x_offset) break;

                                sortedEventList.Add(data);
                            }
                        }
                        sortedEventList.Sort((EventData e1, EventData e2) =>
                        {
                            return e1.EventFrameIndex.CompareTo(e2.EventFrameIndex);
                        });

                        float startY = scrolled_y_pos + height - VarTracerConst.EventStartHigh;
                        Rect preEventRect = new Rect(0, startY, 0, VarTracerConst.EventButtonHeight);
                        for (int i = 0; i < sortedEventList.Count; i++)
                        {
                            var currentEvent = sortedEventList[i];
                            GL.Color(Color.gray);

                            int buttonWidth = 0;
                            if (currentEvent.Duration == 0)
                                buttonWidth = (int)(VarTracerConst.INSTANT_EVENT_BTN_DURATION * VarTracerConst.FPS * kv.Value.XStep);
                            else
                                buttonWidth = (int)(VarTracerConst.INSTANT_EVENT_BTN_DURATION * VarTracerConst.FPS * kv.Value.XStep);
                            //buttonWidth = (int)(currentEvent.Duration * VarTracerConst.FPS * kv.Value.XStep);

                            GUIStyle style = EventButtonStyle;
                            float x = x_offset + currentEvent.EventFrameIndex * kv.Value.XStep - kv.Value.ScrollPos.x;
                            Rect tooltip_r;
                            if (IsEventBtnIntersect(x - buttonWidth / 2, preEventRect.x, buttonWidth, preEventRect.width))
                            {
                                if (preEventRect.y > height + int.Parse(kv.Key) * (height + y_gap))
                                    tooltip_r = new Rect(x - buttonWidth / 2, startY, buttonWidth, VarTracerConst.EventButtonHeight);
                                else
                                    tooltip_r = new Rect(x - buttonWidth / 2, preEventRect.y + VarTracerConst.EventButtonFixGap, buttonWidth, VarTracerConst.EventButtonHeight);
                            }
                            else
                                tooltip_r = new Rect(x - buttonWidth / 2, startY, buttonWidth, VarTracerConst.EventButtonHeight);
                            preEventRect = tooltip_r;
                            var saveColor = GUI.backgroundColor;
                            GUI.backgroundColor = VarTracerUtils.GetColorByIndex(colorIndexDict[currentEvent.EventName]);
                            GUI.Button(tooltip_r, currentEvent.EventName, style);

                            if (Event.current.type == EventType.Repaint && tooltip_r.Contains(Event.current.mousePosition + new Vector2(0, m_controlScreenHeight)))
                            {
                                GUI.backgroundColor = Color.white;
                                GUI.Label(new Rect(tooltip_r.x - 20, tooltip_r.y - 30, 110, 30), "name:" + currentEvent.EventName + "\n"
                                    + "duration:" + currentEvent.Duration + "\n", EditorStyles.textArea);
                            }
                            GUI.backgroundColor = saveColor;
                        }
                    }
                }
                scrolled_y_pos += (height + y_gap);
            }
            return scrolled_y_pos;
        }
        public static void InitializeStyles()
        {
            if (NameLabel == null)
            {
                NameLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
                NameLabel.normal.textColor = Color.white;
                SmallLabel = new GUIStyle(EditorStyles.whiteLabel);
                SmallLabel.normal.textColor = Color.white;

                HoverText = new GUIStyle(EditorStyles.whiteLabel);
                HoverText.alignment = TextAnchor.UpperRight;
                HoverText.normal.textColor = Color.white;

                FracGS = new GUIStyle(EditorStyles.whiteLabel);
                FracGS.alignment = TextAnchor.LowerLeft;

                VarSelectedBtnStyle = new GUIStyle(EditorStyles.whiteBoldLabel);
                VarSelectedBtnStyle.normal.background = Resources.Load("VariableButton") as Texture2D;
                VarSelectedBtnStyle.normal.textColor = Color.white;
                VarSelectedBtnStyle.alignment = TextAnchor.MiddleCenter;

                EventButtonStyle = new GUIStyle(EditorStyles.textArea);
                EventButtonStyle.normal.background = Resources.Load("durationButton") as Texture2D;
                EventButtonStyle.normal.textColor = Color.white;
                EventButtonStyle.alignment = TextAnchor.MiddleCenter;
            }
        }
    }
}
