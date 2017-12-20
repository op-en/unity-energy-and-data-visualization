using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerObject : DataNode {


    public class TopicMap {
        public string Topic;
        public bool Subscribed = false;
        public DataPoint LastDataPoint = null;
        public List<Subscription> Subscribers = new List<Subscription>();

    }

    [Header("Server properties")]
    public List<TopicMap> TopicMapping = new List<TopicMap>();


    public void OnConnect() {
        //Subscribe to all 
        foreach (TopicMap tm in TopicMapping)
        {
            tm.Subscribed = SubscribeTopic(tm.Topic);
        }
    }

    

    override public void Subscribe(Subscription Sub)
    {
        base.Subscribe(Sub);
        TopicMap NewMap;

        //Search through all mappings
        foreach (TopicMap tm in TopicMapping)
        {
            if (Sub.MatchesTopic(tm.Topic)) {
                tm.Subscribers.Add(Sub);

                //Send last data recived
                if (tm.LastDataPoint != null)
                    Sub.TimeDataUpdate(tm.LastDataPoint);

                return;
            }
        }

        NewMap = new TopicMap();
        NewMap.Topic = Sub.Topic;
        NewMap.Subscribers.Add(Sub);
        NewMap.Subscribed = SubscribeTopic(Sub.Topic);
        TopicMapping.Add(NewMap);

    }

	virtual public bool Publish(string topic, string payload) {

		return false;
	}

    virtual public bool SubscribeTopic(string Topic)
    {
        return false;
    }

    override public void Unsubscribe(Subscription Sub)
    {
        base.Unsubscribe(Sub);

        foreach (TopicMap tm in TopicMapping)
        {
            tm.Subscribers.Remove(Sub);
            if (tm.Subscribers.Count == 0) {
                //TODO Unsubscribe topic
                //..
              TopicMapping.Remove(tm);
              
            }
        }
    }

	virtual public bool GetPeriod(string Topic, double from, double To, DataSeries Target){

		return false;
	}


    public void UpdateAllTargets(string Event, JSONObject msg)
    {
        DataPoint Data = new DataPoint();
        LastData = Data;

		if (Event != "mqtt" && Event != "requested")
            return;

        string topic = (string) msg.GetField("topic").str;
        string payload = msg.GetField("payload").str;
        //payload = payload.Substring(1, payload.Length - 1);
        payload = payload.Replace("\\\"", "\"");

        JSONObject json_payload = new JSONObject(payload);



        
        //Only text  
        if (json_payload.IsNull) {
            //print("******************************");
            Data.Texts[0] = payload;
            Data.Timestamp = GameTime.GetInstance().time;

            foreach (TopicMap tm in TopicMapping)
            {
                if (topic == tm.Topic)
                {
                    //Send to all subscribers in the list. 
                    foreach (Subscription Sub in tm.Subscribers)
                    {
                        Sub.TimeDataUpdate(Data);
                    }
                }

            }

            return;
        }


		if (Event == "mqtt") {

			Data.Timestamp = json_payload.GetField ("time").n;
			//Data.Texts[0] = payload;

			json_payload.RemoveField ("time");



			foreach (TopicMap tm in TopicMapping) {
				if (topic == tm.Topic) {
					//Send to all subscribers in the list. 
					foreach (Subscription Sub in tm.Subscribers) {

						if (Sub.Target.Columns == null || Sub.Target.Columns.Count == 0) {
	                        
							Sub.Target.Columns = json_payload.keys;
							//Sub.Target.Columns.Remove("Time");
						}

						Data.Values = new double[Sub.Target.Columns.Count];

						for (int i = 0; i < Sub.Target.Columns.Count; i++) {
							Data.Values [i] = json_payload.GetField (Sub.Target.Columns [i]).n;
						}

	                    
						Sub.TimeDataUpdate (Data);
					}
				}

			}
		}

		if (Event == "requested") {
			print ("DATA:");
			print (json_payload.GetField("results")[0].GetField("series"));
	
			foreach (TopicMap tm in TopicMapping) {
				if (topic == tm.Topic) {
					foreach (Subscription Sub in tm.Subscribers) {
						if (!Sub.Target is DataSeries) {
							continue;	
						}

						if (Sub.Target.Columns == null || Sub.Target.Columns.Count == 0) {
							
						}


						
					}
					
				}
			}

			
		}
			
    }

    void printdata(JSONObject obj)
    {
        switch (obj.type)
        {
            case JSONObject.Type.OBJECT:
                for (int i = 0; i < obj.list.Count; i++)
                {
                    string key = (string)obj.keys[i];
                    JSONObject j = (JSONObject)obj.list[i];
                    Debug.Log("KEY: "+key);
                    printdata(j);
                }
                break;
            case JSONObject.Type.ARRAY:
                foreach (JSONObject j in obj.list)
                {
                    printdata(j);
                }
                break;
            case JSONObject.Type.STRING:
                Debug.Log(obj.str);
                break;
            case JSONObject.Type.NUMBER:
                Debug.Log(obj.n);
                break;
            case JSONObject.Type.BOOL:
                Debug.Log(obj.b);
                break;
            case JSONObject.Type.NULL:
                Debug.Log("NULL");
                break;

        }
    }

}
