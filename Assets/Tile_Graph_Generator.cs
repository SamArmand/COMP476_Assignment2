using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Tile_Graph_Generator : MonoBehaviour
{

    //Fields
    public Tile_Node tileNode;
    public Vector3 levelSize;
    public float radius;
    public Vector3 tileSize;
    public float tileSize_Density;

    public List<Tile_Node> tileNodeList = new List<Tile_Node>();
    public List<PoV_Node> povNodeList = new List<PoV_Node>();

    bool executeOnceFlag = false;

    public Tile_Node startTileNode, endTileNode;
    public PoV_Node startPovNode, endPovNode;

    public List<Tile_Node> tilePathList = new List<Tile_Node>();
    public List<Tile_Node> tileOpenList = new List<Tile_Node>();
    public List<Tile_Node> tileClosedList = new List<Tile_Node>();
    public List<PoV_Node> povPathList = new List<PoV_Node>();
    public List<PoV_Node> povOpenList = new List<PoV_Node>();
    public List<PoV_Node> povClosedList = new List<PoV_Node>();

    public NPC npc;

    public Tile_Node endTileNodeIndicator;
    public PoV_Node endPoVNodeIndicator;

    public List<Tile_Node> smallRoom1TileNodes = new List<Tile_Node>();
    public List<Tile_Node> smallRoom2TileNodes = new List<Tile_Node>();
    public List<Tile_Node> smallRoom3TileNodes = new List<Tile_Node>();

    public List<PoV_Node> smallRoom1PoVNodes = new List<PoV_Node>();
    public List<PoV_Node> smallRoom2PoVNodes = new List<PoV_Node>();
    public List<PoV_Node> smallRoom3PoVNodes = new List<PoV_Node>();

    public List<List<float>> povClusterLookup = new List<List<float>>();
    public List<List<float>> tileClusterLookup = new List<List<float>>();

    public bool tileMode = true;

    public Tile_Node randomTileNode;
    public PoV_Node randomPoVNode;

    private int smallRoom, otherSmallRoom;

    public enum Modes {DIJKSTRA, EUCLIDEAN, CLUSTER};
    public Modes mode = Modes.CLUSTER;

    // Use this for initialization
    void Start()
    {
        levelSize = new Vector3(50, 0, 50);
        tileSize = new Vector3(levelSize.x / tileSize_Density, 0, levelSize.z / tileSize_Density);
        radius = tileSize.x / 2;

        //Generate the tiles, initialize the start node, and make the first calculation
        Scan();

        smallRoom = UnityEngine.Random.Range(0, 3);

        switch (smallRoom)
        {
        case 0:
            randomTileNode = smallRoom1TileNodes[UnityEngine.Random.Range(0, smallRoom1TileNodes.Count)];
            npc.transform.position = new Vector3(randomTileNode.transform.position.x, npc.transform.position.y, randomTileNode.transform.position.z);
            break;
        case 1:
            randomTileNode = smallRoom2TileNodes[UnityEngine.Random.Range(0, smallRoom2TileNodes.Count)];
            npc.transform.position = new Vector3(randomTileNode.transform.position.x, npc.transform.position.y, randomTileNode.transform.position.z);
            break;
        case 2:
            randomTileNode = smallRoom3TileNodes[UnityEngine.Random.Range(0, smallRoom3TileNodes.Count)];
            npc.transform.position = new Vector3(randomTileNode.transform.position.x, npc.transform.position.y, randomTileNode.transform.position.z);
            break;
        }

        generateLookupTable(8);

        ClearTile();
        ClearPov();
        FindStartNode();
        FindEndNode(smallRoom);

        startTileNode.costSoFar = 0;

        calculateCluster_Tile();
    }


    // Update is called once per frame
    public int pathCounter = 0;
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E))
        {
            mode = Modes.EUCLIDEAN;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            mode = Modes.DIJKSTRA;
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            mode = Modes.CLUSTER;
        }

        if (tileMode)
        {

            //Color the nodes according to their nature.
            foreach (Tile_Node node in tileNodeList)
            {
                node.TurnVisible();
                node.renderer.material.color = Color.white;
            }
            foreach (Tile_Node node in tileOpenList)
            {
                node.renderer.material.color = Color.cyan;
            }
            foreach (Tile_Node node in tileClosedList)
            {
                node.renderer.material.color = Color.yellow;
            }
            foreach (Tile_Node node in tilePathList)
            {
                node.renderer.material.color = Color.green;
            }
            startTileNode.renderer.material.color = Color.blue;
            endTileNode.renderer.material.color = Color.red;

            //If the NPC is in the middle of a pathfinding journey, let it continue
            if (tilePathList.Count > pathCounter && endTileNode == tilePathList[tilePathList.Count - 1])
            {
                endTileNodeIndicator.transform.position = endTileNode.transform.position;
                if (Vector3.Angle(npc.transform.forward, (tilePathList[pathCounter].transform.position - npc.transform.position)) > 35)
                {
                    npc.Steering_Stop();
                    npc.rotateTowards(tilePathList[pathCounter].transform.position);
                }
                else
                {
                    if (pathCounter == tilePathList.Count - 1)
                    {
                        npc.Steering_Arrive(tilePathList[pathCounter].transform.position, true);
                    }
                    else
                    {
                        npc.Steering_Arrive(tilePathList[pathCounter].transform.position, false);
                    }
                }
                bool nodeAttained = false;
                Collider[] collisionArray = Physics.OverlapSphere(npc.transform.position, 0.2f);
                for (int i = 0; i < collisionArray.Length; i++)
                {
                    if (collisionArray[i].GetComponent(typeof(Tile_Node)) == tilePathList[pathCounter])
                    {
                        nodeAttained = true;
                    }
                }

                if (nodeAttained)
                {
                    pathCounter++;
                }
            }

            else
            {
                tileMode = false;
                foreach (Tile_Node n in tileNodeList)
                {
                    n.TurnInvisible();
                }
                foreach (PoV_Node n in povNodeList)
                {
                    n.TurnVisible();
                }
                CalculateNewPovPath();

            }

        }

        else
        {
            //Color the nodes according to their nature.
            foreach (PoV_Node node in povNodeList)
            {
                node.TurnVisible();
                node.renderer.material.color = Color.white;
            }
            foreach (PoV_Node node in povOpenList)
            {
                node.renderer.material.color = Color.cyan;
            }
            foreach (PoV_Node node in povClosedList)
            {
                node.renderer.material.color = Color.yellow;
            }
            foreach (PoV_Node node in povPathList)
            {
                node.renderer.material.color = Color.green;
            }
            startPovNode.renderer.material.color = Color.blue;
            endPovNode.renderer.material.color = Color.red;

            //If the NPC is in the middle of a pathfinding journey, let it continue
            if (povPathList.Count > pathCounter && endPovNode == povPathList[povPathList.Count - 1])
            {

                endPoVNodeIndicator.transform.position = endPovNode.transform.position;
                if (Vector3.Angle(npc.transform.forward, (povPathList[pathCounter].transform.position - npc.transform.position)) > 35)
                {
                    npc.Steering_Stop();
                    npc.rotateTowards(povPathList[pathCounter].transform.position);
                }
                else
                {
                    if (pathCounter == povPathList.Count - 1)
                    {
                        npc.Steering_Arrive(povPathList[pathCounter].transform.position, true);
                    }
                    else
                    {
                        npc.Steering_Arrive(povPathList[pathCounter].transform.position, false);
                    }
                }
                bool nodeAttained = false;
                Collider[] collisionArray = Physics.OverlapSphere(npc.transform.position, 0.2f);
                for (int i = 0; i < collisionArray.Length; i++)
                {
                    if (collisionArray[i].GetComponent(typeof(PoV_Node)) == povPathList[pathCounter])
                    {
                        nodeAttained = true;
                    }
                }

                if (nodeAttained)
                {
                    pathCounter++;
                }
            }

            else
            {
                tileMode = true;
                foreach (PoV_Node n in povNodeList)
                {
                    n.TurnInvisible();
                }
                foreach (Tile_Node n in tileNodeList)
                {
                    n.TurnVisible();
                }
                CalculateNewTilePath();

            }

        }

    }

    private void CalculateNewPovPath()
    {
        povOpenList.Clear();
        povClosedList.Clear();
        povPathList.Clear();

        pathCounter = 0;

        //Finding closest PoV_Node to NPC


        startPovNode = povNodeList[0];

        startPovNode.costSoFar = 0;

        foreach (PoV_Node n in povNodeList)
        {
            if (Cost(npc.transform, n.transform) < Cost(npc.transform, startPovNode.transform))
            {
                startPovNode = n;
            }
        }

        smallRoom = otherSmallRoom;

        FindEndNode(smallRoom);

        switch (mode)
        {
        case Modes.CLUSTER:
            calculateCluster_Pov();
            break;
        case Modes.DIJKSTRA:
            calculateDijkstra_Pov();
            break;
        case Modes.EUCLIDEAN:
            calculateEuclidean_Pov();
            break;
        }

    }

    private void CalculateNewTilePath()
    {
        tileOpenList.Clear();
        tileClosedList.Clear();
        tilePathList.Clear();

        pathCounter = 0;

        //Finding closest PoV_Node to NPC
        startTileNode = tileNodeList[0];

        startTileNode.costSoFar = 0;

        foreach (Tile_Node n in tileNodeList)
        {
            if (Cost(npc.transform, n.transform) < Cost(npc.transform, startTileNode.transform))
            {
                startTileNode = n;
            }
        }

        smallRoom = otherSmallRoom;

        FindEndNode(smallRoom);

        switch (mode)
        {
        case Modes.CLUSTER:
            calculateCluster_Tile();
            break;
        case Modes.DIJKSTRA:
            calculateDijkstra_Tile();
            break;
        case Modes.EUCLIDEAN:
            calculateEuclidean_Tile();
            break;
        }

    }


    //Scan the graph for tile placements
    void Scan()
    {

        GameObject[] ns = GameObject.FindGameObjectsWithTag("tile_node");

        foreach (GameObject g in ns)
        {
            tileNodeList.Add(g.GetComponent<Tile_Node>());
        }

        for (int i = 0; i < tileNodeList.Count; i++)
        {
            GenerateNeighbors(tileNodeList[i]);
        }

        foreach (Tile_Node node in tileNodeList)
        {
            Vector3 p = node.transform.position;
            if (p.x <= -16 && p.z <= -16)
            {
                smallRoom1TileNodes.Add(node);
            }
            else if (p.x <= -20 && p.z >= 18)
            {
                smallRoom2TileNodes.Add(node);
            }
            else if (p.x >= 16 && p.z <= 8 && p.z >= 6)
            {
                smallRoom3TileNodes.Add(node);
            }
        }

        GameObject[] povs = GameObject.FindGameObjectsWithTag("pov_node");

        foreach (GameObject o in povs)
        {

            povNodeList.Add(o.GetComponent<PoV_Node>());
            o.GetComponent<PoV_Node>().TurnVisible();
        }

        foreach (PoV_Node node in povNodeList)
        {
            Vector3 p = node.transform.position;
            if (p.x <= -16 && p.z <= -16)
            {
                smallRoom1PoVNodes.Add(node);
            }
            else if (p.x <= -20 && p.z >= 18)
            {
                smallRoom2PoVNodes.Add(node);
            }
            else if (p.x >= 16 && p.z <= 8 && p.z >= 6)
            {
                smallRoom3PoVNodes.Add(node);
            }
        }

    }

    //Find the start node according to the position of the NPC
    void FindStartNode()
    {
        for (int i = 0; i < tileNodeList.Count; i++)
        {
            if (i == 0)
            {
                startTileNode = tileNodeList[i];
            }
            else
            {
                if ((npc.transform.position - tileNodeList[i].transform.position).magnitude < (npc.transform.position - startTileNode.transform.position).magnitude)
                {
                    startTileNode = tileNodeList[i];
                }
            }
        }
    }

    //Find the end node according to the position end node indicator
    void FindEndNode(int smallRoom)
    {

        do
        {
            otherSmallRoom = UnityEngine.Random.Range(0, 3);
        } while (otherSmallRoom == smallRoom);

        switch (otherSmallRoom)
        {
        case 0:
            if (tileMode)
                endTileNode = smallRoom1TileNodes[UnityEngine.Random.Range(0, smallRoom1TileNodes.Count)];
            else
                endPovNode = smallRoom1PoVNodes[UnityEngine.Random.Range(0, smallRoom1PoVNodes.Count)];
            break;
        case 1:
            if (tileMode)
                endTileNode = smallRoom2TileNodes[UnityEngine.Random.Range(0, smallRoom2TileNodes.Count)];
            else
                endPovNode = smallRoom2PoVNodes[UnityEngine.Random.Range(0, smallRoom2PoVNodes.Count)];
            break;
        case 2:
            if (tileMode)
                endTileNode = smallRoom3TileNodes[UnityEngine.Random.Range(0, smallRoom3TileNodes.Count)];
            else
                endPovNode = smallRoom3PoVNodes[UnityEngine.Random.Range(0, smallRoom3PoVNodes.Count)];
            break;
        }

        /*
        for (int i = 0; i < tileNodeList.Count; i++) 
		{
			if (i == 0) 
			{
				endTileNode = tileNodeList[i];
			}
			else {
				if ((endTileNodeIndicator.transform.position - tileNodeList[i].transform.position).magnitude < (endTileNodeIndicator.transform.position - endTileNode.transform.position).magnitude) {
					endTileNode = tileNodeList[i];
				}
			}
		}
        */
    }

    //Generate the neighbors for a Tile_Node
    void GenerateNeighbors(Tile_Node node)
    {
        node.neighborList[(int)Tile_Node.NeighborNodes.UpLeft] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(-tileSize.x, 0, tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.Up] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(0, 0, tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.UpRight] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(tileSize.x, 0, tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.Right] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(tileSize.x, 0, 0)));
        node.neighborList[(int)Tile_Node.NeighborNodes.DownRight] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(tileSize.x, 0, -tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.Down] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(0, 0, -tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.DownLeft] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(-tileSize.x, 0, -tileSize.z)));
        node.neighborList[(int)Tile_Node.NeighborNodes.Left] = tileNodeList.Find(n => (n.transform.position - node.transform.position) == (new Vector3(-tileSize.x, 0, 0)));
    }

    void calculateDijkstra_Tile()
    {
        tileOpenList.Add(startTileNode);

        while (tileOpenList.Count > 0 || tileClosedList.Count != tileNodeList.Count)
        {
            Tile_Node currentNode = tileOpenList[0];

            foreach (Tile_Node candidateNode in tileOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            tileOpenList.Remove(currentNode);
            tileClosedList.Add(currentNode);

            foreach (Tile_Node neighbor in currentNode.neighborList)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool intileOpenList = false;
                bool inClosedList = false;

                if (tileClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (tileOpenList.Contains(neighbor))
                    intileOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));

                if (tileClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileClosedList.Remove(neighbor);
                    tileOpenList.Add(neighbor);

                }

                else if (intileOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !intileOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileOpenList.Add(neighbor);
                }

            }



        }

        tilePathList.Add(endTileNode);
        while (true)
        {

            if (tilePathList[tilePathList.Count - 1].prevNode == startTileNode)
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
                tilePathList.Reverse();
                return;
            }
            else
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
            }
        }

    }

    void calculateEuclidean_Tile()
    {
        tileOpenList.Add(startTileNode);
        startTileNode.heuristicValue = Cost(startTileNode.transform, endTileNode.transform);
        startTileNode.totalEstimatedValue = startTileNode.costSoFar + startTileNode.heuristicValue;

        while (tileOpenList.Count > 0)
        {
            Tile_Node currentNode = tileOpenList[0];

            foreach (Tile_Node candidateNode in tileOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            if (currentNode == endTileNode)
            {
                break;
            }

            tileOpenList.Remove(currentNode);
            tileClosedList.Add(currentNode);

            foreach (Tile_Node neighbor in currentNode.neighborList)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool intileOpenList = false;
                bool inClosedList = false;

                if (tileClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (tileOpenList.Contains(neighbor))
                    intileOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));
                neighbor.heuristicValue = 3*Cost(neighbor.transform, endTileNode.transform);

                if (tileClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileClosedList.Remove(neighbor);
                    tileOpenList.Add(neighbor);

                }

                else if (intileOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !intileOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileOpenList.Add(neighbor);
                }

            }



        }

        tilePathList.Add(endTileNode);
        while (true)
        {

            if (tilePathList[tilePathList.Count - 1].prevNode == startTileNode)
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
                tilePathList.Reverse();
                return;
            }
            else
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
            }
        }

    }

    void calculateCluster_Tile()
    {
        tileOpenList.Add(startTileNode);
        startTileNode.heuristicValue = Cost(startTileNode.transform, endTileNode.transform);
        startTileNode.totalEstimatedValue = startTileNode.costSoFar + startTileNode.heuristicValue;

        while (tileOpenList.Count > 0)
        {
            Tile_Node currentNode = tileOpenList[0];

            foreach (Tile_Node candidateNode in tileOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            if (currentNode == endTileNode)
            {
                break;
            }

            tileOpenList.Remove(currentNode);
            tileClosedList.Add(currentNode);

            foreach (Tile_Node neighbor in currentNode.neighborList)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool intileOpenList = false;
                bool inClosedList = false;

                if (tileClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (tileOpenList.Contains(neighbor))
                    intileOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));
                neighbor.heuristicValue = 3*Cost(neighbor.transform, endTileNode.transform)
                    + getTileClusterHeuristics(neighbor.gameObject.layer, endTileNode.gameObject.layer);

                if (tileClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileClosedList.Remove(neighbor);
                    tileOpenList.Add(neighbor);

                }

                else if (intileOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !intileOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    tileOpenList.Add(neighbor);
                }

            }



        }

        tilePathList.Add(endTileNode);
        while (true)
        {

            if (tilePathList[tilePathList.Count - 1].prevNode == startTileNode)
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
                tilePathList.Reverse();
                return;
            }
            else
            {
                tilePathList.Add(tilePathList[tilePathList.Count - 1].prevNode);
            }
        }
    }
    void calculateDijkstra_Pov()
    {
        povOpenList.Add(startPovNode);

        while (povOpenList.Count > 0 || povClosedList.Count != povNodeList.Count)
        {
            PoV_Node currentNode = povOpenList[0];

            foreach (PoV_Node candidateNode in povOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            povOpenList.Remove(currentNode);
            povClosedList.Add(currentNode);

            foreach (PoV_Node neighbor in currentNode.neighbors)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool inOpenList = false;
                bool inClosedList = false;

                if (povClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (povOpenList.Contains(neighbor))
                    inOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));

                if (povClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povClosedList.Remove(neighbor);
                    povOpenList.Add(neighbor);

                }

                else if (inOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !inOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povOpenList.Add(neighbor);
                }

            }

        }

        povPathList.Add(endPovNode);
        while (true)
        {

            if (povPathList[povPathList.Count - 1].prevNode == startPovNode)
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
                povPathList.Reverse();
                return;
            }
            else
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
            }
        }

    }

    void calculateEuclidean_Pov()
    {
        povOpenList.Add(startPovNode);
        startPovNode.heuristicValue = Cost(startPovNode.transform, endPovNode.transform);
        startPovNode.totalEstimatedValue = startPovNode.costSoFar + startPovNode.heuristicValue;

        while (povOpenList.Count > 0 || povClosedList.Count != povNodeList.Count)
        {
            PoV_Node currentNode = povOpenList[0];

            foreach (PoV_Node candidateNode in povOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            if (currentNode == endPovNode)
            {
                break;
            }

            povOpenList.Remove(currentNode);
            povClosedList.Add(currentNode);

            foreach (PoV_Node neighbor in currentNode.neighbors)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool inOpenList = false;
                bool inClosedList = false;

                if (povClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (povOpenList.Contains(neighbor))
                    inOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));

                neighbor.heuristicValue = 3*Cost(neighbor.transform, endPovNode.transform);

                if (povClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povClosedList.Remove(neighbor);
                    povOpenList.Add(neighbor);

                }

                else if (inOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !inOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povOpenList.Add(neighbor);
                }

            }

        }

        povPathList.Add(endPovNode);
        while (true)
        {

            if (povPathList[povPathList.Count - 1].prevNode == startPovNode)
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
                povPathList.Reverse();
                return;
            }
            else
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
            }
        }

    }


    void calculateCluster_Pov()
    {
        povOpenList.Add(startPovNode);
        startPovNode.heuristicValue = Cost(startPovNode.transform, endPovNode.transform);
        startPovNode.totalEstimatedValue = startPovNode.costSoFar + startPovNode.heuristicValue;

        while (povOpenList.Count > 0 || povClosedList.Count != povNodeList.Count)
        {
            PoV_Node currentNode = povOpenList[0];

            foreach (PoV_Node candidateNode in povOpenList)
            {
                if (candidateNode.totalEstimatedValue < currentNode.totalEstimatedValue)
                {
                    currentNode = candidateNode;
                }
            }

            if (currentNode == endPovNode)
            {
                break;
            }

            povOpenList.Remove(currentNode);
            povClosedList.Add(currentNode);

            foreach (PoV_Node neighbor in currentNode.neighbors)
            {
                if (neighbor == null)
                {
                    continue;
                }

                bool inOpenList = false;
                bool inClosedList = false;

                if (povClosedList.Contains(neighbor))
                    inClosedList = true;
                else if (povOpenList.Contains(neighbor))
                    inOpenList = true;


                float newCost = (currentNode.costSoFar + Cost(currentNode.transform, neighbor.transform));

                neighbor.heuristicValue = 3*Cost(neighbor.transform, endPovNode.transform)
                    + getPovClusterHeuristics(neighbor.gameObject.layer, endPovNode.gameObject.layer);

                if (povClosedList.Contains(neighbor) && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povClosedList.Remove(neighbor);
                    povOpenList.Add(neighbor);

                }

                else if (inOpenList && newCost < neighbor.costSoFar)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                }

                else if (!inClosedList && !inOpenList)
                {
                    neighbor.costSoFar = newCost;
                    neighbor.totalEstimatedValue = neighbor.costSoFar + neighbor.heuristicValue;
                    neighbor.prevNode = currentNode;
                    povOpenList.Add(neighbor);
                }

            }

        }

        povPathList.Add(endPovNode);
        while (true)
        {

            if (povPathList[povPathList.Count - 1].prevNode == startPovNode)
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
                povPathList.Reverse();
                return;
            }
            else
            {
                povPathList.Add(povPathList[povPathList.Count - 1].prevNode);
            }
        }

    }

    private void ClearPov()
    {
        povOpenList.Clear();
        povClosedList.Clear();
        povPathList.Clear();

        foreach (PoV_Node node in povNodeList)
        {
            node.costSoFar = 0;
            node.totalEstimatedValue = 0;
            node.heuristicValue = 0;
        }
    }

    private void ClearTile()
    {
        tileOpenList.Clear();
        tileClosedList.Clear();
        tilePathList.Clear();

        foreach (Tile_Node node in tileNodeList)
        {
            node.costSoFar = 0;
            node.totalEstimatedValue = 0;
            node.heuristicValue = 0;
        }
    }

    private PoV_Node GetPovNodeInCluster(int layerId)
    {
        foreach (PoV_Node node in povNodeList)
        {
            if (node.gameObject.layer == layerId)
                return node;
        }
        return null;
    }

    private Tile_Node GetTileNodeInCluster(int layerId)
    {
        foreach (Tile_Node node in tileNodeList)
        {
            if (node.gameObject.layer == layerId)
                return node;
        }
        return null;
    }


    private void generateLookupTable(int numberOfLayers)
    {

        //-----------------------------------
        //      Pov Lookup Table Generation
        //-----------------------------------
        #region POV Lookup Generation
        for (int i = 0; i < numberOfLayers; ++i)
        {
            povClusterLookup.Add(new List<float>());
            for (int j = 0; j < numberOfLayers; ++j)
            {
                if (i == j)
                {
                    povClusterLookup[i].Add(0);
                }
                else
                {


                    //Get a shortest path using euclidean heuristics
                    int startLayerIndex = LayerMask.NameToLayer("Cluster" + (i + 1));
                    int endLayerIndex = LayerMask.NameToLayer("Cluster" + (j + 1));

                    //Start node will be in cluster "i"
                    startPovNode = GetPovNodeInCluster(startLayerIndex);

                    //End node will be in cluster "j"
                    endPovNode = GetPovNodeInCluster(endLayerIndex);
                    ClearPov();
                    //calculateEuclidean_Pov();
                    calculateDijkstra_Pov();

                    povClusterLookup[i].Add(calculatePovClusterWeight(povPathList, startLayerIndex, endLayerIndex) * 1000000);
                }
            }
        }
        #endregion


        //-----------------------------------
        //      Tile Lookup Table Generation
        //-----------------------------------
        #region Tile Lookup Table
        for (int i = 0; i < numberOfLayers; ++i)
        {
            tileClusterLookup.Add(new List<float>());
            for (int j = 0; j < numberOfLayers; ++j)
            {
                if (i == j)
                {
                    tileClusterLookup[i].Add(0);
                }
                else
                {

                    //Get a shortest path using euclidean heuristics
                    int startLayerIndex = LayerMask.NameToLayer("Cluster" + (i + 1));
                    int endLayerIndex = LayerMask.NameToLayer("Cluster" + (j + 1));

                    //Start node will be in cluster "i"
                    startTileNode = GetTileNodeInCluster(startLayerIndex);

                    //End node will be in cluster "j"
                    endTileNode = GetTileNodeInCluster(endLayerIndex);
                    ClearTile();
                    //calculateEuclidean_Tile();
                    calculateDijkstra_Tile();

                    tileClusterLookup[i].Add(calculateTileClusterWeight(tilePathList, startLayerIndex, endLayerIndex) * 1000000);
                }
            }
        }

        #endregion


    }

    private float calculateTileClusterWeight(List<Tile_Node> pathList, int startLayer, int endlayer)
    {
        int rootIndex = 0;

        //Find the root index
        for (int i = 0; i < pathList.Count; ++i)
        {
            if (pathList[i].gameObject.layer == startLayer)
            {
                rootIndex = i;
            }
        }

        int endIndex = 0;
        //Find the end node index
        for (int i = pathList.Count - 1; i >= 0; --i)
        {
            if (pathList[i].gameObject.layer == endlayer)
            {
                endIndex = i;
            }
        }

        //Check if start and end index are neighbours
        if (endIndex - rootIndex == 1)
        {
            return 1;
        }

        float totalWeight = 0;
        for (int i = rootIndex; i < endIndex - 1; ++i)
        {
            Vector3 currentNodePosition = pathList[i].gameObject.transform.position;
            Vector3 targetNodePosition = pathList[i + 1].gameObject.transform.position;
            totalWeight += Vector3.Distance(currentNodePosition, targetNodePosition);
        }
        return totalWeight;
    }

    private float calculatePovClusterWeight(List<PoV_Node> pathList, int startLayer, int endlayer)
    {
        int rootIndex = 0;

        //Find the root index
        for (int i = 0; i < pathList.Count; ++i)
        {
            if (pathList[i].gameObject.layer == startLayer)
            {
                rootIndex = i;
            }
        }

        int endIndex = 0;
        //Find the end node index
        for (int i = pathList.Count - 1; i >= 0; --i)
        {
            if (pathList[i].gameObject.layer == endlayer)
            {
                endIndex = i;
            }
        }

        //Check if start and end index are neighbours
        if (endIndex - rootIndex == 1)
        {
            return 1;
        }

        float totalWeight = 0;
        for (int i = rootIndex; i < endIndex - 1; ++i)
        {
            Vector3 currentNodePosition = pathList[i].gameObject.transform.position;
            Vector3 targetNodePosition = pathList[i + 1].gameObject.transform.position;
            totalWeight += Vector3.Distance(currentNodePosition, targetNodePosition);
        }
        return totalWeight;
    }

    private float getPovClusterHeuristics(int currentLayer, int targetLayer)
    {
        int currentLayerIndex = layerToClusterIndex(currentLayer);
        int targetLayerIndex = layerToClusterIndex(targetLayer);

        if (currentLayerIndex > povClusterLookup.Count || targetLayerIndex > povClusterLookup[currentLayerIndex].Count)
            return 0;
        print(currentLayerIndex + " " + targetLayerIndex);
        return povClusterLookup[currentLayerIndex][targetLayerIndex];
    }

    private float getTileClusterHeuristics(int currentLayer, int targetLayer)
    {
        int currentLayerIndex = layerToClusterIndex(currentLayer);
        int targetLayerIndex = layerToClusterIndex(targetLayer);

        if (currentLayerIndex > tileClusterLookup.Count || targetLayerIndex > tileClusterLookup[currentLayerIndex].Count)
            return 0;

        return tileClusterLookup[currentLayerIndex][targetLayerIndex];
    }

    private int layerToClusterIndex(int layer)
    {

        return layer - LayerMask.NameToLayer("Cluster1");
    }



    private float Cost(Transform currentNode, Transform neighbor)
    {
        return (currentNode.position - neighbor.position).magnitude;
    }

}