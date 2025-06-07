using System.Collections.Generic;

namespace Managers
{
    public class GameManager
    {
        public static void Reset() => _instance = new GameManager();

        private static GameManager _instance;

        private GameManager()
        {
        }

        public static GameManager Instance => _instance ??= new GameManager();

        public List<Prefabs.Unit> AllyUnits, EnemyUnits;
    }
}