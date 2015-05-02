using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoV_Node : MonoBehaviour
{

    //Fields
    public List<PoV_Node> neighbors = new List<PoV_Node>();
    public bool visible = false;

    public float costSoFar, heuristicValue, totalEstimatedValue;

    public PoV_Node prevNode;

    // Use this for initialization
    void Start()
    {
        renderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
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

}
