using System;
using System.Threading.Tasks;
using MarchingCubes.Examples.DensityFunctions;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MarchingCubes.Examples
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class Chunk : MonoBehaviour
    {
        private Vector3Int _coordinate;
        private bool _isDirty;
        private float _isolevel;
        private int _chunkSize;

        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private ValueGrid<float> _densityField;

        private Func<MeshData> _meshDataDelegate;
        private World _world;

        private NativeArray<float> densities;
        private JobHandle densityJobHandle;
        private DensityCalculationJob densityCalculationJob;
        private bool densitiesChanged;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _mesh = new Mesh();
        }

        private void OnDestroy()
        {
            if(!densityJobHandle.IsCompleted)
                densityJobHandle.Complete();

            densities.Dispose();
        }

        private void OnDisable() {
            if(!densityJobHandle.IsCompleted)
                densityJobHandle.Complete();
        }

        private void Update()
        {
            if (!_isDirty) { return; }
            Generate();
        }

        public void Initialize(World world, int chunkSize, float isolevel, Vector3Int coordinate)
        {
            _world = world;
            _isolevel = isolevel;
            _chunkSize = chunkSize;

            _densityField = new ValueGrid<float>(chunkSize + 1, chunkSize + 1, chunkSize + 1);
            densities = new NativeArray<float>((_chunkSize + 1) * (_chunkSize + 1) * (_chunkSize + 1), Allocator.Persistent);

            _meshDataDelegate = () => MarchingCubes.CreateMeshData(_densityField, isolevel, _world.VoxelScale);

            SetCoordinates(coordinate);
        }

        public void SetCoordinates(Vector3Int coordinate)
        {
            _coordinate = coordinate;
            name = $"Chunk_{coordinate.x.ToString()}_{coordinate.y.ToString()}_{coordinate.z.ToString()}";

            PopulateDensities();

            _isDirty = true;
        }

        private void PopulateDensities()
        {
            Vector3Int offset = _coordinate * _chunkSize;
            
            if (_world.UseThreading)
            {
                densityCalculationJob = new DensityCalculationJob
                {
                    densities = densities,
                    offsetX = offset.x,
                    offsetY = offset.y,
                    offsetZ = offset.z,
                    chunkSize = _chunkSize + 1, // +1 because chunkSize is the amount of "voxels", and that +1 is the amount of density points
                };

                if(!densityJobHandle.IsCompleted)
                    densityJobHandle.Complete();

                densityJobHandle = densityCalculationJob.Schedule(densities.Length, 256);

                densitiesChanged = true;
            }
            else
            {
                _densityField.Populate(_world.DensityFunction.CalculateDensity, offset.x, offset.y, offset.z);
            }
        }

        public void Generate()
        {
            MeshData meshData;

            if (_world.UseThreading)
            {
                if (densitiesChanged)
                {
                    densityJobHandle.Complete();
                    densities.CopyTo(_densityField.data);
                    densitiesChanged = false;
                }

                Task<MeshData> meshTask = Task.Factory.StartNew(_meshDataDelegate);

                meshTask.Wait();

                meshData = meshTask.Result;
            }
            else
            {
                meshData = MarchingCubes.CreateMeshData(_densityField, _isolevel, _world.VoxelScale);
            }

            var (vertices, triangles) = meshData;

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateNormals();

            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;

            _isDirty = false;
        }

        public float GetDensity(int x, int y, int z)
        {
            return _densityField[x, y, z];
        }

        public void SetDensity(float density, int x, int y, int z)
        {
            _densityField[x, y, z] = density;
            _isDirty = true;
        }
    }
}