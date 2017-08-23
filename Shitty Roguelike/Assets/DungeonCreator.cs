using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class DungeonCreator : MonoBehaviour
{
    /* Brainstorming
     * We want an infinite procedural world
     * We should create blocks in a fixed area around the player
     * The Dungeon will be defined by rooms, corridors, doorways, enemies, and possibly miscellaneous items
     * Rooms:
     *  Rectangular
     *  X, Y, Width, Height (must be odd)
     * Corridors:
     *  1 tile (X, Y) and a direction (NS or EW)
     * Doorways:
     *  Same as corridors, but with different graphics. 
     *  Will have different graphics for NS/EW
     * Enemies:
     *  The enemy gameobject script will handle attacking. 
     *  Place enemies on a vacant room tile
     *  Enemies have health, attack, and level (calculated based on health, attack, and possibly other factors)
     * Misc:
     *  We are ignoring misc items for now
     * Algorithm:
     *  Start by randomly placing n rooms in the region that the player is standing in 
     *   Region player standing in should be the area the player can see, plus some excess
     *  Use Prim's algorithm to define corridors. Encourage straight corridors using a priority queue
     *   Start at an odd tile
     *   Add the wall neighbors as 2x2 matricies to the priority queue
     *      Weights assuming source corridor going up (positive y). Rotate for other directions
     *      [x+1, y; x+2, y] Random
     *      [x-1, y; x-2, y] Random
     *      [x, y-1, x, y-2] (don't add to queue)
     *      [x, y+1, x, y+2] Clamp01(Random + 0.25)
     *   Select the top off the priority queue and repeat
     *  Define Doorways as going between rooms and corridors
     *   Get possible tiles, (area around room, excluding corners)
     *   Select a random number of doorways
     *  Whenever the player moves 
     *   If the edges of the display region are odd, add a small chance to spawn new rooms and extend the corridors
     */
    public int areaAroundPlayerX = 40;
    public int areaAroundPlayerY = 30;
    public int initRoomAttempts = 200;
    public int seed;
    public bool useRandomSeed = false;
    public int minRoomSize = 3;
    public int maxRoomSize = 8;
    public GameObject wallPrefab;
    public GameObject roomFloorPrefab; // FIXME: Add room edge prefabs
    public float newRoomProb = 0.02f;

    private Random rand;

    private void Awake()
    {
        InitialGenerate();
        FindObjectOfType<PlayerController>().OnPlayerMove += PlayerController_OnPlayerMove;
    }

    public Dictionary<Coord, GameObject> CoordGameObjectMap;
    protected HashSet<Coord> exploredArea;

    private void PlayerController_OnPlayerMove(int h, int v, Coord PlayerPosition)
    {
        CheckHVValues(h, v);
        // We know only either h OR v is 1 (and one of them must be 1)
        bool horizontal = Mathf.Abs(h) > Mathf.Abs(v);
        bool positive = Mathf.Sign(horizontal ? h : v) == 1;

        // This represents the new total area to display. We will disable everything, then turn on the gameobjects inside this
        //Rect area = new Rect(
        //    PlayerPosition.x - areaAroundPlayerX / 2,
        //    PlayerPosition.y - areaAroundPlayerY / 2,
        //    PlayerPosition.x + areaAroundPlayerX / 2,
        //    PlayerPosition.y + areaAroundPlayerY / 2
        //    );
        Rect area = new Rect(PlayerPosition.x - areaAroundPlayerX / 2, PlayerPosition.y - areaAroundPlayerY / 2, 0, 0);
        area.End = new Coord(PlayerPosition.x + areaAroundPlayerX / 2, PlayerPosition.y + areaAroundPlayerY / 2);
        //Debug.Log(area);
        /* We need to know the players position
         * The players position is never recorded. 
         * It can be calculated based on the unity transform component
         *  but this is hackish
         * the player could keep a coord value as its position
         *  But this is extra bookkeeping and requires passing around another value
         * I think the second is the better solution. I'll see what I think in the morning
         */

        foreach (KeyValuePair<Coord, GameObject> kvp in CoordGameObjectMap)
        {
            if(area.Contains(kvp.Key) == false)
            {
                    kvp.Value.SetActive(false);
            }
            
        }


        // TODO: A lot of duplicated code between this and Initial Generate
        for (int x = area.start.x; x < area.End.x; x++)
        {
            for (int y = area.start.y; y < area.End.y; y++)
            {
                Coord c = new Coord(x, y);
                if(exploredArea.Contains(c) == false)
                {
                    if (c.x % 2 != 0 && c.y % 2 != 0)
                    {
                        if (rand.NextDouble() < newRoomProb)
                        {
                            int startX = c.x;
                            int startY = c.y;
                            int sizeX = NextRandomOdd(minRoomSize, maxRoomSize);
                            int sizeY = NextRandomOdd(minRoomSize, maxRoomSize);
                            Room room = new Room(startX, startY, sizeX, sizeY);
                            bool roomValid = CheckRoomValid(room);
                            if (roomValid)
                            {
                                rooms.Add(room);

                            }
                        }
                    }
                    exploredArea.Add(c);
                }
                if (CoordGameObjectMap.ContainsKey(c))
                {
                    CoordGameObjectMap[c].SetActive(true);
                }
                else
                {
                    GameObject prefab = GetPrefabForCoord(c);
                    GameObject obj = Instantiate(prefab, new Vector3(x, y), Quaternion.identity, this.transform);
                    obj.name = prefab.name + " " + x + " " + y;
                    RoomDebugInfo debug = obj.GetComponent<RoomDebugInfo>();
                    if (debug != null)
                    {
                        Room r;
                        IsInRoom(x, y, out r);
                        debug.Room = r;
                    }
                    CoordGameObjectMap.Add(c, obj);
                }

            }
        }

    }

    private bool CheckRoomValid(Room room)
    {
        bool roomValid = true;
        for (int x = room.start.x; x < room.End.x; x++)
        {
            if (roomValid == false)
                break;
            for (int y = room.start.y; y < room.End.y; y++)
            {
                if (roomValid == false)
                    break;
                if (IsInRoom(x, y))
                    roomValid = false;
            }
        }
        return roomValid;
    }

    private void InitialGenerate()
    {
        rooms = new List<Room>();
        CoordGameObjectMap = new Dictionary<Coord, GameObject>();
        exploredArea = new HashSet<Coord>();
        if (useRandomSeed)
            seed = (int)Network.time;
        rand = new Random(seed);
        for (int i = 0; i < initRoomAttempts; i++)
        {
            int startX = NextRandomOdd(-areaAroundPlayerX / 2, areaAroundPlayerX / 2);
            int startY = NextRandomOdd(-areaAroundPlayerY / 2, areaAroundPlayerY / 2);
            int sizeX = NextRandomOdd(minRoomSize, maxRoomSize);
            int sizeY = NextRandomOdd(minRoomSize, maxRoomSize);
            Room room = new Room(startX, startY, sizeX, sizeY);
            bool roomValid = CheckRoomValid(room);
            if (roomValid)
            {
                rooms.Add(room);
            }
        }

        for (int x = -areaAroundPlayerX / 2; x < areaAroundPlayerX / 2; x++)
        {
            for (int y = -areaAroundPlayerY / 2; y < areaAroundPlayerY / 2; y++)
            {
                Coord c = new Coord(x, y);
                GameObject prefab = GetPrefabForCoord(c);
                GameObject obj = Instantiate(prefab, new Vector3(x, y), Quaternion.identity, this.transform);
                obj.name = prefab.name + " " + x + " " + y;
                RoomDebugInfo debug = obj.GetComponent<RoomDebugInfo>();
                if (debug != null)
                {
                    Room r;
                    IsInRoom(x, y, out r);
                    debug.Room = r;
                }
                CoordGameObjectMap.Add(new Coord(x, y), obj);
                exploredArea.Add(c);
            }
        }
    }

    private GameObject GetPrefabForCoord(Coord c)
    {
        GameObject prefab = wallPrefab;

        Room r;
        if (IsInRoom(c.x, c.y, out r))
            prefab = roomFloorPrefab;
        return prefab;
    }

    private void CheckHVValues(int h, int v)
    {
        if (h == 0 && v == 0)
        {
            throw new UnityException("OnPlayerMove was called but h and v are both zero");
        }
        if (h + v > 1)
        {
            throw new UnityException("OnPlayerMove was called but the sum of h and v > 1");
        }
    }

    [Serializable]
    public class Rect
    {
        public Coord start;
        public Coord size;
        public Coord End
        {
            get
            {
                return new Coord(start.x + size.x, start.y + size.y);
            }
            set
            {
                size = new Coord(value.x - start.x, value.y - start.y);
            }
        }
        public Rect(int startX, int startY, int sizeX, int sizeY)
            : this(new Coord(startX, startY), new Coord(sizeX, sizeY)) { }
        public Rect(Coord start, Coord size)
        {
            this.start = start;
            this.size = size;
        }
        public bool Contains(Coord a)
        {
            return (a.x >= start.x && a.x < start.x + size.x && a.y >= start.y && a.y < start.y + size.y);
        }
        public override string ToString()
        {
            return "Rect: Start: (" + start.x + ", " + start.y + ") Size: (" + size.x + ", " + size.y + ")";
        }
    }

    // A room is a rect
    [Serializable]
    public class Room : Rect
    {
        public Room(int startX, int startY, int sizeX, int sizeY) : base(startX, startY, sizeX, sizeY) { }
        public Room(Coord start, Coord size) : base(start, size) { }
    }


    List<Room> rooms;


    private bool IsInRoom(int x, int y, out Room room)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Contains(new Coord(x, y)))
            {
                room = rooms[i];
                return true;
            }
        }
        room = null;
        return false;
    }

    private bool IsInRoom(int x, int y)
    {
        Room r;
        return IsInRoom(x, y, out r);
    }

    private int NextRandomOdd(int min, int max)
    {
        int a = 0;
        while (a % 2 == 0)
        {
            a = rand.Next(min, max);
        }
        return a;
    }
}
