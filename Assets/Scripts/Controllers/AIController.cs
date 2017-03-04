﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The AIController manages state and calculations for AI and pathfinding in the game.
/// </summary>
public class AIController : MonoBehaviour {
    //The path grid holds a list of AICells. These cells have useful information for pathfinding and AI awareness.
    public HexGrid<AICell> pathGrid = new HexGrid<AICell>();

    /// <summary>
    /// Allow getting/setting for the level grid using [q,r,h]
    /// </summary>
    /// <param name="q">column</param>
    /// <param name="r">row</param>
    /// <param name="h">height</param>
    public AICell this[int q, int r, int h] {
        get { return this.pathGrid[q, r, h]; }
        set { this.pathGrid[q, r, h] = value; }
    }

    /// <summary>
    /// A much slower GetRadius function, but more accurate.  It uses collision box projectsions to determine blocked edges that break
    /// connections to neighboring hex cells, even if those cells exist.  This is useful for finding valid paths through impassible game objects.
    /// </summary>
    /// <param name="cell">Center AICell</param>
    /// <param name="searchHeight">How high (up or down) does the algorithm search for neighbors?  -1 is unbounded</param>
    /// <returns></returns>
    public AICell[] ValidNeighbors(AICell cell, int searchHeight = 1) {
        //Get neighbors normally
        AICell[] neighbors = (AICell[])pathGrid.GetRadius(cell.q, cell.r, cell.h, 1, searchHeight);
        List<AICell> returnNeighbors = new List<AICell>();

        //Loop through all possible neighbors
        foreach (AICell n in neighbors) {
            //Get the vector that points to the edge of the hex in the direction of the neighbor
            Vector3 toEdge = (n.centerPos - cell.centerPos) / 2;

            //Create a rotation for the collider box (so it is orientated along the edge)
            Quaternion rotation = new Quaternion();
            rotation.SetLookRotation(toEdge.normalized, new Vector3(0, 1, 0));

            //Get the collider center position (in between the cells, 1 unit up)
            Vector3 colliderPos = toEdge + cell.centerPos + new Vector3(0, 1, 0);

            //Check the location for physics collisions (if it collides with the middle third of the edge)
            //TODO: Make sure this doesn't collide with the player.
            if (!Physics.CheckBox(colliderPos, new Vector3(HexConst.radius/6f, 0.5f, 0.1f), rotation)) {
                //If it is a valid location, add this to the list.
                returnNeighbors.Add(n);
            }
        }
        return returnNeighbors.ToArray();
    }

    /// <summary>
    /// With a given pathing distance, find all reachable cells in that many steps.
    /// 1 step is defined as a single movement from a start hex to an adjacent, unblocked hex
    /// </summary>
    /// <param name="center">start cell</param>
    /// <param name="steps">number of steps to search</param>
    /// <returns></returns>
    public List<AICell> ReachableInSteps(int[] center, int steps) {
        //Store a list of cells that are reachable within the number of steps
        List<AICell> visited = new List<AICell>();
        //Add the start to the visited list
        visited.Add(pathGrid[center[0], center[1], center[2]]);

        //The firnges list holds lists of cells at each tier of movement.
        //Index 0 contains the center cell
        //Index 1 contains all cells reachable in 1 step
        //Index 2 contains all cells reachable in 2 steps
        //Etc.  it might be helpful to return the fringes in the future for display purposes
        List<List<AICell>> fringes = new List<List<AICell>>();
        //Add the start cell at index 0
        fringes.Add(new List<AICell>());
        fringes[0].Add(pathGrid[center[0], center[1], center[2]]);

        //For every possible step
        for (int k=1; k<=steps; k++) {
            //Create a list for this index
            fringes.Add(new List<AICell>());
            //For each cell in the previous index
            foreach (AICell cell in fringes[k-1]) {
                //Expand it to visible neighbors
                AICell[] neighbors = ValidNeighbors(cell);
                //Add all visible neighbors to the visited set
                //Also add them to the current fringe index for use in the next iteration
                foreach (AICell n in neighbors) {
                    if (!visited.Contains(n)) {
                        visited.Add(n);
                        fringes[k].Add(n);
                    }
                }

            }
        }
        return visited;
    }

