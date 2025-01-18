// Assets/Scripts/DungeonGeneration/Managers/DungeonVisualizer.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration;
using DungeonGeneration.Components;  // Добавляем эту строку


namespace DungeonGeneration.Managers
{
        public class DungeonVisualizer
    {
        private DungeonConfig config;
        private TileSet tileSet;
        private Transform dungeonParent;
        private System.Random random;
        private TileType[,] dungeonGrid; // добавляем это поле

        public DungeonVisualizer(DungeonConfig config, TileSet tileSet, System.Random random)
        {
            this.config = config;
            this.tileSet = tileSet;
            this.random = random;
            this.dungeonParent = new GameObject("Generated Dungeon").transform;
        }

        public void VisualizeDungeon(TileType[,] grid, List<Room> rooms)
        {
            this.dungeonGrid = grid; // сохраняем ссылку на сетку
            CreateBasicStructure(grid);
            AddDoors(rooms);
            AddDecorations(rooms);
            AddEnemies(rooms);
            SpawnPlayer(rooms.First(r => r.type == RoomType.Start));
        }

        private void CreateBasicStructure(TileType[,] dungeonGrid)
        {
            Transform floorsParent = CreateChild("Floors");
            Transform wallsParent = CreateChild("Walls");

            for (int x = 0; x < config.dungeonSize.x; x++)
            {
                for (int y = 0; y < config.dungeonSize.y; y++)
                {
                    if (dungeonGrid[x, y] == TileType.Floor)
                    {
                        CreateTile(tileSet.floorPrefab,
                            new Vector3(x, 0, y),
                            Quaternion.identity, floorsParent);

                        CheckAndCreateWall(x, y, dungeonGrid, wallsParent);
                    }
                }
            }
        }

        private void CheckAndCreateWall(int x, int y, TileType[,] grid, Transform wallsParent)
        {
            // Проверяем все четыре стороны клетки
            if (!IsInBounds(new Vector2Int(x + 1, y)) || grid[x + 1, y] == TileType.Empty)
            {
                // Правая стена
                CreateTile(tileSet.wallPrefab, 
                    new Vector3(x + 0.5f, 1f, y), 
                    Quaternion.Euler(0, 0, 0), 
                    wallsParent);
            }
            
            if (!IsInBounds(new Vector2Int(x - 1, y)) || grid[x - 1, y] == TileType.Empty)
            {
                // Левая стена
                CreateTile(tileSet.wallPrefab, 
                    new Vector3(x - 0.5f, 1f, y), 
                    Quaternion.Euler(0, 0, 0), 
                    wallsParent);
            }
            
            if (!IsInBounds(new Vector2Int(x, y + 1)) || grid[x, y + 1] == TileType.Empty)
            {
                // Передняя стена
                CreateTile(tileSet.wallPrefab, 
                    new Vector3(x, 1f, y + 0.5f), 
                    Quaternion.Euler(0, 90, 0), 
                    wallsParent);
            }
            
            if (!IsInBounds(new Vector2Int(x, y - 1)) || grid[x, y - 1] == TileType.Empty)
            {
                // Задняя стена
                CreateTile(tileSet.wallPrefab, 
                    new Vector3(x, 1f, y - 0.5f), 
                    Quaternion.Euler(0, 90, 0), 
                    wallsParent);
            }
        }

        private void CheckWallDirection(int x, int y, TileType[,] grid, Vector3 position,
            float rotation, Transform parent)
        {
            if (!IsInBounds(new Vector2Int(x, y)) || grid[x, y] == TileType.Empty)
            {
                CreateTile(tileSet.wallPrefab, position,
                    Quaternion.Euler(0, rotation, 0), parent);
            }
        }

        private void AddDoors(List<Room> rooms)
        {
            if (!config.useDoors || tileSet.doorPrefab == null) return;

            Transform doorsParent = CreateChild("Doors");

            foreach (var room in rooms)
            {
                foreach (var doorPos in room.doorPositions)
                {
                    // Определяем направление двери, проверяя соседние клетки
                    bool hasVerticalConnection = IsFloor(doorPos.x, doorPos.y + 1) || IsFloor(doorPos.x, doorPos.y - 1);
                    bool hasHorizontalConnection = IsFloor(doorPos.x + 1, doorPos.y) || IsFloor(doorPos.x - 1, doorPos.y);

                    Vector3 position;
                    float rotation;

                    if (hasHorizontalConnection)
                    {
                        // Дверь в боковой стене
                        bool isRightSide = IsFloor(doorPos.x + 1, doorPos.y);
                        position = new Vector3(
                            doorPos.x + (isRightSide ? 0.5f : -0.5f),
                            0.5f, // Половина высоты стены
                            doorPos.y
                        );
                        rotation = 0f;
                    }
                    else if (hasVerticalConnection)
                    {
                        // Дверь в верхней/нижней стене
                        bool isTopSide = IsFloor(doorPos.x, doorPos.y + 1);
                        position = new Vector3(
                            doorPos.x,
                            0.5f, // Половина высоты стены
                            doorPos.y + (isTopSide ? 0.5f : -0.5f)
                        );
                        rotation = 90f;
                    }
                    else
                    {
                        // Если не можем определить направление, пропускаем эту дверь
                        continue;
                    }

                    GameObject door = CreateTile(tileSet.doorPrefab,
                        position,
                        Quaternion.Euler(0, rotation, 0),
                        doorsParent);

                    if (door.TryGetComponent<Door>(out var doorComponent))
                    {
                        doorComponent.isSecret = room.type == RoomType.Secret;
                    }
                }
            }
        }

