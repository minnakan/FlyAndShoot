using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    const float scale = 50f;

    const float viewThreshHoldForChunkUpdate = 25f;
    const float sqrviewThreshHoldForChunkUpdate = viewThreshHoldForChunkUpdate* viewThreshHoldForChunkUpdate;

    public LODInfo[] lODInfo;

    public static float maxViewDist;

    public Transform viewer;

    public Material mapMaterial;

    static MapGenerator mapGenerator;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunkVisibleViewDist;

    Dictionary<Vector2,TerrainChunk> terrainChunksDict = new Dictionary<Vector2, TerrainChunk>();

    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = lODInfo[lODInfo.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleViewDist = Mathf.RoundToInt(maxViewDist/chunkSize);

        UpdateVisibleChunks();

    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/scale;
        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrviewThreshHoldForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
        
    }

    private void UpdateVisibleChunks()
    {

        for(int i = 0;i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleViewDist; yOffset <= chunkVisibleViewDist; yOffset++)
        {
            for (int xOffset = -chunkVisibleViewDist; xOffset <= chunkVisibleViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX+xOffset,currentChunkCoordY+yOffset);

                if (terrainChunksDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunksDict[viewedChunkCoord].UpdateTerrainChunk();    
                }
                else
                {
                    terrainChunksDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize,lODInfo, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MapData mapData;
        bool mapDataReceived;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] lodInfo;
        LODMesh[] lodMeshes;

        int previousLODIndex = -1;


        public TerrainChunk(Vector2 coord, int size,LODInfo[] lods, Transform parent, Material material)
        {
            this.lodInfo = lods;
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position,Vector2.one *size);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one*scale;
            SetVisible(false);

            lodMeshes = new LODMesh[lodInfo.Length];

            for(int i = 0; i < lodInfo.Length; i++)
            {
                lodMeshes[i] = new LODMesh(lodInfo[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position,OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }


        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < lodInfo.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > lodInfo[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }


                    terrainChunksVisibleLastUpdate.Add(this);


                }
                SetVisible(visible);
            }
            
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        
        int lod;

        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshdata)
        {
            mesh = meshdata.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapdata)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapdata,lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
