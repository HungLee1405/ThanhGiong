using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class VietnamVillageGenerator : EditorWindow
{
    private const string PrefabFolder = "Assets/LeartesStudios/BanditsValley/HDRP/Art/Prefabs/";
    private const string TerrainLayerFolder = "Assets/LeartesStudios/BanditsValley/HDRP/Art/Terrain/";

    [MenuItem("Tools/Vietnam Village/Generate Map")]
    public static void GenerateMap()
    {
        // 1. Create a new Scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "VietnamVillageMap";

        // Remove the default directional light to customize it
        GameObject defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null)
        {
            DestroyImmediate(defaultLight);
        }

        // 2. Setup Lighting and Environment
        GameObject sunObj = new GameObject("Warm Sun (Directional Light)");
        Light sunLight = sunObj.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.92f, 0.78f); // Warm tropical golden sun
        sunLight.intensity = 2.0f;
        sunLight.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(35, -50, 0);

        // 3. Create Root GameObjects for Hierarchy Organization
        GameObject rootMap = new GameObject("== Vietnam Village Map == ");
        GameObject rootHouses = new GameObject("Houses");
        GameObject rootVegetation = new GameObject("Vegetation & Bamboo");
        GameObject rootProps = new GameObject("Decorations & Props");
        GameObject rootRiceFields = new GameObject("Rice Fields");
        rootHouses.transform.parent = rootMap.transform;
        rootVegetation.transform.parent = rootMap.transform;
        rootProps.transform.parent = rootMap.transform;
        rootRiceFields.transform.parent = rootMap.transform;

        // 4. Create Terrain
        int terrainSize = 250; // Increased size for richer terrain
        int heightmapRes = 513;
        int alphamapRes = 513;

        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(terrainSize, 45, terrainSize); // Slightly taller hills
        terrainData.heightmapResolution = heightmapRes;
        terrainData.alphamapResolution = alphamapRes;

        // Setup Terrain Layers (Textures)
        TerrainLayer[] layers = new TerrainLayer[4];
        layers[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerFolder + "TerrainLayer_01.terrainlayer"); // Grass
        layers[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerFolder + "TerrainLayer_02.terrainlayer"); // Dirt (Roads/Banks)
        layers[2] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerFolder + "TerrainLayer_03.terrainlayer"); // Rock (Hills)
        layers[3] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerFolder + "TerrainLayer_05.terrainlayer"); // Dry grass (Rice fields)

        // Fallbacks if not found
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogWarning($"TerrainLayer_{i} không tìm thấy, sử dụng layer mới.");
                layers[i] = new TerrainLayer();
            }
        }
        terrainData.terrainLayers = layers;

        // 5. Generate Terrain Heightmap (River, Hills & Plains with Perlin Noise)
        float[,] heights = new float[heightmapRes, heightmapRes];
        float riverXPos = 175f; // Shifted river to the right to accommodate larger map

        for (int z = 0; z < heightmapRes; z++)
        {
            float zNorm = (float)z / heightmapRes;
            // Winding river math
            float windingRiverX = riverXPos + Mathf.Sin(zNorm * Mathf.PI * 4f) * 20f;

            for (int x = 0; x < heightmapRes; x++)
            {
                float xNorm = (float)x / heightmapRes;
                float xWorld = xNorm * terrainSize;
                float zWorld = zNorm * terrainSize;

                // Base height with Perlin Noise for natural uneven ground
                float baseNoise = Mathf.PerlinNoise(xNorm * 5f, zNorm * 5f) * 0.015f;
                float height = 0.08f + baseNoise; // Ground level (~3.6m to 4.3m)

                // Winding Riverbed carving
                float distToRiver = Mathf.Abs(xWorld - windingRiverX);
                if (distToRiver < 22f)
                {
                    float factor = distToRiver / 22f;
                    // Lower height smoothly to create river channel
                    height = Mathf.Lerp(0.01f, height, factor * factor);
                }

                // Richer terrain: Add mountains/hills at the boundaries (Left, Top, Bottom, and Right edge)
                float borderHills = 0f;
                if (xWorld < 45f) // Left hills
                {
                    borderHills += Mathf.InverseLerp(45f, 0f, xWorld) * 0.35f;
                }
                if (xWorld > 210f) // Right hills beyond the river
                {
                    borderHills += Mathf.InverseLerp(210f, 250f, xWorld) * 0.45f;
                }
                if (zWorld > 210f) // Top hills
                {
                    borderHills += Mathf.InverseLerp(210f, 250f, zWorld) * 0.3f;
                }
                if (zWorld < 35f) // Bottom hills
                {
                    borderHills += Mathf.InverseLerp(35f, 0f, zWorld) * 0.25f;
                }

                // Add detailed mountain noise
                if (borderHills > 0)
                {
                    float detailNoise = Mathf.PerlinNoise(xNorm * 12f, zNorm * 12f) * 0.08f;
                    borderHills += detailNoise;
                }

                height += borderHills;
                heights[z, x] = height;
            }
        }
        terrainData.SetHeights(0, 0, heights);

        // 6. Paint Textures (Alphamaps)
        float[,,] alphamaps = new float[alphamapRes, alphamapRes, 4];
        
        // Define key locations for path generation and house placement (9 houses total)
        Vector3 villageGateLoc = new Vector3(55, 0, 50);
        Vector3 villageCenterLoc = new Vector3(100, 0, 105);
        List<Vector3> houseLocs = new List<Vector3>()
        {
            new Vector3(80, 0, 90),
            new Vector3(75, 0, 125),
            new Vector3(115, 0, 85),
            new Vector3(120, 0, 120),
            new Vector3(95, 0, 145),
            new Vector3(55, 0, 105),  // House near entrance left
            new Vector3(50, 0, 135),  // House left upper
            new Vector3(95, 0, 70),   // House lower center
            new Vector3(135, 0, 145)  // House upper right near river
        };

        for (int z = 0; z < alphamapRes; z++)
        {
            float zNorm = (float)z / alphamapRes;
            float zWorld = zNorm * terrainSize;
            float windingRiverX = riverXPos + Mathf.Sin(zNorm * Mathf.PI * 4f) * 20f;

            for (int x = 0; x < alphamapRes; x++)
            {
                float xNorm = (float)x / alphamapRes;
                float xWorld = xNorm * terrainSize;

                float distToRiver = Mathf.Abs(xWorld - windingRiverX);

                float wGrass = 1f;
                float wDirt = 0f;
                float wRock = 0f;
                float wDryGrass = 0f;

                // Riverbed painting (Dirt and stone)
                if (distToRiver < 22f)
                {
                    wGrass = 0.05f;
                    wDirt = 0.7f;
                    wRock = 0.25f;
                }
                else
                {
                    // Calculate dirt paths: Main road from gate to center, then branches to all houses
                    float distToMainRoad = DistanceToLineSegment(new Vector2(xWorld, zWorld), new Vector2(villageGateLoc.x, villageGateLoc.z), new Vector2(villageCenterLoc.x, villageCenterLoc.z));
                    float distToHouseRoads = float.MaxValue;
                    
                    foreach (var houseLoc in houseLocs)
                    {
                        float d = DistanceToLineSegment(new Vector2(xWorld, zWorld), new Vector2(villageCenterLoc.x, villageCenterLoc.z), new Vector2(houseLoc.x, houseLoc.z));
                        if (d < distToHouseRoads) distToHouseRoads = d;
                    }

                    float roadDist = Mathf.Min(distToMainRoad, distToHouseRoads);
                    if (roadDist < 5f) // Slightly wider roads (5 meters)
                    {
                        float factor = roadDist / 5f;
                        wDirt = Mathf.Lerp(1.0f, 0.0f, factor * factor); // Shaper transition
                        wGrass = 1.0f - wDirt;
                    }

                    // Hills/Mountains textures painting
                    float height = heights[z, x];
                    if (height > 0.14f)
                    {
                        float hillFactor = Mathf.InverseLerp(0.14f, 0.45f, height);
                        wRock = hillFactor * 0.75f;
                        wDryGrass = hillFactor * 0.25f;
                        wGrass = 1f - (wRock + wDryGrass);
                    }

                    // Rice fields (Ruộng lúa): Paint DryGrass in big rectangular grids
                    if (xWorld > 25f && xWorld < 70f && zWorld > 85f && zWorld < 190f)
                    {
                        // Exclude road area from rice fields
                        if (roadDist > 5f)
                        {
                            // Divisions representing dykes (Bờ ruộng)
                            float borderX = Mathf.Abs(Mathf.Sin(xWorld * 0.3f));
                            float borderZ = Mathf.Abs(Mathf.Sin(zWorld * 0.3f));
                            if (borderX > 0.18f && borderZ > 0.18f)
                            {
                                wDryGrass = 0.85f;
                                wGrass = 0.15f;
                            }
                            else
                            {
                                // Mud dykes
                                wDirt = 0.85f;
                                wGrass = 0.15f;
                            }
                        }
                    }
                }

                // Normalize weights
                float total = wGrass + wDirt + wRock + wDryGrass;
                alphamaps[z, x, 0] = wGrass / total;
                alphamaps[z, x, 1] = wDirt / total;
                alphamaps[z, x, 2] = wRock / total;
                alphamaps[z, x, 3] = wDryGrass / total;
            }
        }
        terrainData.SetAlphamaps(0, 0, alphamaps);

        // Instantiate Terrain GameObject
        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "Village Ground Terrain";
        terrainObj.transform.parent = rootMap.transform;
        Terrain terrain = terrainObj.GetComponent<Terrain>();
        terrain.treeDistance = 1200;
        terrain.detailObjectDistance = 300;

        // 7. Place River Water (Water level raised to 3.5m)
        GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Water.prefab");
        if (waterPrefab != null)
        {
            // Place water along the winding riverbed at height 3.5m (0.077 * 45m = ~3.5m)
            // Just slightly below the ground height of 4.0m
            for (int zPos = 10; zPos < terrainSize; zPos += 15)
            {
                float zNorm = (float)zPos / terrainSize;
                float windingRiverX = riverXPos + Mathf.Sin(zNorm * Mathf.PI * 4f) * 20f;
                Vector3 pos = new Vector3(windingRiverX, 3.5f, zPos); 
                
                GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);
                waterInstance.name = $"River Water Segment {zPos}";
                waterInstance.transform.position = pos;
                waterInstance.transform.localScale = new Vector3(10f, 1f, 8f); // Widened
                waterInstance.transform.parent = rootMap.transform;
            }
        }

        // Place Footbridges (Cầu khỉ / Cầu gỗ) across the river
        GameObject plankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Plank.prefab");
        if (plankPrefab != null)
        {
            // Place two bridges at different parts of the river (z = 70 and z = 160)
            float[] bridgeZPoints = { 70f, 160f };
            foreach (var bz in bridgeZPoints)
            {
                float zNorm = bz / terrainSize;
                float windingRiverX = riverXPos + Mathf.Sin(zNorm * Mathf.PI * 4f) * 20f;
                
                Vector3 bridgeCenter = new Vector3(windingRiverX, 3.8f, bz);
                
                // Construct a footbridge using planks
                for (int p = -2; p <= 2; p++)
                {
                    GameObject plank = (GameObject)PrefabUtility.InstantiatePrefab(plankPrefab);
                    plank.name = $"Bridge_Plank_{bz}_{p}";
                    plank.transform.position = bridgeCenter + new Vector3(p * 3.5f, 0, 0);
                    plank.transform.rotation = Quaternion.Euler(0, 90, 0);
                    plank.transform.localScale = new Vector3(1.2f, 1f, 3f);
                    plank.transform.parent = rootProps.transform;
                }

                // Wooden support posts using WoodLog prefabs
                GameObject postPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_WoodLog.prefab");
                if (postPrefab != null)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 postPos = bridgeCenter + new Vector3(side * 5f, -2f, 0);
                        postPos.y = terrain.SampleHeight(postPos);
                        GameObject post = (GameObject)PrefabUtility.InstantiatePrefab(postPrefab);
                        post.name = $"Bridge_Post_{bz}_{side}";
                        post.transform.position = postPos;
                        post.transform.localScale = new Vector3(1.5f, 3.5f, 1.5f);
                        post.transform.parent = rootProps.transform;
                    }
                }
            }
        }

        // 8. Place Village Gate (Cổng làng)
        GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_WoodLog_Gate.prefab");
        if (gatePrefab == null)
        {
            gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Maingate.prefab");
        }
        if (gatePrefab != null)
        {
            GameObject gate = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate.name = "Cong Lang (Village Gate)";
            gate.transform.position = new Vector3(villageGateLoc.x, terrain.SampleHeight(villageGateLoc), villageGateLoc.z);
            gate.transform.rotation = Quaternion.Euler(0, 45, 0); 
            gate.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            gate.transform.parent = rootMap.transform;
        }

        // Ancient Banyan Tree near gate
        GameObject bigTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Tree 3.prefab");
        if (bigTreePrefab != null)
        {
            GameObject banyan = (GameObject)PrefabUtility.InstantiatePrefab(bigTreePrefab);
            banyan.name = "Cay Da Dau Lang (Ancient Banyan Tree)";
            Vector3 treePos = villageGateLoc + new Vector3(-10, 0, -4);
            treePos.y = terrain.SampleHeight(treePos);
            banyan.transform.position = treePos;
            banyan.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f); // Even bigger
            banyan.transform.parent = rootMap.transform;
        }

        // 9. Place Houses (9 houses total)
        string[] houseNames = { 
            "SM_House.prefab", "SM_House_02.prefab", "SM_House_03.prefab", 
            "SM_House_04.prefab", "SM_House_06.prefab", "SM_House.prefab", 
            "SM_House_02.prefab", "SM_House_03.prefab", "SM_House_06.prefab" 
        };
        float[] houseRotations = { -30f, 45f, -120f, 15f, 90f, 0f, -45f, 60f, -90f };

        for (int i = 0; i < houseLocs.Count; i++)
        {
            GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + houseNames[i % houseNames.Length]);
            if (housePrefab != null)
            {
                Vector3 pos = houseLocs[i];
                pos.y = terrain.SampleHeight(pos);
                
                GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab);
                house.name = $"Nha Dan {i + 1} ({houseNames[i % houseNames.Length].Replace(".prefab", "")})";
                house.transform.position = pos;
                house.transform.rotation = Quaternion.Euler(0, houseRotations[i], 0);
                house.transform.parent = rootHouses.transform;

                // Courtyard tiles
                GameObject brickFloorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Floor.prefab");
                if (brickFloorPrefab != null)
                {
                    Vector3 forwardDir = house.transform.forward;
                    Vector3 rightDir = house.transform.right;
                    Vector3 courtCenter = pos + forwardDir * 8f;

                    for (int xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        for (int zOffset = 0; zOffset <= 1; zOffset++)
                        {
                            Vector3 tilePos = courtCenter + rightDir * (xOffset * 4f) + forwardDir * (zOffset * 4f);
                            tilePos.y = terrain.SampleHeight(tilePos) + 0.05f; 
                            
                            GameObject floor = (GameObject)PrefabUtility.InstantiatePrefab(brickFloorPrefab);
                            floor.name = $"Courtyard_Tile_{xOffset}_{zOffset}";
                            floor.transform.position = tilePos;
                            floor.transform.rotation = house.transform.rotation;
                            floor.transform.parent = rootProps.transform;
                        }
                    }
                }

                // Place house props (Chum nước, đống củi, xe bò...)
                PlaceHouseDecorations(pos, house.transform.rotation, terrain, rootProps);
            }
        }

        // 10. Water Well & Watchtower at central area
        GameObject wellCenterObj = new GameObject("Gieng Nuoc (Water Well Area)");
        wellCenterObj.transform.position = villageCenterLoc + new Vector3(12, 0, -12);
        wellCenterObj.transform.position = new Vector3(wellCenterObj.transform.position.x, terrain.SampleHeight(wellCenterObj.transform.position), wellCenterObj.transform.position.z);
        wellCenterObj.transform.parent = rootProps.transform;

        GameObject potPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Pot.prefab");
        if (potPrefab != null)
        {
            for (int angle = 0; angle < 360; angle += 45) // Closer packed jars
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad) * 2.2f, 0, Mathf.Sin(rad) * 2.2f);
                Vector3 jarPos = wellCenterObj.transform.position + offset;
                jarPos.y = terrain.SampleHeight(jarPos);

                GameObject jar = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab);
                jar.name = $"Gieng_Pot_{angle}";
                jar.transform.position = jarPos;
                jar.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                jar.transform.parent = wellCenterObj.transform;
            }
        }
        
        GameObject bucketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Bucket.prefab");
        if (bucketPrefab != null)
        {
            GameObject bucket = (GameObject)PrefabUtility.InstantiatePrefab(bucketPrefab);
            bucket.name = "Gao Nuoc (Well Bucket)";
            bucket.transform.position = wellCenterObj.transform.position + new Vector3(0.3f, 0.1f, 0.3f);
            bucket.transform.parent = wellCenterObj.transform;
        }

        // Watchtower near entrance/river boundary
        GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_WatchTower.prefab");
        if (towerPrefab != null)
        {
            GameObject tower = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab);
            tower.name = "Thap Canh (Watchtower)";
            Vector3 towerPos = new Vector3(135, 0, 50);
            towerPos.y = terrain.SampleHeight(towerPos);
            tower.transform.position = towerPos;
            tower.transform.rotation = Quaternion.Euler(0, -30, 0);
            tower.transform.parent = rootMap.transform;
        }

        // 11. Place Vegetation & Bamboo Groves (Lũy Tre Làng - Increased Density)
        GameObject tree1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Tree 1.prefab");
        GameObject tree2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Tree 2.prefab");
        GameObject tree3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Tree 3.prefab");
        GameObject tree4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Tree 4.prefab");

        List<GameObject> treePrefabs = new List<GameObject>() { tree1Prefab, tree2Prefab, tree3Prefab, tree4Prefab };

        // Increased bamboo clusters centers
        List<Vector3> bambooClusterCenters = new List<Vector3>()
        {
            new Vector3(25, 0, 50),   // Gate Left
            new Vector3(85, 0, 45),   // Gate Right
            new Vector3(20, 0, 100),  // Left border
            new Vector3(30, 0, 160),  // Left upper border
            new Vector3(70, 0, 185),  // Top border left
            new Vector3(130, 0, 195), // Top border center
            new Vector3(180, 0, 180), // Top border right
            new Vector3(145, 0, 70),  // Riverbank lower
            new Vector3(150, 0, 115), // Riverbank center
            new Vector3(140, 0, 165), // Riverbank upper
            new Vector3(85, 0, 115)   // Middle village divider grove
        };

        foreach (var center in bambooClusterCenters)
        {
            // Heightened density (20-30 trees per cluster)
            int count = Random.Range(20, 31);
            for (int j = 0; j < count; j++)
            {
                float radius = Random.Range(1.0f, 7.5f);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 treePos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                treePos.y = terrain.SampleHeight(treePos);

                // Ignore deep water
                if (treePos.y < 3.4f) continue;

                GameObject selectedTreePrefab = treePrefabs[Random.Range(0, 4)];
                if (selectedTreePrefab != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(selectedTreePrefab);
                    tree.name = $"Tre_Luy_{center.x}_{j}";
                    tree.transform.position = treePos;
                    tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    // Simulate Bamboo parameters
                    float heightScale = Random.Range(1.5f, 2.5f);
                    float thicknessScale = Random.Range(0.5f, 0.8f);
                    tree.transform.localScale = new Vector3(thicknessScale, heightScale, thicknessScale);
                    
                    tree.transform.parent = rootVegetation.transform;
                }
            }
        }

        // Add 50 extra scattered trees on hills
        for (int i = 0; i < 50; i++)
        {
            Vector3 hillPos = new Vector3(Random.Range(10f, 240f), 0, Random.Range(10f, 240f));
            hillPos.y = terrain.SampleHeight(hillPos);

            if (hillPos.y > 6.0f) // Only on hills
            {
                GameObject selectedTreePrefab = treePrefabs[Random.Range(0, 4)];
                if (selectedTreePrefab != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(selectedTreePrefab);
                    tree.name = $"Scattered_Hill_Tree_{i}";
                    tree.transform.position = hillPos;
                    tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    tree.transform.localScale = new Vector3(Random.Range(1f, 1.5f), Random.Range(1f, 1.5f), Random.Range(1f, 1.5f));
                    tree.transform.parent = rootVegetation.transform;
                }
            }
        }

        // 12. Row-aligned Crops in Rice Fields (Ruộng lúa xanh mướt)
        // We will plant grass prefabs in straight rows to simulate paddy fields
        GameObject paddyGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Grass_04Foliage.prefab");
        if (paddyGrassPrefab == null)
        {
            paddyGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Grass_03Foliage.prefab");
        }

        if (paddyGrassPrefab != null)
        {
            // Paint crops in the defined rice field region
            float minX = 28f, maxX = 68f;
            float minZ = 88f, maxZ = 188f;

            for (float rz = minZ; rz < maxZ; rz += 2.5f) // Rows spaced by 2.5m
            {
                // Check if this row z-coordinate is near a dyke border to skip placing crops
                float borderZ = Mathf.Abs(Mathf.Sin(rz * 0.3f));
                if (borderZ < 0.2f) continue;

                for (float rx = minX; rx < maxX; rx += 1.8f) // Crops spaced by 1.8m in the row
                {
                    float borderX = Mathf.Abs(Mathf.Sin(rx * 0.3f));
                    if (borderX < 0.2f) continue;

                    Vector3 cropPos = new Vector3(rx, 0, rz);
                    cropPos.y = terrain.SampleHeight(cropPos);

                    GameObject crop = (GameObject)PrefabUtility.InstantiatePrefab(paddyGrassPrefab);
                    crop.name = $"Rice_Paddy_{rx}_{rz}";
                    crop.transform.position = cropPos;
                    crop.transform.localScale = new Vector3(1.1f, 1.4f, 1.1f); // Stretched vertically to look like rice stalks
                    crop.transform.rotation = Quaternion.identity;
                    crop.transform.parent = rootRiceFields.transform;
                }
            }
        }

        // Place general grass & foliage (250 items for lush environment)
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Grass_01Foliage.prefab");
        GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Bush_01Foliage.prefab");
        if (grassPrefab != null || bushPrefab != null)
        {
            for (int i = 0; i < 250; i++)
            {
                Vector3 randPos = new Vector3(Random.Range(30f, 200f), 0, Random.Range(30f, 220f));
                randPos.y = terrain.SampleHeight(randPos);

                // Avoid water
                if (randPos.y < 3.6f) continue;

                float windingRiverX = riverXPos + Mathf.Sin((randPos.z / terrainSize) * Mathf.PI * 4f) * 20f;
                if (Mathf.Abs(randPos.x - windingRiverX) < 18f) continue;

                GameObject foliagePrefab = (Random.value > 0.4f) ? grassPrefab : bushPrefab;
                if (foliagePrefab != null)
                {
                    GameObject foliage = (GameObject)PrefabUtility.InstantiatePrefab(foliagePrefab);
                    foliage.name = $"Scattered_Co_{i}";
                    foliage.transform.position = randPos;
                    foliage.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    foliage.transform.localScale = new Vector3(Random.Range(0.9f, 1.6f), Random.Range(0.9f, 1.6f), Random.Range(0.9f, 1.6f));
                    foliage.transform.parent = rootVegetation.transform;
                }
            }
        }

        // 13. Save Scene
        string scenePath = "Assets/Scenes/VietnamVillageMap.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"Bản đồ làng Việt Nam (Nâng cấp) đã được tạo và lưu thành công tại: {scenePath}");
        
        // Open the scene
        EditorSceneManager.OpenScene(scenePath);
        
        EditorUtility.DisplayDialog("Thành Công", "Bản đồ làng Việt Nam phiên bản NÂNG CẤP đã được tạo thành công!\n- Tăng số lượng nhà lên 9\n- Lũy tre dày đặc và thêm cây đồi núi\n- Ruộng lúa cấy theo hàng\n- Nâng cao mặt nước sông sát mặt đất\n- Các con đường nối liền cổng làng và tất cả các nhà.", "Đồng ý");
    }

    private static void PlaceHouseDecorations(Vector3 housePos, Quaternion houseRot, Terrain terrain, GameObject rootProps)
    {
        Vector3 forward = houseRot * Vector3.forward;
        Vector3 right = houseRot * Vector3.right;

        // Pot (Chum nước) on the left side of the house
        GameObject potPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Pot.prefab");
        if (potPrefab != null)
        {
            Vector3 potPos = housePos - right * 5.5f + forward * 2.2f;
            potPos.y = terrain.SampleHeight(potPos);
            GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab);
            pot.name = "House_ChumNuoc";
            pot.transform.position = potPos;
            pot.transform.parent = rootProps.transform;

            // Extra smaller pots
            Vector3 potPos2 = potPos + right * 1.5f - forward * 0.8f;
            potPos2.y = terrain.SampleHeight(potPos2);
            GameObject pot2 = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab);
            pot2.name = "House_ChumNuoc_Phu";
            pot2.transform.position = potPos2;
            pot2.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            pot2.transform.parent = rootProps.transform;
        }

        // Firewood stack (Đống củi) on the side
        GameObject firewoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Firewood.prefab");
        if (firewoodPrefab != null)
        {
            Vector3 woodPos = housePos - right * 6.5f - forward * 3.5f;
            woodPos.y = terrain.SampleHeight(woodPos);
            GameObject wood = (GameObject)PrefabUtility.InstantiatePrefab(firewoodPrefab);
            wood.name = "House_DongCui";
            wood.transform.position = woodPos;
            wood.transform.rotation = houseRot * Quaternion.Euler(0, 90, 0);
            wood.transform.parent = rootProps.transform;
        }

        // Cart (Xe bò/xe kéo) in the yard
        GameObject cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Cart.prefab");
        if (cartPrefab != null)
        {
            Vector3 cartPos = housePos + right * 8.5f + forward * 7.5f;
            cartPos.y = terrain.SampleHeight(cartPos);
            GameObject cart = (GameObject)PrefabUtility.InstantiatePrefab(cartPrefab);
            cart.name = "Yard_XeKeo";
            cart.transform.position = cartPos;
            cart.transform.rotation = houseRot * Quaternion.Euler(0, -45, 0);
            cart.transform.parent = rootProps.transform;
        }

        // Bench (Ghế dài) in the courtyard
        GameObject benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Bench.prefab");
        if (benchPrefab != null)
        {
            Vector3 benchPos = housePos - right * 4.5f + forward * 6.5f;
            benchPos.y = terrain.SampleHeight(benchPos);
            GameObject bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab);
            bench.name = "Yard_GheGo";
            bench.transform.position = benchPos;
            bench.transform.rotation = houseRot * Quaternion.Euler(0, 90, 0);
            bench.transform.parent = rootProps.transform;
        }

        // Fences (Hàng rào tre/gỗ bao quanh nhà)
        GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "SM_Fence.prefab");
        if (fencePrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                // Left Fence
                Vector3 leftFencePos = housePos - right * 8.5f + forward * (i * 4f);
                leftFencePos.y = terrain.SampleHeight(leftFencePos);
                GameObject leftFence = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab);
                leftFence.name = $"House_Fence_L_{i}";
                leftFence.transform.position = leftFencePos;
                leftFence.transform.rotation = houseRot * Quaternion.Euler(0, 90, 0);
                leftFence.transform.parent = rootProps.transform;

                // Right Fence
                Vector3 rightFencePos = housePos + right * 9.5f + forward * (i * 4f);
                rightFencePos.y = terrain.SampleHeight(rightFencePos);
                GameObject rightFence = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab);
                rightFence.name = $"House_Fence_R_{i}";
                rightFence.transform.position = rightFencePos;
                rightFence.transform.rotation = houseRot * Quaternion.Euler(0, 90, 0);
                rightFence.transform.parent = rootProps.transform;
            }
        }
    }

    private static float DistanceToLineSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq == 0f) return ap.magnitude;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
        Vector2 projection = a + t * ab;
        return (p - projection).magnitude;
    }
}