    /// <summary>
    /// A basic implementation of A*
    /// </summary>
    /// <param name="startCoords">start hex coordinate array</param>
    /// <param name="endCoords">end hex coordinate array</param>
    /// <returns>A list of hex coordinates that form a path.  Null if no path possible</returns>
    public List<int[]> PathBetween(int[] startCoords, int[] endCoords)
    {
        //Get references to the start and end path objects
        AICell cStart = pathGrid[startCoords[0], startCoords[1], startCoords[2]];
        AICell cEnd = pathGrid[endCoords[0], endCoords[1], endCoords[2]];
        //If the start is the end, there is no path
        if (cStart == null || cEnd == null) { return null; }

        //Initialize the open and closed sets.  Sets instead of lists because they need to be unique.
        HashSet<AICell> closed = new HashSet<AICell>();
        HashSet<AICell> open = new HashSet<AICell>();

        //Initialize the start node's values
        cStart.g = DistBetween(cStart, cEnd);
        open.Add(cStart);

        //While items exist in the open set
        while (open.Count > 0)
        {
            //Get the lowest g-valued cell;
            AICell cCell = null;
            foreach (AICell cell in open)
            {
                if (cCell == null) { cCell = cell; continue; }
                if (cCell.g > cell.g)
                {
                    cCell = cell;
                }
            }
            //If the current cell is the goal, return the found path
            if (cCell.Equals(cEnd))
            {
                return ReconstructPath(cCell, cStart);
            }

            //Remove cCell from open and add it to closed
            open.Remove(cCell);
            closed.Add(cCell);

            //Loop through the valid neighbors of cCell
            foreach (AICell nCell in ValidNeighbors(cCell))
            {
                //If the neighbor node is already in the closed set, don't evaluate
                if (closed.Contains(nCell)) { continue; }

                //If the neighbor isn't in the open set
                if (!open.Contains(nCell))
                {
                    //Initialize it's g-value and parent
                    nCell.g = cCell.g + DistBetween(cCell, nCell);
                    nCell.parent = cCell;
                    //Add it to the open set
                    open.Add(nCell);
                    //Neighbor is already in the open set
                }
                else
                {
                    //If moving from the cCell would be quicker than the current saved path
                    if (nCell.g > cCell.g + DistBetween(cCell, nCell))
                    {
                        //Override previous shortest-path data to the nCell
                        nCell.g = cCell.g + DistBetween(cCell, nCell);
                        nCell.parent = cCell;
                    }
                }
            }
        }
        //No path found, return null
        return null;
    }

    /// <summary>
    /// Get the distance between 2 cells (in hex cells)
    /// Basically, how many cells are needed to traverse from p1 to p2
    /// NOTE: Does not account for height differences
    /// </summary>
    /// <param name="cell1">Start cell</param>
    /// <param name="cell2">End cell</param>
    /// <returns>Integer cell distance</returns>
    public int DistBetween(AICell cell1, AICell cell2) {
        //Convert the axial coordinates to cube coordinates for both cells
        int[] ac = HexConst.AxialToCube(cell1.q, cell1.r, cell1.h);
        int[] bc = HexConst.AxialToCube(cell2.q, cell2.r, cell2.h);
        //Calculate the cell distance
        return ((int)Mathf.Abs(ac[0] - bc[0]) + (int)Mathf.Abs(ac[1] - bc[1]) + (int)Mathf.Abs(ac[2] - bc[2])) / 2;
    }

    public AICell[] Neighbors(AICell cell) {

        return null;
    }

    /// <summary>
    /// Recursively travel up the 'parent' chain and construct a list of integer coordinates that form a path.
    /// </summary>
    /// <param name="endCell">End of path</param>
    /// <param name="startCell">Start of path</param>
    /// <returns>A list of integer axial coordinate arrays that form a path</returns>
    private List<int[]> ReconstructPath(AICell endCell, AICell startCell) {
        //Create the return list
        List<int[]> returnList = new List<int[]>();
        //If the end cell is the same as the start cell, return an empty list (no path)
        if (endCell.Equals(startCell)) { return returnList; }
        //Create a temporary cell
        AICell temp = endCell;
        
        do {
            //Add temp to the return list
            returnList.Add(new int[] { temp.q, temp.r, temp.h });
            //Set temp to it's parent
            temp = temp.parent;
            //repeat

        //Loop while temp has not been set to the start cell
        } while (!temp.Equals(startCell));
        //Finally, add the start cell to the list and return the path
        returnList.Add(new int[] { startCell.q, startCell.r, startCell.h });
        return returnList;
    }
}