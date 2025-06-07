namespace Model.Units
{
    public class Caveman1 : Unit
    {
        public Caveman1()
            : base(displayName: "Caveman", prefabName: "caveman_1", maxHp: 100, damage: 1, cost: 10, spawnTime: 1,  level: 1)
        {
        }
    }
}