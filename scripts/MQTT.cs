using UnityEngine;
using System.Collections;

using System;
using System.Runtime.InteropServices;
using System.Text;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


public class MQTT : ServerObject {


	//http://tdoc.info/blog/2014/11/10/mqtt_csharp.html
	private MqttClient4Unity client;
	
	public string brokerHostname = null;
	public int brokerPort = 1883;
	public string userName = null;
	public string password = null;
	public string topic = null;
	
	private Queue msgq = new Queue();

	string lastMessage = null;

	//Singleton functionality 
	private static MQTT _instance;

	void Awake () {
		_instance = this;
	}

	public static MQTT GetInstance () {
		return _instance;
	}


	// Use this for initialization
	void Start () {
		if (brokerHostname != null && userName != null && password != null) {
			Connect ();
			client.Subscribe(topic);
		}
	}
	
	// Update is called once per frame
	void Update () {
		while (client.Count() > 0) {
			string s = client.Receive();
			msgq.Enqueue(s);
			Debug.Log("received :" + s);
			lastMessage = s;
			//GUILayout.Label(s);
		}
		
		if (Input.GetMouseButtonDown (0) == true) {
			client.Publish(topic, System.Text.Encoding.ASCII.GetBytes("nice click!"));
		}
	}
	
	void OnGUI() {
		
		if (lastMessage != null) 
			GUILayout.Label(lastMessage);
	}

	public void Connect()
	{
		client = new MqttClient4Unity(brokerHostname, brokerPort, false, null);
		string clientId = Guid.NewGuid().ToString();
		//client.WillMessage = System.Text.Encoding.ASCII.GetBytes("disconnected");
		//client.WillTopic = "clients/" + clientId;
		//client.WillFlag = false;
		client.Connect(clientId, userName, password, false, MqttMsgConnect.QOS_LEVEL_AT_MOST_ONCE, true, "clients/" + clientId , "disconnected", true, 60);

		Debug.Log ("Connecting");
		client.Publish ("clients/" + clientId,System.Text.Encoding.ASCII.GetBytes("connected"));

	}
	
	public void Publish(string _topic, string msg)
	{
		client.Publish(
			_topic, Encoding.UTF8.GetBytes(msg),
			MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
	}

	public void Request(DataSeriesBuffer ts,string Name,double StartTime,bool Absolute,int BufferSize)
	{
		//Add to query list. 

		//Test
		//ts.Values = new double[4] {1.0,2.0,3.0,4.0};
		//ts.TimeStamps = new double[4] {1452691843.0,1452691849.0,1452691858.0,1452691890.0};
		//ts.BufferValid = true;
		//ts.CurrentSize = BufferSize;
	}
}  
