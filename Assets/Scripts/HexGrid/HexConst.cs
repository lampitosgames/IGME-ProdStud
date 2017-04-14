﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of static functions and variables used for hex grid calculations
/// </summary>
public static class HexConst {
    //Side length of a hex. Also distance from the hex center to any of the hex corners
    //Change these values if/when the hex model changes
    public const float radius = 2.52f;
    public const float height = 0.1282126f * 4f;

    /// <summary>
    /// Convert hex coordinates to world coordinates
    /// </summary>
    /// <param name="q">column</param>
    /// <param name="r">row</param>
    /// <param name="h">height</param>
    /// <returns>point in world</returns>
    public static Vector3 HexToWorldCoord(int q, int r, int h) {
        float x = HexConst.radius * 1.5f * r;
        float y = HexConst.height * h;
        float z = HexConst.radius * Mathf.Sqrt(3) * (q + (r / 2f));
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Get world coordinates and convert them to hex coordinates
    /// </summary>
    /// <param name="pos">a point in the world</param>
    /// <returns>integer array of axial hex coordinates</returns>
    public static int[] CoordToHexIndex(Vector3 pos) {
        float q = (pos.z * Mathf.Sqrt(3f) / 3f - pos.x / 3f) / HexConst.radius;
        float r = (pos.x * (2f / 3f)) / HexConst.radius;
        float h = pos.y / HexConst.height;
        return hexRound(q, r, h);
    }

    /// <summary>
    /// Convert from axial hex coordinates to cube hex coordinates
    /// </summary>
    /// <param name="q">column</param>
    /// <param name="r">row</param>
    /// <param name="h">height</param>
    /// <returns>cube coordinate array [x, y, z, h]</returns>
    public static int[] AxialToCube(int q, int r, int h) {
        // x = q;
        // z = r;
        // y = -x - z;
        return new int[] { q, -q - r, r, h };
    }

    /// <summary>
    /// Convert from cube to axial hex coordinates.
    /// does so by simply dropping the 'y' axis of the cube coordinates
    /// </summary>
    /// <param name="x">cube x coord</param>
    /// <param name="y">cube y coord</param>
    /// <param name="z">cube z coord</param>
    /// <param name="h">height</param>
    /// <returns>axial coordinate array [q, r, h]</returns>
    public static int[] CubeToAxial(int x, int y, int z, int h) {
        return new int[] { x, z, h };
    }

    /// <summary>
    /// Rounds axial hex coordinates to make sure they satisfy the x + y + z = 0 rule of cube coordinates
    /// </summary>
    /// <param name="q">partial axial column coordinate</param>
    /// <param name="r">partial axial row coordinate</param>
    /// <param name="h">un-rounded height coordinate</param>
    /// <returns>verified axial coordinates</returns>
    private static int[] hexRound(float q, float r, float h) {
        //Convert axial to cube coordinates for the algorithm
        //Not using the provided static methods because input values haven't been rounded to ints
        float x = q;
        float z = r;
        float y = -x - z;

        //Round the floats
        float rx = Mathf.Round(x);
        float ry = Mathf.Round(y);
        float rz = Mathf.Round(z);

        //Find how much each value had to be rounded by.  These will always be <=0.5
        float x_diff = Mathf.Abs(rx - x);
        float y_diff = Mathf.Abs(ry - y);
        float z_diff = Mathf.Abs(rz - z);

        //If the x difference is the largest when rounded, make it the changed parameter
        if (x_diff > y_diff && x_diff > z_diff) {
            rx = -ry - rz;
        //If y is largest, it is the changed parameter
        } else if (y_diff > z_diff) {
            ry = -rx - rz;
        //z is largest and it becomes the changed parameter
        } else {
            rz = -rx - ry;
        }

        //Convert cube coords to axial
        int[] returnArray = { Mathf.RoundToInt(rx), Mathf.RoundToInt(rz), Mathf.RoundToInt(h) };
        return returnArray;
    }
}
