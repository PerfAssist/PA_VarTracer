﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestGraph : MonoBehaviour {

	// Use this for initialization
    void Start()
    {
        Application.runInBackground = true;

        VarTracerTool.DefineEvent("MOVE", "Camera");
        VarTracerTool.DefineEvent("NPC_MOVE", "Npc");
        VarTracerTool.DefineEvent("SPEED_DOWN", "Player");
        VarTracerTool.DefineEvent("SPEED_UP", "Player");
    }

	// Update is called once per frame
	void Update () {

        VarTracerTool.UpdateVariable("CameraV_X", Camera.main.velocity.x);
        VarTracerTool.UpdateVariable("CameraV_Y", Camera.main.velocity.y);
        VarTracerTool.UpdateVariable("CameraV_Z", Camera.main.velocity.z);
        VarTracerTool.UpdateVariable("CameraV_T", Camera.main.velocity.magnitude);

        var NpcObj = GameObject.Find("Npc");
        var NpcScript = NpcObj.GetComponent("NpcTest") as NpcTest;
        //VarTracerTool.UpdateVariable("NpcV_X", NpcScript.GetVelocity().x);
        VarTracerTool.UpdateVariable("NpcV_Y", NpcScript.GetVelocity().y);
        VarTracerTool.UpdateVariable("NpcV_Z", NpcScript.GetVelocity().z);
        VarTracerTool.UpdateVariable("NpcV_T", NpcScript.GetVelocity().magnitude);

        var PlayerObj = GameObject.Find("Player");
        var PlayerScript = PlayerObj.GetComponent("PlayerTest") as PlayerTest;
        VarTracerTool.UpdateVariable("PlayerV_X", PlayerScript.GetVelocity().x);
        VarTracerTool.UpdateVariable("PlayerV_Y", PlayerScript.GetVelocity().y);
        VarTracerTool.UpdateVariable("PlayerV_Z", PlayerScript.GetVelocity().z);
        VarTracerTool.UpdateVariable("PlayerV_T", PlayerScript.GetVelocity().magnitude);

	}
}
