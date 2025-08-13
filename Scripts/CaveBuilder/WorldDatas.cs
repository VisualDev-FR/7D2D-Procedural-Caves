using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class WorldDatas
{
    public readonly PathAbstractions.AbstractedLocation location;

    public readonly GameUtils.WorldInfo worldInfo;

    public readonly List<PrefabDataInstance> prefabs;

    public readonly RawHeightMap heightMap;

    public readonly string name;

    public readonly int size;

    public readonly int seed;

    public readonly bool[] roadMap;

    public readonly bool[] waterMap;

    public string dtmPath => Path.Combine(location.FullPath, "dtm.raw");

    public string prefabsPath => Path.Combine(location.FullPath, "prefabs.xml");

    public string splat3Path => Path.Combine(location.FullPath, "splat3.png");

    public string splat4Path => Path.Combine(location.FullPath, "splat4.png");

    public WorldDatas(string worldName)
    {
        this.name = worldName;
        this.location = PathAbstractions.WorldsSearchPaths.GetLocation(worldName);
        this.worldInfo = GameUtils.WorldInfo.LoadWorldInfo(location);
        this.size = worldInfo.WorldSize.x;
        this.seed = GetWorldSeed();
        this.roadMap = ReadTexture(splat3Path);
        this.waterMap = ReadTexture(splat4Path);
        this.prefabs = PrefabLoader.LoadPrefabs(prefabsPath).ToList();
        this.heightMap = new RawHeightMap(dtmPath, size);
    }

    private bool[] ReadTexture(string path)
    {
        var result = new bool[size * size];
        var texture = TextureUtils.LoadTexture(path);
        var pixelCount = 0;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                var pixel = texture.GetPixel(x, y);
                var colorSum = pixel.r + pixel.g + pixel.b;

                result[x + y * size] = colorSum > 0;

                if (colorSum > 0) pixelCount++;
            }
        }

        Logging.Debug($"{pixelCount} pixels for '{path}'");

        return result;
    }

    private int GetWorldSeed()
    {
        if (!worldInfo.DynamicProperties.Contains("Seed"))
        {
            return 0;
        }

        return worldInfo.DynamicProperties.GetInt("Seed");
    }

    public void Debug()
    {
        Logging.Debug($"name: {name}");
        Logging.Debug($"size: {size}");
        Logging.Debug($"seed: {seed}");
    }

    public List<StreetTileData> GetStreetTiles()
    {
        var streetTiles = InitStreetTiles().ToList();
        var StreetTileMapSize = size / 150;

        foreach (var st in streetTiles)
        {
            for (int dx = 0; dx < 150; dx++)
            {
                for (int dy = 0; dy < 150; dy++)
                {
                    int x = st.worldPosition.x + dx;
                    int y = st.worldPosition.y + dy;

                    if (roadMap[x + y * size])
                    {
                        st.ContainsRoad = true;
                    }
                }
            }
        }



        return streetTiles;
    }

    public IEnumerable<StreetTileData> InitStreetTiles()
    {
        var StreetTileMapSize = size / 150;

        for (int x = 0; x < StreetTileMapSize; x++)
        {
            for (int y = 0; y < StreetTileMapSize; y++)
            {
                yield return new StreetTileData(new Vector2i(x, y), StreetTileMapSize);
            }
        }
    }

    public float GetHeight(int x, int z)
    {
        return heightMap.GetHeight(x, z);
    }

    public int GetHeightCeil(int x, int z)
    {
        return Mathf.CeilToInt(GetHeight(x, z));
    }

    public bool IsWater(int x, int z)
    {
        return waterMap[x + z * size];
    }
}

public class StreetTileData
{
    public Vector2i gridPosition;

    public Vector2i worldPosition;

    public Vector2i worldPositionCenter => worldPosition + Vector2i.one * 75;

    public bool overlapsRadiation = false;

    public bool HasPrefabs = false;

    public bool ContainsRoad = false;

    public StreetTileData(Vector2i position, int streetTileMapSize)
    {
        this.gridPosition = position;
        this.worldPosition = gridPosition * 150;
        this.overlapsRadiation = position.x < 1 || position.x >= streetTileMapSize - 1 || position.y < 1 || position.y >= streetTileMapSize - 1;
    }
}