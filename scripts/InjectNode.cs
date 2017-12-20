using UnityEngine;
using System.Collections;
//using TimeSeries;

public class InjectNode : DataNode {

	public DataPoint Data;
	//public string Text;
	//public bool S;
	public bool InjectOnStart = false;
	public bool Randomize = false;

	// Use this for initialization
	void Start () {
		if (InjectOnStart)
			Inject ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void Inject() {
		foreach (DataNode.Subscription Sub in Targets) {
			Debug.Log("Injecting!");
			DataPoint Data2 = Data.Clone ();

			if (Sub.Source == null)
				Sub.Source = this;


			if (Randomize) {

				for(int i=0; i<Data.Values.Length;i++) {
					Data2.Values [i] = Random.Range (0,(float) Data.Values [i]);
				}
					
			}

			Sub.Target.TimeDataUpdate (Sub,Data2);
		}
	}
}
