using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class DataText : DataNode {

	public TextMesh textMesh;
	public Text text;
	public string Unit = "";
	public int decimals = 0;
	public double scale = 1;


	//public string Subproperty = null;
	public int SubpropertyId = 0;


	// Use this for initialization
	void Start () {
		GameObject parentObject;
		parentObject = this.transform.root.gameObject;
		if (textMesh == null && text == null)
		    textMesh = parentObject.GetComponent<TextMesh>();

		if (textMesh == null && text == null)
			text = gameObject.GetComponent(typeof(Text)) as Text;



	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//override public void JsonUpdate(Subscription Sub, JSONObject json) {

	//	if (Subproperty == null)
	//		textMesh.text = json.str;
	//	else {
	//		textMesh.text = json.GetField (Subproperty).str;
	//	}
	//}

	override public void TimeDataUpdate(Subscription Sub, DataPoint data) {

		string newtext = "";

		//if (NodeName == "Heating Energy")
		//	print ("Heating Energy data recived");

		if (data == null)
			return;

		if (data.Values == null)
			return;
		
		if (SubpropertyId >= data.Values.Length)
			return; 

		//Debug.Log (data.Values.Length);
		//Debug.Log (SubpropertyId);
		if (data.Values [SubpropertyId] != null) {

			newtext = Math.Round(data.Values[SubpropertyId]/scale,decimals).ToString() + " " + Unit;


			if (textMesh != null)
				textMesh.text = newtext;
			if (text != null) 
				text.text = newtext;
			return;
		}

		if (data.Texts [SubpropertyId] != null) {
			if (textMesh != null)
				textMesh.text = data.Texts [SubpropertyId] ;
			if (text != null)
				text.text = data.Texts [SubpropertyId] ;
		}

	}





}
