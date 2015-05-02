using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile_Node : MonoBehaviour {

	//Fields
	public enum NeighborNodes {UpLeft = 0, Up = 1, UpRight = 2, Right = 3, DownRight = 4, Down = 5, DownLeft = 6, Left = 7};
	public Tile_Node[] neighborList = new Tile_Node[8];
	public bool visible = false;
		
	public float costSoFar, heuristicValue, totalEstimatedValue;
	
	public Tile_Node prevNode;

	// Use this for initialization
	void Start ()
	{
		renderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update ()
	{

	}

	//Turn the node invisible
	public void TurnInvisible() 
	{
		renderer.enabled = false;
	}

	//Turn the node visible
	public void TurnVisible()
	{
		renderer.enabled = true;
	}

	public int Compare(Tile_Node x, Tile_Node y)
	{
		int compareResult = x.totalEstimatedValue.CompareTo (y.totalEstimatedValue);
		if (compareResult == 0) {
			return(x.heuristicValue.CompareTo (y.heuristicValue));
		}
		return compareResult;
	}
}
