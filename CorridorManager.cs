// CorridorManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGeneration.Managers
{
    public class CorridorManager
    {
        private DungeonConfig config;
        private TileType[,] dungeonGrid;
        private System.Random random;

        public CorridorManager(DungeonConfig config, TileType[,] dungeonGrid, System.Random random)
        {
            this.config = config;
            this.dungeonGrid = dungeonGrid;
            this.random = random;
        }

        public void ConnectRooms(List<Room> rooms)
        {
            if (rooms.Count < 2) return;

            var connectedRooms = new HashSet<Room> { rooms[0] };
            var remainingRooms = new HashSet<Room>(rooms.Skip(1));

            while (remainingRooms.Count > 0)
            {
                Room closestRoom = null;
                Room bestConnector = null;
                float minDistance = float.MaxValue;

                foreach (var roomA in connectedRooms)
                {
                    foreach (var roomB in remainingRooms)
                    {
                        float dist = Vector2.Distance(roomA.center, roomB.center);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestRoom = roomB;
                            bestConnector = roomA;
                        }
                    }
                }

                if (closestRoom != null && bestConnector != null)
                {
                    ConnectTwoRooms(bestConnector, closestRoom);
                    connectedRooms.Add(closestRoom);
                    remainingRooms.Remove(closestRoom);
                }
            }
        }

        private void ConnectTwoRooms(Room roomA, Room roomB)
        {
            // 1. Находим точки соединения комнат
            Vector2Int connectionPointA = FindBestConnectionPoint(roomA, roomB);
            Vector2Int connectionPointB = FindBestConnectionPoint(roomB, roomA);
            
            // 2. Создаем коридор между точками
            CreateCorridor(connectionPointA, connectionPointB);
            
            // 3. Сохраняем точки для дверей
            roomA.doorPositions.Add(connectionPointA);
            roomB.doorPositions.Add(connectionPointB);
        }

        private Vector2Int FindBestConnectionPoint(Room fromRoom, Room toRoom)
        {
            Vector2Int bestPoint = Vector2Int.zero;
            float shortestDistance = float.MaxValue;
            Vector2 targetCenter = toRoom.center;

            // Проверяем все стены комнаты
            List<Vector2Int> wallPoints = GetRoomWallPoints(fromRoom);
            
            foreach (var point in wallPoints)
            {
                float distance = Vector2.Distance(new Vector2(point.x, point.y), targetCenter);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        private List<Vector2Int> GetRoomWallPoints(Room room)
        {
            List<Vector2Int> wallPoints = new List<Vector2Int>();

            // Добавляем точки на горизонтальных стенах
            for (int x = room.x + 1; x < room.x + room.width - 1; x++)
            {
                wallPoints.Add(new Vector2Int(x, room.y)); // Нижняя стена
                wallPoints.Add(new Vector2Int(x, room.y + room.height)); // Верхняя стена
            }

            // Добавляем точки на вертикальных стенах
            for (int y = room.y + 1; y < room.y + room.height - 1; y++)
            {
                wallPoints.Add(new Vector2Int(room.x, y)); // Левая стена
                wallPoints.Add(new Vector2Int(room.x + room.width, y)); // Правая стена
            }

            return wallPoints;
        }

        private void CreateCorridor(Vector2Int startPoint, Vector2Int endPoint)
        {
            Vector2Int current = startPoint;

            // Сначала идем по одной оси
            while (current.x != endPoint.x)
            {
                dungeonGrid[current.x, current.y] = TileType.Floor;
                current.x += (endPoint.x > current.x) ? 1 : -1;
            }

            // Затем по другой оси
            while (current.y != endPoint.y)
            {
                dungeonGrid[current.x, current.y] = TileType.Floor;
                current.y += (endPoint.y > current.y) ? 1 : -1;
            }

            // Отмечаем конечную точку
            dungeonGrid[endPoint.x, endPoint.y] = TileType.Floor;
        }

        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < dungeonGrid.GetLength(0) &&
                   pos.y >= 0 && pos.y < dungeonGrid.GetLength(1);
        }
    }
}