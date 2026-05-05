using UnityEngine;

public class ProceduralMapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public float mapWidth = 50f;
    public float mapLength = 50f;
    public float wallHeight = 4f;

    [Header("Obstacles")]
    public int obstacleCount = 20;
    public int seed = 42;

    [Header("Materials (optional)")]
    public Material groundMaterial;
    public Material wallMaterial;
    public Material obstacleMaterial;

    private const int MAX_PLAYERS = 7;
    private Transform mapRoot;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (mapRoot != null)
            Destroy(mapRoot.gameObject);

        mapRoot = new GameObject("ProceduralMap").transform;
        Random.InitState(seed);

        CreateGround();
        CreateBoundaryWalls();
        CreateObstacles();
        CreateSpawnPoints();

        Debug.Log($"[MapGen] Generated {mapWidth}x{mapLength} map, {obstacleCount} obstacles (seed={seed})");
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(mapRoot);
        ground.transform.localScale = new Vector3(mapWidth / 10f, 1f, mapLength / 10f);
        ground.transform.position = Vector3.zero;

        if (groundMaterial != null)
            ground.GetComponent<MeshRenderer>().material = groundMaterial;
    }

    void CreateBoundaryWalls()
    {
        float hw = mapWidth / 2f;
        float hl = mapLength / 2f;
        float halfH = wallHeight / 2f;

        Vector3[] positions = {
            new Vector3(0, halfH, hl),   // North
            new Vector3(0, halfH, -hl),  // South
            new Vector3(hw, halfH, 0),   // East
            new Vector3(-hw, halfH, 0),  // West
        };
        Vector3[] scales = {
            new Vector3(mapWidth, wallHeight, 0.5f),
            new Vector3(mapWidth, wallHeight, 0.5f),
            new Vector3(0.5f, wallHeight, mapLength),
            new Vector3(0.5f, wallHeight, mapLength),
        };
        string[] names = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = names[i];
            wall.transform.SetParent(mapRoot);
            wall.transform.position = positions[i];
            wall.transform.localScale = scales[i];

            if (wallMaterial != null)
                wall.GetComponent<MeshRenderer>().material = wallMaterial;
        }
    }

    void CreateObstacles()
    {
        float margin = 3f;
        float hw = mapWidth / 2f - margin;
        float hl = mapLength / 2f - margin;

        for (int i = 0; i < obstacleCount; i++)
        {
            PrimitiveType shape = (i % 3 == 0) ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject obs = GameObject.CreatePrimitive(shape);
            obs.name = $"Obstacle_{i}";
            obs.transform.SetParent(mapRoot);

            float x = Random.Range(-hw, hw);
            float z = Random.Range(-hl, hl);
            float sx = Random.Range(0.5f, 2.5f);
            float sy = Random.Range(1.0f, 3.0f);
            float sz = Random.Range(0.5f, 2.5f);

            obs.transform.position = new Vector3(x, sy / 2f, z);
            obs.transform.localScale = new Vector3(sx, sy, sz);

            if (obstacleMaterial != null)
                obs.GetComponent<MeshRenderer>().material = obstacleMaterial;
        }
    }

    void CreateSpawnPoints()
    {
        float hw = mapWidth / 2f - 2f;
        float hl = mapLength / 2f - 2f;

        GameObject seekerSpawn = new GameObject("Spawn_Seeker");
        seekerSpawn.transform.SetParent(mapRoot);
        seekerSpawn.transform.position = new Vector3(-hw, 1f, 0);

        for (int i = 0; i < MAX_PLAYERS - 1; i++)
        {
            GameObject hiderSpawn = new GameObject($"Spawn_Hider_{i}");
            hiderSpawn.transform.SetParent(mapRoot);
            float angle = (360f / (MAX_PLAYERS - 1)) * i;
            float rad = angle * Mathf.Deg2Rad;
            hiderSpawn.transform.position = new Vector3(
                Mathf.Cos(rad) * (hw * 0.6f), 1f, Mathf.Sin(rad) * (hl * 0.6f));
        }
    }
}
