using UnityEngine;
using System.Collections;
using SocketIO;
using System.Collections.Generic;




public class AppServer : SocketIOComponentMod {

	public int RequestCounter = 0;
	//public List<JSONObject> PendingRequest = new List<JSONObject> ();

	class request {
		public int request_id;
		public DataSeries Target;
		public JSONObject Params;
		public double TimeSent = double.NaN;
	}

	List<request> RequestList = new List<request>();


	// Use this for initialization
	void Start () {
		//Debug = Subscribers;

		On("open", DoOnOpen);
		On("mqtt", DoOnMqtt);
		On("series", DoOnSeries);
		On("error", DoOnError);
		On("close", DoOnClose);
		On("requested", DoOnSeries);

		//Debug.Log ("Starting: " + NodeName);

        

		base.Start();
	}
		    
	// Update is called once per frame
	void Update () {
		base.Update();
	}

    public bool MQTTsubscribe(string topic) {
        //JSONObject json = new JSONObject("\"topic\":\"" + topic + "\"");

        if (!IsConnected)
            return false;

        Dictionary<string, string> data = new Dictionary<string, string>();
        data["topic"] = topic;
        
        Emit("subscribe", new JSONObject(data));

        return true;
    }

	override public bool Publish(string topic, string payload) {
		//JSONObject json = new JSONObject("\"topic\":\"" + topic + "\"");

		if (!IsConnected)
			return false;

		Dictionary<string, string> data = new Dictionary<string, string>();
		data["topic"] = topic;
		data["payload"] = payload;

		Emit("publish", new JSONObject(data));

		return true;
	}

    override public bool SubscribeTopic(string Topic)
    {
        return MQTTsubscribe(Topic);
    }

	public bool Request(string topic) {
		//JSONObject json = new JSONObject("\"topic\":\"" + topic + "\"");

		if (!IsConnected)
			return false;

		Dictionary<string, string> data = new Dictionary<string, string>();
		data["topic"] = topic;

		Emit("request", new JSONObject(data));

		return true;
	}

	override public bool GetPeriod(string Topic, double From, double To, DataSeries Target){


		JSONObject parameters = new JSONObject(JSONObject.Type.OBJECT);

		//Dictionary<string, string> data = new Dictionary<string, string>();
		//data["topic"] = Topic;

		parameters.AddField("topic",Topic);

		if (!double.IsNaN(From))
			parameters.AddField("from",From);

		if (!double.IsNaN(To))
			parameters.AddField("to",To);

		RequestCounter++;

		parameters.AddField("request_id",RequestCounter);

		//Save request info
		request rq = new request();
		rq.Target = Target;
		rq.request_id = RequestCounter;
		rq.Params = parameters;
		RequestList.Add (rq);

		if (!IsConnected) {
			rq.TimeSent = double.NaN;
			return false;

		}

		Emit("request", parameters);

		rq.TimeSent = SimulationTime.RealWorldTime;

		return true;


	}
		
    public void DoOnOpen(SocketIOEvent e)
	{
		//print("APPSERVER Open");
        //Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
        //MQTTsubscribe("test/signal");

		foreach (request rq in RequestList) {
			//print (rq.TimeSent);
			if (double.IsNaN(rq.TimeSent)){
				//print ("requesting");
				//print (rq.Params);
				Emit("request", rq.Params);
				rq.TimeSent = SimulationTime.RealWorldTime;
			}
		}

        OnConnect();
    }

	public void DoOnMqtt(SocketIOEvent e)
	{
        string name = NodeName;

        //Debug.Log(NodeName + ": [SocketIO] Mqtt received: " + e.name + " " + e.data);

  


        UpdateAllTargets(e.name, e.data);


        if (e.data == null) { return; }

		Debug.Log(
			"#####################################################" +
			"THIS: " + e.data.GetField("this").str +
			"#####################################################"
		);


        //DataPoint Data = new DataPoint();

        //Data.Texts = e.data;

       



    }

	public void DoOnSeries(SocketIOEvent e)
	{
		//string name = NodeName;

		//Debug.Log(NodeName + ": [SocketIO] Mqtt received: " + e.name + " " + e.data);
		//print ("Series");
		//print (e.data);



		JSONObject msg = e.data;
		double request_id = msg["request_id"].n;

		string topic = (string) msg["topic"].str;
		string payload = msg["payload"].str;
		//payload = payload.Substring(1, payload.Length - 1);
		payload = payload.Replace("\\\"", "\"");
		JSONObject keys,data,values;

		JSONObject json_payload = new JSONObject(payload);
		int keyindex_time=0,test=0;
		int[] keyindex = null;
		int keyindex_counter = 0;
		List<DataPoint> ParsedData = new List<DataPoint>();
		DataPoint dp;

		foreach (request rq in RequestList) {
			if (rq.request_id != request_id)
				continue;

			//print( "MATCHING RESPONSE: " + request_id);

			data = json_payload["results"][0]["series"][0];
			keys = data ["columns"];
			values = data ["values"];

			//print ("DATA:");
			//print (values.Count);
			//print (values);
			//print (values.Count);
			//print (keys);

			//Find index of time
			for (int i=0; i < keys.Count; i++) {
				//print(keys [i].ToString());
				//print (keys [i].str == "time");

				if (keys [i].str == "time") {
					keyindex_time = i;

					break;
				}
					
				return;
			}


   

			//Find other indexes
			keyindex = new int[keys.Count];

            for (int c = 0; c < keys.Count; c++)
            {
                keyindex[c] = rq.Target.AddColumnID(keys[c].str);
            }

            int icols = rq.Target.Columns.Count;

            for (int r=0;r<values.Count;r++) {
				dp = new DataPoint ();
				//print ("Inserted1");
				dp.Timestamp = values [r] [keyindex_time].n/1000.0;
				//print (dp.Timestamp);
				dp.Values = new double[icols];

                for (int c=0; c < icols; c++)
                {
                    dp.Values[c] = double.NaN;
                }

				for (int c = 0; c < keys.Count; c++) {

                    if (keyindex[c] != -1)
                    {
                        if (values[r][c].IsNumber)
                            dp.Values[keyindex[c]] = values[r][c].n;
                        else if (values[r][c].IsString)
                        {
                            try
                            {
                                dp.Values[keyindex[c]] = double.Parse(values[r][c].str);
                            }
                            catch (System.FormatException)
                            {
                                dp.Values[keyindex[c]] = double.NaN;
                            }


                        }
                    }
				}

				rq.Target.InsertData (dp);

			}

			rq.Target.ForceUpdate();


//			print ("Loop test");
//			print (test);

			RequestList.Remove (rq);
			return;

		}



	}

	public void DoOnError(SocketIOEvent e)
	{
		//Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
	}

	public void DoOnClose(SocketIOEvent e)
	{	
		Debug.Log(NodeName + ": [SocketIO] Close received: " + e.name + " " + e.data);
	}


}
