namespace Model.Units
{
    public class Caveman3 : Unit
    {
        public Caveman3()
            : base(displayName: "Dino", prefabName: "caveman_3", maxHp: 100, damage: 5, cost: 100, spawnTime: 5,
                level: 3
            )
        {
        }
    }
}