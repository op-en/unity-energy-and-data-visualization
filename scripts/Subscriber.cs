using UnityEngine;
using System.Collections;

public class Subscriber : MonoBehaviour {

	[Header("Server")]
	public ServerObject Server = null;

	[Space(10)]
	public string Topic;
	public string Subproperty;

	// Use this for initialization
	void Start () {
	
	}


	virtual public void Data_Update(JSONObject json) {
		print("Unhandled data!");
	}


}


