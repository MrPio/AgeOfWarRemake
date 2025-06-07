namespace Model.Units
{
    public class Caveman2 : Unit
    {
        public Caveman2()
            : base(displayName: "Slingshotman", prefabName: "caveman_2", maxHp: 100, damage: 1.5f, cost: 25,
                spawnTime: 2, level: 2, maxDistance: 5f)
        {
        }
    }
}