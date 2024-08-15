// using HarmonyLib;
// using WorldGenerationEngineFinal;

// [HarmonyPatch(typeof(StreetTile), "SpawnPrefabs")]
// public static class StreetTile_SpawnPrefabs
// {
//     public static bool SpawnWildernessCaveEntrances(StreetTile tile)
//     {
//         GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldBuilder.Instance.Seed + 4096953);

//         FastTags<TagGroup.Poi> withoutTags = (WorldBuilder.Instance.Towns == WorldBuilder.GenerationSelections.None) ? FastTags<TagGroup.Poi>.none : traderTag;
//         FastTags<TagGroup.Poi> none = FastTags<TagGroup.Poi>.none;

//         Vector2i worldPositionCenter = WorldPositionCenter;

//         PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(withoutTags, none, default, default, worldPositionCenter);
//         int maxTries = 0;

//         while (++maxTries < 6)
//         {
//             int rotation = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 4)) & 3;
//             int sizeX = wildernessPrefab.size.x;
//             int sizeZ = wildernessPrefab.size.z;

//             if (rotation == 1 || rotation == 3)
//             {
//                 sizeX = wildernessPrefab.size.z;
//                 sizeZ = wildernessPrefab.size.x;
//             }
//             Vector2i vector2i;
//             if (sizeX > 150 || sizeZ > 150)
//             {
//                 vector2i = WorldPositionCenter - new Vector2i((sizeX - 150) / 2, (sizeZ - 150) / 2);
//             }
//             else
//             {
//                 try
//                 {
//                     vector2i = new Vector2i(gameRandom.RandomRange(WorldPosition.x + 10, WorldPosition.x + 150 - sizeX - 10), gameRandom.RandomRange(WorldPosition.y + 10, WorldPosition.y + 150 - sizeZ - 10));
//                 }
//                 catch
//                 {
//                     vector2i = WorldPositionCenter - new Vector2i(sizeX / 2, sizeZ / 2);
//                 }
//             }
//             int maxSize = (sizeX > sizeZ) ? sizeX : sizeZ;

//             Rect rect = new Rect(vector2i.x, vector2i.y, maxSize, maxSize);

//             new Rect(rect.min - new Vector2(maxSize, maxSize) / 2f, rect.size + new Vector2(maxSize, maxSize));

//             Rect rect2 = new Rect(rect.min - new Vector2(maxSize, maxSize) / 2f, rect.size + new Vector2(maxSize, maxSize))
//             {
//                 center = new Vector2(vector2i.x + sizeZ / 2, vector2i.y + sizeX / 2)
//             };

//             if (rect2.max.x >= WorldBuilder.Instance.WorldSize || rect2.min.x < 0f || rect2.max.y >= WorldBuilder.Instance.WorldSize || rect2.min.y < 0f)
//             {
//                 continue;
//             }

//             BiomeType biome = WorldBuilder.Instance.GetBiome((int)rect.center.x, (int)rect.center.y);

//             int medianHeight = Mathf.CeilToInt(WorldBuilder.Instance.GetHeight((int)rect.center.x, (int)rect.center.y));
//             int positionX = vector2i.x;
//             var list = new List<int>();

//             while (true)
//             {
//                 if (positionX < vector2i.x + sizeX)
//                 {
//                     for (int positionZ = vector2i.y; positionZ < vector2i.y + sizeZ; positionZ++)
//                     {
//                         if (positionX >= WorldBuilder.Instance.WorldSize || positionX < 0 || positionZ >= WorldBuilder.Instance.WorldSize || positionZ < 0 || WorldBuilder.Instance.GetWater(positionX, positionZ) > 0 || biome != WorldBuilder.Instance.GetBiome(positionX, positionZ) || Mathf.Abs(Mathf.CeilToInt(WorldBuilder.Instance.GetHeight(positionX, positionZ)) - medianHeight) > 11)
//                         {
//                             goto end_IL_03d4;
//                         }
//                         list.Add((int)WorldBuilder.Instance.GetHeight(positionX, positionZ));
//                     }
//                     positionX++;
//                     continue;
//                 }

//                 medianHeight = getMedianHeight(list);
//                 if (medianHeight + wildernessPrefab.yOffset < 2)
//                 {
//                     break;
//                 }

//                 var vector3i = new Vector3i(subHalfWorld(vector2i.x), getHeightCeil(rect.center) + wildernessPrefab.yOffset + 1, subHalfWorld(vector2i.y));
//                 var vector3i2 = new Vector3i(subHalfWorld(vector2i.x), getHeightCeil(rect.center), subHalfWorld(vector2i.y));
//                 gameRandom.SetSeed(vector2i.x + vector2i.x * vector2i.y + vector2i.y);

//                 rotation = gameRandom.RandomRange(0, 4);
//                 rotation = (rotation + wildernessPrefab.RotationsToNorth) & 3;

//                 Vector2 vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y + sizeZ / 2);

//                 int prefabId = PrefabManager.PrefabInstanceId++;

