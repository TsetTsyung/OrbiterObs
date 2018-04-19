using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region STRUCTS&CLASSES

public struct BulletDebug
{
    public string BulletPosition{ get; set; }
    public string AdjustedPosition { get; set; }
    public string Normal { get; set; }
}

public struct TileMarker
{
    public int X{ get; set; }
    public int Y{ get; set; }

    public TileMarker(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class SingleTileMeshData
{
    // Position in the database, and therefore the world
    public int X { get; set; }
    public int Y { get; set; }

    // Health of the tile - dictates it's texture and whether it exists or not
    public float Health { get; set; }

    // UV coords for the chosen texture
    public float TextureOffset { get; set; }
    public float HealthTextureOffset { get; set; }

    public float UCoord { get; set; }
    public float VCoord { get; set; }

    public TileState tileState;

    public SingleTileMeshData(int x, int y)
    {
        X = x;
        Y = y;
        Health = 100;
        TextureOffset = UnityEngine.Random.Range(0, 3) * 0.25f;
        HealthTextureOffset = 0.75f;
    }

}

#endregion

#region ENUMS
enum Direction { Left, Up, Right, Down };

public enum TileState { Empty, Filled, Completed, Ignore};
#endregion

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class TerrainMeshManager : MonoBehaviour {

    public float scale = 1f;
    public int gameWidth = 640;
    public Texture2D terrainSprite;
    public Material terrainTexture;

    private BulletDebug bulletDebug;
    
    // These are the 4 verts that will make up
    // any square (graphical or physical)      // Top left                   // Top right                 // Bottom Left                 // Bottom Right
    private Vector3[] vertices = { new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };

    private SingleTileMeshData[,] meshData;

    private int meshWidth;
    private int meshHeight;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D polygonCollider;
    private Direction dir;
    private Direction prevDir;
    private Vector3 offset = new Vector2();

    private List<Vector3> newVerts = new List<Vector3>();
    private List<int> newTriangles = new List<int>();
    private List<Vector2> newUVs = new List<Vector2>();
    private List<Vector2> newPath = new List<Vector2>();
    private ContactPoint2D[] contacts;
    private int contactPoints;

    void Awake()
    {
    }


    // Use this for initialization
    void Start() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        mesh = new Mesh();
        contacts = new ContactPoint2D[10];

        //float startTime = Time.realtimeSinceStartup;


        ////////////// DATABASE CONSTRUCTION!! //////////////
        #region DATABASECONSTRUCTION
        if (terrainSprite != null)
        {
            
            Color[] pixels = terrainSprite.GetPixels();
            meshWidth = terrainSprite.width;
            meshHeight = terrainSprite.height;

            int maxHeight = meshHeight;

            // Find the highest point in a linear fashion (hence weird for logic)
            // This is because the textures are stored in 'flat arrays', not 2D
            for (int j = meshHeight-1; j >= 0; j--)
            {
                for (int i = 0; i < meshWidth; i++)
                {
                    if (pixels[(j * meshWidth) + i] != Color.white)
                    {
                        // We've found the highest point of our Terrain, so lets store the height and bug out
                        maxHeight = j+1;
                        i = meshWidth;
                        j = 0;
                    }
                }
            }
            meshHeight = maxHeight;
            // Make a optimised database the right size

            meshData = new SingleTileMeshData[meshWidth, meshHeight];
            //Debug.Log("We have made a database that's " + meshData.GetLength(0) + " by " + meshData.GetLength(1));
            for (int j = 0; j < maxHeight; j++)
            {
                for (int i = 0; i < meshWidth; i++)
                {
                    //Debug.Log("We're making tile " + i + ", " + j);
                    meshData[i, j] = new SingleTileMeshData(i, j);
                    if (pixels[(j * meshWidth) + i] == Color.white)
                    {
                        meshData[i, j].tileState = TileState.Empty;
                    }
                    else
                    {
                        meshData[i, j].tileState = TileState.Filled;
                    }
                }
            }

        }
        else
        {
            // Fill our test data
            meshData = new SingleTileMeshData[4, 4];
            
            meshWidth = meshData.GetLength(0);
            meshHeight = meshData.GetLength(1);

            for (int i = 0; i < meshWidth; i++)
            {
                for (int j = 0; j < meshHeight; j++)
                {
                    meshData[i, j] = new SingleTileMeshData(i, j);
                }
            }

            meshData[0, 0].tileState = TileState.Filled;
            meshData[1, 0].tileState = TileState.Filled;
            meshData[0, 1].tileState = TileState.Filled;
            meshData[2, 1].tileState = TileState.Filled;
            meshData[2, 2].tileState = TileState.Filled;
            meshData[3, 3].tileState = TileState.Filled;
        }

        #endregion
        ////////////// END OF DATABASE CONSTRUCTION!! //////////////

        //startTime = Time.realtimeSinceStartup;
        // Check the array for tiles that cannot be outlined due to being blocked in
        FlagAsIgnore();
        // Build the mesh

        //startTime = Time.realtimeSinceStartup;
        offset = new Vector3(-(Mathf.Floor(meshWidth / 2)), -(Mathf.Floor(meshHeight / 2)), 0f);
        BuildMesh();
        
        // Build the Polygon Collider
        //startTime = Time.realtimeSinceStartup;

        BuildCollider();

        float endTime = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update() {

    }

    private void BuildMesh()
    {
        // Clear out all of the classes and lists
        mesh.Clear();
        newVerts.Clear();
        newVerts.TrimExcess();
        newUVs.Clear();
        newUVs.TrimExcess();
        newTriangles.Clear();
        newTriangles.TrimExcess();

        //Debug.Log("Offset is " + offset);
        //Debug.Log("meshWidth and meshHeight are " + meshWidth + ", " + meshHeight);
        for (int i = 0; i < meshWidth; i++)
        {
            for (int j = 0; j < meshHeight; j++)
            {
                if (meshData[i, j].tileState != TileState.Empty)
                {
                    // make a note of the startpoint for the next square's triangles
                    int startPoint = newVerts.Count;

                    // Draw a 'square'
                    newVerts.Add(new Vector3(vertices[0].x + offset.x + i, vertices[0].y + offset.y + j, 0f));
                    newVerts.Add(new Vector3(vertices[1].x + offset.x + i, vertices[1].y + offset.y + j, 0f));
                    newVerts.Add(new Vector3(vertices[2].x + offset.x + i, vertices[2].y + offset.y + j, 0f));
                    newVerts.Add(new Vector3(vertices[3].x + offset.x + i, vertices[3].y + offset.y + j, 0f));

                    // UV's for the new 'square'
                    newUVs.Add(new Vector2(meshData[i, j].TextureOffset, meshData[i, j].HealthTextureOffset + 0.25f)); // TL UV
                    newUVs.Add(new Vector2(meshData[i, j].TextureOffset + 0.25f, meshData[i, j].HealthTextureOffset + 0.25f)); // TR UV
                    newUVs.Add(new Vector2(meshData[i, j].TextureOffset, meshData[i, j].HealthTextureOffset)); // BL UV
                    newUVs.Add(new Vector2(meshData[i, j].TextureOffset + 0.25f, meshData[i, j].HealthTextureOffset)); // BR UV
                    
                    // First Triangle
                    newTriangles.Add(startPoint);       // TL
                    newTriangles.Add(startPoint + 1);   // TR
                    newTriangles.Add(startPoint + 2);   // BL

                    // Second Triangle
                    newTriangles.Add(startPoint + 2);   // BL
                    newTriangles.Add(startPoint + 1);   // TR
                    newTriangles.Add(startPoint + 3);   // BR

                }
            }
        }
        // Assign the terrain Texture to the meshrenderer
        meshRenderer.material = terrainTexture;

        //Convert the completed lists into arrays and insert into the mesh object
        mesh.vertices = newVerts.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.uv = newUVs.ToArray();

        //Do some housekeeping
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();


        // Change to random colours
        //Color[] colours = new Color[mesh.vertices.Length];
        //
        //for (int i = 0; i < colours.Length; i++)
        //{
        //
        //    colours[i] = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        //}
        //
        //mesh.colors = colours;

        meshFilter.mesh = mesh;
    }

    private void BuildCollider()
    {
        //Debug.Log("We're building the collider people!");
        // First turn off the collider for efficiency
        polygonCollider.enabled = false;

        // Clear out any current paths
        polygonCollider.pathCount = 0;
        newPath.Clear();
        newPath.TrimExcess();

        TileMarker startTile = new TileMarker();
        TileMarker currentTile = new TileMarker();

        dir = Direction.Left;

        startTile = FindStartTile(new TileMarker(0,0));

        //  If we can't find an initial start time, fail!
        if (startTile.X == -10 && startTile.Y == -10)
            return;

        // Set the currentTile to the startTile
        //currentTile = startTile;

        // Now make the mesh for the current Island, and keep doing so until all Islands are completed.
        while (startTile.X != -10 && startTile.Y != -10)
        {
            // First, draw the left side of the starting cube so that we are away from the end check (which checks starting position)
            AddPathNode(new Vector2(vertices[2].x + offset.x + startTile.X, vertices[2].y + offset.y + startTile.Y));
            AddPathNode(new Vector2(vertices[0].x + offset.x + startTile.X, vertices[0].y + offset.y + startTile.Y));

            // Mark tile as completed - we don't want to start here for another collider path
            meshData[startTile.X, startTile.Y].tileState = TileState.Completed;

            // We always start on Up
            dir = Direction.Up;
            prevDir = dir; // Default setting

            // Now set a direction from the starting tile
            dir = FindNextDirection(startTile, dir);

            // Find the next tile form the startTile and set it into the currentTile
            currentTile = FindNextTile(startTile, dir, prevDir);

            // Make the mesh for this Island
            CreateIslandPath(startTile, currentTile, dir, prevDir);

            // Get the number of slots already filled, this will be the zero starting entry point for the newPath
            int slot = polygonCollider.pathCount;

            // Increase the pathCount so that there is room for the newPath
            polygonCollider.pathCount++;

            // Insert the newPath
            polygonCollider.SetPath(slot, newPath.ToArray());

            // Now perform houseKeeping on the newPath ready for the next island or rebuild of the collider
            newPath.Clear();
            newPath.TrimExcess();

            // Now find a new startTile if we can.  If we can't it's new value will be out of range and will fail the loop check
            startTile = FindStartTile(startTile);
            //currentTile = startTile;
        }
        
        // Now turn the collider back on so that it actually works
        //Debug.Log("We're enabling to collider, people!");
        polygonCollider.enabled = true;
    }

    private void CreateIslandPath(TileMarker startTile, TileMarker currentTile, Direction dir, Direction prevDir)
    {
        // Write the points to the path and then keep getting new directions and tiles until its all completed
        //while (!(currentTile.X == startTile.X && currentTile.Y == startTile.Y && dir == Direction.Left))
        // EXPERIMENTAL STOP STATEMENT
        bool looping = true;
        while(looping)
        {
            if (newPath[newPath.Count - 1].x == vertices[2].x + offset.x + startTile.X && newPath[newPath.Count - 1].y == vertices[2].y + offset.y + startTile.Y)
            {
                looping = false;
            }


            if (dir == Direction.Up)
            {
                AddPathNode(new Vector2(vertices[0].x + offset.x + currentTile.X, vertices[0].y + offset.y + currentTile.Y));
            }
            else if (dir == Direction.Right)
            {
                AddPathNode(new Vector2(vertices[1].x + offset.x + currentTile.X, vertices[1].y + offset.y + currentTile.Y));
            }
            else if (dir == Direction.Down)
            {
                AddPathNode(new Vector2(vertices[3].x + offset.x + currentTile.X, vertices[3].y + offset.y + currentTile.Y));
            }
            else
            {
                AddPathNode(new Vector2(vertices[2].x + offset.x + currentTile.X, vertices[2].y + offset.y + currentTile.Y));
            }                                                                                                              

            meshData[currentTile.X, currentTile.Y].tileState = TileState.Completed;

            prevDir = dir;
            dir = FindNextDirection(currentTile, dir);
            currentTile = FindNextTile(currentTile, dir, prevDir);
        }
    }

    private TileMarker FindNextTile(TileMarker currentTile, Direction dir, Direction prevDir)
    {
        if (dir == Direction.Left)
        {
            if (prevDir == Direction.Left)
            {
                return new TileMarker(currentTile.X - 1, currentTile.Y);
            }
            else if (prevDir == Direction.Up)
            {
                return new TileMarker(currentTile.X - 1, currentTile.Y + 1);
            }
            else if (prevDir == Direction.Down)
            {
                return currentTile;
            }
        }
        else if (dir == Direction.Up)
        {
            if (prevDir == Direction.Up)
            {
                return new TileMarker(currentTile.X, currentTile.Y + 1);
            }
            else if (prevDir == Direction.Right)
            {
                return new TileMarker(currentTile.X + 1, currentTile.Y + 1);
            }
            else if (prevDir == Direction.Left)
            {
                return currentTile;
            }
        }
        else if (dir == Direction.Right)
        {
            if(prevDir == Direction.Right)
            {
                return new TileMarker(currentTile.X + 1, currentTile.Y);
            }
            else if(prevDir == Direction.Down)
            {
                return new TileMarker(currentTile.X + 1, currentTile.Y - 1);
            }
            else if(prevDir == Direction.Up)
            {
                return currentTile;
            }
        }
        // We are going down
        else
        {
            if(prevDir == Direction.Down)
            {
                return new TileMarker(currentTile.X, currentTile.Y - 1);
            }
            else if (prevDir == Direction.Left)
            {
                return new TileMarker(currentTile.X - 1, currentTile.Y - 1);
            }
            else if (prevDir == Direction.Right)
            {
                //Debug.LogWarning("We are at point " + newPath.Count + " and we are going down after going right");
                return currentTile;
            }
        }
        //Debug.LogWarning("PROBLEM! WE DIDNT FIND A SOLUTION FOR NEXTTILE");
        return currentTile;
    }

    private Direction FindNextDirection(TileMarker currentTile, Direction dir)
    {
        if (dir == Direction.Up)
        {
            if (currentTile.X > 0 && currentTile.Y < meshHeight - 1 && meshData[currentTile.X - 1, currentTile.Y + 1].tileState != TileState.Empty)
            {
                return Direction.Left;
            }
            else if (currentTile.Y < meshHeight - 1 && meshData[currentTile.X, currentTile.Y + 1].tileState != TileState.Empty)
            {
                return Direction.Up;
            }
            else
            {
                return Direction.Right;
            }
        }
        else if (dir == Direction.Right)
        {
            if (currentTile.X < meshWidth - 1 && currentTile.Y < meshHeight - 1 && meshData[currentTile.X + 1, currentTile.Y + 1].tileState != TileState.Empty)
            {
                return Direction.Up;
            }
            else if (currentTile.X < meshWidth - 1 && meshData[currentTile.X + 1, currentTile.Y].tileState != TileState.Empty)
            {
                return Direction.Right;
            }
            else
            {
                return Direction.Down;
            }
        }
        else if (dir == Direction.Down)
        {
            if (currentTile.X < meshWidth - 1 && currentTile.Y > 0 && meshData[currentTile.X + 1, currentTile.Y - 1].tileState != TileState.Empty)
            {
                return Direction.Right;
            }
            else if (currentTile.Y > 0 && meshData[currentTile.X, currentTile.Y - 1].tileState != TileState.Empty)
            {
                return Direction.Down;
            }
            else
            {
                return Direction.Left;
            }
        }
        else
        // Going left
        {
            if (currentTile.X > 0 && currentTile.Y > 0 && meshData[currentTile.X - 1, currentTile.Y - 1].tileState != TileState.Empty)
            {
                return Direction.Down;
            }
            else if (currentTile.X > 0 && meshData[currentTile.X - 1, currentTile.Y].tileState != TileState.Empty)
            {
                return Direction.Left;
            }
            else
            {
                return Direction.Up;
            }
        }
    }

    private TileMarker FindStartTile(TileMarker previousStartTile)
    {
        TileState tileState = new TileState();
        // First find a starting point
        for (int j = previousStartTile.Y; j < meshHeight; j++)
        {
            for (int i = previousStartTile.X; i < meshWidth; i++)
            {
                {
                    tileState = meshData[i, j].tileState;
                    if (tileState == TileState.Filled)
                    {
                        if (i > 0)
                        {
                            if (meshData[i - 1, j].tileState == TileState.Empty)
                                return new TileMarker(i, j);
                        }
                        else
                        {
                            return new TileMarker(i, j);
                        }
                    }
                }
            }
        }
        return new TileMarker(-10, -10);
    }

    private void AddPathNode(Vector2 nextNode)
    {
        newPath.Add(new Vector2(nextNode.x,nextNode.y));
    }

    private void FlagAsIgnore()
    {
        for (int i = 0; i < meshWidth; i++)
        {
            for (int j = 0; j < meshHeight; j++)
            {
                // First, reset all values to filled
                if (meshData[i, j].tileState != TileState.Empty)
                {
                    meshData[i, j].tileState = TileState.Filled;
                }
                else
                {
                    // It's already empty, so we can continue
                    continue;
                }

                // Check below, then on clockwise
                if (j == 0 || meshData[i, j - 1].tileState != TileState.Empty)
                {
                        // Check left
                    if (i == 0 || meshData[i - 1, j].tileState != TileState.Empty)
                    {
                            // Check above
                        if (j == meshHeight - 1 || meshData[i, j + 1].tileState != TileState.Empty)
                        {
                            // Check right
                            if (i == meshWidth - 1 || meshData[i + 1, j].tileState != TileState.Empty)
                            {
                                // Mark it be ignored, but 'filled'
                                meshData[i, j].tileState = TileState.Ignore;
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    private void AdjustTerrain(Vector2 pointOfImpact, Vector2 normal, float damage)
    {
        // Move the 'point of impact' a little further inside
        pointOfImpact += normal.normalized * 0.1f;
        // Convert to local coords
        pointOfImpact = (Vector2)transform.InverseTransformPoint(new Vector3(pointOfImpact.x, pointOfImpact.y, 0f));
        // Account for the offset
        pointOfImpact -= (Vector2)offset;

        // Now convert to ints for the meshData
        int x = Mathf.RoundToInt(pointOfImpact.x);
        int y = Mathf.RoundToInt(pointOfImpact.y);

        // Now we have the nearest square, let see if we rounded correctly
        if (meshData[x, y].tileState != TileState.Empty)
        {
            // As we did, adjust the database and rebuild the mesh and collider

            ///////////////////////////////////
            //
            //  Starting health is 100 - this
            //  needs to be cleaned up!
            //
            ///////////////////////////////////
            //float maxHealth = 100;
            float previousTextureOffset = meshData[x, y].HealthTextureOffset;
            meshData[x, y].Health -= damage;  // TODO work out if this is the best way, converting floats to int
            float health = meshData[x, y].Health;

            //Debug.Log("Health of tile " + x + ", " + y + " is " + health);

            if (health >= 75)
            {
                // No need to adjust
                //meshData[x, y].HealthTextureOffset = 0.75f;

                //BuildMesh();
            }
            else if (health < 75 && health >= 50)
            {
                meshData[x, y].HealthTextureOffset = 0.5f;

                if (previousTextureOffset != meshData[x,y].TextureOffset)
                    BuildMesh();
            }
            else if (health < 50 && health >= 25)
            {
                meshData[x, y].HealthTextureOffset = 0.25f;

                if (previousTextureOffset != meshData[x, y].TextureOffset)
                    BuildMesh();
            }
            else if (health >=1 && health < 25)
            {
                meshData[x, y].HealthTextureOffset = 0f;

                if (previousTextureOffset != meshData[x, y].TextureOffset)
                    BuildMesh();
            }
            else
            {
               meshData[x, y].tileState = TileState.Empty;

               FlagAsIgnore();

               BuildMesh();

               BuildCollider();
            }
        }
        else
        {
            //Debug.LogError("We registered a hit at an empty square with " + (int)pointOfImpact.x + ", " + (int)pointOfImpact.y);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Bullet"))
        {
            // Okay, so a bullet hit us, remove the block and rebuild the mesh and collider
            //bulletDebug.BulletPosition = collision.transform.position.ToString();

            // first get the amount of Damage that the bullet does
            float damage = 10f;//collision.transform.GetComponent<bulletScript>().damageAmount;



            contactPoints = collision.GetContacts(contacts);

            //Destroy(collision.gameObject);

            for (int i = 0; i < contactPoints; i++)
            {
                if(contacts[i].collider != null)
                {
                    //bulletDebug.Normal = contacts[i].normal.ToString();
                    AdjustTerrain(contacts[i].point, contacts[i].normal, damage);
                }
            }
        }
    }
}
