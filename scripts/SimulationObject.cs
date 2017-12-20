using UnityEngine;
using System.Collections;
using System;

[Serializable] 
public class SimulationObject : MonoBehaviour {

	[Header("Simulation Object properties")]
	public GameTime SimulationTime = null;
	[SerializeField]
	double Next = double.PositiveInfinity;
	[SerializeField]
	double Prev = double.NegativeInfinity;

	bool registered = false;

	public void Awake(){

		//GameTime[] clocks;
 
		//Use first one. 
		if (SimulationTime == null) {
			SimulationTime = GameObject.FindObjectOfType<GameTime> ();
		}
		
	}

	public void Start() {
		

	}


	//This is called on updates form the time object
	virtual public bool UpdateSim(double time) {
		return false;
	}

	public bool RegisterKeypoints(){
		
		if (SimulationTime == null) 
			SimulationTime = GameTime.GetInstance ();

		//Calculate keypoints
		UpdateSim (SimulationTime.time);

		//Register.
		registered = SimulationTime.register (this);


		return registered;
	}

	public void SetNext(double ts){
		Next = ts;
		if (registered && SimulationTime != null)
			SimulationTime.UpdateNext (this);
	}

	public void ForceUpdate(){

		Next = double.NegativeInfinity;
		Prev = double.PositiveInfinity;

		if (registered && SimulationTime != null) {
			SimulationTime.UpdateNext (this);
			SimulationTime.UpdatePrev (this);
		}

	}

	public double GetNext(){
		return Next;
	}

	public void SetPrev(double ts){
		Prev = ts;

		if (registered)
			SimulationTime.UpdatePrev (this);
	}

	public double GetPrev(){
		return Prev;
	}

	public void ResetNext(){
		Next = double.PositiveInfinity;
	}

	public void ResetPrev(){
		Prev = double.NegativeInfinity;
	}

	public bool NextSet(){
		return !double.IsPositiveInfinity (Next);
	}

	public bool PrevSet(){
		return !double.IsNegativeInfinity (Next);
	}

	public bool NeedUpdate(double ts) {
		if (ts < Prev || ts > Next)
			return true;

		return false;
	}

}
