// Assets/Scripts/DungeonGeneration/Managers/RoomManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration;

namespace DungeonGeneration.Managers
{
    public class RoomManager
    {
        private DungeonConfig config;
        private List<Room> rooms;
        private TileType[,] dungeonGrid;
        private System.Random random;

        public RoomManager(DungeonConfig config, System.Random random)
        {
            this.config = config;
            this.random = random;
            this.rooms = new List<Room>();
            this.dungeonGrid = new TileType[config.dungeonSize.x, config.dungeonSize.y];
        }

        public List<Room> GenerateRooms()
        {
            rooms.Clear();
            ClearGrid();

            for (int i = 0; i < config.numberOfRooms; i++)
            {
                if (!TryCreateRoom())
                {
                    Debug.LogWarning($"Could not place room {i}. Stopping generation.");
                    break;
                }
            }

            AssignRoomTypes();
            return rooms;
        }

        private bool TryCreateRoom()
        {
            int attempts = 50;
            while (attempts > 0)
            {
                int width = Random.Range(config.minRoomSize, config.maxRoomSize + 1);
                int height = Random.Range(config.minRoomSize, config.maxRoomSize + 1);

                int x = Random.Range(1, config.dungeonSize.x - width - 1);
                int y = Random.Range(1, config.dungeonSize.y - height - 1);

                Room newRoom = new Room(x, y, width, height);

                if (IsRoomValid(newRoom))
                {
                    rooms.Add(newRoom);
                    MarkRoomInGrid(newRoom);
                    return true;
                }

                attempts--;
            }
            return false;
        }

        private bool IsRoomValid(Room room)
        {
            if (room.x < 0 || room.x + room.width > config.dungeonSize.x ||
                room.y < 0 || room.y + room.height > config.dungeonSize.y)
                return false;

            return !rooms.Any(other => room.Overlaps(other, Mathf.CeilToInt(config.roomSpacing)));
        }

        private void MarkRoomInGrid(Room room)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    dungeonGrid[x, y] = TileType.Floor;
                }
            }
        }

        private void AssignRoomTypes()
        {
            if (rooms.Count == 0) return;

            Vector2 center = new Vector2(config.dungeonSize.x / 2f, config.dungeonSize.y / 2f);
            Room startRoom = rooms.OrderBy(r => Vector2.Distance(r.center, center)).First();
            startRoom.type = RoomType.Start;

            Room bossRoom = rooms.OrderByDescending(r => 
                Vector2.Distance(startRoom.center, r.center)).First();
            bossRoom.type = RoomType.Boss;

            var remainingRooms = rooms.Where(r => 
                r.type != RoomType.Start && r.type != RoomType.Boss).ToList();

            int treasureCount = Random.Range(config.minTreasureRooms, config.maxTreasureRooms + 1);
            AssignRoomType(remainingRooms, RoomType.Treasure, treasureCount);

            AssignRoomType(remainingRooms, RoomType.Shop, config.shopRoomsCount);
            AssignRoomType(remainingRooms, RoomType.Secret, config.secretRoomsCount);

            foreach (var room in remainingRooms)
            {
                room.type = RoomType.Combat;
            }
        }

        private void AssignRoomType(List<Room> rooms, RoomType type, int count)
        {
            for (int i = 0; i < count && rooms.Count > 0; i++)
            {
                int index = random.Next(rooms.Count);
                rooms[index].type = type;
                rooms.RemoveAt(index);
            }
        }

        private void ClearGrid()
        {
            for (int x = 0; x < config.dungeonSize.x; x++)
                for (int y = 0; y < config.dungeonSize.y; y++)
                    dungeonGrid[x, y] = TileType.Empty;
        }

        public TileType[,] GetGrid() => dungeonGrid;
    }
}