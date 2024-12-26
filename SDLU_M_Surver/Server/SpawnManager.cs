using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SpawnManager : ISingleton<SpawnManager>
    {
        private readonly FLocation[] JapanPlayerSpawnTable = new FLocation[20]
        {
            new FLocation(-206,10,-45),
            new FLocation(-272,11,-32),
            new FLocation(-238,11,-12),
            new FLocation(-167,11,-65),
            new FLocation(-150,5,-68),
            new FLocation(-278,5,-103),
            new FLocation(-296,5,-39),
            new FLocation(-243,11,-68),
            new FLocation(-269,5,9),
            new FLocation(-150,5,53),
            new FLocation(-154,6,-52),
            new FLocation(-219,-4,-118),
            new FLocation(-268,3,-116),
            new FLocation(-278,12,-67),
            new FLocation(-225,12,78),
            new FLocation(-116,-3.5f,92),
            new FLocation(-200,4,95),
            new FLocation(-187,11,26),
            new FLocation(-206,12,37),
            new FLocation(-150,5,89),
        };
        private readonly FLocation[] JapanPartsSpawnTable = new FLocation[20]
        {
            new FLocation(-206,10,-45),
            new FLocation(-272,11,-32),
            new FLocation(-238,11,-12),
            new FLocation(-187,11,28),
            new FLocation(-167,11,-65),
            new FLocation(-205,12,37),
            new FLocation(-150,5,89),
            new FLocation(-149,5,-68),
            new FLocation(-278,5,-103),
            new FLocation(-296,5,-39),
            new FLocation(-243,11,69),
            new FLocation(-269.5f,5,9),
            new FLocation(-149,5,53),
            new FLocation(-152.4f,6,-52.3f),
            new FLocation(-219,-4,-118.58f),
            new FLocation(-269,3,-116.4f),
            new FLocation(-278.8f,12,-66.6f),
            new FLocation(-225,12,78),
            new FLocation(-200,4,95),
            new FLocation(-116,-3.5f,92.5f),
        };

        private Dictionary<MapType, FLocation[]> pcSpawnDic = new Dictionary<MapType, FLocation[]>();
        private Dictionary<MapType, FLocation[]> partsSpawnDic = new Dictionary<MapType, FLocation[]>();
        private Random _rand = new Random();
        public SpawnManager()
        {
            pcSpawnDic.Add(MapType.Japan, JapanPlayerSpawnTable);
            partsSpawnDic.Add(MapType.Japan, JapanPartsSpawnTable);
        }
        public MapType GetRandomMap()
        {
            return (MapType)_rand.Next(1, 5);
        }
        public FLocation[] GetPartsTable(MapType type)
        {
            return partsSpawnDic[type];
        }
        public HashSet<FLocation> SetParts(MapType map, int cnt)
        {
            HashSet<FLocation> parts = new HashSet<FLocation>();
            while (parts.Count < cnt)
            {
                int n = _rand.Next(0, partsSpawnDic[map].Length);
                parts.Add(partsSpawnDic[map][n]);
            }
            return parts;
        }
        public FLocation[] GetRandomPCSpawnTable(MapType type)
        {
            FLocation[] pts = pcSpawnDic[type];

            for (int i = 0; i < pts.Length; i++)
            {
                int n = _rand.Next(0, pts.Length);
                (pts[i], pts[n]) = (pts[n], pts[i]);
            }
            return pts;
        }
    }
}