//                 switch (rotation)
//                 {
//                     case 0:
//                         vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y);
//                         break;
//                     case 1:
//                         vector = new Vector2(vector2i.x + sizeX, vector2i.y + sizeZ / 2);
//                         break;
//                     case 2:
//                         vector = new Vector2(vector2i.x + sizeX / 2, vector2i.y + sizeZ);
//                         break;
//                     case 3:
//                         vector = new Vector2(vector2i.x, vector2i.y + sizeZ / 2);
//                         break;
//                 }
//                 float maxSizeZX = 0f;
//                 if (wildernessPrefab.POIMarkers != null)
//                 {
//                     List<Prefab.Marker> markers = wildernessPrefab.RotatePOIMarkers(_bLeft: true, rotation);
//                     for (int i = markers.Count - 1; i >= 0; i--)
//                     {
//                         if (markers[i].MarkerType != Prefab.Marker.MarkerTypes.RoadExit)
//                         {
//                             markers.RemoveAt(i);
//                         }
//                     }
//                     if (markers.Count > 0)
//                     {
//                         int index = gameRandom.RandomRange(0, markers.Count);
//                         Vector3i start = markers[index].Start;
//                         int sizeZX = ((markers[index].Size.x > markers[index].Size.z) ? markers[index].Size.x : markers[index].Size.z);
//                         maxSizeZX = Mathf.Max(maxSizeZX, (float)sizeZX / 2f);
//                         string groupName = markers[index].GroupName;
//                         Vector2 vector2 = new Vector2((float)start.x + (float)markers[index].Size.x / 2f, (float)start.z + (float)markers[index].Size.z / 2f);
//                         vector = new Vector2((float)vector2i.x + vector2.x, (float)vector2i.y + vector2.y);
//                         Vector2 vector3 = vector;
//                         bool isPrefabPath = false;
//                         if (markers.Count > 1)
//                         {
//                             markers = wildernessPrefab.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
//                             if (markers.Count > 0)
//                             {
//                                 index = gameRandom.RandomRange(0, markers.Count);
//                                 vector3 = new Vector2((float)(vector2i.x + markers[index].Start.x) + (float)markers[index].Size.x / 2f, (float)(vector2i.y + markers[index].Start.z) + (float)markers[index].Size.z / 2f);
//                             }
//                             isPrefabPath = true;
//                         }
//                         Path path = new Path(_isCountryRoad: true, maxSizeZX);
//                         path.FinalPathPoints.Add(new Vector2(vector.x, vector.y));
//                         path.pathPoints3d.Add(new Vector3(vector.x, vector3i2.y, vector.y));
//                         path.FinalPathPoints.Add(new Vector2(vector3.x, vector3.y));
//                         path.pathPoints3d.Add(new Vector3(vector3.x, vector3i2.y, vector3.y));
//                         path.IsPrefabPath = isPrefabPath;
//                         path.StartPointID = prefabId;
//                         path.EndPointID = prefabId;
//                         WorldBuilder.Instance.wildernessPaths.Add(path);
//                     }
//                 }
//                 SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(vector2i.x, Mathf.CeilToInt(medianHeight + wildernessPrefab.yOffset + 1), vector2i.y), (byte)rotation);
//                 PrefabDataInstance pdi = new PrefabDataInstance(prefabId, new Vector3i(vector3i.x, medianHeight + wildernessPrefab.yOffset + 1, vector3i.z), (byte)rotation, wildernessPrefab);
//                 AddPrefab(pdi);
//                 WorldBuilder.Instance.WildernessPrefabCount++;
//                 if (medianHeight != getHeightCeil(rect.min.x, rect.min.y) || medianHeight != getHeightCeil(rect.max.x, rect.min.y) || medianHeight != getHeightCeil(rect.min.x, rect.max.y) || medianHeight != getHeightCeil(rect.max.x, rect.max.y))
//                 {
//                     WildernessPOICenter = new Vector2i(rect.center);
//                     WildernessPOISize = Mathf.RoundToInt(Mathf.Max(rect.size.x, rect.size.y));
//                     WildernessPOIHeight = medianHeight;
//                 }
//                 if (maxSizeZX != 0f)
//                 {
//                     WildernessPlanner.WildernessPathInfos.Add(new WorldBuilder.WildernessPathInfo(new Vector2i(vector), prefabId, maxSizeZX, WorldBuilder.Instance.GetBiome((int)vector.x, (int)vector.y)));
//                 }
//                 int num12 = Mathf.FloorToInt(rect.x / 10f) - 1;
//                 int num13 = Mathf.CeilToInt(rect.xMax / 10f) + 1;
//                 int num14 = Mathf.FloorToInt(rect.y / 10f) - 1;
//                 int num15 = Mathf.CeilToInt(rect.yMax / 10f) + 1;
//                 for (int j = num12; j < num13; j++)
//                 {
//                     for (int k = num14; k < num15; k++)
//                     {
//                         if (j >= 0 && j < WorldBuilder.Instance.PathingGrid.GetLength(0) && k >= 0 && k < WorldBuilder.Instance.PathingGrid.GetLength(1))
//                         {
//                             if (j == num12 || j == num13 - 1 || k == num14 || k == num15 - 1)
//                             {
//                                 PathingUtils.SetPathBlocked(j, k, 2);
//                             }
//                             else
//                             {
//                                 PathingUtils.SetPathBlocked(j, k, isBlocked: true);
//                             }
//                         }
//                     }
//                 }
//                 num12 = Mathf.FloorToInt(rect.x) - 1;
//                 num13 = Mathf.CeilToInt(rect.xMax) + 1;
//                 num14 = Mathf.FloorToInt(rect.y) - 1;
//                 num15 = Mathf.CeilToInt(rect.yMax) + 1;
//                 for (int l = num12; l < num13; l += 150)
//                 {
//                     for (int n = num14; n < num15; n += 150)
//                     {
//                         StreetTile streetTileWorld = WorldBuilder.Instance.GetStreetTileWorld(l, n);
//                         if (streetTileWorld != null)
//                         {
//                             streetTileWorld.Used = true;
//                         }
//                     }
//                 }
//                 GameRandomManager.Instance.FreeGameRandom(gameRandom);
//                 return true;

//             end_IL_03d4:
//                 break;
//             }
//         }
//         GameRandomManager.Instance.FreeGameRandom(gameRandom);
//         return false;
//     }

//     public static bool Prefix(StreetTile __instance)
//     {
//         SpawnWildernessCaveEntrances(__instance);
//         return true;
//     }
// }