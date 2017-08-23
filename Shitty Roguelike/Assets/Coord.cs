﻿using System;
using UnityEngine;


[Serializable]
public struct Coord
{
    public int x;
    public int y;
    public Coord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}