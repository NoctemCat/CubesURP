using UnityEngine;

public static class RegisterServices
{

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        //ServiceLocator.Register(new InventorySystem());
        ServiceLocator.Register(new EventSystem());

        ItemDatabaseObject itemDatabase = Resources.Load<ItemDatabaseObject>("Data/Database");
        itemDatabase.UpdateID();
        ServiceLocator.Register(itemDatabase);

        BiomeDatabase biomeDatabase = Resources.Load<BiomeDatabase>("Data/BiomesDatabase");
        //biomeDatabase.Init();
        ServiceLocator.Register(biomeDatabase);
    }
}