        private bool IsFloor(int x, int y)
        {
            if (!IsInBounds(new Vector2Int(x, y))) return false;
            return dungeonGrid[x, y] == TileType.Floor;
        }

        
        private bool HasWallAt(Vector2Int pos)
        {
            if (!IsInBounds(pos)) return true;
            return dungeonGrid[pos.x, pos.y] == TileType.Empty;
        }

        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < config.dungeonSize.x &&
                pos.y >= 0 && pos.y < config.dungeonSize.y;
        }


        private void AddDecorations(List<Room> rooms)
        {
            if (tileSet.decorationPrefabs == null || tileSet.decorationPrefabs.Length == 0)
                return;

            Transform decorParent = CreateChild("Decorations");
            var usedPositions = new HashSet<Vector2Int>();

            foreach (var room in rooms)
            {
                int decorCount = Mathf.RoundToInt(
                    (room.width * room.height) * config.decorationDensity * 0.1f);

                for (int i = 0; i < decorCount; i++)
                {
                    Vector2Int pos = GetValidDecorationPosition(room, usedPositions);
                    if (pos != Vector2Int.zero)
                    {
                        GameObject decorPrefab = tileSet.decorationPrefabs[
                            random.Next(tileSet.decorationPrefabs.Length)];

                        CreateTile(decorPrefab,
                            new Vector3(pos.x, 0, pos.y),
                            Quaternion.Euler(0, random.Next(360), 0),
                            decorParent);

                        usedPositions.Add(pos);
                    }
                }
            }
        }

        private void AddEnemies(List<Room> rooms)
        {
            if (tileSet.enemyPrefabs == null || tileSet.enemyPrefabs.Length == 0)
                return;

            Transform enemiesParent = CreateChild("Enemies");
            var usedPositions = new HashSet<Vector2Int>();

            foreach (var room in rooms)
            {
                if (room.type != RoomType.Combat && room.type != RoomType.Boss)
                    continue;

                int enemyCount = room.type == RoomType.Boss ? 1 :
                    Mathf.RoundToInt((room.width * room.height) * config.enemyDensity * 0.1f);

                for (int i = 0; i < enemyCount; i++)
                {
                    Vector2Int pos = GetValidEnemyPosition(room, usedPositions);
                    if (pos != Vector2Int.zero)
                    {
                        GameObject enemyPrefab = room.type == RoomType.Boss ?
                            tileSet.bossPrefab :
                            tileSet.enemyPrefabs[random.Next(tileSet.enemyPrefabs.Length)];

                        if (enemyPrefab != null)
                        {
                            CreateTile(enemyPrefab,
                                new Vector3(pos.x, 1, pos.y),
                                Quaternion.Euler(0, random.Next(360), 0),
                                enemiesParent);

                            usedPositions.Add(pos);
                        }
                    }
                }
            }
        }

        private void SpawnPlayer(Room startRoom)
        {
            if (tileSet.playerPrefab == null) return;

            Vector3 spawnPos = new Vector3(
                startRoom.center.x,
                1,
                startRoom.center.y
            );

            CreateTile(tileSet.playerPrefab, spawnPos, Quaternion.identity, dungeonParent);
        }

        private Vector2Int GetValidDecorationPosition(Room room, HashSet<Vector2Int> usedPositions)
        {
            int attempts = 20;
            while (attempts > 0)
            {
                Vector2Int pos = room.GetRandomPointInside();
                if (!usedPositions.Any(p =>
                    Vector2.Distance(p, pos) < config.minDecorSpacing))
                {
                    return pos;
                }
                attempts--;
            }
            return Vector2Int.zero;
        }

        private Vector2Int GetValidEnemyPosition(Room room, HashSet<Vector2Int> usedPositions)
        {
            int attempts = 20;
            while (attempts > 0)
            {
                Vector2Int pos = room.GetRandomPointInside();
                if (!usedPositions.Any(p =>
                    Vector2.Distance(p, pos) < config.minEnemySpacing))
                {
                    return pos;
                }
                attempts--;
            }
            return Vector2Int.zero;
        }

        private Transform CreateChild(string name)
        {
            var child = new GameObject(name).transform;
            child.parent = dungeonParent;
            return child;
        }

        private GameObject CreateTile(GameObject prefab, Vector3 position,
            Quaternion rotation, Transform parent)
        {
            return Object.Instantiate(prefab, position, rotation, parent);
        }
    }
}