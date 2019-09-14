using System.Collections.Generic;
using MarchingCubes.Examples.DensityFunctions;
using UnityEngine;

namespace MarchingCubes.Examples
{
    public class World : MonoBehaviour
    {
        [Header("Chunk settings")]
        [SerializeField] private int chunkSize = 8;
        [SerializeField] private GameObject chunkPrefab;

        [Header("Marching Cubes settings")]
        [SerializeField] private float isolevel = 0.5f;
        [SerializeField] private DensityFunction densityFunction;

        [Header("Player settings")]
        [SerializeField] private int renderDistance = 4;
        [SerializeField] private Transform player;

        [Header("Other settings")]
        [SerializeField] private bool useThreading = true;
        [SerializeField] private float voxelScale = 1;

        private Dictionary<Vector3Int, Chunk> _chunks;
        private Vector3 _startCoordinate;
        private Queue<Chunk> _availableChunks;

        public DensityFunction DensityFunction { get; private set; }
        public bool UseThreading { get => useThreading; private set => useThreading = value; }
        public float VoxelScale { get => voxelScale; private set => voxelScale = value; }

        private void Awake()
        {            
            DensityFunction = densityFunction;
            _availableChunks = new Queue<Chunk>();
            _chunks = new Dictionary<Vector3Int, Chunk>();
            if(densityFunction is InitializedDensityFunction initializable)
            {
                initializable.Initialize();
            }
        }
        
        private void Start()
        {
            Vector3Int playerCoordinate = CoordinateFromWorldPosition(player.position);
            GenerateNewTerrain(playerCoordinate);
        }

        private void Update()
        {
            Vector3Int playerCoordinate = CoordinateFromWorldPosition(player.position);

            if (Mathf.Abs(playerCoordinate.x - _startCoordinate.x) >= 1 ||
                Mathf.Abs(playerCoordinate.y - _startCoordinate.y) >= 1 ||
                Mathf.Abs(playerCoordinate.z - _startCoordinate.z) >= 1)
            {
                GenerateNewTerrain(playerCoordinate);
            }
        }

        private void GenerateNewTerrain(Vector3Int playerCoordinate)
        {
            // TODO: Initialize this only once
            var newTerrain = new Dictionary<Vector3Int, Chunk>((int)Mathf.Pow(renderDistance * 2 + 1, 3));

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector3Int chunkCoordinate = playerCoordinate + new Vector3Int(x, y, z);
                        bool chunkExists = _chunks.TryGetValue(chunkCoordinate, out Chunk chunk);
                        
                        if (!chunkExists)
                        {
                            if (_availableChunks.Count > 0)
                            {
                                chunk = _availableChunks.Dequeue();
                                chunk.gameObject.SetActive(true);
                                chunk.transform.position = (Vector3)chunkCoordinate * chunkSize * voxelScale;
                                chunk.SetCoordinates(chunkCoordinate);
                            }
                            else
                            {
                                chunk = CreateChunk(chunkCoordinate);
                            }
                        }

                        newTerrain.Add(chunkCoordinate, chunk);
                    }
                }
            }

            foreach (var key in _chunks.Keys)
            {
                if (newTerrain.ContainsKey(key)) { continue; }
                
                Chunk chunk = _chunks[key];
                if (_availableChunks.Contains(chunk)) { continue; }
                
                _availableChunks.Enqueue(chunk);
                chunk.gameObject.SetActive(false);
            }

            _chunks = newTerrain;

            _startCoordinate = playerCoordinate;
        }

        private Vector3Int CoordinateFromWorldPosition(Vector3 worldPosition)
        {
            var coordinate = worldPosition.FloorToNearestX(chunkSize * voxelScale) / (chunkSize * voxelScale);

            int coordX = Mathf.FloorToInt(coordinate.x);
            int coordY = Mathf.FloorToInt(coordinate.y);
            int coordZ = Mathf.FloorToInt(coordinate.z);

            return new Vector3Int(coordX, coordY, coordZ);
        }

        private Chunk GetChunk(int x, int y, int z)
        {
            int newX = Utils.FloorToNearestX(x, chunkSize);
            int newY = Utils.FloorToNearestX(y, chunkSize);
            int newZ = Utils.FloorToNearestX(z, chunkSize);

            Vector3Int key = new Vector3Int(newX, newY, newZ);
            return _chunks[key];
        }

        public float GetDensity(int x, int y, int z)
        {
            Chunk chunk = GetChunk(x, y, z);
            if (chunk == null)
            {
                return 0;
            }

            float density = chunk.GetDensity(x.Mod(chunkSize),
                                             y.Mod(chunkSize),
                                             z.Mod(chunkSize));
            return density;
        }

        public void SetDensity(float density, Vector3Int pos)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3Int chunkPos = (pos - LookupTables.CubeCorners[i]).FloorToNearestX(chunkSize);
                Chunk chunk = GetChunk(chunkPos.x, chunkPos.y, chunkPos.z);
                Vector3Int localPos = (pos - chunkPos).Mod(chunkSize + 1);

                chunk.SetDensity(density, localPos.x, localPos.y, localPos.z);
            }
        }

        private Chunk CreateChunk(Vector3Int chunkCoordinate)
        {
            var chunk = Instantiate(chunkPrefab, (Vector3)chunkCoordinate * chunkSize * voxelScale, Quaternion.identity, transform).GetComponent<Chunk>();

            chunk.Initialize(this, chunkSize, isolevel, chunkCoordinate);
            _chunks.Add(chunkCoordinate, chunk);

            return chunk;
        }
    }
}